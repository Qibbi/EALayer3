namespace BinaryAssetBuilder.AudioEL3Compiler
{
    public static class Endian
    {
        public static unsafe void ByteSwapInt16(ushort* pValue)
        {
            ushort value = (ushort)((*pValue & 0x000000FFu) << 8 | *pValue >> 8 & 0x000000FFu);
            *pValue = value;
        }

        public static unsafe void ByteSwapInt32(uint* pValue)
        {
            uint value = (*pValue & 0x000000FFu) << 24 | (*pValue & 0x0000FF00u) << 8 | (*pValue & 0x00FF0000u) >> 8 | *pValue >> 24 & 0x000000FFu;
            *pValue = value;
        }

        public static unsafe void ByteSwapInt64(ulong* pValue)
        {
            ulong value = (*pValue & 0x00000000000000FFuL) << 56 | (*pValue & 0x000000000000FF00uL) << 40 | (*pValue & 0x0000000000FF0000uL) << 24 | (*pValue & 0x00000000FF000000uL) << 8
                        | (*pValue & 0x000000FF00000000uL) >> 8 | (*pValue & 0x0000FF0000000000uL) >> 24 | (*pValue & 0x00FF000000000000uL) >> 40 | *pValue >> 56 & 0xFF00000000000000uL;
            *pValue = value;
        }
    }
}
