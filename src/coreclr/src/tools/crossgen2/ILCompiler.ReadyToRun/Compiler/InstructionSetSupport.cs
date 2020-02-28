using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Internal.TypeSystem;

namespace ILCompiler
{
    public class InstructionSetSupport
    {
        private readonly HashSet<string> _supportedInstructionSets = new HashSet<string>();
        private readonly HashSet<string> _unsupportedInstructionSets = new HashSet<string>();
        private readonly TargetArchitecture _targetArchitecture;

        internal InstructionSetSupport(IEnumerable<string> supportedInstructionSets, IEnumerable<string> unsupportedInstructionSets, TargetArchitecture architecture)
        {
            _supportedInstructionSets.UnionWith(supportedInstructionSets);
            _unsupportedInstructionSets.UnionWith(unsupportedInstructionSets);
            _targetArchitecture = architecture;
        }

        public bool IsInstructionSetSupported(string instructionSet)
        {
            return _supportedInstructionSets.Contains(instructionSet);
        }

        public bool IsInstructionSetExplicitlyUnsupported(string instructionSet)
        {
            return _unsupportedInstructionSets.Contains(instructionSet);
        }

        public IEnumerable<string> SupportedInstructionSets => _supportedInstructionSets;

        public TargetArchitecture Architecture => _targetArchitecture;

        public bool IsSupportedInstructionSetIntrinsic(MethodDesc method)
        {
            TargetDetails target = method.Context.Target;
            var owningType = (MetadataType)method.OwningType;

            if (target.Architecture == TargetArchitecture.X64)
            {
                if (owningType.Name == "X64")
                    owningType = (MetadataType)owningType.ContainingType;
                if (owningType.Namespace != "System.Runtime.Intrinsics.X86")
                    return false;
            }
            else if (target.Architecture == TargetArchitecture.X86)
            {
                if (owningType.Namespace != "System.Runtime.Intrinsics.X86")
                    return false;
            }
            else if (target.Architecture == TargetArchitecture.ARM64)
            {
                if (owningType.Name == "Arm64")
                    owningType = (MetadataType)owningType.ContainingType;
                if (owningType.Namespace != "System.Runtime.Intrinsics.Arm")
                    return false;
            }
            else if (target.Architecture == TargetArchitecture.ARM)
            {
                if (owningType.Namespace != "System.Runtime.Intrinsics.Arm")
                    return false;
            }
            else
            {
                throw new InternalCompilerErrorException("Unknown architecture");
            }

            return IsInstructionSetSupported(owningType.Name);
        }
    }

    public class InstructionSetSupportBuilder
    {
        static Dictionary<TargetArchitecture, Dictionary<string, string[]>> s_instructionSetSupport = ComputeInstructionSetSupport();
        static Dictionary<TargetArchitecture, string[]> s_baselineSupport = ComputeStandardBaselineSupport();

        private static Dictionary<TargetArchitecture, Dictionary<string, string[]>> ComputeInstructionSetSupport()
        {
            var supportMatrix = new Dictionary<TargetArchitecture, Dictionary<string, string[]>>();
            supportMatrix[TargetArchitecture.ARM] = new Dictionary<string, string[]>();
            supportMatrix[TargetArchitecture.X64] = ComputeInstructSetSupportForX64();
            supportMatrix[TargetArchitecture.X86] = supportMatrix[TargetArchitecture.X64]; // At the moment support for x86 matches X64;
            supportMatrix[TargetArchitecture.ARM64] = new Dictionary<string, string[]>();

            return supportMatrix;
        }

        private static Dictionary<TargetArchitecture, string[]> ComputeStandardBaselineSupport()
        {
            var supportMatrix = new Dictionary<TargetArchitecture, string[]>();
            supportMatrix[TargetArchitecture.ARM] = Array.Empty<string>();
            supportMatrix[TargetArchitecture.X64] = new string[] { "Sse", "Sse2" };
            supportMatrix[TargetArchitecture.X86] = supportMatrix[TargetArchitecture.X64]; // At the moment support for x86 matches X64;
            supportMatrix[TargetArchitecture.ARM64] = new string[] { "ArmBase", "AdvSimd" };

            return supportMatrix;
        }

        private static Dictionary<string, string[]> ComputeInstructSetSupportForX64()
        {
            var support = new Dictionary<string, string[]>();
            support["Aes"] = new string[] { "Aes" };
            support["Avx"] = new string[] { "Sse42" };
            support["Avx2"] = new string[] { "Avx" };
            support["Bmi1"] = new string[] { "Avx" };
            support["Bmi2"] = new string[] { "Avx" };
            support["Fma"] = new string[] { "Avx" };
            support["Lzcnt"] = Array.Empty<string>();
            support["Pclmulqdq"] = new string[] { "Sse2" };
            support["Popcnt"] = new string[] { "Sse42" };
            support["Sse"] = Array.Empty<string>();
            support["Sse2"] = new string[] { "Sse" };
            support["Sse3"] = new string[] { "Sse2" };
            support["Ssse3"] = new string[] { "Sse3" };
            support["Sse41"] = new string[] { "Ssse3" };
            support["Sse42"] = new string[] { "Sse41" };

            return support;
        }

        private static Dictionary<string, string[]> ComputeInstructSetSupportForArm64()
        {
            var support = new Dictionary<string, string[]>();
            support["ArmBase"] = Array.Empty<string>();
            support["AdvSimd"] = new string[] { "ArmBase" };
            support["Aes"] = new string[] { "ArmBase" };
            support["Crc32"] = new string[] { "ArmBase" };
            support["Sha1"] = new string[] { "ArmBase" };
            support["Sha256"] = new string[] { "ArmBase" };

            return support;
        }

        private readonly HashSet<string> _supportedInstructionSets = new HashSet<string>();
        private readonly HashSet<string> _unsupportedInstructionSets = new HashSet<string>();
        private readonly TargetArchitecture _architecture;

        public InstructionSetSupportBuilder(TargetArchitecture architecture)
        {
            _architecture = architecture;
            _supportedInstructionSets.UnionWith(s_baselineSupport[architecture]);
        }

        /// <summary>
        /// Add a supported instruction set to the specified list. 
        /// </summary>
        /// <returns>returns "false" if instruction set isn't valid on this architecture</returns>
        public bool AddSupportedInstructionSet(string instructionSet)
        {
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
        public InstructionSetSupport CreateInstructionSetSupport(Action<string, string> invalidInstructionSetImplication)
        {
            string[] initialSupportedInstructionSets = _supportedInstructionSets.ToArray();
            foreach (string specifiedInstructionSet in initialSupportedInstructionSets)
            {
                if (!AddImpliedInstructionSetSupport(specifiedInstructionSet, specifiedInstructionSet, invalidInstructionSetImplication))
                    return null;
            }

            return new InstructionSetSupport(_supportedInstructionSets, _unsupportedInstructionSets, _architecture);
        }

        private bool AddImpliedInstructionSetSupport(string initialInstructionSet, string instructionSet, Action<string, string> invalidInstructionSetImplication)
        {
            foreach (string impliedInstructionSet in s_instructionSetSupport[_architecture][instructionSet])
            {
                if (_supportedInstructionSets.Add(instructionSet))
                {
                    if (_unsupportedInstructionSets.Contains(impliedInstructionSet))
                    {
                        invalidInstructionSetImplication(initialInstructionSet, impliedInstructionSet);
                        return false;
                    }

                    if (!AddImpliedInstructionSetSupport(initialInstructionSet, impliedInstructionSet, invalidInstructionSetImplication))
                        return false;
                }
            }

            return true;
        }
    }
}
