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
    public class CustomAttributeTests
    {

        [Fact]
        public void CustomAttribute_HexByteBlob_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly
                {
                    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
                }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            // Verify the custom attribute was emitted on the assembly
            var asmDef = reader.GetAssemblyDefinition();
            var attrs = asmDef.GetCustomAttributes();
            Assert.NotEmpty(attrs);
        }


        [Fact]
        public void HexByteBlob_DigitLetterPairsCorrect()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 3F 5F 00 00 )
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var customAttrs = reader.GetCustomAttributes(MetadataTokens.MethodDefinitionHandle(1));
            foreach (var caHandle in customAttrs)
            {
                var ca = reader.GetCustomAttribute(caHandle);
                var blob = reader.GetBlobBytes(ca.Value);
                // Blob should be exactly: 01 00 3F 5F 00 00
                Assert.Equal(6, blob.Length);
                Assert.Equal(0x3F, blob[2]);
                Assert.Equal(0x5F, blob[3]);
            }
        }


        [Fact]
        public void CustomAttributeOnMethod_EmittedCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 Main() cil managed
                    {
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = ( 01 00 00 00 )
                        .entrypoint
                        ldc.i4 100
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal("Main", reader.GetString(method.Name));

            var customAttrs = method.GetCustomAttributes();
            Assert.Equal(1, customAttrs.Count);
        }


        [Fact]
        public void CustomAttributeOnType_EmittedCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 01 00 00 )
                    .method public instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The custom attribute should be in the CustomAttribute table
            Assert.True(reader.GetTableRowCount(TableIndex.CustomAttribute) >= 1,
                "Should have at least one custom attribute");

            // Find the ComVisibleAttribute on the type
            var typeHandle = MetadataTokens.TypeDefinitionHandle(2); // Test type
            var attrs = reader.GetCustomAttributes(typeHandle);
            Assert.True(attrs.Count >= 1, "Test type should have at least one custom attribute");
        }


        [Fact]
        public void CustomAttributeBlobDescr_EmptyBraces_CorrectProlog()
        {
            // '= {}' should produce a 4-byte blob: 01 00 (prolog) 00 00 (0 named args)
            string source = """
                .assembly extern mscorlib { }
                .assembly extern xunit.core { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = {}
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestMethod");

            var attrs = reader.GetCustomAttributes(MetadataTokens.MethodDefinitionHandle(
                MetadataTokens.GetRowNumber(reader.MethodDefinitions
                    .First(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "TestMethod"))));
            Assert.True(attrs.Count >= 1);

            var attr = reader.GetCustomAttribute(attrs.First());
            var blobBytes = reader.GetBlobBytes(attr.Value);
            // Should be exactly 4 bytes: 01 00 (prolog) 00 00 (0 named args)
            Assert.Equal(4, blobBytes.Length);
            Assert.Equal(0x01, blobBytes[0]); // prolog low byte
            Assert.Equal(0x00, blobBytes[1]); // prolog high byte
            Assert.Equal(0x00, blobBytes[2]); // named arg count low
            Assert.Equal(0x00, blobBytes[3]); // named arg count high
        }

    }
}
