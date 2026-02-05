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
        void Arrays(
            int size,
            [MarshalUsing(CountElementName = nameof(size))]
            StatelessAllShapesType[] param,
            [MarshalUsing(CountElementName = nameof(size))]
            in StatelessAllShapesType[] paramIn,
            [MarshalUsing(CountElementName = nameof(size))]
            out StatelessAllShapesType[] paramOut,
            [MarshalUsing(CountElementName = nameof(size))]
            ref StatelessAllShapesType[] paramRef,
            [MarshalUsing(CountElementName = nameof(size))]
            [In] StatelessAllShapesType[] paramContentsIn,
            [MarshalUsing(CountElementName = nameof(size))]
            [Out] StatelessAllShapesType[] paramContentsOut,
            [MarshalUsing(CountElementName = nameof(size))]
            [In, Out] StatelessAllShapesType[] paramContentsInOut);

    }

    [NativeMarshalling(typeof(AllStatelessMarshallerShapes))]
    internal class StatelessAllShapesType
    {
    }

    internal struct StatelessAllShapesNative
    {
    }

    [CustomMarshaller(typeof(StatelessAllShapesType), MarshalMode.Default, typeof(AllStatelessMarshallerShapes))]
    internal unsafe static class AllStatelessMarshallerShapes
    {
        public static ref nint GetPinnableReference(StatelessAllShapesType managed) => throw new NotImplementedException();
        public static int BufferSize => 32;
        public static StatelessAllShapesNative* ConvertToUnmanaged(StatelessAllShapesType managed, Span<byte> buffer) => throw new NotImplementedException();
        public static StatelessAllShapesNative* ConvertToUnmanaged(StatelessAllShapesType managed) => throw new NotImplementedException();
        public static StatelessAllShapesType ConvertToManaged(StatelessAllShapesNative* unmanaged) => throw new NotImplementedException();
        public static StatelessAllShapesType ConvertToManagedFinally(StatelessAllShapesNative* unmanaged) => throw new NotImplementedException();
        public static void Free(StatelessAllShapesNative* unmanaged) { }
    }
}
