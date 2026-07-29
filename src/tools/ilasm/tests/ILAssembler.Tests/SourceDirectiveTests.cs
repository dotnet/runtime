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
        private const string CSharpLanguageGuid = "{3F5162F8-07C6-11D3-9053-00C04FA302A1}";
        private const string CSharpVendorGuid = "{994B45C4-E6E9-11D2-903F-00C04FA302A1}";
        private const string DocumentTypeGuid = "{5A869D0B-6611-11D3-BD2A-0000F80849BD}";

        [Fact]
        public void LanguageDecl_DoesNotThrow()
        {
            string source = $$"""
                .assembly test { }
                .language "{{CSharpLanguageGuid}}", "{{CSharpVendorGuid}}"
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
            string source = $$"""
                .assembly test { }
                .language "{{CSharpLanguageGuid}}", "{{CSharpVendorGuid}}", "{{DocumentTypeGuid}}"
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void LanguageDecl_NonGuid_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .language "C#", "Microsoft", "Not-a-guid"
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
            string source = $$"""
                .assembly test { }
                .language '{{CSharpLanguageGuid}}'
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
            Assert.Equal(Guid.Parse(CSharpLanguageGuid), languageGuid);

            // Verify method debug info exists (sequence points were recorded)
            Assert.NotEmpty(pdbReader.MethodDebugInformation);
        }

        [Fact]
        public void PdbGeneration_LanguageWithVendorAndDocumentType_CreatesValidEmbeddedPdb()
        {
            string source = $$"""
                .assembly test { }
                .language '{{CSharpLanguageGuid}}', '{{CSharpVendorGuid}}', '{{DocumentTypeGuid}}'
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
            Assert.Equal(Guid.Parse(CSharpLanguageGuid), languageGuid);

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

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "M", ILOpcode.ldstr);
            string value = reader.GetUserString(MetadataTokens.UserStringHandle(token & 0x00FFFFFF));
            Assert.Equal("Hello\nWorld\t!", value);
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

        [Fact]
        public void LineDirective_WithRange_EmitsPortablePdbSequencePointWithSpecifiedStartPosition()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        .line 10, 10 : 5, 6 "test.cs"
                        nop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var methodHandle = reader.MethodDefinitions.Single(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "TestMethod");

            var embeddedPdbEntry = pe.ReadDebugDirectory().Single(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            using var pdbProvider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry);
            var pdbReader = pdbProvider.GetMetadataReader();
            var debugHandle = MetadataTokens.MethodDebugInformationHandle(MetadataTokens.GetRowNumber(methodHandle));
            var debugInfo = pdbReader.GetMethodDebugInformation(debugHandle);
            var sequencePoint = debugInfo.GetSequencePoints().First(point => !point.IsHidden);

            Assert.Equal(0, sequencePoint.Offset);
            Assert.Equal(10, sequencePoint.StartLine);
            Assert.Equal(5, sequencePoint.StartColumn);

            var document = pdbReader.GetDocument(sequencePoint.Document);
            Assert.Contains("test.cs", pdbReader.GetString(document.Name));
        }

        [Theory]
        [InlineData(".line 10 'single.cs'", "single.cs")]
        [InlineData(".line 11", "default.cs")]
        [InlineData(".line 12 : 3 'single.cs'", "single.cs")]
        [InlineData(".line 13 : 4", "default.cs")]
        [InlineData(".line 14 : 5, 6 'single.cs'", "single.cs")]
        [InlineData(".line 15 : 6, 7", "default.cs")]
        [InlineData(".line 16, 17 : 8 'single.cs'", "single.cs")]
        [InlineData(".line 18, 19 : 9", "default.cs")]
        [InlineData(".line 20, 21 : 10, 11 'single.cs'", "single.cs")]
        [InlineData(".line 22, 23 : 12, 13", "default.cs")]
        [InlineData(".line 24 \"double.cs\"", "double.cs")]
        [InlineData(".line 25 : 14 \"double.cs\"", "double.cs")]
        [InlineData(".line 26 : 15, 16 \"double.cs\"", "double.cs")]
        [InlineData(".line 27, 28 : 17 \"double.cs\"", "double.cs")]
        [InlineData(".line 29, 30 : 18, 19 \"double.cs\"", "double.cs")]
        public void LineDirective_SyntaxVariant_EmitsPortablePdbMethodDebugInformation(
            string directive,
            string expectedDocument)
        {
            string initialDirective =
                directive.Contains('\'') || directive.Contains('"')
                    ? string.Empty
                    : ".line 1, 1 : 1, 2 \"default.cs\"";
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        {{initialDirective}}
                        {{directive}}
                        nop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var methodHandle = reader.MethodDefinitions
                .Single(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "M");
            var embeddedPdb = Assert.Single(
                pe.ReadDebugDirectory(),
                entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            using var pdbProvider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdb);
            var pdbReader = pdbProvider.GetMetadataReader();
            var debugInformation = pdbReader.GetMethodDebugInformation(
                MetadataTokens.MethodDebugInformationHandle(MetadataTokens.GetRowNumber(methodHandle)));
            SequencePoint[] sequencePoints = debugInformation.GetSequencePoints().ToArray();

            Assert.False(debugInformation.SequencePointsBlob.IsNil);
            Assert.NotEmpty(sequencePoints);
            Assert.Contains(
                expectedDocument,
                pdbReader.GetString(pdbReader.GetDocument(debugInformation.Document).Name));
        }

        [Theory]
        [InlineData(".language '3f5162f8-07c6-11d3-9053-00c04fa302a1'")]
        [InlineData(".language '3f5162f8-07c6-11d3-9053-00c04fa302a1', '994b45c4-e6e9-11d2-903f-00c04fa302a1'")]
        [InlineData(".language '3f5162f8-07c6-11d3-9053-00c04fa302a1', '994b45c4-e6e9-11d2-903f-00c04fa302a1', '5a869d0b-6611-11d3-bd2a-0000f80849bd'")]
        [InlineData(".language \"3f5162f8-07c6-11d3-9053-00c04fa302a1\" \"994b45c4-e6e9-11d2-903f-00c04fa302a1\"")]
        [InlineData(".language \"3f5162f8-07c6-11d3-9053-00c04fa302a1\" \"994b45c4-e6e9-11d2-903f-00c04fa302a1\" \"5a869d0b-6611-11d3-bd2a-0000f80849bd\"")]
        public void LanguageDirective_SyntaxVariant_EmitsDocumentLanguage(string languageDirective)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        {{languageDirective}}
                        .line 10 "document.cs"
                        nop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var embeddedPdb = Assert.Single(
                pe.ReadDebugDirectory(),
                entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            using var pdbProvider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdb);
            var pdbReader = pdbProvider.GetMetadataReader();
            var document = pdbReader.GetDocument(Assert.Single(pdbReader.Documents));

            Assert.Equal(
                new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1"),
                pdbReader.GetGuid(document.Language));
            Assert.Contains("document.cs", pdbReader.GetString(document.Name));
        }

        [Fact]
        public void ClassScopedLanguageAndLineDirectives_ApplyToMethodSequencePoint()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .language '3f5162f8-07c6-11d3-9053-00c04fa302a1'
                    .line 1 "class.cs"
                    .method public static void M() cil managed
                    {
                        .line 10
                        nop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var embeddedPdb = Assert.Single(
                pe.ReadDebugDirectory(),
                entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            using var pdbProvider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdb);
            var pdbReader = pdbProvider.GetMetadataReader();
            var document = pdbReader.GetDocument(Assert.Single(pdbReader.Documents));

            Assert.Equal(
                new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1"),
                pdbReader.GetGuid(document.Language));
            Assert.Contains("class.cs", pdbReader.GetString(document.Name));
        }

        [Fact]
        public void MultiDocumentCompile_WithLineDirectivesAcrossDocuments_EmitsPdbDocumentsForEachSource()
        {
            var documents = ImmutableArray.Create(
                new SourceText("""
                    .assembly test { }
                    .class public auto ansi beforefieldinit First
                    {
                        .method public static void M1() cil managed
                        {
                            .line 10 "doc1.cs"
                            nop
                            ret
                        }
                    }
                    """, "doc1.il"),
                new SourceText("""
                    .class public auto ansi beforefieldinit Second
                    {
                        .method public static void M2() cil managed
                        {
                            .line 20 "doc2.cs"
                            nop
                            ret
                        }
                    }
                    """, "doc2.il"));

            var compiler = new DocumentCompiler();
            var (diagnostics, image) = compiler.Compile(
                documents,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { Pdb = true });

            Assert.Empty(diagnostics);
            Assert.NotNull(image);

            var imageBuilder = new BlobBuilder();
            image!.Serialize(imageBuilder);
            using var pe = new PEReader(imageBuilder.ToImmutableArray());
            var embeddedPdbEntry = Assert.Single(
                pe.ReadDebugDirectory(),
                entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            using var pdbProvider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry);
            var pdbReader = pdbProvider.GetMetadataReader();
            var documentNames = pdbReader.Documents
                .Select(handle => pdbReader.GetString(pdbReader.GetDocument(handle).Name))
                .ToArray();

            Assert.Equal(2, documentNames.Length);
            Assert.Contains(documentNames, name => name.Contains("doc1.cs", StringComparison.Ordinal));
            Assert.Contains(documentNames, name => name.Contains("doc2.cs", StringComparison.Ordinal));
            Assert.Equal(pe.GetMetadataReader().MethodDefinitions.Count, pdbReader.MethodDebugInformation.Count);
        }

    }
}
