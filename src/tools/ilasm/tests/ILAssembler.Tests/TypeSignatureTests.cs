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

namespace Microsoft.DotNet.ILAssembler.Tests
{
    public class TypeSignatureTests
    {
        [Theory]
        [InlineData("valuetype [mscorlib]System.IntPtr", true, SignatureTypeCode.IntPtr)]
        [InlineData("valuetype [mscorlib]System.UIntPtr", true, SignatureTypeCode.UIntPtr)]
        [InlineData("native int", false, SignatureTypeCode.IntPtr)]
        [InlineData("native unsigned int", false, SignatureTypeCode.UIntPtr)]
        public void NativeIntegerGenericMethod_PreservesReturnTypeEncoding(string returnType, bool explicitType, SignatureTypeCode shortCode)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public AddressHelper
                {
                    .method public hidebysig static {{returnType}} AddressOf<T>(!!0& t)
                    {
                        ldarg 0
                        ldind.i
                        ret
                    }
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            MethodDefinition method = reader.GetMethodDefinition(reader.MethodDefinitions.Single());
            BlobReader signature = reader.GetBlobReader(method.Signature);
            Assert.Equal((byte)SignatureAttributes.Generic, signature.ReadByte());
            Assert.Equal(1, signature.ReadCompressedInteger());
            Assert.Equal(1, signature.ReadCompressedInteger());
            if (explicitType)
            {
                Assert.Equal((byte)SignatureTypeKind.ValueType, signature.ReadByte());
                TypeReference type = reader.GetTypeReference((TypeReferenceHandle)signature.ReadTypeHandle());
                Assert.Equal("System", reader.GetString(type.Namespace));
                Assert.Equal(shortCode == SignatureTypeCode.IntPtr ? "IntPtr" : "UIntPtr", reader.GetString(type.Name));
                Assert.Equal("mscorlib", reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name));
            }
            else
            {
                Assert.Equal((byte)shortCode, signature.ReadByte());
            }

            Assert.Equal((byte)SignatureTypeCode.ByReference, signature.ReadByte());
            Assert.Equal((byte)SignatureTypeCode.GenericMethodParameter, signature.ReadByte());
            Assert.Equal(0, signature.ReadCompressedInteger());
            Assert.Equal(0, signature.RemainingBytes);
        }

        [Fact]
        public void MultiDimArrayBounds_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32[0...,0...] V_0)
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void MultiDimArrayLocal_EmitsArraySignatureShape()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32[0...,0...] V_0)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            StandaloneSignature signature = GetLocalSignature(pe, reader, "M");
            Assert.Equal(
                new[] { "int32[0...,0...]" },
                signature.DecodeLocalSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        [Fact]
        public void ArrayBoundVariants_DecodeExpectedShapes()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals init (
                            int32[] szArray,
                            int32[,] unspecified,
                            int32[5] sized,
                            int32[2...] lowerBoundOnly,
                            int32[2...5] bounded)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            StandaloneSignature signature = GetLocalSignature(pe, reader, "M");

            Assert.Equal(
                new[]
                {
                    "int32[]",
                    "int32[..., ...]".Replace(" ", string.Empty),
                    "int32[0...4]",
                    "int32[2...]",
                    "int32[2...5]",
                },
                signature.DecodeLocalSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        [Fact]
        public void MultiDimensionalLocalSignature_EmitsArrayShape()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32[0...,0...] matrix)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            StandaloneSignature signature = GetLocalSignature(pe, reader, "M");
            Assert.Equal(
                new[] { "int32[0...,0...]" },
                signature.DecodeLocalSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

        private static StandaloneSignature GetLocalSignature(
            PEReader pe,
            MetadataReader reader,
            string methodName)
        {
            MethodDefinition method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == methodName);
            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);

            Assert.False(body.LocalSignature.IsNil);
            return reader.GetStandaloneSignature(body.LocalSignature);
        }
    }
}
