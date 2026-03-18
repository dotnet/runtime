// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Internal.JitInterface;
using Internal.TypeSystem;

using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class InstructionSetHelpTests
    {
        private static readonly TargetArchitecture[] s_architecturesWithInstructionSets =
        [
            TargetArchitecture.ARM64,
            TargetArchitecture.X64,
            TargetArchitecture.X86,
            TargetArchitecture.RiscV64,
        ];

        [Theory]
        [MemberData(nameof(GetArchitectures))]
        public void HelpTextShowsNoDuplicateInstructionSetNames(TargetArchitecture architecture)
        {
            // Simulate the exact logic used in ILCompilerRootCommand.PrintExtendedHelp
            // and Crossgen2RootCommand.PrintExtendedHelp to produce the help text:
            // DistinctBy on Name (case-insensitive), then filter by Specifiable.
            var helpTextNames = InstructionSetFlags
                .ArchitectureToValidInstructionSets(architecture)
                .DistinctBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Where(i => i.Specifiable)
                .Select(i => i.Name)
                .ToList();

            Assert.True(helpTextNames.Count > 0, $"Architecture {architecture} should have at least one specifiable instruction set.");
            Assert.Equal(helpTextNames.Count, helpTextNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Theory]
        [MemberData(nameof(GetArchitectures))]
        public void AllSpecifiableNamesAppearInHelpText(TargetArchitecture architecture)
        {
            // Verify that the DistinctBy approach doesn't accidentally hide any specifiable
            // name due to ordering (e.g., a non-specifiable entry appearing first for a name).
            var allSpecifiableNames = InstructionSetFlags
                .ArchitectureToValidInstructionSets(architecture)
                .Where(i => i.Specifiable)
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var helpTextNames = InstructionSetFlags
                .ArchitectureToValidInstructionSets(architecture)
                .DistinctBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Where(i => i.Specifiable)
                .Select(i => i.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.True(allSpecifiableNames.SetEquals(helpTextNames),
                $"Help text names for {architecture} should match all specifiable names. " +
                $"Missing: [{string.Join(", ", allSpecifiableNames.Except(helpTextNames))}] " +
                $"Extra: [{string.Join(", ", helpTextNames.Except(allSpecifiableNames))}]");
        }

        [Fact]
        public void AllCpuGroupNamesResolveToValidInstructionSets()
        {
            foreach (string cpuName in InstructionSetFlags.AllCpuNames)
            {
                bool resolvedForAtLeastOneArch = false;

                foreach (var architecture in s_architecturesWithInstructionSets)
                {
                    var sets = InstructionSetFlags.CpuNameToInstructionSets(cpuName, architecture);
                    if (sets != null)
                    {
                        resolvedForAtLeastOneArch = true;
                        var builder = new InstructionSetSupportBuilder(architecture);
                        bool allValid = builder.AddSupportedInstructionSet(cpuName);
                        Assert.True(allValid, $"CPU group '{cpuName}' should resolve to valid instruction sets for {architecture}.");
                    }
                }

                Assert.True(resolvedForAtLeastOneArch, $"CPU group '{cpuName}' should resolve for at least one architecture.");
            }
        }

        [Fact]
        public void AllCpuGroupNamesAreUnique()
        {
            var cpuNames = InstructionSetFlags.AllCpuNames.ToList();
            var distinctNames = cpuNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Assert.Equal(distinctNames.Count, cpuNames.Count);
        }

        [Fact]
        public void NoPredefinedGroupNamedX86Dash64WithoutVersion()
        {
            // x86-64 (without v-suffix) was removed in .NET 10 because SSE4.2
            // became the baseline. Only x86-64-v2, v3, v4 should exist.
            var cpuNames = InstructionSetFlags.AllCpuNames
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("x86-64", cpuNames);
        }

        public static IEnumerable<object[]> GetArchitectures()
        {
            foreach (var arch in s_architecturesWithInstructionSets)
            {
                yield return new object[] { arch };
            }
        }
    }
}
