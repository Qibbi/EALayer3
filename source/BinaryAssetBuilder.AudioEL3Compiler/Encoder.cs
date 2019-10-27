using System.Collections.Generic;

namespace BinaryAssetBuilder.AudioEL3Compiler
{
    public class Encoder
    {
        public enum UncompressedSampleMode
        {
            REPLACE_ALL,
            REPLACE_PART
        }

        public class UncompressedSampleFrame
        {
            public UncompressedSampleMode Mode;
            public int Count;
            public int OffsetInOutput;
            public short[] Data;
        }

        public class ChannelInfo
        {
            public int Scfsi;
            public int Size;
            public int SideInfoX;
            public int SideInfoY;
        }

        public class Granule
        {
            public bool IsUsed;
            public MpegVersion Version;
            public byte SampleRateIndex;
            public int SampleRate;
            public MpegChannelType ChannelMode;
            public int NumberOfChannels;
            public byte ModeExtension;
            public byte Index;
            public byte[] Data;
            public int DataSize;
            public int DataSizeBits;
            public UncompressedSampleFrame Uncompressed;
            public List<ChannelInfo> ChannelInfo;

            public Granule()
            {
                IsUsed = false;
                Version = 0;
                Uncompressed = new UncompressedSampleFrame();
                ChannelInfo = new List<ChannelInfo>();
            }
        }

        public class Frame
        {
            public Granule[] Granules;


            public Frame()
            {
                Granules = new[] { new Granule(), new Granule() };
            }
        }
    }
}
