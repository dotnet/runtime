
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Marshalling helper methods that will likely live in S.R.IS.Marshal
    /// when we integrate our APIs with dotnet/runtime.
    /// </summary>
    public static class MarshalEx
    {
        /// <summary>
        /// Sets the handle of <paramref name="safeHandle"/> to the specified <paramref name="handle"/>.
        /// </summary>
        /// <param name="safeHandle"><see cref="SafeHandle"/> instance to update</param>
        /// <param name="handle">Pre-existing handle</param>
        public static void InitHandle(SafeHandle safeHandle, IntPtr handle)
        {            
            typeof(SafeHandle).GetMethod("SetHandle", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(safeHandle, new object[] { handle });
        }

        /// <summary>
        /// Set the last platform invoke error on the thread 
        /// </summary>
        public static void SetLastPInvokeError(int error)
        {
            MethodInfo? method = typeof(Marshal).GetMethod("SetLastWin32Error", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                method = typeof(Marshal).GetMethod("SetLastPInvokeError", BindingFlags.Public | BindingFlags.Static);
            }

            method!.Invoke(null, new object[] { error });
        }

        /// <summary>
        /// Get the last system error on the current thread (errno on Unix, GetLastError on Windows)
        /// </summary>
        public static unsafe int GetLastSystemError()
        {
            // Would be internal call that handles getting the last error for the thread using the PAL

            if (OperatingSystem.IsWindows())
            {
                return Kernel32.GetLastError();
            }
            else if (OperatingSystem.IsMacOS())
            {
                return *libc.__error();
            }
            else if (OperatingSystem.IsLinux())
            {
                return *libc.__errno_location();
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Set the last system error on the current thread (errno on Unix, SetLastError on Windows)
        /// </summary>
        public static unsafe void SetLastSystemError(int error)
        {
            // Would be internal call that handles setting the last error for the thread using the PAL

            if (OperatingSystem.IsWindows())
            {
                Kernel32.SetLastError(error);
            }
            else if (OperatingSystem.IsMacOS())
            {
                *libc.__error() = error;
            }
            else if (OperatingSystem.IsLinux())
            {
                *libc.__errno_location() = error;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private class Kernel32
        {
            [DllImport(nameof(Kernel32))]
            public static extern void SetLastError(int error);

            [DllImport(nameof(Kernel32))]
            public static extern int GetLastError();
        }

        private class libc
        {
            [DllImport(nameof(libc))]
            internal static unsafe extern int* __errno_location();

            [DllImport(nameof(libc))]
            internal static unsafe extern int* __error();
        }
    }
}
