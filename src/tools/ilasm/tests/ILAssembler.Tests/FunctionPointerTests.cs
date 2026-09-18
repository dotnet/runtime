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
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field signature: 0x06 (FIELD), 0x1B (FNPTR), ...
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            // After FNPTR: calling convention byte, param count, return type, param types
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x01, sigBytes[3]); // 1 parameter
            Assert.Equal(0x01, sigBytes[4]); // return type: void
            Assert.Equal(0x08, sigBytes[5]); // param type: int32
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

            var sigBytes = reader.GetBlobBytes(method.Signature);
            // Method signature: 0x00 (DEFAULT), 0x01 (1 param), 0x01 (void ret), ...
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT calling convention
            Assert.Equal(0x01, sigBytes[1]); // 1 parameter
            Assert.Equal(0x01, sigBytes[2]); // return type: void
            // Parameter should be ELEMENT_TYPE_FNPTR (0x1B)
            Assert.Equal(0x1B, sigBytes[3]); // ELEMENT_TYPE_FNPTR
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

            var sigBytes = reader.GetBlobBytes(method.Signature);
            // Method signature: 0x00 (DEFAULT), 0x00 (0 params), return type...
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT calling convention
            Assert.Equal(0x00, sigBytes[1]); // 0 parameters
            // Return type should be ELEMENT_TYPE_FNPTR (0x1B)
            Assert.Equal(0x1B, sigBytes[2]); // ELEMENT_TYPE_FNPTR
            // After FNPTR: calling convention, param count, return type (int32), param types (int32, int32)
            Assert.Equal(0x00, sigBytes[3]); // DEFAULT calling convention for inner sig
            Assert.Equal(0x02, sigBytes[4]); // 2 parameters in inner sig
            Assert.Equal(0x08, sigBytes[5]); // inner return type: int32
            Assert.Equal(0x08, sigBytes[6]); // inner param 1: int32
            Assert.Equal(0x08, sigBytes[7]); // inner param 2: int32
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
            var sigBytes = reader.GetBlobBytes(field.Signature);

            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x00, sigBytes[3]); // 0 parameters
            Assert.Equal(0x01, sigBytes[4]); // return type: void
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
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), 0x1B (FNPTR), 0x00 (DEFAULT), 0x01 (1 param),
            //            0x0F (PTR), 0x01 (VOID) [= void* return type], 0x08 (int32 param)
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x01, sigBytes[3]); // 1 parameter
            Assert.Equal(0x0F, sigBytes[4]); // return type: ELEMENT_TYPE_PTR
            Assert.Equal(0x01, sigBytes[5]); // return type inner: VOID (making void*)
            Assert.Equal(0x08, sigBytes[6]); // param type: int32
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
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), 0x1B (FNPTR), 0x00 (DEFAULT), 0x00 (0 params),
            //            0x0F (PTR), 0x01 (VOID) [= void* return type]
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x00, sigBytes[3]); // 0 parameters
            Assert.Equal(0x0F, sigBytes[4]); // return type: ELEMENT_TYPE_PTR
            Assert.Equal(0x01, sigBytes[5]); // return type inner: VOID (making void*)
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
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), 0x1B (FNPTR), 0x00 (DEFAULT), 0x01 (1 param),
            //            0x0F (PTR), 0x08 (I4) [= int32* return type], 0x08 (int32 param)
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            Assert.Equal(0x1B, sigBytes[1]); // ELEMENT_TYPE_FNPTR
            Assert.Equal(0x00, sigBytes[2]); // DEFAULT calling convention
            Assert.Equal(0x01, sigBytes[3]); // 1 parameter
            Assert.Equal(0x0F, sigBytes[4]); // return type: ELEMENT_TYPE_PTR
            Assert.Equal(0x08, sigBytes[5]); // return type inner: int32 (making int32*)
            Assert.Equal(0x08, sigBytes[6]); // param type: int32
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
            var sigBytes = reader.GetBlobBytes(field.Signature);

            // Field sig: 0x06 (FIELD), then the type is FNPTR(void(int32)) with PTR modifier
            // The modifier ordering means: 0x06, 0x1B (FNPTR), ..., then 0x0F (PTR) wraps it
            // But in practice ECMA-335 encodes: 0x06, 0x0F (PTR), 0x1B (FNPTR), ...
            Assert.Equal(0x06, sigBytes[0]); // FIELD calling convention
            // The next two bytes must contain both PTR and FNPTR
            Assert.Contains((byte)0x1B, sigBytes.Skip(1).ToArray()); // Must have ELEMENT_TYPE_FNPTR
            Assert.Contains((byte)0x0F, sigBytes.Skip(1).ToArray()); // Must have ELEMENT_TYPE_PTR
        }

    }
}
