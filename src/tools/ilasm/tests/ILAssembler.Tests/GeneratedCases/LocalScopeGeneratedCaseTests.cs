// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

// The description parameter is not read: it names the generated case in the test's display name.
#pragma warning disable xUnit1026

namespace ILAssembler.Tests.GeneratedCases
{
    /// <summary>
    /// Local slots, lexical name scopes and the errors for LocalScope and LocalVariable rows that the Portable PDB
    /// specification does not allow, checked over the seeded generated methods of
    /// <see cref="LocalScopeCaseGenerator"/> against the generator's model.
    /// </summary>
    public class LocalScopeGeneratedCaseTests
    {
        /// <summary>Gets the errors the generator's model predicts for a case, each as <c>id name</c>, in order.</summary>
        private static string[] PredictedErrors(int index)
            => LocalScopeCaseGenerator.Cases[index].Methods.SelectMany(method => method.Errors).ToArray();

        /// <summary>
        /// Describes a diagnostic as <c>id name</c>, with the source text at its location, which for the errors
        /// of the LocalScope and LocalVariable rows is the name of the local they are reported at.
        /// </summary>
        private static string Describe(Diagnostic diagnostic)
            => $"{diagnostic.Id} {diagnostic.Location.Source.Text.Substring(diagnostic.Location.Span.Start, diagnostic.Location.Span.Length)}";

