// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;
using Internal.JitInterface;

namespace ILCompiler
{
    public class InstructionSetSupport
    {
        private readonly TargetArchitecture _targetArchitecture;
        private readonly InstructionSetFlags _optimisticInstructionSets;
        private readonly InstructionSetFlags _supportedInstructionSets;
        private readonly InstructionSetFlags _unsupportedInstructionSets;
        private readonly InstructionSetFlags _nonSpecifiableInstructionSets;

        public InstructionSetSupport(InstructionSetFlags supportedInstructionSets, InstructionSetFlags unsupportedInstructionSets, TargetArchitecture architecture) :
            this(supportedInstructionSets, unsupportedInstructionSets, supportedInstructionSets, default(InstructionSetFlags), architecture)
        {
        }

        public InstructionSetSupport(InstructionSetFlags supportedInstructionSets, InstructionSetFlags unsupportedInstructionSets, InstructionSetFlags optimisticInstructionSets, InstructionSetFlags nonSpecifiableInstructionSets, TargetArchitecture architecture)
        {
            _supportedInstructionSets = supportedInstructionSets;
            _unsupportedInstructionSets = unsupportedInstructionSets;
            _optimisticInstructionSets = optimisticInstructionSets;
            _targetArchitecture = architecture;
            _nonSpecifiableInstructionSets = nonSpecifiableInstructionSets;
        }

        public bool IsInstructionSetSupported(InstructionSet instructionSet)
        {
            return _supportedInstructionSets.HasInstructionSet(instructionSet);
        }

        public bool IsInstructionSetExplicitlyUnsupported(InstructionSet instructionSet)
        {
            return _unsupportedInstructionSets.HasInstructionSet(instructionSet);
        }

        public InstructionSetFlags OptimisticFlags => _optimisticInstructionSets;
        public InstructionSetFlags SupportedFlags => _supportedInstructionSets;
        public InstructionSetFlags ExplicitlyUnsupportedFlags => _unsupportedInstructionSets;
        public InstructionSetFlags NonSpecifiableFlags => _nonSpecifiableInstructionSets;

        public TargetArchitecture Architecture => _targetArchitecture;

        public static string GetHardwareIntrinsicId(TargetArchitecture architecture, TypeDesc potentialTypeDesc)
        {
            if (!potentialTypeDesc.IsIntrinsic || !(potentialTypeDesc is MetadataType potentialType))
                return "";

            if (architecture == TargetArchitecture.X64)
            {
                if (potentialType.Name == "X64")
                    potentialType = (MetadataType)potentialType.ContainingType;
                if (potentialType.Namespace != "System.Runtime.Intrinsics.X86")
                    return "";
            }
            else if (architecture == TargetArchitecture.X86)
            {
                if (potentialType.Name == "X64")
                    potentialType = (MetadataType)potentialType.ContainingType;
                if (potentialType.Namespace != "System.Runtime.Intrinsics.X86")
                    return "";
            }
            else if (architecture == TargetArchitecture.ARM64)
            {
                if (potentialType.Name == "Arm64")
                    potentialType = (MetadataType)potentialType.ContainingType;
                if (potentialType.Namespace != "System.Runtime.Intrinsics.Arm")
                    return "";
            }
            else if (architecture == TargetArchitecture.ARM)
            {
                if (potentialType.Namespace != "System.Runtime.Intrinsics.Arm")
                    return "";
            }
            else
            {
                throw new InternalCompilerErrorException("Unknown architecture");
            }

            return potentialType.Name;
        }

        public SimdVectorLength GetVectorTSimdVector()
        {
            if ((_targetArchitecture == TargetArchitecture.X64) || (_targetArchitecture == TargetArchitecture.X86))
            {
                Debug.Assert(InstructionSet.X64_AVX2 == InstructionSet.X86_AVX2);
                Debug.Assert(InstructionSet.X64_SSE2 == InstructionSet.X86_SSE2);
                if (IsInstructionSetSupported(InstructionSet.X86_AVX2))
                    return SimdVectorLength.Vector256Bit;
                else if (IsInstructionSetExplicitlyUnsupported(InstructionSet.X86_AVX2) && IsInstructionSetSupported(InstructionSet.X64_SSE2))
                    return SimdVectorLength.Vector128Bit;
                else
                    return SimdVectorLength.None;
            }
            else if (_targetArchitecture == TargetArchitecture.ARM64)
            {
                return SimdVectorLength.Vector128Bit;
            }
            else if (_targetArchitecture == TargetArchitecture.ARM)
            {
                return SimdVectorLength.None;
            }
            else if (_targetArchitecture == TargetArchitecture.LoongArch64)
            {
                return SimdVectorLength.None;
            }
            else
            {
                Debug.Assert(false); // Unknown architecture
                return SimdVectorLength.None;
            }
        }
    }

    public class InstructionSetSupportBuilder
    {
        private static Dictionary<TargetArchitecture, Dictionary<string, InstructionSet>> s_instructionSetSupport = ComputeInstructionSetSupport();
        private static Dictionary<TargetArchitecture, InstructionSetFlags> s_nonSpecifiableInstructionSets = ComputeNonSpecifiableInstructionSetSupport();

        private static Dictionary<TargetArchitecture, Dictionary<string, InstructionSet>> ComputeInstructionSetSupport()
        {
            var supportMatrix = new Dictionary<TargetArchitecture, Dictionary<string, InstructionSet>>();
            foreach (TargetArchitecture arch in Enum.GetValues(typeof(TargetArchitecture)))
            {
                supportMatrix[arch] = ComputeInstructSetSupportForArch(arch);
            }

            return supportMatrix;
        }

