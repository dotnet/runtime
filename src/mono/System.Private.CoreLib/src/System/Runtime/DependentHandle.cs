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
            set => UnsafeSetTarget(value);
        }

        public object? Dependent
        {
            get => GetDependent();
            set => UnsafeSetDependent(value);
        }

        public (object? Target, object? Dependent) GetTargetAndDependent()
        {
            Ephemeron[]? data = _data;

            if (data is null)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return default;
            }

            Ephemeron e = data[0];

            return e.Key != GC.EPHEMERON_TOMBSTONE ? (e.Key, e.Value) : default;
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

        internal void UnsafeSetTarget(object? target)
        {
            Ephemeron[]? data = _data;

            if (data is null)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return;
            }

            data[0].Key = target;
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

            if (e.Key != GC.EPHEMERON_TOMBSTONE)
            {
                dependent = e.Value;

                return e.Key;
            }
            else
            {
                dependent = null;

                return null;
            }
        }

        public void Dispose()
        {
            _data = null;
        }

        private object? GetDependent()
        {
            Ephemeron[]? data = _data;

            if (data is null)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return default;
            }

            Ephemeron e = data[0];

            return e.Key != GC.EPHEMERON_TOMBSTONE ? e.Value : null;
        }
    }
}
