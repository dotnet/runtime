// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public void CreateInstanceT_StructWithPrivateDefaultConstructor_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance<StructWithPrivateDefaultConstructor>());

        [Fact]
        public void CreateInstanceT_StructWithoutDefaultConstructor_ThrowsMissingMethodException() =>
            Activator.CreateInstance<StructWithoutDefaultConstructor>();

        [Fact]
        public void CreateInstanceT_StructWithDefaultConstructorThatThrows_ThrowsTargetInvocationException() =>
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance<StructWithDefaultConstructorThatThrows>());

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
    }
}
