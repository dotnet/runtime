// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Xunit;

namespace ILAssembler.Tests
{
    /// <summary>
    /// The sequence points of method bodies, read back from the Portable PDB: each instruction is mapped when
    /// it is emitted, to its own line of the <c>.il</c> source (columns 1 to 2) when no <c>.line</c> directive
    /// is in effect, and to the coordinates of the directive in effect otherwise; consecutive instructions with
    /// the same coordinates share a point.
    /// </summary>
    public class SequencePointTests
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

        /// <summary>Gets the 1-based number of the only line of <paramref name="source"/> that contains <paramref name="marker"/>.</summary>
        private static int LineOf(string source, string marker)
        {
            string[] lines = source.Split('\n');
            int index = Array.FindIndex(lines, line => line.Contains(marker, StringComparison.Ordinal));
            Assert.True(index >= 0, $"No line contains '{marker}'.");
            Assert.Equal(index, Array.FindLastIndex(lines, line => line.Contains(marker, StringComparison.Ordinal)));
            return index + 1;
        }

        private static (int Offset, int StartLine, int StartColumn, int EndLine, int EndColumn, string Document)[] Points(PortablePdbTestReader pdb, string method)
            => pdb.GetSequencePoints(method)
                .Select(point => (point.Offset, point.StartLine, point.StartColumn, point.EndLine, point.EndColumn, pdb.GetDocumentName(point.Document)))
                .ToArray();

        /// <summary>The point an instruction on <paramref name="line"/> of <paramref name="document"/> gets with no <c>.line</c> in effect.</summary>
        private static (int, int, int, int, int, string) OnLine(int offset, int line, string document = "test.il")
            => (offset, line, 1, line, 2, document);

        private static PortablePdbTestReader Compile(Options options, ImmutableArray<SourceText> inputs, params (string Name, string Text)[] includes)
        {
            var (diagnostics, result) = new DocumentCompiler().Compile(
                inputs,
                path => new SourceText(includes.Single(include => include.Name == path).Text, path),
                _ => throw new InvalidOperationException("Unexpected resource"),
                options);
            Assert.Empty(diagnostics);
            Assert.NotNull(result);
            return new PortablePdbTestReader(result!);
        }

        private static PortablePdbTestReader CompileWithIncludes(string source, params (string Name, string Text)[] includes)
            => Compile(new Options { Debug = true }, [new SourceText(source, "test.il")], includes);

        [Theory]
        [InlineData(0, 0x10000000, false, false)]
        [InlineData(0, 0x10000000, false, true)]
        [InlineData(0, 0x10000000, true, true)]
        [InlineData(0x10000001, 0, false, false)]
        [InlineData(0x10000001, 0, false, true)]
        [InlineData(0x10000001, 0, true, true)]
        public void UnencodableStartLineDelta_IsDiagnosed(int firstLine, int secondLine, bool hiddenBetween, bool errorTolerant)
        {
            string hidden = hiddenBetween ? ".line 0xFEEFEE,0xFEEFEE : 0,0\nnop\n" : "";
            string invalidDirective = $".line {secondLine},{secondLine} : 1,2 'second.cs'";
            string source = Program(Method("M", $"""
                .line {firstLine},{firstLine} : 1,2 'first.cs'
                nop
                {hidden}
                {invalidDirective}
                nop
                nop
                .line {firstLine},{firstLine} : 1,2 'first.cs'
                ret
                """));
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { Debug = true, ErrorTolerant = errorTolerant });

            Diagnostic error = Assert.Single(diagnostics);
            Assert.Equal("ILA0061", error.Id);
            Assert.Equal(source.IndexOf(invalidDirective, StringComparison.Ordinal), error.Location.Span.Start);
            if (!errorTolerant)
            {
                Assert.Null(result);
                return;
            }

            using var pdb = new PortablePdbTestReader(Assert.IsType<CompilationResult>(result));
            BlobReader blob = pdb.Pdb.GetBlobReader(pdb.GetDebugInformation("M").SequencePointsBlob);
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(firstLine, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            if (hiddenBetween)
            {
                Assert.Equal(1, blob.ReadCompressedInteger());
                Assert.Equal(0, blob.ReadCompressedInteger());
                Assert.Equal(0, blob.ReadCompressedInteger());
            }
            Assert.Equal(3, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedSignedInteger());
            Assert.Equal(0, blob.ReadCompressedSignedInteger());
            Assert.Equal(0, blob.RemainingBytes);
        }

