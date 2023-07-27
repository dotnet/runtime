// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    [NativeMarshalling(typeof(StatefulTypeMarshaller))]
    internal class StatefulType
    {
    }

    [CustomMarshaller(typeof(StatefulType), MarshalMode.Default, typeof(StatefulTypeMarshaller))]
    internal struct StatefulTypeMarshaller
    {
        public void FromManaged(StatefulType managed)
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

        public StatefulType ToManaged()
        {
            throw new System.NotImplementedException();
        }

        public void Free()
        {
            throw new System.NotImplementedException();
        }

        public void OnInvoked() { }
    }
}
