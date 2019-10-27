using BinaryAssetBuilder.AudioEL3Compiler.Relo;
using System;
using System.IO;

namespace BinaryAssetBuilder.AudioEL3Compiler
{
    public static class StreamExtensions
    {
        public static unsafe int Read(this Stream stream, IntPtr buffer, int count)
        {
            byte[] temp = new byte[count];
            int result = stream.Read(temp, 0, count);
            fixed (byte* fpTemp = &temp[0])
            {
                MarshalUtil.CopyMemory(buffer, (IntPtr)fpTemp, count);
            }
            return result;
        }
    }
}
