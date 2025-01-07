// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    public struct DependentHandle : IDisposable
    {
        private IntPtr _handle;

        public DependentHandle(object? target, object? dependent) =>
            _handle = RuntimeImports.RhHandleAllocDependent(target, dependent);

        public bool IsAllocated => _handle != IntPtr.Zero;

        public object? Target
        {
            get
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                return RuntimeImports.RhHandleGet(handle);
            }
            set
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0 || value is not null)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                RuntimeImports.RhHandleSet(handle, null);
            }
        }


        public object? Dependent
        {
            get
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                RuntimeImports.RhHandleGetDependent(handle, out object? dependent);
                return dependent;
            }
            set
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                RuntimeImports.RhHandleSetDependentSecondary(handle, value);
            }
        }

        public (object? Target, object? Dependent) TargetAndDependent
        {
            get
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                object? target = RuntimeImports.RhHandleGetDependent(handle, out object? dependent);

                return (target, dependent);
            }
        }

        internal object? UnsafeGetTarget()
        {
            return RuntimeImports.RhHandleGet(_handle);
        }

        internal object? UnsafeGetTargetAndDependent(out object? dependent)
        {
            return RuntimeImports.RhHandleGetDependent(_handle, out dependent);
        }

        internal void UnsafeSetTargetToNull()
        {
            RuntimeImports.RhHandleSet(_handle, null);
        }

        internal void UnsafeSetDependent(object? dependent)
        {
            RuntimeImports.RhHandleSetDependentSecondary(_handle, dependent);
        }

        public void Dispose()
        {
            // Forces the DependentHandle back to non-allocated state
            // (if not already there) and frees the handle if needed.
            IntPtr handle = _handle;

            if ((nint)handle != 0)
            {
                _handle = IntPtr.Zero;

                RuntimeImports.RhHandleFree(handle);
            }
        }
    }
}
