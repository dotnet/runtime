// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(), Guid("0A52B77C-E08B-4274-A1F4-1A2BF2C07E60")]
    internal partial interface IStatelessCollectionStatelessElement
    {
        void Method(
            [MarshalUsing(CountElementName = nameof(size))] StatelessCollection<StatelessType> p,
            int size);

        void MethodIn(
            [MarshalUsing(CountElementName = nameof(size))] in StatelessCollection<StatelessType> pIn,
            in int size);

        void MethodRef(
            [MarshalUsing(CountElementName = nameof(size))] ref StatelessCollection<StatelessType> pRef,
            int size);

        void MethodOut(
            [MarshalUsing(CountElementName = nameof(size))] out StatelessCollection<StatelessType> pOut,
            out int size);

        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessCollection<StatelessType> Return(int size);

        [PreserveSig]
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessCollection<StatelessType> ReturnPreserveSig(int size);
    }

    [NativeMarshalling(typeof(StatelessCollectionMarshaller<,>))]
    internal class StatelessCollection<T>
    {
    }

    internal struct NativeCollection<T>
    {

    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.ManagedToUnmanagedIn, typeof(StatelessCollectionMarshaller<,>.ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.UnmanagedToManagedOut, typeof(StatelessCollectionMarshaller<,>.ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.ManagedToUnmanagedOut, typeof(StatelessCollectionMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.UnmanagedToManagedIn, typeof(StatelessCollectionMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.UnmanagedToManagedRef, typeof(StatelessCollectionMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.ManagedToUnmanagedRef, typeof(StatelessCollectionMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.ElementIn, typeof(StatelessCollectionMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.ElementOut, typeof(StatelessCollectionMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.ElementRef, typeof(StatelessCollectionMarshaller<,>.Bidirectional))]
    internal static unsafe class StatelessCollectionMarshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        internal static class Bidirectional
        {
            public static NativeCollection<T> AllocateContainerForUnmanagedElements(StatelessCollection<T> managed, out int numElements)
            {
                throw new NotImplementedException();
            }

            public static StatelessCollection<T> AllocateContainerForManagedElements(NativeCollection<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<T> GetManagedValuesSource(StatelessCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(NativeCollection<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(NativeCollection<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static Span<T> GetManagedValuesDestination(StatelessCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static void Free(NativeCollection<T> unmanaged) { }
        }

        internal static class ManagedToUnmanaged
        {
            public static NativeCollection<T> AllocateContainerForUnmanagedElements(StatelessCollection<T> managed, out int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<T> GetManagedValuesSource(StatelessCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(NativeCollection<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static void Free(NativeCollection<T> unmanaged) => throw new NotImplementedException();
        }

        internal static class UnmanagedToManaged
        {
            public static StatelessCollection<T> AllocateContainerForManagedElements(NativeCollection<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(NativeCollection<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static Span<T> GetManagedValuesDestination(StatelessCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static void Free(NativeCollection<T> unmanaged) => throw new NotImplementedException();
        }
    }
}
