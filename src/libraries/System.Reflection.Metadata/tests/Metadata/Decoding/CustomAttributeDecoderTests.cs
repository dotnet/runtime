// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata.Tests;
using System.Reflection.PortableExecutable;
using Xunit;
using DecodingResources = System.Reflection.Metadata.Tests.Decoding;

namespace System.Reflection.Metadata.Decoding.Tests
{
    public class CustomAttributeDecoderTests
    {
        [Fact]
        public void TestCustomAttributeDecoder()
        {
            using (var stream = new MemoryStream(DecodingResources.CustomAttributeDecoder))
            using (var peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                var provider = new CustomAttributeTypeProvider();
                TypeDefinitionHandle typeDefHandle = FindTestType(reader, "CustomAttributeDecoderTests", "HasAttributes");

                int i = 0;
                foreach (CustomAttributeHandle attributeHandle in reader.GetCustomAttributes(typeDefHandle))
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                    CustomAttributeValue<string> value = attribute.DecodeValue(provider);

                    switch (i++)
                    {
                        case 0: // [Test]
                            Assert.Empty(value.FixedArguments);
                            Assert.Empty(value.NamedArguments);
                            break;

                        case 1: // [Test("0", 1, 2.0)]
                            Assert.Equal(3, value.FixedArguments.Length);
                            Assert.Equal("string", value.FixedArguments[0].Type);
                            Assert.Equal("0", value.FixedArguments[0].Value);
                            Assert.Equal("int32", value.FixedArguments[1].Type);
                            Assert.Equal(1, value.FixedArguments[1].Value);
                            Assert.Equal("float64", value.FixedArguments[2].Type);
                            Assert.Equal(2.0, value.FixedArguments[2].Value);
                            Assert.Empty(value.NamedArguments);
                            break;

                        case 2: // [Test(StringField = "0", Int32Field = 1, SByteEnumArrayProperty = new[] { SByteEnum.Value })]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(3, value.NamedArguments.Length);

                            Assert.Equal(CustomAttributeNamedArgumentKind.Field, value.NamedArguments[0].Kind);
                            Assert.Equal("StringField", value.NamedArguments[0].Name);
                            Assert.Equal("string", value.NamedArguments[0].Type);
                            Assert.Equal("0", value.NamedArguments[0].Value);

                            Assert.Equal(CustomAttributeNamedArgumentKind.Field, value.NamedArguments[1].Kind);
                            Assert.Equal("Int32Field", value.NamedArguments[1].Name);
                            Assert.Equal("int32", value.NamedArguments[1].Type);
                            Assert.Equal(1, value.NamedArguments[1].Value);

                            Assert.Equal(CustomAttributeNamedArgumentKind.Property, value.NamedArguments[2].Kind);
                            Assert.Equal("SByteEnumArrayProperty", value.NamedArguments[2].Name);
                            Assert.Equal("CustomAttributeDecoderTests.SByteEnum[]", value.NamedArguments[2].Type);
                            var sbyteEnumArray = (ImmutableArray<CustomAttributeTypedArgument<string>>)value.NamedArguments[2].Value;
                            Assert.Equal(1, sbyteEnumArray.Length);
                            Assert.Equal("CustomAttributeDecoderTests.SByteEnum", sbyteEnumArray[0].Type);
                            Assert.Equal((sbyte)-1, sbyteEnumArray[0].Value);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

#if NET && !TARGET_BROWSER // Generic attribute is not supported on .NET Framework.
        [Fact]
        public void TestCustomAttributeDecoderGeneric()
        {
            using (var stream = new MemoryStream(DecodingResources.CustomAttributeDecoder))
            using (var peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                var provider = new CustomAttributeTypeProvider();
                TypeDefinitionHandle typeDefHandle = FindTestType(reader, "CustomAttributeDecoderTests", "HasGenericAttributes");

                int i = 0;
                foreach (CustomAttributeHandle attributeHandle in reader.GetCustomAttributes(typeDefHandle))
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                    CustomAttributeValue<string> value = attribute.DecodeValue(provider);

                    switch (i++)
                    {
                        case 0: // [GenericAttribute<bool>]
                            Assert.Empty(value.FixedArguments);
                            Assert.Empty(value.NamedArguments);
                            break;

                        case 1: // [GenericAttribute<string>("Hello")]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string", value.FixedArguments[0].Type);
                            Assert.Equal("Hello", value.FixedArguments[0].Value);
                            Assert.Empty(value.NamedArguments);
                            break;

                        case 2: // [GenericAttribute<int>(12)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int32", value.FixedArguments[0].Type);
                            Assert.Equal(12, value.FixedArguments[0].Value);
                            Assert.Empty(value.NamedArguments);
                            break;

                        case 3: // [GenericAttribute<string>("Hello", 12, TProperty = "Bye")]
                            Assert.Equal(2, value.FixedArguments.Length);
                            Assert.Equal("string", value.FixedArguments[0].Type);
                            Assert.Equal("Hello", value.FixedArguments[0].Value);
                            Assert.Equal("int32", value.FixedArguments[1].Type);
                            Assert.Equal(12, value.FixedArguments[1].Value);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("TProperty", value.NamedArguments[0].Name);
                            Assert.Equal("string", value.NamedArguments[0].Type);
                            Assert.Equal("Bye", value.NamedArguments[0].Value);
                            break;

                        case 4: // [GenericAttribute<byte>(1, TProperty = 2)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("uint8", value.FixedArguments[0].Type);
                            Assert.Equal((byte)1, value.FixedArguments[0].Value);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("TProperty", value.NamedArguments[0].Name);
                            Assert.Equal("uint8", value.NamedArguments[0].Type);
                            Assert.Equal((byte)2, value.NamedArguments[0].Value);
                            break;

                        case 5: // [GenericAttribute2<bool, int>(true, 13)]
                            Assert.Equal(2, value.FixedArguments.Length);
                            Assert.Equal("bool", value.FixedArguments[0].Type);
                            Assert.Equal(true, value.FixedArguments[0].Value);
                            Assert.Equal("int32", value.FixedArguments[1].Type);
                            Assert.Equal(13, value.FixedArguments[1].Value);
                            Assert.Empty(value.NamedArguments);
                            break;

                        case 6: // [GenericAttribute<Type>(typeof(HasAttributes))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal(provider.GetSystemType(), value.FixedArguments[0].Type);
                            Assert.Contains("CustomAttributeDecoderTests.HasAttributes", (string)value.FixedArguments[0].Value);
                            Assert.Empty(value.NamedArguments);
                            break;

                        case 7: // [GenericAttribute<Type>(TProperty = typeof(HasAttributes))]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("TProperty", value.NamedArguments[0].Name);
                            Assert.Equal(provider.GetSystemType(), value.NamedArguments[0].Type);
                            Assert.Contains("CustomAttributeDecoderTests.HasAttributes", (string)value.NamedArguments[0].Value);
                            break;
                    }
                }

                Assert.Equal(8, i);
            }
        }

        [Fact]
        public void TestCustomAttributeDecoderGenericArray()
        {
            using (var stream = new MemoryStream(DecodingResources.CustomAttributeDecoder))
            using (var peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                var provider = new CustomAttributeTypeProvider();
                TypeDefinitionHandle typeDefHandle = FindTestType(reader, "CustomAttributeDecoderTests", "HasGenericArrayAttributes");

                int i = 0;
                foreach (CustomAttributeHandle attributeHandle in reader.GetCustomAttributes(typeDefHandle))
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                    CustomAttributeValue<string> value = attribute.DecodeValue(provider);

                    switch (i++)
                    {
                        case 0: // [GenericAttribute2<int[], byte[]>(new int[] { 1, 2, 3 }, new byte[] { 4, 5 })]
                        {
                            Assert.Equal(2, value.FixedArguments.Length);
                            ImmutableArray<CustomAttributeTypedArgument<string>> array1 = (ImmutableArray<CustomAttributeTypedArgument<string>>)(value.FixedArguments[0].Value);
                            Assert.Equal("int32[]", value.FixedArguments[0].Type);
                            Assert.Equal(1, array1[0].Value);
                            Assert.Equal(3, array1[2].Value);
                            ImmutableArray<CustomAttributeTypedArgument<string>> array2 = (ImmutableArray<CustomAttributeTypedArgument<string>>)(value.FixedArguments[1].Value);
                            Assert.Equal("uint8[]", value.FixedArguments[1].Type);
                            Assert.Equal((byte)4, array2[0].Value);
                            Assert.Equal((byte)5, array2[1].Value);
                            Assert.Empty(value.NamedArguments);
                            break;
                        }

                        case 1: // [GenericAttribute<byte>(1, TProperty = 2, TArrayProperty = new byte[] { 3, 4 })]
                        {
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("uint8", value.FixedArguments[0].Type);
                            Assert.Equal((byte)1, value.FixedArguments[0].Value);
                            Assert.Equal(2, value.NamedArguments.Length);
                            Assert.Equal("uint8", value.NamedArguments[0].Type);
                            Assert.Equal((byte)2, value.NamedArguments[0].Value);
                            ImmutableArray<CustomAttributeTypedArgument<string>> array = (ImmutableArray<CustomAttributeTypedArgument<string>>)(value.NamedArguments[1].Value);
                            Assert.Equal("uint8[]", value.NamedArguments[1].Type);
                            Assert.Equal((byte)3, array[0].Value);
                            break;
                        }
                    }
                }

                Assert.Equal(2, i);
            }
        }
#endif

        private static TypeDefinitionHandle FindTestType(MetadataReader reader, string @namespace, string name)
        {
            foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
            {
                TypeDefinition definition = reader.GetTypeDefinition(handle);
                if (reader.StringComparer.Equals(definition.Namespace, @namespace) &&
                    reader.StringComparer.Equals(definition.Name, name))
                {
                    return handle;
                }
            }

            Assert.Fail($"Cannot find test type: {@namespace}.{name}");
            return default;
        }

        private class CustomAttributeTypeProvider : DisassemblingTypeProvider, ICustomAttributeTypeProvider<string>
        {
            public string GetSystemType()
            {
                return "[System.Runtime]System.Type";
            }

            public bool IsSystemType(string type)
            {
                return type == "[System.Runtime]System.Type";
            }

            public string GetTypeFromSerializedName(string name)
            {
                return name;
            }

            public PrimitiveTypeCode GetUnderlyingEnumType(string type)
            {
                // Strip assembly-qualified suffix if present (e.g. "Namespace.Type, Assembly, Version=...")
                int commaIndex = type.IndexOf(',');
                string typeName = commaIndex >= 0 ? type.Substring(0, commaIndex).Trim() : type;

                return typeName switch
                {
                    "CustomAttributeDecoderTests.SByteEnum" => PrimitiveTypeCode.SByte,
                    "CustomAttributeDecoderTests.Int16Enum" => PrimitiveTypeCode.Int16,
                    "CustomAttributeDecoderTests.Int32Enum" => PrimitiveTypeCode.Int32,
                    "CustomAttributeDecoderTests.Int64Enum" => PrimitiveTypeCode.Int64,
                    "CustomAttributeDecoderTests.ByteEnum" => PrimitiveTypeCode.Byte,
                    "CustomAttributeDecoderTests.UInt16Enum" => PrimitiveTypeCode.UInt16,
                    "CustomAttributeDecoderTests.UInt32Enum" => PrimitiveTypeCode.UInt32,
                    "CustomAttributeDecoderTests.UInt64Enum" => PrimitiveTypeCode.UInt64,
                    "CustomAttributeDecoderTests.MyEnum" => PrimitiveTypeCode.Int32,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
            }
        }
    }
}
