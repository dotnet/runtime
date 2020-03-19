// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Thunkerator
{
    public class InstructionSetGenerator
    {
        class InstructionSetInfo
        {
            public string Architecture { get; }
            public string ManagedName { get; }
            public string R2rName { get; }
            public string R2rNumericValue { get; }
            public string JitName { get; }

            public InstructionSetInfo(string architecture, string managedName, string r2rName, string r2rNumericValue, string jitName)
            {
                Architecture = architecture;
                ManagedName = managedName;
                R2rName = String.IsNullOrEmpty(r2rName) ? managedName : r2rName;
                R2rNumericValue = r2rNumericValue;
                JitName = jitName;
            }

            public InstructionSetInfo(string architecture, InstructionSetInfo similarInstructionSet)
            {
                Architecture = architecture;
                ManagedName = similarInstructionSet.ManagedName;
                R2rName = similarInstructionSet.R2rName;
                R2rNumericValue = similarInstructionSet.R2rNumericValue;
                JitName = similarInstructionSet.JitName;
            }

            public string PublicName
            {
                get
                {
                    if (!String.IsNullOrEmpty(ManagedName))
                        return ManagedName;
                    else if (!String.IsNullOrEmpty(R2rName))
                        return R2rName;
                    else
                        return JitName;
                }
            }
        }

        class InstructionSetImplication
        {
            public string Architecture { get; }
            public string JitName { get; }
            public string ImpliedJitName { get; }

            public InstructionSetImplication(string architecture, string jitName, string impliedJitName)
            {
                Architecture = architecture;
                JitName = jitName;
                ImpliedJitName = impliedJitName;
            }

            public InstructionSetImplication(string architecture, InstructionSetImplication similarInstructionSet)
            {
                Architecture = architecture;
                ImpliedJitName = similarInstructionSet.ImpliedJitName;
                JitName = similarInstructionSet.JitName;
            }
        }

        List<InstructionSetInfo> _instructionSets = new List<InstructionSetInfo>();
        List<InstructionSetImplication> _implications = new List<InstructionSetImplication>();
        Dictionary<string, HashSet<string>> _64bitVariants = new Dictionary<string, HashSet<string>>();
        SortedDictionary<string,int> _r2rNamesByName = new SortedDictionary<string,int>();
        SortedDictionary<int,string> _r2rNamesByNumber = new SortedDictionary<int,string>();
        SortedSet<string> _architectures = new SortedSet<string>();
        Dictionary<string,List<string>> _architectureJitNames = new Dictionary<string,List<string>>();
        HashSet<string> _64BitArchitectures = new HashSet<string>();
        Dictionary<string,string> _64BitVariantArchitectureJitNameSuffix = new Dictionary<string,string>();

        void ArchitectureEncountered(string arch)
        {
            if (!_64bitVariants.ContainsKey(arch))
                _64bitVariants.Add(arch, new HashSet<string>());
            _architectures.Add(arch);
            if (!_architectureJitNames.ContainsKey(arch))
                _architectureJitNames.Add(arch, new List<string>());
        }

        void ValidateArchitectureEncountered(string arch)
        {
            if (!_architectures.Contains(arch))
                throw new Exception("Architecture not defined");
        }

        private string ArchToIfDefArch(string arch)
        {
            if (arch == "X64")
                return "AMD64";
            return arch;
        }


        private string ArchToInstructionSetSuffixArch(string arch)
        {
            return _64BitVariantArchitectureJitNameSuffix[arch];
        }

        public bool ParseInput(TextReader tr)
        {
            int currentLineIndex = 1;
            for (string currentLine = tr.ReadLine(); currentLine != null; currentLine = tr.ReadLine(), currentLineIndex++)
            {
                try
                {
                    if (currentLine.Length == 0)
                    {
                        continue; // Its an empty line, ignore
                    }

                    if (currentLine[0] == ';')
                    {
                        continue; // Its a comment
                    }

                    string[] command = currentLine.Split(',');
                    for (int i = 0; i < command.Length; i++)
                    {
                        command[i] = command[i].Trim();
                    }
                    switch(command[0])
                    {
                        case "definearch":
                            if (command.Length != 4)
                                throw new Exception($"Incorrect number of args for definearch {command.Length}");
                            ArchitectureEncountered(command[1]);
                            if (command[2] == "64Bit")
                            {
                                _64BitArchitectures.Add(command[1]);
                            }
                            else if (command[2] != "32Bit")
                            {
                                throw new Exception("Architecture must be 32Bit or 64Bit");
                            }
                            _64BitVariantArchitectureJitNameSuffix[command[1]] = command[3];
                            break;
                        case "instructionset":
                            if (command.Length != 6)
                                throw new Exception("Incorrect number of args for instructionset");
                            ValidateArchitectureEncountered(command[1]);
                            _architectureJitNames[command[1]].Add(command[5]);
                            _instructionSets.Add(new InstructionSetInfo(command[1],command[2],command[3],command[4],command[5]));
                            break;
                        case "instructionset64bit":
                            if (command.Length != 3)
                                throw new Exception("Incorrect number of args for instructionset");
                            ValidateArchitectureEncountered(command[1]);
                            _64bitVariants[command[1]].Add(command[2]);
                            _architectureJitNames[command[1]].Add(command[2] + "_" + ArchToInstructionSetSuffixArch(command[1]));
                            break;
                        case "implication":
                            if (command.Length != 4)
                                throw new Exception("Incorrect number of args for instructionset");
                            ValidateArchitectureEncountered(command[1]);
                            _implications.Add(new InstructionSetImplication(command[1],command[2], command[3]));
                            break;
                        case "copyinstructionsets":
                            if (command.Length != 3)
                                throw new Exception("Incorrect number of args for instructionset");
                            ValidateArchitectureEncountered(command[1]);
                            ValidateArchitectureEncountered(command[2]);
                            string arch = command[1];
                            string targetarch = command[2];
                            foreach (var val in _instructionSets.ToArray())
                            {
                                if (val.Architecture != arch)
                                    continue;
                                _instructionSets.Add(new InstructionSetInfo(targetarch, val));
                                _architectureJitNames[targetarch].Add(val.JitName);
                            }
                            foreach (var val in _implications.ToArray())
                            {
                                if (val.Architecture != arch)
                                    continue;
                                _implications.Add(new InstructionSetImplication(targetarch, val));
                            }
                            foreach (var val in _64bitVariants[arch])
                            {
                                _64bitVariants[targetarch].Add(val);
                                _architectureJitNames[targetarch].Add(val + "_" + ArchToInstructionSetSuffixArch(targetarch));
                            }
                            break;
                        default:
                            throw new Exception("Unknown command");
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error parsing line {0} : {1}", currentLineIndex, e.Message);
                    return false;
                }
            }

            foreach (var instructionSet in _instructionSets)
            {
                if (!String.IsNullOrEmpty(instructionSet.R2rName))
                {
                    int r2rValue = Int32.Parse(instructionSet.R2rNumericValue);
                    if (_r2rNamesByName.ContainsKey(instructionSet.R2rName))
                    {
                        if (_r2rNamesByName[instructionSet.R2rName] != r2rValue)
                            throw new Exception("R2R name/number mismatch");
                    }
                    else
                    {
                        _r2rNamesByName.Add(instructionSet.R2rName, r2rValue);
                        _r2rNamesByNumber.Add(r2rValue, instructionSet.R2rName);
                    }
                }
            }

            foreach (var architectureInfo in _architectureJitNames)
            {
                if (architectureInfo.Value.Count > 62)
                {
                    throw new Exception("Too many instruction sets added. Scheme of using uint64_t as instruction mask will need updating");
                }
            }

            return true;
        }

        public void WriteManagedReadyToRunInstructionSet(TextWriter tr)
        {
            // Write header
            tr.Write(@"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// DO NOT EDIT THIS FILE! IT IS AUTOGENERATED
// FROM /src/coreclr/src/tools/Common/JitInterface/ThunkGenerator/InstructionSetDesc.txt
// using /src/coreclr/src/tools/Common/JitInterface/ThunkGenerator/gen.bat

using System;
using System.Runtime.InteropServices;
using Internal.TypeSystem;

namespace Internal.ReadyToRunConstants
{
    public enum ReadyToRunInstructionSet
    {
");

            foreach (var r2rEntry in _r2rNamesByNumber)
            {
                tr.WriteLine($"        {r2rEntry.Value}={r2rEntry.Key},");
            }
            tr.Write(@"
    }

    public static class ReadyToRunInstructionSetHelper
    {
        ReadyToRunInstructionSet? R2RInstructionSetFromJitInstructionSet(TargetArchitecture architecture, Internal.JitInterface.InstructionSet instructionSet)
        {
            switch (architecture)
            {
");
            foreach (string architecture in _architectures)
            {
                tr.Write($@"
                case TargetArchitecture.{architecture}:
                    {{
                        switch (instructionSet)
                        {{
");
                foreach (var instructionSet in _instructionSets)
                {
                    if (instructionSet.Architecture != architecture) continue;

                    string r2rEnumerationValue;
                    if (!String.IsNullOrEmpty(instructionSet.R2rName))
                        r2rEnumerationValue = $"ReadyToRunInstructionSet.{instructionSet.R2rName}";
                    else
                        r2rEnumerationValue = $"null";

                    tr.WriteLine($"                            case InstructionSet.{architecture}_{instructionSet.JitName}: return {r2rEnumerationValue};");
                    if (_64BitArchitectures.Contains(architecture) && _64bitVariants[architecture].Contains(instructionSet.JitName))
                        tr.WriteLine($"                            case InstructionSet.{architecture}_{instructionSet.JitName}_{ArchToInstructionSetSuffixArch(architecture)}: return {r2rEnumerationValue};");
                }

                tr.Write(@"
                            default: throw new Exception(""Unknown instruction set"");
                        }
                    }
");
            }

            tr.Write(@"
                default: throw new Exception(""Unknown architecture"");
            }
        }
    }
}
");
            }

        public void WriteManagedJitInstructionSet(TextWriter tr)
        {
            // Write header
            tr.Write(@"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// DO NOT EDIT THIS FILE! IT IS AUTOGENERATED
// FROM /src/coreclr/src/tools/Common/JitInterface/ThunkGenerator/InstructionSetDesc.txt
// using /src/coreclr/src/tools/Common/JitInterface/ThunkGenerator/gen.bat

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    public enum InstructionSet
    {
        ILLEGAL = 0,
        NONE = 63,
");
            foreach (string architecture in _architectures)
            {
                int counter = 1;
                foreach (var jitName in _architectureJitNames[architecture])
                {
                    tr.WriteLine($"        {architecture}_{jitName}={counter++},");
                }
            }

            tr.Write(@"
    }

    public struct InstructionSetFlags
    {
        ulong _flags;
        
        public void AddInstructionSet(InstructionSet instructionSet)
        {
            _flags = _flags | (((ulong)1) << (int)instructionSet);
        }

        public void RemoveInstructionSet(InstructionSet instructionSet)
        {
            _flags = _flags & ~(((ulong)1) << (int)instructionSet);
        }

        public bool HasInstructionSet(InstructionSet instructionSet)
        {
            return (_flags & (((ulong)1) << (int)instructionSet)) != 0;
        }

        public bool Equals(InstructionSetFlags other)
        {
            return _flags == other._flags;
        }

        public static InstructionSetFlags ExpandInstructionSetByImplication(TargetArchitecture architecture, InstructionSetFlags input)
        {
            InstructionSetFlags oldflags = input;
            InstructionSetFlags resultflags = input;
            do
            {
                oldflags = resultflags;
                switch(architecture)
                {
");
            foreach (string architecture in _architectures)
            {
                tr.Write($@"
                case TargetArchitecture.{architecture}:
");
                foreach (var instructionSet in _instructionSets)
                {
                    if (instructionSet.Architecture != architecture) continue;
                    if (_64BitArchitectures.Contains(architecture) && _64bitVariants[architecture].Contains(instructionSet.JitName))
                        AddImplication(architecture, instructionSet.JitName, $"{instructionSet.JitName}_{ArchToInstructionSetSuffixArch(architecture)}");
                }
                foreach (var implication in _implications)
                {
                    if (implication.Architecture != architecture) continue;
                    AddImplication(architecture, implication.JitName, implication.ImpliedJitName);
                }
                tr.WriteLine("                    break;");
            }

            tr.Write(@"
                }
            } while (!oldflags.Equals(resultflags));
            return resultflags;
        }

        public static IEnumerable<KeyValuePair<string,InstructionSet>> ArchitectureToValidInstructionSets(TargetArchitecture architecture)
        {
            switch (architecture)
            {
");
            foreach (string architecture in _architectures)
            {
                tr.Write($@"
                case TargetArchitecture.{architecture}:
");
                foreach (var instructionSet in _instructionSets)
                {
                    if (instructionSet.Architecture != architecture) continue;
                    tr.WriteLine($"                    yield return new KeyValuePair<string, InstructionSet>(\"{instructionSet.PublicName}\", InstructionSet.{architecture}_{instructionSet.JitName});");
                }
                tr.WriteLine("                    break;");
            }
            tr.Write(@"
            }
        }

        public void Set64BitInstructionSetVariants(TargetArchitecture architecture)
        {
            switch (architecture)
            {
");
            foreach (string architecture in _architectures)
            {
                tr.Write($@"
                case TargetArchitecture.{architecture}:
");
                foreach (var instructionSet in _instructionSets)
                {
                    if (instructionSet.Architecture != architecture) continue;

                    if (_64BitArchitectures.Contains(architecture) && _64bitVariants[architecture].Contains(instructionSet.JitName))
                    {
                        tr.WriteLine($"                    if (HasInstructionSet(InstructionSet.{architecture}_{instructionSet.JitName}))");
                        tr.WriteLine($"                        AddInstructionSet(InstructionSet.{architecture}_{instructionSet.JitName}_{ArchToInstructionSetSuffixArch(architecture)});");
                    }
                }

                tr.WriteLine("                    break;");
            }
            tr.Write(@"
            }
        }
    }
}
");
            return;
            void AddImplication(string architecture, string jitName, string impliedJitName)
            {
                tr.WriteLine($"                    if (resultflags.HasInstructionSet(InstructionSet.{architecture}_{jitName}))");
                tr.WriteLine($"                        resultflags.AddInstructionSet(InstructionSet.{architecture}_{impliedJitName});");
            }
        }

        public void WriteNativeCorInfoInstructionSet(TextWriter tr)
        {
            // Write header
            tr.Write(@"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// DO NOT EDIT THIS FILE! IT IS AUTOGENERATED
// FROM /src/coreclr/src/tools/Common/JitInterface/ThunkGenerator/InstructionSetDesc.txt
// using /src/coreclr/src/tools/Common/JitInterface/ThunkGenerator/gen.bat

#ifndef CORINFOINSTRUCTIONSET_H
#define CORINFOINSTRUCTIONSET_H

enum CORINFO_InstructionSet
{
    InstructionSet_ILLEGAL = 0,
    InstructionSet_NONE = 63,
");
            foreach (string architecture in _architectures)
            {
                tr.WriteLine($"#ifdef TARGET_{ArchToIfDefArch(architecture)}");
                int counter = 1;
                foreach (var jitName in _architectureJitNames[architecture])
                {
                    tr.WriteLine($"    InstructionSet_{jitName}={counter++},");
                }
                tr.WriteLine($"#endif // TARGET_{ArchToIfDefArch(architecture)}");
            }
            tr.Write(@"
};

struct CORINFO_InstructionSetFlags
{
private:
    uint64_t _flags = 0;
public:
    void AddInstructionSet(CORINFO_InstructionSet instructionSet)
    {
        _flags = _flags | (((uint64_t)1) << instructionSet);
    }

    void RemoveInstructionSet(CORINFO_InstructionSet instructionSet)
    {
        _flags = _flags & ~(((uint64_t)1) << instructionSet);
    }

    bool HasInstructionSet(CORINFO_InstructionSet instructionSet) const
    {
        return _flags & (((uint64_t)1) << instructionSet);
    }

    bool Equals(CORINFO_InstructionSetFlags other) const
    {
        return _flags == other._flags;
    }

    void Add(CORINFO_InstructionSetFlags other)
    {
        _flags |= other._flags;
    }

    bool IsEmpty() const
    {
        return _flags == 0;
    }

    void Reset()
    {
        _flags = 0;
    }

    void Set64BitInstructionSetVariants()
    {
");
            foreach (string architecture in _architectures)
            {
                tr.WriteLine($"#ifdef TARGET_{ArchToIfDefArch(architecture)}");
                foreach (var instructionSet in _instructionSets)
                {
                    if (instructionSet.Architecture != architecture) continue;

                    if (_64BitArchitectures.Contains(architecture) && _64bitVariants[architecture].Contains(instructionSet.JitName))
                    {
                        tr.WriteLine($"        if (HasInstructionSet(InstructionSet_{instructionSet.JitName}))");
                        tr.WriteLine($"            AddInstructionSet(InstructionSet_{instructionSet.JitName}_{ArchToInstructionSetSuffixArch(architecture)});");
                    }
                }

                tr.WriteLine($"#endif // TARGET_{ArchToIfDefArch(architecture)}");
            }
            tr.Write(@"
    }

    uint64_t GetFlagsRaw()
    {
        return _flags;
    }

    void SetFromFlagsRaw(uint64_t flags)
    {
        _flags = flags;
    }
};

inline CORINFO_InstructionSetFlags EnsureInstructionSetFlagsAreValid(CORINFO_InstructionSetFlags input)
{
    CORINFO_InstructionSetFlags oldflags = input;
    CORINFO_InstructionSetFlags resultflags = input;
    do
    {
        oldflags = resultflags;
");
            foreach (string architecture in _architectures)
            {
                tr.WriteLine($"#ifdef TARGET_{ArchToIfDefArch(architecture)}");
                foreach (var instructionSet in _instructionSets)
                {
                    if (instructionSet.Architecture != architecture) continue;
                    if (_64BitArchitectures.Contains(architecture) && _64bitVariants[architecture].Contains(instructionSet.JitName))
                        AddImplication(architecture, instructionSet.JitName, $"{instructionSet.JitName}_{ArchToInstructionSetSuffixArch(architecture)}");
                }
                foreach (var implication in _implications)
                {
                    if (implication.Architecture != architecture) continue;
                    AddImplication(architecture, implication.JitName, implication.ImpliedJitName);
                }
                tr.WriteLine($"#endif // TARGET_{ArchToIfDefArch(architecture)}");
            }
            tr.Write(@"
    } while (!oldflags.Equals(resultflags));
    return resultflags;
}



#endif // CORINFOINSTRUCTIONSET_H
");
            return;

            void AddImplication(string architecture, string jitName, string impliedJitName)
            {
                tr.WriteLine($"        if (resultflags.HasInstructionSet(InstructionSet_{jitName}) && !resultflags.HasInstructionSet(InstructionSet_{impliedJitName}))");
                tr.WriteLine($"            resultflags.RemoveInstructionSet(InstructionSet_{jitName});");
            }
        }

        public void WriteNativeReadyToRunInstructionSet(TextWriter tr)
        {
            // Write header
            tr.Write(@"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// DO NOT EDIT THIS FILE! IT IS AUTOGENERATED
// FROM /src/coreclr/src/tools/Common/JitInterface/ThunkGenerator/InstructionSetDesc.txt
// using /src/coreclr/src/tools/Common/JitInterface/ThunkGenerator/gen.bat

#ifndef READYTORUNINSTRUCTIONSET_H
#define READYTORUNINSTRUCTIONSET_H
enum ReadyToRunInstructionSet
{
");

            foreach (var r2rEntry in _r2rNamesByNumber)
            {
                tr.WriteLine($"    READYTORUN_INSTRUCTION_{r2rEntry.Value}={r2rEntry.Key},");
            }
            tr.Write(@"
};

#endif // READYTORUNINSTRUCTIONSET_H
");
        }
    }
}