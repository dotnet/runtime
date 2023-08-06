// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4731FA5D-C103-4A22-87A1-58DCEDD4A9B3")]
    internal partial interface IStatefulPinnedMarshalling
    {
        void Method(StatefulPinnedType param);
        void MethodIn(in StatefulPinnedType param);
        void MethodOut(out StatefulPinnedType param);
        void MethodRef(ref StatefulPinnedType param);
        StatefulPinnedType Return();
        [PreserveSig]
        StatefulPinnedType ReturnPreserveSig();
    }

    [NativeMarshalling(typeof(StatefulPinnedTypeMarshaller))]
    internal class StatefulPinnedType
    {
    }

    [CustomMarshaller(typeof(StatefulPinnedType), MarshalMode.Default, typeof(StatefulPinnedTypeMarshaller))]
    internal struct StatefulPinnedTypeMarshaller
    {

        public static int BufferSize => 64;
        public ref int GetPinnableReference() => throw new System.NotImplementedException();
        public void FromManaged(StatefulPinnedType managed, Span<byte> buffer)
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

        public StatefulPinnedType ToManaged()
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
