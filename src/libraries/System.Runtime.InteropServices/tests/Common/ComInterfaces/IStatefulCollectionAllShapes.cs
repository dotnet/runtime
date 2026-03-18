// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("4731FA5D-C103-4A22-87A1-58DCEDD4A9B3")]
    internal partial interface IStatefulCollectionAllShapes
    {
        void Method([MarshalUsing(CountElementName = nameof(size))] StatefulCollectionAllShapes<StatelessType> param, int size);
        void MethodIn([MarshalUsing(CountElementName = nameof(size))] in StatefulCollectionAllShapes<StatelessType> param, int size);
        void MethodOut([MarshalUsing(CountElementName = nameof(size))] out StatefulCollectionAllShapes<StatelessType> param, out int size);
        void MethodRef([MarshalUsing(CountElementName = nameof(size))] ref StatefulCollectionAllShapes<StatelessType> param, int size);
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatefulCollectionAllShapes<StatelessType> Return(out int size);
        [PreserveSig]
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatefulCollectionAllShapes<StatelessType> ReturnPreserveSig(out int size);
    }

    [NativeMarshalling(typeof(StatefulCollectionAllShapesMarshaller<,>))]
    internal class StatefulCollectionAllShapes<T>
    {
    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatefulCollectionAllShapes<>), MarshalMode.Default, typeof(StatefulCollectionAllShapesMarshaller<,>))]
    internal unsafe struct StatefulCollectionAllShapesMarshaller<TManagedElement, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        public StatefulCollectionAllShapesMarshaller() { }
        public void OnInvoked() { }

        // ManagedToUnmanaged
        public void FromManaged(StatefulCollectionAllShapes<TManagedElement> collection) => throw new NotImplementedException();

        public ReadOnlySpan<TManagedElement> GetManagedValuesSource() => throw new NotImplementedException();

        public Span<TUnmanagedElement> GetUnmanagedValuesDestination() => throw new NotImplementedException();

        public ref nint GetPinnableReference() => throw new NotImplementedException();

        public TUnmanagedElement* ToUnmanaged() => throw new NotImplementedException();

        public static ref TUnmanagedElement* GetPinnableReference(StatefulCollectionAllShapes<TManagedElement> collection) => throw new NotImplementedException();


        // ManagedToUnmanaged with Caller Allocated Buffer
        public static int BufferSize { get; }

        public void FromManaged(StatefulCollectionAllShapes<TManagedElement> collection, Span<byte> buffer) => throw new NotImplementedException();


        // UnmanagedToManaged
        public void FromUnmanaged(TUnmanagedElement* value) => throw new NotImplementedException(); // Should not throw exceptions.

        public ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int numElements) => throw new NotImplementedException(); // Can throw exceptions.

        public Span<TManagedElement> GetManagedValuesDestination(int numElements) => throw new NotImplementedException(); // Can throw exceptions.

        public StatefulCollectionAllShapes<TManagedElement> ToManaged() => throw new NotImplementedException(); // Can throw exceptions

        public void Free() { }


        // UnmanagedToManaged with guaranteed unmarshalling
        public StatefulCollectionAllShapes<TManagedElement> ToManagedFinally() => throw new NotImplementedException();
    }
}
