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

#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.IO
{
    internal sealed unsafe class PinnedBufferMemoryStream : UnmanagedMemoryStream
    {
        private byte[] _array;
        private GCHandle _pinningHandle;

        internal PinnedBufferMemoryStream(byte[] array)
        {
            Debug.Assert(array != null, "Array can't be null");

            _array = array;
            _pinningHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            // Now the byte[] is pinned for the lifetime of this instance.
            // But I also need to get a pointer to that block of memory...
            int len = array.Length;
            fixed (byte* ptr = &MemoryMarshal.GetReference((Span<byte>)array))
                Initialize(ptr, len, len, FileAccess.Read);
        }

#if !netstandard
        public override int Read(Span<byte> buffer) => ReadCore(buffer);

        public override void Write(ReadOnlySpan<byte> buffer) => WriteCore(buffer);
#endif

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
