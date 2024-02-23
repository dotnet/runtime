// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;
using Internal.JitInterface;

namespace ILCompiler
{
    [Flags]
    public enum InstructionSetSupportFlags
    {
        Vector512Throttling = 0x1,
    }

    public class InstructionSetSupport
    {
        private readonly TargetArchitecture _targetArchitecture;
        private readonly InstructionSetFlags _optimisticInstructionSets;
        private readonly InstructionSetFlags _supportedInstructionSets;
        private readonly InstructionSetFlags _unsupportedInstructionSets;
        private readonly InstructionSetFlags _nonSpecifiableInstructionSets;
        private readonly InstructionSetSupportFlags _flags;

        public InstructionSetSupport(InstructionSetFlags supportedInstructionSets, InstructionSetFlags unsupportedInstructionSets, TargetArchitecture architecture) :
            this(supportedInstructionSets, unsupportedInstructionSets, supportedInstructionSets, default(InstructionSetFlags), architecture)
        {
        }

        public InstructionSetSupport(InstructionSetFlags supportedInstructionSets, InstructionSetFlags unsupportedInstructionSets, InstructionSetFlags optimisticInstructionSets, InstructionSetFlags nonSpecifiableInstructionSets, TargetArchitecture architecture, InstructionSetSupportFlags flags = 0)
        {
            _supportedInstructionSets = supportedInstructionSets;
            _unsupportedInstructionSets = unsupportedInstructionSets;
            _optimisticInstructionSets = optimisticInstructionSets;
            _targetArchitecture = architecture;
            _nonSpecifiableInstructionSets = nonSpecifiableInstructionSets;
            _flags = flags;
        }

        public bool IsInstructionSetSupported(InstructionSet instructionSet)
        {
            return _supportedInstructionSets.HasInstructionSet(instructionSet);
        }

