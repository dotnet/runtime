using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class Handles
    {
        /// <summary>
        /// Using <see cref="Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid"/> in tests.
        /// </summary>
        private const nint InvalidHandle = -1;

        private static nint LastHandle = 0;

        private static readonly HashSet<nint> ActiveHandles = new HashSet<nint>();

        [UnmanagedCallersOnly(EntryPoint = "alloc_handle")]
        public static nint AllocateHandle()
        {
            return AllocateHandleCore();
        }

        [UnmanagedCallersOnly(EntryPoint = "alloc_handle_out")]
        public static void AllocateHandleOut(nint* handle)
        {
            *handle = AllocateHandleCore();
        }

        [UnmanagedCallersOnly(EntryPoint = "release_handle")]
        public static byte ReleaseHandle(nint handle)
        {
            return (byte)(ReleaseHandleCore(handle) ? 1 : 0);
        }

        [UnmanagedCallersOnly(EntryPoint = "is_handle_alive")]
        public static byte IsHandleAlive(nint handle)
        {
            return (byte)(IsHandleAliveCore(handle) ? 1 : 0);
        }

        [UnmanagedCallersOnly(EntryPoint = "modify_handle")]
        public static void ModifyHandle(nint* handle, byte newHandle)
        {
            if (newHandle != 0)
            {
                *handle = AllocateHandleCore();
            }
        }

        private static object m_lock = new object();

        private static nint AllocateHandleCore()
        {
            lock (m_lock)
            {
                if (LastHandle == int.MaxValue)
                {
                    return InvalidHandle;
                }

                nint newHandle = ++LastHandle;
                ActiveHandles.Add(newHandle);
                return newHandle;
            }
        }

        private static bool IsHandleAliveCore(nint handle)
        {
            lock (m_lock)
            {
                return ActiveHandles.Contains(handle);
            }
        }

        private static bool ReleaseHandleCore(nint handle)
        {
            lock (m_lock)
            {
                return ActiveHandles.Remove(handle);
            }
        }
    }
}