        [Theory]
        [InlineData(0, 0x0fffffff)]
        [InlineData(0x10000000, 0)]
        public void EncodableStartLineDeltaBoundary_IsPreserved(int firstLine, int secondLine)
        {
            string source = Program(Method("M", $"""
                .line {firstLine},{firstLine} : 1,2 'a.cs'
                nop
                .line {secondLine},{secondLine} : 1,2 'a.cs'
                ret
                """));
            using var pdb = PortablePdbTestReader.Compile(source);
            BlobReader blob = pdb.Pdb.GetBlobReader(pdb.GetDebugInformation("M").SequencePointsBlob);
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(firstLine, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(secondLine - firstLine, blob.ReadCompressedSignedInteger());
            Assert.Equal(0, blob.ReadCompressedSignedInteger());
            Assert.Equal(0, blob.RemainingBytes);
        }

        [Theory]
        [InlineData(0, 0x10000000, false)]
        [InlineData(0, 0x10000000, true)]
        [InlineData(0x10000001, 0, false)]
        [InlineData(0x10000001, 0, true)]
        public void RejectedStartLineDelta_DoesNotSuppressTheNextMethodsFirstPoint(int firstLine, int secondLine, bool repeatDirective)
        {
            string directive = $".line {secondLine},{secondLine} : 1,2 'a.cs'";
            string source = Program(
                Method("A", $"""
                    .line {firstLine},{firstLine} : 1,2 'a.cs'
                    nop
                    {directive}
                    nop
                    {(repeatDirective ? directive : "")}
                    ret
                    """) +
                Method("B", "nop\nret") +
                Method("C", "ret"));
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { Debug = true, ErrorTolerant = true });

            Assert.Equal(repeatDirective ? 2 : 1, diagnostics.Length);
            Assert.All(diagnostics, diagnostic => Assert.Equal(DiagnosticIds.InvalidSequencePoint, diagnostic.Id));
            using var pdb = new PortablePdbTestReader(Assert.IsType<CompilationResult>(result));
            AssertSinglePoint("A", firstLine);
            AssertSinglePoint("B", secondLine);
            Assert.True(pdb.GetDebugInformation("C").SequencePointsBlob.IsNil);

            void AssertSinglePoint(string method, int line)
            {
                MethodDebugInformation info = pdb.GetDebugInformation(method);
                Assert.False(info.SequencePointsBlob.IsNil);
                Assert.Equal("a.cs", pdb.GetDocumentName(info.Document));
                BlobReader blob = pdb.Pdb.GetBlobReader(info.SequencePointsBlob);
                Assert.Equal(0, blob.ReadCompressedInteger());
                Assert.Equal(0, blob.ReadCompressedInteger());
                Assert.Equal(0, blob.ReadCompressedInteger());
                Assert.Equal(1, blob.ReadCompressedInteger());
                Assert.Equal(line, blob.ReadCompressedInteger());
                Assert.Equal(1, blob.ReadCompressedInteger());
                Assert.Equal(0, blob.RemainingBytes);
            }
        }

        [Fact]
        public void StartLineDelta_DoesNotCrossMethodBoundaries()
        {
            string source = Program(
                Method("A", ".line 0,0 : 1,2 'a.cs'\nret") +
                Method("B", ".line 0x10000000,0x10000000 : 1,2 'b.cs'\nret"));
            using var pdb = PortablePdbTestReader.Compile(source);

            BlobReader blob = pdb.Pdb.GetBlobReader(pdb.GetDebugInformation("B").SequencePointsBlob);
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(0x10000000, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.RemainingBytes);
        }

        [Theory]
        [InlineData("10,9 : 1,2")]
        [InlineData("10,10 : 2,1")]
        [InlineData("10,10 : 2,2")]
        [InlineData("10,10 : 0,0")]
        [InlineData("10,10 : -1,2")]
        [InlineData("10,10 : 1,-1")]
        [InlineData("10,10 : 65536,65537")]
        [InlineData("10,10 : 1,65536")]
        [InlineData("10,11 : 70000,1")]
        [InlineData("-1,10 : 1,2")]
        [InlineData("10,-1 : 1,2")]
        [InlineData("536870912,536870912 : 1,2")]
        [InlineData("10,536870912 : 1,2")]
        [InlineData("0xFEEFEE,0xFEEFEE : 1,2")]
        [InlineData("0xFEEFEE,0xFEEFEE : 0,1")]
        [InlineData("0xFEEFEE,0xFEEFEE : 1,0")]
        [InlineData("0xFEEFEE,0xFEEFEF : 0,0")]
        [InlineData("10,0xFEEFEE : 0,0")]
        [InlineData("10 : 65535")]
        [InlineData("10 : 65534")]
        [InlineData("0xFEEFEE : 1")]
        public void InvalidLineDirective_ReportsErrorAtTheDirective(string coordinates)
        {
            string source = Program(Method("M", $".line {coordinates} 'a.cs'\nnop\nret"));
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { Debug = true });

