using System;

namespace BinaryAssetBuilder.AudioEL3Compiler.Relo
{
    using InteropMarshal = System.Runtime.InteropServices.Marshal;

    public static class Marshaler
    {
        public static unsafe void Marshal(string text, String* str, Tracker tracker)
        {
            IntPtr hglobal = InteropMarshal.StringToHGlobalAnsi(text);
            sbyte* textPtr = (sbyte*)hglobal.ToPointer();
            sbyte* lenPtr = textPtr;
            while (*lenPtr != 0)
            {
                ++lenPtr;
            }
            int length = (int)(lenPtr - textPtr);
            str->Length = length;
            if (tracker.IsBigEndian)
            {
                Tracker.ByteSwap32((uint*)&str->Length);
            }
            tracker.Push((void**)&str->Data, 1, length + 1);
            sbyte* strValue = (sbyte*)str->Data;
            int c;
            do
            {
                *strValue = *textPtr++;
                c = *strValue++;
            }
            while (c != 0);
            tracker.Pop();
            InteropMarshal.FreeHGlobal(hglobal);
            hglobal = IntPtr.Zero;
        }
    }
}

/*using System;

namespace BinaryAssetBuilder.AudioEL3Compiler.Relo
{
    using InteropMarshal = System.Runtime.InteropServices.Marshal;

    internal static class Marshaler
    {
        internal static class Utils
        {
            internal static unsafe uint Tokenize(sbyte** buffer, sbyte* text)
            {
                sbyte* lengthIdx = text;
                while (*lengthIdx != 0)
                {
                    ++lengthIdx;
                }
                sbyte* copy = (sbyte*)MarshalUtil.AllocateMemory((int)(lengthIdx - text - 1));
                if ((IntPtr)copy != (IntPtr)(*(int*)buffer))
                {
                    MarshalUtil.FreeMemory(*(IntPtr*)buffer);
                }
                *(IntPtr*)buffer = (IntPtr)copy;
                uint idx = 0;
                while (*text != 0)
                {
                    while (!IsSpace(*text) && *text != 0)
                    {
                        ++text;
                    }
                    if (*text != 0)
                    {
                        for (; IsSpace(*text) && *text != 0; ++text)
                        {
                            *copy++ = *text;
                        }
                        *copy++ = 0;
                        ++idx;
                    }
                    else
                    {
                        break;
                    }
                }
                return idx;
            }

            internal static sbyte CharToLower(sbyte chr)
            {
                throw new NotImplementedException();
            }
        }

        internal static unsafe void Marshal(sbyte* text, AssetId* @string, Tracker tracker)
        {
            Marshal(text, (String)@string, tracker);
        }

        internal static unsafe void Marshal(sbyte* text, DataBlob* @object, Tracker tracker)
        {
            throw new NotImplementedException(); // 10919
        }

        internal static unsafe void Marshal(INode* node, BaseAssetType* @object, Tracker tracker)
        {
            throw new NotImplementedException(); // 18605
        }

        internal static unsafe void Marshal(INode* node, BaseInheritableAsset* @object, Tracker tracker)
        {
            throw new NotImplementedException(); // 18615
        }

        internal static unsafe void Marshal(sbyte* text, ushort* @object, Tracker tracker)
        {
            throw new NotImplementedException(); // 18642
        }

        internal static unsafe void Marshal(sbyte* text, short* @object, Tracker tracker)
        {
            throw new NotImplementedException(); // 18658
        }

        internal static unsafe void Marshal(sbyte* text, uint* @object, Tracker tracker)
        {
            throw new NotImplementedException(); // 18685
        }

        internal static unsafe void Marshal(sbyte* text, int* @object, Tracker tracker)
        {
            throw new NotImplementedException(); // 18674
        }
    }
}
*/
