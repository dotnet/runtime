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
    public class FieldTests
    {
        [Fact]
        public void TrailingCustomAttribute_AttachesToField()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test
                {
                    .field public static int32 Value
                    .custom instance void [mscorlib]System.ThreadStaticAttribute::.ctor() = (01 00 00 00)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var type = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));

            Assert.Empty(type.GetCustomAttributes());
            Assert.Single(field.GetCustomAttributes());
        }

        [Fact]
        public void TrailingFieldCustomAttribute_DoesNotLeakAcrossClasses()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi A
                {
                    .field public static int32 Value
                }
                .class public auto ansi B
                {
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var typeB = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "B");

            Assert.Empty(field.GetCustomAttributes());
            Assert.Single(typeB.GetCustomAttributes());
        }

        [Fact]
        public void GlobalFieldTrailingCustomAttribute_AttachesToField()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .field public static int32 Value
                .custom instance void [mscorlib]System.ThreadStaticAttribute::.ctor() = (01 00 00 00)
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));

            Assert.Empty(reader.GetModuleDefinition().GetCustomAttributes());
            Assert.Single(field.GetCustomAttributes());
        }

        [Fact]
        public void FieldLayout_ExplicitOffset()
        {
            // Test explicit field offset with [n] syntax
            string source = """
                .class public explicit ansi sealed beforefieldinit UnionStruct
                    extends [System.Runtime]System.ValueType
                {
                    .field [0] public int32 intValue
                    .field [0] public float32 floatValue
                    .field [0] public float64 doubleValue
                }
                .assembly extern System.Runtime { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeHandle = reader.TypeDefinitions
                .First(h => reader.GetString(reader.GetTypeDefinition(h).Name) == "UnionStruct");

            var typeDef = reader.GetTypeDefinition(typeHandle);

            // Verify ExplicitLayout is set (this was a regression bug - EXPLICIT token wasn't being parsed)
            Assert.True(typeDef.Attributes.HasFlag(System.Reflection.TypeAttributes.ExplicitLayout),
                $"Expected ExplicitLayout, got {typeDef.Attributes} (0x{(int)typeDef.Attributes:X8})");

            var fields = typeDef.GetFields()
                .Select(reader.GetFieldDefinition).ToArray();
            Assert.Equal(3, fields.Length);

            // All fields should have offset 0, creating a union
            Assert.Equal(0, fields[0].GetOffset());
            Assert.Equal(0, fields[1].GetOffset());
            Assert.Equal(0, fields[2].GetOffset());
        }

        [Fact]
        public void FieldAttributes_EmitExpectedFlagsAndNullConstant()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field family initonly int32 FamilyInitOnly
                    .field assembly int32 AssemblyField
                    .field famandassem int32 FamAndAssemField
                    .field famorassem int32 FamOrAssemField
                    .field privatescope int32 PrivateScopeField
                    .field public notserialized int32 NotSerializedField
                    .field flags(0x36) int32 FlaggedField
                    .field public volatile int32 VolatileField
                    .field public static literal string NullString = nullref
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var fields = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .ToDictionary(field => reader.GetString(field.Name));

            Assert.Equal(FieldAttributes.Family, fields["FamilyInitOnly"].Attributes & FieldAttributes.FieldAccessMask);
            Assert.True(fields["FamilyInitOnly"].Attributes.HasFlag(FieldAttributes.InitOnly));
            Assert.Equal(FieldAttributes.Assembly, fields["AssemblyField"].Attributes & FieldAttributes.FieldAccessMask);
            Assert.Equal(FieldAttributes.FamANDAssem, fields["FamAndAssemField"].Attributes & FieldAttributes.FieldAccessMask);
            Assert.Equal(FieldAttributes.FamORAssem, fields["FamOrAssemField"].Attributes & FieldAttributes.FieldAccessMask);
            Assert.Equal(FieldAttributes.PrivateScope, fields["PrivateScopeField"].Attributes & FieldAttributes.FieldAccessMask);
#pragma warning disable SYSLIB0050
            Assert.True(fields["NotSerializedField"].Attributes.HasFlag(FieldAttributes.NotSerialized));
#pragma warning restore SYSLIB0050
            Assert.Equal((FieldAttributes)0x36, fields["FlaggedField"].Attributes);
            Assert.Equal(FieldAttributes.Public, fields["VolatileField"].Attributes & FieldAttributes.FieldAccessMask);

            var nullConstant = reader.GetConstant(fields["NullString"].GetDefaultValue());
            Assert.Equal(ConstantTypeCode.NullReference, nullConstant.TypeCode);
        }


        [Fact]
        public void FieldRVA_MultipleDataSections()
        {
            // Test multiple .data declarations with different data types
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .data IntData = int32(0x12345678)
                .data ByteData = bytearray (AA BB CC DD EE FF)
                .data FloatData = float32(3.14159)

                .class public explicit ansi sealed beforefieldinit DataHolder extends [mscorlib]System.ValueType
                {
                    .size 16
                    .field [0] public static int32 IntField at IntData
                    .field [4] public static int32 ByteField at ByteData
                    .field [8] public static float32 FloatField at FloatData
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var testType = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(t => reader.GetString(t.Name) == "DataHolder");

            var fields = testType.GetFields()
                .Select(reader.GetFieldDefinition)
                .ToDictionary(f => reader.GetString(f.Name));

            // Verify IntField RVA and data (little-endian: 0x12345678 = 78 56 34 12)
            int intRva = fields["IntField"].GetRelativeVirtualAddress();
            Assert.NotEqual(0, intRva);

            // Verify ByteField RVA
            int byteRva = fields["ByteField"].GetRelativeVirtualAddress();
            Assert.NotEqual(0, byteRva);

            // Verify FloatField has an RVA
            int floatRva = fields["FloatField"].GetRelativeVirtualAddress();
            Assert.NotEqual(0, floatRva);

            // Each field should point to different data locations
            Assert.NotEqual(intRva, byteRva);
            Assert.NotEqual(intRva, floatRva);
            Assert.NotEqual(byteRva, floatRva);
        }


        [Fact]
        public void FieldInit_ByteArray_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .field static int32 field1 at 0
                }
                .data data1 = bytearray (00 01 02 03)
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void FieldConstant_WithCharValue_UsesBlobBuilderExtensions()
        {
            // Test field with char constant (exercises BlobBuilderExtensions.WriteSerializedValue<char>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal char CharField = char(0x0041)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "CharField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Char, constant.TypeCode);
        }


        [Fact]
        public void FieldConstant_WithDoubleValue_UsesBlobBuilderExtensions()
        {
            // Test field with double constant (exercises BlobBuilderExtensions.WriteSerializedValue<double>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal float64 DoubleField = float64(3.14159265358979)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "DoubleField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Double, constant.TypeCode);
        }


        [Fact]
        public void FieldConstant_WithInt16Value_UsesBlobBuilderExtensions()
        {
            // Test field with int16 constant (exercises BlobBuilderExtensions.WriteSerializedValue<short>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal int16 ShortField = int16(12345)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "ShortField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Int16, constant.TypeCode);
        }


        [Fact]
        public void FieldConstant_WithInt64Value_UsesBlobBuilderExtensions()
        {
            // Test field with int64 constant (exercises BlobBuilderExtensions.WriteSerializedValue<long>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal int64 LongField = int64(9223372036854775807)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "LongField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Int64, constant.TypeCode);
        }


        [Fact]
        public void FieldConstant_WithInt8Value_UsesBlobBuilderExtensions()
        {
            // Test field with int8 constant (exercises BlobBuilderExtensions.WriteSerializedValue<sbyte>)
            // Use hex 0xD6 which is -42 in signed byte representation
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal int8 SByteField = int8(42)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "SByteField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.SByte, constant.TypeCode);
        }


        [Fact]
        public void FieldConstant_WithFloat32Value_UsesBlobBuilderExtensions()
        {
            // Test field with float32 constant (exercises BlobBuilderExtensions.WriteSerializedValue<float>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal float32 FloatField = float32(3.14)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "FloatField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Single, constant.TypeCode);
        }


        [Fact]
        public void FieldConstant_WithUInt16Value_UsesBlobBuilderExtensions()
        {
            // Test field with uint16 constant (exercises BlobBuilderExtensions.WriteSerializedValue<ushort>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal uint16 UShortField = uint16(65535)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "UShortField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.UInt16, constant.TypeCode);
        }


        [Fact]
        public void FieldConstant_WithUInt32Value_UsesBlobBuilderExtensions()
        {
            // Test field with uint32 constant (exercises BlobBuilderExtensions.WriteSerializedValue<uint>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal uint32 UIntField = uint32(4294967295)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "UIntField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.UInt32, constant.TypeCode);
        }


        [Fact]
        public void FieldConstant_WithUInt64Value_UsesBlobBuilderExtensions()
        {
            // Test field with uint64 constant (exercises BlobBuilderExtensions.WriteSerializedValue<ulong>)
            // Use smaller value that fits in int64 range
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal uint64 ULongField = uint64(9223372036854775807)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "ULongField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.UInt64, constant.TypeCode);
        }


        [Fact]
        public void FieldConstant_WithUInt8Value_UsesBlobBuilderExtensions()
        {
            // Test field with uint8 constant (exercises BlobBuilderExtensions.WriteSerializedValue<byte>)
            string source = """
                .assembly test { }
                .assembly extern System.Runtime { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .field public static literal uint8 ByteField = uint8(255)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var fieldDef = reader.FieldDefinitions
                .Select(h => reader.GetFieldDefinition(h))
                .First(f => reader.GetString(f.Name) == "ByteField");

            var constant = reader.GetConstant(fieldDef.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.Byte, constant.TypeCode);
        }


        [Fact]
        public void NativeInt_FieldType_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .field public static native int f1
                    .field public static native uint f2
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var fields = typeDef.GetFields().ToArray();
            Assert.Equal(2, fields.Length);
            // native int → IntPtr (SignatureTypeCode 0x18)
            var sig1 = reader.GetBlobReader(reader.GetFieldDefinition(fields[0]).Signature);
            Assert.Equal(0x06, sig1.ReadByte()); // FIELD calling convention
            Assert.Equal(0x18, sig1.ReadByte()); // ELEMENT_TYPE_I (IntPtr)
            // native uint → UIntPtr (SignatureTypeCode 0x19)
            var sig2 = reader.GetBlobReader(reader.GetFieldDefinition(fields[1]).Signature);
            Assert.Equal(0x06, sig2.ReadByte());
            Assert.Equal(0x19, sig2.ReadByte()); // ELEMENT_TYPE_U (UIntPtr)
        }


        [Fact]
        public void VolatileFieldAttribute_AcceptedAsModifier()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .field public static volatile int32 myField
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void SelfTypeReference_InField()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit MyClass extends [System.Runtime]System.Object
                {
                    .field public static class MyClass instance
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            Assert.Equal(2, reader.TypeDefinitions.Count);
        }


        [Fact]
        public void FieldRtSpecialName_Preserved()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi sealed TestEnum extends [mscorlib]System.Enum
                {
                    .field public specialname rtspecialname uint8 value__
                    .field public static literal valuetype TestEnum A = uint8(0x00)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal("value__", reader.GetString(field.Name));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.RTSpecialName));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.SpecialName));
        }


        [Fact]
        public void LocalFieldAccess_ResolvesToFieldDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static int32 myField
                    .method public static int32 GetField() cil managed
                    {
                        ldsfld int32 MyClass::myField
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // No MemberRef rows should exist for the local field access
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));

            // Verify the ldsfld instruction references a FieldDef token
            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "GetField");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var ilReader = body.GetILReader();
            Assert.Equal(ILOpCode.Ldsfld, (ILOpCode)ilReader.ReadByte());
            int token = ilReader.ReadInt32();
            Assert.Equal(0x04, (token >> 24) & 0xFF); // FieldDef table (0x04)
        }


        [Fact]
        public void LocalInstanceFieldAccess_ResolvesToFieldDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public int32 value
                    .method public instance int32 GetValue() cil managed
                    {
                        ldarg.0
                        ldfld int32 MyClass::value
                        ret
                    }
                    .method public instance void SetValue(int32 v) cil managed
                    {
                        ldarg.0
                        ldarg.1
                        stfld int32 MyClass::value
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }


        [Fact]
        public void CrossTypeLocalFieldAccess_ResolvesToFieldDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit ClassA extends [mscorlib]System.Object
                {
                    .field public static int32 SharedValue
                }
                .class public auto ansi beforefieldinit ClassB extends [mscorlib]System.Object
                {
                    .method public static int32 GetShared() cil managed
                    {
                        ldsfld int32 ClassA::SharedValue
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }


        [Fact]
        public void FieldLiteralConstant_SetsHasDefaultFlag()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ByteEnum extends [mscorlib]System.Enum
                {
                    .field public specialname rtspecialname uint8 value__
                    .field public static literal valuetype ByteEnum A = uint8(0)
                    .field public static literal valuetype ByteEnum B = uint8(1)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(2, reader.GetTableRowCount(TableIndex.Constant));

            // Fields A and B (handles 2 and 3, after value__)
            var fieldA = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(2));
            var fieldB = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(3));

            Assert.True(fieldA.Attributes.HasFlag(FieldAttributes.HasDefault));
            Assert.True(fieldB.Attributes.HasFlag(FieldAttributes.HasDefault));

            // Verify constant values
            var constA = reader.GetConstant(fieldA.GetDefaultValue());
            var constB = reader.GetConstant(fieldB.GetDefaultValue());

            Assert.Equal(ConstantTypeCode.Byte, constA.TypeCode);
            Assert.Equal(ConstantTypeCode.Byte, constB.TypeCode);

            Assert.Equal(0, reader.GetBlobReader(constA.Value).ReadByte());
            Assert.Equal(1, reader.GetBlobReader(constB.Value).ReadByte());
        }


        [Theory]
        [InlineData("int32", "int32(42)", ConstantTypeCode.Int32)]
        [InlineData("int64", "int64(100)", ConstantTypeCode.Int64)]
        [InlineData("float32", "float32(3.14)", ConstantTypeCode.Single)]
        [InlineData("bool", "bool(true)", ConstantTypeCode.Boolean)]
        public void FieldLiteralConstant_VariousTypes(string fieldType, string initExpr, ConstantTypeCode expectedTypeCode)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static literal {{fieldType}} myConst = {{initExpr}}
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.HasDefault));

            var constant = reader.GetConstant(field.GetDefaultValue());
            Assert.Equal(expectedTypeCode, constant.TypeCode);
        }


        [Fact]
        public void FieldLiteralString_SetsHasDefaultFlag()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static literal string myStr = "hello"
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.HasDefault));

            var constant = reader.GetConstant(field.GetDefaultValue());
            Assert.Equal(ConstantTypeCode.String, constant.TypeCode);
        }


        [Fact]
        public void FieldRtSpecialName_ImplicitlyAddsSpecialName()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed ByteEnum extends [mscorlib]System.Enum
                {
                    .field public rtspecialname uint8 value__
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.SpecialName));
            Assert.True(field.Attributes.HasFlag(FieldAttributes.RTSpecialName));
        }


        [Fact]
        public void ModReq_InFieldSignature_PreservedInRewrittenBlob()
        {
            // A field with modreq should preserve the modifier in the signature
            // after the TypeRef→TypeDef signature rewriting pass.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) volatileField
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), 0x1F (CMOD_REQD), <coded index for IsVolatile>, 0x08 (I4)
            Assert.Equal(0x06, sigBytes[0]); // FIELD header
            Assert.Equal((byte)SignatureTypeCode.RequiredModifier, sigBytes[1]); // CMOD_REQD
            // The last byte should be the underlying type (int32 = 0x08)
            Assert.Equal(0x08, sigBytes[^1]);
        }


        [Fact]
        public void FieldRefInIL_BackpatchedAfterResolution()
        {
            // When a field instruction (ldfld/ldsfld/...) references a field of a local type via
            // [self-assembly]Type::Field, the field MemberRef resolves to a local FieldDef and the
            // IL token is backpatched to that FieldDef. This exercises the instr_field fieldRef path.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed MyStruct extends [mscorlib]System.ValueType
                {
                    .field public int32 x
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 Load(valuetype [test]MyStruct s) cil managed
                    {
                        ldarga.s s
                        ldfld int32 [test]MyStruct::x
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The field resolves to a local FieldDef, so no MemberRef row is emitted and the ldfld
            // IL token is backpatched to that FieldDef.
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Load", ILOpcode.ldfld);
            DocumentCompilerTestHelpers.AssertFieldDefToken(reader, token, "x");
        }


        [Fact]
        public void MultiDimArrayField_PreservedAfterSignatureRewrite()
        {
            // Multi-dimensional array types in field signatures must survive rewriting.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public int32[0...] arr1d
                    .field public int32[0...,0...] arr2d
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // arr1d: FIELD (0x06), ARRAY (0x14), I4 (0x08), rank=1, ...
            var field1 = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sig1 = reader.GetBlobBytes(field1.Signature);
            Assert.Equal(0x06, sig1[0]); // FIELD
            Assert.Equal(0x14, sig1[1]); // ELEMENT_TYPE_ARRAY
            Assert.Equal(0x08, sig1[2]); // ELEMENT_TYPE_I4
            Assert.Equal(0x01, sig1[3]); // rank = 1

            // arr2d: FIELD (0x06), ARRAY (0x14), I4 (0x08), rank=2, ...
            var field2 = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(2));
            var sig2 = reader.GetBlobBytes(field2.Signature);
            Assert.Equal(0x06, sig2[0]); // FIELD
            Assert.Equal(0x14, sig2[1]); // ELEMENT_TYPE_ARRAY
            Assert.Equal(0x08, sig2[2]); // ELEMENT_TYPE_I4
            Assert.Equal(0x02, sig2[3]); // rank = 2
        }

    }
}
