// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Tests
{
    public partial class ActivatorTests
    {
        [Fact]
        public void CreateInstanceT_Array_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance<int[]>());

        [Fact]
        public void CreateInstanceT_Interface_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance<IInterface>());

        [Fact]
        public void CreateInstanceT_AbstractClass_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance<AbstractClass>());

        [Fact]
        public void CreateInstanceT_ClassWithDeefaultConstructor_InvokesConstructor() =>
            Activator.CreateInstance<ClassWithDefaultConstructor>();

        [Fact]
        public void CreateInstanceT_ClassWithPublicConstructor_InvokesConstructor() =>
            Assert.True(Activator.CreateInstance<ClassWithPublicDefaultConstructor>().ConstructorInvoked);

        [Fact]
        public void CreateInstanceT_ClassWithPrivateConstructor_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance<ClassWithPrivateDefaultConstructor>());

        [Fact]
        public void CreateInstanceT_ClassWithoutDefaultConstructor_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance<ClassWithoutDefaultConstructor>());

        [Fact]
        public void CreateInstanceT_ClassWithDefaultConstructorThatThrows_ThrowsTargetInvocationException() =>
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance<ClassWithDefaultConstructorThatThrows>());

        [Fact]
        public void CreateInstanceT_StructWithDefaultConstructor_InvokesConstructor() =>
            Activator.CreateInstance<StructWithDefaultConstructor>();

        [Fact]
        public void CreateInstanceT_StructWithPublicDefaultConstructor_InvokesConstructor() =>
            Assert.True(Activator.CreateInstance<StructWithPublicDefaultConstructor>().ConstructorInvoked);

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51912", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public void CreateInstanceT_StructWithPrivateDefaultConstructor_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance<StructWithPrivateDefaultConstructor>());

        [Fact]
        public void CreateInstanceT_StructWithoutDefaultConstructor_InvokesConstructor() =>
            Activator.CreateInstance<StructWithoutDefaultConstructor>();

        [Fact]
        public void CreateInstanceT_StructWithDefaultConstructorThatThrows_ThrowsTargetInvocationException() =>
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance<StructWithDefaultConstructorThatThrows>());

        [Fact]
        public void CreateInstanceT_GenericTypes()
        {
            TestGenericClassWithDefaultConstructor<string>();
            TestGenericClassWithDefaultConstructor<int>();

            TestGenericStructWithDefaultConstructor<string>();
            TestGenericStructWithDefaultConstructor<int>();

            void TestGenericClassWithDefaultConstructor<T>()
                => Assert.Equal(typeof(T), Activator.CreateInstance<GenericClassWithDefaultConstructor<T>>().TypeOfT);

            void TestGenericStructWithDefaultConstructor<T>()
                => Assert.Equal(typeof(T), Activator.CreateInstance<GenericStructWithDefaultConstructor<T>>().TypeOfT);
        }

        private interface IInterface
        {
        }

        private abstract class AbstractClass
        {
        }

        public class ClassWithDefaultConstructor
        {
        }

        private class ClassWithPublicDefaultConstructor
        {
            public readonly bool ConstructorInvoked;

            public ClassWithPublicDefaultConstructor() =>
                ConstructorInvoked = true;
        }

        private class ClassWithPrivateDefaultConstructor
        {
            private ClassWithPrivateDefaultConstructor() { }
        }

        private class ClassWithoutDefaultConstructor
        {
            public ClassWithoutDefaultConstructor(int value) { }
        }

        private class ClassWithDefaultConstructorThatThrows
        {
            public ClassWithDefaultConstructorThatThrows() =>
                throw new Exception();
        }

        public class GenericClassWithDefaultConstructor<T>
        {
            public GenericClassWithDefaultConstructor() =>
                TypeOfT = typeof(T);

            public Type TypeOfT { get; }
        }

        public struct StructWithDefaultConstructorThatThrows
        {
            public StructWithDefaultConstructorThatThrows() =>
                throw new Exception();
        }

        public struct GenericStructWithDefaultConstructor<T>
        {
            public GenericStructWithDefaultConstructor() =>
                TypeOfT = typeof(T);

            public Type TypeOfT { get; }
        }
    }
}
