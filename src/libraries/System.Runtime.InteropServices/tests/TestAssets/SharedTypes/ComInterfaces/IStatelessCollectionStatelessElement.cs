// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(), Guid("0A52B77C-E08B-4274-A1F4-1A2BF2C07E60")]
    partial interface IStatelessCollectionStatelessElement
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

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatelessCollection<>), MarshalMode.Default, typeof(StatelessCollectionMarshaller<,>.Default))]
    internal static unsafe class StatelessCollectionMarshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        internal static class Default
        {
            public static nint AllocateContainerForUnmanagedElements(StatelessCollection<T> managed, out int numElements)
            {
                throw new System.NotImplementedException();
            }

            public static StatelessCollection<T> AllocateContainerForManagedElements(nint unmanaged, int numElements)
            {
                throw new System.NotImplementedException();
            }

            public static System.ReadOnlySpan<T> GetManagedValuesSource(StatelessCollection<T> managed)
            {
                throw new System.NotImplementedException();
            }

            public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(nint unmanaged, int numElements)
            {
                throw new System.NotImplementedException();
            }

            public static System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(nint unmanaged, int numElements)
            {
                throw new System.NotImplementedException();
            }

            public static System.Span<T> GetManagedValuesDestination(StatelessCollection<T> managed)
            {
                throw new System.NotImplementedException();
            }

            public static void Free(nint unmanaged) { }
        }
    }
}