            Diagnostic error = Assert.Single(diagnostics);
            Assert.Equal("ILA0061", error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Contains("sequence point", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("test.il", error.Location.Source.Path);
            Assert.Equal(source.IndexOf(".line", StringComparison.Ordinal), error.Location.Span.Start);
            Assert.Equal(5, error.Location.Span.Length);
            Assert.Null(result);
        }

        [Theory]
        [InlineData("10", 10, 0, 10)]
        [InlineData("10 : 5", 10, 5, 10)]
        [InlineData("10,11 : 5", 10, 5, 11)]
        [InlineData("10 : 65533", 10, 65533, 10)]
        public void OmittedEndColumn_UsesReaderCompatibleMaximumColumn(string coordinates, int startLine, int startColumn, int endLine)
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", $".line {coordinates} 'a.cs'\nret")));
            Assert.Equal(new[] { (0, startLine, startColumn, endLine, ushort.MaxValue - 1, "a.cs") }, Points(pdb, "M"));
        }

        [Theory]
        [InlineData("10,10 : 0,1", 10, 0, 10, 1)]
        [InlineData("10,11 : 9,1", 10, 9, 11, 1)]
        [InlineData("10,11 : 5,5", 10, 5, 11, 5)]
        [InlineData("10,10 : 1,65534", 10, 1, 10, 65534)]
        [InlineData("0xFEEFEE,0xFEEFEE : 0,0", 0xFEEFEE, 0, 0xFEEFEE, 0)]
        [InlineData("0xFEEFEE", 0xFEEFEE, 0, 0xFEEFEE, 0)]
        [InlineData("0xFEEFEE : 0", 0xFEEFEE, 0, 0xFEEFEE, 0)]
        [InlineData("0xFEEFEE,0xFEEFEE : 0", 0xFEEFEE, 0, 0xFEEFEE, 0)]
        public void ValidLineDirective_PreservesCoordinates(string coordinates, int startLine, int startColumn, int endLine, int endColumn)
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", $".line {coordinates} 'a.cs'\nret")));

