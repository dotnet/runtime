// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Internal;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata.Tests
{
    internal struct PinnedBlob : IDisposable
    {
        private GCHandle _bytes; // non-readonly as Free() mutates to prevent double-free.
        private readonly byte[] _blob;

        public PinnedBlob(ImmutableArray<byte> blob)
            : this(ImmutableByteArrayInterop.DangerousGetUnderlyingArray(blob))
        {
        }

        public unsafe PinnedBlob(byte[] blob)
        {
            _bytes = GCHandle.Alloc(blob, GCHandleType.Pinned);
            _blob = blob;
        }

        public unsafe BlobReader CreateReader()
        {
            return CreateReader(0, _blob.Length);
        }

        public unsafe BlobReader CreateReader(int start, int byteCount)
        {
            BlobUtilities.ValidateRange(_blob.Length, start, byteCount, nameof(byteCount));
            return new BlobReader((byte*)_bytes.AddrOfPinnedObject() + start, byteCount);
        }

        public void Dispose()
        {
            _bytes.Free();
        }
    }
}
