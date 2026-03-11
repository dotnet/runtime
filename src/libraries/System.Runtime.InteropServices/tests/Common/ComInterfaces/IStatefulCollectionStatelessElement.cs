// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(), Guid("0A52B77C-E08B-4274-A1F4-1A2BF2C07E60")]
    partial interface IStatefulCollectionStatelessElement
    {
        void Method(
            [MarshalUsing(CountElementName = nameof(size))] StatefulCollection<StatelessType> p,
            int size);
        void MethodIn(
            [MarshalUsing(CountElementName = nameof(size))] in StatefulCollection<StatelessType> pIn,
            in int size);
        void MethodRef(
            [MarshalUsing(CountElementName = nameof(size))] ref StatefulCollection<StatelessType> pRef,
            int size);
        void MethodOut(
            [MarshalUsing(CountElementName = nameof(size))] out StatefulCollection<StatelessType> pOut,
            out int size);
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatefulCollection<StatelessType> Return(int size);
        [PreserveSig]
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatefulCollection<StatelessType> ReturnPreserveSig(int size);
    }

    [NativeMarshalling(typeof(StatefulCollectionMarshaller<,>))]
    internal class StatefulCollection<T>
    {
    }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatefulCollection<>), MarshalMode.Default, typeof(StatefulCollectionMarshaller<,>.Default))]
    static unsafe class StatefulCollectionMarshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        public ref struct Default
        {
            public byte* ToUnmanaged() => throw null;
            public System.ReadOnlySpan<T> GetManagedValuesSource() => throw null;
            public System.Span<TUnmanagedElement> GetUnmanagedValuesDestination() => throw null;

            public void FromUnmanaged(byte* value) => throw null;
            public System.Span<T> GetManagedValuesDestination(int numElements) => throw null;
            public System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int numElements) => throw null;

            public void Free() => throw null;

            public void FromManaged(StatefulCollection<T> managed) => throw null;

            public StatefulCollection<T> ToManaged() => throw null;
        }
    }
}
