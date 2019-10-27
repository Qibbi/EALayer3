using BinaryAssetBuilder.AudioEL3Compiler.Relo;
using BinaryAssetBuilder.AudioEL3Compiler.SageBinaryData;
using BinaryAssetBuilder.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.XPath;
using static BinaryAssetBuilder.AudioEL3Compiler.Encoder;

namespace BinaryAssetBuilder.AudioEL3Compiler
{
    public class Plugin : IAssetBuilderPlugin
    {
        private enum AssetType : uint
        {
            AudioFileMP3Passthrough = 0x15BDBF02u,
            AudioFile = 0x166B084Du
        }

        private static readonly string _tempFilenameSuffix = "BinaryAssetBuilder.AudioCompiler.tempfile";
        private static readonly uint _effictiveAudioPluginVersion = 1001010u;
        private static readonly uint _effectiveAudioPluginVersion360 = _effictiveAudioPluginVersion + 0x001E8480u;

        private Tracer _tracer = Tracer.GetTracer(nameof(AudioEL3Compiler), "Provides Audio processing functionality.");
        private uint _hashCode = 0u;
        private TargetPlatform _platform = TargetPlatform.Win32;

        public Plugin()
        {
        }
        
        private unsafe bool EncodeEALayer3(InstanceDeclaration declaration, AudioFile audioFile, out AssetBuffer result)
        {
            if (_platform != TargetPlatform.Win32) throw new InvalidOperationException("Critical: EALayer3 audio compiler should not be called on non Win32 platforms.");
            if (audioFile.PCCompression != PCAudioCompressionSetting.EALAYER3)
                throw new InvalidOperationException("Critical: EALayer3 audio compiler should not be called on non EALayer3 compression.");
            AudioFileRuntime* audioFileRuntime;
            Tracker tracker = new Tracker(&audioFileRuntime, _platform == TargetPlatform.Xbox360);
            string filePath = audioFile.File;
            if (!filePath.EndsWith(".mp3")) throw new InvalidOperationException("Critical: EALayer3 audio compiler should not be called on non MP3 files.");
            bool isPathEndingWithSound = Path.GetDirectoryName(filePath).ToLower().EndsWith("\\sounds"); // flag2
            bool? isStreamedBox = audioFile.IsStreamedOnPC; // flagPtr
            int? sampleRateBox = audioFile.PCSampleRate; // numPtr1
            int quality = audioFile.PCQuality; // num3
            // compressionType, 1 == none, 29 == xas, 28 == xma (xbox, will actually set to none or same as xas depending on the isPathEndingWithSound flag)
            MpegConverterCompressionType compression; // num4
            switch (audioFile.PCCompression)
            {
                case PCAudioCompressionSetting.NONE:
                    compression = !isPathEndingWithSound ? MpegConverterCompressionType.None : MpegConverterCompressionType.XAS;
                    break;
                case PCAudioCompressionSetting.XAS:
                    compression = MpegConverterCompressionType.XAS;
                    break;
                case PCAudioCompressionSetting.EALAYER3:
                    compression = MpegConverterCompressionType.EALayer3;
                    break;
                default:
                    throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Internal error: xml compiler returned bad PC audio compression type of {0}.", audioFile.PCCompression);
            }
            bool isStreamed = isStreamedBox ?? !isPathEndingWithSound; // flag1
            if (audioFile.SubtitleStringName != null)
            {
                Relo.Marshaler.Marshal(audioFile.SubtitleStringName, &audioFileRuntime->SubtitleStringName, tracker);
            }
            else
            {
                Relo.Marshaler.Marshal($"DIALOGEVENT:{Path.GetFileNameWithoutExtension(filePath)}SubTitle", &audioFileRuntime->SubtitleStringName, tracker);
            }
            audioFileRuntime->NumberOfChannels = 0;
            audioFileRuntime->NumberOfSamples = 0;
            audioFileRuntime->SampleRate = 0;
            audioFileRuntime->HeaderData = IntPtr.Zero;
            audioFileRuntime->HeaderDataSize = 0;
            // identify format (SIMEX_id(streamPtr))            - we don't need that, though we might want to check for mp3
            // check format needs to be 0 or 1 aka WAVE or AIFF - we don't need that, though we might want to check for mp3
            // open audio file (SIMEX_open(streamPtr, &instancePtr, format)
            using (MpegConverter converter = new MpegConverter(filePath))
            {
                string tempFile = declaration.CustomDataPath + _tempFilenameSuffix; // str2
                _tracer.TraceNote("Creating temp file {0}", tempFile);
                using (AutoCleanUpTempFiles cleanUpTempFiles = new AutoCleanUpTempFiles(tempFile))
                {
                    // set temp filename SIMEX_setfilename(str2ptr)
                    converter.SetOutputFilePath(tempFile);
                    // create temp output file SIMEX_create(null, &instancePtr, 39)
                    try
                    {
                        converter.CreateOutputFiles();
                        MpegConverterSettings converterSettings = converter.GetSettings();
                        // start iterating through elements (num elements is returned by SIMEX_open)
                        // get info SIMEX_info(instancePtr, &infoPtr, idx)
                        // read element SIMEX_read(instancePtr, infoPtr, idx)
                        if (isStreamed)
                        {
                            // infoPtr + 526 = 0x1000;
                            converterSettings.IsStreamed = true;
                            _tracer.TraceNote("Setting play location to streamed.");
                        }
                        else
                        {
                            // infoPtr + 526 = 0x0800;
                            converterSettings.IsStreamed = false;
                            _tracer.TraceNote("Setting play location to RAM.");
                        }
                        // infoPtr + 509 = compression;
                        converterSettings.CompressionType = compression;
                        _tracer.TraceNote("Setting compression type to {0}.", compression); // SIMEX_getsamplerepname(compression)
                        if (compression == MpegConverterCompressionType.XMA || compression == MpegConverterCompressionType.EALayer3)
                        {
                            if (quality < 0 || quality > 100)
                                throw new BinaryAssetBuilderException(ErrorCode.InvalidArgument, "Audio file {0}: Quality parameter must be between 0 and 100.", declaration);
                            // infoPtr + 536 = quality
                            converterSettings.CompressionQuality = quality;
                            _tracer.TraceNote("Setting compression quality to {0}.", quality);
                        }
                        // if (sampleRateBox.HasValue && sampleRateBox.Value != *(int*)(*(infoPtr + 540))
                        if (sampleRateBox.HasValue && sampleRateBox.Value != converterSettings.SampleRate)
                        {
                            int sampleRate = sampleRateBox.Value;
                            if (sampleRate < 400 || sampleRate > 96000)
                                throw new BinaryAssetBuilderException(ErrorCode.InvalidArgument, "Audio file {0}: Sample rate must be between 400 and 96000.", declaration);
                            _tracer.TraceNote("Resampling from {0} to {1}", converterSettings.SampleRate, sampleRate); // infoPtr + 540
                            // TODO: resampling
                            _tracer.TraceWarning("Warning: Resampling is currently not implemented.");
                        }
                        // if (*(int*)infoPtr != 0)
                        audioFileRuntime->NumberOfChannels = (byte)converterSettings.NumberOfChannels; // infoPtr + 510
                        audioFileRuntime->NumberOfSamples = converterSettings.NumberOfSamples; // infoPtr + 544
                        audioFileRuntime->SampleRate = converterSettings.SampleRate; // infoPtr + 540
                        if (audioFileRuntime->NumberOfChannels != 1 && audioFileRuntime->NumberOfChannels != 2 && audioFileRuntime->NumberOfChannels != 4 && audioFileRuntime->NumberOfChannels != 6)
                            _tracer.TraceWarning("Warning: Audio file {0} has {1} channels. The only supported channel counts are 1, 2, 4, and 6; sample will probably use only the first channel in the engine.", declaration, audioFileRuntime->NumberOfChannels);
                        // TODO if (!SIMEX_write(tmpFile, infoPtr, idx))
                        if (converter.WriteOutput(converterSettings))
                            throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Internal error writing element of \"{0}\".", tempFile);
                    }
                    finally
                    {
                        converter.CloseOutputFiles();
                    }
                    string tempFileSnr = tempFile + ".snr";
                    if (isStreamed)
                    {
                        string tempFileSns = tempFile + ".sns";
                        if (File.Exists(declaration.CustomDataPath))
                        {
                            File.Delete(declaration.CustomDataPath);
                        }
                        _tracer.TraceNote("Creating output file {0}\n", declaration.CustomDataPath);
                        File.Move(tempFileSns, declaration.CustomDataPath);
                        using (Stream headerStream = new FileStream(tempFileSnr, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            int length = (int)headerStream.Length;
                            sbyte** headerData = (sbyte**)audioFileRuntime->HeaderData;
                            tracker.Push((void**)headerData, 1, length);
                            headerStream.Read((IntPtr)(*(int*)audioFileRuntime->HeaderData), length);
                            audioFileRuntime->HeaderDataSize = length;
                            tracker.Pop();
                        }
                    }
                    else
                    {
                        if (File.Exists(declaration.CustomDataPath))
                        {
                            File.Delete(declaration.CustomDataPath);
                        }
                        _tracer.TraceNote("Creating output file {0}\n", declaration.CustomDataPath);
                        File.Move(tempFileSnr, declaration.CustomDataPath);
                    }
                    if (tracker.IsBigEndian)
                    {
                        Tracker.ByteSwap32((uint*)&audioFileRuntime->NumberOfSamples);
                        Tracker.ByteSwap32((uint*)&audioFileRuntime->SampleRate);
                        Tracker.ByteSwap32((uint*)&audioFileRuntime->HeaderDataSize);
                    }
                    result = new AssetBuffer();
                    FinalizeTracker(tracker, result);
                }
            }
            return true;
        }

