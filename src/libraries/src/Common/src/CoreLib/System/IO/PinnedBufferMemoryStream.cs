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
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.IO
{
    internal sealed unsafe class PinnedBufferMemoryStream : UnmanagedMemoryStream
    {
        private byte[] _array;
        private GCHandle _pinningHandle;

        internal PinnedBufferMemoryStream(byte[] array)
        {
            Debug.Assert(array != null, "Array can't be null");

            int len = array.Length;
            // Handle 0 length byte arrays specially.
            if (len == 0)
            {
                array = new byte[1];
                len = 0;
            }

            _array = array;
            _pinningHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            // Now the byte[] is pinned for the lifetime of this instance.
            // But I also need to get a pointer to that block of memory...
            fixed (byte* ptr = &_array[0])
                Initialize(ptr, len, len, FileAccess.Read);
        }

        ~PinnedBufferMemoryStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (_pinningHandle.IsAllocated)
            {
                _pinningHandle.Free();
            }

            base.Dispose(disposing);
        }
    }
}
