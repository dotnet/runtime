// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(), Guid("0A52B77C-E08B-4274-A1F4-1A2BF2C07E60")]
    partial interface ICustomCollectionStateful
    {
        [return: MarshalUsing(ConstantElementCount = 10)]
        TestCollection<Element> Method(
            [MarshalUsing(CountElementName = "pSize")] TestCollection<Element> p,
            int pSize,
            [MarshalUsing(CountElementName = "pInSize")] in TestCollection<Element> pIn,
            in int pInSize,
            int pRefSize,
            [MarshalUsing(CountElementName = "pRefSize")] ref TestCollection<Element> pRef,
            [MarshalUsing(CountElementName = "pOutSize")] out TestCollection<Element> pOut,
            out int pOutSize);
    }

    [NativeMarshalling(typeof(Marshaller<,>))]
    class TestCollection<T> { }

    [ContiguousCollectionMarshaller]
    [CustomMarshaller(typeof(TestCollection<>), MarshalMode.Default, typeof(Marshaller<,>.Ref))]
    static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
    {
        public ref struct Ref
        {
            public void FromManaged(TestCollection<T> managed) => throw null;
            public byte* ToUnmanaged() => throw null;
            public System.ReadOnlySpan<T> GetManagedValuesSource() => throw null;
            public System.Span<TUnmanagedElement> GetUnmanagedValuesDestination() => throw null;

            public void FromUnmanaged(byte* value) => throw null;
            public TestCollection<T> ToManaged() => throw null;
            public System.Span<T> GetManagedValuesDestination(int numElements) => throw null;
            public System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int numElements) => throw null;

            public void Free()
            {
                throw new System.NotImplementedException();
            }
        }
    }

    [NativeMarshalling(typeof(ElementMarshaller))]
    struct Element
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public bool b;
#pragma warning restore CS0649
    }

    [CustomMarshaller(typeof(Element), MarshalMode.ElementIn, typeof(ElementMarshaller))]
    [CustomMarshaller(typeof(Element), MarshalMode.ElementRef, typeof(ElementMarshaller))]
    [CustomMarshaller(typeof(Element), MarshalMode.ElementOut, typeof(ElementMarshaller))]
    static class ElementMarshaller
    {
        public struct Native { }
        public static Native ConvertToUnmanaged(Element e) => throw null;
        public static Element ConvertToManaged(Native n) => throw null;
    }
}
