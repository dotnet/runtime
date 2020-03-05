// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ILCompiler.DependencyAnalysis.ReadyToRun;
using Internal.TypeSystem;

namespace ILCompiler
{
    public class InstructionSetSupport : ICompilationRootProvider
    {
        private static Dictionary<ValueTuple<TargetArchitecture, string, string>, bool> s_apiSupportViaImplication = new Dictionary<ValueTuple<TargetArchitecture, string, string>, bool>();
        private static object s_lock = new object();

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
        public IEnumerable<string> ExplicitlyUnsupportedInstructionSets => _unsupportedInstructionSets;

        public TargetArchitecture Architecture => _targetArchitecture;

        public static string GetHardwareIntrinsicId(TargetArchitecture architecture, TypeDesc potentialTypeDesc)
        {
            if (!(potentialTypeDesc is MetadataType))
                return "";

            MetadataType potentialType = (MetadataType)potentialTypeDesc;

            if (architecture == TargetArchitecture.X64)
            {
                if (potentialType.Name == "X64")
                    potentialType = (MetadataType)potentialType.ContainingType;
                if (potentialType.Namespace != "System.Runtime.Intrinsics.X86")
                    return "";
            }
            else if (architecture == TargetArchitecture.X86)
            {
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

        public bool IsSupportedInstructionSetIntrinsic(MethodDesc method)
        {
            return IsInstructionSetSupported(GetHardwareIntrinsicId(_targetArchitecture, method.OwningType));
        }

        public SimdVectorLength GetVectorTSimdVector()
        {
            if ((_targetArchitecture == TargetArchitecture.X64) || (_targetArchitecture == TargetArchitecture.X86))
            {
                if (IsInstructionSetSupported("Avx2"))
                    return SimdVectorLength.Vector256Bit;
                else if (IsInstructionSetExplicitlyUnsupported("Avx2") && IsInstructionSetSupported("Sse2"))
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
            else
            {
                throw new InternalCompilerErrorException("Unknown architecture");
            }
        }

        public static bool DoesInstructionSetImplyOtherInstructionSet(TargetArchitecture architecture, string instructionSet, string impliedInstructionSet)
        {
            lock (s_lock)
            {
                if (!s_apiSupportViaImplication.TryGetValue((architecture, instructionSet, impliedInstructionSet), out bool instructionSetImplied))
                {
                    InstructionSetSupportBuilder builder = new InstructionSetSupportBuilder(architecture);
                    builder.AddSupportedInstructionSet(instructionSet);
                    instructionSetImplied = builder.CreateInstructionSetSupport(null).IsInstructionSetSupported(impliedInstructionSet);
                    s_apiSupportViaImplication.Add((architecture, instructionSet, impliedInstructionSet), instructionSetImplied);
                }
                return instructionSetImplied;
            }
        }

        void ICompilationRootProvider.AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            string instructionSetSupportString = ReadyToRunInstructionSetSupportSignature.ToInstructionSetSupportString(this);
            ReadyToRunInstructionSetSupportSignature instructionSetSupportSig = new ReadyToRunInstructionSetSupportSignature(instructionSetSupportString);

            rootProvider.AddRoot(new Import(rootProvider.NodeFactory.EagerImports, instructionSetSupportSig), "Baseline instruction set support");
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
            supportMatrix[TargetArchitecture.ARM64] = ComputeInstructSetSupportForArm64();

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
            support["Aes"] = new string[] { "Sse2" };
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

        /// <summary>
        /// Validate that the instruction set support values hardcoded above are actually accurate and up to date
        /// </summary>
        /// <param name="coreLibModule"></param>
        [Conditional("DEBUG")]
        public static void ValidateInstructionSetSupport(ModuleDesc coreLibModule)
        {
            var moduleDefinedSupportMatrix = new Dictionary<TargetArchitecture, Dictionary<string, string[]>>();
            moduleDefinedSupportMatrix[TargetArchitecture.ARM] = new Dictionary<string, string[]>();
            moduleDefinedSupportMatrix[TargetArchitecture.X64] = ComputeInstructionSetSupportMatrixFromPEFile(coreLibModule, "System.Runtime.Intrinsics.X86");
            moduleDefinedSupportMatrix[TargetArchitecture.X86] = moduleDefinedSupportMatrix[TargetArchitecture.X64]; // At the moment support for x86 matches X64;
            moduleDefinedSupportMatrix[TargetArchitecture.ARM64] = ComputeInstructionSetSupportMatrixFromPEFile(coreLibModule, "System.Runtime.Intrinsics.Arm");

            Debug.Assert(moduleDefinedSupportMatrix.Count == s_instructionSetSupport.Count);
            foreach (var moduleDefinedArchitectureSupport in moduleDefinedSupportMatrix)
            {
                Debug.Assert(s_instructionSetSupport.ContainsKey(moduleDefinedArchitectureSupport.Key));
                var instructionSetsOnArch = s_instructionSetSupport[moduleDefinedArchitectureSupport.Key];

                Debug.Assert(moduleDefinedArchitectureSupport.Value.Count == instructionSetsOnArch.Count);
                foreach (var moduleDefinedInstructionSetSupport in moduleDefinedArchitectureSupport.Value)
                {
                    Debug.Assert(instructionSetsOnArch.ContainsKey(moduleDefinedInstructionSetSupport.Key));
                    string[] impliedInstructionSets = instructionSetsOnArch[moduleDefinedInstructionSetSupport.Key];

                    Debug.Assert(moduleDefinedInstructionSetSupport.Value.Length == impliedInstructionSets.Length);
                    foreach (string moduleDefinedImpliedInstructionSet in moduleDefinedInstructionSetSupport.Value)
                    {
                        Debug.Assert(impliedInstructionSets.Contains(moduleDefinedImpliedInstructionSet));
                    }
                }
            }
        }

        private static Dictionary<string, string[]> ComputeInstructionSetSupportMatrixFromPEFile(ModuleDesc coreLibModule, string @namespace)
        {
            var support = new Dictionary<string, string[]>();

            foreach (MetadataType type in coreLibModule.GetAllTypes())
            {
                if (type.Namespace == @namespace)
                {
                    // Ignore the FloatComparisonMode type.
                    if (@namespace == "System.Runtime.Intrinsics.X86" && type.Name == "FloatComparisonMode")
                        continue;

                    // Bmi1 implies Avx even though it doesn't in the api surface.
                    if (type.Name == "Bmi1")
                    {
                        support.Add(type.Name, new string[] { "Avx" });
                        continue;
                    }

                    // Bmi2 implies Avx even though it doesn't in the api surface.
                    if (type.Name == "Bmi2")
                    {
                        support.Add(type.Name, new string[] { "Avx" });
                        continue;
                    }


                    if (type.BaseType == type.Context.GetWellKnownType(WellKnownType.Object))
                    {
                        support.Add(type.Name, Array.Empty<string>());
                    }
                    else
                    {
                        support.Add(type.Name, new string[] { type.BaseType.Name });
                    }
                }
            }

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
                if (_supportedInstructionSets.Add(impliedInstructionSet))
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
