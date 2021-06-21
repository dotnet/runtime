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
                    SetHandle(NativeMemoryHelper.Alloc(size));
                }
                else
                {
                    SetHandle(NativeMemoryHelper.Realloc(handle, size));
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
                // Reset the extra information given to the GC
                if (_bufferSize > 0)
                {
                    GC.RemoveMemoryPressure(_bufferSize);
                    _bufferSize = 0;
                }
                
                NativeMemoryHelper.Free(handle);
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
