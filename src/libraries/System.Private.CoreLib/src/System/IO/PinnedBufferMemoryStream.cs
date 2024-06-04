// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO
{
    /// <summary>
    /// Pins a <see langword="byte[]"/>, exposing it as an unmanaged memory stream.  Used in <see cref="System.Resources.ResourceReader"/> for corner cases.
    /// </summary>
    internal sealed unsafe class PinnedBufferMemoryStream : UnmanagedMemoryStream
    {
        private readonly byte[] _array;
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

#if !RESOURCES_EXTENSIONS
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
