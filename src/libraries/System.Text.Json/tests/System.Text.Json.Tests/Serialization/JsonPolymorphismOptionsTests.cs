// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;
using static System.Text.Json.Serialization.Tests.PolymorphicTests;

namespace System.Text.Json.Tests.Serialization
{
    public static class JsonPolymorphismOptionsTests
    {
        [Fact]
        public static void JsonPolymorphismOptions_DefaultInstance()
        {
            var options = new JsonPolymorphismOptions();

            Assert.False(options.IgnoreUnrecognizedTypeDiscriminators);
            Assert.Equal(JsonUnknownDerivedTypeHandling.FailSerialization, options.UnknownDerivedTypeHandling);
            Assert.Equal("$type", options.TypeDiscriminatorPropertyName);
            Assert.Empty(options.DerivedTypes);
        }

        [Theory]
        [MemberData(nameof(GetDerivedTypes))]
        public static void JsonPolymorphismOptions_AddDerivedTypes(JsonDerivedType[] derivedTypes)
        {
            var options = new JsonPolymorphismOptions();
            foreach (JsonDerivedType derivedType in derivedTypes)
            {
                options.DerivedTypes.Add(derivedType);
            }

            Assert.Equal(derivedTypes, options.DerivedTypes);
        }

        public static IEnumerable<object[]> GetDerivedTypes()
        {
            yield return WrapArgs(default(JsonDerivedType));
            yield return WrapArgs(new JsonDerivedType(typeof(int)));
            yield return WrapArgs(new JsonDerivedType(typeof(void), "void"));
            yield return WrapArgs(new JsonDerivedType(typeof(object), 42));
            yield return WrapArgs(new JsonDerivedType(typeof(string)));
            yield return WrapArgs(
                new JsonDerivedType(typeof(JsonSerializerOptions)),
                new JsonDerivedType(typeof(int), 42),
                new JsonDerivedType(typeof(void), "void"));

            static object[] WrapArgs(params JsonDerivedType[] derivedTypes) => new object[] { derivedTypes };
        }

        [Fact]
        public static void JsonPolymorphismOptions_AssigningOptionsToJsonTypeInfoKindNone_ThrowsInvalidOperationException()
        {
            var options = new JsonPolymorphismOptions();
            JsonTypeInfo jti = JsonTypeInfo.CreateJsonTypeInfo(typeof(int), new());
            Assert.Equal(JsonTypeInfoKind.None, jti.Kind);

            Assert.Throws<InvalidOperationException>(() => jti.PolymorphismOptions = options);
        }

        [Fact]
        public static void JsonPolymorphismOptions_AssigningOptionsToSecondJsonTypeInfo_ThrowsInvalidOperationException()
        {
            var options = new JsonPolymorphismOptions();

            JsonTypeInfo jti1 = JsonTypeInfo.CreateJsonTypeInfo(typeof(PolymorphicClass), new());
            jti1.PolymorphismOptions = options;

            JsonTypeInfo jti2 = JsonTypeInfo.CreateJsonTypeInfo(typeof(PolymorphicClass), new());
            Assert.Throws<ArgumentException>(() => jti2.PolymorphismOptions = options);
        }

        [Fact]
        public static void JsonPolymorphismOptions_CreateBlankJsonTypeInfo_ContainsNoPolymorphismMetadata()
        {
            JsonSerializerOptions options = JsonSerializerOptions.Default;

            // Sanity check: type returns polymorphism options using the default resolver
            JsonTypeInfo jti = options.TypeInfoResolver.GetTypeInfo(typeof(PolymorphicClass), options);
            Assert.NotNull(jti.PolymorphismOptions);

            // Blank instance should not contain polymorphism options
            jti = JsonTypeInfo.CreateJsonTypeInfo(typeof(PolymorphicClass), options);
            Assert.Null(jti.PolymorphismOptions);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(object))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(IEnumerable<int>))]
        [InlineData(typeof(PolymorphicClass))]
        [InlineData(typeof(PolymorphicClass.DerivedClass1_NoTypeDiscriminator))]
        [InlineData(typeof(PolymorphicClass.DerivedClass1_TypeDiscriminator))]
        [InlineData(typeof(PolymorphicClassWithConstructor))]
        [InlineData(typeof(PolymorphicList))]
        [InlineData(typeof(PolymorphicDictionary))]
        [InlineData(typeof(PolymorphicClassWithCustomTypeDiscriminator))]
        [InlineData(typeof(PolymorphicClassWithoutDerivedTypeAttribute))]
        [InlineData(typeof(PolymorphicClass_InvalidCustomTypeDiscriminatorPropertyName))]
        [InlineData(typeof(PolymorphicClassWithNullDerivedTypeAttribute))]
        [InlineData(typeof(PolymorphicClassWithStructDerivedTypeAttribute))]
        [InlineData(typeof(PolymorphicClassWithObjectDerivedTypeAttribute))]
        [InlineData(typeof(PolymorphicClassWithNonAssignableDerivedTypeAttribute))]
        [InlineData(typeof(PolymorphicAbstractClassWithAbstractClassDerivedType))]
        [InlineData(typeof(PolymorphicClassWithDuplicateDerivedTypeRegistrations))]
        [InlineData(typeof(PolymorphicClasWithDuplicateTypeDiscriminators))]
        [InlineData(typeof(PolymorphicGenericClass<int>))]
        [InlineData(typeof(PolymorphicDerivedGenericClass.DerivedClass<int>))]
        [InlineData(typeof(PolymorphicClass_CustomConverter_TypeDiscriminator))]
        [InlineData(typeof(PolymorphicClass_CustomConverter_NoTypeDiscriminator))]
        public static void DefaultResolver_ReportsCorrectPolymorphismMetadata(Type polymorphicType)
        {
            JsonPolymorphicAttribute? polymorphicAttribute = polymorphicType.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false);
            JsonDerivedTypeAttribute[] derivedTypeAttributes = polymorphicType.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false).ToArray();

            JsonSerializer.Serialize(42); // Ensure default converters have been rooted
            var options = JsonSerializerOptions.Default;
            JsonTypeInfo jsonTypeInfo = options.TypeInfoResolver.GetTypeInfo(polymorphicType, options);

            Assert.Equal(polymorphicType, jsonTypeInfo.Type);

            JsonPolymorphismOptions? polyOptions = jsonTypeInfo.PolymorphismOptions;
            if (polymorphicAttribute == null && derivedTypeAttributes.Length == 0)
            {
                Assert.Null(polyOptions);
            }
            else
            {
                Assert.NotNull(polyOptions);

                Assert.Equal(polymorphicAttribute?.IgnoreUnrecognizedTypeDiscriminators ?? false, polyOptions.IgnoreUnrecognizedTypeDiscriminators);
                Assert.Equal(polymorphicAttribute?.UnknownDerivedTypeHandling ?? default, polyOptions.UnknownDerivedTypeHandling);
                Assert.Equal(polymorphicAttribute?.TypeDiscriminatorPropertyName ?? "$type", polyOptions.TypeDiscriminatorPropertyName);
                Assert.Equal(
                    expected: derivedTypeAttributes.Select(attr => (attr.DerivedType, attr.TypeDiscriminator)),
                    actual: polyOptions.DerivedTypes.Select(attr => (attr.DerivedType, attr.TypeDiscriminator)));
            }
        }
    }
}
