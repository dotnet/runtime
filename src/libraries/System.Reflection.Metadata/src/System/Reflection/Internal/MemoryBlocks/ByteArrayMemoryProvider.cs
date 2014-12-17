// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Runtime.CompilerServices;

namespace System.Reflection.Internal
{
    internal sealed class ByteArrayMemoryProvider : MemoryBlockProvider
    {
        internal readonly ImmutableArray<byte> array;
        private StrongBox<GCHandle> _pinned;

        public ByteArrayMemoryProvider(ImmutableArray<byte> array)
        {
            this.array = array;
        }

        ~ByteArrayMemoryProvider()
        {
            Dispose(disposing: false);
        }

        protected override void Dispose(bool disposing)
        {
            if (_pinned != null)
            {
                _pinned.Value.Free();
                _pinned = null;
            }
        }

        public override int Size
        {
            get
            {
                return array.Length;
            }
        }

        protected override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
        {
            return new ByteArrayMemoryBlock(this, start, size);
        }

        public override Stream GetStream(out StreamConstraints constraints)
        {
            constraints = new StreamConstraints(null, 0, Size);
            return new ImmutableMemoryStream(array);
        }

        internal unsafe byte* Pointer
        {
            get
            {
                if (_pinned == null)
                {
                    var newPinned = new StrongBox<GCHandle>(
                        GCHandle.Alloc(ImmutableByteArrayInterop.DangerousGetUnderlyingArray(array), GCHandleType.Pinned));

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
