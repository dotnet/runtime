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
    public class SourceDirectiveTests
    {

        [Fact]
        public void LanguageDecl_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .language "C#" "3.0"
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void LanguageDecl_MultipleParameters_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .language "C#" "3.0" "vendor"
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void ExtSourceSpec_LineDirective_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        .line 10 "test.cs"
                        nop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void ExtSourceSpec_LineWithColumn_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        .line 10 : 5 "test.cs"
                        nop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void ExtSourceSpec_LineDirectiveHashLine_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        #line 42 "program.cs"
                        nop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void PdbGeneration_WithLineAndLanguageDirectives_CreatesValidEmbeddedPdb()
        {
            // Use C# language GUID
            string csharpGuid = "{3F5162F8-07C6-11D3-9053-00C04FA302A1}";
            string source = $$"""
                .assembly test { }
                .language '{{csharpGuid}}'
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        .line 10 "test.cs"
                        nop
                        .line 15 "test.cs"
                        nop
                        .line 20 "test.cs"
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            // Verify debug directory exists with embedded PDB
            var debugDirectory = pe.ReadDebugDirectory();
            Assert.NotEmpty(debugDirectory);

            var embeddedPdbEntry = debugDirectory.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            Assert.NotEqual(default, embeddedPdbEntry);

            // Read the embedded PDB and verify contents
            var pdbProvider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry);
            var pdbReader = pdbProvider.GetMetadataReader();

            // Verify document exists with correct name and language
            Assert.NotEmpty(pdbReader.Documents);
            var document = pdbReader.GetDocument(pdbReader.Documents.First());
            var docName = pdbReader.GetString(document.Name);
            Assert.Contains("test.cs", docName);

            var languageGuid = pdbReader.GetGuid(document.Language);
            Assert.Equal(Guid.Parse(csharpGuid), languageGuid);

            // Verify method debug info exists (sequence points were recorded)
            Assert.NotEmpty(pdbReader.MethodDebugInformation);
        }


        [Fact]
        public void PdbGeneration_WithoutLineDirectives_NoPdbGenerated()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        nop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            // Verify no embedded PDB when no debug directives
            var debugDirectory = pe.ReadDebugDirectory();
            var embeddedPdbEntry = debugDirectory.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            Assert.Equal(default, embeddedPdbEntry);
        }


        [Fact]
        public void StringEscape_NewlineInLdstr()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        ldstr "Hello\nWorld\t!"
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void MultiDocument_DefinePropagatesToNextDocument()
        {
            var doc1 = new SourceText("""
                #define ASSEMBLY_NAME "TestAssembly"
                .assembly extern mscorlib { }
                .assembly ASSEMBLY_NAME { }
                """, "doc1.il");

            var doc2 = new SourceText("""
                .class public auto ansi beforefieldinit ASSEMBLY_NAME extends [mscorlib]System.Object
                {
                }
                """, "doc2.il");

            var compiler = new DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(
                [doc1, doc2],
                _ => { Assert.Fail("Expected no includes"); return default; },
                _ => { Assert.Fail("Expected no resources"); return default; },
                new Options());

            Assert.Empty(diagnostics);
            Assert.NotNull(result);

            var blobBuilder = new BlobBuilder();
            result!.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();

            // doc2 should have the type named "TestAssembly" (from the macro)
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal("TestAssembly", reader.GetString(typeDef.Name));
        }

    }
}