            Assert.Equal(new[] { (0, startLine, startColumn, endLine, endColumn, "a.cs") }, Points(pdb, "M"));
        }

        [Theory]
        [InlineData("10,10 : 1,65535", 1, 0, 65534)]
        [InlineData("10,11 : 65535,65535", 65535, 1, 0)]
        public void MaximumColumn_IsEncodedWithoutClamping(string coordinates, int startColumn, int deltaLines, int deltaColumns)
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", $".line {coordinates} 'a.cs'\nret")));
            MethodDebugInformation info = pdb.GetDebugInformation("M");
            Assert.Equal("a.cs", pdb.GetDocumentName(info.Document));

            // Read the encoding directly because the SDK sequence-point reader rejects legal maximum columns.
            BlobReader blob = pdb.Pdb.GetBlobReader(info.SequencePointsBlob);
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(deltaLines, blob.ReadCompressedInteger());
            Assert.Equal(deltaColumns, deltaLines == 0
                ? blob.ReadCompressedInteger()
                : blob.ReadCompressedSignedInteger());
            Assert.Equal(10, blob.ReadCompressedInteger());
            Assert.Equal(startColumn, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.RemainingBytes);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InvalidLineDirective_WithErrorTolerance_OmitsOnlyItsSequencePoints(bool errorTolerant)
        {
            string source = Program(Method("M", """
                .line 10,10 : 1,2 'a.cs'
                nop
                .line 20,19 : 1,2 'a.cs'
                nop
                nop
                .line 30,30 : 3,4 'a.cs'
                ret
                """));
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { Debug = true, ErrorTolerant = errorTolerant });
            Assert.Equal("ILA0061", Assert.Single(diagnostics).Id);
            if (errorTolerant)
            {
                using var pdb = new PortablePdbTestReader(Assert.IsType<CompilationResult>(result));
                Assert.Equal(new[] { (0, 10, 1, 10, 2, "a.cs"), (3, 30, 3, 30, 4, "a.cs") }, Points(pdb, "M"));
            }
            else
            {
                Assert.Null(result);
            }
        }

        [Fact]
        public void InvalidLineDirective_WithoutAPdb_DoesNotAffectAssembly()
        {
            string source = Program(Method("M", ".line 10,9 : 70000,0 'a.cs'\nret"));
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options());

            Assert.Null(result.PortablePdb);
        }

        [Theory]
        [InlineData(true, null, false)]
        [InlineData(false, DebugMode.Impl, false)]
        [InlineData(false, DebugMode.Opt, false)]
        [InlineData(false, null, true)]
        public void InvalidLineDirective_IsValidatedForEveryPdbSwitch(bool debug, DebugMode? debugMode, bool pdbSwitch)
        {
            string source = Program(".line 10,9 : 1,2 'a.cs'\n" + Method("M", "ret"));
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { Debug = debug, DebugMode = debugMode, Pdb = pdbSwitch });

            Assert.Equal("ILA0061", Assert.Single(diagnostics).Id);
            Assert.Null(result);
        }

        [Fact]
        public void InvalidHashLineDirective_IsValidated()
        {
            string source = Program(Method("M", "#line 10,9 : 1,2 'a.cs'\nret"));
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { Debug = true });

            Diagnostic error = Assert.Single(diagnostics);
            Assert.Equal("ILA0061", error.Id);
            Assert.Equal(source.IndexOf("#line", StringComparison.Ordinal), error.Location.Span.Start);
            Assert.Null(result);
        }

        [Fact]
        public void InvalidLineDirectiveInAnInclude_ReportsTheIncludedSource()
        {
            const string IncludedSource = ".line 10,9 : 1,2 'a.cs'\nnop";
            string source = Program(Method("M", "#include \"inc.il\"\nret"));
            var (diagnostics, result) = new DocumentCompiler().Compile(
                new SourceText(source, "test.il"),
                path =>
                {
                    Assert.Equal("inc.il", path);
                    return new SourceText(IncludedSource, path);
                },
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { Debug = true });

            Diagnostic error = Assert.Single(diagnostics);
            Assert.Equal("ILA0061", error.Id);
            Assert.Equal("inc.il", error.Location.Source.Path);
            Assert.Equal(IncludedSource, error.Location.Source.Text);
            Assert.Equal(0, error.Location.Span.Start);
            Assert.Equal(5, error.Location.Span.Length);
            Assert.Null(result);
        }

        [Fact]
        public void AutoIncrementDirective_DoubleQuotedLineOnlyFormAnchorsTheNextLine()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    #line 0 "a.cs"
                    nop nop

                    ret
            """)));

            Assert.Equal(new[] { (0, 0, 0, 0, 65534, "a.cs"), (2, 2, 0, 2, 65534, "a.cs") }, Points(pdb, "M"));
        }

        [Theory]
        [InlineData("#line 10 'a.cs'", 0, 65534)]
        [InlineData("#line 10", 0, 65534)]
        [InlineData("#line 10 : 2 'a.cs'", 2, 65534)]
        [InlineData("#line 10,10 : 2,4 'a.cs'", 2, 4)]
        public void AutoIncrementDirective_OtherFormsAnchorTheDirectiveLine(string directive, int startColumn, int endColumn)
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", $"{directive}\nnop\nret")));
            string document = directive.Contains("a.cs", StringComparison.Ordinal) ? "a.cs" : "test.il";

            Assert.Equal(
                new[] { (0, 11, startColumn, 11, endColumn, document), (1, 12, startColumn, 12, endColumn, document) },
                Points(pdb, "M"));
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void AutoIncrementDirective_ShiftsBothLinesByPhysicalSourceLines(string lineEnding)
        {
            string source = Program(Method("M", """
                    #line 100,102 : 7,3 'a.cs' nop nop
                    nop

                    // a comment
                    /* two
                       lines */
                    ret
            """)).ReplaceLineEndings(lineEnding);
            using var pdb = PortablePdbTestReader.Compile(source);

            Assert.Equal(
                new[] { (0, 100, 7, 102, 3, "a.cs"), (2, 101, 7, 103, 3, "a.cs"), (3, 106, 7, 108, 3, "a.cs") },
                Points(pdb, "M"));
        }

        [Fact]
        public void AutoIncrementDirective_ResetsAtDirectivesAndKeepsTheCurrentDocument()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    #line 10,10 : 1,3 'a.cs'
                    nop
                    .line 20,20 : 2,4 ''
                    nop
                    nop
                    #line 30,31 : 3,5
                    nop
                    #line 40,40 : 4,6 'b.cs'
                    ret
            """)));

            Assert.Equal(
                new[] { (0, 11, 1, 11, 3, "a.cs"), (1, 20, 2, 20, 4, "a.cs"), (3, 31, 3, 32, 5, "a.cs"), (4, 41, 4, 41, 6, "b.cs") },
                Points(pdb, "M"));
        }

        [Fact]
        public void AutoIncrementDirective_ContinuesAcrossMethods()
        {
            string methods =
                "    #line 50,52 : 1,3 'a.cs'\n" +
                Method("A", "        nop\n        ret\n") + "\n" +
                Method("B", "        nop\n        ret\n");
            string source = Program(methods);
            int directiveLine = LineOf(source, "#line");
            using var pdb = PortablePdbTestReader.Compile(source);

            foreach (string name in new[] { "A", "B" })
            {
                int firstLine = LineOf(source, $"void {name}") + 2 - directiveLine;
                Assert.Equal(
                    new[] { (0, 50 + firstLine, 1, 52 + firstLine, 3, "a.cs"), (1, 51 + firstLine, 1, 53 + firstLine, 3, "a.cs") },
                    Points(pdb, name));
            }
        }

        [Fact]
        public void AutoIncrementDirective_IncludeStatesAreIndependent()
        {
            using var pdb = CompileWithIncludes(
                Program(Method("M", """
                    #line 10,11 : 1,3 'outer.cs'
                    nop
            #include "inc.il"
                    ret
            """)),
                ("inc.il", "#line 30,32 : 2,4 'inner.cs'\nnop\nnop\n"));

            Assert.Equal(
                new[] { (0, 11, 1, 12, 3, "outer.cs"), (1, 31, 2, 33, 4, "inner.cs"), (2, 32, 2, 34, 4, "inner.cs"), (3, 13, 1, 14, 3, "outer.cs") },
                Points(pdb, "M"));
        }

        [Fact]
        public void AutoIncrementDirective_MacroInstructionsUseTheInvocationLine()
        {
            string source = "#define TWO \"nop nop\"\n" + Program(Method("M", """
                    #line 10,10 : 1,3 'a.cs'
                    TWO

                    ret
            """));
            using var pdb = PortablePdbTestReader.Compile(source);

            Assert.Equal(new[] { (0, 11, 1, 11, 3, "a.cs"), (2, 13, 1, 13, 3, "a.cs") }, Points(pdb, "M"));
        }

        [Fact]
        public void AutoIncrementDirective_HiddenMarkerRemainsHiddenUntilTheNextDirective()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    #line 0xFEEFEE,0xFEEFEE : 0,0 'a.cs'
                    nop
                    nop
                    #line 10,10 : 1,3 ''
                    ret
            """)));

            Assert.Equal(
                new[] { (0, SequencePoint.HiddenLine, 0, SequencePoint.HiddenLine, 0, "a.cs"), (2, 11, 1, 11, 3, "a.cs") },
                Points(pdb, "M"));
        }

        [Fact]
        public void AutoIncrementDirective_WithoutAPdbDoesNotValidateEffectiveCoordinates()
        {
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(
                Program(Method("M", "#line 0x1fffffff,0x1fffffff : 1,3 'a.cs'\nret")), new Options());

            Assert.Null(result.PortablePdb);
        }

        [Fact]
        public void AutoIncrementDirective_LastRepresentableLineIsEmitted()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    #line 0x1ffffffd,0x1ffffffe : 1,3 'a.cs'
                    ret
            """)));

            MethodDebugInformation info = pdb.GetDebugInformation("M");
            Assert.Equal("a.cs", pdb.GetDocumentName(info.Document));

            // Read the encoding directly because the SDK sequence-point reader rejects this legal line range.
            BlobReader blob = pdb.Pdb.GetBlobReader(info.SequencePointsBlob);
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(2, blob.ReadCompressedSignedInteger());
            Assert.Equal(0x1ffffffe, blob.ReadCompressedInteger());
            Assert.Equal(1, blob.ReadCompressedInteger());
            Assert.Equal(0, blob.RemainingBytes);
        }

        [Theory]
        [InlineData("0x1ffffffe,0x1fffffff : 1,3")]
        [InlineData("0x7fffffff,0x7fffffff : 1,3")]
        [InlineData("16707565,16707565 : 1,3")]
        public void AutoIncrementDirective_InvalidEffectiveRangeIsReportedAtItsDirective(string coordinates)
        {
            string source = Program(Method("M", $$"""
                    #line {{coordinates}} 'a.cs'
                    nop
                    .line 10,10 : 1,3 'b.cs'
                    ret
            """));
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { Debug = true, ErrorTolerant = true });
            Diagnostic diagnostic = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.InvalidSequencePoint, diagnostic.Id);
            Assert.Equal(source.IndexOf("#line", StringComparison.Ordinal), diagnostic.Location.Span.Start);
            Assert.NotNull(result);
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(new[] { (1, 10, 1, 10, 3, "b.cs") }, Points(pdb, "M"));
        }

        [Fact]
        public void MethodWithoutLineDirectives_HasAPointOnTheSourceLineOfEachInstruction()
        {
            string source = Program(Method("M", """
                    ldc.i4.1 // first

                    // a comment line
                    ldc.i4.2 // second
                    add // third
                    pop // fourth
                    ret // fifth
            """));
            using var pdb = PortablePdbTestReader.Compile(source);

            Assert.Equal(
                new[]
                {
                    OnLine(0, LineOf(source, "// first")),
                    OnLine(1, LineOf(source, "// second")),
                    OnLine(2, LineOf(source, "// third")),
                    OnLine(3, LineOf(source, "// fourth")),
                    OnLine(4, LineOf(source, "// fifth")),
                },
                Points(pdb, "M"));
            Assert.Equal("test.il", pdb.GetMethodDocumentName("M"));
        }

        [Fact]
        public void InstructionsOnOneLine_ShareOnePoint()
        {
            string source = Program(Method("M", """
                    ldc.i4.1 ldc.i4.2 add // one line
                    pop // next line
                    ret // last line
            """));
            using var pdb = PortablePdbTestReader.Compile(source);

            Assert.Equal(
                new[] { OnLine(0, LineOf(source, "// one line")), OnLine(3, LineOf(source, "// next line")), OnLine(4, LineOf(source, "// last line")) },
                Points(pdb, "M"));
        }

        [Theory]
        [InlineData(true, null, false)]
        [InlineData(false, DebugMode.Impl, false)]
        [InlineData(false, DebugMode.Opt, false)]
        [InlineData(false, null, true)]
        public void EachSwitchThatProducesAPdb_GivesTheSourceLinePoints(bool debug, DebugMode? debugMode, bool pdbSwitch)
        {
            string source = Program(Method("M", """
                    nop // first
                    ret // second
            """));
            using var pdb = new PortablePdbTestReader(DocumentCompilerTestHelpers.CompileAndGetResult(
                source, new Options { Debug = debug, DebugMode = debugMode, Pdb = pdbSwitch }));

            Assert.Equal(new[] { OnLine(0, LineOf(source, "// first")), OnLine(1, LineOf(source, "// second")) }, Points(pdb, "M"));
        }

        [Fact]
        public void LineDirectiveInABody_GivesTheInstructionsAfterItItsCoordinates()
        {
            string source = Program(Method("M", """
                    nop // before
                    .line 100,100 : 3,7 'a.cs'
                    nop
                    nop
                    ret
            """));
            using var pdb = PortablePdbTestReader.Compile(source);

            Assert.Equal(new[] { OnLine(0, LineOf(source, "// before")), (1, 100, 3, 100, 7, "a.cs") }, Points(pdb, "M"));
        }

        [Fact]
        public void LineDirectiveBeforeTheFirstInstruction_GivesItItsCoordinates()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 100,100 : 3,7 'a.cs'
                    nop
                    ret
            """)));

            Assert.Equal(new[] { (0, 100, 3, 100, 7, "a.cs") }, Points(pdb, "M"));
        }

        [Fact]
        public void InstructionAfterALineDirective_GetsAPointEvenWithTheCoordinatesOfTheLastPoint()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(Method("M", """
                    .line 5,5 : 1,2 'a.cs'
                    nop
                    .line 5,5 : 1,2 'a.cs'
                    nop
                    ret
            """)));

            Assert.Equal(new[] { (0, 5, 1, 5, 2, "a.cs"), (1, 5, 1, 5, 2, "a.cs") }, Points(pdb, "M"));
        }

        [Fact]
        public void LineDirectiveAfterTheLastInstruction_GivesItsMethodNoPoint()
        {
            string source = Program(
                Method("A", """
                    nop // a1
                    ret // a2
                    .line 50,50 : 1,3 'a.cs'
            """) +
                Method("B", """
                    nop
                    ret
            """));
            using var pdb = PortablePdbTestReader.Compile(source);

            Assert.Equal(new[] { OnLine(0, LineOf(source, "// a1")), OnLine(1, LineOf(source, "// a2")) }, Points(pdb, "A"));
        }

        [Fact]
        public void LineDirectiveAfterTheLastInstruction_AppliesToTheFirstInstructionOfTheNextMethod()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(
                Method("A", """
                    nop
                    ret
                    .line 50,50 : 1,3 'a.cs'
            """) +
                Method("B", """
                    nop
                    ret
            """)));

            Assert.Equal(new[] { (0, 50, 1, 50, 3, "a.cs") }, Points(pdb, "B"));
        }

        [Fact]
        public void ClassLevelLineDirective_AppliesToTheFirstInstructionOfTheNextMethod()
        {
            using var pdb = PortablePdbTestReader.Compile(Program(
                "    .line 7,7 : 3,4 'c.cs'\n" +
                Method("A", """
                    nop
                    ret
            """)));

            Assert.Equal(new[] { (0, 7, 3, 7, 4, "c.cs") }, Points(pdb, "A"));
        }

        [Fact]
        public void LaterMethodWithoutALineDirective_SharesTheLastPoint()
        {
            // Each instruction's coordinates are compared with those of the last point recorded, in any method,
            // and only a directive resets that. B, after A's .line and without one of its own, maps to the same
            // coordinates and so gets no point; ildasm writes this shape for a method that had no sequence points,
            // when it writes .line directives at all.
            using var pdb = PortablePdbTestReader.Compile(Program(
                Method("A", """
                    .line 5,5 : 1,9 'a.cs'
                    nop
                    ret
            """) +
                Method("B", """
                    nop
                    nop
                    ret
            """)));

            MethodDebugInformation debugInformation = pdb.GetDebugInformation("B");
            Assert.Equal(new[] { (0, 5, 1, 5, 9, "a.cs") }, Points(pdb, "A"));
            Assert.True(debugInformation.Document.IsNil);
            Assert.True(debugInformation.SequencePointsBlob.IsNil);
        }

        [Fact]
        public void HiddenLineDirective_HidesTheInstructionsAfterIt()
        {
            string source = Program(Method("M", """
                    nop // shown
                    .line 16707566,16707566 : 0,0 ''
                    nop
                    nop
                    ret
            """));
            using var pdb = PortablePdbTestReader.Compile(source);

            SequencePoint[] points = pdb.GetSequencePoints("M");
            Assert.Equal(new[] { (0, false), (1, true) }, points.Select(point => (point.Offset, point.IsHidden)));
            Assert.Equal(LineOf(source, "// shown"), points[0].StartLine);
        }

        [Fact]
        public void PointsOnTheSourceThenOnALineDirectivesFile_HaveANilMethodDocumentAndTheInputFileAsInitialDocument()
        {
            string source = Program(Method("F", """
                    ldc.i4.1 // x1
                    pop // x2
                    .line 100,100 : 1,10 'ext.cs'
                    ldc.i4.1
                    pop
                    .line 101,101 : 1,10 ''
                    ret
            """));
            using var pdb = PortablePdbTestReader.Compile(source);

            Assert.Null(pdb.GetMethodDocumentName("F"));
            Assert.Equal(pdb.GetDocumentRowNumber("test.il"), pdb.ReadBlobHeader("F").InitialDocument);
            Assert.Equal(
                new[] { OnLine(0, LineOf(source, "// x1")), OnLine(1, LineOf(source, "// x2")), (2, 100, 1, 100, 10, "ext.cs"), (4, 101, 1, 101, 10, "ext.cs") },
                Points(pdb, "F"));
        }

        [Fact]
        public void InstructionsInAnIncludedFile_AreOnTheirLinesOfTheIncludedFile()
        {
            string source = Program(Method("M", """
                    nop // before
            #include "inc.il"
                    nop // after
                    ret // last
            """));
            using var pdb = CompileWithIncludes(source, ("inc.il", "ldc.i4.1\npop\n"));

            Assert.Equal(
                new[]
                {
                    OnLine(0, LineOf(source, "// before")),
                    OnLine(1, 1, "inc.il"),
                    OnLine(2, 2, "inc.il"),
                    OnLine(3, LineOf(source, "// after")),
                    OnLine(4, LineOf(source, "// last")),
                },
                Points(pdb, "M"));
        }

        [Fact]
        public void IncludedFile_IsADocumentAfterTheInputFile()
        {
            using var pdb = CompileWithIncludes(
                Program(Method("M", """
                    nop
            #include "inc.il"
                    ret
            """)),
                ("inc.il", "ldc.i4.1\npop\n"));

            Assert.Equal(new[] { "test.il", "inc.il" }, pdb.DocumentNames);
        }

        [Fact]
        public void IncludedFileWithoutAPointOfItsOwn_IsNotADocument()
        {
            // Every instruction of the included file is under its .line directive, so no point is in the file itself.
            using var pdb = CompileWithIncludes(
                Program(Method("M", """
                    nop
            #include "inc.il"
                    ret
            """)),
                ("inc.il", ".line 70,70 : 1,2 'x.cs'\nldc.i4.1\npop\n"));

            Assert.Equal(new[] { "test.il", "x.cs" }, pdb.DocumentNames);
        }

        [Fact]
        public void LineDirectiveInAnIncludedFile_DoesNotApplyAfterTheInclude()
        {
            string source = Program(Method("M", """
                    nop // before
            #include "inc.il"
                    nop // after
                    ret // last
            """));
            using var pdb = CompileWithIncludes(source, ("inc.il", ".line 70,70 : 1,2 'x.cs'\nldc.i4.1\npop\n"));

            Assert.Equal(
                new[] { OnLine(0, LineOf(source, "// before")), (1, 70, 1, 70, 2, "x.cs"), OnLine(3, LineOf(source, "// after")), OnLine(4, LineOf(source, "// last")) },
                Points(pdb, "M"));
        }

        [Fact]
        public void LineDirectiveBeforeAnInclude_DoesNotApplyInTheIncludedFile()
        {
            string source = Program(Method("M", """
                    .line 40,40 : 1,5 'outer.cs'
                    nop
            #include "inc.il"
                    ret
            """));
            using var pdb = CompileWithIncludes(source, ("inc.il", "ldc.i4.1\npop\n"));

            Assert.Equal(
                new[] { (0, 40, 1, 40, 5, "outer.cs"), OnLine(1, 1, "inc.il"), OnLine(2, 2, "inc.il"), (3, 40, 1, 40, 5, "outer.cs") },
                Points(pdb, "M"));
        }

        [Fact]
        public void SecondInputFile_StartsWithoutTheLineDirectiveOfTheFirst()
        {
            string first = Program(Method("A", """
                    .line 5,5 : 1,2 'a.cs'
                    nop
                    ret
            """));
            string second = """
                .class public auto ansi beforefieldinit Test2
                {
                    .method public static void B() cil managed
                    {
                        nop // b1
                        ret // b2
                    }
                }
                """;
            using var pdb = Compile(new Options { Debug = true }, [new SourceText(first, "test.il"), new SourceText(second, "second.il")]);

            Assert.Equal(new[] { OnLine(0, LineOf(second, "// b1"), "second.il"), OnLine(1, LineOf(second, "// b2"), "second.il") }, Points(pdb, "B"));
        }

        [Fact]
        public void InstructionOnTheLineOfTheLastPointButInAnotherInputFile_GetsAPoint()
        {
            // An instruction's span includes its document, so B's first instruction, on the line of A's last
            // point but in another input file, gets a point.
            string first = Program(Method("A", """
                    nop
                    ret // a-last
            """));
            int line = LineOf(first, "// a-last");
            string second =
                ".class public auto ansi beforefieldinit Test2\n{\n.method public static void B() cil managed\n{\n" +
                new string('\n', line - 5) +
                "ret // b-only\n}\n}\n";
            Assert.Equal(line, LineOf(second, "// b-only"));
            using var pdb = Compile(new Options { Debug = true }, [new SourceText(first, "test.il"), new SourceText(second, "second.il")]);

            Assert.Equal(new[] { OnLine(0, line, "second.il") }, Points(pdb, "B"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Optimize_PointsAreAtTheOffsetsOfTheEmittedForms(bool optimize)
        {
            string source = DocumentCompilerTestHelpers.MethodSource("""
                ldc.i4 0
                ldarg 0
                BACK: nop
                br BACK
                ret
                """);
            using var pdb = new PortablePdbTestReader(DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Optimize = optimize, Debug = true }));

            Assert.Equal(optimize ? new[] { 0, 1, 2, 3, 5 } : new[] { 0, 5, 9, 10, 15 }, pdb.GetSequencePoints("M").Select(point => point.Offset));
        }

        [Fact]
        public void Deterministic_CompilingTwice_GivesIdenticalPdbs()
        {
            string source = Program(Method("M", """
                    nop
                    ret
            """));
            var options = new Options { Debug = true, Deterministic = true };

            ImmutableArray<byte> first = DocumentCompilerTestHelpers.GetPortablePdb(DocumentCompilerTestHelpers.CompileAndGetResult(source, options));
            ImmutableArray<byte> second = DocumentCompilerTestHelpers.GetPortablePdb(DocumentCompilerTestHelpers.CompileAndGetResult(source, options));

            Assert.Equal<byte>(first, second);
        }

        /// <summary>An instruction with a malformed operand for each kind of reference operand an instruction can take.</summary>
        public static TheoryData<string, string> MalformedReferenceOperands => new()
        {
            { "method", "call void Test::M(int32,)" },
            { "field", "ldsfld int32 Test::" },
            { "metadata token", "ldsfld mdtoken()" },
            { "type", "box class Test<int32,>" },
            { "calli signature", "calli void(int32,)" },
            { "owner", "ldtoken method void Test::M(int32,)" },
        };

        private static PortablePdbTestReader CompileWithErrors(string source)
        {
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { Debug = true, ErrorTolerant = true });
            Assert.Contains(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(result);
            return new PortablePdbTestReader(result!);
        }

        [Theory]
        [MemberData(nameof(MalformedReferenceOperands))]
        public void InstructionWithAMalformedOperand_EmitsNothing_AndTheNextInstructionHasThePointAtItsOffset(string kind, string instruction)
        {
            _ = kind;
            string source = Program(Method("M", $$"""
                    nop // n1
                    {{instruction}}
                    nop // n2
                    ret // r
            """));
            using var pdb = CompileWithErrors(source);

            Assert.Equal(
                new[] { OnLine(0, LineOf(source, "// n1")), OnLine(1, LineOf(source, "// n2")), OnLine(2, LineOf(source, "// r")) },
                Points(pdb, "M"));
        }

        [Theory]
        [MemberData(nameof(MalformedReferenceOperands))]
        public void InstructionWithAMalformedOperand_AtTheEndOfABody_GetsNoPoint(string kind, string instruction)
        {
            // The malformed instruction emits nothing, so a point for it would be at the end of the body.
            _ = kind;
            string source = Program(Method("M", $$"""
                    nop // n1
                    {{instruction}}
            """));
            using var pdb = CompileWithErrors(source);

            Assert.Equal(new[] { OnLine(0, LineOf(source, "// n1")) }, Points(pdb, "M"));
        }

        [Fact]
        public void InstructionsFromADefineMacro_AreOnTheLineWhereTheMacroIsUsed()
        {
            string source = "#define NOP \"nop\"\n#define TWO \"nop nop\"\n" + Program(Method("M", """
                    NOP // m1
                    TWO // m2
                    ret // m3
            """));
            using var pdb = PortablePdbTestReader.Compile(source);

            Assert.Equal(
                new[] { OnLine(0, LineOf(source, "// m1")), OnLine(1, LineOf(source, "// m2")), OnLine(3, LineOf(source, "// m3")) },
                Points(pdb, "M"));
        }

        [Fact]
        public void IncludedFile_IsADocumentWithTheLanguageInEffectAtItsFirstPoint()
        {
            using var pdb = CompileWithIncludes(
                Program("    .language '3f5162f8-07c6-11d3-9053-00c04fa302a1'\n#include \"m.il\"\n"),
                ("m.il", ".method public static void A() cil managed\n{\nnop\nret\n}\n"));

            Assert.Equal(new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1"), pdb.GetDocumentLanguage("m.il"));
        }
    }
}
