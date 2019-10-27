using System;
using System.Collections.Generic;
using System.IO;

namespace BinaryAssetBuilder.AudioEL3Compiler
{
    internal class BitStream
    {
        private byte[] _buffer;
        private uint _bufferBitLength;
        private int _position;
        private byte _partial;
        private int _partialLeft;

        internal bool IsEndOfStream => _bufferBitLength == 0;
        public int Length => _buffer.Length;
        internal int Position => _position;

        internal BitStream(byte[] buffer)
        {
            _buffer = buffer;
            _bufferBitLength = (uint)_buffer.Length * 8u;
        }

        internal BitStream(byte[] buffer, int startIndex)
        {
            if (startIndex < 0 || startIndex >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(startIndex));
            _buffer = buffer;
            _position = startIndex;
            _bufferBitLength = (uint)(_buffer.Length - _position) * 8u;
        }

        internal BitStream(byte[] buffer, uint bufferBitLenth) : this(buffer)
        {
            if (bufferBitLenth > (buffer.Length * 8)) throw new ArgumentOutOfRangeException(nameof(bufferBitLenth));
            _bufferBitLength = bufferBitLenth;
        }

        internal bool ReadBit()
        {
            byte b = ReadByte(1);
            return (b & 1) == 1;
        }

        internal byte ReadByte(int count)
        {
            if (IsEndOfStream) throw new EndOfStreamException();
            if (count <= 0 || count > 8) throw new ArgumentOutOfRangeException(nameof(count));
            if (count > _bufferBitLength) throw new ArgumentOutOfRangeException(nameof(count));
            _bufferBitLength -= (uint)count;
            byte result = 0;
            if (_partialLeft >= count)
            {
                int rightShift = 8 - count;
                result = (byte)(_partial >> rightShift);
                _partial <<= count;
                _partialLeft -= count;
            }
            else
            {
                byte next = _buffer[_position++];
                int rightShift = 8 - count;
                result = (byte)(_partial >> rightShift);
                rightShift = Math.Abs(count - _partialLeft - 8);
                result |= (byte)(next >> rightShift);
                _partial = (byte)(next << (count - _partialLeft));
                _partialLeft = 8 - count - _partialLeft;
            }
            return result;
        }

        internal ushort ReadUInt16(int count)
        {
            if (count <= 0 || count > 16) throw new ArgumentOutOfRangeException(nameof(count));
            ushort result = 0;
            while (count > 0)
            {
                int toRead = 8;
                if (count < 8)
                {
                    toRead = count;
                }
                result <<= toRead;
                byte b = ReadByte(toRead);
                result |= b;
                count -= toRead;
            }
            return result;
        }

        internal uint ReadUInt32(int count)
        {
            if (count <= 0 || count > 32) throw new ArgumentOutOfRangeException(nameof(count));
            uint result = 0;
            while (count > 0)
            {
                int toRead = 8;
                if (count < 8)
                {
                    toRead = count;
                }
                result <<= toRead;
                byte b = ReadByte(toRead);
                result |= b;
                count -= toRead;
            }
            return result;
        }

        internal ulong ReadUInt64(int count)
        {
            if (count <= 0 || count > 64) throw new ArgumentOutOfRangeException(nameof(count));
            ulong result = 0;
            while (count > 0)
            {
                int toRead = 8;
                if (count < 8)
                {
                    toRead = count;
                }
                result <<= toRead;
                byte b = ReadByte(toRead);
                result |= b;
                count -= toRead;
            }
            return result;
        }

        internal void Pad()
        {
            _partialLeft = 8;
            ++_position;
        }
    }

    internal class BitStreamWriter
    {
        private List<byte> _buffer;
        private int _position;
        private int _partialLeft;

        internal void Write(byte bits, int count)
        {
            if (count <= 0 || count > 8) throw new ArgumentOutOfRangeException(nameof(count));
            byte buffer;
            if (_partialLeft > 0)
            {
                buffer = _buffer[_position];
                if (count > _partialLeft)
                {
                    buffer |= (byte)((bits & (0xFF >> (8 - count))) >> (count - _partialLeft));
                }
                else
                {
                    buffer |= (byte)((bits & (0xFF >> (8 - count))) << (_partialLeft - count));
                }
                _buffer[_position] = buffer;
            }
            if (count > _partialLeft)
            {
                _partialLeft = 8 - count - _partialLeft;
                buffer = (byte)(bits << _partialLeft);
                _buffer.Add(buffer);
                ++_position;
            }
            else
            {
                _partialLeft -= count;
            }
        }

        internal void Write(uint bits, int count)
        {
            if (count <= 0 || count > 32) throw new ArgumentOutOfRangeException(nameof(count));
            int fullBytes = count / 8;
            int toWrite = count % 8;
            for (; fullBytes >= 0; --fullBytes)
            {
                byte data = (byte)(bits >> (fullBytes * 8));
                if (toWrite > 0)
                {
                    Write(data, toWrite);
                }
                if (fullBytes > 0)
                {
                    toWrite = 8;
                }
            }
        }

        internal byte[] ToArray()
        {
            return _buffer.ToArray();
        }
    }
}
