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
    public class LocalTests
    {
        [Fact]
        public void Diagnostic_LocalNotFound()
        {
            // Reference a local variable that doesn't exist
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .locals (int32 x)
                        ldloc NonExistentLocal
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LocalNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void NamedLocal_CanBeReferencedByStloc()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32 myLocal)
                        ldc.i4.0
                        stloc myLocal
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void LocalMethodCall_ResolvesToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static int32 Helper() cil managed
                    {
                        ldc.i4.1
                        ret
                    }
                    .method public static int32 Caller() cil managed
                    {
                        call int32 MyClass::Helper()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // No MemberRef rows should exist for the local method call
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));

            // Verify the call instruction references a MethodDef token
            var callerMethod = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Caller");
            var body = pe.GetMethodBody(callerMethod.RelativeVirtualAddress);
            var ilReader = body.GetILReader();
            Assert.Equal(ILOpCode.Call, (ILOpCode)ilReader.ReadByte());
            int token = ilReader.ReadInt32();
            Assert.Equal(0x06, (token >> 24) & 0xFF); // MethodDef table (0x06)
        }

        [Fact]
        public void MixedLocalAndExternalRefs_ResolvesCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .field public static int32 myField
                    .method public static void Helper() cil managed
                    {
                        ret
                    }
                    .method public static void Caller() cil managed
                    {
                        // Local method call -> should resolve to MethodDef
                        call void MyClass::Helper()
                        // External method call -> should remain MemberRef
                        call string [mscorlib]System.Object::ToString(object)
                        pop
                        // Local field access -> should resolve to FieldDef
                        ldsfld int32 MyClass::myField
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Only the external call should produce a MemberRef row
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.MemberRef));

            var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(1));
            Assert.Equal("ToString", reader.GetString(memberRef.Name));
        }

        [Fact]
        public void LocalVarargMethodCall_ResolvesBaseToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static vararg void VarFunc() cil managed
                    {
                        ret
                    }
                    .method public static void Caller() cil managed
                    {
                        ldc.i4.1
                        call vararg void MyClass::VarFunc(..., int32)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Only 1 MemberRef: the vararg call-site. The base method resolved to MethodDef.
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.MemberRef));

            var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(1));
            Assert.Equal("VarFunc", reader.GetString(memberRef.Name));
            // The call-site MemberRef's parent should be the resolved MethodDef
            Assert.Equal(HandleKind.MethodDefinition, memberRef.Parent.Kind);

            // Verify the signature has the sentinel marker (it's a vararg call-site)
            var sigBytes = reader.GetBlobBytes(memberRef.Signature);
            Assert.Contains((byte)SignatureTypeCode.Sentinel, sigBytes);
        }

        [Fact]
        public void LocalVarargWithRequiredParams_ResolvesBaseToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static vararg void Printf(string fmt) cil managed
                    {
                        ret
                    }
                    .method public static void Caller() cil managed
                    {
                        ldstr "hello %d %s"
                        ldc.i4.1
                        ldstr "world"
                        call vararg void MyClass::Printf(string, ..., int32, string)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Only 1 MemberRef: the vararg call-site
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.MemberRef));

            var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(1));
            Assert.Equal("Printf", reader.GetString(memberRef.Name));
            Assert.Equal(HandleKind.MethodDefinition, memberRef.Parent.Kind);

            // Verify param count in signature: should be 3 (1 required + 2 optional)
            var sigBytes = reader.GetBlobBytes(memberRef.Signature);
            Assert.Equal(0x05, sigBytes[0]); // vararg
            Assert.Equal(3, sigBytes[1]);    // param count = 3
        }

        [Fact]
        public void MultipleLocalMethodCalls_AllResolveToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void A() cil managed { ret }
                    .method public static void B() cil managed { ret }
                    .method public static void C() cil managed { ret }
                    .method public static void Caller() cil managed
                    {
                        call void MyClass::A()
                        call void MyClass::B()
                        call void MyClass::C()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void ForwardReferencedLocalMethod_ResolvesToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Caller() cil managed
                    {
                        // Calls a method defined later in the same type
                        call void MyClass::Target()
                        ret
                    }
                    .method public static void Target() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void CrossTypeLocalMethodCall_ResolvesToMethodDef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit ClassA extends [mscorlib]System.Object
                {
                    .method public static void DoWork() cil managed
                    {
                        ret
                    }
                }
                .class public auto ansi beforefieldinit ClassB extends [mscorlib]System.Object
                {
                    .method public static void Caller() cil managed
                    {
                        call void ClassA::DoWork()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }

        [Fact]
        public void NestedTypeRef_EnclosingLocalNestedMissing_EmitsValidResolutionScope()
        {
            // Reference [test]Outer/Inner where Outer is defined locally (and resolves to a local
            // TypeDef) but the nested type Inner is NOT defined. Inner must remain a TypeRef whose
            // ResolutionScope is Outer's *TypeRef* row (a valid ResolutionScope coded index), not
            // Outer's resolved TypeDefinition handle (which is an invalid ResolutionScope and would
            // throw ArgumentException at emission).
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Outer extends [mscorlib]System.Object
                {
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        ldtoken [test]Outer/Inner
                        pop
                        ret
                    }
                }
                """;

            // Compilation must succeed (no ArgumentException from an invalid ResolutionScope).
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Inner is not defined locally, so it stays a TypeRef scoped to Outer's TypeRef row.
            var innerTypeRef = reader.GetTypeReference(DocumentCompilerTestHelpers.FindTypeRef(reader, "Inner"));
            Assert.Equal(HandleKind.TypeReference, innerTypeRef.ResolutionScope.Kind);
            var outerTypeRef = reader.GetTypeReference((TypeReferenceHandle)innerTypeRef.ResolutionScope);
            Assert.Equal("Outer", reader.GetString(outerTypeRef.Name));

            // Outer resolves to a local TypeDef but its TypeRef row is still emitted and scoped to
            // the self-AssemblyRef.
            Assert.Equal(HandleKind.AssemblyReference, outerTypeRef.ResolutionScope.Kind);

            // The ldtoken IL operand is Inner's TypeRef token (Inner is not a local type).
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "M", ILOpcode.ldtoken);
            DocumentCompilerTestHelpers.AssertTypeRefToken(reader, token, "Inner");
        }

        [Fact]
        public void LocalsInit_EmitsStandaloneSignature()
        {
            // .locals init (...) should emit a StandAloneSig that is connected
            // to the method body, causing ildasm to show the .locals directive.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32 x, string s)
                        ldc.i4.0
                        stloc.0
                        ldnull
                        stloc.1
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int sigCount = reader.GetTableRowCount(TableIndex.StandAloneSig);
            Assert.True(sigCount >= 1, $"Should have at least 1 StandAloneSig for .locals, got {sigCount}");

            var sig = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
            var sigBytes = reader.GetBlobBytes(sig.Signature);

            // LOCAL_SIG (0x07), 2 locals, I4 (0x08), STRING (0x0E)
            Assert.Equal(0x07, sigBytes[0]); // LOCAL_SIG
            Assert.Equal(0x02, sigBytes[1]); // 2 locals
            Assert.Equal(0x08, sigBytes[2]); // int32
            Assert.Equal(0x0E, sigBytes[3]); // string

            // The method should have InitLocals flag
            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            Assert.True(body.LocalVariablesInitialized);
        }

        [Fact]
        public void LocalsWithoutInit_EmitsStandaloneSignature()
        {
            // .locals (...) without init should still emit a StandAloneSig
            // but without the InitLocals flag.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals (int32 x)
                        ldc.i4.0
                        stloc.0
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int sigCount = reader.GetTableRowCount(TableIndex.StandAloneSig);
            Assert.True(sigCount >= 1, $"Should have at least 1 StandAloneSig for .locals, got {sigCount}");

            var sig = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
            var sigBytes = reader.GetBlobBytes(sig.Signature);

            Assert.Equal(0x07, sigBytes[0]); // LOCAL_SIG
            Assert.Equal(0x01, sigBytes[1]); // 1 local
            Assert.Equal(0x08, sigBytes[2]); // int32

            // The method should NOT have InitLocals flag
            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            Assert.False(body.LocalVariablesInitialized);
        }

        [Fact]
        public void LocalsWithArrayType_EmitsStandaloneSignature()
        {
            // .locals init with array type should emit correct signature.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32[0...] arr)
                        ldnull
                        stloc.0
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int sigCount = reader.GetTableRowCount(TableIndex.StandAloneSig);
            Assert.True(sigCount >= 1, $"Should have at least 1 StandAloneSig, got {sigCount}");

            var sig = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
            var sigBytes = reader.GetBlobBytes(sig.Signature);

            Assert.Equal(0x07, sigBytes[0]); // LOCAL_SIG
            Assert.Equal(0x01, sigBytes[1]); // 1 local
            Assert.Equal(0x14, sigBytes[2]); // ELEMENT_TYPE_ARRAY
            Assert.Equal(0x08, sigBytes[3]); // ELEMENT_TYPE_I4
            Assert.Equal(0x01, sigBytes[4]); // rank = 1
        }

        [Fact]
        public void ExplicitSlots_OutOfOrder_SignatureIsInSlotOrderAndNamesResolveToTheirSlots()
        {
            string source = OneMethod("""
                .locals init ([1] string b, [0] int32 a)
                ldloc a
                pop
                ldloc b
                pop
                ret
                """);

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.Equal(["int32", "string"], GetLocalTypes(pe));
            Assert.Equal(["ldloc 0", "ldloc 1"], GetLocalOperands(pe));
        }

        [Fact]
        public void LocalWithoutASlot_AfterAnExplicitSlot_TakesTheSlotAfterTheHighest()
        {
            string source = OneMethod("""
                .locals init ([0] int32 a, [2] int32 c)
                .locals init ([1] string b, object d)
                ldloc d
                pop
                ret
                """);

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.Equal(["int32", "string", "int32", "object"], GetLocalTypes(pe));
            Assert.Equal(["ldloc 3"], GetLocalOperands(pe));
        }

        [Fact]
        public void ExplicitSlot_GapFilledLater_GivesOneSignatureEntryPerSlot()
        {
            string source = OneMethod("""
                .locals init ([2] int32 c)
                .locals init ([0] string a, [1] object b)
                ldloc a
                pop
                ldloc b
                pop
                ldloc c
                pop
                ret
                """);

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.Equal(["string", "object", "int32"], GetLocalTypes(pe));
            Assert.Equal(["ldloc 0", "ldloc 1", "ldloc 2"], GetLocalOperands(pe));
        }

        [Fact]
        public void ExplicitSlot_GapNeverFilled_ReportsEachUntypedSlotAndWritesInt32ForIt()
        {
            string source = OneMethod("""
                .locals init ([2] string c)
                ldloc c
                pop
                ret
                """);

            ImmutableArray<Diagnostic> diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Equal(
                [
                    (DiagnosticIds.UndefinedLocalSlotType, DiagnosticSeverity.Error, "Undefined type of local var slot 0 in method M"),
                    (DiagnosticIds.UndefinedLocalSlotType, DiagnosticSeverity.Error, "Undefined type of local var slot 1 in method M"),
                ],
                diagnostics.Select(diagnostic => (diagnostic.Id, diagnostic.Severity, diagnostic.Message)));

            // Error-tolerant output keeps a well-formed signature.
            Assert.Equal(["int32", "int32", "string"], GetErrorTolerantLocalTypes(source));
        }

        [Fact]
        public void ExplicitSlot_ReusedInAClosedSiblingScopeWithTheSameType_IsOneSignatureEntry()
        {
            string source = OneMethod("""
                {
                    .locals init ([0] int32 x)
                    ldloc x
                    pop
                }
                {
                    .locals init ([0] int32 y)
                    ldloc y
                    pop
                }
                ret
                """);

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.Equal(["int32"], GetLocalTypes(pe));
            Assert.Equal(["ldloc 0", "ldloc 0"], GetLocalOperands(pe));
        }

        [Fact]
        public void ExplicitSlot_ReusedWithADifferentType_ReportsATypeConflict()
        {
            string source = OneMethod("""
                {
                    .locals init ([0] int32 x)
                    nop
                }
                {
                    .locals init ([0] string y)
                    nop
                }
                ret
                """);

            ImmutableArray<Diagnostic> diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());

            Diagnostic diagnostic = Assert.Single(diagnostics);
            Assert.Equal(
                (DiagnosticIds.LocalSlotTypeConflict, DiagnosticSeverity.Error, "Local var slot 0: type conflict"),
                (diagnostic.Id, diagnostic.Severity, diagnostic.Message));
        }

        [Fact]
        public void ExplicitSlot_ReusedWithADifferentType_TheSlotTakesTheNewType()
        {
            // As in native ilasm, the conflicting declaration still types the slot.
            string source = OneMethod("""
                {
                    .locals init ([0] int32 x)
                    nop
                }
                {
                    .locals init ([0] string y)
                    nop
                }
                ret
                """);

            Assert.Equal(["string"], GetErrorTolerantLocalTypes(source));
        }

        [Fact]
        public void ExplicitSlot_ReusedWhileItsLocalIsInScope_ReportsAWarning()
        {
            string source = OneMethod("""
                .locals init ([0] int32 x)
                {
                    .locals init ([0] int32 y)
                    ldloc y
                    pop
                }
                ret
                """);

            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options());

            Diagnostic diagnostic = Assert.Single(diagnostics);
            Assert.Equal(
                (DiagnosticIds.LocalSlotInUse, DiagnosticSeverity.Warning, "Local var slot 0 is in use"),
                (diagnostic.Id, diagnostic.Severity, diagnostic.Message));
            Assert.NotNull(result);
        }

        [Fact]
        public void ExplicitSlot_ReusedAfterItsScopeClosed_IsNotInUse()
        {
            string source = OneMethod("""
                {
                    .locals init ([0] int32 x)
                    nop
                }
                .locals init ([0] int32 y)
                ldloc y
                pop
                ret
                """);

            Assert.Empty(DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options()));
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            Assert.Equal(["int32"], GetLocalTypes(pe));
            Assert.Equal(["ldloc 0"], GetLocalOperands(pe));
        }

        [Fact]
        public void ExplicitSlot_ClosingABlockThatRedeclaredAnEnclosingSlot_LeavesTheSlotNotInUse()
        {
            // Native ilasm keeps one in-use flag per slot, cleared when any scope that declared the slot closes.
            // So after the block closes, slot 0 is not in use although a's scope is still open, and c's
            // declaration gets no warning; only b's does.
            string source = OneMethod("""
                .locals init ([0] int32 a)
                {
                    .locals init ([0] int32 b)
                    nop
                }
                .locals init ([0] int32 c)
                ret
                """);

            ImmutableArray<Diagnostic> diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());

            Diagnostic diagnostic = Assert.Single(diagnostics);
            Assert.Equal(
                (DiagnosticIds.LocalSlotInUse, DiagnosticSeverity.Warning, "Local var slot 0 is in use"),
                (diagnostic.Id, diagnostic.Severity, diagnostic.Message));
        }

        [Theory]
        [InlineData("65536")]
        [InlineData("-2")]
        [InlineData("0x10000")]
        [InlineData("2147483648")]
        [InlineData("4294967296")]
        [InlineData("0x100000000")]
        [InlineData("-4294967296")]
        [InlineData("4294967295")]
        [InlineData("040000000000")]
        [InlineData("0x10000000000000000")]
        public void ExplicitSlot_OutsideTheSixteenBitRange_IsReportedAndTakesTheNextSlot(string slot)
        {
            // The message names the slot as written: 2147483648 does not fit an int32, and is not reported as
            // the negative value it wraps to. The literals past 32 bits would wrap to 0, and 4294967295 to -1
            // ("no explicit slot"); slot 0 is taken first, so x taking slot 1 shows that none of them did.
            string source = OneMethod($$"""
                .locals init ([0] int32 a, [{{slot}}] int32 x)
                ldloc x
                pop
                ret
                """);

            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true });

            Diagnostic diagnostic = Assert.Single(diagnostics);
            Assert.Equal(
                (DiagnosticIds.LocalSlotOutOfRange, DiagnosticSeverity.Error, $"Local var slot {slot} is out of range; a slot index must be between 0 and 65535"),
                (diagnostic.Id, diagnostic.Severity, diagnostic.Message));
            Assert.NotNull(result);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result!));
            Assert.Equal(["int32", "int32"], GetLocalTypes(pe));
            Assert.Equal(["ldloc 1"], GetLocalOperands(pe));
        }

        [Theory]
        [InlineData("65535")]
        [InlineData("0xFFFF")]
        [InlineData("0177777")]
        public void ExplicitSlot_65535_IsInRangeHoweverWritten(string slot)
        {
            // The slots before 65535 are never typed, so each gets an undefined-type error; nothing else is
            // reported.
            string source = OneMethod($$"""
                .locals init ([{{slot}}] int32 x)
                ldloc x
                pop
                ret
                """);

            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true });

            Assert.All(diagnostics, diagnostic => Assert.Equal(DiagnosticIds.UndefinedLocalSlotType, diagnostic.Id));
            Assert.NotNull(result);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result!));
            Assert.Equal(["ldloc 65535"], GetLocalOperands(pe));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NextSlot_AfterSlot65535_IsReportedAndTheLocalIsNotDeclared(bool debug)
        {
            // The slots before 65535 are never typed, so each also gets an undefined-type error; those are not
            // what this test is about.
            string source = OneMethod("""
                .locals init ([65535] int32 last, int32 next)
                ret
                """);

            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true, Debug = debug });

            Diagnostic diagnostic = Assert.Single(diagnostics, diagnostic => diagnostic.Id != DiagnosticIds.UndefinedLocalSlotType);
            Assert.Equal(
                (DiagnosticIds.LocalSlotOutOfRange, DiagnosticSeverity.Error, "Local var slot 65536 is out of range; a slot index must be between 0 and 65535"),
                (diagnostic.Id, diagnostic.Severity, diagnostic.Message));
            Assert.NotNull(result);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result!));
            Assert.Equal(65536, GetLocalTypes(pe).Length);
        }

        [Fact]
        public void NextSlot_65535_IsTheLastSlot()
        {
            string source = OneMethod("""
                .locals init ([65534] int32 a, string b)
                ldloc b
                pop
                ret
                """);

            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true });

            // Only the undefined-type errors for the untyped slots 0 to 65533.
            Assert.Equal(65534, diagnostics.Count(diagnostic => diagnostic.Id == DiagnosticIds.UndefinedLocalSlotType));
            Assert.Equal(65534, diagnostics.Length);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result!));
            Assert.Equal(["ldloc 65535"], GetLocalOperands(pe));
            Assert.Equal("string", GetLocalTypes(pe)[^1]);
        }

        [Fact]
        public void LocalNames_AreCaseSensitive()
        {
            // As in native ilasm, which compares local names with strcmp.
            string source = OneMethod("""
                .locals init (int32 a, string A)
                ldnull
                stloc A
                ret
                """);

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.Equal(["stloc 1"], GetLocalOperands(pe));
        }

        [Fact]
        public void ExplicitSlot_MinusOne_TakesTheNextSlot()
        {
            // As in native ilasm, [-1] is the encoding of "no explicit slot".
            string source = OneMethod("""
                .locals init (string a, [-1] int32 b)
                ldloc b
                pop
                ret
                """);

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.Equal(["string", "int32"], GetLocalTypes(pe));
            Assert.Equal(["ldloc 1"], GetLocalOperands(pe));
        }

        [Fact]
        public void Sentinel_TakesNoSlot()
        {
            // As in native ilasm, a ... sentinel in a .locals list is not a local.
            string source = OneMethod("""
                .locals init (int32 a, ..., int32 b)
                ldloc b
                pop
                ret
                """);

            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options());

            Assert.Empty(diagnostics);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result!));
            Assert.Equal(["int32", "int32"], GetLocalTypes(pe));
            Assert.Equal(["ldloc 1"], GetLocalOperands(pe));
        }

        [Fact]
        public void SentinelPrefixedType_IsALocal()
        {
            // In "... int32 a" the sentinel is part of the type: the argument is a typed local, not the
            // standalone sentinel that takes no slot. As in native ilasm, its type is written as given.
            string source = OneMethod("""
                .locals init ([0] ... int32 a)
                ldloc a
                pop
                ret
                """);

            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options());

            Assert.Empty(diagnostics);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result!));
            MetadataReader reader = pe.GetMetadataReader();
            MethodBodyBlock body = DocumentCompilerTestHelpers.GetMethodBody(pe, "M");
            // LOCAL_SIG, one local, SENTINEL, I4.
            Assert.Equal(
                [0x07, 0x01, 0x41, 0x08],
                reader.GetBlobBytes(reader.GetStandaloneSignature(body.LocalSignature).Signature));
            Assert.Equal(["ldloc 0"], GetLocalOperands(pe));
        }

        [Fact]
        public void ShadowedName_ResolvesToTheInnerLocalInsideTheBlockAndToTheOuterLocalOutside()
        {
            string source = OneMethod("""
                .locals init (int32 a)
                stloc a
                {
                    .locals init (int32 a)
                    stloc a
                    ldloc a
                    pop
                }
                ldloc a
                pop
                ret
                """);

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.Equal(["int32", "int32"], GetLocalTypes(pe));
            Assert.Equal(["stloc 0", "stloc 1", "ldloc 1", "ldloc 0"], GetLocalOperands(pe));
        }

        [Fact]
        public void NameDeclaredInABlock_IsNotFoundAfterTheBlock()
        {
            string source = OneMethod("""
                .locals init (int32 a)
                {
                    .locals init (int32 b)
                    ldloc b
                    pop
                }
                ldloc b
                pop
                ret
                """);

            ImmutableArray<Diagnostic> diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());

            Diagnostic diagnostic = Assert.Single(diagnostics);
            Assert.Equal(
                (DiagnosticIds.LocalNotFound, "Local variable 'b' not found"),
                (diagnostic.Id, diagnostic.Message));
        }

        [Fact]
        public void NameDeclaredTwiceInOneScope_ResolvesToTheFirstDeclaration()
        {
            string source = OneMethod("""
                .locals init (int32 a, string a)
                ldloc a
                pop
                ret
                """);

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.Equal(["int32", "string"], GetLocalTypes(pe));
            Assert.Equal(["ldloc 0"], GetLocalOperands(pe));
        }

        /// <summary>Wraps a method body in a program with one static method <c>M</c>.</summary>
        internal static string OneMethod(string body) => $$"""
            .assembly extern System.Runtime { }
            .assembly test { }
            .class public auto ansi beforefieldinit Test extends [System.Runtime]System.Object
            {
                .method public static void M() cil managed
                {
            {{body}}
                }
            }
            """;

        /// <summary>Decodes the local signature of method <c>M</c>: one type per slot.</summary>
        internal static string[] GetLocalTypes(PEReader pe, string methodName = "M")
        {
            MetadataReader reader = pe.GetMetadataReader();
            MethodBodyBlock body = DocumentCompilerTestHelpers.GetMethodBody(pe, methodName);
            Assert.False(body.LocalSignature.IsNil);
            return reader.GetStandaloneSignature(body.LocalSignature)
                .DecodeLocalSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null)
                .ToArray();
        }

        private static string[] GetErrorTolerantLocalTypes(string source)
        {
            (_, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true });
            Assert.NotNull(result);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result!));
            return GetLocalTypes(pe);
        }

        /// <summary>
        /// Gets the local-variable instructions of method <c>M</c> in order, as <c>ldloc n</c>, <c>ldloca n</c> or
        /// <c>stloc n</c>. Short and macro forms are reported as their long form. Fails on an opcode that the
        /// tests in this file do not use, so that an operand is never misread.
        /// </summary>
        internal static string[] GetLocalOperands(PEReader pe, string methodName = "M")
        {
            byte[] il = DocumentCompilerTestHelpers.GetMethodBody(pe, methodName).GetILBytes()!;
            var operands = new List<string>();
            int offset = 0;
            while (offset < il.Length)
            {
                byte opcode = il[offset++];
                switch (opcode)
                {
                    case 0x00: // nop
                    case 0x14: // ldnull
                    case 0x16: // ldc.i4.0
                    case 0x17: // ldc.i4.1
                    case 0x18: // ldc.i4.2
                    case 0x26: // pop
                    case 0x2A: // ret
                    case 0xDC: // endfinally
                        break;
                    case >= 0x06 and <= 0x09: // ldloc.0 - ldloc.3
                        operands.Add($"ldloc {opcode - 0x06}");
                        break;
                    case >= 0x0A and <= 0x0D: // stloc.0 - stloc.3
                        operands.Add($"stloc {opcode - 0x0A}");
                        break;
                    case 0x11: // ldloc.s
                        operands.Add($"ldloc {il[offset++]}");
                        break;
                    case 0x12: // ldloca.s
                        operands.Add($"ldloca {il[offset++]}");
                        break;
                    case 0x13: // stloc.s
                        operands.Add($"stloc {il[offset++]}");
                        break;
                    case 0xDE: // leave.s
                        offset += 1;
                        break;
                    case 0xFE:
                        byte second = il[offset++];
                        string name = second switch
                        {
                            0x0C => "ldloc",
                            0x0D => "ldloca",
                            0x0E => "stloc",
                            _ => throw new InvalidOperationException($"Unexpected opcode 0xFE 0x{second:X2} at {offset - 2}"),
                        };
                        operands.Add($"{name} {BinaryPrimitives.ReadUInt16LittleEndian(il.AsSpan(offset))}");
                        offset += 2;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected opcode 0x{opcode:X2} at {offset - 1}");
                }
            }

            return operands.ToArray();
        }
    }
}
