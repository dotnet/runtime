// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class MethodBaseTests
    {
        public static IEnumerable<object[]> ContainsGenericParameters_TestData()
        {
            // Methods
            yield return new object[] { Helpers.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestGenericMethod)), true };
            yield return new object[] { Helpers.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestGenericMethod)).MakeGenericMethod(typeof(int)), false };
            yield return new object[] { Helpers.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestMethod)), false };
            yield return new object[] { Helpers.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestPartialGenericMethod)), true };
            yield return new object[] { Helpers.GetMethod(typeof(NonGenericClass), nameof(NonGenericClass.TestGenericReturnTypeMethod)), true };

            yield return new object[] { Helpers.GetMethod(typeof(GenericClass<int>), nameof(GenericClass<int>.TestMethod)), false };
            yield return new object[] { Helpers.GetMethod(typeof(GenericClass<>), nameof(GenericClass<int>.TestMethod)), true };
            yield return new object[] { Helpers.GetMethod(typeof(GenericClass<>), nameof(GenericClass<int>.TestMultipleGenericMethod)), true };
            yield return new object[] { Helpers.GetMethod(typeof(GenericClass<int>), nameof(GenericClass<int>.TestMultipleGenericMethod)), true };
            yield return new object[] { Helpers.GetMethod(typeof(GenericClass<>), nameof(GenericClass<int>.TestVoidMethod)), true };
            yield return new object[] { Helpers.GetMethod(typeof(GenericClass<int>), nameof(GenericClass<int>.TestVoidMethod)), false };

            // Constructors
            yield return new object[] { typeof(NonGenericClass).GetConstructor(new Type[0]), false };
            yield return new object[] { typeof(NonGenericClass).GetConstructor(new Type[] { typeof(int) }), false };

            foreach (MethodBase constructor in typeof(GenericClass<>).GetConstructors())
            {
                // ContainsGenericParameters should behave same for both methods and constructors.
                // If method/ctor or the declaring type contains uninstantiated open generic parameter,
                // ContainsGenericParameters should return true. (Which also means we can't invoke that type)
                yield return new object[] { constructor, true };
            }

            foreach (MethodBase constructor in typeof(GenericClass<int>).GetConstructors())
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
            MethodInfo method = type.GetMethod(name, typeArguments);
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
