// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public class MethodInfoTests
    {
        [Theory]
        [InlineData(typeof(MI_NonGenericClass), nameof(MI_NonGenericClass.PublicMethod))]
        [InlineData(typeof(MI_NonGenericClass), nameof(MI_NonGenericClass.PublicStaticMethod))]
        [InlineData(typeof(StringImpersonator), nameof(StringImpersonator.IsNullOrEmpty))]
        public void Properties(Type type, string name)
        {
            MethodInfo method = Helpers.GetMethod(type, name);
            Assert.Equal(type, method.DeclaringType);
            Assert.Equal(type.GetTypeInfo().Module, method.Module);

            Assert.Equal(name, method.Name);
            Assert.Equal(MemberTypes.Method, method.MemberType);
        }

        [Theory]
        // Base class
        [InlineData(typeof(MI_NonGenericClass), nameof(MI_NonGenericClass.MethodA), new Type[] { typeof(string) }, typeof(MI_NonGenericClass))]
        [InlineData(typeof(MI_NonGenericClass), nameof(MI_NonGenericClass.MethodA), new Type[] { typeof(string), typeof(int) }, typeof(MI_NonGenericClass))]
        [InlineData(typeof(MI_NonGenericClass), nameof(MI_NonGenericClass.MethodA), new Type[] { typeof(int) }, typeof(MI_NonGenericClass))]
        // Abstract base class
        [InlineData(typeof(MI_AbstractClass), nameof(MI_AbstractClass.MethodA), new Type[0], typeof(MI_AbstractClass))]
        [InlineData(typeof(MI_AbstractClass), nameof(MI_AbstractClass.MethodA), new Type[] { typeof(int) }, typeof(MI_AbstractClass))]
        // Abstract sub class
        [InlineData(typeof(MI_AbstractSubClass), nameof(MI_AbstractSubClass.MethodA), new Type[0], typeof(MI_AbstractClass))]
        [InlineData(typeof(MI_AbstractSubClass), nameof(MI_AbstractSubClass.MethodA), new Type[] { typeof(int) }, typeof(MI_AbstractClass))]
        // Sub class (both have implementations)
        [InlineData(typeof(MI_BaseClass), nameof(MI_BaseClass.MethodA), new Type[] { typeof(int) }, typeof(MI_BaseClass))]
        // Unrelated classes with the same method
        [InlineData(typeof(MI_ClassWithSameMethod1), nameof(MI_ClassWithSameMethod1.MethodA), new Type[] { typeof(int) }, typeof(MI_ClassWithSameMethod1))]
        [InlineData(typeof(MI_ClassWithSameMethod2), nameof(MI_ClassWithSameMethod1.MethodA), new Type[] { typeof(int) }, typeof(MI_ClassWithSameMethod2))]
        // Interfaces
        [InlineData(typeof(MI_Interface), nameof(MI_Interface.MethodA), new Type[0], typeof(MI_Interface))]
        [InlineData(typeof(MI_ClassWithInterface), nameof(MI_ClassWithInterface.MethodA), new Type[0], typeof(MI_ClassWithInterface))]
        public void GetBaseDefinition(Type type, string name, Type[] typeArguments, Type declaringType)
        {
            MethodInfo method = type.GetMethod(name, typeArguments);
            MethodInfo baseDefinition = method.GetBaseDefinition();

            Assert.Equal(name, baseDefinition.Name);
            Assert.Equal(declaringType, baseDefinition.DeclaringType);
            if (type == declaringType)
            {
                Assert.Equal(baseDefinition, method);
            }
        }

        public static IEnumerable<object[]> GetGenericArguments_TestData()
        {
            yield return new object[] { typeof(MI_GenericClass<int>), nameof(MI_GenericClass<int>.TestGenericMethod), new string[0] };
            yield return new object[] { typeof(MI_NonGenericClass), nameof(MI_NonGenericClass.TestGenericMethod), new string[] { "T", "U" } };
            yield return new object[] { typeof(MI_NonGenericClass), nameof(MI_NonGenericClass.TestPartialGenericMethod), new string[] { "T" } };
            yield return new object[] { typeof(MI_GenericClass<int>), nameof(MI_GenericClass<int>.TestMultipleGenericMethod), new string[] { "U" } };
            yield return new object[] { typeof(MI_GenericClass<>), nameof(MI_GenericClass<int>.TestMethod), new string[0] };
            yield return new object[] { typeof(MI_GenericClass<>), nameof(MI_GenericClass<int>.TestGenericReturnTypeMethod), new string[0] };
        }

        [Theory]
        [MemberData(nameof(GetGenericArguments_TestData))]
        public void GetGenericArguments(Type type, string name, string[] argumentNames)
        {
            MethodInfo method = Helpers.GetMethod(type, name);
            Type[] arguments = method.GetGenericArguments();
            Assert.Equal(argumentNames.Length, arguments.Length);
            Assert.Equal(argumentNames, arguments.Select(argumentType => argumentType.Name));
        }

        [Fact]
        public void Invoke_StringArgument_ReturnsString()
        {
            MethodInfo method = typeof(MI_NonGenericClass).GetMethod(nameof(MI_NonGenericClass.MethodA), new Type[] { typeof(string) });
            Assert.Equal("test string", method.Invoke(new MI_NonGenericClass(), new object[] { "test string" }));
        }
    }

    public class MI_NonGenericClass
    {
        public void PublicMethod() { }
        public static void PublicStaticMethod() { }

        public T TestGenericMethod<T, U>(T p1, U p2) => p1;
        public void TestPartialGenericMethod<T>(T p1, int val) { }

        private void MethodA() { }
        public string MethodA(string str) => str;
        public void MethodA(int i32) { }
        private void MethodA(uint ui32) { }
        public int MethodA(string str, int i32) => 0;
        public uint MethodA(string str, uint ui32) => 0;
    }

    public class MI_GenericClass<T>
    {
        public void TestMultipleGenericMethod<U>(T p1, U p2) { }
        public void TestGenericMethod(T p1) { }
        public void TestMethod(int val) { }
        public T TestGenericReturnTypeMethod() => default(T);
    }

    public abstract class MI_AbstractClass
    {
        public abstract void MethodA();
        public virtual int MethodA(int i) => i;
    }

    public class MI_AbstractSubClass : MI_AbstractClass
    {
        public override void MethodA() { }
        public override int MethodA(int i) => 0;
    }

    public class MI_BaseClass
    {
        public virtual int MethodA(int i) => i;
    }

    public class MI_SubClass : MI_BaseClass
    {
        public override int MethodA(int i) => 0;
    }

    public class MI_ClassWithSameMethod1
    {
        public int MethodA(int i) => i;
    }

    public class MI_ClassWithSameMethod2
    {
        public int MethodA(int i) => 0;
    }

    public interface MI_Interface
    {
        void MethodA();
    }

    public class MI_ClassWithInterface : MI_Interface
    {
        public void MethodA() { }
    }
}
