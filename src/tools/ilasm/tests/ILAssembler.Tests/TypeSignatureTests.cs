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
    public class TypeSignatureTests
    {

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
            var signature = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
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
            var signature = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));

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

            var signature = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
            Assert.Equal(
                new[] { "int32[0...,0...]" },
                signature.DecodeLocalSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
        }

    }
}
