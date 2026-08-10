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
    public class MethodTests
    {

        [Fact]
        public void Diagnostic_AbstractMethodNotInAbstractType()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public abstract void AbstractMethod() cil managed
                    {
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.AbstractMethodNotInAbstractType, error.Id);
            Assert.Equal(DiagnosticSeverity.Warning, error.Severity);
        }


        [Fact]
        public void Diagnostic_DuplicateMethod()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        ret
                    }
                    .method public static void TestMethod() cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = diagnostics.FirstOrDefault(d => d.Id == DiagnosticIds.DuplicateMethod);
            Assert.NotNull(error);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void VarargMethod_Definition_Compiles()
        {
            // Test vararg method definition
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static vararg void VarargMethod(int32 x) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "VarargMethod");

            // VarArgs calling convention = 0x5
            var signature = reader.GetBlobReader(methodDef.Signature);
            var header = signature.ReadByte();
            Assert.Equal(0x5, header & 0x0F);
        }


        [Fact]
        public void ArrayType_InMethodSignature_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M(int32[] arr) cil managed { ret }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var methods = typeDef.GetMethods().ToArray();
            Assert.Single(methods);
            var methodDef = reader.GetMethodDefinition(methods[0]);
            var sig = reader.GetBlobReader(methodDef.Signature);
            Assert.Equal(0x00, sig.ReadByte()); // DEFAULT calling convention
            Assert.Equal(1, sig.ReadCompressedInteger()); // param count
            Assert.Equal(0x01, sig.ReadByte()); // return type: void
            Assert.Equal(0x1D, sig.ReadByte()); // ELEMENT_TYPE_SZARRAY
            Assert.Equal(0x08, sig.ReadByte()); // ELEMENT_TYPE_I4 (int32)
        }


        [Fact]
        public void SimpleOverride_EmitsMethodImpl()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestOverride { }

                .class interface public abstract auto ansi IFoo
                {
                    .method public hidebysig newslot abstract virtual instance int32 GetVal() cil managed { }
                }

                .class public auto ansi beforefieldinit Bar extends [mscorlib]System.Object implements IFoo
                {
                    .method public hidebysig newslot virtual final instance int32 GetVal() cil managed
                    {
                        .override IFoo::GetVal
                        ldc.i4.s 42
                        ret
                    }
                    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int methodImplCount = reader.GetTableRowCount(TableIndex.MethodImpl);
            Assert.Equal(1, methodImplCount);
        }


        [Fact]
        public void OverrideWithExplicitSignature_EmitsMethodImpl()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestOverride { }

                .class public auto ansi beforefieldinit Base extends [mscorlib]System.Object
                {
                    .method public hidebysig newslot virtual instance object GetVal(string& res) cil managed
                    {
                        ldnull
                        ret
                    }
                }

                .class public auto ansi beforefieldinit Derived extends Base
                {
                    .method public hidebysig newslot virtual instance object GetVal(string& res) cil managed
                    {
                        .override method instance object Base::GetVal(string&)
                        ldnull
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int methodImplCount = reader.GetTableRowCount(TableIndex.MethodImpl);
            Assert.Equal(1, methodImplCount);
        }

        [Theory]
        [InlineData(".override method instance int32 [External]IFoo::M(string) with method instance int32 Bar::Impl(string)")]
        [InlineData(".override [External]IFoo::M with instance int32 Bar::Impl(string)")]
        public void ClassScopeOverride_EmitsMethodImpl(string overrideDirective)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly extern External { }
                .assembly TestOverride { }
                .class public auto ansi beforefieldinit Bar extends [mscorlib]System.Object implements [External]IFoo
                {
                    {{overrideDirective}}
                    .method public hidebysig newslot virtual final instance int32 Impl(string value) cil managed
                    {
                        ldc.i4.s 42
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var bar = reader.TypeDefinitions
                .Select(handle => (Handle: handle, Definition: reader.GetTypeDefinition(handle)))
                .Single(type => reader.GetString(type.Definition.Name) == "Bar");
            var implementation = reader.GetMethodImplementation(Assert.Single(bar.Definition.GetMethodImplementations()));

            Assert.Equal(HandleKind.MethodDefinition, implementation.MethodBody.Kind);
            Assert.Equal("Impl", reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)implementation.MethodBody).Name));
            Assert.Equal("M", reader.GetString(reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration).Name));
        }

        [Fact]
        public void ClassScopeShortOverride_EmitsDeclarationMemberRefFirst()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly extern External { }
                .assembly TestOverride { }
                .class public auto ansi Bar extends [mscorlib]System.Object
                {
                    .override [External]IFoo::M with instance int32 [External]Body::Impl(string)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var bar = reader.TypeDefinitions
                .Select(handle => reader.GetTypeDefinition(handle))
                .Single(type => reader.GetString(type.Name) == "Bar");
            var implementation = reader.GetMethodImplementation(Assert.Single(bar.GetMethodImplementations()));

            Assert.Equal(HandleKind.MemberReference, implementation.MethodDeclaration.Kind);
            Assert.Equal(HandleKind.MemberReference, implementation.MethodBody.Kind);
            Assert.True(
                MetadataTokens.GetRowNumber((MemberReferenceHandle)implementation.MethodDeclaration)
                < MetadataTokens.GetRowNumber((MemberReferenceHandle)implementation.MethodBody));
            Assert.Equal("M", reader.GetString(reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration).Name));
            Assert.Equal("Impl", reader.GetString(reader.GetMemberReference((MemberReferenceHandle)implementation.MethodBody).Name));
        }


        [Fact]
        public void MultipleOverrides_EmitsAllMethodImpls()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly TestOverride { }

                .class public auto ansi beforefieldinit GenBase<A,B> extends [mscorlib]System.Object
                {
                    .method public hidebysig newslot virtual instance object Func1(string& res) cil managed
                    {
                        ldnull
                        ret
                    }
                    .method public hidebysig newslot virtual instance object Func2(string& res) cil managed
                    {
                        ldnull
                        ret
                    }
                }

                .class public auto ansi beforefieldinit Derived<U,V> extends class GenBase<!U,!V>
                {
                    .method public hidebysig newslot virtual instance object Func1(string& res) cil managed
                    {
                        .override method instance object class GenBase<!U,!V>::Func1(string&)
                        ldnull
                        ret
                    }
                    .method public hidebysig newslot virtual instance object Func2(string& res) cil managed
                    {
                        .override method instance object class GenBase<!U,!V>::Func2(string&)
                        ldnull
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int methodImplCount = reader.GetTableRowCount(TableIndex.MethodImpl);
            Assert.Equal(2, methodImplCount);
        }


        [Fact]
        public void MethodRtSpecialName_Preserved()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal(".ctor", reader.GetString(method.Name));
            Assert.True(method.Attributes.HasFlag(MethodAttributes.RTSpecialName));
            Assert.True(method.Attributes.HasFlag(MethodAttributes.SpecialName));
        }


        [Fact]
        public void ExternalMethodCall_KeepsMemberRef()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Test() cil managed
                    {
                        call int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(1, reader.GetTableRowCount(TableIndex.MemberRef));
            var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(1));
            Assert.Equal("get_CurrentManagedThreadId", reader.GetString(memberRef.Name));
        }


        [Fact]
        public void ExternalVarargMethodCall_KeepsTypeRefParent()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Caller() cil managed
                    {
                        ldstr "format"
                        ldc.i4.1
                        box [mscorlib]System.Int32
                        call vararg int32 [mscorlib]System.String::Format(string, ..., object)
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the vararg call-site MemberRef
            int memberRefCount = reader.GetTableRowCount(TableIndex.MemberRef);
            Assert.True(memberRefCount >= 1);

            bool foundCallSite = false;
            for (int i = 1; i <= memberRefCount; i++)
            {
                var memberRef = reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(i));
                if (reader.GetString(memberRef.Name) == "Format")
                {
                    var sigBytes = reader.GetBlobBytes(memberRef.Signature);
                    if (sigBytes.Any(b => b == (byte)SignatureTypeCode.Sentinel))
                    {
                        foundCallSite = true;
                        // For external vararg call-sites, the parent should be TypeRef or MemberRef
                        // (not TypeDef, since String.Format is external)
                        Assert.True(
                            memberRef.Parent.Kind is HandleKind.TypeReference or HandleKind.MemberReference,
                            $"External vararg call-site parent should be TypeRef or MemberRef, got {memberRef.Parent.Kind}");
                    }
                }
            }
            Assert.True(foundCallSite, "Should have found the external vararg call-site MemberRef with sentinel");
        }


        [Fact]
        public void CctorMethod_HasSpecialNameAttribute()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public specialname rtspecialname static void .cctor() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == ".cctor");

            Assert.True(method.Attributes.HasFlag(MethodAttributes.SpecialName));
            Assert.True(method.Attributes.HasFlag(MethodAttributes.RTSpecialName));
        }


        [Fact]
        public void RtSpecialName_ImplicitlyAddsSpecialName()
        {
            // When only rtspecialname is specified (without specialname),
            // native ilasm implicitly adds specialname
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public rtspecialname static void .cctor() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == ".cctor");

            // Both SpecialName and RTSpecialName should be set
            Assert.True(method.Attributes.HasFlag(MethodAttributes.SpecialName));
            Assert.True(method.Attributes.HasFlag(MethodAttributes.RTSpecialName));
        }


        [Fact]
        public void NonStaticMethod_AutoInstanceCallingConvention()
        {
            // Non-static methods in a class should automatically get the instance
            // calling convention, even if not explicitly specified in the IL source
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public void DoWork() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "DoWork");

            // Check the signature has the instance flag
            var sigBytes = reader.GetBlobBytes(method.Signature);
            byte header = sigBytes[0];
            Assert.True((header & (byte)SignatureAttributes.Instance) != 0,
                $"Method signature should have Instance flag. Header byte: 0x{header:X2}");
        }


        [Fact]
        public void StaticMethod_NoAutoInstanceCallingConvention()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void DoWork() cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "DoWork");

            var sigBytes = reader.GetBlobBytes(method.Signature);
            byte header = sigBytes[0];
            Assert.True((header & (byte)SignatureAttributes.Instance) == 0,
                $"Static method should NOT have Instance flag. Header byte: 0x{header:X2}");
        }


        [Fact]
        public void ModOpt_InMethodSignature_PreservedInRewrittenBlob()
        {
            // A method parameter with modopt should preserve the modifier
            // after signature rewriting.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var sigBytes = reader.GetBlobBytes(method.Signature);

            // Method sig: 0x00 (DEFAULT), 0x01 (1 param), 0x01 (void ret),
            // then param: 0x20 (CMOD_OPT), <coded index>, 0x08 (I4)
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT
            Assert.Equal(0x01, sigBytes[1]); // 1 param
            Assert.Equal(0x01, sigBytes[2]); // void return
            Assert.Equal((byte)SignatureTypeCode.OptionalModifier, sigBytes[3]); // CMOD_OPT
            // The last byte is the underlying type (int32 = 0x08)
            Assert.Equal(0x08, sigBytes[^1]);
        }

    }
}
