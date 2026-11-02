// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

namespace ILAssembler.Tests
{
    /// <summary>
    /// The Portable PDB LocalScope and LocalVariable rows of named locals (docs/design/specs/PortablePdb-Metadata.md,
    /// "LocalScope Table" and "LocalVariable Table"), read back with System.Reflection.Metadata. Instructions are
    /// written in their long forms unless a test says otherwise, so the offsets in the comments are exact.
    /// </summary>
    public class LocalScopeTests
    {
        /// <summary>
        /// Gets the method's LocalScope rows in table order, each as <c>start-end: name=index ...</c> with the
        /// LocalVariable rows the scope owns.
        /// </summary>
        internal static string[] Scopes(PortablePdbTestReader pdb, string methodName = "M")
            => pdb.Pdb.GetLocalScopes(pdb.GetMethodHandle(methodName))
                .Select(pdb.Pdb.GetLocalScope)
                .Select(scope => $"{scope.StartOffset}-{scope.EndOffset}: " + string.Join(" ", scope.GetLocalVariables()
                    .Select(pdb.Pdb.GetLocalVariable)
                    .Select(variable => $"{pdb.Pdb.GetString(variable.Name)}={variable.Index}")))
                .ToArray();

        private static PortablePdbTestReader Compile(string body, Options? options = null)
            => PortablePdbTestReader.Compile(LocalTests.OneMethod(body), options);

