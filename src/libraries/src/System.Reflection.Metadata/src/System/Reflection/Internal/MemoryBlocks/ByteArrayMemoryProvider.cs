// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal
{
    internal sealed class ByteArrayMemoryProvider : MemoryBlockProvider
    {
        private readonly ImmutableArray<byte> _array;
        private StrongBox<GCHandle> _pinned;

        public ByteArrayMemoryProvider(ImmutableArray<byte> array)
        {
            Debug.Assert(!array.IsDefault);
            _array = array;
        }

        ~ByteArrayMemoryProvider()
        {
            Dispose(disposing: false);
        }

        protected override void Dispose(bool disposing)
        {
            _pinned?.Value.Free();
            _pinned = null;
        }

        public override int Size => _array.Length;
        public ImmutableArray<byte> Array => _array;

        protected override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
        {
            return new ByteArrayMemoryBlock(this, start, size);
        }

        public override Stream GetStream(out StreamConstraints constraints)
        {
            constraints = new StreamConstraints(null, 0, Size);
            return new ImmutableMemoryStream(_array);
        }

        internal unsafe byte* Pointer
        {
            get
            {
                if (_pinned == null)
                {
                    var newPinned = new StrongBox<GCHandle>(
                        GCHandle.Alloc(ImmutableByteArrayInterop.DangerousGetUnderlyingArray(_array), GCHandleType.Pinned));

                    if (Interlocked.CompareExchange(ref _pinned, newPinned, null) != null)
                    {
                        // another thread has already allocated the handle:
                        newPinned.Value.Free();
                    }
                }

                return (byte*)_pinned.Value.AddrOfPinnedObject();
            }
        }
    }
}
