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
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Optimization_SequencePointsFollowEmittedInstructionSizes(bool optimize)
        {
            string source = DocumentCompilerTestHelpers.MethodSource("""
                .line 10,10:1,2 'test.cs'
                ldc.i4 0
                .line 20,20:1,2 'test.cs'
                ldarg 0
                .line 30,30:1,2 'test.cs'
                br END
                .line 40,40:1,2 'test.cs'
                END: ret
                """);
            using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbImage(
                DocumentCompilerTestHelpers.CompileAndGetPortablePdb(source, new Options { Optimize = optimize, Debug = true }));
            MetadataReader reader = provider.GetMetadataReader();
            SequencePoint[] points = reader.GetMethodDebugInformation(reader.MethodDebugInformation.Single())
                .GetSequencePoints().ToArray();
            Assert.Equal(optimize ? new[] { 0, 1, 2, 7 } : new[] { 0, 5, 9, 14 }, points.Select(point => point.Offset));
            Assert.Equal(new[] { 10, 20, 30, 40 }, points.Select(point => point.StartLine));
        }

        private const string CSharpLanguageGuid = "{3F5162F8-07C6-11D3-9053-00C04FA302A1}";
        private const string CSharpVendorGuid = "{994B45C4-E6E9-11D2-903F-00C04FA302A1}";
        private const string DocumentTypeGuid = "{5A869D0B-6611-11D3-BD2A-0000F80849BD}";
        private const string VisualBasicLanguageGuid = "{3A12D0B8-C26C-11D0-B442-00A0244A1DD2}";

        [Fact]
        public void LanguageDecl_DoesNotThrow()
        {
            string source = $$"""
                .assembly test { }
                .language '{{CSharpLanguageGuid}}', '{{CSharpVendorGuid}}'
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
                .language '{{CSharpLanguageGuid}}', '{{CSharpVendorGuid}}', '{{DocumentTypeGuid}}'
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
                .language 'C#', 'Microsoft', 'Not-a-guid'
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
                        .line 10 : 5 'test.cs'
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
        public void PdbGeneration_WithLineAndLanguageDirectives_CreatesValidPdb()
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

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Debug = true });

            // Read the PDB and verify contents
            using var pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
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
        public void PdbGeneration_LanguageWithVendorAndDocumentType_CreatesValidPdb()
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

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Debug = true });

            // Read the PDB and verify contents
            using var pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
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

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options());
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));

            // Verify no PDB and no debug directory when no debug directives
            Assert.Null(result.PortablePdb);
            Assert.Empty(pe.ReadDebugDirectory());
        }

        [Fact]
        public void LineDirective_WithoutDebugOrPdb_ProducesNoPdb()
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

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options());
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));

            // As in native ilasm, .line alone produces no PDB: only /DEBUG or /PDB does.
            Assert.Null(result.PortablePdb);
            Assert.Empty(pe.ReadDebugDirectory());
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
                        .line 10, 10 : 5, 6 'test.cs'
                        nop
                        ret
                    }
                }
                """;

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Debug = true });
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            var reader = pe.GetMetadataReader();
            var methodHandle = reader.MethodDefinitions.Single(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "TestMethod");

            using var pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
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
        public void LineDirective_SyntaxVariant_EmitsPortablePdbMethodDebugInformation(
            string directive,
            string expectedDocument)
        {
            string initialDirective =
                directive.Contains('\'') || directive.Contains('"')
                    ? string.Empty
                    : ".line 1, 1 : 1, 2 'default.cs'";
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

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Debug = true });
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            var reader = pe.GetMetadataReader();
            var methodHandle = reader.MethodDefinitions
                .Single(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "M");
            using var pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
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

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Debug = true });
            using var pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
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

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Debug = true });
            using var pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
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
            using var pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(image);
            var pdbReader = pdbProvider.GetMetadataReader();
            var documentNames = pdbReader.Documents
                .Select(handle => pdbReader.GetString(pdbReader.GetDocument(handle).Name))
                .ToArray();

            Assert.Equal(2, documentNames.Length);
            Assert.Contains(documentNames, name => name.Contains("doc1.cs", StringComparison.Ordinal));
            Assert.Contains(documentNames, name => name.Contains("doc2.cs", StringComparison.Ordinal));
            Assert.Equal(pe.GetMetadataReader().MethodDefinitions.Count, pdbReader.MethodDebugInformation.Count);
        }

        [Fact]
        public void MultiDocumentCompile_WithDifferentLanguages_PreservesEachPdbDocumentLanguage()
        {
            ImmutableArray<SourceText> documents =
            [
                new SourceText($$"""
                    .assembly test { }
                    .language '{{CSharpLanguageGuid}}'
                    .class public auto ansi First
                    {
                        .method public static void M1() cil managed
                        {
                            .line 10 "first.cs"
                            nop
                            ret
                        }
                    }
                    """, "first.il"),
                new SourceText($$"""
                    .language '{{VisualBasicLanguageGuid}}'
                    .class public auto ansi Second
                    {
                        .method public static void M2() cil managed
                        {
                            .line 20 "second.vb"
                            nop
                            ret
                        }
                    }
                    """, "second.il"),
            ];

            DocumentCompiler compiler = new();
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
                documents,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { Pdb = true });

            Assert.Empty(diagnostics);
            Assert.NotNull(result);

            using MetadataReaderProvider pdbProvider =
                DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result!);
            MetadataReader pdbReader = pdbProvider.GetMetadataReader();
            Dictionary<string, Guid> documentLanguages = pdbReader.Documents.ToDictionary(
                handle => pdbReader.GetString(pdbReader.GetDocument(handle).Name),
                handle => pdbReader.GetGuid(pdbReader.GetDocument(handle).Language));

            Assert.Equal(Guid.Parse(CSharpLanguageGuid), documentLanguages["first.cs"]);
            Assert.Equal(Guid.Parse(VisualBasicLanguageGuid), documentLanguages["second.vb"]);
        }

        [Fact]
        public void MultiDocumentCompile_DoesNotReusePreviousDocumentPath()
        {
            var documents = ImmutableArray.Create(
                new SourceText("""
                    .assembly test { }
                    .class public auto ansi First
                    {
                        .method public static void M1() cil managed
                        {
                            .line 10 "first.cs"
                            nop
                            ret
                        }
                    }
                    """, "first.il"),
                new SourceText("""
                    .class public auto ansi Second
                    {
                        .method public static void M2() cil managed
                        {
                            .line 20
                            nop
                            ret
                        }
                    }
                    """, "second.il"));

            var compiler = new DocumentCompiler();
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
                documents,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { Pdb = true });

            Assert.Empty(diagnostics);
            Assert.NotNull(result);

            var image = new BlobBuilder();
            result!.Serialize(image);
            using var pe = new PEReader(image.ToImmutableArray());
            MetadataReader reader = pe.GetMetadataReader();
            MethodDefinitionHandle firstMethod = reader.MethodDefinitions
                .Single(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "M1");
            MethodDefinitionHandle secondMethod = reader.MethodDefinitions
                .Single(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "M2");
            using MetadataReaderProvider pdbProvider =
                DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
            MetadataReader pdbReader = pdbProvider.GetMetadataReader();

            MethodDebugInformation firstDebugInformation = pdbReader.GetMethodDebugInformation(
                MetadataTokens.MethodDebugInformationHandle(MetadataTokens.GetRowNumber(firstMethod)));
            MethodDebugInformation secondDebugInformation = pdbReader.GetMethodDebugInformation(
                MetadataTokens.MethodDebugInformationHandle(MetadataTokens.GetRowNumber(secondMethod)));

            Assert.False(firstDebugInformation.SequencePointsBlob.IsNil);
            Assert.True(secondDebugInformation.SequencePointsBlob.IsNil);
            Assert.Contains(
                "first.cs",
                pdbReader.GetString(pdbReader.GetDocument(firstDebugInformation.Document).Name));
        }

        [Fact]
        public void MalformedLanguageDirective_DoesNotPartiallyUpdateGuidState()
        {
            ImmutableArray<SourceText> documents =
            [
                new SourceText($$"""
                    .assembly test { }
                    .language '{{CSharpLanguageGuid}}'
                    .language '{{DocumentTypeGuid}}',
                    """, "broken.il"),
                new SourceText("""
                    .class public auto ansi Test
                    {
                        .method public static void M() cil managed
                        {
                            .line 10 "document.cs"
                            nop
                            ret
                        }
                    }
                    """, "valid.il"),
            ];

            var compiler = new DocumentCompiler();
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
                documents,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true, Pdb = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(result);

            using MetadataReaderProvider pdbProvider =
                DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result!);
            MetadataReader pdbReader = pdbProvider.GetMetadataReader();
            Document document = pdbReader.GetDocument(Assert.Single(pdbReader.Documents));

            Assert.Equal(Guid.Parse(CSharpLanguageGuid), pdbReader.GetGuid(document.Language));
        }

    }
}
