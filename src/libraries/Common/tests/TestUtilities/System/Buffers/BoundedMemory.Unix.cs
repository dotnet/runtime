// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers
{
    public static unsafe partial class BoundedMemory
    {
        private static UnixImplementation<T> AllocateWithoutDataPopulationUnix<T>(int elementCount, PoisonPagePlacement placement) where T : unmanaged
        {
            // On non-Windows platforms, we don't yet have support for changing the permissions of individual pages.
            // We'll instead use AllocHGlobal / FreeHGlobal to carve out a r+w section of unmanaged memory.

            return new UnixImplementation<T>(elementCount);
        }

        private sealed class UnixImplementation<T> : BoundedMemory<T> where T : unmanaged
        {
            private readonly AllocHGlobalHandle _handle;
            private readonly int _elementCount;
            private readonly BoundedMemoryManager _memoryManager;

            public UnixImplementation(int elementCount)
            {
                _handle = AllocHGlobalHandle.Allocate(checked(elementCount * (nint)sizeof(T)));
                _elementCount = elementCount;
                _memoryManager = new BoundedMemoryManager(this);
            }

            public override bool IsReadonly => false;

            public override int Length => _elementCount;

            public override Memory<T> Memory => _memoryManager.Memory;

            public override Span<T> Span
            {
                get
                {
                    bool refAdded = false;
                    try
                    {
                        _handle.DangerousAddRef(ref refAdded);
                        return new Span<T>((void*)_handle.DangerousGetHandle(), _elementCount);
                    }
                    finally
                    {
                        if (refAdded)
                        {
                            _handle.DangerousRelease();
                        }
                    }
                }
            }

            public override void Dispose()
            {
                _handle.Dispose();
            }

            public override void MakeReadonly()
            {
                // no-op
            }

            public override void MakeWriteable()
            {
                // no-op
            }

            private sealed class BoundedMemoryManager : MemoryManager<T>
            {
                private readonly UnixImplementation<T> _impl;

                public BoundedMemoryManager(UnixImplementation<T> impl)
                {
                    _impl = impl;
                }

                public override Memory<T> Memory => CreateMemory(_impl._elementCount);

                protected override void Dispose(bool disposing)
                {
                    // no-op; the handle will be disposed separately
                }

                public override Span<T> GetSpan() => _impl.Span;

                public override MemoryHandle Pin(int elementIndex)
                {
                    if ((uint)elementIndex > (uint)_impl._elementCount)
                    {
                        throw new ArgumentOutOfRangeException(paramName: nameof(elementIndex));
                    }

                    bool refAdded = false;
                    try
                    {
                        _impl._handle.DangerousAddRef(ref refAdded);
                        return new MemoryHandle((T*)_impl._handle.DangerousGetHandle() + elementIndex);
                    }
                    finally
                    {
                        if (refAdded)
                        {
                            _impl._handle.DangerousRelease();
                        }
                    }
                }

                public override void Unpin()
                {
                    // no-op - we don't unpin native memory
                }
            }
        }

        private sealed class AllocHGlobalHandle : SafeHandle
        {
            // Called by P/Invoke when returning SafeHandles
            private AllocHGlobalHandle()
                : base(IntPtr.Zero, ownsHandle: true)
            {
            }

            internal static AllocHGlobalHandle Allocate(nint byteLength)
            {
                AllocHGlobalHandle retVal = new AllocHGlobalHandle();
                retVal.SetHandle(Marshal.AllocHGlobal(byteLength)); // this is for unit testing; don't bother setting up a CER on Full Framework
                return retVal;
            }

            // Do not provide a finalizer - SafeHandle's critical finalizer will
            // call ReleaseHandle for you.

            public override bool IsInvalid => (handle == IntPtr.Zero);

            protected override bool ReleaseHandle()
            {
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }
    }
}
