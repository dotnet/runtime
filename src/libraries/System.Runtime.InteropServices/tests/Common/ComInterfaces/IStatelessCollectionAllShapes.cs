// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4731FA5D-C103-4A22-87A1-58DCEDD4A9B3")]
    internal partial interface IStatelessCollectionAllShapes
    {
        void Method([MarshalUsing(CountElementName = nameof(size))] StatelessCollectionAllShapes<StatelessType> param, int size);
        void MethodIn([MarshalUsing(CountElementName = nameof(size))] in StatelessCollectionAllShapes<StatelessType> param, int size);
        void MethodOut([MarshalUsing(CountElementName = nameof(size))] out StatelessCollectionAllShapes<StatelessType> param, out int size);
        void MethodRef([MarshalUsing(CountElementName = nameof(size))] ref StatelessCollectionAllShapes<StatelessType> param, int size);
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessCollectionAllShapes<StatelessType> Return(out int size);
        [PreserveSig]
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessCollectionAllShapes<StatelessType> ReturnPreserveSig(out int size);
    }

    [NativeMarshalling(typeof(StatelessCollectionAllShapesMarshaller<,>))]
    internal class StatelessCollectionAllShapes<T>
    {
    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatelessCollectionAllShapes<>), MarshalMode.Default, typeof(StatelessCollectionAllShapesMarshaller<,>))]
    internal unsafe static class StatelessCollectionAllShapesMarshaller<TManagedElement, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        public static void Free(TUnmanagedElement* unmanaged) { }

        // ToUnmanaged
        public static TUnmanagedElement* AllocateContainerForUnmanagedElements(StatelessCollectionAllShapes<TManagedElement> managed, out int numElements)
            => throw new NotImplementedException();

        public static ReadOnlySpan<TManagedElement> GetManagedValuesSource(StatelessCollectionAllShapes<TManagedElement> managed) // Can throw exceptions
            => throw new NotImplementedException();

        public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements) // Can throw exceptions
            => throw new NotImplementedException();

        public static ref TUnmanagedElement* GetPinnableReference(StatelessCollectionAllShapes<TManagedElement> managed)
            => throw new NotImplementedException();



        // Caller Allocated buffer ToUnmanaged
        public static int BufferSize { get; }
        public static TUnmanagedElement* AllocateContainerForUnmanagedElements(StatelessCollectionAllShapes<TManagedElement> managed, Span<byte> buffer, out int numElements)
            => throw new NotImplementedException();


        // ToManaged
        public static StatelessCollectionAllShapes<TManagedElement> AllocateContainerForManagedElements(TUnmanagedElement* unmanaged, int numElements)
            => throw new NotImplementedException();

        public static Span<TManagedElement> GetManagedValuesDestination(StatelessCollectionAllShapes<TManagedElement> managed)
            => throw new NotImplementedException();

        public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TUnmanagedElement* unmanaged, int numElements)
            => throw new NotImplementedException();


        //ToManaged Guaranteed marshalling
        public static StatelessCollectionAllShapes<TManagedElement> AllocateContainerForManagedElementsFinally(TUnmanagedElement* unmanaged, int numElements)
            => throw new NotImplementedException();
    }
}