        public bool IsInstructionSetOptimisticallySupported(InstructionSet instructionSet)
        {
            return _optimisticInstructionSets.HasInstructionSet(instructionSet);
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

        public InstructionSetSupportFlags Flags => _flags;

        public static string GetHardwareIntrinsicId(TargetArchitecture architecture, TypeDesc potentialTypeDesc)
        {
            if (!potentialTypeDesc.IsIntrinsic || !(potentialTypeDesc is MetadataType potentialType))
                return "";

            if (architecture == TargetArchitecture.X64)
            {
                if (potentialType.Name == "X64")
                    potentialType = (MetadataType)potentialType.ContainingType;
                if (potentialType.Name == "VL")
                    potentialType = (MetadataType)potentialType.ContainingType;
                if (potentialType.Namespace != "System.Runtime.Intrinsics.X86")
                    return "";
            }
            else if (architecture == TargetArchitecture.X86)
            {
                if (potentialType.Name == "X64")
                    potentialType = (MetadataType)potentialType.ContainingType;
                if (potentialType.Name == "VL")
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
            else if (architecture == TargetArchitecture.RiscV64)
            {
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
                Debug.Assert(InstructionSet.X64_VectorT128 == InstructionSet.X86_VectorT128);
                Debug.Assert(InstructionSet.X64_VectorT256 == InstructionSet.X86_VectorT256);
                Debug.Assert(InstructionSet.X64_VectorT512 == InstructionSet.X86_VectorT512);

                // TODO-XArch: Add support for 512-bit Vector<T>
                Debug.Assert(!IsInstructionSetOptimisticallySupported(InstructionSet.X64_VectorT512));

                if (IsInstructionSetOptimisticallySupported(InstructionSet.X64_VectorT256))
                {
                    Debug.Assert(!IsInstructionSetOptimisticallySupported(InstructionSet.X64_VectorT128));
                    return SimdVectorLength.Vector256Bit;
                }
                else if (IsInstructionSetOptimisticallySupported(InstructionSet.X64_VectorT128))
                {
                    return SimdVectorLength.Vector128Bit;
                }
                else
                {
                    return SimdVectorLength.None;
                }
            }
            else if (_targetArchitecture == TargetArchitecture.ARM64)
            {
                if (IsInstructionSetOptimisticallySupported(InstructionSet.ARM64_VectorT128))
                {
                    return SimdVectorLength.Vector128Bit;
                }
                else
                {
                    return SimdVectorLength.None;
                }
            }
            else if (_targetArchitecture == TargetArchitecture.ARM)
            {
                return SimdVectorLength.None;
            }
            else if (_targetArchitecture == TargetArchitecture.LoongArch64)
            {
                return SimdVectorLength.None;
            }
            else if (_targetArchitecture == TargetArchitecture.RiscV64)
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

        private readonly SortedSet<string> _supportedInstructionSets;
        private readonly SortedSet<string> _unsupportedInstructionSets;
        private readonly TargetArchitecture _architecture;

        public TargetArchitecture Architecture => _architecture;

        public InstructionSetSupportBuilder(TargetArchitecture architecture)
        {
            _supportedInstructionSets = new SortedSet<string>();
            _unsupportedInstructionSets = new SortedSet<string>();
            _architecture = architecture;
        }

        public InstructionSetSupportBuilder(InstructionSetSupportBuilder other)
        {
            _supportedInstructionSets = new SortedSet<string>(other._supportedInstructionSets);
            _unsupportedInstructionSets = new SortedSet<string>(other._unsupportedInstructionSets);
            _architecture = other._architecture;
        }

        public override string ToString()
            => (_supportedInstructionSets.Count > 0 ? "+" : "")
               + string.Join(",+", _supportedInstructionSets)
               + (_supportedInstructionSets.Count > 0 && _unsupportedInstructionSets.Count > 0 ? "," : "")
               + (_unsupportedInstructionSets.Count > 0 ? "-" : "")
               + string.Join(",-", _unsupportedInstructionSets);

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
        public bool ComputeInstructionSetFlags(int maxVectorTBitWidth,
                                               bool skipAddingVectorT,
                                               out InstructionSetFlags supportedInstructionSets,
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

            if (skipAddingVectorT)
            {
                // For partial AOT scenarios, we need to skip adding Vector<T>
                // in the supported set so it doesn't cause the entire image
                // to be thrown away due to the host machine supporting a larger
                // size.

                return true;
            }

            switch (_architecture)
            {
                case TargetArchitecture.X64:
                case TargetArchitecture.X86:
                {
                    Debug.Assert(InstructionSet.X86_SSE2 == InstructionSet.X64_SSE2);
                    Debug.Assert(InstructionSet.X86_AVX2 == InstructionSet.X64_AVX2);
                    Debug.Assert(InstructionSet.X86_AVX512F == InstructionSet.X64_AVX512F);

                    Debug.Assert(InstructionSet.X86_VectorT128 == InstructionSet.X64_VectorT128);
                    Debug.Assert(InstructionSet.X86_VectorT256 == InstructionSet.X64_VectorT256);
                    Debug.Assert(InstructionSet.X86_VectorT512 == InstructionSet.X64_VectorT512);

                    // We only want one size supported for Vector<T> and we want the other sizes explicitly
                    // unsupported to ensure we throw away the given methods if runtime picks a larger size

                    Debug.Assert(supportedInstructionSets.HasInstructionSet(InstructionSet.X86_SSE2));
                    Debug.Assert((maxVectorTBitWidth == 0) || (maxVectorTBitWidth >= 128));
                    supportedInstructionSets.AddInstructionSet(InstructionSet.X86_VectorT128);

                    if (supportedInstructionSets.HasInstructionSet(InstructionSet.X86_AVX2))
                    {
                        if ((maxVectorTBitWidth == 0) || (maxVectorTBitWidth >= 256))
                        {
                            supportedInstructionSets.RemoveInstructionSet(InstructionSet.X86_VectorT128);
                            supportedInstructionSets.AddInstructionSet(InstructionSet.X86_VectorT256);
                        }

                        // TODO-XArch: Add support for 512-bit Vector<T>
                    }
                    break;
                }

                case TargetArchitecture.ARM64:
                {
                    Debug.Assert(supportedInstructionSets.HasInstructionSet(InstructionSet.ARM64_AdvSimd));
                    Debug.Assert((maxVectorTBitWidth == 0) || (maxVectorTBitWidth >= 128));
                    supportedInstructionSets.AddInstructionSet(InstructionSet.ARM64_VectorT128);
                    break;
                }
            }

            return true;
        }
    }
}
