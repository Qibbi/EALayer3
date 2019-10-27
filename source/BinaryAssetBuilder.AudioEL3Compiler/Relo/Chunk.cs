using System;

namespace BinaryAssetBuilder.AudioEL3Compiler.Relo
{
    public class Chunk : IDisposable
    {
        public IntPtr InstanceBuffer; // 00
        public int InstanceBufferSize; // 04
        public IntPtr RelocationBuffer; // 08
        public int RelocationBufferSize; // 12
        public IntPtr ImportsBuffer; // 16
        public int ImportsBufferSize; // 20

        public Chunk()
        {
            InstanceBuffer = IntPtr.Zero;
            InstanceBufferSize = 0;
            RelocationBuffer = IntPtr.Zero;
            RelocationBufferSize = 0;
            ImportsBuffer = IntPtr.Zero;
            ImportsBufferSize = 0;
        }

        internal void Allocate(int instanceBufferSize, int relocationBufferSize, int importsBufferSize)
        {
            InstanceBufferSize = instanceBufferSize;
            RelocationBufferSize = relocationBufferSize;
            ImportsBufferSize = importsBufferSize;
            InstanceBuffer = MarshalUtil.AllocateMemory(InstanceBufferSize + 4);
            if (RelocationBufferSize > 0)
            {
                RelocationBuffer = MarshalUtil.AllocateMemory(RelocationBufferSize);
            }
            if (ImportsBufferSize > 0)
            {
                ImportsBuffer = MarshalUtil.AllocateMemory(ImportsBufferSize);
            }
        }

        public void Dispose()
        {
            if (InstanceBuffer != IntPtr.Zero)
            {
                MarshalUtil.FreeMemory(InstanceBuffer);
                InstanceBuffer = IntPtr.Zero;
            }
            if (RelocationBuffer != IntPtr.Zero)
            {
                MarshalUtil.FreeMemory(RelocationBuffer);
                RelocationBuffer = IntPtr.Zero;
            }
            if (ImportsBuffer != IntPtr.Zero)
            {
                MarshalUtil.FreeMemory(ImportsBuffer);
                ImportsBuffer = IntPtr.Zero;
            }
        }
    }
}
