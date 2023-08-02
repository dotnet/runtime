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

    [NativeMarshalling(typeof(StatefulFinallyTypeMarshaller))]
    internal class StatefulFinallyType
    {
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
            public void FromManaged(StatefulFinallyType managed)
            {
                throw new System.NotImplementedException();
            }

            public nint ToUnmanaged()
            {
                throw new System.NotImplementedException();
            }

            public void FromUnmanaged(nint unmanaged)
            {
                throw new System.NotImplementedException();
            }

            public StatefulFinallyType ToManagedFinally()
            {
                throw new NotImplementedException();
            }


            public void Free()
            {
                throw new System.NotImplementedException();
            }

            public void OnInvoked() { }
        }

        internal struct ManagedToUnmanaged
        {
            public void FromManaged(StatefulFinallyType managed)
            {
                throw new System.NotImplementedException();
            }

            public nint ToUnmanaged()
            {
                throw new System.NotImplementedException();
            }

            public void Free()
            {
                throw new System.NotImplementedException();
            }

            public void OnInvoked() { }
        }

        internal struct UnmanagedToManaged
        {
            public void FromUnmanaged(nint unmanaged)
            {
                throw new System.NotImplementedException();
            }
            public StatefulFinallyType ToManagedFinally()
            {
                throw new NotImplementedException();
            }

            public void Free()
            {
                throw new System.NotImplementedException();
            }

            public void OnInvoked() { }
        }
    }
}
