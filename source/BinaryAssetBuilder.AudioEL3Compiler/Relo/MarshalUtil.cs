using System;
using System.Runtime.InteropServices;
using System.Security;

namespace BinaryAssetBuilder.AudioEL3Compiler.Relo
{
    public static class MarshalUtil
    {
        private const string _memDll = "msvcrt.dll";

        public static int SizeOf(Type type)
        {
            return Marshal.SizeOf(type);
        }

        public static int SizeOf<T>() where T : struct
        {
            return Marshal.SizeOf(typeof(T));
        }

        public static int SizeOf<T>(T[] array) where T : struct
        {
            return array is null ? 0 : array.Length * SizeOf<T>();
        }

        [DllImport(_memDll, EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr CopyMemory(IntPtr dest, IntPtr src, ulong count);

        public static void CopyMemory(IntPtr dest, IntPtr src, int count)
        {
            CopyMemory(dest, src, (ulong)count);
        }

        public static void CopyMemory(IntPtr dest, IntPtr src, uint count)
        {
            CopyMemory(dest, src, (ulong)count);
        }

        [DllImport(_memDll, EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr ClearMemory(IntPtr ptr, byte value, ulong count);

        public static void ClearMemory(IntPtr ptr, byte value, int count)
        {
            ClearMemory(ptr, value, (ulong)count);
        }

        public static void ClearMemory(IntPtr ptr, byte value, uint count)
        {
            ClearMemory(ptr, value, (ulong)count);
        }

        public static unsafe IntPtr AllocateMemory(int sizeInBytes, int align = 16)
        {
            ulong mask = (ulong)(align - 1);
            if ((align & (int)mask) != 0) throw new ArgumentException("Not power of two.", nameof(align));
            IntPtr ptr = Marshal.AllocHGlobal(sizeInBytes + (int)mask + sizeof(void*));
            byte* result = (byte*)(((ulong)ptr + (ulong)sizeof(void*) + mask) & ~mask);
            ((IntPtr*)result)[-1] = ptr;
            return new IntPtr(result);
        }

        public static unsafe IntPtr AllocateMemory(uint sizeInBytes, int align = 16)
        {
            ulong mask = (ulong)(align - 1);
            if ((align & (int)mask) != 0) throw new ArgumentException("Not power of two.", nameof(align));
            IntPtr ptr = Marshal.AllocHGlobal((IntPtr)(sizeInBytes + (int)mask + sizeof(void*)));
            byte* result = (byte*)(((ulong)ptr + (ulong)sizeof(void*) + mask) & ~mask);
            ((IntPtr*)result)[-1] = ptr;
            return new IntPtr(result);
        }

        public static IntPtr AllocateClearedMemory(int sizeInBytes, byte value = 0, int align = 16)
        {
            IntPtr result = AllocateMemory(sizeInBytes, align);
            ClearMemory(result, value, sizeInBytes);
            return result;
        }

        public static IntPtr AllocateClearedMemory(uint sizeInBytes, byte value = 0, int align = 16)
        {
            IntPtr result = AllocateMemory(sizeInBytes, align);
            ClearMemory(result, value, sizeInBytes);
            return result;
        }

        public static unsafe void FreeMemory(IntPtr ptr)
        {
            Marshal.FreeHGlobal(((IntPtr*)ptr)[-1]);
        }
    }
}
