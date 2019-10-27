using BinaryAssetBuilder.AudioEL3Compiler.Relo;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BinaryAssetBuilder.AudioEL3Compiler
{
    public enum MpegVersion : byte
    {
        V2_5,
        Reserved,
        V2,
        V1
    }

    public enum MpegChannelType : byte
    {
        Stereo,
        JointStereo,
        DualChannel,
        Mono
    }

    public class MpegFrameHeader
    {
        public MpegVersion Version;
        public bool HasCrc;
        public byte BitrateIndex;
        public byte SampleRateIndex;
        public bool HasPadding;
        public MpegChannelType ChannelMode;
        public byte ModeExtension;
        public int HeaderSize;
        public int SampleRate;
        public int FrameSize;
        public int NumberOfChannels;
    }

    public enum MpegConverterCompressionType
    {
        None = 1,
        XMA = 28,
        XAS = 29,
        EALayer3 = 30
    }

    public class MpegConverterSettings
    {
        public bool IsStreamed { get; set; }
        public MpegConverterCompressionType CompressionType { get; set; }
        public int CompressionQuality { get; set; }
        public int SampleRate { get; set; }
        public int NumberOfChannels { get; set; }
        public int NumberOfSamples { get; set; }
    }

    public class MpegConverter : IDisposable
    {
        private enum AudioFileDataCompressionType : byte
        {
            Uncompressed = 2,
            XMA,
            XAS,
            EALayer3_EL31,
            EALayer3_L32P,
            EALayer3_L32S
        }

        private enum AudioFileDataChannels : byte
        {
            Mono,
            Stereo = 4,
            Quad = 12,
            Surround = 20
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct AudioFileDataSns
        {
            [FieldOffset(0)] public AudioFileDataCompressionType CompressionType;
            [FieldOffset(1)] public AudioFileDataChannels Channels;
            [FieldOffset(2)] public short SampleRate;
            [FieldOffset(4)] public byte Unknown; // 0x40 when in .asset
            [FieldOffset(4)] public int NumberOfSamples;
        }

        [StructLayout(LayoutKind.Sequential, Size = 12)]
        private struct AudioFileDataHeader
        {
            public AudioFileDataCompressionType CompressionType;
            public AudioFileDataChannels Channels;
            public short SampleRate;
            public int NumberOfSamples;
            public int Size;
        }

        private static readonly int[,] _sampleRateTable = new[,]
        {
            { 11025, 12000, 8000, 0 },
            { 0, 0, 0, 0 },
            { 22050, 24000, 16000, 0 },
            { 44100, 48000, 32000, 0 }
        };
        private static readonly int[,] _bitrateTable = new[,]
        {
            { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 ,0, 0, 0, 0, 0, 0, 0 },
            { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },
            { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 }
        };

        private Stream _input;
        private string _outFilePath;
        private Stream _outputSnr;
        private Stream _outputSns;

        public MpegConverter(string path)
        {
            _input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        private unsafe uint ReadSyncSafeInt(uint value)
        {
            Endian.ByteSwapInt32(&value);
            return (value & 0x0000007Fu) | (value & 0x00007F00u) >> 1 | (value & 0x007F0000u) >> 2 | (value & 0x7F000000u) >> 3;
        }

        private unsafe void SkipID3Tag(byte[] header)
        {
            int size = 10;
            fixed (byte* pSize = &header[6])
            {
                size += (int)ReadSyncSafeInt(*(uint*)pSize);
            }
            if (header[3] == 4)
            {
                if ((header[5] & 0x10) != 0)
                {
                    size += 10;
                }
            }
            _input.Seek(size, SeekOrigin.Current);
        }

        private bool ParseMpegFrameHeader(MpegFrameHeader mpegFrameHeader, byte[] header)
        {
            BitStream bitStream = new BitStream(header);
            if (bitStream.ReadUInt16(11) != 0x07FFu) throw new BinaryAssetBuilderException(ErrorCode.InternalError, "MP3 sync bits do not match.");
            mpegFrameHeader.Version = (MpegVersion)bitStream.ReadByte(2);
            if (bitStream.ReadByte(2) != 0x01u) throw new BinaryAssetBuilderException(ErrorCode.InternalError, "MP3 frame header error.");
            mpegFrameHeader.HasCrc = bitStream.ReadBit();
            mpegFrameHeader.BitrateIndex = bitStream.ReadByte(4);
            mpegFrameHeader.SampleRateIndex = bitStream.ReadByte(2);
            mpegFrameHeader.HasPadding = bitStream.ReadBit();
            bitStream.ReadBit();
            mpegFrameHeader.ChannelMode = (MpegChannelType)bitStream.ReadByte(2);
            mpegFrameHeader.ModeExtension = bitStream.ReadByte(2);
            mpegFrameHeader.HeaderSize = 4 + (mpegFrameHeader.HasCrc ? 2 : 0);
            mpegFrameHeader.SampleRate = _sampleRateTable[(int)mpegFrameHeader.Version, mpegFrameHeader.SampleRateIndex];
            switch (mpegFrameHeader.Version)
            {
                case MpegVersion.V1:
                    mpegFrameHeader.FrameSize = 144000 * _bitrateTable[(int)mpegFrameHeader.Version, mpegFrameHeader.BitrateIndex] / mpegFrameHeader.SampleRate;
                    break;
                case MpegVersion.V2:
                case MpegVersion.V2_5:
                    mpegFrameHeader.FrameSize = 144000 * _bitrateTable[(int)mpegFrameHeader.Version, mpegFrameHeader.BitrateIndex] / (mpegFrameHeader.SampleRate / 2);
                    break;
                default:
                    throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Bad MP3 version.");
            }
            if (mpegFrameHeader.HasPadding)
            {
                ++mpegFrameHeader.FrameSize;
            }
            mpegFrameHeader.NumberOfChannels = mpegFrameHeader.ChannelMode == MpegChannelType.Mono ? 1 : 2;
            return true;
        }

        public MpegFrameHeader ReadFrameHeader()
        {
            byte[] header = new byte[10];
            long frameStart = _input.Position;
            while (header[0] != 0xFF)
            {
                frameStart = _input.Position;
                int value = _input.ReadByte();
                if (value == -1)
                {
                    return null;
                }
                header[0] = (byte)value;
            }
            _input.Read(header, 1, 9);
            _input.Seek(frameStart, SeekOrigin.Begin);
            MpegFrameHeader result = new MpegFrameHeader();
            if (!ParseMpegFrameHeader(result, header))
                throw new BinaryAssetBuilderException(ErrorCode.InvalidArgument, "Could not parse MP3 frame header.");
            _input.Seek(result.HeaderSize, SeekOrigin.Current);
            return result;
        }

        public MpegConverterSettings GetSettings()
        {
            MpegConverterSettings result = new MpegConverterSettings();
            // at the moment we ignore everything but IsStreamed & channels anyway, we assume channels are same for every mp3 header
            byte[] header = new byte[10];
            long frameStart = _input.Position;
            _input.Read(header, 0, 10);
            _input.Seek(frameStart, SeekOrigin.Begin);
            if (header[0] == 'I' && header[1] == 'D' && header[2] == '3')
            {
                SkipID3Tag(header);
            }
            while (header[0] != 0xFF)
            {
                frameStart = _input.Position;
                int value = _input.ReadByte();
                if (value == -1)
                    throw new BinaryAssetBuilderException(ErrorCode.InvalidArgument, "Could not find MP3 frame header.");
                header[0] = (byte)value;
            }
            _input.Read(header, 1, 9);
            _input.Seek(frameStart, SeekOrigin.Begin);
            MpegFrameHeader frameHeader = new MpegFrameHeader();
            if (!ParseMpegFrameHeader(frameHeader, header))
                throw new BinaryAssetBuilderException(ErrorCode.InvalidArgument, "Could not parse MP3 frame header.");
            result.NumberOfChannels = frameHeader.NumberOfChannels;
            result.SampleRate = frameHeader.SampleRate;
            return result;
        }

        public void SetOutputFilePath(string path)
        {
            _outFilePath = path;
        }

        public void CreateOutputFiles()
        {
            _outputSnr = new FileStream(_outFilePath + ".snr", FileMode.Create, FileAccess.Write, FileShare.None);
            _outputSns = new FileStream(_outFilePath + ".sns", FileMode.Create, FileAccess.Write, FileShare.None);
        }

        public class BlockInfo
        {
            public int NumberOfSamples;
            public int Size;
        }

        public unsafe bool WriteOutput(BlockInfo info, MpegConverterSettings settings)
        {
            AudioFileDataHeader audioFileDataHeader = new AudioFileDataHeader
            {
                CompressionType = AudioFileDataCompressionType.EALayer3_EL31,
                Channels = settings.NumberOfChannels == 1 ? AudioFileDataChannels.Mono : AudioFileDataChannels.Stereo,
                SampleRate = (short)settings.SampleRate,
                NumberOfSamples = info.NumberOfSamples,
                Size = info.Size
            };
            Endian.ByteSwapInt16((ushort*)&audioFileDataHeader.SampleRate);
            Endian.ByteSwapInt32((uint*)&audioFileDataHeader.NumberOfSamples);
            Endian.ByteSwapInt32((uint*)&audioFileDataHeader.Size);
            if (settings.IsStreamed)
            {
                byte[] buffer = new byte[2];
                _outputSnr.Write(buffer, 0, buffer.Length);
            }
            else
            {
                byte[] buffer = new byte[sizeof(AudioFileDataHeader)];
                fixed (byte* pBuffer = &buffer[0])
                {
                    MarshalUtil.CopyMemory((IntPtr)pBuffer, (IntPtr)(byte*)&audioFileDataHeader, sizeof(AudioFileDataHeader));
                }
                _outputSnr.Write(buffer, 0, buffer.Length);
            }
            MpegFrameHeader frameHeader;
            while ((frameHeader = ReadFrameHeader()) != null)
            {
                BitStreamWriter writer = new BitStreamWriter();
                writer.Write(0, 8);
                writer.Write((byte)frameHeader.Version, 2);
                writer.Write(frameHeader.SampleRateIndex, 2);
                writer.Write((byte)frameHeader.ChannelMode, 2);
                writer.Write(frameHeader.ModeExtension, 2);
                // read frame data
                writer.Write(); // TODO: is granule index == 1
                // if granule index == 1 && version == mv_1
                //   for channels -> 4 bits scfsi
                // for channels -> channel info 12 + 32 + x bits
                // add up data size
                // data size += 8 - size % 8
                // data size /= 8
                // if data size > 0
                // copy data
            }
            return true;
        }

        public void CloseOutputFiles()
        {
            if (_outputSnr != null)
            {
                _outputSnr.Dispose();
                _outputSnr = null;
            }
            if (_outputSns != null)
            {
                _outputSns.Dispose();
                _outputSns = null;
            }
        }

        public void Dispose()
        {
            if (_input != null)
            {
                _input.Dispose();
                _input = null;
            }
        }
    }
}
