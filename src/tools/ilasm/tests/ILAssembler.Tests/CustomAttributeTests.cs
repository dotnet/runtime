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
                        float32[] emptySingles,
                        float64[] emptyDoubles,
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
                        string[] strings,
                        string[] emptyStrings) cil managed
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
                        string[],
                        string[]) = {
                        float32[2](1.5 2)
                        float64[2](3.5 4)
                        float32[0]( )
                        float64[0]( )
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
                        string[0]( )
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
            Assert.Equal(16, value.FixedArguments.Length);
            AssertArrayArgument(value.FixedArguments[0], "float32[]", 1.5f, 2f);
            AssertArrayArgument(value.FixedArguments[1], "float64[]", 3.5, 4d);
            AssertArrayArgument(value.FixedArguments[2], "float32[]");
            AssertArrayArgument(value.FixedArguments[3], "float64[]");
            AssertArrayArgument(value.FixedArguments[4], "int64[]", 5L, 6L);
            AssertArrayArgument(value.FixedArguments[5], "int32[]", 7, 8);
            AssertArrayArgument(value.FixedArguments[6], "int16[]", (short)9, (short)10);
            AssertArrayArgument(value.FixedArguments[7], "int8[]", (sbyte)11, (sbyte)12);
            AssertArrayArgument(value.FixedArguments[8], "uint64[]", 13UL, 14UL);
            AssertArrayArgument(value.FixedArguments[9], "uint32[]", 15U, 16U);
            AssertArrayArgument(value.FixedArguments[10], "uint16[]", (ushort)17, (ushort)18);
            AssertArrayArgument(value.FixedArguments[11], "uint8[]", (byte)19, (byte)20);
            AssertArrayArgument(value.FixedArguments[12], "char[]", 'A', 'B');
            AssertArrayArgument(value.FixedArguments[13], "bool[]", true, false);
            AssertArrayArgument(value.FixedArguments[14], "string[]", "alpha", null);
            AssertArrayArgument(value.FixedArguments[15], "string[]");
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

        [Fact]
        public void CustomAttribute_ObjectArrayWithNestedObjectWrapper_DecodesProperly()
        {
            // Regression test: object(object(...)) elements in object[] were previously encoded with
            // a TaggedObject (0x51) type code, which caused BadImageFormatException when decoding.
            // The fix ensures object(...) wrappers are unwrapped to the concrete inner type.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ObjArrAttribute extends [mscorlib]System.Attribute
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
                    .custom instance void ObjArrAttribute::.ctor(object[]) = {
                        object[2](object(bool(true)) object(int32(42)))
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var testType = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var attribute = reader.GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(testType)));
            CustomAttributeValue<string> value = attribute.DecodeValue(DocumentCompilerTestHelpers.Decoder);

            Assert.Single(value.FixedArguments);
            ImmutableArray<CustomAttributeTypedArgument<string>> elements =
                Assert.IsType<ImmutableArray<CustomAttributeTypedArgument<string>>>(value.FixedArguments[0].Value);
            Assert.Collection(
                elements,
                item =>
                {
                    Assert.Equal("bool", item.Type);
                    Assert.Equal(true, item.Value);
                },
                item =>
                {
                    Assert.Equal("int32", item.Type);
                    Assert.Equal(42, item.Value);
                });
        }

        [Fact]
        public void CustomAttribute_ObjectArrayWithNestedArrays_DecodesProperly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ObjArrAttribute extends [mscorlib]System.Attribute
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
                    .custom instance void ObjArrAttribute::.ctor(object[]) = {
                        object[2](int32[2](1 2) object(string[2]('alpha' nullref)))
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var testType = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            var attribute = reader.GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(testType)));
            CustomAttributeValue<string> value = attribute.DecodeValue(DocumentCompilerTestHelpers.Decoder);
            ImmutableArray<CustomAttributeTypedArgument<string>> elements =
                Assert.IsType<ImmutableArray<CustomAttributeTypedArgument<string>>>(
                    Assert.Single(value.FixedArguments).Value);

            Assert.Collection(
                elements,
                element =>
                {
                    Assert.Equal("int32[]", element.Type);
                    AssertArrayValue(element.Value, 1, 2);
                },
                element =>
                {
                    Assert.Equal("string[]", element.Type);
                    AssertArrayValue(element.Value, "alpha", null);
                });
        }

        [Theory]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Test extends [mscorlib]System.Object
            {
                .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(int32) = {
                    float32('a')
                }
            }
            """)]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Test extends [mscorlib]System.Object
            {
                .field public static literal float32 F = float32('a')
            }
            """)]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Test extends [mscorlib]System.Object
            {
                .method public static void M(float32 value) cil managed
                {
                    .param [1] = float32('a')
                    ret
                }
            }
            """)]
        public void MalformedScalarInitializer_ReportsDiagnosticsInsteadOfThrowing(string source)
        {
            ImmutableArray<Diagnostic> diagnostics =
                DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "Parser");
        }

        [Fact]
        public void MalformedNestedCustomAttributeSequence_DoesNotLeakFramesIntoNextDocument()
        {
            ImmutableArray<SourceText> documents =
            [
                new SourceText("""
                    .assembly extern mscorlib { }
                    .assembly test { }
                    .class public auto ansi Broken extends [mscorlib]System.Object
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(object[]) = {
                            object[2](type[1]([Discarded]Namespace.Type)
                    """, "broken.il"),
                new SourceText("""
                    .class public auto ansi Following extends [mscorlib]System.Object
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = { }
                    }
                    """, "following.il")
            ];

            DocumentCompiler compiler = new();
            var (diagnostics, result) = compiler.Compile(
                documents,
                _ => { Assert.Fail("Expected no includes"); return default; },
                _ => { Assert.Fail("Expected no resources"); return default; },
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "Parser");
            Assert.NotNull(result);

            BlobBuilder image = new();
            result!.Serialize(image);
            using PEReader pe = new(image.ToImmutableArray());
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinitionHandle followingHandle = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Following");

            Assert.DoesNotContain(
                reader.AssemblyReferences.Select(reader.GetAssemblyReference),
                reference => reader.GetString(reference.Name) == "Discarded");
            AssertCustomAttributeBlob(
                reader,
                Assert.Single(reader.GetCustomAttributes(followingHandle)));
        }

        private static string TypeWithAttribute(string attributeType, string constructor = ".ctor()", string value = "( 01 00 00 00 )") => $$"""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Test extends [mscorlib]System.Object
            {
                .custom instance void [mscorlib]{{attributeType}}::{{constructor}} = {{value}}
            }
            """;

        private static TypeDefinition GetTestType(MetadataReader reader) =>
            reader.GetTypeDefinition(reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test"));

        public static TheoryData<string, TypeAttributes> PseudoAttributeTypeFlagData => new()
        {
            { "System.Runtime.InteropServices.ComImportAttribute", TypeAttributes.Import },
            { "System.SerializableAttribute", TypeAttributes.Serializable },
            { "System.Runtime.CompilerServices.SpecialNameAttribute", TypeAttributes.SpecialName },
            { "System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeImportAttribute", TypeAttributes.WindowsRuntime },
        };

        [Theory]
        [MemberData(nameof(PseudoAttributeTypeFlagData))]
        public void PseudoCustomAttribute_OnType_LowersToFlagAndDropsAttribute(string attributeType, TypeAttributes expected)
        {
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(TypeWithAttribute(attributeType), new Options());
            var reader = pe.GetMetadataReader();
            var testType = GetTestType(reader);

            Assert.Equal(expected, testType.Attributes & expected);
            Assert.Empty(testType.GetCustomAttributes());
        }

        [Theory]
        [InlineData("System.Runtime.InteropServices.GuidAttribute", ".ctor(string)", "( 01 00 24 30 31 32 33 34 35 36 37 2D 30 31 32 33 2D 30 31 32 33 2D 30 31 32 33 2D 30 30 31 31 32 32 33 33 34 34 35 35 00 00 )")]
        [InlineData("System.Runtime.InteropServices.InterfaceTypeAttribute", ".ctor(int16)", "( 01 00 01 00 00 00 )")]
        [InlineData("System.Runtime.InteropServices.InterfaceTypeAttribute", ".ctor(int16)", "( 01 00 03 00 00 00 )")]
        [InlineData("System.Runtime.InteropServices.ClassInterfaceAttribute", ".ctor(int16)", "( 01 00 02 00 00 00 )")]
        public void PseudoCustomAttribute_ValidateOnly_KeepsAttribute(string attributeType, string constructor, string value)
        {
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                TypeWithAttribute(attributeType, constructor, value),
                new Options());
            var reader = pe.GetMetadataReader();

            Assert.Single(GetTestType(reader).GetCustomAttributes());
        }

        [Theory]
        [InlineData("System.Runtime.InteropServices.GuidAttribute", ".ctor(string)", "( 01 00 04 6E 6F 70 65 00 00 )", DiagnosticIds.PseudoCustomAttributeInvalidGuid)]
        [InlineData("System.Runtime.InteropServices.InterfaceTypeAttribute", ".ctor(int16)", "( 01 00 07 00 00 00 )", DiagnosticIds.PseudoCustomAttributeInvalidValue)]
        [InlineData("System.Runtime.InteropServices.ClassInterfaceAttribute", ".ctor(int16)", "( 01 00 09 00 00 00 )", DiagnosticIds.PseudoCustomAttributeInvalidValue)]
        [InlineData("System.SerializableAttribute", ".ctor()", "( 01 00 00 00 )", DiagnosticIds.PseudoCustomAttributeInvalidTarget)]
        public void PseudoCustomAttribute_InvalidValueOrTarget_ReportsDiagnostic(
            string attributeType,
            string constructor,
            string value,
            string expectedDiagnosticId)
        {
            // SerializableAttribute is only valid on a type, so applying it to a method is an invalid target.
            string source = expectedDiagnosticId == DiagnosticIds.PseudoCustomAttributeInvalidTarget
                ? $$"""
                    .assembly extern mscorlib { }
                    .assembly test { }
                    .class public auto ansi Test extends [mscorlib]System.Object
                    {
                        .method public static void M() cil managed
                        {
                            .custom instance void [mscorlib]{{attributeType}}::{{constructor}} = {{value}}
                            ret
                        }
                    }
                    """
                : TypeWithAttribute(attributeType, constructor, value);

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(expectedDiagnosticId, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        }

        [Theory]
        [InlineData("( 01 00 00 00 00 00 00 00 )", TypeAttributes.SequentialLayout)]
        [InlineData("( 01 00 01 00 00 00 00 00 )", TypeAttributes.ExtendedLayout)]
        [InlineData("( 01 00 02 00 00 00 00 00 )", TypeAttributes.ExplicitLayout)]
        [InlineData("( 01 00 03 00 00 00 00 00 )", TypeAttributes.AutoLayout)]
        public void PseudoCustomAttribute_StructLayout_SetsLayoutMask(string value, TypeAttributes expected)
        {
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                TypeWithAttribute("System.Runtime.InteropServices.StructLayoutAttribute", ".ctor(int32)", value),
                new Options());
            var reader = pe.GetMetadataReader();
            var testType = GetTestType(reader);

            Assert.Equal(expected, testType.Attributes & TypeAttributes.LayoutMask);
            Assert.Empty(testType.GetCustomAttributes());
        }

        [Fact]
        public void PseudoCustomAttribute_StructLayout_NamedArgumentsSetClassLayoutAndCharSet()
        {
            // LayoutKind.Explicit with Pack = 4, Size = 16 and CharSet = Unicode (3).
            string value = "( 01 00 02 00 00 00 03 00 53 08 04 50 61 63 6B 04 00 00 00 "
                + "53 08 04 53 69 7A 65 10 00 00 00 "
                + "53 55 26 53 79 73 74 65 6D 2E 52 75 6E 74 69 6D 65 2E 49 6E 74 65 72 6F 70 53 65 72 76 69 63 65 73 2E 43 68 61 72 53 65 74 "
                + "07 43 68 61 72 53 65 74 03 00 00 00 )";

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                TypeWithAttribute("System.Runtime.InteropServices.StructLayoutAttribute", ".ctor(int32)", value),
                new Options());
            var reader = pe.GetMetadataReader();
            var testType = GetTestType(reader);

            Assert.Equal(TypeAttributes.ExplicitLayout, testType.Attributes & TypeAttributes.LayoutMask);
            Assert.Equal(TypeAttributes.UnicodeClass, testType.Attributes & TypeAttributes.StringFormatMask);

            var layout = testType.GetLayout();
            Assert.Equal(4, layout.PackingSize);
            Assert.Equal(16, layout.Size);
            Assert.Empty(testType.GetCustomAttributes());
        }

        [Fact]
        public void PseudoCustomAttribute_DynamicSecurityMethod_SetsRequireSecObjectAndIsDropped()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .custom instance void [mscorlib]System.Security.DynamicSecurityMethodAttribute::.ctor() = ( 01 00 00 00 )
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(definition => reader.GetString(definition.Name) == "M");

            Assert.Equal(MethodAttributes.RequireSecObject, method.Attributes & MethodAttributes.RequireSecObject);
            Assert.Empty(method.GetCustomAttributes());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PseudoCustomAttribute_SuppressUnmanagedCodeSecurity_SetsHasSecurityAndIsKept(bool onType)
        {
            string attribute = ".custom instance void [mscorlib]System.Security.SuppressUnmanagedCodeSecurityAttribute::.ctor() = ( 01 00 00 00 )";
            string source = onType
                ? $$"""
                    .assembly extern mscorlib { }
                    .assembly test { }
                    .class public auto ansi Test extends [mscorlib]System.Object
                    {
                        {{attribute}}
                    }
                    """
                : $$"""
                    .assembly extern mscorlib { }
                    .assembly test { }
                    .class public auto ansi Test extends [mscorlib]System.Object
                    {
                        .method public static void M() cil managed
                        {
                            {{attribute}}
                            ret
                        }
                    }
                    """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            if (onType)
            {
                var testType = GetTestType(reader);
                Assert.Equal(TypeAttributes.HasSecurity, testType.Attributes & TypeAttributes.HasSecurity);
                Assert.Single(testType.GetCustomAttributes());
            }
            else
            {
                var method = reader.MethodDefinitions
                    .Select(reader.GetMethodDefinition)
                    .Single(definition => reader.GetString(definition.Name) == "M");
                Assert.Equal(MethodAttributes.HasSecurity, method.Attributes & MethodAttributes.HasSecurity);
                Assert.Single(method.GetCustomAttributes());
            }
        }

        [Fact]
        public void PseudoCustomAttribute_UnknownNamedArgument_ReportsDiagnostic()
        {
            // StructLayoutAttribute with a named field named "Bogus".
            string value = "( 01 00 00 00 00 00 01 00 53 08 05 42 6F 67 75 73 01 00 00 00 )";

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(
                TypeWithAttribute("System.Runtime.InteropServices.StructLayoutAttribute", ".ctor(int32)", value),
                new Options());

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.PseudoCustomAttributeUnknownArgument, diagnostic.Id);
        }

        [Fact]
        public void PseudoCustomAttribute_StructLayout_ExplicitPackAndSizeDirectivesWin()
        {
            // LayoutKind.Sequential with Pack = 16, on a type that also declares .pack and .size.
            // The native assembler emits the ClassLayout row for the explicit directives in a later
            // phase than the one that applies the attribute, so the directives take precedence.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public sequential ansi sealed Test extends [mscorlib]System.ValueType
                {
                    .custom instance void [mscorlib]System.Runtime.InteropServices.StructLayoutAttribute::.ctor(int32) = ( 01 00 00 00 00 00 01 00 53 08 04 50 61 63 6B 10 00 00 00 )
                    .pack 4
                    .size 32
                    .field public int32 Value
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var layout = GetTestType(reader).GetLayout();

            Assert.Equal(4, layout.PackingSize);
            Assert.Equal(32, layout.Size);
        }

        [Fact]
        public void PseudoCustomAttribute_NonPseudoAttribute_IsStillEmitted()
        {
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                TypeWithAttribute("System.ObsoleteAttribute"),
                new Options());
            var reader = pe.GetMetadataReader();

            Assert.Single(GetTestType(reader).GetCustomAttributes());
        }

        [Fact]
        public void PseudoCustomAttribute_LoweredAttribute_DoesNotShiftRemainingAttributeOwners()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi First extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 00 00 )
                    .custom instance void [mscorlib]System.SerializableAttribute::.ctor() = ( 01 00 00 00 )
                }
                .class public auto ansi Second extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 00 00 )
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            foreach (string typeName in new[] { "First", "Second" })
            {
                var typeDefinition = reader.GetTypeDefinition(reader.TypeDefinitions
                    .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == typeName));
                var attribute = reader.GetCustomAttribute(Assert.Single(typeDefinition.GetCustomAttributes()));
                var constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                var declaringType = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                Assert.Equal("ObsoleteAttribute", reader.GetString(declaringType.Name));
            }

            var first = reader.GetTypeDefinition(reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "First"));
            Assert.Equal(TypeAttributes.Serializable, first.Attributes & TypeAttributes.Serializable);
        }

        [Fact]
        public void MethodBodyCustomAttributes_PreserveOwnerAndOrder()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M<T>(int32 value) cil managed
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                        .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (01 00 00 00)
                        .param [1]
                            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (01 00 00 00)
                            .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                        .param type T
                            .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (01 00 00 00)
                        .param constraint T, [mscorlib]System.IDisposable
                            .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (01 00 00 00)
                            .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            MethodDefinitionHandle methodHandle = Assert.Single(reader.MethodDefinitions);
            var method = reader.GetMethodDefinition(methodHandle);
            ParameterHandle parameterHandle = method.GetParameters()
                .Single(handle => reader.GetParameter(handle).SequenceNumber == 1);
            GenericParameterHandle genericParameterHandle = Assert.Single(method.GetGenericParameters());
            GenericParameterConstraintHandle constraintHandle =
                Assert.Single(reader.GetGenericParameter(genericParameterHandle).GetConstraints());

            Assert.Equal(
                ["ObsoleteAttribute", "DebuggerHiddenAttribute"],
                GetAttributeTypeNames(reader, reader.GetCustomAttributes(methodHandle)));
            Assert.Equal(
                ["DebuggerHiddenAttribute", "ObsoleteAttribute"],
                GetAttributeTypeNames(reader, reader.GetCustomAttributes(parameterHandle)));
            Assert.Equal(
                ["ObsoleteAttribute", "DebuggerHiddenAttribute"],
                GetAttributeTypeNames(reader, reader.GetCustomAttributes(genericParameterHandle)));
            Assert.Equal(
                ["DebuggerHiddenAttribute", "ObsoleteAttribute"],
                GetAttributeTypeNames(reader, reader.GetCustomAttributes(constraintHandle)));

            static string[] GetAttributeTypeNames(
                MetadataReader reader,
                CustomAttributeHandleCollection attributes)
                => attributes
                    .Select(reader.GetCustomAttribute)
                    .Select(attribute => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor))
                    .Select(constructor => reader.GetTypeReference((TypeReferenceHandle)constructor.Parent))
                    .Select(type => reader.GetString(type.Name))
                    .ToArray();
        }

        [Fact]
        public void PseudoCustomAttribute_ZeroArgDescriptor_MalformedBlobSkipped()
        {
            // SerializableAttribute has zero fixed and zero named-arg descriptors. The native
            // emitter does not parse the blob at all for such attributes, so even a completely
            // malformed blob (bad prolog or arbitrary bytes) must produce no diagnostics.
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                TypeWithAttribute("System.SerializableAttribute", ".ctor()", "( FF FF DE AD BE EF )"),
                new Options());
            var reader = pe.GetMetadataReader();
            var testType = GetTestType(reader);

            Assert.Equal(TypeAttributes.Serializable, testType.Attributes & TypeAttributes.Serializable);
            Assert.Empty(testType.GetCustomAttributes());
        }

        [Fact]
        public void PseudoCustomAttribute_FixedArgsNoNamedDescriptors_EverettBlobWithNoNamedCountAccepted()
        {
            // GuidAttribute has one fixed string arg and no named-arg descriptors. When the blob
            // ends immediately after the fixed argument with no 2-byte named-arg count, the native
            // emitter accepts it as Everett-compatible behavior. Compilation must succeed and the
            // attribute row must be retained (GuidAttribute has KeepAttribute = true).
            // Blob: 01 00 (prolog) 24 (SerString length = 36) + 36 UTF-8 bytes of GUID -- no trailing 00 00.
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                TypeWithAttribute(
                    "System.Runtime.InteropServices.GuidAttribute",
                    ".ctor(string)",
                    "( 01 00 24 30 31 32 33 34 35 36 37 2D 30 31 32 33 2D 30 31 32 33 2D 30 31 32 33 2D 30 30 31 31 32 32 33 33 34 34 35 35 )"),
                new Options());
            var reader = pe.GetMetadataReader();

            Assert.Single(GetTestType(reader).GetCustomAttributes());
        }

        [Fact]
        public void PseudoCustomAttribute_NamedArgCount0x8000_TreatedAsSignedNegativeAndSkipped()
        {
            // The named-argument count is stored and compared as a signed INT16 in the native
            // emitter. A count of 0x8000 (-32768 when sign-extended) causes the loop to execute
            // zero times, so no named arguments are consumed. The fixed layout effect is applied
            // and the CA row is dropped.
            // Blob: 01 00 (prolog) 02 00 (I2 = LayoutKind.Explicit) 00 80 (count = 0x8000 LE).
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                TypeWithAttribute(
                    "System.Runtime.InteropServices.StructLayoutAttribute",
                    ".ctor(int16)",
                    "( 01 00 02 00 00 80 )"),
                new Options());
            var reader = pe.GetMetadataReader();
            var testType = GetTestType(reader);

            Assert.Equal(TypeAttributes.ExplicitLayout, testType.Attributes & TypeAttributes.LayoutMask);
            Assert.Empty(testType.GetCustomAttributes());
        }

        [Fact]
        public void PseudoCustomAttribute_TruncatedFixedArg_ReportsInvalidBlobDiagnostic()
        {
            // A blob that is too short to supply all fixed arguments must produce
            // PseudoCustomAttributeInvalidBlob and no output image in normal mode.
            // Blob: 01 00 (valid prolog) 01 (1 byte; I4 requires 4 bytes) -- truncated.
            var compiler = new DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(
                    TypeWithAttribute(
                        "System.Runtime.InteropServices.StructLayoutAttribute",
                        ".ctor(int32)",
                        "( 01 00 01 )"),
                    "test.il"),
                _ => { Assert.Fail("Expected no includes"); return default; },
                _ => { Assert.Fail("Expected no resources"); return default; },
                new Options());

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.PseudoCustomAttributeInvalidBlob, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Null(result);
        }
    }
}
