// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    internal struct Ephemeron
    {
        public object? key;
        public object? value;
    }
}

namespace System.Runtime
{
    //
    // Instead of dependent handles, mono uses arrays of Ephemeron objects.
    //
    public struct DependentHandle : IDisposable
    {
        private Ephemeron[] data;

        public DependentHandle(object? target, object? dependent)
        {
            data = new Ephemeron[1];
            data[0].key = target;
            data[0].value = dependent;
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

            if (data[0].key == GC.EPHEMERON_TOMBSTONE)
            {
                return default;
            }

            return (data[0].key, data[0].value);
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

            if (data[0].key == GC.EPHEMERON_TOMBSTONE)
            {
                dependent = null;

                return null;
            }

            dependent = data[0].value;

            return data[0].key;
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

            if (data[0].key == GC.EPHEMERON_TOMBSTONE)
            {
                return null;
            }

            return data[0].key;
        }

        private void SetTarget(object? target)
        {
            if (this.data is not Ephemeron[] data)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return;
            }

            data[0].key = target;
        }

        private object? GetDependent()
        {
            if (this.data is not Ephemeron[] data)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return default;
            }

            if (data[0].key == GC.EPHEMERON_TOMBSTONE)
            {
                return null;
            }

            return data[0].value;
        }

        private void SetDependent(object? dependent)
        {
            if (this.data is not Ephemeron[] data)
            {
                ThrowHelper.ThrowInvalidOperationException();

                return;
            }

            data[0].value = dependent;
        }
    }
}
