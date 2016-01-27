// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Pins a byte[], exposing it as an unmanaged memory 
**          stream.  Used in ResourceReader for corner cases.
**
**
===========================================================*/
using System;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace System.IO {
    internal sealed unsafe class PinnedBufferMemoryStream : UnmanagedMemoryStream
    {
        private byte[] _array;
        private GCHandle _pinningHandle;

        // The new inheritance model requires a Critical default ctor since base (UnmanagedMemoryStream) has one
        [System.Security.SecurityCritical]
        private PinnedBufferMemoryStream():base(){}

        [System.Security.SecurityCritical]  // auto-generated
        internal PinnedBufferMemoryStream(byte[] array)
        {
            Contract.Assert(array != null, "Array can't be null");

            int len = array.Length;
            // Handle 0 length byte arrays specially.
            if (len == 0) {
                array = new byte[1];
                len = 0;
            }

            _array = array;
            _pinningHandle = new GCHandle(array, GCHandleType.Pinned);
            // Now the byte[] is pinned for the lifetime of this instance.
            // But I also need to get a pointer to that block of memory...
            fixed(byte* ptr = _array)
                Initialize(ptr, len, len, FileAccess.Read, true);
        }

        ~PinnedBufferMemoryStream()
        {
            Dispose(false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override void Dispose(bool disposing)
        {
            if (_isOpen) {
                _pinningHandle.Free();
                _isOpen = false;
            }
#if _DEBUG
            // To help track down lifetime issues on checked builds, force 
            //a full GC here.
            if (disposing) {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
#endif
            base.Dispose(disposing);
        }
    }
}
