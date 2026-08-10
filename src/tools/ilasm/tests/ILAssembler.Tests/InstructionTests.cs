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
    public class InstructionTests
    {

        [Fact]
        public void Diagnostic_LabelNotFound()
        {
            // Reference an undefined label in a branch instruction
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        br UndefinedLabel
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LabelNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void DataLabelReference_FixedUpCorrectly()
        {
            // Test that .data with a reference to another label (&Label) is patched with the correct RVA
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data TargetData = int32(0x12345678)
                .data PointerData = &TargetData
                .class public explicit ansi sealed beforefieldinit DataHolder extends [mscorlib]System.ValueType
                {
                    .size 8
                    .field [0] public static int32 Target at TargetData
                    .field [4] public static int32 Pointer at PointerData
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

            // Both fields should have RVAs
            int targetRva = fields["Target"].GetRelativeVirtualAddress();
            int pointerRva = fields["Pointer"].GetRelativeVirtualAddress();
            Assert.NotEqual(0, targetRva);
            Assert.NotEqual(0, pointerRva);

            // The pointer field should contain the RVA of the target data
            // Read the actual data from the PE at the pointer location
            var pointerSection = pe.GetSectionData(pointerRva);
            int storedRva = BinaryPrimitives.ReadInt32LittleEndian(pointerSection.GetContent().AsSpan(0, 4));

            // The stored RVA should equal the target's RVA
            Assert.Equal(targetRva, storedRva);

            // Verify the target data contains the expected value
            var targetSection = pe.GetSectionData(targetRva);
            int targetValue = BinaryPrimitives.ReadInt32LittleEndian(targetSection.GetContent().AsSpan(0, 4));
            Assert.Equal(0x12345678, targetValue);
        }


        [Fact]
        public void DataLabelReference_MultipleReferences_AllFixedUp()
        {
            // Test multiple references to the same label
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data Target = int32(42)
                .data Ptr1 = &Target
                .data Ptr2 = &Target
                .class public explicit ansi sealed beforefieldinit DataHolder extends [mscorlib]System.ValueType
                {
                    .size 12
                    .field [0] public static int32 TargetField at Target
                    .field [4] public static int32 Ptr1Field at Ptr1
                    .field [8] public static int32 Ptr2Field at Ptr2
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

            int targetRva = fields["TargetField"].GetRelativeVirtualAddress();
            int ptr1Rva = fields["Ptr1Field"].GetRelativeVirtualAddress();
            int ptr2Rva = fields["Ptr2Field"].GetRelativeVirtualAddress();

            // Read both pointer values
            int storedRva1 = BinaryPrimitives.ReadInt32LittleEndian(pe.GetSectionData(ptr1Rva).GetContent().AsSpan(0, 4));
            int storedRva2 = BinaryPrimitives.ReadInt32LittleEndian(pe.GetSectionData(ptr2Rva).GetContent().AsSpan(0, 4));

            // Both should point to the target
            Assert.Equal(targetRva, storedRva1);
            Assert.Equal(targetRva, storedRva2);
        }


        [Fact]
        public void HexLabelName_NotConfusedWithHexByte()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        br AA
                        nop
                    AA: ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void PrefixInstruction_Volatile_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .field public static int32 myField
                    .method public static void M() cil managed
                    {
                        volatile.
                        ldsfld int32 Test::myField
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void PrefixInstruction_Tail_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static int32 M() cil managed
                    {
                        ldc.i4.0
                        tail.
                        call int32 Test::M()
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void LdelemU8_InstructionParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M(unsigned int64[] arr) cil managed
                    {
                        ldarg.0
                        ldc.i4.0
                        ldelem.u8
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void UnusedInstruction_ParsedCorrectly()
        {
            string source = """
                .assembly test { }
                .method public static void F() cil managed
                {
                    IL_0000: unused
                    IL_0001: ret
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal([0xFE, 0x22, 0x2A], il);
        }

        [Fact]
        public void Ldtoken_NumericTokenEmittedCorrectly()
        {
            string source = """
                .assembly test { }
                .method public static void F() cil managed
                {
                    ldtoken 0
                    ret
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal([0xD0, 0x00, 0x00, 0x00, 0x00, 0x2A], il);
        }

        [Fact]
        public void Calli_WritesOpcodeAndSignatureToken()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        calli void()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal(0x29, il[0]);
            var signatureHandle = (StandaloneSignatureHandle)MetadataTokens.EntityHandle(BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(1)));
            Assert.Equal(0x2A, il[5]);

            MethodSignature<PrimitiveTypeCode> signature = DecodeCalliSignature(reader, signatureHandle);
            Assert.Equal(SignatureCallingConvention.Default, signature.Header.CallingConvention);
            Assert.False(signature.Header.IsInstance);
            Assert.False(signature.Header.HasExplicitThis);
            Assert.Equal(PrimitiveTypeCode.Void, signature.ReturnType);
            Assert.Empty(signature.ParameterTypes);
        }

        [Theory]
        [InlineData("explicit instance")]
        [InlineData("instance explicit")]
        public void Calli_ExplicitInstance_PreservesExplicitThis(string callingConvention)
        {
            string source = $$"""
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        calli {{callingConvention}} void()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            MethodSignature<PrimitiveTypeCode> signature = DecodeCalliSignature(reader, MetadataTokens.StandaloneSignatureHandle(1));

            Assert.Equal(SignatureCallingConvention.Default, signature.Header.CallingConvention);
            Assert.True(signature.Header.IsInstance);
            Assert.True(signature.Header.HasExplicitThis);
            Assert.Equal(PrimitiveTypeCode.Void, signature.ReturnType);
            Assert.Empty(signature.ParameterTypes);
        }

        [Fact]
        public void Calli_VarArgSignature_DoesNotCountSentinelAsParameter()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        calli vararg void(int32, ..., string, int64)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            MethodSignature<PrimitiveTypeCode> signature = DecodeCalliSignature(reader, MetadataTokens.StandaloneSignatureHandle(1));

            Assert.Equal(SignatureCallingConvention.VarArgs, signature.Header.CallingConvention);
            Assert.Equal(1, signature.RequiredParameterCount);
            Assert.Equal([PrimitiveTypeCode.Int32, PrimitiveTypeCode.String, PrimitiveTypeCode.Int64], signature.ParameterTypes.ToArray());
        }

        [Fact]
        public void MaxStackDirective_IsPreserved()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        .maxstack 3
                        ldc.i4.1
                        localloc
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");

            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);
            Assert.Equal(3, body.MaxStack);
            Assert.True(body.LocalVariablesInitialized);
        }

        [Fact]
        public void MaxStackDirective_DoesNotInitializeExistingLocals()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        .maxstack 3
                        .locals (int32 V_0)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);

            Assert.Equal(3, body.MaxStack);
            Assert.False(body.LocalVariablesInitialized);
        }

        [Fact]
        public void MethodWithoutMaxStack_UsesNativeDefault()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        ldc.i4.0
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);

            Assert.Equal(8, body.MaxStack);
            Assert.False(body.LocalVariablesInitialized);
        }

        [Fact]
        public void ZeroInitWithoutLocals_ForcesFatHeader()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        .zeroinit
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);

            Assert.Equal(8, body.MaxStack);
            Assert.True(body.LocalVariablesInitialized);
        }

        [Theory]
        [InlineData("4294967295", 4294967295d)]
        [InlineData("4503599627370496", 4503599627370496d)]
        [InlineData("-4294967295", -4294967295d)]
        [InlineData("0xFFFFFFFF", 4294967295d)]
        [InlineData("4294967295.", 4294967295d)]
        public void LdcR8_IntegerLiteral_PreservesValue(string literal, double expected)
        {
            string source = $$"""
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        ldc.r8 {{literal}}
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal(0x23, il[0]);
            Assert.Equal(expected, BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(il.AsSpan(1))));
        }

        [Fact]
        public void LdcR4_IntegerLiteral_PreservesValue()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        ldc.r4 4294967295
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal(0x22, il[0]);
            Assert.Equal(4294967295f, BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(1))));
        }

        private static MethodSignature<PrimitiveTypeCode> DecodeCalliSignature(MetadataReader reader, StandaloneSignatureHandle handle)
        {
            BlobReader blobReader = reader.GetBlobReader(reader.GetStandaloneSignature(handle).Signature);
            var decoder = new SignatureDecoder<PrimitiveTypeCode, object?>(PrimitiveTypeProvider.Instance, reader, genericContext: null);

            return decoder.DecodeMethodSignature(ref blobReader);
        }

        private sealed class PrimitiveTypeProvider : ISignatureTypeProvider<PrimitiveTypeCode, object?>
        {
            public static PrimitiveTypeProvider Instance { get; } = new();

            public PrimitiveTypeCode GetArrayType(PrimitiveTypeCode elementType, ArrayShape shape) => elementType;
            public PrimitiveTypeCode GetByReferenceType(PrimitiveTypeCode elementType) => elementType;
            public PrimitiveTypeCode GetFunctionPointerType(MethodSignature<PrimitiveTypeCode> signature) => PrimitiveTypeCode.IntPtr;
            public PrimitiveTypeCode GetGenericInstantiation(PrimitiveTypeCode genericType, ImmutableArray<PrimitiveTypeCode> typeArguments) => genericType;
            public PrimitiveTypeCode GetGenericMethodParameter(object? genericContext, int index) => PrimitiveTypeCode.Object;
            public PrimitiveTypeCode GetGenericTypeParameter(object? genericContext, int index) => PrimitiveTypeCode.Object;
            public PrimitiveTypeCode GetModifiedType(PrimitiveTypeCode modifier, PrimitiveTypeCode unmodifiedType, bool isRequired) => unmodifiedType;
            public PrimitiveTypeCode GetPinnedType(PrimitiveTypeCode elementType) => elementType;
            public PrimitiveTypeCode GetPointerType(PrimitiveTypeCode elementType) => elementType;
            public PrimitiveTypeCode GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode;
            public PrimitiveTypeCode GetSZArrayType(PrimitiveTypeCode elementType) => elementType;
            public PrimitiveTypeCode GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => PrimitiveTypeCode.Object;
            public PrimitiveTypeCode GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => PrimitiveTypeCode.Object;
            public PrimitiveTypeCode GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => PrimitiveTypeCode.Object;
        }

        [Fact]
        public void Ldtoken_FieldReference_IsBackpatched()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .field public static int32 F
                    .method public static void M() cil managed
                    {
                        ldtoken field int32 Test::F
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "M", ILOpcode.ldtoken);

            DocumentCompilerTestHelpers.AssertFieldDefToken(reader, token, "F");
        }


        [Fact]
        public void MethodNameF1_NotConfusedWithHexByte()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static int32 f1() cil managed
                    {
                        ldc.i4.0
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var methods = typeDef.GetMethods().ToArray();
            Assert.Single(methods);
            Assert.Equal("f1", reader.GetString(reader.GetMethodDefinition(methods[0]).Name));
        }


        [Fact]
        public void SwitchInstruction_CommaLabels()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.i4.0
                        switch (L0, L1, L2)
                    L0: nop
                    L1: nop
                    L2: ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void FieldRVA_DataLabelEmitted()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .data D_1 = int32(42)
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 myData at D_1
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The FieldRVA table should have an entry
            int fieldRvaCount = reader.GetTableRowCount(TableIndex.FieldRva);
            Assert.True(fieldRvaCount >= 1, $"FieldRVA table should have at least 1 entry, has {fieldRvaCount}");
        }

    }
}
