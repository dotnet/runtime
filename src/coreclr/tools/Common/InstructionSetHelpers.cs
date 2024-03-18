// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

using ILCompiler;

using Internal.TypeSystem;

using InstructionSet = Internal.JitInterface.InstructionSet;

namespace System.CommandLine
{
    internal static partial class Helpers
    {
        public static InstructionSetSupport ConfigureInstructionSetSupport(string instructionSet, int maxVectorTBitWidth, bool isVectorTOptimistic, TargetArchitecture targetArchitecture, TargetOS targetOS,
            string mustNotBeMessage, string invalidImplicationMessage, Logger logger, bool optimizingForSize = false)
        {
            InstructionSetSupportBuilder instructionSetSupportBuilder = new(targetArchitecture);
            InstructionSetSupportFlags flags = 0;

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

            // Whether to allow optimistically expanding the instruction sets beyond what was specified.
            // We seed this from optimizingForSize - if we're size-optimizing, we don't want to unnecessarily
            // compile both branches of IsSupported checks.
            bool allowOptimistic = !optimizingForSize;

            if (instructionSet == "native")
            {
                // We're compiling for a specific chip
                allowOptimistic = false;

                if (GetTargetArchitecture(null) != targetArchitecture)
                {
                    throw new CommandLineException("Instruction set 'native' not supported when cross-compiling to a different architecture.");
                }

                string jitInterfaceLibrary = "jitinterface_" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
                nint libHandle = NativeLibrary.Load(jitInterfaceLibrary, System.Reflection.Assembly.GetExecutingAssembly(), DllImportSearchPath.ApplicationDirectory);
                int cpuFeatures;
                unsafe
                {
                    var getCpuFeatures = (delegate* unmanaged<int>)NativeLibrary.GetExport(libHandle, "JitGetProcessorFeatures");
                    cpuFeatures = getCpuFeatures();
                }
                HardwareIntrinsicHelpers.AddRuntimeRequiredIsaFlagsToBuilder(instructionSetSupportBuilder, cpuFeatures);

                if (targetArchitecture is TargetArchitecture.X64 or TargetArchitecture.X86)
                {
                    // Some architectures can experience frequency throttling when executing
                    // 512-bit width instructions. To account for this we set the
                    // default preferred vector width to 256-bits in some scenarios.
                    (int Eax, int Ebx, int Ecx, int Edx) cpuidInfo = X86Base.CpuId(0, 0);
                    bool isGenuineIntel = (cpuidInfo.Ebx == 0x756E6547) && // Genu
                                          (cpuidInfo.Edx == 0x49656E69) && // ineI
                                          (cpuidInfo.Ecx == 0x6C65746E);   // ntel
                    if (isGenuineIntel)
                    {
                        cpuidInfo = X86Base.CpuId(1, 0);
                        Debug.Assert((cpuidInfo.Edx & (1 << 15)) != 0); // CMOV
                        int model = (cpuidInfo.Eax >> 4) & 0xF;
                        int family = (cpuidInfo.Eax >> 8) & 0xF;
                        int extendedModel = (cpuidInfo.Eax >> 16) & 0xF;

                        if (family == 0x06)
                        {
                            if (extendedModel == 0x05)
                            {
                                if (model == 0x05)
                                {
                                    // * Skylake (Server)
                                    // * Cascade Lake
                                    // * Cooper Lake

                                    flags |= InstructionSetSupportFlags.Vector512Throttling;
                                }
                            }
                            else if (extendedModel == 0x06)
                            {
                                if (model == 0x06)
                                {
                                    // * Cannon Lake

                                    flags |= InstructionSetSupportFlags.Vector512Throttling;
                                }
                            }
                        }
                    }

                    if ((flags & InstructionSetSupportFlags.Vector512Throttling) != 0 && logger.IsVerbose)
                        logger.LogMessage("Vector512 is throttled");
                }

                if (logger.IsVerbose)
                    logger.LogMessage($"The 'native' instruction set expanded to {instructionSetSupportBuilder}");
            }
            else if (instructionSet != null)
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

            // When we are in a fully AOT scenario, such as NativeAOT, then Vector<T>
            // can be directly part of the supported ISAs. This is because we are targeting
            // an exact machine and we won't have any risk of a more capable machine supporting
            // a larger Vector<T> and needing to invalidate any methods pre-compiled targeting
            // smaller sizes.
            //
            // However, when we are in a partial AOT scenario, such as Crossgen2, then
            // Vector<T> must only appear in the optimistic set since the size supported
            // by the pre-compiled code may be smaller (or larger) than what is actually
            // supported at runtime.

            bool skipAddingVectorT = isVectorTOptimistic;

            instructionSetSupportBuilder.ComputeInstructionSetFlags(maxVectorTBitWidth, skipAddingVectorT, out var supportedInstructionSet, out var unsupportedInstructionSet,
                (string specifiedInstructionSet, string impliedInstructionSet) =>
                    throw new CommandLineException(string.Format(invalidImplicationMessage, specifiedInstructionSet, impliedInstructionSet)));

            // Due to expansion by implication, the optimistic set is most often a pure superset of the supported set
            //
            // However, there are some gaps in cases like Arm64 neon where none of the optimistic sets imply it. Likewise,
            // the optimistic set would be missing the explicitly unsupported sets. So we effectively clone the list and
            // tack on the additional optimistic bits after. This ensures the optimistic set remains an accurate superset
            InstructionSetSupportBuilder optimisticInstructionSetSupportBuilder = new InstructionSetSupportBuilder(instructionSetSupportBuilder);

            // Optimistically assume some instruction sets are present.
            if (allowOptimistic && (targetArchitecture == TargetArchitecture.X86 || targetArchitecture == TargetArchitecture.X64))
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
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("serialize");

                // If AVX was enabled, we can opportunistically enable instruction sets which use the VEX encodings
                Debug.Assert(InstructionSet.X64_AVX == InstructionSet.X86_AVX);
                if (supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX))
                {
                    // TODO: Enable optimistic usage of AVX2 once we validate it doesn't break Vector<T> usage
                    // optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("avx2");

                    if (supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX2))
                    {
                        optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("avxvnni");
                    }

                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("fma");
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("bmi");
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("bmi2");
                }

