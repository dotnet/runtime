// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Tests
{
    public partial class ActivatorTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsComInterop))]
        public void CreateInstanceT_ComObject_Success()
        {
            WbemContext instance = Activator.CreateInstance<WbemContext>();
            Assert.NotNull(instance);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.SupportsComInterop))]
        public void CreateFactoryT_ComObject_Success()
        {
            Func<WbemContext> factory = Activator.CreateFactory<WbemContext>();
            Assert.NotNull(factory);
            WbemContext instance = factory();
            Assert.NotNull(instance);
        }

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

        [Fact]
        public void CreateFactoryT_ReferenceTypeWithPublicCtor_Success()
        {
            Func<PublicType> factory = Activator.CreateFactory<PublicType>();
            PublicType instance = factory();
            Assert.NotNull(instance);
            Assert.True(instance.CtorWasCalled);
        }

        [Fact]
        public void CreateFactoryT_ReferenceTypeWithPrivateCtor_ThrowsMissingMethodException()
            => Assert.Throws<MissingMethodException>(() => Activator.CreateFactory<PrivateTypeWithDefaultCtor>());

        [Fact]
        public void CreateFactoryT_ReferenceTypeWithPrivateCtorThatThrows_DoesNotWrapInTargetInvocationException()
        {
            Func<TypeWithDefaultCtorThatThrows> factory = Activator.CreateFactory<TypeWithDefaultCtorThatThrows>();
            Assert.Throws<Exception>(() => factory()); // no TIE
        }

        [Fact]
        public void CreateFactoryT_OfNullableT_ReturnsNull()
        {
            Func<int?> factory = Activator.CreateFactory<int?>();
            Assert.NotNull(factory);
            int? instance = factory();
            Assert.False(instance.HasValue);
        }

        [Fact]
        public void CreateFactoryT_OfValueTypeWithoutDefaultCtor_ReturnsDefaultT()
        {
            Func<ValueTypeWithParameterfulConstructor> factory = Activator.CreateFactory<ValueTypeWithParameterfulConstructor>();
            Assert.NotNull(factory);
            ValueTypeWithParameterfulConstructor instance = factory();
            Assert.False(instance.ParameterfulCtorWasCalled);
        }

        [Fact]
        public void CreateFactoryT_OfValueTypeWithPublicDefaultCtor_CallsDefaultCtor()
        {
            Func<StructWithPublicDefaultConstructor> factory = Activator.CreateFactory<StructWithPublicDefaultConstructor>();
            Assert.NotNull(factory);
            StructWithPublicDefaultConstructor instance = factory();
            Assert.True(instance.ConstructorInvoked);
            Assert.True(IsAddressOnLocalStack(instance.AddressPassedToConstructor)); // using Func<T>, value type ctor is called using ref to stack local, no boxing
        }

        [Fact]
        public void CreateFactoryT_OfValueTypeWithPrivateDefaultCtor_ThrowsMissingMethodException()
            => Assert.Throws<MissingMethodException>(() => Activator.CreateFactory<StructWithPrivateDefaultConstructor>());

        [Fact]
        public void CreateFactoryT_OfValueTypeWithPublicDefaultCtorThatThrows_DoesNotWrapInTargetInvocationException()
        {
            Func<StructWithDefaultConstructorThatThrows> factory = Activator.CreateFactory<StructWithDefaultConstructorThatThrows>();
            Assert.NotNull(factory);
            Assert.Throws<Exception>(() => factory()); // shouldn't wrap in TIE
        }

        [Fact]
        public void CreateFactoryT_Array_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateFactory<int[]>());

        [Fact]
        public void CreateFactoryT_Interface_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateFactory<IInterface>());

        [Fact]
        public void CreateFactoryT_AbstractClass_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateFactory<AbstractClass>());

        [Fact]
        public void CreateFactoryT_String_ThrowsMissingMethodException() =>
            Assert.Throws<MissingMethodException>(() => Activator.CreateFactory<string>());

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
