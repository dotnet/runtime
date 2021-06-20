// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Speech.Internal
{
    /// <summary>
    /// Encapsulate SafeHandle for Win32 Memory Handles
    /// </summary>
    internal sealed class NativeMemorySafeHandle : SafeHandle
    {
        #region Constructors

        public NativeMemorySafeHandle() : base(IntPtr.Zero, true)
        {
        }

        // This destructor will run only if the Dispose method
        // does not get called.
        ~NativeMemorySafeHandle()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            ReleaseHandle();
            base.Dispose(disposing);
        }

        #endregion

        #region internal Methods

        internal unsafe IntPtr Buffer(int size)
        {
            if (size > _bufferSize)
            {
                if (_bufferSize == 0)
                {
#if NET6_0_OR_GREATER
                    SetHandle((nint)NativeMemory.Alloc((uint)size));
#else
                    SetHandle(Marshal.AllocHGlobal(size));
#endif
                }
                else
                {
#if NET6_0_OR_GREATER
                    SetHandle((nint)NativeMemory.Realloc((void*)(nint)handle, (uint)size));
#else
                    SetHandle(Marshal.ReAllocHGlobal(handle, (IntPtr)size));
#endif
                }

                GC.AddMemoryPressure(size - _bufferSize);
                _bufferSize = size;
            }

            return handle;
        }

        /// <summary>
        /// True if the no memory is allocated
        /// </summary>
        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Releases the Win32 Memory handle
        /// </summary>
        protected override unsafe bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                _bufferSize = 0;
#if NET6_0_OR_GREATER
                NativeMemory.Free((void*)(nint)handle);
#else
                Marshal.FreeHGlobal(handle);
#endif
                handle = IntPtr.Zero;

                return true;
            }

            return false;
        }

#endregion

#region Private Fields

        private int _bufferSize;

#endregion
    }
}
