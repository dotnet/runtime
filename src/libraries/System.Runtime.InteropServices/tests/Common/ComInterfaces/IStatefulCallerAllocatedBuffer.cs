// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4731FA5D-C103-4A22-87A1-58DCEDD4A9B3")]
    internal partial interface IStatefulCallerAllocatedBufferMarshalling
    {
        void Method(StatefulCallerAllocatedBufferType param);
        void MethodIn(in StatefulCallerAllocatedBufferType param);
        void MethodOut(out StatefulCallerAllocatedBufferType param);
        void MethodRef(ref StatefulCallerAllocatedBufferType param);
        StatefulCallerAllocatedBufferType Return();
        [PreserveSig]
        StatefulCallerAllocatedBufferType ReturnPreserveSig();
    }

    [NativeMarshalling(typeof(StatefulCallerAllocatedBufferTypeMarshaller))]
    internal class StatefulCallerAllocatedBufferType
    {
    }

    [CustomMarshaller(typeof(StatefulCallerAllocatedBufferType), MarshalMode.Default, typeof(StatefulCallerAllocatedBufferTypeMarshaller))]
    internal struct StatefulCallerAllocatedBufferTypeMarshaller
    {
        public static int BufferSize => 64;

        public void FromManaged(StatefulCallerAllocatedBufferType managed, Span<byte> buffer)
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

        public StatefulCallerAllocatedBufferType ToManaged()
        {
            throw new NotImplementedException();
        }

        public void Free()
        {
            throw new NotImplementedException();
        }
    }
}
