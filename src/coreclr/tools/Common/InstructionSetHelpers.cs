// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler;
using Internal.TypeSystem;
using InstructionSet = Internal.JitInterface.InstructionSet;

namespace System.CommandLine
{
    internal static partial class Helpers
    {
        public static InstructionSetSupport ConfigureInstructionSetSupport(string instructionSet, TargetArchitecture targetArchitecture, TargetOS targetOS,
            string mustNotBeMessage, string invalidImplicationMessage)
        {
            InstructionSetSupportBuilder instructionSetSupportBuilder = new(targetArchitecture);

            // Ready to run images are built with certain instruction set baselines
            if ((targetArchitecture == TargetArchitecture.X86) || (targetArchitecture == TargetArchitecture.X64))
            {
                instructionSetSupportBuilder.AddSupportedInstructionSet("sse2"); // Lower baselines included by implication
            }
            else if (targetArchitecture == TargetArchitecture.ARM64)
            {
                if (targetOS == TargetOS.OSX)
                {
                    // For osx-arm64 we know that apple-m1 is a baseline
                    instructionSetSupportBuilder.AddSupportedInstructionSet("apple-m1");
                }
                else
                {
                    instructionSetSupportBuilder.AddSupportedInstructionSet("neon"); // Lower baselines included by implication
                }
            }

            if (instructionSet != null)
            {
                List<string> instructionSetParams = new List<string>();

                // Normalize instruction set format to include implied +.
                string[] instructionSetParamsInput = instructionSet.Split(',');
                for (int i = 0; i < instructionSetParamsInput.Length; i++)
                {
                    instructionSet = instructionSetParamsInput[i];

                    if (string.IsNullOrEmpty(instructionSet))
                        throw new CommandLineException(string.Format(mustNotBeMessage, ""));

                    char firstChar = instructionSet[0];
                    if ((firstChar != '+') && (firstChar != '-'))
                    {
                        instructionSet =  "+" + instructionSet;
                    }
                    instructionSetParams.Add(instructionSet);
                }

                Dictionary<string, bool> instructionSetSpecification = new Dictionary<string, bool>();
                foreach (string instructionSetSpecifier in instructionSetParams)
                {
                    instructionSet = instructionSetSpecifier.Substring(1);

                    bool enabled = instructionSetSpecifier[0] == '+' ? true : false;
                    if (enabled)
                    {
                        if (!instructionSetSupportBuilder.AddSupportedInstructionSet(instructionSet))
                            throw new CommandLineException(string.Format(mustNotBeMessage, instructionSet));
                    }
                    else
                    {
                        if (!instructionSetSupportBuilder.RemoveInstructionSetSupport(instructionSet))
                            throw new CommandLineException(string.Format(mustNotBeMessage, instructionSet));
                    }
                }
            }

            instructionSetSupportBuilder.ComputeInstructionSetFlags(out var supportedInstructionSet, out var unsupportedInstructionSet,
                (string specifiedInstructionSet, string impliedInstructionSet) =>
                    throw new CommandLineException(string.Format(invalidImplicationMessage, specifiedInstructionSet, impliedInstructionSet)));

            InstructionSetSupportBuilder optimisticInstructionSetSupportBuilder = new InstructionSetSupportBuilder(targetArchitecture);

            // Optimistically assume some instruction sets are present.
            if (targetArchitecture == TargetArchitecture.X86 || targetArchitecture == TargetArchitecture.X64)
            {
                // We set these hardware features as opportunistically enabled as most of hardware in the wild supports them.
                // Note that we do not indicate support for AVX, or any other instruction set which uses the VEX encodings as
                // the presence of those makes otherwise acceptable code be unusable on hardware which does not support VEX encodings.
                //
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sse4.2"); // Lower SSE versions included by implication
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("aes");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("pclmul");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("movbe");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("popcnt");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("lzcnt");

                // If AVX was enabled, we can opportunistically enable instruction sets which use the VEX encodings
                Debug.Assert(InstructionSet.X64_AVX == InstructionSet.X86_AVX);
                if (supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX))
                {
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("fma");
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("bmi");
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("bmi2");
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("avxvnni");
                }
            }
            else if (targetArchitecture == TargetArchitecture.ARM64)
            {
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("aes");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("crc");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sha1");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sha2");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("lse");
            }

            optimisticInstructionSetSupportBuilder.ComputeInstructionSetFlags(out var optimisticInstructionSet, out _,
                (string specifiedInstructionSet, string impliedInstructionSet) => throw new NotSupportedException());
            optimisticInstructionSet.Remove(unsupportedInstructionSet);
            optimisticInstructionSet.Add(supportedInstructionSet);

            return new InstructionSetSupport(supportedInstructionSet,
                unsupportedInstructionSet,
                optimisticInstructionSet,
                InstructionSetSupportBuilder.GetNonSpecifiableInstructionSetsForArch(targetArchitecture),
                targetArchitecture);
        }
    }
}
