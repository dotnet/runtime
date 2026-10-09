// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace ILAssembler.Tests
{
    /// <summary>
    /// The Portable PDB documents of sequence points: which document each point belongs to, and how a method
    /// whose points span several documents is encoded (docs/design/specs/PortablePdb-Metadata.md,
    /// "MethodDebugInformation Table" and "Sequence Points Blob").
    /// </summary>
    public class PdbDocumentTests
    {
        private static string Program(string methods) => $$"""
            .assembly extern System.Runtime { }
            .assembly test { }
            .class public auto ansi beforefieldinit Test
            {
            {{methods}}
            }
            """;

        private static string Method(string name, string body) => $$"""
                .method public static void {{name}}() cil managed
                {
            {{body}}
                }
            """;

        private const string TwoDocumentMethod = """
                    .line 1,1 : 1,2 'a.cs'
                    nop
                    .line 2,2 : 1,2 'b.cs'
                    ret
            """;

        [Fact]
        public void MethodSpanningTwoDocuments_HasNilDocument()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", TwoDocumentMethod)));

            Assert.True(pdb.GetDebugInformation("M").Document.IsNil);
        }

        [Fact]
        public void MethodSpanningTwoDocuments_BlobHeaderNamesTheFirstPointsDocumentAsInitialDocument()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", TwoDocumentMethod)));

            Assert.Equal(pdb.GetDocumentRowNumber("a.cs"), pdb.ReadBlobHeader("M").InitialDocument);
        }

        [Fact]
        public void MethodSpanningTwoDocuments_EachPointIsInItsOwnDocument()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", TwoDocumentMethod)));

            Assert.Equal(new[] { "a.cs", "b.cs" }, pdb.GetSequencePointDocumentNames("M"));
            Assert.Equal(new[] { 1, 2 }, pdb.GetSequencePoints("M").Select(point => point.StartLine));
        }

        [Fact]
        public void DocumentSwitchingBackAndForth_EachPointIsInItsOwnDocument()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 1,1 : 1,2 'a.cs'
                    nop
                    .line 2,2 : 1,2 'b.cs'
                    nop
                    .line 3,3 : 1,2 'a.cs'
                    nop
                    .line 4,4 : 1,2 'b.cs'
                    ret
            """)));

            Assert.Equal(new[] { "a.cs", "b.cs", "a.cs", "b.cs" }, pdb.GetSequencePointDocumentNames("M"));
        }

        [Fact]
        public void MethodsInOneDocumentEach_NameTheirOwnDocument()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(
                Method("M1", """
                    .line 1,1 : 1,2 'a.cs'
                    nop
                    .line 2,2 : 1,2 'a.cs'
                    ret
            """) +
                Method("M2", """
                    .line 3,3 : 1,2 'b.cs'
                    ret
            """)));

            Assert.Equal(("a.cs", "b.cs"), (pdb.GetMethodDocumentName("M1"), pdb.GetMethodDocumentName("M2")));
        }

        [Fact]
        public void TwoDirectivesAtOneOffset_KeepTheLastCoordinatesAndItsDocument()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 1,1 : 1,2 'a.cs'
                    .line 2,2 : 3,4 'b.cs'
                    nop
                    ret
            """)));

            SequencePoint point = Assert.Single(pdb.GetSequencePoints("M"));
            Assert.Equal((0, 2, 3, "b.cs"), (point.Offset, point.StartLine, point.StartColumn, pdb.GetDocumentName(point.Document)));
        }

        [Theory]
        [InlineData(".line 2 ''")]
        [InlineData(".line 2 \"\"")]
        [InlineData(".line 2,2 : 1,2 ''")]
        public void EmptyFileName_KeepsTheCurrentDocument(string emptyNameDirective)
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", $$"""
                    .line 1,1 : 1,2 'a.cs'
                    nop
                    {{emptyNameDirective}}
                    ret
            """)));

            Assert.Equal(new[] { "a.cs", "a.cs" }, pdb.GetSequencePointDocumentNames("M"));
            Assert.DoesNotContain(string.Empty, pdb.DocumentNames);
        }

        [Fact]
        public void EmptyFileNameOnTheFirstDirectiveOfAMethod_KeepsThePreviousMethodsDocument()
        {
            // ildasm writes '' on the first .line of a method whose file is the same as the previous method's.
            using var pdb = PortablePdbTestReader.Compile(Program(
                Method("M1", """
                    .line 1,1 : 1,2 'a.cs'
                    ret
            """) +
                Method("M2", """
                    .line 2,2 : 1,2 ''
                    nop
                    .line 3,3 : 1,2 ''
                    ret
            """)));

            Assert.Equal("a.cs", pdb.GetMethodDocumentName("M2"));
            Assert.Equal(new[] { "a.cs", "a.cs" }, pdb.GetSequencePointDocumentNames("M2"));
        }

        [Fact]
        public void HiddenPoint_HasNoDocumentRecordAndBelongsToTheCurrentDocument()
        {
            // As in native ilasm, a hidden point in another file makes the method span documents, but the hidden
            // point is encoded without a document-record and so is read in the current document.
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 1,1 : 1,2 'a.cs'
                    nop
                    .line 16707566,16707566 : 0,0 'b.cs'
                    nop
                    .line 3,3 : 1,2 'a.cs'
                    ret
            """)));

            SequencePoint[] points = pdb.GetSequencePoints("M");
            Assert.Equal(new[] { false, true, false }, points.Select(point => point.IsHidden));
            Assert.Equal(new[] { "a.cs", "a.cs", "a.cs" }, pdb.GetSequencePointDocumentNames("M"));

            // No document-record at all: not before the hidden point, and not before the third point, whose
            // document is still the current one.
            pdb.AssertNoDocumentRecordInSequencePointsBlob("M");
        }

        [Fact]
        public void AssertNoDocumentRecordInSequencePointsBlob_FailsForAMethodThatSwitchesDocuments()
        {
            // The check the previous test relies on fails when the blob does have a document-record: here, the one
            // before the point in b.cs.
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", TwoDocumentMethod)));

            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => pdb.AssertNoDocumentRecordInSequencePointsBlob("M"));
        }

        [Fact]
        public void HiddenPointInAnotherFile_MakesTheMethodSpanDocuments()
        {
            // Native ilasm counts the document of every point, hidden or not, when it decides whether a method
            // spans several documents.
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 1,1 : 1,2 'a.cs'
                    nop
                    .line 16707566,16707566 : 0,0 'b.cs'
                    nop
                    .line 3,3 : 1,2 'a.cs'
                    ret
            """)));

            Assert.True(pdb.GetDebugInformation("M").Document.IsNil);
        }

        [Fact]
        public void HiddenFirstPoint_ItsDocumentIsTheInitialDocument()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 16707566,16707566 : 0,0 'b.cs'
                    nop
                    .line 1,1 : 1,2 'a.cs'
                    ret
            """)));

            Assert.Equal(pdb.GetDocumentRowNumber("b.cs"), pdb.ReadBlobHeader("M").InitialDocument);
            Assert.Equal(new[] { "b.cs", "a.cs" }, pdb.GetSequencePointDocumentNames("M"));
        }

        [Fact]
        public void MethodWithoutSequencePoints_HasNilDocumentAndNoBlob()
        {
            // The spec says Document is nil when a method has no sequence points. Native ilasm names the current
            // document there instead; this deliberately follows the spec.
            using var pdb = PortablePdbTestReader.Compile(Program(
                Method("M1", """
                    .line 1,1 : 1,2 'a.cs'
                    ret
            """) +
                Method("M2", """
                    ret
            """)));

            MethodDebugInformation debugInformation = pdb.GetDebugInformation("M2");
            Assert.True(debugInformation.Document.IsNil);
            Assert.True(debugInformation.SequencePointsBlob.IsNil);
        }

        private static string ProgramWithBodylessMethod(string bodylessMethod) => $$"""
            .assembly extern System.Runtime { }
            .assembly test { }
            .class public abstract auto ansi beforefieldinit Test
            {
                {{bodylessMethod}}
                .method public static void M() cil managed
                {
                    .line 3,3 : 1,2 'a.cs'
                    ret
                }
            }
            """;

        [Fact]
        public void AbstractMethodWithALineDirective_HasNilDocumentAndNoBlob()
        {
            // A method without an IL body has no instructions for its .line directive to map; native ilasm records
            // sequence points as it emits instructions, so it records none.
            using var pdb = PortablePdbTestReader.Compile(ProgramWithBodylessMethod("""
                .method public hidebysig newslot abstract virtual instance void Abs() cil managed
                {
                    .line 1,1 : 1,2 'a.cs'
                }
            """));

            MethodDebugInformation debugInformation = pdb.GetDebugInformation("Abs");
            Assert.Equal(0, pdb.Image.GetMethodDefinition(pdb.GetMethodHandle("Abs")).RelativeVirtualAddress);
            Assert.True(debugInformation.Document.IsNil);
            Assert.True(debugInformation.SequencePointsBlob.IsNil);
        }

        [Fact]
        public void PInvokeMethodWithALineDirective_HasNilDocumentAndNoBlob()
        {
            using var pdb = PortablePdbTestReader.Compile(ProgramWithBodylessMethod("""
                .method public hidebysig static pinvokeimpl("libc" as "puts" cdecl) int32 Puts(native int s) cil managed preservesig
                {
                    .line 2,2 : 1,2 'a.cs'
                }
            """));

            MethodDebugInformation debugInformation = pdb.GetDebugInformation("Puts");
            Assert.Equal(0, pdb.Image.GetMethodDefinition(pdb.GetMethodHandle("Puts")).RelativeVirtualAddress);
            Assert.True(debugInformation.Document.IsNil);
            Assert.True(debugInformation.SequencePointsBlob.IsNil);
        }

        [Fact]
        public void BodyWithDirectivesButNoInstructions_HasNilDocumentAndNoBlob()
        {
            // .maxstack, .locals and a label emit no IL, so the method is written without a body (RVA 0), as in
            // native ilasm, and its .line directive has nothing to map.
            using var pdb = PortablePdbTestReader.Compile(Program(Method("Empty", """
                    .maxstack 1
                    .locals init (int32 x)
                    .line 1,1 : 1,2 'a.cs'
                    HERE:
            """)));

            MethodDebugInformation debugInformation = pdb.GetDebugInformation("Empty");
            Assert.Equal(0, pdb.Image.GetMethodDefinition(pdb.GetMethodHandle("Empty")).RelativeVirtualAddress);
            Assert.True(debugInformation.Document.IsNil);
            Assert.True(debugInformation.SequencePointsBlob.IsNil);
        }

        [Fact]
        public void ErrorTolerantCompile_MethodWithEmittedInstructions_KeepsItsSequencePoints()
        {
            // The undefined label is a recoverable error: with ErrorTolerant the image is still produced, the method
            // still has its instructions, and so it keeps its points.
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                Program(Method("M", """
                    .line 1,1 : 1,2 'a.cs'
                    br NOWHERE
                    .line 2,2 : 1,2 'b.cs'
                    ret
            """)),
                new Options { Debug = true, ErrorTolerant = true });
            Assert.Contains(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(result);
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(new[] { "a.cs", "b.cs" }, pdb.GetSequencePointDocumentNames("M"));
        }

        private static readonly Guid ILAssemblyLanguage = new("af046cd3-d0e1-11d2-977c-00a0c9b4d50c");
        private static readonly Guid CSharpLanguage = new("3f5162f8-07c6-11d3-9053-00c04fa302a1");

        private static CompilationResult CompileDocuments(Options options, params SourceText[] documents)
        {
            var (diagnostics, result) = new DocumentCompiler().Compile(
                documents.ToImmutableArray(),
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                options);
            Assert.Empty(diagnostics);
            Assert.NotNull(result);
            return result!;
        }

        [Fact]
        public void InputFile_IsTheFirstDocument()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 1,1 : 1,2 'a.cs'
                    ret
            """)));

            Assert.Equal(new[] { "test.il", "a.cs" }, pdb.DocumentNames);
        }

        [Fact]
        public void LineWithoutFileNameBeforeAnyNamedFile_IsInTheInputFile()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 5
                    ret
            """)));

            Assert.Equal("test.il", pdb.GetMethodDocumentName("M"));
        }

        [Fact]
        public void Documents_AreInTheOrderTheyAreNamed_NotInMethodDefinitionOrder()
        {
            // The global method comes after the class in the source, but its MethodDef row comes first.
            using var pdb = PortablePdbTestReader.Compile($$"""
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                {{Method("M", """
                    .line 1,1 : 1,2 'a.cs'
                    ret
            """)}}
                }
                .method public static void G() cil managed
                {
                    .line 2,2 : 1,2 'b.cs'
                    ret
                }
                """);

            Assert.True(MetadataTokens.GetRowNumber(pdb.GetMethodHandle("G")) < MetadataTokens.GetRowNumber(pdb.GetMethodHandle("M")));
            Assert.Equal(new[] { "test.il", "a.cs", "b.cs" }, pdb.DocumentNames);
        }

        [Theory]
        [InlineData("top level")]
        [InlineData("class level")]
        [InlineData("replaced in a method")]
        public void FileNamedByALineDirective_IsADocumentEvenWithoutSequencePoints(string where)
        {
            string unused = ".line 5,5 : 1,2 'unused.cs'";
            string methodBody = where == "replaced in a method"
                ? $"{unused}\n.line 1,1 : 1,2 'a.cs'\nret"
                : ".line 1,1 : 1,2 'a.cs'\nret";
            string source = $$"""
                .assembly extern System.Runtime { }
                .assembly test { }
                {{(where == "top level" ? unused : "")}}
                .class public auto ansi beforefieldinit Test
                {
                    {{(where == "class level" ? unused : "")}}
                    .method public static void M() cil managed
                    {
                        {{methodBody}}
                    }
                }
                """;

            using var pdb = PortablePdbTestReader.Compile(source);

            Assert.Equal(new[] { "test.il", "unused.cs", "a.cs" }, pdb.DocumentNames);
        }

        [Fact]
        public void Documents_DefaultToTheILAssemblyLanguage()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 1,1 : 1,2 'a.cs'
                    ret
            """)));

            Assert.Equal(
                new[] { ILAssemblyLanguage, ILAssemblyLanguage },
                pdb.Pdb.Documents.Select(handle => pdb.Pdb.GetGuid(pdb.Pdb.GetDocument(handle).Language)));
        }

        [Fact]
        public void LanguageDirective_AppliesToDocumentsDefinedAfterIt()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(
                Method("M1", """
                    .line 1,1 : 1,2 'a.cs'
                    ret
            """) +
                $"    .language '{CSharpLanguage}'\n" +
                Method("M2", """
                    .line 2,2 : 1,2 'b.cs'
                    ret
            """)));

            Assert.Equal(
                (ILAssemblyLanguage, ILAssemblyLanguage, CSharpLanguage),
                (pdb.GetDocumentLanguage("test.il"), pdb.GetDocumentLanguage("a.cs"), pdb.GetDocumentLanguage("b.cs")));
        }

        [Fact]
        public void FileNamedAgainAfterALanguageDirective_IsTheSameDocumentWithItsFirstLanguage()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(
                Method("M1", """
                    .line 1,1 : 1,2 'a.cs'
                    ret
            """) +
                $"    .language '{CSharpLanguage}'\n" +
                Method("M2", """
                    .line 2,2 : 1,2 'a.cs'
                    ret
            """)));

            Assert.Equal(new[] { "test.il", "a.cs" }, pdb.DocumentNames);
            Assert.Equal(ILAssemblyLanguage, pdb.GetDocumentLanguage("a.cs"));
            Assert.Equal("a.cs", pdb.GetMethodDocumentName("M2"));
        }

        [Fact]
        public void FileNamesDifferingOnlyInCase_AreDifferentDocuments()
        {
            // Native ilasm compares document names with strcmp.
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 1,1 : 1,2 'a.cs'
                    nop
                    .line 2,2 : 1,2 'A.cs'
                    ret
            """)));

            Assert.Equal(new[] { "test.il", "a.cs", "A.cs" }, pdb.DocumentNames);
        }

        [Fact]
        public void LanguageDirectiveInOneInputFile_AppliesToDocumentsDefinedInTheNextInputFile()
        {
            CompilationResult result = CompileDocuments(
                new Options { Pdb = true },
                new SourceText($".assembly test {{ }}\n.language '{CSharpLanguage}'\n", "first.il"),
                new SourceText(Program(Method("M", """
                    .line 1,1 : 1,2 'x.cs'
                    ret
            """)).Replace(".assembly test { }", string.Empty), "second.il"));
            using var pdb = new PortablePdbTestReader(result);

            Assert.Equal(
                (ILAssemblyLanguage, CSharpLanguage, CSharpLanguage),
                (pdb.GetDocumentLanguage("first.il"), pdb.GetDocumentLanguage("second.il"), pdb.GetDocumentLanguage("x.cs")));
        }

        [Fact]
        public void EachInputFile_IsADocument()
        {
            CompilationResult result = CompileDocuments(
                new Options { Pdb = true },
                new SourceText(".assembly test { }", "first.il"),
                new SourceText(".class public auto ansi Test { }", "second.il"));
            using var pdb = new PortablePdbTestReader(result);

            Assert.Equal(new[] { "first.il", "second.il" }, pdb.DocumentNames);
        }

        [Fact]
        public void Deterministic_MultiDocumentProgram_GivesIdenticalPdbBytes()
        {
            SourceText[] documents =
            [
                new SourceText(Program(
                    Method("M1", TwoDocumentMethod) +
                    Method("M2", """
                    .line 3,3 : 1,2 ''
                    nop
                    .line 4,4 : 1,2 'c.cs'
                    ret
            """)), "first.il"),
                new SourceText("""
                    .class public auto ansi beforefieldinit Second
                    {
                        .method public static void M3() cil managed
                        {
                            .line 5
                            nop
                            .line 6 'a.cs'
                            ret
                        }
                    }
                    """, "second.il"),
            ];
            var options = new Options { Debug = true, Deterministic = true };

            ImmutableArray<byte> first = DocumentCompilerTestHelpers.GetPortablePdb(CompileDocuments(options, documents));
            ImmutableArray<byte> second = DocumentCompilerTestHelpers.GetPortablePdb(CompileDocuments(options, documents));

            Assert.Equal<byte>(first, second);
        }

        private static string MethodWithLocals(string name, string locals) => Method(name, $$"""
                    .locals init ({{locals}})
                    .line 1,1 : 1,2 'a.cs'
                    ldc.i4.0
                    stloc.0
                    ret
            """);

        [Fact]
        public void LocalSignature_IsTheStandAloneSigRowNumberOfTheBodysLocalSignature()
        {
            // Two different local signatures, so the second method's is StandAloneSig row 2.
            using var pdb = PortablePdbTestReader.Compile(Program(
                MethodWithLocals("M1", "int32 x") +
                MethodWithLocals("M2", "int64 y, int32 z")));

            Assert.Equal<(int?, int?)>((1, 2), (pdb.GetBodyLocalSignatureRowNumber("M1"), pdb.GetBodyLocalSignatureRowNumber("M2")));
            Assert.Equal<(int?, int?)>(
                (pdb.GetBodyLocalSignatureRowNumber("M1"), pdb.GetBodyLocalSignatureRowNumber("M2")),
                (pdb.ReadBlobHeader("M1").LocalSignature, pdb.ReadBlobHeader("M2").LocalSignature));
        }

        [Fact]
        public void LocalSignature_IsZeroWithoutLocals()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(
                MethodWithLocals("M1", "int32 x") +
                Method("M2", """
                    .line 2,2 : 1,2 'a.cs'
                    ret
            """)));

            Assert.Equal(0, pdb.ReadBlobHeader("M2").LocalSignature);
        }

        [Fact]
        public void MethodWithLocalsButNoLineDirective_HasNoBlob()
        {
            // The spec's nil blob for a method without sequence points: there is no blob, so no LocalSignature,
            // even though the body has a local signature.
            using var pdb = PortablePdbTestReader.Compile(Program(
                MethodWithLocals("M1", "int32 x") +
                Method("M2", """
                    .locals init (int64 y)
                    ldc.i4.0
                    pop
                    ret
            """)));

            MethodDebugInformation debugInformation = pdb.GetDebugInformation("M2");
            Assert.Equal(2, pdb.GetBodyLocalSignatureRowNumber("M2"));
            Assert.True(debugInformation.Document.IsNil);
            Assert.True(debugInformation.SequencePointsBlob.IsNil);
        }

        [Fact]
        public void LocalSignature_WithFold_IsTheRowNumberTheSharedBodyReferences()
        {
            using var pdb = PortablePdbTestReader.Compile(
                Program(
                    MethodWithLocals("M1", "int64 y") +
                    MethodWithLocals("M2", "int32 x") +
                    MethodWithLocals("M3", "int32 x")),
                new Options { Debug = true, Fold = true });

            MethodDefinition second = pdb.Image.GetMethodDefinition(pdb.GetMethodHandle("M2"));
            MethodDefinition third = pdb.Image.GetMethodDefinition(pdb.GetMethodHandle("M3"));
            Assert.Equal(second.RelativeVirtualAddress, third.RelativeVirtualAddress);
            Assert.Equal(2, pdb.GetBodyLocalSignatureRowNumber("M3"));
            Assert.Equal<(int?, int?)>(
                (pdb.GetBodyLocalSignatureRowNumber("M2"), pdb.GetBodyLocalSignatureRowNumber("M3")),
                (pdb.ReadBlobHeader("M2").LocalSignature, pdb.ReadBlobHeader("M3").LocalSignature));
        }
    }
}
