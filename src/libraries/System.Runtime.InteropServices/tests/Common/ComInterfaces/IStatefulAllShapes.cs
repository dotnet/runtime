// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4732FA5D-C105-4A23-87A7-58DCEDD4A9B3")]
    internal partial interface IStatefulAllShapes
    {
        void Method(StatefulAllShapesType param);
        void MethodIn(in StatefulAllShapesType param);
        void MethodOut(out StatefulAllShapesType param);
        void MethodRef(ref StatefulAllShapesType param);
        StatefulAllShapesType Return();
        [PreserveSig]
        StatefulAllShapesType ReturnPreserveSig();
    }

    [NativeMarshalling(typeof(AllStatefulMarshallerShapes))]
    internal class StatefulAllShapesType
    {
    }

    internal unsafe struct StatefulAllShapesNative
    {
    }

    [CustomMarshaller(typeof(StatefulAllShapesType), MarshalMode.Default, typeof(AllStatefulMarshallerShapes))]
    internal unsafe ref struct AllStatefulMarshallerShapes
    {
        public static ref nint GetPinnableReference(StatefulAllShapesType managed) => throw new NotImplementedException();
        public ref nint GetPinnableReference() => throw new NotImplementedException("This is not currently used anywhere");
        public static int BufferSize => sizeof(StatefulAllShapesNative);
        public void FromManaged(StatefulAllShapesType managed, Span<byte> buffer) => throw new NotImplementedException();
        public void FromManaged(StatefulAllShapesType managed) => throw new NotImplementedException();
        public StatefulAllShapesNative* ToUnmanaged() => throw new NotImplementedException();
        public void FromUnmanaged(StatefulAllShapesNative* unmanaged) => throw new NotImplementedException();
        public StatefulAllShapesType ToManaged() => throw new NotImplementedException();
        public StatefulAllShapesType ToManagedFinally() => throw new NotImplementedException();
        public void Free() => throw new NotImplementedException();
    }
}
