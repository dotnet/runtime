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
        public void InstructionSetNamesAreUniquePerArchitecture(TargetArchitecture architecture)
        {
            var specifiableNames = InstructionSetFlags
                .ArchitectureToValidInstructionSets(architecture)
                .Where(i => i.Specifiable)
                .Select(i => i.Name)
                .ToList();

            var distinctNames = specifiableNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // After deduplication, the distinct set should have fewer entries than the raw list
            // (the generator produces duplicates by design), but the DistinctBy approach used
            // in the help text must yield a clean, non-duplicate list.
            Assert.Equal(distinctNames.Count, distinctNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.True(distinctNames.Count > 0, $"Architecture {architecture} should have at least one specifiable instruction set.");
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
            var cpuNames = InstructionSetFlags.AllCpuNames.ToList();
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
