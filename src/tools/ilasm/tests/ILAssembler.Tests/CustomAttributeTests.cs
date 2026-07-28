// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class CustomAttributeTests
    {

        [Fact]
        public void CustomAttribute_HexByteBlob_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
                }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            // Verify the custom attribute was emitted on the assembly
            var asmDef = reader.GetAssemblyDefinition();
            var attrs = asmDef.GetCustomAttributes();
            Assert.NotEmpty(attrs);
        }


        [Fact]
        public void HexByteBlob_DigitLetterPairsCorrect()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 3F 5F 00 00 )
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var customAttrs = reader.GetCustomAttributes(MetadataTokens.MethodDefinitionHandle(1));
            foreach (var caHandle in customAttrs)
            {
                var ca = reader.GetCustomAttribute(caHandle);
                var blob = reader.GetBlobBytes(ca.Value);
                // Blob should be exactly: 01 00 3F 5F 00 00
                Assert.Equal(6, blob.Length);
                Assert.Equal(0x3F, blob[2]);
                Assert.Equal(0x5F, blob[3]);
            }
        }


        [Fact]
        public void CustomAttributeOnMethod_EmittedCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 Main() cil managed
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 00 00 )
                        .entrypoint
                        ldc.i4 100
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal("Main", reader.GetString(method.Name));

            var customAttrs = method.GetCustomAttributes();
            Assert.Equal(1, customAttrs.Count);
        }


        [Fact]
        public void CustomAttributeOnType_EmittedCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 01 00 00 )
                    .method public instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The custom attribute should be in the CustomAttribute table
            Assert.True(reader.GetTableRowCount(TableIndex.CustomAttribute) >= 1,
                "Should have at least one custom attribute");

            // Find the ComVisibleAttribute on the type
            var typeHandle = MetadataTokens.TypeDefinitionHandle(2); // Test type
            var attrs = reader.GetCustomAttributes(typeHandle);
            Assert.True(attrs.Count >= 1, "Test type should have at least one custom attribute");
        }


        [Fact]
        public void CustomAttributeBlobDescr_EmptyBraces_CorrectProlog()
        {
            // '= {}' should produce a 4-byte blob: 01 00 (prolog) 00 00 (0 named args)
            string source = """
                .assembly extern mscorlib { }
                .assembly extern xunit.core { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = {}
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestMethod");

            var attrs = reader.GetCustomAttributes(MetadataTokens.MethodDefinitionHandle(
                MetadataTokens.GetRowNumber(reader.MethodDefinitions
                    .First(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "TestMethod"))));
            Assert.True(attrs.Count >= 1);

            var attr = reader.GetCustomAttribute(attrs.First());
            var blobBytes = reader.GetBlobBytes(attr.Value);
            // Should be exactly 4 bytes: 01 00 (prolog) 00 00 (0 named args)
            Assert.Equal(4, blobBytes.Length);
            Assert.Equal(0x01, blobBytes[0]); // prolog low byte
            Assert.Equal(0x00, blobBytes[1]); // prolog high byte
            Assert.Equal(0x00, blobBytes[2]); // named arg count low
            Assert.Equal(0x00, blobBytes[3]); // named arg count high
        }

        [Fact]
        public void CustomAttribute_LocalAttributeConstructor_UsesMethodDefinitionHandleAndBlob()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed beforefieldinit LocalAttribute extends [mscorlib]System.Attribute
                {
                    .method public hidebysig specialname rtspecialname instance void .ctor(int32 value) cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void LocalAttribute::.ctor(int32) = ( 01 00 2A 00 00 00 00 00 )
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var testTypeHandle = reader.TypeDefinitions
                .First(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var customAttributeHandle = Assert.Single(reader.GetCustomAttributes(testTypeHandle));
            var customAttribute = reader.GetCustomAttribute(customAttributeHandle);

            Assert.Equal(HandleKind.MethodDefinition, customAttribute.Constructor.Kind);

            var constructorHandle = (MethodDefinitionHandle)customAttribute.Constructor;
            var constructor = reader.GetMethodDefinition(constructorHandle);
            Assert.Equal(".ctor", reader.GetString(constructor.Name));

            var attributeTypeHandle = reader.TypeDefinitions
                .First(handle => reader.GetTypeDefinition(handle).GetMethods().Contains(constructorHandle));
            Assert.Equal("LocalAttribute", reader.GetString(reader.GetTypeDefinition(attributeTypeHandle).Name));

            Assert.Equal([0x01, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x00, 0x00], reader.GetBlobBytes(customAttribute.Value));
        }

        [Fact]
        public void CustomAttribute_VerbalPrimitiveArrays_EmitExpectedBlob()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ArrayAttribute extends [mscorlib]System.Attribute
                {
                    .method public specialname rtspecialname instance void .ctor(
                        float32[] singles,
                        float64[] doubles,
                        int64[] int64s,
                        int32[] int32s,
                        int16[] int16s,
                        int8[] int8s,
                        uint64[] uint64s,
                        uint32[] uint32s,
                        uint16[] uint16s,
                        uint8[] uint8s,
                        char[] chars,
                        bool[] bools,
                        string[] strings) cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void ArrayAttribute::.ctor(
                        float32[],
                        float64[],
                        int64[],
                        int32[],
                        int16[],
                        int8[],
                        uint64[],
                        uint32[],
                        uint16[],
                        uint8[],
                        char[],
                        bool[],
                        string[]) = {
                        float32[2](1.5 2)
                        float64[2](3.5 4)
                        int64[2](5 6)
                        int32[2](7 8)
                        int16[2](9 10)
                        int8[2](11 12)
                        uint64[2](13 14)
                        uint32[2](15 16)
                        uint16[2](17 18)
                        uint8[2](19 20)
                        char[2](65 66)
                        bool[2](true false)
                        string[2]('alpha' nullref)
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var type = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var attribute = reader.GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(type)));
            CustomAttributeValue<string> value = attribute.DecodeValue(DocumentCompilerTestHelpers.Decoder);

            Assert.Empty(value.NamedArguments);
            Assert.Equal(13, value.FixedArguments.Length);
            AssertArrayArgument(value.FixedArguments[0], "float32[]", 1.5f, 2f);
            AssertArrayArgument(value.FixedArguments[1], "float64[]", 3.5, 4d);
            AssertArrayArgument(value.FixedArguments[2], "int64[]", 5L, 6L);
            AssertArrayArgument(value.FixedArguments[3], "int32[]", 7, 8);
            AssertArrayArgument(value.FixedArguments[4], "int16[]", (short)9, (short)10);
            AssertArrayArgument(value.FixedArguments[5], "int8[]", (sbyte)11, (sbyte)12);
            AssertArrayArgument(value.FixedArguments[6], "uint64[]", 13UL, 14UL);
            AssertArrayArgument(value.FixedArguments[7], "uint32[]", 15U, 16U);
            AssertArrayArgument(value.FixedArguments[8], "uint16[]", (ushort)17, (ushort)18);
            AssertArrayArgument(value.FixedArguments[9], "uint8[]", (byte)19, (byte)20);
            AssertArrayArgument(value.FixedArguments[10], "char[]", 'A', 'B');
            AssertArrayArgument(value.FixedArguments[11], "bool[]", true, false);
            AssertArrayArgument(value.FixedArguments[12], "string[]", "alpha", null);
        }

        [Fact]
        public void CustomAttribute_VerbalScalarArguments_DecodeExpectedValues()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ScalarAttribute extends [mscorlib]System.Attribute
                {
                    .method public specialname rtspecialname instance void .ctor(
                        bool booleanValue,
                        int8 int8Value,
                        uint8 uint8Value,
                        int16 int16Value,
                        uint16 uint16Value,
                        int32 int32Value,
                        uint32 uint32Value,
                        int64 int64Value,
                        uint64 uint64Value,
                        char charValue,
                        float32 singleValue,
                        float32 singleBits,
                        float64 doubleValue,
                        float64 doubleBits,
                        string text,
                        class [mscorlib]System.Type typeValue) cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void ScalarAttribute::.ctor(
                        bool,
                        int8,
                        uint8,
                        int16,
                        uint16,
                        int32,
                        uint32,
                        int64,
                        uint64,
                        char,
                        float32,
                        float32,
                        float64,
                        float64,
                        string,
                        class [mscorlib]System.Type) = {
                        bool(true)
                        int8(-1)
                        uint8(255)
                        int16(-2)
                        uint16(65535)
                        int32(-3)
                        uint32(4294967295)
                        int64(-4)
                        uint64(9223372036854775807)
                        char(65)
                        float32(1.5)
                        float32(1065353216)
                        float64(2.5)
                        float64(4607182418800017408)
                        string('text')
                        type(class 'Contoso.Target')
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var type = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            CustomAttributeValue<string> value = reader
                .GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(type)))
                .DecodeValue(DocumentCompilerTestHelpers.Decoder);

            Assert.Equal(
                new object?[]
                {
                    true,
                    (sbyte)-1,
                    (byte)255,
                    (short)-2,
                    (ushort)65535,
                    -3,
                    uint.MaxValue,
                    -4L,
                    9223372036854775807UL,
                    'A',
                    1.5f,
                    1f,
                    2.5d,
                    1d,
                    "text",
                    "Contoso.Target",
                },
                value.FixedArguments.Select(argument => argument.Value));
            Assert.Empty(value.NamedArguments);
        }

        [Fact]
        public void CustomAttribute_VerbalNamedArguments_EmitExpectedBlob()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed NamedAttribute extends [mscorlib]System.Attribute
                {
                    .method public specialname rtspecialname instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void NamedAttribute::.ctor() = {
                        field int32 Number = int32(42)
                        property string Message = string('hello')
                        property type Target = type(class 'Contoso.Target')
                        property object Boxed = object(int32(7))
                        property int32[] Values = int32[2](1 2)
                        property enum class 'Contoso.Kind' Kind = int32(3)
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var type = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var attribute = reader.GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(type)));
            CustomAttributeValue<string> value = attribute.DecodeValue(DocumentCompilerTestHelpers.Decoder);

            Assert.Empty(value.FixedArguments);
            Assert.Equal(6, value.NamedArguments.Length);
            AssertNamedArgument(value.NamedArguments[0], CustomAttributeNamedArgumentKind.Field, "Number", "int32", 42);
            AssertNamedArgument(value.NamedArguments[1], CustomAttributeNamedArgumentKind.Property, "Message", "string", "hello");
            AssertNamedArgument(value.NamedArguments[2], CustomAttributeNamedArgumentKind.Property, "Target", "System.Type", "Contoso.Target");
            AssertNamedArgument(value.NamedArguments[3], CustomAttributeNamedArgumentKind.Property, "Boxed", "int32", 7);
            Assert.Equal("Values", value.NamedArguments[4].Name);
            Assert.Equal("int32[]", value.NamedArguments[4].Type);
            AssertArrayValue(value.NamedArguments[4].Value, 1, 2);
            AssertNamedArgument(value.NamedArguments[5], CustomAttributeNamedArgumentKind.Property, "Kind", "Contoso.Kind", 3);
        }

        [Fact]
        public void CustomAttribute_VerbalTypeArgumentFromUnversionedAssemblyReference_EmitsAssemblyQualifiedTypeName()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly extern External.Assembly { }
                .assembly test { }
                .class public auto ansi sealed TypeAttribute extends [mscorlib]System.Attribute
                {
                    .method public specialname rtspecialname instance void .ctor(class [mscorlib]System.Type value) cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void TypeAttribute::.ctor(class [mscorlib]System.Type) = {
                        type([External.Assembly]Contoso.ExternalType)
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var externalAssembly = reader.AssemblyReferences
                .Select(reader.GetAssemblyReference)
                .Single(reference => reader.GetString(reference.Name) == "External.Assembly");
            var testType = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var attribute = reader.GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(testType)));

            Assert.Equal(new Version(0, 0, 0, 0), externalAssembly.Version);
            CustomAttributeValue<string> value = attribute.DecodeValue(DocumentCompilerTestHelpers.Decoder);

            var argument = Assert.Single(value.FixedArguments);
            Assert.Equal("System.Type", argument.Type);
            Assert.Equal(
                "Contoso.ExternalType, External.Assembly, PublicKeyToken=null",
                argument.Value);
            Assert.Empty(value.NamedArguments);
        }

        [Fact]
        public void CustomAttribute_VerbalTypeAndObjectArrays_PreserveElementOrder()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly extern External.Assembly { }
                .assembly test { }
                .class public auto ansi sealed ArrayAttribute extends [mscorlib]System.Attribute
                {
                    .method public specialname rtspecialname instance void .ctor(
                        class [mscorlib]System.Type[] types,
                        object[] values) cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void ArrayAttribute::.ctor(class [mscorlib]System.Type[], object[]) = {
                        type[3]([External.Assembly]Contoso.ExternalType nullref class 'Quoted.Type')
                        object[3](int32(1) string('text') object(bool(true)))
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var type = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var attribute = reader.GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(type)));
            CustomAttributeValue<string> value = attribute.DecodeValue(DocumentCompilerTestHelpers.Decoder);

            Assert.Equal(2, value.FixedArguments.Length);
            Assert.Equal("System.Type[]", value.FixedArguments[0].Type);
            AssertArrayValue(
                value.FixedArguments[0].Value,
                "Contoso.ExternalType, External.Assembly, PublicKeyToken=null",
                null,
                "Quoted.Type");

            Assert.Equal("object[]", value.FixedArguments[1].Type);
            ImmutableArray<CustomAttributeTypedArgument<string>> boxedValues =
                Assert.IsType<ImmutableArray<CustomAttributeTypedArgument<string>>>(value.FixedArguments[1].Value);
            Assert.Collection(
                boxedValues,
                item =>
                {
                    Assert.Equal("int32", item.Type);
                    Assert.Equal(1, item.Value);
                },
                item =>
                {
                    Assert.Equal("string", item.Type);
                    Assert.Equal("text", item.Value);
                },
                item =>
                {
                    Assert.Equal("bool", item.Type);
                    Assert.Equal(true, item.Value);
                });
            Assert.Empty(value.NamedArguments);
        }

        [Fact]
        public void CustomAttribute_ExplicitOwners_AttachToTypeFieldAndMethod()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public int32 Value
                    .method public static void M() cil managed
                    {
                        ret
                    }
                }

                .custom (Test) instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                .custom (field int32 Test::Value) instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                .custom (method void Test::M()) instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeHandle = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var type = reader.GetTypeDefinition(typeHandle);
            var fieldHandle = Assert.Single(type.GetFields());
            var methodHandle = Assert.Single(type.GetMethods());

            AssertCustomAttributeBlob(reader, Assert.Single(reader.GetCustomAttributes(typeHandle)));
            AssertCustomAttributeBlob(reader, Assert.Single(reader.GetCustomAttributes(fieldHandle)));
            AssertCustomAttributeBlob(reader, Assert.Single(reader.GetCustomAttributes(methodHandle)));
        }

        [Fact]
        public void CustomAttribute_NoValueAndExplicitOwnerVerbalForms_DecodeExpectedValues()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ValueAttribute extends [mscorlib]System.Attribute
                {
                    .method public specialname rtspecialname instance void .ctor(int32 value) cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor()
                }
                .custom (Test) instance void ValueAttribute::.ctor(int32) = { int32(42) }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var testType = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var attributes = reader.GetCustomAttributes(testType)
                .Select(reader.GetCustomAttribute)
                .Select(attribute => attribute.DecodeValue(DocumentCompilerTestHelpers.Decoder))
                .ToArray();

            Assert.Equal(2, attributes.Length);
            Assert.Contains(
                attributes,
                attribute => attribute.FixedArguments.Length == 0 && attribute.NamedArguments.Length == 0);
            Assert.Contains(
                attributes,
                attribute =>
                    attribute.FixedArguments.Length == 1 &&
                    attribute.FixedArguments[0].Type == "int32" &&
                    Equals(attribute.FixedArguments[0].Value, 42));
        }

        [Fact]
        public void CustomAttribute_BoxedByteArray_ReportsErrorAndEmitsDecodableFallback()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ArrayAttribute extends [mscorlib]System.Attribute
                {
                    .method public specialname rtspecialname instance void .ctor(object[] values) cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Attribute::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void ArrayAttribute::.ctor(object[]) = {
                        object[1](bytearray(01))
                    }
                }
                """;

            var compiler = new DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ => { Assert.Fail("Expected no includes"); return default; },
                _ => { Assert.Fail("Expected no resources"); return default; },
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == DiagnosticIds.InvalidMetadataToken);
            Assert.NotNull(result);

            var image = new BlobBuilder();
            result!.Serialize(image);
            using var pe = new PEReader(image.ToImmutableArray());
            var reader = pe.GetMetadataReader();
            var testType = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            CustomAttributeValue<string> attribute = reader
                .GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(testType)))
                .DecodeValue(DocumentCompilerTestHelpers.Decoder);
            var values = Assert.IsType<ImmutableArray<CustomAttributeTypedArgument<string>>>(
                Assert.Single(attribute.FixedArguments).Value);

            var value = Assert.Single(values);
            Assert.Equal("string", value.Type);
            Assert.Null(value.Value);
        }

        [Fact]
        public void AssemblyCustomAttribute_PreservesConstructorAndBlob()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
                }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var customAttributeHandle = Assert.Single(reader.GetAssemblyDefinition().GetCustomAttributes());
            var customAttribute = reader.GetCustomAttribute(customAttributeHandle);
            var constructor = reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
            var attributeType = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);

            Assert.Equal(".ctor", reader.GetString(constructor.Name));
            Assert.Equal("CompilationRelaxationsAttribute", reader.GetString(attributeType.Name));
            Assert.Equal([0x01, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00], reader.GetBlobBytes(customAttribute.Value));
        }

        private static void AssertArrayArgument(
            CustomAttributeTypedArgument<string> argument,
            string expectedType,
            params object?[] expectedValues)
        {
            Assert.Equal(expectedType, argument.Type);
            AssertArrayValue(argument.Value, expectedValues);
        }

        private static void AssertArrayValue(object? value, params object?[] expectedValues)
        {
            ImmutableArray<CustomAttributeTypedArgument<string>> values =
                Assert.IsType<ImmutableArray<CustomAttributeTypedArgument<string>>>(value);
            Assert.Equal(expectedValues, values.Select(item => item.Value));
        }

        private static void AssertNamedArgument(
            CustomAttributeNamedArgument<string> argument,
            CustomAttributeNamedArgumentKind kind,
            string name,
            string type,
            object? value)
        {
            Assert.Equal(kind, argument.Kind);
            Assert.Equal(name, argument.Name);
            Assert.Equal(type, argument.Type);
            Assert.Equal(value, argument.Value);
        }

        private static void AssertCustomAttributeBlob(MetadataReader reader, CustomAttributeHandle handle)
        {
            var attribute = reader.GetCustomAttribute(handle);
            Assert.Equal(
                [0x01, 0x00, 0x00, 0x00],
                reader.GetBlobBytes(attribute.Value));
        }

    }
}
