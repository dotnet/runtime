
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
        /// Create an instance of the given <typeparamref name="TSafeHandle"/>.
        /// </summary>
        /// <typeparam name="TSafeHandle">Type of the SafeHandle</typeparam>
        /// <returns>New instance of <typeparamref name="TSafeHandle"/></returns>
        /// <remarks>
        /// The <typeparamref name="TSafeHandle"/> must be non-abstract and have a parameterless constructor.
        /// </remarks>
        public static TSafeHandle CreateSafeHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors)]TSafeHandle>()
            where TSafeHandle : SafeHandle
        {
            if (typeof(TSafeHandle).IsAbstract || typeof(TSafeHandle).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, Type.EmptyTypes, null) == null)
            {
                throw new MissingMemberException($"The safe handle type '{typeof(TSafeHandle).FullName}' must be a non-abstract type with a parameterless constructor.");
            }

            TSafeHandle safeHandle = (TSafeHandle)Activator.CreateInstance(typeof(TSafeHandle), nonPublic: true)!;
            return safeHandle;
        }

        /// <summary>
        /// Sets the handle of <paramref name="safeHandle"/> to the specified <paramref name="handle"/>.
        /// </summary>
        /// <param name="safeHandle"><see cref="SafeHandle"/> instance to update</param>
        /// <param name="handle">Pre-existing handle</param>
        public static void SetHandle(SafeHandle safeHandle, IntPtr handle)
        {            
            typeof(SafeHandle).GetMethod("SetHandle", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(safeHandle, new object[] { handle });
        }

        /// <summary>
        /// Set the last platform invoke error on the thread 
        /// </summary>
        public static void SetLastWin32Error(int error)
        {
            typeof(Marshal).GetMethod("SetLastWin32Error", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { error });
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
