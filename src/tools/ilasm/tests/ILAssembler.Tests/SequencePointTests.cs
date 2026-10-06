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
    /// The sequence points of method bodies, read back from the Portable PDB: each instruction is mapped when it is
    /// emitted, to its own line of the <c>.il</c> source (columns 1 to 2) when no <c>.line</c> directive is in effect,
    /// as native ilasm does, and to the coordinates of the directive in effect otherwise; consecutive instructions
    /// with the same coordinates share a point.
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

        /// <summary>The point native ilasm gives an instruction on <paramref name="line"/> of <paramref name="document"/> with no <c>.line</c> in effect.</summary>
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
        public void LaterMethodWithoutALineDirective_SharesTheLastPoint_AsInNativeIlasm()
        {
            // Native ilasm compares each instruction's coordinates with the last point it recorded, in any method,
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
            // Native ilasm compares only lines and columns, so it would give B's first instruction no point.
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
