// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.Tests.Serialization
{
    public static class JsonPolymorphicTypeConfigurationTests
    {
        [Theory]
        [InlineData(typeof(IEnumerable))]
        [InlineData(typeof(Interface))]
        [InlineData(typeof(Class))]
        [InlineData(typeof(GenericClass<int>))]
        public static void SupportedBaseTypeArgument_ShouldSucceed(Type baseType)
        {
            var configuration = new JsonPolymorphicTypeConfiguration(baseType);
            Assert.Equal(baseType, configuration.BaseType);
            Assert.Empty(configuration);
        }

        [Theory]
        [InlineData(typeof(IEnumerable), typeof(IEnumerable<int>))]
        [InlineData(typeof(IList<string>), typeof(string[]))]
        [InlineData(typeof(MemberInfo), typeof(Type))]
        [InlineData(typeof(Interface), typeof(Class))]
        [InlineData(typeof(Interface), typeof(Struct))]
        [InlineData(typeof(Class), typeof(GenericClass<int>))]
        public static void SupportedDerivedTypeArgument_ShouldSucceed(Type baseType, Type derivedType)
        {
            var configuration = new JsonPolymorphicTypeConfiguration(baseType).WithDerivedType(derivedType);
            Assert.Equal(new (Type, object?)[] { (derivedType, null) }, configuration);

            configuration = new JsonPolymorphicTypeConfiguration(baseType).WithDerivedType(derivedType, "typeDiscriminator");
            Assert.Equal(new (Type, object?)[] { (derivedType, "typeDiscriminator") }, configuration);

            configuration = new JsonPolymorphicTypeConfiguration(baseType).WithDerivedType(derivedType, 42);
            Assert.Equal(new (Type, object?)[] { (derivedType, 42) }, configuration);
        }

        [Fact]
        public static void SupportsDeclaringBaseTypeAsDerivedType()
        {
            var configuration =
                new JsonPolymorphicTypeConfiguration(typeof(Class))
                    .WithDerivedType(typeof(Class));

            Assert.Equal(new (Type, object?)[] { (typeof(Class), null) }, configuration);

            configuration =
                new JsonPolymorphicTypeConfiguration(typeof(Class))
                    .WithDerivedType(typeof(Class), "typeDiscriminator");

            Assert.Equal(new (Type, object?)[] { (typeof(Class), "typeDiscriminator") }, configuration);

            configuration =
                new JsonPolymorphicTypeConfiguration(typeof(Class))
                    .WithDerivedType(typeof(Class), 42);

            Assert.Equal(new (Type, object?)[] { (typeof(Class), 42) }, configuration);
        }

        [Fact]
        public static void SupportsMixingAndMatchingTypeDiscriminators()
        {
            var configuration =
                new JsonPolymorphicTypeConfiguration(typeof(Class))
                    .WithDerivedType(typeof(GenericClass<bool>))
                    .WithDerivedType(typeof(GenericClass<int>), 42)
                    .WithDerivedType(typeof(GenericClass<string>), "typeDiscriminator");

            Assert.Equal(
                new (Type, object?)[]
                {
                    (typeof(GenericClass<bool>), null),
                    (typeof(GenericClass<int>), 42),
                    (typeof(GenericClass<string>), "typeDiscriminator"),
                },
                configuration);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(int*))]
        [InlineData(typeof(string))]
        [InlineData(typeof(object))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(Struct))]
        [InlineData(typeof(ReadOnlySpan<int>))]
        [InlineData(typeof(SealedClass))]
        [InlineData(typeof(GenericClass<>))]
        public static void InvalidBaseTypeArgument_ThrowsArgumentException(Type baseType)
        {
            Assert.Throws<ArgumentException>(() => new JsonPolymorphicTypeConfiguration(baseType));
        }

        [Fact]
        public static void NullBaseTypeArgument_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonPolymorphicTypeConfiguration(null));
        }

        [Theory]
        [InlineData(typeof(Interface), typeof(object))]
        [InlineData(typeof(Class), typeof(Interface))]
        [InlineData(typeof(Class), typeof(GenericClass<>))]
        public static void InvalidDerivedTypeArgument_ThrowsArgumentException(Type baseType, Type derivedType)
        {
            var configuration = new JsonPolymorphicTypeConfiguration(baseType);

            Assert.Throws<ArgumentException>(() => configuration.WithDerivedType(derivedType));
            Assert.Empty(configuration);

            Assert.Throws<ArgumentException>(() => configuration.WithDerivedType(derivedType, "typeDiscriminator"));
            Assert.Empty(configuration);
        }

        [Fact]
        public static void NullDerivedTypeArgument_ThrowsArgumentNullException()
        {
            var configuration = new JsonPolymorphicTypeConfiguration(typeof(Class));
            Assert.Throws<ArgumentNullException>(() => configuration.WithDerivedType(derivedType: null));
            Assert.Empty(configuration);
        }

        [Fact]
        public static void DuplicateDerivedType_ThrowsArgumentException()
        {
            var configuration = new JsonPolymorphicTypeConfiguration(typeof(Class)).WithDerivedType(typeof(GenericClass<int>));
            Assert.Throws<ArgumentException>(() => configuration.WithDerivedType(typeof(GenericClass<int>)));
            Assert.Equal(new (Type, object?)[] { (typeof(GenericClass<int>), null) }, configuration);
        }

        [Fact]
        public static void DuplicateTypeDiscriminator_String_ThrowsArgumentException()
        {
            var configuration =
                new JsonPolymorphicTypeConfiguration(typeof(Class))
                    .WithDerivedType(typeof(GenericClass<int>), "discriminator1")
                    .WithDerivedType(typeof(GenericClass<string>), "discriminator2");

            Assert.Equal(new (Type, object?)[] { (typeof(GenericClass<int>), "discriminator1"), (typeof(GenericClass<string>), "discriminator2") }, configuration);

            Assert.Throws<ArgumentException>(() => configuration.WithDerivedType(typeof(GenericClass<bool>), "discriminator2"));

            Assert.Equal(new (Type, object?)[] { (typeof(GenericClass<int>), "discriminator1"), (typeof(GenericClass<string>), "discriminator2") }, configuration);
        }

        [Fact]
        public static void DuplicateTypeDiscriminator_Int_ThrowsArgumentException()
        {
            var configuration =
                new JsonPolymorphicTypeConfiguration(typeof(Class))
                    .WithDerivedType(typeof(GenericClass<int>), 0)
                    .WithDerivedType(typeof(GenericClass<string>), 1);

            Assert.Equal(new (Type, object?)[] { (typeof(GenericClass<int>), 0), (typeof(GenericClass<string>), 1) }, configuration);

            Assert.Throws<ArgumentException>(() => configuration.WithDerivedType(typeof(GenericClass<bool>), 1));

            Assert.Equal(new (Type, object?)[] { (typeof(GenericClass<int>), 0), (typeof(GenericClass<string>), 1) }, configuration);
        }

        [Fact]
        public static void ModifyingAfterAssignmentToOptions_ShouldThrowInvalidOperationException()
        {
            var config = new JsonPolymorphicTypeConfiguration(typeof(Class))
                .WithDerivedType(typeof(GenericClass<int>), "derived");

            _ = new JsonSerializerOptions { PolymorphicTypeConfigurations = { config } };

            Assert.Throws<InvalidOperationException>(() => config.WithDerivedType(typeof(GenericClass<string>), "derived2"));
            Assert.Throws<InvalidOperationException>(() => config.WithDerivedType(typeof(GenericClass<string>), 42));
            Assert.Throws<InvalidOperationException>(() => config.TypeDiscriminatorPropertyName = "_case");
            Assert.Throws<InvalidOperationException>(() => config.UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType);
            Assert.Throws<InvalidOperationException>(() => config.IgnoreUnrecognizedTypeDiscriminators = true);
        }

        private interface Interface { }
        private class Class : Interface { }
        private struct Struct : Interface { }
        private sealed class SealedClass : Interface { }
        private class GenericClass<T> : Class { }
    }
}