        /// <summary>
        /// Compiles a program on its own (a compilation result can be serialized only once, so not the one a
        /// <see cref="PortablePdbTestReader"/> has read) and gets the IL of method <c>M</c>.
        /// </summary>
        private static byte[] CompileAndGetIL(string source, Options options)
        {
            (_, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, options);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result!));
            return DocumentCompilerTestHelpers.GetMethodBody(pe).GetILBytes()!;
        }

        /// <summary>
        /// Compiles a program on its own, as <see cref="CompileAndGetIL"/> does, and gets the local-variable
        /// instructions of method <c>M</c> (<see cref="LocalTests.GetLocalOperands"/>).
        /// </summary>
        private static string[] CompileAndGetLocalOperands(string source, Options options)
        {
            (_, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, options);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result!));
            return LocalTests.GetLocalOperands(pe);
        }

        [Fact]
        public void RootScope_SpansTheBodyAndListsTheMethodLevelNamedLocals()
        {
            using var pdb = Compile("""
                .locals init (int32 a, string b)
                nop
                ret
                """);

            Assert.Equal(["0-2: a=0 b=1"], Scopes(pdb));
        }

        [Fact]
        public void NestedBlock_SpansTheOffsetsOfItsBraces()
        {
            using var pdb = Compile("""
                .locals init (int32 a)
                nop
                {
                    .locals init (int32 b)
                    nop
                    nop
                }
                ret
                """);

            Assert.Equal(["0-4: a=0", "1-3: b=1"], Scopes(pdb));
        }

        [Fact]
        public void SiblingBlocks_AreDisjointScopes()
        {
            using var pdb = Compile("""
                nop
                {
                    .locals init (int32 x)
                    nop
                }
                nop
                {
                    .locals init (int32 y)
                    nop
                    nop
                }
                ret
                """);

            Assert.Equal(["1-2: x=0", "3-5: y=1"], Scopes(pdb));
        }

        [Fact]
        public void Scopes_AreSortedByStartOffsetThenLongestFirst()
        {
            // The blocks close in the order c, b, d, a, root; the table must not follow that order.
            using var pdb = Compile("""
                .locals init (int32 r)
                {
                    .locals init (int32 a)
                    {
                        .locals init (int32 b)
                        nop
                        {
                            .locals init (int32 c)
                            nop
                        }
                    }
                    {
                        .locals init (int32 d)
                        nop
                    }
                }
                ret
                """);

            Assert.Equal(["0-4: r=0", "0-3: a=1", "0-2: b=2", "1-2: c=3", "2-3: d=4"], Scopes(pdb));
        }

        [Fact]
        public void ScopesWithTheSameRange_EnclosingScopeComesFirst()
        {
            using var pdb = Compile("""
                {
                    .locals init (int32 outer)
                    {
                        .locals init (int32 inner)
                        nop
                    }
                }
                ret
                """);

            Assert.Equal(["0-1: outer=0", "0-1: inner=1"], Scopes(pdb));
        }

        [Fact]
        public void UnnamedLocal_HasNoVariableRow()
        {
            using var pdb = Compile("""
                .locals init (int32, string b)
                nop
                ret
                """);

            Assert.Equal(["0-2: b=1"], Scopes(pdb));
        }

        [Fact]
        public void BlockWithOnlyUnnamedLocals_HasNoScopeRow()
        {
            using var pdb = Compile("""
                .locals init (int32 a)
                {
                    .locals init (int32, string)
                    nop
                }
                ret
                """);

            Assert.Equal(["0-2: a=0"], Scopes(pdb));
        }

        [Fact]
        public void MethodWithoutNamedLocalsAtTheTop_HasOnlyTheBlockScope()
        {
            // The root scope gets a row only when the method-level .locals name a local.
            using var pdb = Compile("""
                nop
                {
                    .locals init (int32 b)
                    nop
                }
                ret
                """);

            Assert.Equal(["1-2: b=0"], Scopes(pdb));
        }

        /// <summary>
        /// A block that declares <c>z</c> and <c>y</c> and has no instructions. Its LocalScope row would have length 0,
        /// and a row's Length is in (0..0x80000000).
        /// </summary>
        private static readonly string s_blockWithoutInstructions = LocalTests.OneMethod("""
            .locals init (int32 a)
            nop
            {
                .locals init (int32 z, int32 y)
            }
            ret
            """);

        [Fact]
        public void BlockWithoutInstructions_IsReportedAtItsFirstNamedLocal()
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(s_blockWithoutInstructions, new Options { Debug = true });

            Diagnostic error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LocalScopeWithoutInstructions, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            AssertAtLocalName(error, s_blockWithoutInstructions, "int32 z", "z");
            Assert.Null(result);
        }

        [Fact]
        public void BlockWithoutInstructions_WithErrorTolerance_HasARowOfLengthZero()
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                s_blockWithoutInstructions,
                new Options { Debug = true, ErrorTolerant = true });
            Assert.Equal(DiagnosticIds.LocalScopeWithoutInstructions, Assert.Single(diagnostics).Id);
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(["0-2: a=0", "1-1: z=1 y=2"], Scopes(pdb));
        }

        [Fact]
        public void ScopesWithoutInstructionsAndRepeatedNamesAndSlots_WithoutAPdb_AreNotReported()
        {
            // Scopes are recorded only for a PDB, so without one none of this is checked and the image is written.
            // Only the slot's in-use warning, which does not depend on a PDB, is reported.
            string source = LocalTests.OneMethod("""
                .locals init (int32 a, string a)
                nop
                {
                    .locals init (int32 z, [2] int32 y)
                }
                ret
                """);

            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options());

            Assert.Equal(DiagnosticIds.LocalSlotInUse, Assert.Single(diagnostics).Id);
            Assert.NotNull(result);

            (diagnostics, _) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { Debug = true });
            Assert.Equal(
                [
                    DiagnosticIds.LocalSlotInUse,
                    DiagnosticIds.LocalNameDeclaredTwiceInScope,
                    DiagnosticIds.LocalScopeWithoutInstructions,
                    DiagnosticIds.LocalSlotNamedTwiceInScope,
                ],
                diagnostics.Select(diagnostic => diagnostic.Id));
        }

        [Fact]
        public void TryAndCatchBodies_AreScopes()
        {
            using var pdb = Compile("""
                .locals init (int32 a)
                .try
                {
                    .locals init (int32 t)
                    nop
                    leave.s DONE
                }
                catch [System.Runtime]System.Object
                {
                    .locals init (object e)
                    stloc e
                    leave.s DONE
                }
                DONE: ret
                """);

            Assert.Equal(["0-10: a=0", "0-3: t=1", "3-9: e=2"], Scopes(pdb));
        }

        [Fact]
        public void FinallyBody_IsAScope()
        {
            using var pdb = Compile("""
                .try
                {
                    nop
                    leave.s DONE
                }
                finally
                {
                    .locals init (int32 f)
                    nop
                    endfinally
                }
                DONE: ret
                """);

            Assert.Equal(["3-5: f=0"], Scopes(pdb));
        }

        [Fact]
        public void FilterAndHandlerBodies_AreScopes()
        {
            using var pdb = Compile("""
                .try
                {
                    nop
                    leave.s DONE
                }
                filter
                {
                    .locals init (object x)
                    stloc x
                    ldc.i4.1
                    endfilter
                }
                {
                    .locals init (object h)
                    stloc h
                    leave.s DONE
                }
                DONE: ret
                """);

            Assert.Equal(["3-10: x=0", "10-16: h=1"], Scopes(pdb));
        }

        [Fact]
        public void BlockInsideATryBody_IsAScope()
        {
            using var pdb = Compile("""
                .try
                {
                    nop
                    {
                        .locals init (int32 n)
                        nop
                    }
                    leave.s DONE
                }
                catch [System.Runtime]System.Object
                {
                    pop
                    leave.s DONE
                }
                DONE: ret
                """);

            Assert.Equal(["1-2: n=0"], Scopes(pdb));
        }

        [Fact]
        public void SlotGapReusedSlotAndShadowedNameInTryAndCatchBodies_GiveARowForEachScope()
        {
            // Slot 1 is left open at the top, then declared by the .try body and again by the catch body;
            // the unnamed local takes slot 3 and gets no row; the catch body's LOCAL_0 is a new local, at
            // slot 4, and its row is in the catch scope.
            string program = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public abstract auto ansi sealed beforefieldinit Test extends [System.Runtime]System.Object
                {
                    .method public hidebysig static int32 Foo(int32 a) cil managed
                    {
                        .locals init ([0] int32 LOCAL_0, [2] int32 LOCAL_2, int32)
                        nop                      // 0x00
                        ldarg.0                  // 0x01
                        stloc LOCAL_0            // 0x02
                        .try
                        {
                            .locals ([1] string LOCAL_1)
                            ldstr "try"          // 0x06
                            stloc LOCAL_1        // 0x0b
                            leave.s DONE         // 0x0f
                        }
                        catch [System.Runtime]System.Object
                        {
                            .locals ([1] string LOCAL_1B, int32 LOCAL_0)
                            pop                  // 0x11
                            ldc.i4.7             // 0x12
                            stloc LOCAL_0        // 0x13
                            ldloc LOCAL_0        // 0x17
                            stloc LOCAL_2        // 0x1b
                            leave.s DONE         // 0x1f
                        }
                        DONE: ldloc LOCAL_0      // 0x21
                        ldloc LOCAL_2            // 0x25
                        add                      // 0x29
                        ret                      // 0x2a
                    }
                }
                """;

            using var pdb = PortablePdbTestReader.Compile(program);

            Assert.Equal(
                ["0-43: LOCAL_0=0 LOCAL_2=2", "6-17: LOCAL_1=1", "17-33: LOCAL_1B=1 LOCAL_0=4"],
                Scopes(pdb, "Foo"));
            Assert.Equal(3, pdb.Pdb.GetTableRowCount(TableIndex.LocalScope));
            Assert.Equal(5, pdb.Pdb.GetTableRowCount(TableIndex.LocalVariable));
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(
                DocumentCompilerTestHelpers.CompileAndGetResult(program, new Options { Debug = true })));
            Assert.Equal(["int32", "string", "int32", "int32", "int32"], LocalTests.GetLocalTypes(pe, "Foo"));
        }

        [Fact]
        public void EachScopeOwnsExactlyItsVariables_AcrossMethods()
        {
            string program = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test extends [System.Runtime]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32 a, int32 b)
                        nop
                        {
                            .locals init (string c, string d)
                            nop
                        }
                        ret
                    }
                    .method public static void N() cil managed
                    {
                        .locals init (int64 e)
                        nop
                        {
                            .locals init (object f, object g, object h)
                            nop
                        }
                        ret
                    }
                }
                """;

            using var pdb = PortablePdbTestReader.Compile(program);

            Assert.Equal(["0-3: a=0 b=1", "1-2: c=2 d=3"], Scopes(pdb, "M"));
            Assert.Equal(["0-3: e=0", "1-2: f=1 g=2 h=3"], Scopes(pdb, "N"));
            Assert.Equal(4, pdb.Pdb.GetTableRowCount(TableIndex.LocalScope));
            Assert.Equal(8, pdb.Pdb.GetTableRowCount(TableIndex.LocalVariable));
        }

        [Fact]
        public void VariableIndex_IsTheExplicitSlot()
        {
            using var pdb = Compile("""
                .locals init ([3] int32 x, [0] int32 y)
                .locals init ([1] string z, [2] string w)
                nop
                ret
                """);

            Assert.Equal(["0-2: x=3 y=0 z=1 w=2"], Scopes(pdb));
        }

        [Fact]
        public void Variables_HaveNoAttributes()
        {
            using var pdb = Compile("""
                .locals init (int32 a)
                {
                    .locals init (int32 b)
                    nop
                }
                ret
                """);

            Assert.All(
                pdb.Pdb.LocalVariables.Select(pdb.Pdb.GetLocalVariable),
                variable => Assert.Equal(LocalVariableAttributes.None, variable.Attributes));
            Assert.Equal(2, pdb.Pdb.LocalVariables.Count);
        }

        /// <summary>
        /// Asserts that a diagnostic is at a local's name: the last <paramref name="name"/>.Length characters of the
        /// only occurrence of <paramref name="declaration"/> in <paramref name="source"/>.
        /// </summary>
        internal static void AssertAtLocalName(Diagnostic diagnostic, string source, string declaration, string name)
        {
            int start = source.IndexOf(declaration, System.StringComparison.Ordinal);
            Assert.True(start >= 0, $"'{declaration}' is not in the source");
            Assert.True(source.IndexOf(declaration, start + 1, System.StringComparison.Ordinal) < 0, $"'{declaration}' is in the source more than once");
            Assert.EndsWith(name, declaration, System.StringComparison.Ordinal);
            Assert.Equal(new SourceSpan(start + declaration.Length - name.Length, name.Length), diagnostic.Location.Span);
        }

        private static readonly string s_nameDeclaredTwice = LocalTests.OneMethod("""
            .locals init (int32 a, string a)
            ret
            """);

        [Fact]
        public void NameDeclaredTwiceInOneScope_IsReportedAtTheSecondDeclaration()
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(s_nameDeclaredTwice, new Options { Debug = true });

            Diagnostic error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LocalNameDeclaredTwiceInScope, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            AssertAtLocalName(error, s_nameDeclaredTwice, "string a", "a");
            Assert.Null(result);
        }

        [Fact]
        public void NameDeclaredTwiceInOneScope_WithErrorTolerance_HasARowForEachDeclaration()
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                s_nameDeclaredTwice,
                new Options { Debug = true, ErrorTolerant = true });
            Assert.Equal(DiagnosticIds.LocalNameDeclaredTwiceInScope, Assert.Single(diagnostics).Id);
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(["0-1: a=0 a=1"], Scopes(pdb));
        }

        private static readonly string s_slotNamedTwice = LocalTests.OneMethod("""
            .locals init ([0] int32 a, [0] int32 b)
            ret
            """);

        [Fact]
        public void SlotNamedTwiceInOneScope_IsReportedAtTheSecondDeclaration()
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(s_slotNamedTwice, new Options { Debug = true });

            // The in-use warning is reported with or without a PDB.
            Assert.Equal([DiagnosticIds.LocalSlotInUse, DiagnosticIds.LocalSlotNamedTwiceInScope], diagnostics.Select(diagnostic => diagnostic.Id));
            Diagnostic error = diagnostics[1];
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Equal("Local var 'b': slot 0 is already named 'a' in this scope", error.Message);
            AssertAtLocalName(error, s_slotNamedTwice, "[0] int32 b", "b");
            Assert.Null(result);
        }

        [Fact]
        public void SlotNamedTwiceInOneScope_WithErrorTolerance_HasARowForEachDeclaration()
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                s_slotNamedTwice,
                new Options { Debug = true, ErrorTolerant = true });
            Assert.Equal([DiagnosticIds.LocalSlotInUse, DiagnosticIds.LocalSlotNamedTwiceInScope], diagnostics.Select(diagnostic => diagnostic.Id));
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(["0-1: a=0 b=0"], Scopes(pdb));
        }

        /// <summary>
        /// A scope where <c>b</c> shares slot 0 with <c>a</c>, a second <c>b</c> has slot 1, and <c>c</c> shares slot
        /// 1 with the second <c>b</c>. The name <c>b</c> refers to slot 0, its first declaration.
        /// </summary>
        private static readonly string s_namesAndSlotsDeclaredTwice = LocalTests.OneMethod("""
            .locals init ([0] int32 a, [0] int32 b, [1] string b, [1] string c)
            ldloc c
            pop
            ldloc b
            pop
            ret
            """);

        [Fact]
        public void NamesAndSlotsDeclaredTwice_EachRepeatIsReportedAtItsDeclaration()
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(s_namesAndSlotsDeclaredTwice, new Options { Debug = true });

            // The in-use warnings come from the declarations; the errors from the PDB rows, in declaration order.
            Assert.Equal(
                [
                    DiagnosticIds.LocalSlotInUse,
                    DiagnosticIds.LocalSlotInUse,
                    DiagnosticIds.LocalSlotNamedTwiceInScope,
                    DiagnosticIds.LocalNameDeclaredTwiceInScope,
                    DiagnosticIds.LocalSlotNamedTwiceInScope,
                ],
                diagnostics.Select(diagnostic => diagnostic.Id));
            AssertAtLocalName(diagnostics[2], s_namesAndSlotsDeclaredTwice, "[0] int32 b", "b");
            AssertAtLocalName(diagnostics[3], s_namesAndSlotsDeclaredTwice, "[1] string b", "b");
            AssertAtLocalName(diagnostics[4], s_namesAndSlotsDeclaredTwice, "[1] string c", "c");
            Assert.Equal("Local var 'c': slot 1 is already named 'b' in this scope", diagnostics[4].Message);
            Assert.Null(result);
        }

        [Fact]
        public void NamesAndSlotsDeclaredTwice_WithErrorTolerance_HaveARowForEachDeclarationAndTheNameRefersToTheFirst()
        {
            var options = new Options { Debug = true, ErrorTolerant = true };
            (_, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(s_namesAndSlotsDeclaredTwice, options);
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(["0-11: a=0 b=0 b=1 c=1"], Scopes(pdb));
            Assert.Equal(["ldloc 1", "ldloc 0"], CompileAndGetLocalOperands(s_namesAndSlotsDeclaredTwice, options));
        }

        [Fact]
        public void OneDeclarationRepeatingANameAndASlot_IsReportedForBoth()
        {
            // The second a is declared by another .locals of the same scope, at the slot of the first.
            string source = LocalTests.OneMethod("""
                .locals init (int32 a)
                .locals init ([0] int32 a)
                ret
                """);

            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { Debug = true });

            Assert.Equal(
                [DiagnosticIds.LocalSlotInUse, DiagnosticIds.LocalNameDeclaredTwiceInScope, DiagnosticIds.LocalSlotNamedTwiceInScope],
                diagnostics.Select(diagnostic => diagnostic.Id));
            AssertAtLocalName(diagnostics[1], source, "[0] int32 a", "a");
            AssertAtLocalName(diagnostics[2], source, "[0] int32 a", "a");
            Assert.Null(result);
        }

        [Fact]
        public void DistinctNamesAndSlotsInEachScope_AreNotReported()
        {
            // A name or a slot may appear again in another scope: a shadows the root's a, and d reuses slot 2 after
            // the block that declared it closed.
            using var pdb = Compile("""
                .locals init (int32 a, string b)
                nop
                {
                    .locals init (int32 a, object c)
                    nop
                }
                {
                    .locals init ([2] int32 d)
                    nop
                }
                ret
                """);

            Assert.Equal(["0-4: a=0 b=1", "1-2: a=2 c=3", "2-3: d=2"], Scopes(pdb));
        }

        [Fact]
        public void UnnamedLocal_DoesNotClaimItsSlot()
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                LocalTests.OneMethod("""
                    .locals init ([0] int32, [0] int32 b)
                    ret
                    """),
                new Options { Debug = true });
            Assert.Equal(DiagnosticIds.LocalSlotInUse, Assert.Single(diagnostics).Id);
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(["0-1: b=0"], Scopes(pdb));
        }

        [Fact]
        public void Sentinel_HasNoRowAndTakesNoIndex()
        {
            using var pdb = Compile("""
                .locals init (int32 a, ..., int32 b)
                ret
                """);

            Assert.Equal(["0-1: a=0 b=1"], Scopes(pdb));
        }

        [Theory]
        [InlineData("debug")]
        [InlineData("pdb")]
        [InlineData("debug-mode impl")]
        [InlineData("debug-mode opt")]
        [InlineData("debug and debug-mode impl")]
        public void EverySwitchThatRequestsAPdb_GivesTheSameScopesAndTheSameIL(string switches)
        {
            Options options = switches switch
            {
                "debug" => new Options { Debug = true },
                "pdb" => new Options { Pdb = true },
                "debug-mode impl" => new Options { DebugMode = DebugMode.Impl },
                "debug-mode opt" => new Options { DebugMode = DebugMode.Opt },
                "debug and debug-mode impl" => new Options { Debug = true, DebugMode = DebugMode.Impl },
                _ => throw new System.ArgumentOutOfRangeException(nameof(switches)),
            };
            string source = LocalTests.OneMethod("""
                .locals init (int32 a)
                nop
                {
                    .locals init (string b)
                    ldloc b
                    pop
                }
                ret
                """);
            using var pdb = PortablePdbTestReader.Compile(source, options);

            Assert.Equal(["0-7: a=0", "1-6: b=1"], Scopes(pdb));
            Assert.Equal(CompileAndGetIL(source, new Options()), CompileAndGetIL(source, options));
        }

        [Fact]
        public void LocalNamesDifferingOnlyInCase_EachHaveARow()
        {
            using var pdb = Compile("""
                .locals init (int32 a, string A)
                nop
                ret
                """);

            Assert.Equal(["0-2: a=0 A=1"], Scopes(pdb));
        }

        private static readonly string s_methodWithoutInstructions = LocalTests.OneMethod("""
            .locals init (int32 a, int32 b)
            """);

        [Fact]
        public void MethodWithoutInstructions_IsReportedAtItsFirstNamedLocal()
        {
            // The root scope of an empty body has length 0, which the specification does not allow.
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) =
                DocumentCompilerTestHelpers.CompileWithDiagnostics(s_methodWithoutInstructions, new Options { Debug = true });

            Diagnostic error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LocalScopeWithoutInstructions, error.Id);
            Assert.Equal("Local var 'a' is declared in a scope that has no instructions; a PDB local scope must not be empty", error.Message);
            AssertAtLocalName(error, s_methodWithoutInstructions, "int32 a", "a");
            Assert.Null(result);
        }

        [Fact]
        public void MethodWithoutInstructions_WithErrorTolerance_HasARowOfLengthZero()
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                s_methodWithoutInstructions,
                new Options { Debug = true, ErrorTolerant = true });
            Assert.Equal(DiagnosticIds.LocalScopeWithoutInstructions, Assert.Single(diagnostics).Id);
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(["0-0: a=0 b=1"], Scopes(pdb));
            Assert.Equal(1, pdb.Pdb.GetTableRowCount(TableIndex.LocalScope));
        }

        [Fact]
        public void NextSlot_AfterSlot65535_HasNoVariableRow()
        {
            // The local that would take slot 65536 is reported and not declared (LocalTests), so error-tolerant
            // output still gets a PDB: the Index column cannot hold 65536.
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                LocalTests.OneMethod("""
                    .locals init ([65535] int32 last, int32 next)
                    ret
                    """),
                new Options { Debug = true, ErrorTolerant = true });
            Assert.Equal(DiagnosticIds.LocalSlotOutOfRange, Assert.Single(diagnostics, diagnostic => diagnostic.Id != DiagnosticIds.UndefinedLocalSlotType).Id);
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(["0-1: last=65535"], Scopes(pdb));
        }

        [Fact]
        public void NextSlot_65535_HasARow()
        {
            (_, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                LocalTests.OneMethod("""
                    .locals init ([65534] int32 a, string b)
                    ldloc b
                    pop
                    ret
                    """),
                new Options { Debug = true, ErrorTolerant = true });
            using var pdb = new PortablePdbTestReader(result!);

            Assert.Equal(["0-6: a=65534 b=65535"], Scopes(pdb));
        }

        [Theory]
        [InlineData(false, new[] { "0-14: a=0", "5-13: b=1" })]
        [InlineData(true, new[] { "0-5: a=0", "2-4: b=1" })]
        public void ScopeOffsets_AreThoseOfTheEmittedInstructions(bool optimize, string[] expected)
        {
            // With optimization, stloc a, ldloc a and stloc b are one-byte instructions.
            using var pdb = Compile(
                """
                .locals init (int32 a)
                ldc.i4.0
                stloc a
                {
                    .locals init (int32 b)
                    ldloc a
                    stloc b
                }
                ret
                """,
                new Options { Debug = true, Optimize = optimize });

            Assert.Equal(expected, Scopes(pdb));
        }

        [Fact]
        public void Deterministic_ProgramWithNestedScopes_GivesIdenticalPdbBytes()
        {
            string source = LocalTests.OneMethod("""
                .locals init (int32 a)
                {
                    .locals init (int32 b, string c)
                    nop
                    {
                        .locals init (object d)
                        nop
                    }
                }
                ret
                """);
            var options = new Options { Debug = true, Deterministic = true };

            ImmutableArray<byte> first = DocumentCompilerTestHelpers.CompileAndGetPortablePdb(source, options);
            ImmutableArray<byte> second = DocumentCompilerTestHelpers.CompileAndGetPortablePdb(source, options);

            Assert.Equal<byte>(first, second);
            using var provider = MetadataReaderProvider.FromPortablePdbImage(first);
            Assert.Equal(3, provider.GetMetadataReader().GetTableRowCount(TableIndex.LocalScope));
        }

        [Fact]
        public void Fold_MethodsSharingABody_EachHaveTheirScopes()
        {
            // The two bodies differ only in their local names, which are not part of the IL or of the local
            // signature, so the bodies fold; each method keeps its own names.
            string program = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test extends [System.Runtime]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .locals init (int32 a)
                        nop
                        {
                            .locals init (int32 b)
                            nop
                        }
                        ret
                    }
                    .method public static void N() cil managed
                    {
                        .locals init (int32 x)
                        nop
                        {
                            .locals init (int32 y)
                            nop
                        }
                        ret
                    }
                }
                """;

            using var pdb = PortablePdbTestReader.Compile(program, new Options { Debug = true, Fold = true });

            Assert.Equal(
                pdb.Image.GetMethodDefinition(pdb.GetMethodHandle("M")).RelativeVirtualAddress,
                pdb.Image.GetMethodDefinition(pdb.GetMethodHandle("N")).RelativeVirtualAddress);
            Assert.Equal(["0-3: a=0", "1-2: b=1"], Scopes(pdb, "M"));
            Assert.Equal(["0-3: x=0", "1-2: y=1"], Scopes(pdb, "N"));
        }
    }
}
