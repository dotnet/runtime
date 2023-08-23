// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces.asdf
{
    [global::System.Runtime.InteropServices.Marshalling.GeneratedComInterface(), global::System.Runtime.InteropServices.Guid("0A52B77C-E08B-4274-A1F4-1A2BF2C07E60")]
    partial interface INaiveAPI
    {

        void Method([MarshalUsing(ConstantElementCount = 10)] StatelessCollectionAllShapes<int> p);
    }
    internal class StatelessCollectionAllShapes<T>
    {
        public T _field;
    }
    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(StatelessCollectionAllShapes<>), MarshalMode.Default, typeof(StatelessCollectionAllShapesMarshaller<,>))]
    internal unsafe static class StatelessCollectionAllShapesMarshaller<TManagedElement, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        public static void Free(TUnmanagedElement* unmanaged) { }

        // ToUnmanaged
        public static TUnmanagedElement* AllocateContainerForUnmanagedElements(StatelessCollectionAllShapes<TManagedElement> managed, out int numElements)
            => throw null;
        public static System.ReadOnlySpan<TManagedElement> GetManagedValuesSource(StatelessCollectionAllShapes<TManagedElement> managed) // Can throw exceptions
            => throw null;
        public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements) // Can throw exceptions
            => throw null;
        public static ref TUnmanagedElement* GetPinnableReference(StatelessCollectionAllShapes<TManagedElement> managed)
            => throw null;

        // Caller Allocated buffer ToUnmanaged
        public static int BufferSize { get; } = 10;
        public static TUnmanagedElement* AllocateContainerForUnmanagedElements(StatelessCollectionAllShapes<TManagedElement> managed, System.Span<byte> buffer, out int numElements)
            => throw null;

        // ToManaged
        public static StatelessCollectionAllShapes<TManagedElement> AllocateContainerForManagedElements(TUnmanagedElement* unmanaged, int numElements)
            => throw null;
        public static System.Span<TManagedElement> GetManagedValuesDestination(StatelessCollectionAllShapes<TManagedElement> managed)
            => throw null;
        public static System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TUnmanagedElement* unmanaged, int numElements)
            => throw null;

        //ToManaged Guaranteed marshalling
        public static StatelessCollectionAllShapes<TManagedElement> AllocateContainerForManagedElementsFinally(TUnmanagedElement* unmanaged, int numElements)
            => throw null;
    }
    [GeneratedComInterface]
    [Guid("9FA4A8A9-3D8F-48A8-B6FB-B45B5F1B9FB6")]
    internal partial interface IIntArray
    {
        [return: MarshalUsing(CountElementName = nameof(size))]
        int[] GetReturn(out int size);
        int GetOut([MarshalUsing(CountElementName = MarshalUsingAttribute.ReturnsCountValue)] out int[] array);
        void SetContents([MarshalUsing(CountElementName = nameof(size))] int[] array, int size);
        void FillAscending([Out][MarshalUsing(CountElementName = nameof(size))] int[] array, int size);
        void Double([In, Out][MarshalUsing(CountElementName = nameof(size))] int[] array, int size);
        void PassIn([MarshalUsing(CountElementName = nameof(size))] in int[] array, int size);
        void SwapArray([MarshalUsing(CountElementName = nameof(size))] ref int[] array, int size);
    }

    [GeneratedComClass]
    internal partial class IIntArrayImpl : IIntArray
    {
        int[] _data;
        public int[] GetReturn(out int size)
        {
            size = _data.Length;
            return _data;
        }
        public int GetOut(out int[] array)
        {
            array = _data;
            return array.Length;
        }
        public void SetContents(int[] array, int size)
        {
            _data = new int[size];
            array.CopyTo(_data, 0);
        }

        public void FillAscending(int[] array, int size)
        {
            for (int i = 0; i < size; i++)
            {
                array[i] = i;
            }
        }
        public void Double(int[] array, int size)
        {
            for (int i = 0; i < size; i++)
            {
                array[i] = array[i] * 2;
            }

        }

        public void PassIn([MarshalUsing(CountElementName = "size")] in int[] array, int size)
        {
            _data = new int[size];
            array.CopyTo(_data, 0);
        }
        public void SwapArray([MarshalUsing(CountElementName = "size")] ref int[] array, int size)
        {
            var temp = _data;
            _data = array;
            array = temp;
        }
    }
}
