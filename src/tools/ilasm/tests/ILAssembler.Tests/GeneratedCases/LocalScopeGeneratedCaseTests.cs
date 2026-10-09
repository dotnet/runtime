// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    /// Local slots and lexical name scopes, checked over the seeded generated methods of
    /// <see cref="LocalScopeCaseGenerator"/> against the generator's model of native ilasm.
    /// </summary>
    public class LocalScopeGeneratedCaseTests
    {
        private static PEReader Compile(int index)
            => new(DocumentCompilerTestHelpers.Compile(LocalScopeCaseGenerator.Cases[index].ToSource(), new Options { Debug = true }));

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
            => PortablePdbTestReader.Compile(LocalScopeCaseGenerator.Cases[index].ToSource());

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
                Assert.All(scopes, scope => Assert.True(scope.Length > 0 && scope.EndOffset <= bodySize));
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
        public void LocalVariables_OfAScopeHaveDistinctNamesAndIndices(int index, string description)
        {
            using PortablePdbTestReader pdb = CompileWithPdb(index);
            Assert.Equal(PredictedScopeRows(index), pdb.Pdb.LocalScopes.Count);
            foreach (LocalScope scope in pdb.Pdb.LocalScopes.Select(pdb.Pdb.GetLocalScope))
            {
                LocalVariable[] variables = scope.GetLocalVariables().Select(pdb.Pdb.GetLocalVariable).ToArray();
                Assert.NotEmpty(variables);
                Assert.Equal(variables.Length, variables.Select(variable => pdb.Pdb.GetString(variable.Name)).Distinct().Count());
                Assert.Equal(variables.Length, variables.Select(variable => variable.Index).Distinct().Count());
            }
        }
    }
}
