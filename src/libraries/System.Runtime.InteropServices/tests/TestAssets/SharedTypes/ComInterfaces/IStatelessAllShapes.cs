// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    /// <summary>
    /// This type uses a stateless marshaller with all methods from every marshaller shape present
    /// </summary>
    [GeneratedComInterface]
    [Guid("4732FA5D-C105-4A23-87A7-58DCEDD4A9B3")]
    internal partial interface IStatelessAllShapes
    {
        void Method(StatelessAllShapesType param);
        void MethodIn(in StatelessAllShapesType param);
        void MethodOut(out StatelessAllShapesType param);
        void MethodRef(ref StatelessAllShapesType param);
        StatelessAllShapesType Return();
        [PreserveSig]
        StatelessAllShapesType ReturnPreserveSig();
    }

    [NativeMarshalling(typeof(AllStatelessMarshallerShapes))]
    internal class StatelessAllShapesType
    {
        int I;
    }

    internal struct StatelessAllShapesNative
    {
        int I;
    }

    [CustomMarshaller(typeof(StatelessAllShapesType), MarshalMode.Default, typeof(AllStatelessMarshallerShapes))]
    internal static class AllStatelessMarshallerShapes
    {
        public static ref nint GetPinnableReference(StatelessAllShapesType managed) => throw new NotImplementedException();
        public static int BufferSize => 32;
        public static StatelessAllShapesNative ConvertToUnmanaged(StatelessAllShapesType managed, Span<byte> buffer) => throw new NotImplementedException();
        public static StatelessAllShapesNative ConvertToUnmanaged(StatelessAllShapesType managed) => throw new NotImplementedException();
        public static StatelessAllShapesType ConvertToManaged(StatelessAllShapesNative unmanaged) => throw new NotImplementedException();
        public static StatelessAllShapesType ConvertToManagedFinally(StatelessAllShapesNative unmanaged) => throw new NotImplementedException();
        public static void Free(StatelessAllShapesNative unmanaged) { }
    }
}