        /// <summary>
        /// Compiles a case with a PDB and error tolerance, so that a case with errors still has an image, and checks
        /// that the diagnostics are the errors the model predicts.
        /// </summary>
        private static CompilationResult CompileErrorTolerant(int index)
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                LocalScopeCaseGenerator.Cases[index].ToSource(),
                new Options { Debug = true, ErrorTolerant = true });
            Assert.Equal(PredictedErrors(index), diagnostics.Select(Describe));
            return result!;
        }

        private static PEReader Compile(int index)
            => new(DocumentCompilerTestHelpers.Serialize(CompileErrorTolerant(index)));

        [Fact]
        public void Generator_ProducesEveryShape()
        {
            string[] shapes =
            [
                "blocks", "depth1", "depth2", "depth3", "depth4", "duplicateNameInScope", "explicitNext", "fillsPadding",
                "gaps", "namedScopeWithoutInstructions", "padDeclaredAtEnd", "references", "reusesClosedSlot", "shadows",
                "unnamed",
            ];

            Assert.All(shapes, shape => Assert.True(
                LocalScopeCaseGenerator.CasesPerShape.GetValueOrDefault(shape) >= 5,
                $"{shape}: {LocalScopeCaseGenerator.CasesPerShape.GetValueOrDefault(shape)} cases"));
        }

        [Fact]
        public void Generator_HasManyCasesWithAndWithoutErrors()
        {
            // The theories below check both outcomes: an image, and errors with no image. Most cases have an empty
            // block or a repeated name somewhere, so fewer are free of errors.
            int casesWithErrors = Enumerable.Range(0, LocalScopeCaseGenerator.Cases.Length).Count(index => PredictedErrors(index).Length > 0);
            int casesWithoutErrors = LocalScopeCaseGenerator.Cases.Length - casesWithErrors;
            Assert.True(casesWithErrors >= 100 && casesWithoutErrors >= 25, $"{casesWithErrors} cases with errors, {casesWithoutErrors} without");
        }

        [Theory]
        [MemberData(nameof(LocalScopeCaseGenerator.CaseData), MemberType = typeof(LocalScopeCaseGenerator))]
        public void WithAPdb_TheInvalidRowsAreReportedAndACaseWithErrorsHasNoImage(int index, string description)
        {
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                LocalScopeCaseGenerator.Cases[index].ToSource(),
                new Options { Debug = true });

            Assert.Equal(PredictedErrors(index), diagnostics.Select(Describe));
            Assert.All(diagnostics, diagnostic => Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity));
            Assert.Equal(diagnostics.IsEmpty, result is not null);
        }

        [Theory]
        [MemberData(nameof(LocalScopeCaseGenerator.CaseData), MemberType = typeof(LocalScopeCaseGenerator))]
        public void WithoutAPdb_EveryCaseAssemblesWithoutDiagnostics(int index, string description)
        {
            // The scopes are recorded only for a PDB, so their rows are not checked without one.
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                LocalScopeCaseGenerator.Cases[index].ToSource(),
                new Options());

            Assert.Empty(diagnostics);
            Assert.NotNull(result);
        }

        [Theory]
        [MemberData(nameof(LocalScopeCaseGenerator.CaseData), MemberType = typeof(LocalScopeCaseGenerator))]
        public void LocalSignature_HasOneEntryPerSlotWithTheGeneratedType(int index, string description)
        {
            using PEReader pe = Compile(index);
            foreach (GeneratedLocalsMethod method in LocalScopeCaseGenerator.Cases[index].Methods)
            {
                if (method.SlotTypes.IsEmpty)
                {
                    Assert.True(DocumentCompilerTestHelpers.GetMethodBody(pe, method.Name).LocalSignature.IsNil);
                    continue;
                }

                Assert.Equal(method.SlotTypes, LocalTests.GetLocalTypes(pe, method.Name));
            }
        }

        [Theory]
        [MemberData(nameof(LocalScopeCaseGenerator.CaseData), MemberType = typeof(LocalScopeCaseGenerator))]
        public void LocalNames_ResolveToTheFirstDeclarationInTheInnermostScopeThatDeclaresThem(int index, string description)
        {
            using PEReader pe = Compile(index);
            foreach (GeneratedLocalsMethod method in LocalScopeCaseGenerator.Cases[index].Methods)
            {
                Assert.Equal(
                    method.ReferencedSlots.Select(slot => $"ldloc {slot}"),
                    LocalTests.GetLocalOperands(pe, method.Name));
            }
        }

        private static PortablePdbTestReader CompileWithPdb(int index)
            => new(CompileErrorTolerant(index));

        /// <summary>Gets the number of LocalScope rows the generator's model predicts for a case.</summary>
        private static int PredictedScopeRows(int index)
            => LocalScopeCaseGenerator.Cases[index].Methods.Sum(method => method.Scopes.Length);

        [Fact]
        public void Generator_MostCasesHaveScopeRows()
        {
            // The table-wide theories below check the rows a case has; a case with none checks nothing there.
            int casesWithRows = Enumerable.Range(0, LocalScopeCaseGenerator.Cases.Length).Count(index => PredictedScopeRows(index) > 0);
            Assert.True(casesWithRows >= 190, $"{casesWithRows} of {LocalScopeCaseGenerator.Cases.Length} cases have LocalScope rows");
        }

        [Theory]
        [MemberData(nameof(LocalScopeCaseGenerator.CaseData), MemberType = typeof(LocalScopeCaseGenerator))]
        public void LocalScopes_AreTheGeneratedScopesWithTheirNamedLocalsAndSlots(int index, string description)
        {
            using PortablePdbTestReader pdb = CompileWithPdb(index);
            foreach (GeneratedLocalsMethod method in LocalScopeCaseGenerator.Cases[index].Methods)
            {
                Assert.Equal(method.Scopes, LocalScopeTests.Scopes(pdb, method.Name));
            }
        }

        [Theory]
        [MemberData(nameof(LocalScopeCaseGenerator.CaseData), MemberType = typeof(LocalScopeCaseGenerator))]
        public void LocalScopeTable_IsSortedAndTheScopesOfAMethodNestOrAreDisjointWithinItsBody(int index, string description)
        {
            using PortablePdbTestReader pdb = CompileWithPdb(index);
            using PEReader pe = Compile(index);
            LocalScope[] rows = pdb.Pdb.LocalScopes.Select(pdb.Pdb.GetLocalScope).ToArray();
            Assert.Equal(PredictedScopeRows(index), rows.Length);
            for (int i = 1; i < rows.Length; i++)
            {
                (int Method, int Start, int NegativeLength) previous = Key(rows[i - 1]);
                (int Method, int Start, int NegativeLength) current = Key(rows[i]);
                Assert.True(previous.CompareTo(current) <= 0, $"row {i} is out of order");
            }

            foreach (IGrouping<MethodDefinitionHandle, LocalScope> method in rows.GroupBy(row => row.Method))
            {
                string name = pdb.Image.GetString(pdb.Image.GetMethodDefinition(method.Key).Name);
                int bodySize = DocumentCompilerTestHelpers.GetMethodBody(pe, name).GetILBytes()!.Length;
                LocalScope[] scopes = method.ToArray();
                Assert.All(scopes, scope => Assert.True(scope.EndOffset <= bodySize));
                for (int i = 0; i < scopes.Length; i++)
                {
                    for (int j = i + 1; j < scopes.Length; j++)
                    {
                        int start = Math.Max(scopes[i].StartOffset, scopes[j].StartOffset);
                        int end = Math.Min(scopes[i].EndOffset, scopes[j].EndOffset);
                        bool disjoint = start >= end;
                        bool nested = (start, end) == (scopes[i].StartOffset, scopes[i].EndOffset)
                            || (start, end) == (scopes[j].StartOffset, scopes[j].EndOffset);
                        Assert.True(disjoint || nested, $"{name}: scopes {i} and {j} overlap");
                    }
                }
            }

            static (int, int, int) Key(LocalScope scope)
                => (MetadataTokens.GetRowNumber(scope.Method), scope.StartOffset, -scope.Length);
        }

        [Theory]
        [MemberData(nameof(LocalScopeCaseGenerator.CaseData), MemberType = typeof(LocalScopeCaseGenerator))]
        public void EveryEmptyScopeAndRepeatedNameOrIndexInTheRows_IsReported(int index, string description)
        {
            // Read from the PDB, independently of the model: each LocalScope row of length 0 is one error, and so is
            // each LocalVariable row that repeats a name, or an index, of an earlier row of its scope.
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                LocalScopeCaseGenerator.Cases[index].ToSource(),
                new Options { Debug = true, ErrorTolerant = true });
            using var pdb = new PortablePdbTestReader(result!);
            Assert.Equal(PredictedScopeRows(index), pdb.Pdb.LocalScopes.Count);
            int emptyScopes = 0;
            int repeatedNames = 0;
            int repeatedIndices = 0;
            foreach (LocalScope scope in pdb.Pdb.LocalScopes.Select(pdb.Pdb.GetLocalScope))
            {
                LocalVariable[] variables = scope.GetLocalVariables().Select(pdb.Pdb.GetLocalVariable).ToArray();
                Assert.NotEmpty(variables);
                emptyScopes += scope.Length == 0 ? 1 : 0;
                repeatedNames += variables.Length - variables.Select(variable => pdb.Pdb.GetString(variable.Name)).Distinct().Count();
                repeatedIndices += variables.Length - variables.Select(variable => variable.Index).Distinct().Count();
            }

            Assert.Equal(emptyScopes, diagnostics.Count(diagnostic => diagnostic.Id == DiagnosticIds.LocalScopeWithoutInstructions));
            Assert.Equal(repeatedNames, diagnostics.Count(diagnostic => diagnostic.Id == DiagnosticIds.LocalNameDeclaredTwiceInScope));
            Assert.Equal(repeatedIndices, diagnostics.Count(diagnostic => diagnostic.Id == DiagnosticIds.LocalSlotNamedTwiceInScope));
        }
    }
}
