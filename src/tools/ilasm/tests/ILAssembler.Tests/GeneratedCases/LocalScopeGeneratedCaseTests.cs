// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
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
                "gaps", "padDeclaredAtEnd", "references", "reusesClosedSlot", "shadows", "unnamed",
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
    }
}
