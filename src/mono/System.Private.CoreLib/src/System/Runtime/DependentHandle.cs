// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    internal struct Ephemeron
    {
        public object? Key;
        public object? Value;
    }

    //
    // Instead of dependent handles, mono uses arrays of Ephemeron objects.
    //
    public struct DependentHandle : IDisposable
    {
        private Ephemeron[]? _data;

        public DependentHandle(object? target, object? dependent)
        {
            _data = new Ephemeron[1];
            _data[0].Key = target;
            _data[0].Value = dependent;
            GC.register_ephemeron_array(_data);
        }

        public bool IsAllocated => _data is not null;

        public object? Target
        {
            get => UnsafeGetTarget();
            set
            {
                Ephemeron[]? data = _data;

                if (data is null || value is not null)
                {
                    ThrowHelper.ThrowInvalidOperationException();

                    return;
                }

                data[0].Key = null;
            }
        }

        public object? Dependent
        {
            get => UnsafeGetDependent();
            set => UnsafeSetDependent(value);
        }

        public (object? Target, object? Dependent) TargetAndDependent
        {
            get
            {
                object? target = UnsafeGetTargetAndDependent(out object? dependent);

                return (target, dependent);
            }
        }

        internal object? UnsafeGetTarget()
        {
            Ephemeron[]? data = _data;

            if (data is null)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return default;
            }

            object? key = data[0].Key;

            return key != GC.EPHEMERON_TOMBSTONE ? key : null;
        }

        internal object? UnsafeGetDependent()
        {
            Ephemeron[]? data = _data;

            if (data is null)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return default;
            }

            Ephemeron e = data[0];

            return e.Key != GC.EPHEMERON_TOMBSTONE && e.Key is not null ? e.Value : null;
        }

        internal object? UnsafeGetTargetAndDependent(out object? dependent)
        {
            Ephemeron[]? data = _data;

            if (data is null)
            {
                ThrowHelper.ThrowInvalidOperationException();

                dependent = null;

                return null;
            }

            Ephemeron e = data[0];

            if (e.Key != GC.EPHEMERON_TOMBSTONE && e.Key is not null)
            {
                dependent = e.Value;

                return e.Key;
            }

            dependent = null;

            return null;
        }

        internal void UnsafeSetTargetToNull()
        {
            Ephemeron[]? data = _data;

            if (data is null)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return;
            }

            data[0].Key = null;
        }

        internal void UnsafeSetDependent(object? dependent)
        {
            Ephemeron[]? data = _data;

            if (data is null)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return;
            }

            data[0].Value = dependent;
        }

        public void Dispose()
        {
            _data = null;
        }
    }
}
