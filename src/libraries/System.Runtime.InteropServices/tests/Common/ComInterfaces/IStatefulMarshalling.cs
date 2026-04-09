// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4731FA5D-C103-4A22-87A1-58DCEDD4A9B3")]
    internal partial interface IStatefulMarshalling
    {
        void Method(StatefulType param);
        void MethodIn(in StatefulType param);
        void MethodOut(out StatefulType param);
        void MethodRef(ref StatefulType param);
        StatefulType Return();
        [PreserveSig]
        StatefulType ReturnPreserveSig();
    }

    [GeneratedComClass]
    internal partial class StatefulMarshalling : IStatefulMarshalling
    {
        public void Method(StatefulType param) => param.i++;
        public void MethodIn(in StatefulType param) => param.i++;
        public void MethodOut(out StatefulType param) => param = new StatefulType() { i = 1 };
        public void MethodRef(ref StatefulType param) { }
        public StatefulType Return() => new StatefulType() { i = 1 };
        public StatefulType ReturnPreserveSig() => new StatefulType() { i = 1 };
    }

    [NativeMarshalling(typeof(StatefulTypeMarshaller))]
    internal class StatefulType
    {
        public int i;
    }

    internal struct StatefulNative
    {
        public int i;
    }

    [CustomMarshaller(typeof(StatefulType), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatefulType), MarshalMode.UnmanagedToManagedOut, typeof(ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatefulType), MarshalMode.ManagedToUnmanagedOut, typeof(UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatefulType), MarshalMode.UnmanagedToManagedIn, typeof(UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatefulType), MarshalMode.UnmanagedToManagedRef, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatefulType), MarshalMode.ManagedToUnmanagedRef, typeof(Bidirectional))]
    internal struct StatefulTypeMarshaller
    {
        public static int FreeCount => Bidirectional.FreeCount + ManagedToUnmanaged.FreeCount + UnmanagedToManaged.FreeCount;
        internal struct Bidirectional
        {
            public static int FreeCount { get; private set; }
            StatefulType? _managed;
            bool _hasManaged;
            StatefulNative _unmanaged;
            bool _hasUnmanaged;

            public void FromManaged(StatefulType managed)
            {
                _hasManaged = true;
                _managed = managed;
            }

            public StatefulNative ToUnmanaged()
            {
                if (!_hasManaged) throw new InvalidOperationException();
                return new StatefulNative() { i = _managed.i };
            }

            public void FromUnmanaged(StatefulNative unmanaged)
            {
                _hasUnmanaged = true;
                _unmanaged = unmanaged;
            }

            public StatefulType ToManaged()
            {
                if (!_hasUnmanaged)
                {
                    throw new InvalidOperationException();
                }
                if (_hasManaged && _managed.i == _unmanaged.i)
                {
                    return _managed;
                }
                return new StatefulType() { i = _unmanaged.i };
            }

            public void Free()
            {
                FreeCount++;
            }

            public void OnInvoked() { }
        }

        internal struct ManagedToUnmanaged
        {
            public static int FreeCount { get; private set; }
            StatefulType? _managed;
            bool _hasManaged;
            public void FromManaged(StatefulType managed)
            {
                _hasManaged = true;
                _managed = managed;
            }

            public StatefulNative ToUnmanaged()
            {
                if (!_hasManaged) throw new InvalidOperationException();
                return new StatefulNative() { i = _managed.i };
            }

            public void Free()
            {
                FreeCount++;
            }

            public void OnInvoked() { }
        }

        internal struct UnmanagedToManaged
        {
            public static int FreeCount { get; private set; }
            StatefulNative _unmanaged;
            bool _hasUnmanaged;

            public void FromUnmanaged(StatefulNative unmanaged)
            {
                _hasUnmanaged = true;
                _unmanaged = unmanaged;
            }

            public StatefulType ToManaged()
            {
                if (!_hasUnmanaged)
                {
                    throw new InvalidOperationException();
                }
                return new StatefulType() { i = _unmanaged.i };
            }

            public void Free()
            {
                FreeCount++;
            }

            public void OnInvoked() { }
        }
    }
}
