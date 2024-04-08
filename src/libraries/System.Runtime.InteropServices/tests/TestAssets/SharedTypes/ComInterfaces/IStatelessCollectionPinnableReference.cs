// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(), Guid("0A52BA7C-E08B-42A4-A1F4-1A2BF2C07E60")]
    internal partial interface IStatelessCollectionPinnableReference
    {
        void Method(
            [MarshalUsing(CountElementName = nameof(size))] StatelessCollectionPinnableReference<int> p,
            int size);

        void MethodIn(
            [MarshalUsing(CountElementName = nameof(size))] in StatelessCollectionPinnableReference<int> pIn,
            in int size);

        void MethodRef(
            [MarshalUsing(CountElementName = nameof(size))] ref StatelessCollectionPinnableReference<int> pRef,
            int size);

        void MethodOut(
            [MarshalUsing(CountElementName = nameof(size))] out StatelessCollectionPinnableReference<int> pOut,
            out int size);

        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessCollectionPinnableReference<int> Return(int size);
    }

    [NativeMarshalling(typeof(StatelessCollectionPinnableReferenceMarshaller<,>))]
    internal class StatelessCollectionPinnableReference<T>
    {
    }

    internal struct StatelessCollectionPinnableReferenceNative
    {
        public static unsafe explicit operator void*(StatelessCollectionPinnableReferenceNative _) => throw new NotImplementedException();
        public static unsafe explicit operator StatelessCollectionPinnableReferenceNative(void* _) => throw new NotImplementedException();
    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatelessCollectionPinnableReference<>), MarshalMode.ManagedToUnmanagedRef, typeof(StatelessCollectionPinnableReferenceMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollectionPinnableReference<>), MarshalMode.UnmanagedToManagedRef, typeof(StatelessCollectionPinnableReferenceMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollectionPinnableReference<>), MarshalMode.ElementIn, typeof(StatelessCollectionPinnableReferenceMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollectionPinnableReference<>), MarshalMode.ElementOut, typeof(StatelessCollectionPinnableReferenceMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollectionPinnableReference<>), MarshalMode.ElementRef, typeof(StatelessCollectionPinnableReferenceMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollectionPinnableReference<>), MarshalMode.ManagedToUnmanagedOut, typeof(StatelessCollectionPinnableReferenceMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessCollectionPinnableReference<>), MarshalMode.UnmanagedToManagedIn, typeof(StatelessCollectionPinnableReferenceMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessCollectionPinnableReference<>), MarshalMode.ManagedToUnmanagedIn, typeof(StatelessCollectionPinnableReferenceMarshaller<,>.ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatelessCollectionPinnableReference<>), MarshalMode.UnmanagedToManagedOut, typeof(StatelessCollectionPinnableReferenceMarshaller<,>.ManagedToUnmanaged))]
    internal static class StatelessCollectionPinnableReferenceMarshaller<T, TUnmanaged> where TUnmanaged : unmanaged
    {
        public static class Bidirectional
        {
            public static ref StatelessCollectionPinnableReferenceNative GetPinnableReference(StatelessCollectionPinnableReference<T> managed) => throw new NotImplementedException();
            public static StatelessCollectionPinnableReferenceNative AllocateContainerForUnmanagedElements(StatelessCollectionPinnableReference<T> managed, out int numElements) => throw new NotImplementedException();
            public static StatelessCollectionPinnableReference<T> AllocateContainerForManagedElements(StatelessCollectionPinnableReferenceNative unmanaged, int numElements) => throw new NotImplementedException();
            public static ReadOnlySpan<T> GetManagedValuesSource(StatelessCollectionPinnableReference<T> managed) => throw new NotImplementedException();
            public static Span<TUnmanaged> GetUnmanagedValuesDestination(StatelessCollectionPinnableReferenceNative unmanaged, int numElements) => throw new NotImplementedException();
            public static ReadOnlySpan<TUnmanaged> GetUnmanagedValuesSource(StatelessCollectionPinnableReferenceNative unmanaged, int numElements) => throw new NotImplementedException();
            public static Span<T> GetManagedValuesDestination(StatelessCollectionPinnableReference<T> managed) => throw new NotImplementedException();
            public static void Free(StatelessCollectionPinnableReferenceNative native) { }
        }

        public static class UnmanagedToManaged
        {
            public static StatelessCollectionPinnableReference<T> AllocateContainerForManagedElements(StatelessCollectionPinnableReferenceNative unmanaged, int numElements) => throw new NotImplementedException();
            public static ReadOnlySpan<TUnmanaged> GetUnmanagedValuesSource(StatelessCollectionPinnableReferenceNative unmanaged, int numElements) => throw new NotImplementedException();
            public static Span<T> GetManagedValuesDestination(StatelessCollectionPinnableReference<T> managed) => throw new NotImplementedException();
            public static void Free(StatelessCollectionPinnableReferenceNative native) { }
        }

        public static class ManagedToUnmanaged
        {
            public static ref StatelessCollectionPinnableReferenceNative GetPinnableReference(StatelessCollectionPinnableReference<T> managed) => throw new NotImplementedException();
            public static StatelessCollectionPinnableReferenceNative AllocateContainerForUnmanagedElements(StatelessCollectionPinnableReference<T> managed, out int numElements) => throw new NotImplementedException();
            public static ReadOnlySpan<T> GetManagedValuesSource(StatelessCollectionPinnableReference<T> managed) => throw new NotImplementedException();
            public static Span<TUnmanaged> GetUnmanagedValuesDestination(StatelessCollectionPinnableReferenceNative unmanaged, int numElements) => throw new NotImplementedException();
            public static void Free(StatelessCollectionPinnableReferenceNative native) { }
        }
    }
}
