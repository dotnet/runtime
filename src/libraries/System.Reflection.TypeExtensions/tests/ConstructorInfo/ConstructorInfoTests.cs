// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class ConstructorInfoTests
    {
        public class ConstructorInfoInvoke
        {
            public ConstructorInfoInvoke() { }
            public ConstructorInfoInvoke(int i) { }
            public ConstructorInfoInvoke(int i, string s) { }
            public ConstructorInfoInvoke(string s, int i) { }
            public ConstructorInfoInvoke(int i, int j, int k) { throw new Exception(); }
        }

        public abstract class TestAbstractClass
        {
            public TestAbstractClass() { }
            public abstract void TestAbstractMethod();
        }

        [Fact]
        public void ConstructorName_ReturnsExpected()
        {
            Assert.Equal(".ctor", ConstructorInfo.ConstructorName);
        }

        public static IEnumerable<object[]> Invoke_TestData()
        {
            yield return new object[] { new Type[0], new object[0] };
            yield return new object[] { new Type[] { typeof(int) }, new object[] { 1 } };
            yield return new object[] { new Type[] { typeof(int), typeof(string) }, new object[] { 1, "Hello, Test!" } };
            yield return new object[] { new Type[] { typeof(string), typeof(int) }, new object[] { "Hello, Test!", 1 } };
            yield return new object[] { new Type[] { typeof(string), typeof(int) }, new object[] { null, 1 } };
        }

        [Theory]
        [MemberData(nameof(Invoke_TestData))]
        public void Invoke(Type[] constructorTypeParameters, object[] parameters)
        {
            ConstructorInfo constructor = TypeExtensions.GetConstructor(typeof(ConstructorInfoInvoke), constructorTypeParameters);
            object constructedObject = constructor.Invoke(parameters);
            Assert.NotNull(constructedObject);
        }

        public static IEnumerable<object[]> Invoke_Invalid_TestData()
        {
            // Constructor is in an abstract class
            yield return new object[] { typeof(TestAbstractClass), new Type[0], new object[0], typeof(MemberAccessException) };

            // Mismatched values
            yield return new object[] { typeof(ConstructorInfoInvoke), new Type[] { typeof(int), typeof(string) }, new object[] { 1, 2 }, typeof(ArgumentException) };

            // Constructor throws an exception
            yield return new object[] { typeof(ConstructorInfoInvoke), new Type[] { typeof(int), typeof(int), typeof(int) }, new object[] { 1, 2, 3 }, typeof(TargetInvocationException) };

            // Incorrect number of parameters
            yield return new object[] { typeof(ConstructorInfoInvoke), new Type[] { typeof(int), typeof(string) }, new object[] { 1, "test", "test1" }, typeof(TargetParameterCountException) };
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15317", TestRuntimes.Mono)]
        [MemberData(nameof(Invoke_Invalid_TestData))]
        public void Invoke_Invalid(Type constructorParent, Type[] constructorTypeParameters, object[] parameters, Type exceptionType)
        {
            ConstructorInfo constructor = TypeExtensions.GetConstructor(constructorParent, constructorTypeParameters);
            Assert.Throws(exceptionType, () => constructor.Invoke(parameters));
        }

        [Fact]
        public void TypeConstructorName_ReturnsExpected()
        {
            Assert.Equal(".cctor", ConstructorInfo.TypeConstructorName);
        }

        [Theory]
        [InlineData(typeof(ConstructorInfoInvoke), new Type[] { typeof(int) })]
        [InlineData(typeof(string), new Type[] { typeof(char), typeof(int) })]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51211", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public void Properties(Type type, Type[] typeParameters)
        {
            ConstructorInfo constructor = TypeExtensions.GetConstructor(type, typeParameters);

            Assert.Equal(type, constructor.DeclaringType);
            Assert.Equal(type.GetTypeInfo().Module, constructor.Module);
            Assert.Equal(ConstructorInfo.ConstructorName, constructor.Name);

            Assert.True(constructor.IsConstructor);
            Assert.Equal(MemberTypes.Constructor, constructor.MemberType);
        }
    }
}