                Debug.Assert(InstructionSet.X64_AVX512F == InstructionSet.X86_AVX512F);
                if (supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX512F))
                {
                    Debug.Assert(supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX512F_VL));
                    Debug.Assert(supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX512BW));
                    Debug.Assert(supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX512BW_VL));
                    Debug.Assert(supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX512CD));
                    Debug.Assert(supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX512CD_VL));
                    Debug.Assert(supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX512DQ));
                    Debug.Assert(supportedInstructionSet.HasInstructionSet(InstructionSet.X64_AVX512DQ_VL));

                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("avx512vbmi");
                    optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("avx512vbmi_vl");
                }
            }
            else if (targetArchitecture == TargetArchitecture.ARM64)
            {
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("aes");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("crc");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sha1");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("sha2");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("lse");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("dotprod");
                optimisticInstructionSetSupportBuilder.AddSupportedInstructionSet("rdma");
            }

            // Vector<T> can always be part of the optimistic set, we only want to optionally exclude it from the supported set
            optimisticInstructionSetSupportBuilder.ComputeInstructionSetFlags(maxVectorTBitWidth, skipAddingVectorT: false, out var optimisticInstructionSet, out _,
                (string specifiedInstructionSet, string impliedInstructionSet) => throw new NotSupportedException());
            optimisticInstructionSet.Remove(unsupportedInstructionSet);
            optimisticInstructionSet.Add(supportedInstructionSet);

            return new InstructionSetSupport(supportedInstructionSet,
                unsupportedInstructionSet,
                optimisticInstructionSet,
                InstructionSetSupportBuilder.GetNonSpecifiableInstructionSetsForArch(targetArchitecture),
                targetArchitecture,
                flags);
        }
    }
}
