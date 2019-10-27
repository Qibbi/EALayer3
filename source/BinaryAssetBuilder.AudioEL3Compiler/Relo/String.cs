using System;
using System.Runtime.InteropServices;

namespace BinaryAssetBuilder.AudioEL3Compiler.Relo
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public struct String
    {
        public int Length;
        public IntPtr Data;

        public override unsafe string ToString()
        {
            return new string((sbyte*)Data);
        }
    }
}
