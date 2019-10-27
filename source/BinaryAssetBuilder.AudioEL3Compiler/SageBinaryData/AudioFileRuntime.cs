using System;
using System.Runtime.InteropServices;

namespace BinaryAssetBuilder.AudioEL3Compiler.SageBinaryData
{
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct AudioFileRuntime
    {
        public int Zero; // 00
        public Relo.String SubtitleStringName; // 04
        // public int SubtitleStringNameLength;
        // public int SubtitleStringNameOffset;
        public int NumberOfSamples; // 12
        public int SampleRate; // 16
        public IntPtr HeaderData; // 20
        public int HeaderDataSize; // 24
        public byte NumberOfChannels; // 28
    }

    // internal class AudioFileRuntime
    // {
    //     public string SubtitleStringName;
    //     public int NumberOfSamples;
    //     public int SampleRate;
    //     public int HeaderData;
    //     public int HeaderDataSize;
    //     public byte NumberOfChannels;
    // }
}
