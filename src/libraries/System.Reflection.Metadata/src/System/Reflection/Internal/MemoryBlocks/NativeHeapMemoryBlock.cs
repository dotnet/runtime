// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Represents memory block allocated on native heap.
    /// </summary>
    /// <remarks>
    /// Owns the native memory resource.
    /// </remarks>
    internal sealed class NativeHeapMemoryBlock : AbstractMemoryBlock
    {
        private sealed unsafe class DisposableData : CriticalDisposableObject
        {
            private IntPtr _pointer;

            public DisposableData(int size)
            {
#if FEATURE_CER
                // make sure the current thread isn't aborted in between allocating and storing the pointer
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { /* intentionally left blank */ }
                finally
#endif
                {
                    _pointer = Marshal.AllocHGlobal(size);
                }
            }

            protected override void Release()
            {
#if FEATURE_CER
                // make sure the current thread isn't aborted in between zeroing the pointer and freeing the memory
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { /* intentionally left blank */ }
                finally
#endif
                {
                    IntPtr ptr = Interlocked.Exchange(ref _pointer, IntPtr.Zero);
                    if (ptr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }

            public byte* Pointer => (byte*)_pointer;
        }

        private readonly DisposableData _data;
        private readonly int _size;

        internal NativeHeapMemoryBlock(int size)
        {
            _data = new DisposableData(size);
            _size = size;
        }

        public override void Dispose() => _data.Dispose();
        public unsafe override byte* Pointer => _data.Pointer;
        public override int Size => _size;
    }
}
