// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("3BBB0C99-7D6C-4AD1-BE4C-ACB4C2127F02")]
    internal partial interface IStatelessPinnableCollectionBlittableElements
    {
        void Method(
            [MarshalUsing(CountElementName = nameof(size))] StatelessPinnableCollection<int> p,
            int size);

        void MethodIn(
            [MarshalUsing(CountElementName = nameof(size))] in StatelessPinnableCollection<int> pIn,
            in int size);

        void MethodRef(
            [MarshalUsing(CountElementName = nameof(size))] ref StatelessPinnableCollection<int> pRef,
            int size);

        void MethodOut(
            [MarshalUsing(CountElementName = nameof(size))] out StatelessPinnableCollection<int> pOut,
            out int size);

        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessPinnableCollection<int> Return(int size);

        [PreserveSig]
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessPinnableCollection<int> ReturnPreserveSig(int size);
    }

    [NativeMarshalling(typeof(StatelessPinnableCollectionMarshaller<,>))]
    internal class StatelessPinnableCollection<T> where T : unmanaged
    {
    }

    internal unsafe struct StatelessPinnableCollectionNative<T> where T : unmanaged
    {
        public static explicit operator StatelessPinnableCollectionNative<T>(void* ptr) => new StatelessPinnableCollectionNative<T>();
        public static explicit operator void*(StatelessPinnableCollectionNative<T> ptr) => (void*)null;
    }


    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatelessPinnableCollection<>), MarshalMode.ManagedToUnmanagedIn, typeof(StatelessPinnableCollectionMarshaller<,>.ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatelessPinnableCollection<>), MarshalMode.UnmanagedToManagedOut, typeof(StatelessPinnableCollectionMarshaller<,>.ManagedToUnmanaged))]
    [CustomMarshaller(typeof(StatelessPinnableCollection<>), MarshalMode.ManagedToUnmanagedOut, typeof(StatelessPinnableCollectionMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessPinnableCollection<>), MarshalMode.UnmanagedToManagedIn, typeof(StatelessPinnableCollectionMarshaller<,>.UnmanagedToManaged))]
    [CustomMarshaller(typeof(StatelessPinnableCollection<>), MarshalMode.UnmanagedToManagedRef, typeof(StatelessPinnableCollectionMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessPinnableCollection<>), MarshalMode.ManagedToUnmanagedRef, typeof(StatelessPinnableCollectionMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessPinnableCollection<>), MarshalMode.ElementIn, typeof(StatelessPinnableCollectionMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessPinnableCollection<>), MarshalMode.ElementOut, typeof(StatelessPinnableCollectionMarshaller<,>.Bidirectional))]
    [CustomMarshaller(typeof(StatelessPinnableCollection<>), MarshalMode.ElementRef, typeof(StatelessPinnableCollectionMarshaller<,>.Bidirectional))]
    internal static unsafe class StatelessPinnableCollectionMarshaller<T, TUnmanagedElement>
        where T : unmanaged
        where TUnmanagedElement : unmanaged
    {
        internal static class Bidirectional
        {
            public static StatelessPinnableCollectionNative<T> AllocateContainerForUnmanagedElements(StatelessPinnableCollection<T> managed, out int numElements)
            {
                throw new NotImplementedException();
            }

            public static StatelessPinnableCollection<T> AllocateContainerForManagedElements(StatelessPinnableCollectionNative<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<T> GetManagedValuesSource(StatelessPinnableCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(StatelessPinnableCollectionNative<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(StatelessPinnableCollectionNative<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static Span<T> GetManagedValuesDestination(StatelessPinnableCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static ref StatelessPinnableCollectionNative<T> GetPinnableReference(StatelessPinnableCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static void Free(StatelessPinnableCollectionNative<T> unmanaged) { }
        }

        internal static class ManagedToUnmanaged
        {
            public static StatelessPinnableCollectionNative<T> AllocateContainerForUnmanagedElements(StatelessPinnableCollection<T> managed, out int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<T> GetManagedValuesSource(StatelessPinnableCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(StatelessPinnableCollectionNative<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ref StatelessPinnableCollectionNative<T> GetPinnableReference(StatelessPinnableCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static void Free(StatelessPinnableCollectionNative<T> unmanaged) => throw new NotImplementedException();
        }

        internal static class UnmanagedToManaged
        {
            public static StatelessPinnableCollection<T> AllocateContainerForManagedElements(StatelessPinnableCollectionNative<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(StatelessPinnableCollectionNative<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            // Should be removed: https://github.com/dotnet/runtime/issues/89885
            public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(StatelessPinnableCollectionNative<T> unmanaged, int numElements)
            {
                throw new NotImplementedException();
            }

            public static Span<T> GetManagedValuesDestination(StatelessPinnableCollection<T> managed)
            {
                throw new NotImplementedException();
            }

            public static void Free(StatelessPinnableCollectionNative<T> unmanaged) => throw new NotImplementedException();

        }
    }
}
