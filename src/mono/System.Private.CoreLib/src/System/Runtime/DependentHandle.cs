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
        private Ephemeron[] data;

        public DependentHandle(object? target, object? dependent)
        {
            data = new Ephemeron[1];
            data[0].Key = target;
            data[0].Value = dependent;
            GC.register_ephemeron_array(data);
        }

        public bool IsAllocated => data is not null;

        public object? Target
        {
            get => GetTarget();
            set => SetTarget(value);
        }

        public object? Dependent
        {
            get => GetDependent();
            set => SetDependent(value);
        }

        public (object? Target, object? Dependent) GetTargetAndDependent()
        {
            if (this.data is not Ephemeron[] data)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return default;
            }

            if (data[0].Key == GC.EPHEMERON_TOMBSTONE)
            {
                return default;
            }

            return (data[0].Key, data[0].Value);
        }

        internal object? UnsafeGetTarget()
        {
            return GetTarget();
        }

        internal void UnsafeSetTarget(object? target)
        {
            SetTarget(target);
        }

        internal void UnsafeSetDependent(object? dependent)
        {
            SetDependent(dependent);
        }

        internal object? UnsafeGetTargetAndDependent(out object? dependent)
        {
            if (this.data is not Ephemeron[] data)
            {
                ThrowHelper.ThrowInvalidOperationException();

                dependent = null;

                return null;
            }

            if (data[0].Key == GC.EPHEMERON_TOMBSTONE)
            {
                dependent = null;

                return null;
            }

            dependent = data[0].Value;

            return data[0].Key;
        }

        public void Dispose()
        {
            data = null!;
        }

        private object? GetTarget()
        {
            // Getting the secondary object is more expensive than getting the first so
            // we provide a separate primary-only accessor for those times we only want the
            // primary.

            if (this.data is not Ephemeron[] data)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return default;
            }

            if (data[0].Key == GC.EPHEMERON_TOMBSTONE)
            {
                return null;
            }

            return data[0].Key;
        }

        private void SetTarget(object? target)
        {
            if (this.data is not Ephemeron[] data)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return;
            }

            data[0].Key = target;
        }

        private object? GetDependent()
        {
            if (this.data is not Ephemeron[] data)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return default;
            }

            if (data[0].Key == GC.EPHEMERON_TOMBSTONE)
            {
                return null;
            }

            return data[0].Value;
        }

        private void SetDependent(object? dependent)
        {
            if (this.data is not Ephemeron[] data)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return;
            }

            data[0].Value = dependent;
        }
    }
}
