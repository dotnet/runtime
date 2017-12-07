// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal sealed class NativeOwnedMemory : OwnedMemory<byte>
    {
        private readonly int _length;
        private IntPtr _ptr;
        private int _retainedCount;
        private bool _disposed;

        public NativeOwnedMemory(int length)
        {
            _length = length;
            _ptr = Marshal.AllocHGlobal(length);
        }

        ~NativeOwnedMemory()
        {
            Debug.WriteLine($"{nameof(NativeOwnedMemory)} being finalized");
            Dispose(false);
        }

        public override bool IsDisposed
        {
            get
            {
                lock (this)
                {
                    return _disposed && _retainedCount == 0;
                }
            }
        }

        public override int Length => _length;

        protected override bool IsRetained
        {
            get
            {
                lock (this)
                {
                    return _retainedCount > 0;
                }
            }
        }

        public override unsafe Span<byte> Span => new Span<byte>((void*)_ptr, _length);

        public override unsafe MemoryHandle Pin() => new MemoryHandle(this, (void*)_ptr);

        public override bool Release()
        {
            lock (this)
            {
                if (_retainedCount > 0)
                {
                    _retainedCount--;
                    if (_retainedCount == 0)
                    {
                        if (_disposed)
                        {
                            Marshal.FreeHGlobal(_ptr);
                            _ptr = IntPtr.Zero;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public override void Retain()
        {
            lock (this)
            {
                if (_retainedCount == 0 && _disposed)
                {
                    throw new Exception();
                }
                _retainedCount++;
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                _disposed = true;
                if (_retainedCount == 0)
                {
                    Marshal.FreeHGlobal(_ptr);
                    _ptr = IntPtr.Zero;
                }
            }
        }

        protected override bool TryGetArray(out ArraySegment<byte> arraySegment)
        {
            arraySegment = default(ArraySegment<byte>);
            return false;
        }
    }
}
