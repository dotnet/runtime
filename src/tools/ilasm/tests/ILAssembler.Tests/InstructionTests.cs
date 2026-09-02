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
        public void Diagnostic_SwitchLabelNotFound_PointsToInstruction()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.i4.0
                        switch (UndefinedLabel)
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LabelNotFound, error.Id);
            Assert.Equal(source.IndexOf("switch", StringComparison.Ordinal), error.Location.Span.Start);
        }

        [Theory]
        [InlineData("ldc.i4")]
        [InlineData("ldc.i8")]
        [InlineData("ldarg")]
        [InlineData("br")]
        [InlineData("call instance void")]
        [InlineData("ldsfld int32")]
        [InlineData("ldsfld mdtoken(")]
        [InlineData("box")]
        [InlineData("ldtoken")]
        [InlineData("calli default void(")]
        [InlineData("calli vararg void(class [mscorlib]System.Tuple`1<method void *(int32 modreq(")]
        [InlineData("ldc.r8 float64(")]
        [InlineData("ldc.r8 bytearray(00 00")]
        [InlineData("ldstr \"A\" +")]
        [InlineData("ldstr ansi(\"A\"")]
        [InlineData("ldstr bytearray(48 00")]
        [InlineData("switch (L0,")]
        public void MalformedSimpleInstruction_DoesNotLeakMethodState(string malformedInstruction)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void Bad() cil managed
                    {
                        {{malformedInstruction}}
                    }

                    .method public static void Good() cil managed
                    {
                        ret
                    }
                }
                """;

            DocumentCompiler compiler = new();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ =>
                {
                    Assert.Fail("Expected no includes");
                    return default;
                },
                _ =>
                {
                    Assert.Fail("Expected no resources");
                    return default;
                },
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "Parser");
            Assert.NotNull(result);

            BlobBuilder image = new();
            result!.Serialize(image);
            using PEReader pe = new(image.ToImmutableArray());
            byte[] il = GetMethodIL(pe, "Good");

            Assert.Equal([0x2A], il);
        }

        [Fact]
        public void MalformedCalliSignature_DoesNotMaterializeDiscardedReferences()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void Bad() cil managed
                    {
                        calli default void(class [Unused]Payload
                    }

                    .method public static void Good() cil managed
                    {
                        call void [Used]Target::M()
                        ret
                    }
                }
                """;

            DocumentCompiler compiler = new();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ =>
                {
                    Assert.Fail("Expected no includes");
                    return default;
                },
                _ =>
                {
                    Assert.Fail("Expected no resources");
                    return default;
                },
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "Parser");
            Assert.NotNull(result);

            BlobBuilder image = new();
            result!.Serialize(image);
            using PEReader pe = new(image.ToImmutableArray());
            MetadataReader reader = pe.GetMetadataReader();
            string[] assemblyReferences = reader.AssemblyReferences
                .Select(handle => reader.GetString(reader.GetAssemblyReference(handle).Name))
                .ToArray();

            Assert.Contains("Used", assemblyReferences);
            Assert.DoesNotContain("Unused", assemblyReferences);
            Assert.Equal([0x28, 0x01, 0x00, 0x00, 0x0A, 0x2A], GetMethodIL(pe, "Good"));
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
        public void ReferenceAndCalliOperands_NestedSignaturesDecodeCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        ldsfld method vararg void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string) class [mscorlib]System.Tuple`1<class [mscorlib]System.Tuple`1<int32>>::Callback
                        pop
                        call void class [mscorlib]System.Tuple`1<class [mscorlib]System.Tuple`1<int32>>::Invoke(method vararg void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string))
                        ldc.i4.0
                        conv.i
                        calli vararg void(class [mscorlib]System.Tuple`1<class [mscorlib]System.Tuple`1<int32>>, ..., method vararg void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string))
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            MemberReference fieldReference = reader.MemberReferences
                .Select(reader.GetMemberReference)
                .Single(reference => reader.GetString(reference.Name) == "Callback");
            Assert.Equal(
                "method void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string)",
                fieldReference.DecodeFieldSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));

            MemberReference methodReference = reader.MemberReferences
                .Select(reader.GetMemberReference)
                .Single(reference => reader.GetString(reference.Name) == "Invoke");
            MethodSignature<string> methodSignature =
                methodReference.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal("void", methodSignature.ReturnType);
            Assert.Equal(
                new[] { "method void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string)" },
                methodSignature.ParameterTypes);

            MethodSignature<string> calliSignature = reader
                .GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1))
                .DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal(SignatureCallingConvention.VarArgs, calliSignature.Header.CallingConvention);
            Assert.Equal(1, calliSignature.RequiredParameterCount);
            Assert.Equal(
                new[]
                {
                    "[mscorlib]System.Tuple`1<[mscorlib]System.Tuple`1<int32>>",
                    "method void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string)",
                },
                calliSignature.ParameterTypes);
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
        public void SwitchInstruction_NamedLabels_EmitsExpectedBranchTable()
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

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "M");
            int switchOffset = Array.IndexOf(il, (byte)0x45);

            Assert.True(switchOffset >= 0);
            Assert.Equal(3, BitConverter.ToInt32(il, switchOffset + 1));
            Assert.Equal(0, BitConverter.ToInt32(il, switchOffset + 5));
            Assert.Equal(1, BitConverter.ToInt32(il, switchOffset + 9));
            Assert.Equal(2, BitConverter.ToInt32(il, switchOffset + 13));
        }

        [Theory]
        [InlineData("ldc.r4", "1.5", 1.5)]
        [InlineData("ldc.r8", "1.5", 1.5)]
        [InlineData("ldc.r8", ".5", 0.5)]
        [InlineData("ldc.r8", "5e+1", 50.0)]
        [InlineData("ldc.r8", "-1.25e-2", -0.0125)]
        [InlineData("ldc.r8", "float32(0x3F800000)", 1.0)]
        public void FloatingPointInstruction_TextAndFloat32BitForms_EmitExpectedValue(
            string opcode,
            string literal,
            double expected)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        {{opcode}} {{literal}}
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "M");
            if (opcode == "ldc.r4")
            {
                Assert.Equal(0x22, il[0]);
                Assert.Equal((float)expected, BitConverter.ToSingle(il, 1));
            }
            else
            {
                Assert.Equal(0x23, il[0]);
                Assert.Equal(expected, BitConverter.ToDouble(il, 1));
            }
        }

        [Fact]
        public void FloatingPointInstruction_Float64BitPattern_EmitsExpectedDouble()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static float64 GetPi() cil managed
                    {
                        ldc.r8 float64(0x400921FB54442D18)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "GetPi");

            Assert.Equal(0x23, il[0]);
            Assert.Equal(Math.PI, BitConverter.ToDouble(il, 1), 14);
        }

        [Fact]
        public void FloatingPointInstruction_IntegerOverflow_ReportsDiagnostic()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.r8 99999999999999999999999999999999
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LiteralOutOfRange, error.Id);
        }

        [Fact]
        public void FloatingPointInstruction_ByteForms_EmitExpectedConstants()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static float32 GetSingle() cil managed
                    {
                        ldc.r4 (00 00 80 3F)
                        ret
                    }

                    .method public static float64 GetDouble() cil managed
                    {
                        ldc.r8 bytearray(00 00 00 00 00 00 F0 3F)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] singleIL = GetMethodIL(pe, "GetSingle");
            byte[] doubleIL = GetMethodIL(pe, "GetDouble");

            Assert.Equal(0x22, singleIL[0]);
            Assert.Equal(1f, BitConverter.ToSingle(singleIL, 1));
            Assert.Equal(0x23, doubleIL[0]);
            Assert.Equal(1d, BitConverter.ToDouble(doubleIL, 1));
        }

        [Fact]
        public void CalliInstruction_EmitsStandaloneSignatureToken()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 M(native int functionPointer) cil managed
                    {
                        ldarg.0
                        calli default int32()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.StandAloneSig));

            var signature = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
            MethodSignature<string> decodedSignature =
                signature.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal(SignatureCallingConvention.Default, decodedSignature.Header.CallingConvention);
            Assert.Equal("int32", decodedSignature.ReturnType);
            Assert.Empty(decodedSignature.ParameterTypes);

            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "M", ILOpcode.calli);
            Assert.Equal(MetadataTokens.GetToken(MetadataTokens.StandaloneSignatureHandle(1)), token);
        }

        [Theory]
        [InlineData("unmanaged cdecl", (int)SignatureCallingConvention.CDecl, false, false)]
        [InlineData("unmanaged stdcall", (int)SignatureCallingConvention.StdCall, false, false)]
        [InlineData("unmanaged thiscall", (int)SignatureCallingConvention.ThisCall, false, false)]
        [InlineData("unmanaged fastcall", (int)SignatureCallingConvention.FastCall, false, false)]
        [InlineData("unmanaged", (int)SignatureCallingConvention.Unmanaged, false, false)]
        [InlineData("vararg", (int)SignatureCallingConvention.VarArgs, false, false)]
        [InlineData("instance default", (int)SignatureCallingConvention.Default, true, false)]
        [InlineData("callconv(0x05)", (int)SignatureCallingConvention.VarArgs, false, false)]
        public void CalliInstruction_CallingConvention_EmitsDecodedSignature(
            string callingConvention,
            int expectedCallingConvention,
            bool expectedInstance,
            bool expectedExplicitThis)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        ldc.i4.0
                        conv.i
                        calli {{callingConvention}} void()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var signature = reader.GetStandaloneSignature(Assert.Single(
                Enumerable.Range(1, reader.GetTableRowCount(TableIndex.StandAloneSig))
                    .Select(MetadataTokens.StandaloneSignatureHandle)));
            MethodSignature<string> decoded =
                signature.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);

            Assert.Equal((SignatureCallingConvention)expectedCallingConvention, decoded.Header.CallingConvention);
            Assert.Equal(expectedInstance, decoded.Header.IsInstance);
            Assert.True(
                decoded.Header.HasExplicitThis == expectedExplicitThis,
                $"Expected explicit-this={expectedExplicitThis}, header=0x{decoded.Header.RawValue:X2}");
            Assert.Equal("void", decoded.ReturnType);
            Assert.Empty(decoded.ParameterTypes);
        }

        [Fact]
        public void LdstrInstruction_ComposedAnsiAndRawForms_EmitExpectedUserStrings()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static string GetUtf16() cil managed
                    {
                        ldstr bytearray(48 00 69 00)
                        ret
                    }

                    .method public static string GetAnsi() cil managed
                    {
                        ldstr ansi("A" + "B")
                        ret
                    }

                    .method public static string GetOddAnsi() cil managed
                    {
                        ldstr ansi("A" + "BC")
                        ret
                    }

                    .method public static string GetComposed() cil managed
                    {
                        ldstr "A" + "B"
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal("Hi", ReadLdstrValue(pe, reader, "GetUtf16"));
            Assert.Equal("\u4241", ReadLdstrValue(pe, reader, "GetAnsi"));
            Assert.Equal("\u4241\u0043", ReadLdstrValue(pe, reader, "GetOddAnsi"));
            Assert.Equal("AB", ReadLdstrValue(pe, reader, "GetComposed"));
        }

        [Fact]
        public void Ldstr_WithControlAndQuotedEscapes_EmitsExpectedUserStrings()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test extends [System.Runtime]System.Object
                {
                    .method public static string ControlEscapes() cil managed
                    {
                        ldstr "A\bB\fC\vD\aE\?F\'G"
                        ret
                    }

                    .method public static string QuotedLiteral() cil managed
                    {
                        ldstr "double\"quoted\?"
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal("A\bB\fC\vD\aE?F'G", ReadLdstrValue(pe, reader, "ControlEscapes"));
            Assert.Equal("double\"quoted?", ReadLdstrValue(pe, reader, "QuotedLiteral"));
        }

        [Fact]
        public void Ldstr_WithLineContinuationAndShortOrHighOctalEscapes_EmitsExpectedUserString()
        {
            string source =
                ".assembly extern System.Runtime { }\n" +
                ".assembly test { }\n" +
                ".class public auto ansi beforefieldinit Test extends [System.Runtime]System.Object\n" +
                "{\n" +
                "    .method public static string FallbackEscapes() cil managed\n" +
                "    {\n" +
                "        ldstr \"line\\\n" +
                "              continued\\12\\400!\"\n" +
                "        ret\n" +
                "    }\n" +
                "}\n";

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal("linecontinued12400!", ReadLdstrValue(pe, reader, "FallbackEscapes"));
        }

        [Fact]
        public void SwitchInstruction_IntegerOffsets_EmitsExpectedBranchTable()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 M(int32 value) cil managed
                    {
                        ldarg.0
                        switch (3, 6)
                        ldc.i4.0
                        ret
                        ldc.i4.1
                        ret
                        ldc.i4.2
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "M");
            int switchOffset = Array.IndexOf(il, (byte)0x45);

            Assert.True(switchOffset >= 0);
            Assert.Equal(2, BitConverter.ToInt32(il, switchOffset + 1));
            Assert.Equal(3, BitConverter.ToInt32(il, switchOffset + 5));
            Assert.Equal(6, BitConverter.ToInt32(il, switchOffset + 9));
        }

        [Theory]
        [InlineData("switch ()")]
        [InlineData("switch ( )")]
        public void SwitchInstruction_Empty_EmitsEmptyBranchTable(string instruction)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        {{instruction}}
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "M");

            Assert.Equal(0x45, il[0]);
            Assert.Equal(0, BitConverter.ToInt32(il, 1));
        }

        [Fact]
        public void LdtokenInstruction_TypeReference_EmitsTypeReferenceToken()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static valuetype [mscorlib]System.RuntimeTypeHandle GetHandle() cil managed
                    {
                        ldtoken [mscorlib]System.Int32
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            byte[] il = GetMethodIL(pe, "GetHandle");

            Assert.Equal(0xD0, il[0]);
            int token = BitConverter.ToInt32(il, 1);
            var handle = MetadataTokens.EntityHandle(token);
            Assert.Equal(HandleKind.TypeReference, handle.Kind);
            Assert.Equal("Int32", reader.GetString(reader.GetTypeReference((TypeReferenceHandle)handle).Name));
        }

        [Fact]
        public void InstructionOperandForms_EmitExpectedTokensAndRawOperands()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 Value

                    .method public specialname rtspecialname instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }

                    .method public static void Exercise(object value) cil managed
                    {
                        br 0
                        ldsfld mdtoken(0x04000001)
                        pop
                        ldsfld int32 Test::Value
                        pop
                        ldsfld mdtoken(0x01000001)
                        pop
                        ldarg.0
                        unaligned. 1
                        ldind.i4
                        pop
                        ldarg.0
                        callvirt instance string [mscorlib]System.Object::ToString()
                        pop
                        newobj instance void Test::.ctor()
                        pop
                        ldc.i4.0
                        ldnull
                        ldc.i4.0
                        conv.i
                        calli default void(int32, string)
                        ldtoken Test
                        pop
                        switch ()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            byte[] il = GetMethodIL(pe, "Exercise");

            ImmutableArray<int> fieldTokens =
                DocumentCompilerTestHelpers.GetTokenOperands(pe, reader, "Exercise", ILOpcode.ldsfld);
            Assert.Equal(3, fieldTokens.Length);
            Assert.Equal(HandleKind.FieldDefinition, MetadataTokens.EntityHandle(fieldTokens[0]).Kind);
            Assert.Equal(HandleKind.FieldDefinition, MetadataTokens.EntityHandle(fieldTokens[1]).Kind);
            Assert.Equal(HandleKind.TypeReference, MetadataTokens.EntityHandle(fieldTokens[2]).Kind);

            int callvirtToken =
                DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Exercise", ILOpcode.callvirt);
            Assert.Equal(
                "ToString",
                reader.GetString(reader.GetMemberReference((MemberReferenceHandle)MetadataTokens.EntityHandle(callvirtToken)).Name));

            int newobjToken =
                DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Exercise", ILOpcode.newobj);
            Assert.Equal(
                ".ctor",
                reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)MetadataTokens.EntityHandle(newobjToken)).Name));

            int typeToken =
                DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Exercise", ILOpcode.ldtoken);
            Assert.Equal(
                "Test",
                reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.EntityHandle(typeToken)).Name));

            var calliSignature = reader.GetStandaloneSignature(
                MetadataTokens.StandaloneSignatureHandle(reader.GetTableRowCount(TableIndex.StandAloneSig)));
            MethodSignature<string> decodedCalli =
                calliSignature.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal("void", decodedCalli.ReturnType);
            Assert.Equal(new[] { "int32", "string" }, decodedCalli.ParameterTypes);

            Assert.True(ContainsSequence(il, [0x38, 0x00, 0x00, 0x00, 0x00]));
            Assert.True(ContainsSequence(il, [0xFE, 0x12, 0x01]));
            Assert.True(ContainsSequence(il, [0x45, 0x00, 0x00, 0x00, 0x00]));
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

        private static byte[] GetMethodIL(PEReader pe, string methodName)
        {
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(definition => reader.GetString(definition.Name) == methodName);
            return pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
        }

        private static string ReadLdstrValue(PEReader pe, MetadataReader reader, string methodName)
        {
            byte[] il = GetMethodIL(pe, methodName);
            Assert.Equal(0x72, il[0]);
            int token = BitConverter.ToInt32(il, 1);
            Assert.Equal(0x70, (token >> 24) & 0xFF);
            return reader.GetUserString(MetadataTokens.UserStringHandle(token & 0x00FFFFFF));
        }

        private static bool ContainsSequence(byte[] bytes, ReadOnlySpan<byte> sequence)
        {
            for (int i = 0; i <= bytes.Length - sequence.Length; i++)
            {
                if (bytes.AsSpan(i, sequence.Length).SequenceEqual(sequence))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
