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
    public class FunctionPointerTests
    {

        [Fact]
        public void FunctionPointer_InFieldSignature_EmitsFnPtrTypeCode()
        {
            // A field of function pointer type: method void *(int32)
            // The signature should contain ELEMENT_TYPE_FNPTR (0x1B).
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void *(int32) fnPtrField
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal(
                "method void *(int32)",
                field.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }


        [Fact]
        public void FunctionPointer_InMethodParameter_EmitsFnPtrTypeCode()
        {
            // A method parameter of function pointer type.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Invoke(method void *(int32) callback) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Invoke");

            MethodSignature<string> signature =
                method.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal("void", signature.ReturnType);
            Assert.Equal(new[] { "method void *(int32)" }, signature.ParameterTypes);
        }


        [Fact]
        public void FunctionPointer_AsReturnType_EmitsFnPtrTypeCode()
        {
            // A method returning a function pointer.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static method int32 *(int32, int32) GetAdder() cil managed
                    {
                        ldc.i4.0
                        conv.i
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "GetAdder");

            MethodSignature<string> signature =
                method.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal("method int32 *(int32, int32)", signature.ReturnType);
            Assert.Empty(signature.ParameterTypes);
        }


        [Fact]
        public void FunctionPointer_NoArgs_EmitsFnPtrTypeCode()
        {
            // Function pointer with no parameters: method void *()
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void *() fnPtrField
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal(
                "method void *()",
                field.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }


        [Fact]
        public void FunctionPointer_ReturningVoidPtr_EmitsFnPtrWithPtrReturnType()
        {
            // A function pointer that returns void*: method void * *(int32)
            // Two * tokens: the first makes the return type void*, the second is the fnptr separator.
            // Signature: FNPTR, DEFAULT, 1 param, PTR(VOID), I4
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void * *(int32) fnPtrReturningVoidPtr
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal(
                "method void* *(int32)",
                field.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }


        [Fact]
        public void FunctionPointer_ReturningVoidPtr_NoArgs_EmitsFnPtrWithPtrReturnType()
        {
            // method void * *() — fnptr returning void* with no params
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void * *() fnPtrReturningVoidPtrNoArgs
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal(
                "method void* *()",
                field.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }


        [Fact]
        public void FunctionPointer_ReturningInt32Ptr_EmitsFnPtrWithPtrReturnType()
        {
            // method int32 * *(int32) — fnptr returning int32* with one param
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method int32 * *(int32) fnPtrReturningInt32Ptr
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal(
                "method int32* *(int32)",
                field.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }


        [Fact]
        public void FunctionPointer_PtrToFnPtr_EmitsPtrThenFnPtr()
        {
            // A pointer-to-function-pointer: method void *(int32)*
            // The outer * (after closing paren) makes this PTR(FNPTR(void(int32)))
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method void *(int32)* ptrToFnPtr
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal(
                "method void *(int32)*",
                field.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        [Fact]
        public void FunctionPointer_VarargSignature_EmitsSentinel()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static method vararg void *(int32, ..., object) fnPtrField
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal(
                "method void *(int32, ..., object)",
                field.DecodeSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

    }
}
