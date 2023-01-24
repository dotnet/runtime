// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class MethodBaseTests
    {
        public static IEnumerable<object[]> ContainsGenericParameters_TestData()
        {
            // Methods
            yield return new object[] { TypeExtensions.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestGenericMethod), Helpers.AllFlags), true };
            yield return new object[] { TypeExtensions.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestGenericMethod), Helpers.AllFlags).MakeGenericMethod(typeof(string)), false };
            yield return new object[] { TypeExtensions.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestMethod), Helpers.AllFlags), false };
            yield return new object[] { TypeExtensions.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestPartialGenericMethod), Helpers.AllFlags), true };
            yield return new object[] { TypeExtensions.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestGenericReturnTypeMethod), Helpers.AllFlags), true };

            yield return new object[] { TypeExtensions.GetMethod(typeof(GenericClass<int>), nameof(GenericClass<int>.TestMethod), Helpers.AllFlags), false };
            yield return new object[] { TypeExtensions.GetMethod(typeof(GenericClass<>), nameof(GenericClass<int>.TestMethod), Helpers.AllFlags), true };
            yield return new object[] { TypeExtensions.GetMethod(typeof(GenericClass<>), nameof(GenericClass<int>.TestMultipleGenericMethod), Helpers.AllFlags), true };
            yield return new object[] { TypeExtensions.GetMethod(typeof(GenericClass<int>), nameof(GenericClass<int>.TestMultipleGenericMethod), Helpers.AllFlags), true };
            yield return new object[] { TypeExtensions.GetMethod(typeof(GenericClass<>), nameof(GenericClass<int>.TestVoidMethod), Helpers.AllFlags), true };
            yield return new object[] { TypeExtensions.GetMethod(typeof(GenericClass<int>), nameof(GenericClass<int>.TestVoidMethod), Helpers.AllFlags), false };

            // Constructors
            yield return new object[] { TypeExtensions.GetConstructor(typeof(NonGenericClass), new Type[0]), false };
            yield return new object[] { TypeExtensions.GetConstructor(typeof(NonGenericClass), new Type[] { typeof(int) }), false };

            foreach (MethodBase constructor in TypeExtensions.GetConstructors(typeof(GenericClass<>)))
            {
                // ContainsGenericParameters should behave same for both methods and constructors.
                // If method/ctor or the declaring type contains uninstantiated open generic parameter,
                // ContainsGenericParameters should return true. (Which also means we can't invoke that type)
                yield return new object[] { constructor, true };
            }

            foreach (MethodBase constructor in TypeExtensions.GetConstructors(typeof(GenericClass<int>)))
            {
                yield return new object[] { constructor, false };
            }
        }

        [Theory]
        [MemberData(nameof(ContainsGenericParameters_TestData))]
        public void ContainsGenericParameters(MethodBase methodBase, bool expected)
        {
            Assert.Equal(expected, methodBase.ContainsGenericParameters);
        }

        [Theory]
        [InlineData(typeof(NonGenericClass), "TestMethod2", new Type[] { typeof(int), typeof(float), typeof(string) })]
        public void GetMethod_String_Type(Type type, string name, Type[] typeArguments)
        {
            MethodInfo method = TypeExtensions.GetMethod(type, name, typeArguments);
            Assert.Equal(name, method.Name);
        }

        public class NonGenericClass
        {
            public NonGenericClass() { }
            public NonGenericClass(int val) { }

            public void TestGenericMethod<T>(T p1) { }

            public void TestMethod(int val) { }

            public void TestMethod2(int val) { }
            public void TestMethod2(int val1, float val2, string val3) { }

            public void TestPartialGenericMethod<T>(int val, T p1) { }

            public T TestGenericReturnTypeMethod<T>() => default(T);
        }

        public class GenericClass<T>
        {
            public GenericClass() { }
            public GenericClass(T val) { }
            public GenericClass(T p, int val) { }

            public void TestMethod(T p1) { }
            public void TestMultipleGenericMethod<U>(U p2) { }
            public void TestVoidMethod() { }
        }
    }
}
