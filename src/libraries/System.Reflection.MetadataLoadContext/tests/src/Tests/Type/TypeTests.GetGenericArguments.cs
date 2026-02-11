// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class TypeTests
    {
        [Fact]
        public static void GetGenericArguments_ReturnsTypeArray()
        {
            // Test that GetGenericArguments() returns Type[] not RoType[]
            Type genericType = typeof(GenericClass1<>).Project();
            Type[] genericArguments = genericType.GetGenericArguments();

            // Verify the returned array is Type[]
            Assert.Equal(typeof(Type[]), genericArguments.GetType());

            // Verify we can use AsSpan() on the result (this would throw ArrayTypeMismatchException with RoType[])
            ReadOnlySpan<Type> span = genericArguments.AsSpan();
            Assert.Equal(1, span.Length);
        }

        [Fact]
        public static void GetGenericArguments_MultipleParameters_ReturnsTypeArray()
        {
            // Test with multiple generic parameters
            Type genericType = typeof(GenericClass2<,>).Project();
            Type[] genericArguments = genericType.GetGenericArguments();

            // Verify the returned array is Type[]
            Assert.Equal(typeof(Type[]), genericArguments.GetType());

            // Verify we can use AsSpan() on the result
            ReadOnlySpan<Type> span = genericArguments.AsSpan();
            Assert.Equal(2, span.Length);
        }

        [Fact]
        public static void GetGenericArguments_ConstructedGenericType_ReturnsTypeArray()
        {
            // Test with a constructed generic type
            Type constructedType = typeof(GenericClass1<int>).Project();
            Type[] genericArguments = constructedType.GetGenericArguments();

            // Verify the returned array is Type[]
            Assert.Equal(typeof(Type[]), genericArguments.GetType());

            // Verify we can use AsSpan() on the result
            ReadOnlySpan<Type> span = genericArguments.AsSpan();
            Assert.Equal(1, span.Length);
            Assert.Equal("Int32", span[0].Name);
        }

        [Fact]
        public static void GetGenericArguments_NonGenericType_ReturnsEmptyTypeArray()
        {
            // Test with a non-generic type
            Type nonGenericType = typeof(object).Project();
            Type[] genericArguments = nonGenericType.GetGenericArguments();

            // Verify the returned array is Type[]
            Assert.Equal(typeof(Type[]), genericArguments.GetType());

            // Verify we can use AsSpan() on the result
            ReadOnlySpan<Type> span = genericArguments.AsSpan();
            Assert.Equal(0, span.Length);
        }
    }
}
