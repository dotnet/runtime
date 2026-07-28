// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class MemberReferenceTests
    {
        [Fact]
        public void LocalVarargCallSite_WithLocalOptionalType_EmitsSentinelMemberRefSignature()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Payload extends [mscorlib]System.Object
                {
                }
                .class public auto ansi beforefieldinit Printer extends [mscorlib]System.Object
                {
                    .method public static vararg void Print(string format) cil managed
                    {
                        ret
                    }

                    .method public static void Caller() cil managed
                    {
                        ldstr "payload"
                        ldnull
                        call vararg void Printer::Print(string, ..., class [test]Payload)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var memberRefHandle = Assert.Single(reader.MemberReferences);
            var memberRef = reader.GetMemberReference(memberRefHandle);
            Assert.Equal("Print", reader.GetString(memberRef.Name));
            Assert.Equal(HandleKind.MethodDefinition, memberRef.Parent.Kind);

            // The optional vararg parameter is carried by the MemberRef signature only.
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.Param));

            MethodSignature<string> signature =
                memberRef.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal(SignatureCallingConvention.VarArgs, signature.Header.CallingConvention);
            Assert.Equal(1, signature.RequiredParameterCount);
            Assert.Equal("void", signature.ReturnType);
            Assert.Equal(new[] { "string", "Payload" }, signature.ParameterTypes);

            int callToken = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Caller", ILOpcode.call);
            Assert.Equal(MetadataTokens.GetToken(memberRefHandle), callToken);
        }

        [Fact]
        public void FieldReference_OnLocalGenericInstantiation_UsesTypeSpecParent()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Box`1<T> extends [mscorlib]System.Object
                {
                    .field public !0 Value
                }
                .class public auto ansi beforefieldinit Caller extends [mscorlib]System.Object
                {
                    .method public static int32 Read(class Box`1<int32> boxInstance) cil managed
                    {
                        ldarg.0
                        ldfld int32 class Box`1<int32>::Value
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var memberRefHandle = Assert.Single(reader.MemberReferences);
            var memberRef = reader.GetMemberReference(memberRefHandle);
            Assert.Equal("Value", reader.GetString(memberRef.Name));
            Assert.Equal(HandleKind.TypeSpecification, memberRef.Parent.Kind);
            Assert.Equal(
                "int32",
                memberRef.DecodeFieldSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));

            var parentTypeSpec = reader.GetTypeSpecification((TypeSpecificationHandle)memberRef.Parent);
            Assert.Equal(
                "Box`1<int32>",
                parentTypeSpec.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));

            int fieldToken = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Read", ILOpcode.ldfld);
            Assert.Equal(MetadataTokens.GetToken(memberRefHandle), fieldToken);
        }

        [Fact]
        public void MethodReference_OnLocalGenericInstantiation_UsesTypeSpecParent()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Box`1<T> extends [mscorlib]System.Object
                {
                    .method public hidebysig instance void Touch() cil managed
                    {
                        ret
                    }
                }
                .class public auto ansi beforefieldinit Caller extends [mscorlib]System.Object
                {
                    .method public static void Invoke(class Box`1<int32> boxInstance) cil managed
                    {
                        ldarg.0
                        call instance void class Box`1<int32>::Touch()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var memberRefHandle = Assert.Single(reader.MemberReferences);
            var memberRef = reader.GetMemberReference(memberRefHandle);
            Assert.Equal("Touch", reader.GetString(memberRef.Name));
            Assert.Equal(HandleKind.TypeSpecification, memberRef.Parent.Kind);

            MethodSignature<string> signature =
                memberRef.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.True(signature.Header.IsInstance);
            Assert.Equal("void", signature.ReturnType);
            Assert.Empty(signature.ParameterTypes);

            var parentTypeSpec = reader.GetTypeSpecification((TypeSpecificationHandle)memberRef.Parent);
            Assert.Equal(
                "Box`1<int32>",
                parentTypeSpec.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));

            int callToken = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Invoke", ILOpcode.call);
            Assert.Equal(MetadataTokens.GetToken(memberRefHandle), callToken);
        }

        [Fact]
        public void MethodReferenceForms_EmitResolvableCallTokensAndMethodSpecification()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .typedef method void Test::Target() as TargetAlias
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Target() cil managed
                    {
                        ret
                    }

                    .method public static void GenericTarget<T>() cil managed
                    {
                        ret
                    }

                    .method public static void Caller() cil managed
                    {
                        call void Target()
                        call void Test::GenericTarget<[1]>()
                        call void Test::GenericTarget<int32>()
                        call mdtoken(0x06000001)
                        call TargetAlias
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            ImmutableArray<int> callTokens =
                DocumentCompilerTestHelpers.GetTokenOperands(pe, reader, "Caller", ILOpcode.call);

            Assert.Equal(5, callTokens.Length);
            Assert.Equal("Target", GetMethodName(reader, MetadataTokens.EntityHandle(callTokens[0])));
            Assert.Equal("GenericTarget", GetMethodName(reader, MetadataTokens.EntityHandle(callTokens[1])));
            Assert.Equal(HandleKind.MethodSpecification, MetadataTokens.EntityHandle(callTokens[2]).Kind);
            Assert.Equal("Target", GetMethodName(reader, MetadataTokens.EntityHandle(callTokens[3])));
            Assert.Equal("Target", GetMethodName(reader, MetadataTokens.EntityHandle(callTokens[4])));

            var methodSpecification = reader.GetMethodSpecification(
                (MethodSpecificationHandle)MetadataTokens.EntityHandle(callTokens[2]));
            Assert.Equal(
                new[] { "int32" },
                methodSpecification.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        [Fact]
        public void ExternalFieldReference_EmitsMemberRefFieldSignature()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Reader extends [mscorlib]System.Object
                {
                    .method public static string GetEmpty() cil managed
                    {
                        ldsfld string [mscorlib]System.String::Empty
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var memberRefHandle = Assert.Single(reader.MemberReferences);
            var memberRef = reader.GetMemberReference(memberRefHandle);
            var parent = reader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);

            Assert.Equal("Empty", reader.GetString(memberRef.Name));
            Assert.Equal("String", reader.GetString(parent.Name));
            Assert.Equal(
                "string",
                memberRef.DecodeFieldSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        private static string GetMethodName(MetadataReader reader, EntityHandle handle)
        {
            return handle.Kind switch
            {
                HandleKind.MethodDefinition =>
                    reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name),
                HandleKind.MemberReference =>
                    reader.GetString(reader.GetMemberReference((MemberReferenceHandle)handle).Name),
                _ => throw new InvalidOperationException($"Unexpected method handle kind {handle.Kind}."),
            };
        }

    }
}