        private static Dictionary<TargetArchitecture, InstructionSetFlags> ComputeNonSpecifiableInstructionSetSupport()
        {
            var matrix = new Dictionary<TargetArchitecture, InstructionSetFlags>();
            foreach (TargetArchitecture arch in Enum.GetValues(typeof(TargetArchitecture)))
            {
                matrix[arch] = ComputeNonSpecifiableInstructionSetSupportForArch(arch);
            }

            return matrix;
        }

        private static Dictionary<string, InstructionSet> ComputeInstructSetSupportForArch(TargetArchitecture architecture)
        {
            var support = new Dictionary<string, InstructionSet>();
            foreach (var instructionSet in InstructionSetFlags.ArchitectureToValidInstructionSets(architecture))
            {
                // Only instruction sets with associated R2R enum values are are specifiable
                if (instructionSet.Specifiable)
                    support.Add(instructionSet.Name, instructionSet.InstructionSet);
            }

            return support;
        }

        private static InstructionSetFlags ComputeNonSpecifiableInstructionSetSupportForArch(TargetArchitecture architecture)
        {
            var support = new InstructionSetFlags();
            foreach (var instructionSet in InstructionSetFlags.ArchitectureToValidInstructionSets(architecture))
            {
                // Only instruction sets with associated R2R enum values are are specifiable
                if (!instructionSet.Specifiable)
                    support.AddInstructionSet(instructionSet.InstructionSet);
            }

            return support;
        }

        public static InstructionSetFlags GetNonSpecifiableInstructionSetsForArch(TargetArchitecture architecture)
        {
            return s_nonSpecifiableInstructionSets[architecture];
        }

        private readonly SortedSet<string> _supportedInstructionSets = new SortedSet<string>();
        private readonly SortedSet<string> _unsupportedInstructionSets = new SortedSet<string>();
        private readonly TargetArchitecture _architecture;

        public InstructionSetSupportBuilder(TargetArchitecture architecture)
        {
            _architecture = architecture;
        }

        /// <summary>
        /// Add a supported instruction set to the specified list.
        /// </summary>
        /// <returns>returns "false" if instruction set isn't valid on this architecture</returns>
        public bool AddSupportedInstructionSet(string instructionSet)
        {
            // First, check if it's a "known cpu family" group of instruction sets e.g. "haswell"
            var sets = InstructionSetFlags.CpuNameToInstructionSets(instructionSet, _architecture);
            if (sets != null)
            {
                foreach (string set in sets)
                {
                    if (!s_instructionSetSupport[_architecture].ContainsKey(set))
                    {
                        // Groups can contain other groups
                        if (AddSupportedInstructionSet(set))
                        {
                            continue;
                        }
                        return false;
                    }
                    _supportedInstructionSets.Add(set);
                    _unsupportedInstructionSets.Remove(set);
                }
                return true;
            }

            if (!s_instructionSetSupport[_architecture].ContainsKey(instructionSet))
                return false;

            _supportedInstructionSets.Add(instructionSet);
            _unsupportedInstructionSets.Remove(instructionSet);
            return true;
        }

        /// <summary>
        /// Removes a supported instruction set to the specified list.
        /// </summary>
        /// <returns>returns "false" if instruction set isn't valid on this architecture</returns>
        public bool RemoveInstructionSetSupport(string instructionSet)
        {
            if (!s_instructionSetSupport[_architecture].ContainsKey(instructionSet))
                return false;

            _supportedInstructionSets.Remove(instructionSet);
            _unsupportedInstructionSets.Add(instructionSet);
            return true;
        }

        /// <summary>
        /// Seal modifications to instruction set support
        /// </summary>
        /// <returns>returns "false" if instruction set isn't valid on this architecture</returns>
        public bool ComputeInstructionSetFlags(out InstructionSetFlags supportedInstructionSets,
                                                              out InstructionSetFlags unsupportedInstructionSets,
                                                              Action<string, string> invalidInstructionSetImplication)
        {
            supportedInstructionSets = new InstructionSetFlags();
            unsupportedInstructionSets = new InstructionSetFlags();
            Dictionary<string, InstructionSet> instructionSetConversion = s_instructionSetSupport[_architecture];

            foreach (string unsupported in _unsupportedInstructionSets)
            {
                unsupportedInstructionSets.AddInstructionSet(instructionSetConversion[unsupported]);
            }
            unsupportedInstructionSets.ExpandInstructionSetByReverseImplication(_architecture);
            unsupportedInstructionSets.Set64BitInstructionSetVariants(_architecture);

            if ((_architecture == TargetArchitecture.X86) || (_architecture == TargetArchitecture.ARM))
                unsupportedInstructionSets.Set64BitInstructionSetVariantsUnconditionally(_architecture);

            foreach (string supported in _supportedInstructionSets)
            {
                supportedInstructionSets.AddInstructionSet(instructionSetConversion[supported]);
                supportedInstructionSets.ExpandInstructionSetByImplication(_architecture);

                foreach (string unsupported in _unsupportedInstructionSets)
                {
                    InstructionSetFlags checkForExplicitUnsupport = new InstructionSetFlags();
                    checkForExplicitUnsupport.AddInstructionSet(instructionSetConversion[unsupported]);
                    checkForExplicitUnsupport.ExpandInstructionSetByReverseImplication(_architecture);
                    checkForExplicitUnsupport.Set64BitInstructionSetVariants(_architecture);

                    InstructionSetFlags supportedTemp = supportedInstructionSets;
                    supportedTemp.Remove(checkForExplicitUnsupport);

                    // If removing the explicitly unsupported instruction sets, changes the set of
                    // supported instruction sets, then the parameter is invalid
                    if (!supportedTemp.Equals(supportedInstructionSets))
                    {
                        invalidInstructionSetImplication(supported, unsupported);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
