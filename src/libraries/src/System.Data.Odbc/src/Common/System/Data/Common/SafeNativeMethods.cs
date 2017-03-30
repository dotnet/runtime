using System.Runtime.InteropServices;

namespace System.Data.Common
{
    internal class SafeNativeMethods
    {
        internal static IntPtr LocalAlloc(IntPtr initialSize)
        {
            var handle = Marshal.AllocHGlobal(initialSize);
            ZeroMemory(handle, (int)initialSize);
            return handle;
        }

        internal static void LocalFree(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        internal static void ZeroMemory(IntPtr ptr, int length)
        {
            var zeroes = new byte[length];
            Marshal.Copy(zeroes, 0, ptr, length);
        }
    }
}
