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
    }
}
