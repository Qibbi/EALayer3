using BinaryAssetBuilder.AudioEL3Compiler.SageBinaryData;
using System;
using System.Collections.Generic;

namespace BinaryAssetBuilder.AudioEL3Compiler.Relo
{
    public class Tracker : IDisposable
    {
        private class Bookmark
        {
            public int Index; // 00
            public int From; // 04
            public int To; // 08
        }

        private class Block
        {
            public IntPtr Data; // 00
            public int Size; // 04

            public Block(int size)
            {
                Data = MarshalUtil.AllocateClearedMemory(size);
                Size = size;
            }
        }

        private int _instanceBufferSize; // 00
        private List<int> _stack; // 04
        private List<Block> _blocks; // 24
        private List<Bookmark> _relocations; // 40
        private List<Bookmark> _imports; // 56

        public bool IsBigEndian { get; } // 72

        private static bool BookmarkCompare(Bookmark x, Bookmark y)
        {
            return x.Index < y.Index || x.Index <= y.Index && x.To < y.To;
        }

        internal static unsafe void ByteSwap32(uint* pValue)
        {
            uint nValue;
            byte* bpValue = (byte*)pValue;
            byte* pNValue = (byte*)&nValue;
            *pNValue = bpValue[3];
            pNValue[1] = bpValue[2];
            pNValue[2] = bpValue[1];
            pNValue[3] = *bpValue;
            *pValue = nValue;
        }

        public unsafe Tracker(AudioFileRuntime** asset, bool isBigEndian)
        {
            _stack = new List<int>();
            _blocks = new List<Block>();
            _relocations = new List<Bookmark>();
            _imports = new List<Bookmark>();
            int blockIndex = Allocate(1, MarshalUtil.SizeOf(typeof(AudioFileRuntime)));
            _stack.Add(blockIndex);
            *(IntPtr*)asset = _blocks[blockIndex].Data;
            IsBigEndian = isBigEndian;
        }

        private unsafe int Allocate(int count, int size)
        {
            int blockSize = (count * size + 3) & -4;
            Block block = new Block(blockSize);
            _blocks.Add(block);
            _instanceBufferSize += blockSize;
            return _blocks.Count - 1;
        }

        public unsafe void* Push(void** pointerLocation, int size, int count)
        {
            int index = _stack[_stack.Count - 1];
            int newIndex = Allocate(count, size);
            _stack.Add(newIndex);
            if ((IntPtr)pointerLocation == IntPtr.Zero)
            {
                return (void*)IntPtr.Zero;
            }
            Bookmark bookmark = new Bookmark
            {
                Index = index,
                From = (int)((byte*)pointerLocation - (byte*)_blocks[index].Data),
                To = newIndex
            };
            *(IntPtr*)pointerLocation = _blocks[newIndex].Data;
            _relocations.Add(bookmark);
            return *pointerLocation;
        }

        public void Pop()
        {
            _stack.RemoveAt(_stack.Count - 1);
        }

        public unsafe void AddReference(void* location, int value)
        {
            int index = _stack[_stack.Count - 1];
            Bookmark bookmark = new Bookmark
            {
                Index = index,
                From = (int)((byte*)location - (byte*)_blocks[index].Data),
                To = value
            };
            *(int*)location = value;
            _imports.Add(bookmark);
        }

        public unsafe void MakeRelocatable(Chunk chunk)
        {
            int importsBufferSize = 0;
            if (_imports.Count > 0)
            {
                importsBufferSize = (_imports.Count + 1) * 4;
            }
            int relocationBufferSize = 0;
            if (_relocations.Count > 0)
            {
                relocationBufferSize = (_relocations.Count + 1) * 4;
            }
            chunk.Allocate(_instanceBufferSize, relocationBufferSize, importsBufferSize);
            byte* instanceBuffer = (byte*)chunk.InstanceBuffer;
            byte* instanceBufferPosition = instanceBuffer;
            int blockCount = _blocks.Count;
            int* bookmarks = (int*)MarshalUtil.AllocateMemory(blockCount > 0x3FFFFFFF ? uint.MaxValue : (uint)(blockCount * 4));
            int idx = 0;
            foreach (Block block in _blocks)
            {
                MarshalUtil.CopyMemory((IntPtr)instanceBufferPosition, block.Data, block.Size);
                bookmarks[idx++] = (int)(instanceBufferPosition - instanceBuffer);
                instanceBufferPosition += block.Size;
            }
            if (relocationBufferSize > 0)
            {
                int* relocationBuffer = (int*)chunk.RelocationBuffer;
                _relocations.Sort(new Comparison<Bookmark>((x, y) => BookmarkCompare(x, y) ? -1 : 1));
                foreach (Bookmark relocation in _relocations)
                {
                    int from = bookmarks[relocation.Index] + relocation.From;
                    *relocationBuffer = from;
                    if (IsBigEndian)
                    {
                        ByteSwap32((uint*)relocationBuffer);
                    }
                    int to = bookmarks[relocation.To];
                    if (IsBigEndian)
                    {
                        ByteSwap32((uint*)&to);
                    }
                    *(int*)(instanceBuffer + from) = to;
                    relocationBuffer++;
                }
                *relocationBuffer = -1;
            }
            if (importsBufferSize > 0u)
            {
                int* importsBuffer = (int*)chunk.ImportsBuffer;
                _imports.Sort(new Comparison<Bookmark>((x, y) => BookmarkCompare(x, y) ? -1 : 1));
                foreach (Bookmark import in _imports)
                {
                    int from = bookmarks[import.Index] + import.From;
                    *importsBuffer = from;
                    if (IsBigEndian)
                    {
                        ByteSwap32((uint*)importsBuffer);
                    }
                    int to = import.To;
                    if (IsBigEndian)
                    {
                        ByteSwap32((uint*)&to);
                    }
                    *(int*)(instanceBuffer + from) = to;
                    importsBuffer++;
                }
                *importsBuffer = -1;
            }
        }

        public unsafe void Dispose()
        {
            while (_blocks.Count != 0)
            {
                MarshalUtil.FreeMemory(_blocks[_blocks.Count - 1].Data);
                _blocks.RemoveAt(_blocks.Count - 1);
            }
            _imports.Clear();
            _relocations.Clear();
            _blocks.Clear();
            _stack.Clear();
        }
    }
}
