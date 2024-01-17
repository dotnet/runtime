// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(), Guid("0A52B77C-E08B-4274-A1F4-1A2BF2C07E60")]
    internal partial interface IStatelessCollectionCallerAllocatedBuffer
    {
        void Method(
            [MarshalUsing(CountElementName = nameof(size))] StatelessCollectionCallerAllocatedBuffer<StatelessType> p,
            int size);

        void MethodIn(
            [MarshalUsing(CountElementName = nameof(size))] in StatelessCollectionCallerAllocatedBuffer<StatelessType> pIn,
            in int size);

        void MethodRef(
            [MarshalUsing(CountElementName = nameof(size))] ref StatelessCollectionCallerAllocatedBuffer<StatelessType> pRef,
            int size);

        void MethodOut(
            [MarshalUsing(CountElementName = nameof(size))] out StatelessCollectionCallerAllocatedBuffer<StatelessType> pOut,
            out int size);

        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessCollectionCallerAllocatedBuffer<StatelessType> Return(int size);

        [PreserveSig]
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessCollectionCallerAllocatedBuffer<StatelessType> ReturnPreserveSig(int size);
    }

    [NativeMarshalling(typeof(StatelessCollectionCallerAllocatedBufferMarshaller<,>))]
    internal class StatelessCollectionCallerAllocatedBuffer<T>
    {
    }

    internal struct StatelessCollectionCallerAllocatedBufferNative
    {

    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatelessCollectionCallerAllocatedBuffer<>), MarshalMode.ManagedToUnmanagedIn, typeof(StatelessCollectionCallerAllocatedBufferMarshaller<,>.ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatelessCollectionCallerAllocatedBuffer<>), MarshalMode.UnmanagedToManagedOut, typeof(StatelessCollectionCallerAllocatedBufferMarshaller<,>.ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatelessCollectionCallerAllocatedBuffer<>), MarshalMode.ManagedToUnmanagedOut, typeof(StatelessCollectionCallerAllocatedBufferMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessCollectionCallerAllocatedBuffer<>), MarshalMode.UnmanagedToManagedIn, typeof(StatelessCollectionCallerAllocatedBufferMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessCollectionCallerAllocatedBuffer<>), MarshalMode.UnmanagedToManagedRef, typeof(StatelessCollectionCallerAllocatedBufferMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessCollectionCallerAllocatedBuffer<>), MarshalMode.ManagedToUnmanagedRef, typeof(StatelessCollectionCallerAllocatedBufferMarshaller<,>.Bidirectional))]
    internal static unsafe class StatelessCollectionCallerAllocatedBufferMarshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        internal static class Bidirectional
        {
            public static int BufferSize => throw new NotImplementedException();
            public static StatelessCollectionCallerAllocatedBufferNative AllocateContainerForUnmanagedElements(StatelessCollectionCallerAllocatedBuffer<T> managed, Span<byte> buffer, out int numElements)
            {
                throw new NotImplementedException();
            }

            // Bidirectional requires non-buffer version of this method
            public static StatelessCollectionCallerAllocatedBufferNative AllocateContainerForUnmanagedElements(StatelessCollectionCallerAllocatedBuffer<T> managed, out int numElements)
            {
                throw new NotImplementedException();
            }

            public static StatelessCollectionCallerAllocatedBuffer<T> AllocateContainerForManagedElements(StatelessCollectionCallerAllocatedBufferNative unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<T> GetManagedValuesSource(StatelessCollectionCallerAllocatedBuffer<T> managed)
            {
                throw new NotImplementedException();
            }

            public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(StatelessCollectionCallerAllocatedBufferNative unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(StatelessCollectionCallerAllocatedBufferNative unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static Span<T> GetManagedValuesDestination(StatelessCollectionCallerAllocatedBuffer<T> managed)
            {
                throw new NotImplementedException();
            }

            public static void Free(StatelessCollectionCallerAllocatedBufferNative unmanaged) { }
        }

        internal static class ManagedToUnmanaged
        {
            public static int BufferSize => throw new NotImplementedException();
            public static StatelessCollectionCallerAllocatedBufferNative AllocateContainerForUnmanagedElements(StatelessCollectionCallerAllocatedBuffer<T> managed, Span<byte> buffer, out int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<T> GetManagedValuesSource(StatelessCollectionCallerAllocatedBuffer<T> managed)
            {
                throw new NotImplementedException();
            }

            public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(StatelessCollectionCallerAllocatedBufferNative unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static void Free(StatelessCollectionCallerAllocatedBufferNative unmanaged) => throw new NotImplementedException();
        }

        internal static class UnmanagedToManaged
        {
            public static StatelessCollectionCallerAllocatedBuffer<T> AllocateContainerForManagedElements(StatelessCollectionCallerAllocatedBufferNative unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(StatelessCollectionCallerAllocatedBufferNative unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static Span<T> GetManagedValuesDestination(StatelessCollectionCallerAllocatedBuffer<T> managed)
            {
                throw new NotImplementedException();
            }

            public static void Free(StatelessCollectionCallerAllocatedBufferNative unmanaged) => throw new NotImplementedException();

        }
    }
}
