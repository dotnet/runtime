// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection.Metadata;
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
            Assert.Equal(new[] { "point@0", "hidden@1", "point@2" }, pdb.ReadBlobRecords("M"));
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
    }
}
