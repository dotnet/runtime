// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(), Guid("0A52BA7C-E08B-42A4-A1F4-1A2BF2C07E60")]
    internal partial interface IStatefulCollectionPinnableReference
    {
        void Method(
            [MarshalUsing(CountElementName = nameof(size))] StatefulCollectionPinnableReference<StatelessType> p,
            int size);

        void MethodIn(
            [MarshalUsing(CountElementName = nameof(size))] in StatefulCollectionPinnableReference<StatelessType> pIn,
            in int size);

        void MethodRef(
            [MarshalUsing(CountElementName = nameof(size))] ref StatefulCollectionPinnableReference<StatelessType> pRef,
            int size);

        void MethodOut(
            [MarshalUsing(CountElementName = nameof(size))] out StatefulCollectionPinnableReference<StatelessType> pOut,
            out int size);

        [return: MarshalUsing(CountElementName = nameof(size))]
        StatefulCollectionPinnableReference<StatelessType> Return(int size);

        [PreserveSig]
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatefulCollectionPinnableReference<StatelessType> ReturnPreserveSig(int size);
    }

    [NativeMarshalling(typeof(StatefulCollectionPinnableReferenceMarshaller<,>))]
    internal class StatefulCollectionPinnableReference<T>
    {
    }

    internal struct StatefulCollectionPinnableReferenceNative
    {
    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatefulCollectionPinnableReference<>), MarshalMode.ManagedToUnmanagedRef, typeof(StatefulCollectionPinnableReferenceMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatefulCollectionPinnableReference<>), MarshalMode.UnmanagedToManagedRef, typeof(StatefulCollectionPinnableReferenceMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatefulCollectionPinnableReference<>), MarshalMode.ManagedToUnmanagedOut, typeof(StatefulCollectionPinnableReferenceMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatefulCollectionPinnableReference<>), MarshalMode.UnmanagedToManagedIn, typeof(StatefulCollectionPinnableReferenceMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatefulCollectionPinnableReference<>), MarshalMode.ManagedToUnmanagedIn, typeof(StatefulCollectionPinnableReferenceMarshaller<,>.ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatefulCollectionPinnableReference<>), MarshalMode.UnmanagedToManagedOut, typeof(StatefulCollectionPinnableReferenceMarshaller<,>.ManagedToUnmanaged))]
    internal static class StatefulCollectionPinnableReferenceMarshaller<T, TUnmanaged> where TUnmanaged : unmanaged
    {
        public struct Bidirectional
        {
            public void FromManaged(StatefulCollectionPinnableReference<T> managed) => throw new NotImplementedException();
            public StatefulCollectionPinnableReferenceNative ToUnmanaged() => throw new NotImplementedException();
            public ReadOnlySpan<T> GetManagedValuesSource() => throw new NotImplementedException();
            public Span<TUnmanaged> GetUnmanagedValuesDestination() => throw new NotImplementedException();

            public void FromUnmanaged(StatefulCollectionPinnableReferenceNative unmanaged) => throw new NotImplementedException();
            public StatefulCollectionPinnableReference<T> ToManaged() => throw new NotImplementedException();
            public ReadOnlySpan<TUnmanaged> GetUnmanagedValuesSource(int numElements) => throw new NotImplementedException();
            public Span<T> GetManagedValuesDestination(int numElements) => throw new NotImplementedException();

            public void Free() => throw new NotImplementedException();
        }

        public struct UnmanagedToManaged
        {
            public void Free() => throw new NotImplementedException();
            public void FromUnmanaged(StatefulCollectionPinnableReferenceNative unmanaged) => throw new NotImplementedException();
            public StatefulCollectionPinnableReference<T> ToManaged() => throw new NotImplementedException();
            public ReadOnlySpan<TUnmanaged> GetUnmanagedValuesSource(int numElements) => throw new NotImplementedException();
            public Span<T> GetManagedValuesDestination(int numElements) => throw new NotImplementedException();
        }

        public struct ManagedToUnmanaged
        {
            public void FromManaged(StatefulCollectionPinnableReference<T> managed) => throw new NotImplementedException();
            public StatefulCollectionPinnableReferenceNative ToUnmanaged() => throw new NotImplementedException();
            public ReadOnlySpan<T> GetManagedValuesSource() => throw new NotImplementedException();
            public Span<TUnmanaged> GetUnmanagedValuesDestination() => throw new NotImplementedException();
            public void Free() => throw new NotImplementedException();
        }
    }
}