        public void Initialize(object configObject, TargetPlatform platform)
        {
            _hashCode = HashProvider.GetTypeHash(GetType());
            _platform = platform;
        }

        public uint GetAllTypesHash()
        {
            return 0xEB19D975u;
        }

        public ExtendedTypeInformation GetExtendedTypeInformation(uint typeId)
        {
            ExtendedTypeInformation result = new ExtendedTypeInformation
            {
                HasCustomData = true,
                TypeId = typeId
            };
            AssetType assetType = (AssetType)typeId;
            switch (assetType)
            {
                case AssetType.AudioFileMP3Passthrough:
                    result.ProcessingHash = _platform != TargetPlatform.Xbox360 ? _effictiveAudioPluginVersion ^ 0x3520BB9Cu : _effectiveAudioPluginVersion360 ^ 0x3520BB9Cu;
                    result.TypeHash = 0x610DB321u;
                    result.TypeName = nameof(AssetType.AudioFileMP3Passthrough);
                    break;
                case AssetType.AudioFile:
                    result.ProcessingHash = _platform != TargetPlatform.Xbox360 ? _effictiveAudioPluginVersion ^ 0x83398E45u : _effectiveAudioPluginVersion360 ^ 0x83398E45u;
                    result.TypeHash = 0x46410F77u;
                    result.TypeName = nameof(AssetType.AudioFile);
                    break;
            }
            return result;
        }

