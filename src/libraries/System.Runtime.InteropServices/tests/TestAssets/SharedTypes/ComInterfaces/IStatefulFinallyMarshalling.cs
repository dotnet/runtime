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

    [CustomMarshaller(typeof(StatefulFinallyType), MarshalMode.Default, typeof(StatefulFinallyTypeMarshaller))]
    internal struct StatefulFinallyTypeMarshaller
    {
        public void FromManaged(StatefulFinallyType managed)
        {
            throw new NotImplementedException();
        }

        public nint ToUnmanaged()
        {
            throw new NotImplementedException();
        }

        public void FromUnmanaged(nint unmanaged)
        {
            throw new NotImplementedException();
        }

        public StatefulFinallyType ToManagedFinally()
        {
            throw new NotImplementedException();
        }

        public void Free()
        {
            throw new NotImplementedException();
        }
    }
}
