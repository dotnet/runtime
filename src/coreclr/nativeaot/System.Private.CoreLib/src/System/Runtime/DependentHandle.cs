// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    public struct DependentHandle : IDisposable
    {
        private const nint IsDeferFinalizeBit = 1;

        private nint _taggedHandle;

        public DependentHandle(object? target, object? dependent) =>
            _taggedHandle = RuntimeImports.RhHandleAllocDependent(false, target, dependent);

        internal DependentHandle(bool isDeferFinalize, object? target, object? dependent) =>
            _taggedHandle = RuntimeImports.RhHandleAllocDependent(isDeferFinalize, target, dependent) | (isDeferFinalize ? IsDeferFinalizeBit : 0);

        public bool IsAllocated => _taggedHandle != 0;

        public object? Target
        {
            get
            {
                nint handle = _taggedHandle & ~IsDeferFinalizeBit;

                if (handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                return RuntimeImports.RhHandleGet(handle);
            }
            set
            {
                nint handle = _taggedHandle & ~IsDeferFinalizeBit;

                if (handle == 0 || value is not null)
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
                nint handle = _taggedHandle & ~IsDeferFinalizeBit;

                if (handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                RuntimeImports.RhHandleGetDependent(handle, out object? dependent);
                return dependent;
            }
            set
            {
                nint taggedHandle = _taggedHandle;

                if (taggedHandle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                nint handle = taggedHandle & ~IsDeferFinalizeBit;
                bool isDeferFinalize = (taggedHandle & IsDeferFinalizeBit) != 0;
                RuntimeImports.RhHandleSetDependentSecondary(isDeferFinalize, handle, value);
            }
        }

        public (object? Target, object? Dependent) TargetAndDependent
        {
            get
            {
                nint handle = _taggedHandle & ~IsDeferFinalizeBit;

                if (handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                object? target = RuntimeImports.RhHandleGetDependent(handle, out object? dependent);

                return (target, dependent);
            }
        }

        internal object? UnsafeGetTarget()
        {
            return RuntimeImports.RhHandleGet(_taggedHandle & ~IsDeferFinalizeBit);
        }

        internal object? UnsafeGetTargetAndDependent(out object? dependent)
        {
            return RuntimeImports.RhHandleGetDependent(_taggedHandle & ~IsDeferFinalizeBit, out dependent);
        }

        internal void UnsafeSetTargetToNull()
        {
            RuntimeImports.RhHandleSet(_taggedHandle & ~IsDeferFinalizeBit, null);
        }

        internal void UnsafeSetDependent(object? dependent)
        {
            nint handle = _taggedHandle & ~IsDeferFinalizeBit;
            bool isDeferFinalize = (_taggedHandle & IsDeferFinalizeBit) != 0;
            RuntimeImports.RhHandleSetDependentSecondary(isDeferFinalize, handle, dependent);
        }

        public void Dispose()
        {
            // Forces the DependentHandle back to non-allocated state
            // (if not already there) and frees the handle if needed.
            IntPtr handle = _taggedHandle & ~IsDeferFinalizeBit;

            if (handle != 0)
            {
                _taggedHandle = 0;
                RuntimeImports.RhHandleFree(handle);
            }
        }
    }
}
