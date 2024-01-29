// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4731FA5D-C103-4A22-87A1-58DCEDD4A9B3")]
    internal partial interface IStatefulFinallyMarshalling
    {
        void Method(StatefulFinallyType param);
        void MethodIn(in StatefulFinallyType param);
        void MethodOut(out StatefulFinallyType param);
        void MethodRef(ref StatefulFinallyType param);
        StatefulFinallyType Return();
        [PreserveSig]
        StatefulFinallyType ReturnPreserveSig();
    }

    [GeneratedComClass]
    internal partial class StatefulFinallyMarshalling : IStatefulFinallyMarshalling
    {
        public void Method(StatefulFinallyType param)
        {
            _ = param.i;
        }
        public void MethodIn(in StatefulFinallyType param)
        {
            _ = param.i;
        }
        public void MethodOut(out StatefulFinallyType param)
        {
            param = new StatefulFinallyType() { i = 42 };
        }
        public void MethodRef(ref StatefulFinallyType param)
        {
            _ = param.i;
            param = new StatefulFinallyType() { i = 99 };
        }
        public StatefulFinallyType Return()
            => new StatefulFinallyType() { i = 8 };
        public StatefulFinallyType ReturnPreserveSig()
            => new StatefulFinallyType() { i = 3 };
    }

    [NativeMarshalling(typeof(StatefulFinallyTypeMarshaller))]
    internal class StatefulFinallyType
    {
        public int i;
    }

    internal struct StatefulFinallyNative
    {
        public int i;
    }

    [CustomMarshaller(typeof(StatefulFinallyType), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatefulFinallyType), MarshalMode.UnmanagedToManagedOut, typeof(ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatefulFinallyType), MarshalMode.ManagedToUnmanagedOut, typeof(UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatefulFinallyType), MarshalMode.UnmanagedToManagedIn, typeof(UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatefulFinallyType), MarshalMode.UnmanagedToManagedRef, typeof(Bidirectional))]
    [CustomMarshaller(typeof(StatefulFinallyType), MarshalMode.ManagedToUnmanagedRef, typeof(Bidirectional))]
    internal struct StatefulFinallyTypeMarshaller
    {
        internal struct Bidirectional
        {
            int managed_i;
            int unmanaged_i;

            public void FromManaged(StatefulFinallyType managed)
            {
                managed_i = managed.i;
            }

            public StatefulFinallyNative ToUnmanaged()
            {
                return new StatefulFinallyNative() { i = this.managed_i };
            }

            public void FromUnmanaged(StatefulFinallyNative unmanaged)
            {
                unmanaged_i = unmanaged.i;
            }

            public StatefulFinallyType ToManagedFinally()
            {
                return new StatefulFinallyType() { i = unmanaged_i };
            }

            public void Free() { }

            public void OnInvoked() { }
        }

        internal struct ManagedToUnmanaged
        {
            int managed_i;

            public void FromManaged(StatefulFinallyType managed)
            {
                managed_i = managed.i;
            }

            public StatefulFinallyNative ToUnmanaged()
            {
                return new StatefulFinallyNative() { i = this.managed_i };
            }

            public void Free() { }

            public void OnInvoked() { }
        }

        internal struct UnmanagedToManaged
        {
            int unmanaged_i;

            public void FromUnmanaged(StatefulFinallyNative unmanaged)
            {
                unmanaged_i = unmanaged.i;
            }
            public StatefulFinallyType ToManagedFinally()
            {
                return new StatefulFinallyType() { i = unmanaged_i };
            }

            public void Free() { }

            public void OnInvoked() { }
        }
    }
}
