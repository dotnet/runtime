// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal
{
    internal sealed unsafe class MemoryMappedFileBlock : AbstractMemoryBlock
    {
        private sealed class DisposableData : CriticalDisposableObject
        {
            // Usually a MemoryMappedViewAccessor, but kept
            // as an IDisposable for better testability.
            private IDisposable? _accessor;
            private SafeBuffer? _safeBuffer;
            private byte* _pointer;

            public DisposableData(IDisposable accessor, SafeBuffer safeBuffer, long offset)
            {
#if FEATURE_CER
                // Make sure the current thread isn't aborted in between acquiring the pointer and assigning the fields.
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { /* intentionally left blank */ }
                finally
#endif
                {
                    byte* basePointer = null;
                    safeBuffer.AcquirePointer(ref basePointer);

                    _accessor = accessor;
                    _safeBuffer = safeBuffer;
                    _pointer = basePointer + offset;
                }
            }

            protected override void Release()
            {
#if FEATURE_CER
                // Make sure the current thread isn't aborted in between zeroing the references and releasing/disposing.
                // Safe buffer only frees the underlying resource if its ref count drops to zero, so we have to make sure it does.
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { /* intentionally left blank */ }
                finally
#endif
                {
                    Interlocked.Exchange(ref _safeBuffer, null)?.ReleasePointer();
                    Interlocked.Exchange(ref _accessor, null)?.Dispose();
                }

                _pointer = null;
            }

            public byte* Pointer => _pointer;
        }

        private readonly DisposableData _data;
        private readonly int _size;

        internal unsafe MemoryMappedFileBlock(IDisposable accessor, SafeBuffer safeBuffer, long offset, int size)
        {
            _data = new DisposableData(accessor, safeBuffer, offset);
            _size = size;
        }

        public override void Dispose() => _data.Dispose();
        public override byte* Pointer => _data.Pointer;
        public override int Size => _size;
    }
}
