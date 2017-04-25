// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private unsafe sealed class DisposableData : CriticalDisposableObject
        {
            private IntPtr _pointer;

            public DisposableData(int size)
            {
                // make sure the current thread isn't aborted in between allocating and storing the pointer
#if !NETSTANDARD11
                RuntimeHelpers.PrepareConstrainedRegions();
#endif
                try
                {
                }
                finally
                {
                    _pointer = Marshal.AllocHGlobal(size);
                }
            }
                        
            protected override void Release()
            {
                // make sure the current thread isn't aborted in between zeroing the pointer and freeing the memory
#if !NETSTANDARD11
                RuntimeHelpers.PrepareConstrainedRegions();
#endif
                try
                {
                }
                finally
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
