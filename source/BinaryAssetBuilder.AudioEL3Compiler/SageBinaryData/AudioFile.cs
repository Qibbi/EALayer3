using System;
using System.Xml;

namespace BinaryAssetBuilder.AudioEL3Compiler.SageBinaryData
{
    internal enum PCAudioCompressionSetting
    {
        NONE,
        XAS,
        EALAYER3
    }

    internal enum XenonAudioCompressionSetting
    {
        NONE,
        XMA,
        EALAYER3
    }

    // [StructLayout(LayoutKind.Sequential, Size = 52)]
    internal class AudioFile
    {
        public string id; // 0 - zero
        public string File; // 4
        public int? PCSampleRate; // 12
        public PCAudioCompressionSetting? PCCompression; // 16
        public int PCQuality = 75; // 20
        public bool? IsStreamedOnPC; // 24
        public int? XenonSampleRate; // 28
        public XenonAudioCompressionSetting? XenonCompression; // 32
        public int XenonQuality = 75; // 36
        public bool? IsStreamedOnXenon; // 40
        public string SubtitleStringName; // 44
        public string GUIPreset; // 48

        public static AudioFile MarshalFromNode(XmlNode node)
        {
            AudioFile result = new AudioFile();
            result.id = node.Attributes[nameof(id)].Value;
            if (node.Attributes[nameof(File)] == null) throw new InvalidOperationException($"Critical: Required node 'File' not found in AudioFile:{node.Attributes["id"]}.");
            result.File = node.Attributes[nameof(File)].Value;
            if (node.Attributes[nameof(PCSampleRate)] != null)
            {
                result.PCSampleRate = int.Parse(node.Attributes[nameof(PCSampleRate)].Value);
            }
            if (node.Attributes[nameof(PCCompression)] != null)
            {
                result.PCCompression = (PCAudioCompressionSetting)Enum.Parse(typeof(PCAudioCompressionSetting), node.Attributes[nameof(PCCompression)].Value);
            }
            if (node.Attributes[nameof(PCQuality)] != null)
            {
                result.PCQuality = int.Parse(node.Attributes[nameof(PCQuality)].Value);
            }
            if (node.Attributes[nameof(XenonSampleRate)] != null)
            {
                result.XenonSampleRate = int.Parse(node.Attributes[nameof(XenonSampleRate)].Value);
            }
            if (node.Attributes[nameof(XenonCompression)] != null)
            {
                result.XenonCompression = (XenonAudioCompressionSetting)Enum.Parse(typeof(XenonAudioCompressionSetting), node.Attributes[nameof(XenonCompression)].Value);
            }
            if (node.Attributes[nameof(XenonQuality)] != null)
            {
                result.XenonQuality = int.Parse(node.Attributes[nameof(XenonQuality)].Value);
            }
            if (node.Attributes[nameof(SubtitleStringName)] != null)
            {
                result.SubtitleStringName = node.Attributes[nameof(SubtitleStringName)].Value;
            }
            if (node.Attributes[nameof(GUIPreset)] != null)
            {
                result.GUIPreset = node.Attributes[nameof(GUIPreset)].Value;
            }
            if (node.Attributes[nameof(IsStreamedOnPC)] != null)
            {
                result.IsStreamedOnPC = bool.Parse(node.Attributes[nameof(IsStreamedOnPC)].Value);
            }
            if (node.Attributes[nameof(IsStreamedOnXenon)] != null)
            {
                result.IsStreamedOnXenon = bool.Parse(node.Attributes[nameof(IsStreamedOnXenon)].Value);
            }
            return result;
        }
    }
}
