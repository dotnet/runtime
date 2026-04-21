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

                        case 3: // [Test("0", 1, 2.0, StringField = "0", Int32Field = 1, DoubleField = 2.0)]
                            Assert.Equal(3, value.FixedArguments.Length);
                            Assert.Equal(3, value.NamedArguments.Length);
                            Assert.Equal("0", value.FixedArguments[0].Value);
                            Assert.Equal(1, value.FixedArguments[1].Value);
                            Assert.Equal(2.0, value.FixedArguments[2].Value);
                            Assert.Equal("StringField", value.NamedArguments[0].Name);
                            Assert.Equal("0", value.NamedArguments[0].Value);
                            Assert.Equal("Int32Field", value.NamedArguments[1].Name);
                            Assert.Equal(1, value.NamedArguments[1].Value);
                            Assert.Equal("DoubleField", value.NamedArguments[2].Name);
                            Assert.Equal(2.0, value.NamedArguments[2].Value);
                            break;

                        case 4: // [Test((object)null)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string", value.FixedArguments[0].Type);
                            Assert.Null(value.FixedArguments[0].Value);
                            break;

                        case 5: // [Test((string)null)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string", value.FixedArguments[0].Type);
                            Assert.Null(value.FixedArguments[0].Value);
                            break;

                        case 6: // [Test((Type)null)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal(provider.GetSystemType(), value.FixedArguments[0].Type);
                            Assert.Null(value.FixedArguments[0].Value);
                            break;

                        case 7: // [Test((int[])null)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int32[]", value.FixedArguments[0].Type);
                            Assert.Null(value.FixedArguments[0].Value);
                            break;

                        case 8: // [Test("string")]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string", value.FixedArguments[0].Type);
                            Assert.Equal("string", value.FixedArguments[0].Value);
                            break;

                        case 9: // [Test((sbyte)-1)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int8", value.FixedArguments[0].Type);
                            Assert.Equal((sbyte)-1, value.FixedArguments[0].Value);
                            break;

                        case 10: // [Test((short)-2)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int16", value.FixedArguments[0].Type);
                            Assert.Equal((short)-2, value.FixedArguments[0].Value);
                            break;

                        case 11: // [Test((int)-4)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int32", value.FixedArguments[0].Type);
                            Assert.Equal(-4, value.FixedArguments[0].Value);
                            break;

                        case 12: // [Test((long)-8)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int64", value.FixedArguments[0].Type);
                            Assert.Equal((long)-8, value.FixedArguments[0].Value);
                            break;

                        case 13: // [Test((sbyte)-1)] duplicate
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int8", value.FixedArguments[0].Type);
                            Assert.Equal((sbyte)-1, value.FixedArguments[0].Value);
                            break;

                        case 14: // [Test((short)-2)] duplicate
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int16", value.FixedArguments[0].Type);
                            Assert.Equal((short)-2, value.FixedArguments[0].Value);
                            break;

                        case 15: // [Test((int)-4)] duplicate
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int32", value.FixedArguments[0].Type);
                            Assert.Equal(-4, value.FixedArguments[0].Value);
                            break;

                        case 16: // [Test((long)-8)] duplicate
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int64", value.FixedArguments[0].Type);
                            Assert.Equal((long)-8, value.FixedArguments[0].Value);
                            break;

                        case 17: // [Test((byte)1)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("uint8", value.FixedArguments[0].Type);
                            Assert.Equal((byte)1, value.FixedArguments[0].Value);
                            break;

                        case 18: // [Test((ushort)2)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("uint16", value.FixedArguments[0].Type);
                            Assert.Equal((ushort)2, value.FixedArguments[0].Value);
                            break;

                        case 19: // [Test((uint)4)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("uint32", value.FixedArguments[0].Type);
                            Assert.Equal((uint)4, value.FixedArguments[0].Value);
                            break;

                        case 20: // [Test((ulong)8)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("uint64", value.FixedArguments[0].Type);
                            Assert.Equal((ulong)8, value.FixedArguments[0].Value);
                            break;

                        case 21: // [Test(true)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("bool", value.FixedArguments[0].Type);
                            Assert.Equal(true, value.FixedArguments[0].Value);
                            break;

                        case 22: // [Test(false)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("bool", value.FixedArguments[0].Type);
                            Assert.Equal(false, value.FixedArguments[0].Value);
                            break;

                        case 23: // [Test(typeof(string))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal(provider.GetSystemType(), value.FixedArguments[0].Type);
                            Assert.Contains("System.String", (string)value.FixedArguments[0].Value);
                            break;

                        case 24: // [Test(new string[] { })]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string[]", value.FixedArguments[0].Type);
                            Assert.Empty((ImmutableArray<CustomAttributeTypedArgument<string>>)value.FixedArguments[0].Value);
                            break;

                        case 25: // [Test(new string[] { "x", "y", "z", null })]
                        {
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string[]", value.FixedArguments[0].Type);
                            var strArray = (ImmutableArray<CustomAttributeTypedArgument<string>>)value.FixedArguments[0].Value;
                            Assert.Equal(4, strArray.Length);
                            Assert.Equal("x", strArray[0].Value);
                            Assert.Equal("y", strArray[1].Value);
                            Assert.Equal("z", strArray[2].Value);
                            Assert.Null(strArray[3].Value);
                            break;
                        }

                        case 26: // [Test((object)("string"))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string", value.FixedArguments[0].Type);
                            Assert.Equal("string", value.FixedArguments[0].Value);
                            break;

                        case 27: // [Test((object)(sbyte)-1)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int8", value.FixedArguments[0].Type);
                            Assert.Equal((sbyte)-1, value.FixedArguments[0].Value);
                            break;

                        case 28: // [Test((object)(short)-2)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int16", value.FixedArguments[0].Type);
                            Assert.Equal((short)-2, value.FixedArguments[0].Value);
                            break;

                        case 29: // [Test((object)(int)-4)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int32", value.FixedArguments[0].Type);
                            Assert.Equal(-4, value.FixedArguments[0].Value);
                            break;

                        case 30: // [Test((object)(long)-8)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int64", value.FixedArguments[0].Type);
                            Assert.Equal((long)-8, value.FixedArguments[0].Value);
                            break;

                        case 31: // [Test((object)(sbyte)-1)] duplicate
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int8", value.FixedArguments[0].Type);
                            Assert.Equal((sbyte)-1, value.FixedArguments[0].Value);
                            break;

                        case 32: // [Test((object)(short)-2)] duplicate
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int16", value.FixedArguments[0].Type);
                            Assert.Equal((short)-2, value.FixedArguments[0].Value);
                            break;

                        case 33: // [Test((object)(int)-4)] duplicate
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int32", value.FixedArguments[0].Type);
                            Assert.Equal(-4, value.FixedArguments[0].Value);
                            break;

                        case 34: // [Test((object)(long)-8)] duplicate
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("int64", value.FixedArguments[0].Type);
                            Assert.Equal((long)-8, value.FixedArguments[0].Value);
                            break;

                        case 35: // [Test((object)(byte)1)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("uint8", value.FixedArguments[0].Type);
                            Assert.Equal((byte)1, value.FixedArguments[0].Value);
                            break;

                        case 36: // [Test((object)(ushort)2)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("uint16", value.FixedArguments[0].Type);
                            Assert.Equal((ushort)2, value.FixedArguments[0].Value);
                            break;

                        case 37: // [Test((object)(uint)4)]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("uint32", value.FixedArguments[0].Type);
                            Assert.Equal((uint)4, value.FixedArguments[0].Value);
                            break;

                        case 38: // [Test((object)(true))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("bool", value.FixedArguments[0].Type);
                            Assert.Equal(true, value.FixedArguments[0].Value);
                            break;

                        case 39: // [Test((object)(false))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("bool", value.FixedArguments[0].Type);
                            Assert.Equal(false, value.FixedArguments[0].Value);
                            break;

                        case 40: // [Test((object)(typeof(string)))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Contains("System.String", (string)value.FixedArguments[0].Value);
                            break;

                        case 41: // [Test((object)(SByteEnum.Value))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.StartsWith("CustomAttributeDecoderTests.SByteEnum", value.FixedArguments[0].Type);
                            Assert.Equal((sbyte)-1, value.FixedArguments[0].Value);
                            break;

                        case 42: // [Test((object)(Int16Enum.Value))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.StartsWith("CustomAttributeDecoderTests.Int16Enum", value.FixedArguments[0].Type);
                            Assert.Equal((short)-2, value.FixedArguments[0].Value);
                            break;

                        case 43: // [Test((object)(Int32Enum.Value))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.StartsWith("CustomAttributeDecoderTests.Int32Enum", value.FixedArguments[0].Type);
                            Assert.Equal(-3, value.FixedArguments[0].Value);
                            break;

                        case 44: // [Test((object)(Int64Enum.Value))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.StartsWith("CustomAttributeDecoderTests.Int64Enum", value.FixedArguments[0].Type);
                            Assert.Equal((long)-4, value.FixedArguments[0].Value);
                            break;

                        case 45: // [Test((object)(ByteEnum.Value))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.StartsWith("CustomAttributeDecoderTests.ByteEnum", value.FixedArguments[0].Type);
                            Assert.Equal((byte)1, value.FixedArguments[0].Value);
                            break;

                        case 46: // [Test((object)(UInt16Enum.Value))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.StartsWith("CustomAttributeDecoderTests.UInt16Enum", value.FixedArguments[0].Type);
                            Assert.Equal((ushort)2, value.FixedArguments[0].Value);
                            break;

                        case 47: // [Test((object)(UInt32Enum.Value))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.StartsWith("CustomAttributeDecoderTests.UInt32Enum", value.FixedArguments[0].Type);
                            Assert.Equal((uint)3, value.FixedArguments[0].Value);
                            break;

                        case 48: // [Test((object)(UInt64Enum.Value))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.StartsWith("CustomAttributeDecoderTests.UInt64Enum", value.FixedArguments[0].Type);
                            Assert.Equal((ulong)4, value.FixedArguments[0].Value);
                            break;

                        case 49: // [Test((object)(new string[] { }))]
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string[]", value.FixedArguments[0].Type);
                            Assert.Empty((ImmutableArray<CustomAttributeTypedArgument<string>>)value.FixedArguments[0].Value);
                            break;

                        case 50: // [Test((object)(new string[] { "x", "y", "z", null }))]
                        {
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string[]", value.FixedArguments[0].Type);
                            var strArray = (ImmutableArray<CustomAttributeTypedArgument<string>>)value.FixedArguments[0].Value;
                            Assert.Equal(4, strArray.Length);
                            break;
                        }

                        case 51: // [Test((object)(new Int32Enum[] { Int32Enum.Value }))]
                        {
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.StartsWith("CustomAttributeDecoderTests.Int32Enum[]", value.FixedArguments[0].Type);
                            var int32EnumArray = (ImmutableArray<CustomAttributeTypedArgument<string>>)value.FixedArguments[0].Value;
                            Assert.Equal(1, int32EnumArray.Length);
                            Assert.Equal(-3, int32EnumArray[0].Value);
                            break;
                        }

                        case 52: // [Test(new object[] { ... })]
                        {
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("object[]", value.FixedArguments[0].Type);
                            var objArray = (ImmutableArray<CustomAttributeTypedArgument<string>>)value.FixedArguments[0].Value;
                            Assert.Equal(25, objArray.Length);
                            Assert.Equal("string", objArray[0].Value);
                            Assert.Equal((sbyte)-1, objArray[1].Value);
                            Assert.Equal((short)-2, objArray[2].Value);
                            Assert.Equal(-4, objArray[3].Value);
                            Assert.Equal((long)-8, objArray[4].Value);
                            Assert.Equal((byte)1, objArray[9].Value);
                            Assert.Equal((ushort)2, objArray[10].Value);
                            Assert.Equal((uint)4, objArray[11].Value);
                            Assert.Equal(true, objArray[12].Value);
                            Assert.Equal(false, objArray[13].Value);
                            Assert.Contains("System.String", (string)objArray[14].Value);
                            break;
                        }

                        case 53: // [Test(StringField = "string")]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal(CustomAttributeNamedArgumentKind.Field, value.NamedArguments[0].Kind);
                            Assert.Equal("StringField", value.NamedArguments[0].Name);
                            Assert.Equal("string", value.NamedArguments[0].Type);
                            Assert.Equal("string", value.NamedArguments[0].Value);
                            break;

                        case 54: // [Test(SByteField = -1)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("SByteField", value.NamedArguments[0].Name);
                            Assert.Equal("int8", value.NamedArguments[0].Type);
                            Assert.Equal((sbyte)-1, value.NamedArguments[0].Value);
                            break;

                        case 55: // [Test(Int16Field = -2)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("Int16Field", value.NamedArguments[0].Name);
                            Assert.Equal("int16", value.NamedArguments[0].Type);
                            Assert.Equal((short)-2, value.NamedArguments[0].Value);
                            break;

                        case 56: // [Test(Int32Field = -4)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("Int32Field", value.NamedArguments[0].Name);
                            Assert.Equal("int32", value.NamedArguments[0].Type);
                            Assert.Equal(-4, value.NamedArguments[0].Value);
                            break;

                        case 57: // [Test(Int64Field = -8)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("Int64Field", value.NamedArguments[0].Name);
                            Assert.Equal("int64", value.NamedArguments[0].Type);
                            Assert.Equal((long)-8, value.NamedArguments[0].Value);
                            break;

                        case 58: // [Test(SByteField = -1)] duplicate
                            Assert.Equal("int8", value.NamedArguments[0].Type);
                            Assert.Equal((sbyte)-1, value.NamedArguments[0].Value);
                            break;

                        case 59: // [Test(Int16Field = -2)] duplicate
                            Assert.Equal("int16", value.NamedArguments[0].Type);
                            Assert.Equal((short)-2, value.NamedArguments[0].Value);
                            break;

                        case 60: // [Test(Int32Field = -4)] duplicate
                            Assert.Equal("int32", value.NamedArguments[0].Type);
                            Assert.Equal(-4, value.NamedArguments[0].Value);
                            break;

                        case 61: // [Test(Int64Field = -8)] duplicate
                            Assert.Equal("int64", value.NamedArguments[0].Type);
                            Assert.Equal((long)-8, value.NamedArguments[0].Value);
                            break;

                        case 62: // [Test(ByteField = 1)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("ByteField", value.NamedArguments[0].Name);
                            Assert.Equal("uint8", value.NamedArguments[0].Type);
                            Assert.Equal((byte)1, value.NamedArguments[0].Value);
                            break;

                        case 63: // [Test(UInt16Field = 2)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("UInt16Field", value.NamedArguments[0].Name);
                            Assert.Equal("uint16", value.NamedArguments[0].Type);
                            Assert.Equal((ushort)2, value.NamedArguments[0].Value);
                            break;

                        case 64: // [Test(UInt32Field = 4)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("UInt32Field", value.NamedArguments[0].Name);
                            Assert.Equal("uint32", value.NamedArguments[0].Type);
                            Assert.Equal((uint)4, value.NamedArguments[0].Value);
                            break;

                        case 65: // [Test(UInt64Field = 8)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("UInt64Field", value.NamedArguments[0].Name);
                            Assert.Equal("uint64", value.NamedArguments[0].Type);
                            Assert.Equal((ulong)8, value.NamedArguments[0].Value);
                            break;

                        case 66: // [Test(BooleanField = true)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("BooleanField", value.NamedArguments[0].Name);
                            Assert.Equal("bool", value.NamedArguments[0].Type);
                            Assert.Equal(true, value.NamedArguments[0].Value);
                            break;

                        case 67: // [Test(BooleanField = false)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("BooleanField", value.NamedArguments[0].Name);
                            Assert.Equal("bool", value.NamedArguments[0].Type);
                            Assert.Equal(false, value.NamedArguments[0].Value);
                            break;

                        case 68: // [Test(TypeField = typeof(string))]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("TypeField", value.NamedArguments[0].Name);
                            Assert.Equal(provider.GetSystemType(), value.NamedArguments[0].Type);
                            Assert.Contains("System.String", (string)value.NamedArguments[0].Value);
                            break;

                        case 69: // [Test(SByteEnumField = SByteEnum.Value)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("SByteEnumField", value.NamedArguments[0].Name);
                            Assert.Equal("CustomAttributeDecoderTests.SByteEnum", value.NamedArguments[0].Type);
                            Assert.Equal((sbyte)-1, value.NamedArguments[0].Value);
                            break;

                        case 70: // [Test(Int16EnumField = Int16Enum.Value)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("Int16EnumField", value.NamedArguments[0].Name);
                            Assert.Equal("CustomAttributeDecoderTests.Int16Enum", value.NamedArguments[0].Type);
                            Assert.Equal((short)-2, value.NamedArguments[0].Value);
                            break;

                        case 71: // [Test(Int32EnumField = Int32Enum.Value)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("Int32EnumField", value.NamedArguments[0].Name);
                            Assert.Equal("CustomAttributeDecoderTests.Int32Enum", value.NamedArguments[0].Type);
                            Assert.Equal(-3, value.NamedArguments[0].Value);
                            break;

                        case 72: // [Test(Int64EnumField = Int64Enum.Value)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("Int64EnumField", value.NamedArguments[0].Name);
                            Assert.Equal("CustomAttributeDecoderTests.Int64Enum", value.NamedArguments[0].Type);
                            Assert.Equal((long)-4, value.NamedArguments[0].Value);
                            break;

                        case 73: // [Test(ByteEnumField = ByteEnum.Value)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("ByteEnumField", value.NamedArguments[0].Name);
                            Assert.Equal("CustomAttributeDecoderTests.ByteEnum", value.NamedArguments[0].Type);
                            Assert.Equal((byte)1, value.NamedArguments[0].Value);
                            break;

                        case 74: // [Test(UInt16EnumField = UInt16Enum.Value)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("UInt16EnumField", value.NamedArguments[0].Name);
                            Assert.Equal("CustomAttributeDecoderTests.UInt16Enum", value.NamedArguments[0].Type);
                            Assert.Equal((ushort)2, value.NamedArguments[0].Value);
                            break;

                        case 75: // [Test(UInt32EnumField = UInt32Enum.Value)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("UInt32EnumField", value.NamedArguments[0].Name);
                            Assert.Equal("CustomAttributeDecoderTests.UInt32Enum", value.NamedArguments[0].Type);
                            Assert.Equal((uint)3, value.NamedArguments[0].Value);
                            break;

                        case 76: // [Test(UInt64EnumField = UInt64Enum.Value)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal("UInt64EnumField", value.NamedArguments[0].Name);
                            Assert.Equal("CustomAttributeDecoderTests.UInt64Enum", value.NamedArguments[0].Type);
                            Assert.Equal((ulong)4, value.NamedArguments[0].Value);
                            break;

                        case 77: // [Test(new string[] { })] second occurrence
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string[]", value.FixedArguments[0].Type);
                            Assert.Empty((ImmutableArray<CustomAttributeTypedArgument<string>>)value.FixedArguments[0].Value);
                            break;

                        case 78: // [Test(new string[] { "x", "y", "z", null })] second occurrence
                        {
                            Assert.Equal(1, value.FixedArguments.Length);
                            Assert.Equal("string[]", value.FixedArguments[0].Type);
                            var strArray = (ImmutableArray<CustomAttributeTypedArgument<string>>)value.FixedArguments[0].Value;
                            Assert.Equal(4, strArray.Length);
                            break;
                        }

                        case 79: // [Test(ObjectField = null)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("ObjectField", value.NamedArguments[0].Name);
                            Assert.Equal("string", value.NamedArguments[0].Type);
                            Assert.Null(value.NamedArguments[0].Value);
                            break;

                        case 80: // [Test(StringField = null)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("StringField", value.NamedArguments[0].Name);
                            Assert.Equal("string", value.NamedArguments[0].Type);
                            Assert.Null(value.NamedArguments[0].Value);
                            break;

                        case 81: // [Test(Int32ArrayProperty = null)]
                            Assert.Empty(value.FixedArguments);
                            Assert.Equal(1, value.NamedArguments.Length);
                            Assert.Equal("Int32ArrayProperty", value.NamedArguments[0].Name);
                            Assert.Equal("int32[]", value.NamedArguments[0].Type);
                            Assert.Null(value.NamedArguments[0].Value);
                            break;
                    }
                }

                Assert.Equal(82, i);
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