        public AssetBuffer ProcessAudioFileInstance(InstanceDeclaration declaration)
        {
            XmlNamespaceManager namespaceManager = declaration.Document.NamespaceManager;
            XPathNavigator navigator = declaration.Node.CreateNavigator();
            // TODO: initialize tracker on new audiofileruntimeptr
            AudioFile audioFile = AudioFile.MarshalFromNode(declaration.Node);
            if (_platform != TargetPlatform.Win32 || audioFile.PCCompression != PCAudioCompressionSetting.EALAYER3)
            {
                foreach (PluginDescriptor plugin in Settings.Current.Plugins)
                {
                    if (plugin.Plugin is AudioCompiler.Plugin acPlugin)
                    {
                        return acPlugin.ProcessAudioFileInstance(declaration);
                    }
                }
                throw new InvalidOperationException("Critical: Original EALA BinaryAssetBuilder.AudioCompiler.dll plugin not found.");
            }
            if (!EncodeEALayer3(declaration, audioFile, out AssetBuffer result))
                throw new BinaryAssetBuilderException(ErrorCode.InternalError, "EALayer3 audio compiler was unable to compile AudioFile:{0}.", audioFile.id);
            return result;
        }

        public unsafe void FinalizeTracker(Tracker tracker, AssetBuffer buffer)
        {
            using (Chunk chunk = new Chunk())
            {
                tracker.MakeRelocatable(chunk);
                buffer.InstanceData = new byte[chunk.InstanceBufferSize];
                if (chunk.InstanceBufferSize > 0)
                {
                    fixed (byte* pBuffer = &buffer.InstanceData[0])
                    {
                        MarshalUtil.CopyMemory((IntPtr)pBuffer, chunk.InstanceBuffer, chunk.InstanceBufferSize);
                    }
                }
                buffer.RelocationData = new byte[chunk.RelocationBufferSize];
                if (chunk.RelocationBufferSize > 0)
                {
                    fixed (byte* pBuffer = &buffer.RelocationData[0])
                    {
                        MarshalUtil.CopyMemory((IntPtr)pBuffer, chunk.RelocationBuffer, chunk.RelocationBufferSize);
                    }
                }
                buffer.ImportsData = new byte[chunk.ImportsBufferSize];
                if (chunk.ImportsBufferSize > 0)
                {
                    fixed (byte* pBuffer = &buffer.ImportsData[0])
                    {
                        MarshalUtil.CopyMemory((IntPtr)pBuffer, chunk.ImportsBuffer, chunk.ImportsBufferSize);
                    }
                }
            }
        }

        public AssetBuffer ProcessMP3PassthroughInstance(InstanceDeclaration declaration)
        {
            foreach (PluginDescriptor plugin in Settings.Current.Plugins)
            {
                if (plugin.Plugin is AudioCompiler.Plugin acPlugin)
                {
                    return acPlugin.ProcessMP3PassthroughInstance(declaration);
                }
            }
            throw new InvalidOperationException("Critical: Original EALA BinaryAssetBuilder.AudioCompiler.dll plugin not found.");
        }

        public AssetBuffer ProcessInstance(InstanceDeclaration declaration)
        {
            AssetBuffer result;
            if ((AssetType)declaration.Handle.TypeId == AssetType.AudioFile)
            {
                result = ProcessAudioFileInstance(declaration);
            }
            else if ((AssetType)declaration.Handle.TypeId == AssetType.AudioFileMP3Passthrough)
            {
                result = ProcessMP3PassthroughInstance(declaration);
            }
            else
            {
                _tracer.TraceWarning($"Warning: Couldn't process {declaration}. No matching handler found.");
                result = null;
            }
            return result;
        }
    }
}
