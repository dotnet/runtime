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
    public class TypeReferenceTests
    {

        [Fact]
        public void ThisOutsideClass_ReportsError()
        {
            // Using .this outside of a class definition should report an error
            // Test at module level where there's no class context
            string source = """
                .assembly extern System.Runtime { }
                .typedef class .this as MyThis
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ThisOutsideClass, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void BaseOutsideClass_ReportsError()
        {
            // Using .base outside of a class definition should report an error
            string source = """
                .assembly extern System.Runtime { }
                .typedef class .base as MyBase
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.BaseOutsideClass, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void NesterOutsideNestedClass_ReportsError()
        {
            // Using .nester outside of a nested class should report an error
            string source = """
                .assembly extern System.Runtime { }
                .typedef class .nester as MyNester
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.NesterOutsideNestedClass, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void ForwardTypeReference_ResolvedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Base extends [System.Runtime]System.Object
                {
                    .field public static class Derived child
                }
                .class public auto ansi beforefieldinit Derived extends Base
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            Assert.Equal(3, reader.TypeDefinitions.Count);
        }


        [Fact]
        public void ExternalTypeRef_StaysTypeRef()
        {
            // An external TypeRef (different assembly) should NOT resolve to TypeDef.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // External call should remain as MemberRef with TypeRef parent.
            Assert.True(reader.GetTableRowCount(TableIndex.MemberRef) >= 1);
            Assert.True(reader.GetTableRowCount(TableIndex.TypeRef) >= 1);
        }

        [Fact]
        public void ExplicitNetstandardTypeRef_PreservesResolutionScope()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly extern netstandard { }
                .assembly test { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .method public static void M() cil managed
                    {
                        call void [netstandard]System.Console::WriteLine()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            TypeReferenceHandle consoleHandle = DocumentCompilerTestHelpers.FindTypeRef(reader, "Console");
            var console = reader.GetTypeReference(consoleHandle);
            var assemblyReference = reader.GetAssemblyReference((AssemblyReferenceHandle)console.ResolutionScope);

            Assert.Equal("netstandard", reader.GetString(assemblyReference.Name));
        }

        [Theory]
        [InlineData("Console")]
        [InlineData("Exception")]
        public void ExplicitMscorlibNonCoreTypeRef_PreservesResolutionScope(string typeName)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi Test extends [System.Runtime]System.Object
                {
                    .method public static void M() cil managed
                    {
                        ldtoken [mscorlib]System.{{typeName}}
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            TypeReferenceHandle typeHandle = DocumentCompilerTestHelpers.FindTypeRef(reader, typeName);
            var type = reader.GetTypeReference(typeHandle);
            var assemblyReference = reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope);

            Assert.Equal("mscorlib", reader.GetString(assemblyReference.Name));
        }


        [Fact]
        public void ResolvedTypeRefs_StillEmittedAsRows_InPseudoHandleOrder()
        {
            // Even when a self-assembly TypeRef resolves to a local TypeDef, its TypeRef row is
            // still emitted (matching native ilasm, which preserves all TypeRef rows). The rows are
            // emitted in pseudo-handle (creation) order, so "First" precedes "Second".
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi First extends [mscorlib]System.Object
                {
                    .method public static void A() cil managed { ret }
                }
                .class public auto ansi Second extends [mscorlib]System.Object
                {
                    .method public static void B() cil managed { ret }
                }
                .class public auto ansi Caller extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        call void [test]First::A()
                        call void [test]Second::B()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var firstTypeRef = DocumentCompilerTestHelpers.FindTypeRef(reader, "First");
            var secondTypeRef = DocumentCompilerTestHelpers.FindTypeRef(reader, "Second");
            Assert.Equal(HandleKind.AssemblyReference, reader.GetTypeReference(firstTypeRef).ResolutionScope.Kind);
            Assert.Equal(HandleKind.AssemblyReference, reader.GetTypeReference(secondTypeRef).ResolutionScope.Kind);
            Assert.True(MetadataTokens.GetRowNumber(firstTypeRef) < MetadataTokens.GetRowNumber(secondTypeRef),
                "TypeRef rows should be emitted in pseudo-handle (creation) order");

            // Both calls resolve to MethodDefs, and no MemberRef rows are emitted.
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }


        [Fact]
        public void UnqualifiedSystemString_ResolvesToCoreLibTypeRef()
        {
            // Unqualified 'System.String' (without [assembly] prefix) should resolve
            // to a TypeRef from the corelib, not create a local TypeDef
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Greet(class System.String msg) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // System.String should be a TypeRef, not a TypeDef
            // Only 2 TypeDefs should exist: <Module> and Test
            Assert.Equal(2, reader.GetTableRowCount(TableIndex.TypeDef));

            // System.String should be in TypeRef table
            bool foundStringTypeRef = reader.TypeReferences
                .Select(h => reader.GetTypeReference(h))
                .Any(t => reader.GetString(t.Name) == "String" && reader.GetString(t.Namespace) == "System");
            Assert.True(foundStringTypeRef, "System.String should be a TypeRef, not a TypeDef");
        }


        [Fact]
        public void TypeRefInILToken_BackpatchedAfterResolution()
        {
            // When a type instruction (unbox.any, box, castclass, etc.) references
            // a type via [self-assembly]Type, the IL token must be backpatched to the
            // resolved TypeDef handle after TypeRef→TypeDef resolution.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed MyStruct extends [mscorlib]System.ValueType
                {
                    .field public int32 x
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 Unbox(object o) cil managed
                    {
                        ldarg.0
                        unbox.any [test]MyStruct
                        ldfld int32 MyStruct::x
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The [test]MyStruct TypeRef row is still emitted, but the IL token is backpatched to
            // the resolved TypeDef handle. Decode the unbox.any operand and assert it is the
            // MyStruct TypeDef token.
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Unbox", ILOpcode.unbox_any);
            DocumentCompilerTestHelpers.AssertTypeDefToken(reader, token, "MyStruct");
        }


        [Fact]
        public void TypeRefInCastclass_BackpatchedAfterResolution()
        {
            // castclass with [self-assembly]Type should use TypeDef token.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyClass extends [mscorlib]System.Object
                {
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static class MyClass Cast(object o) cil managed
                    {
                        ldarg.0
                        castclass [test]MyClass
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The castclass IL token is backpatched to the resolved MyClass TypeDef.
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Cast", ILOpcode.castclass);
            DocumentCompilerTestHelpers.AssertTypeDefToken(reader, token, "MyClass");
        }


        [Fact]
        public void TypeRefInLdtoken_BackpatchedAfterResolution()
        {
            // ldtoken with [self-assembly]Type should use TypeDef token.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyType extends [mscorlib]System.Object
                {
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void GetToken() cil managed
                    {
                        ldtoken [test]MyType
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The ldtoken IL token is backpatched to the resolved MyType TypeDef.
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "GetToken", ILOpcode.ldtoken);
            DocumentCompilerTestHelpers.AssertTypeDefToken(reader, token, "MyType");
        }

    }
}
