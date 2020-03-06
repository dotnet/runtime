// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Internal.Text;
using Internal.ReadyToRunConstants;
using System.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ReadyToRunInstructionSetSupportSignature : Signature
    {
        string _instructionSetsSupport;

        public static string ToInstructionSetSupportString(InstructionSetSupport instructionSetSupport)
        {
            IEnumerable<string> instructionSetsSupported = instructionSetSupport.SupportedInstructionSets;
            IEnumerable<string> instructionSetsExplicitlyUnsupported = instructionSetSupport.ExplicitlyUnsupportedInstructionSets;

            StringBuilder builder = new StringBuilder();
            string[] supportedInstructionSets = instructionSetsSupported.ToArray();
            Array.Sort(supportedInstructionSets);
            string[] explicitlyUnsupportedInstructionSets = instructionSetsExplicitlyUnsupported.ToArray();
            Array.Sort(explicitlyUnsupportedInstructionSets);

            bool addDelimeter = false;
            foreach (string instructionSetSupported in supportedInstructionSets)
            {
                if (addDelimeter)
                    builder.Append('+');
                addDelimeter = true;
                builder.Append(instructionSetSupported);
            }
            builder.Append(',');

            addDelimeter = false;
            foreach (string instructionSetUnsupported in explicitlyUnsupportedInstructionSets)
            {
                if (addDelimeter)
                    builder.Append('-');
                addDelimeter = true;
                builder.Append(instructionSetUnsupported);
            }

            return builder.ToString();
        }

        public ReadyToRunInstructionSetSupportSignature(string instructionSetsSupport)
        {
            _instructionSetsSupport = instructionSetsSupport;
        }

        private ReadyToRunInstructionSet InstructionSetFromString(string instructionSetString)
        {
            var enumEntry = typeof(ReadyToRunInstructionSet).GetField(instructionSetString);
            return (ReadyToRunInstructionSet)enumEntry.GetValue(null);
        }
        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder builder = new ObjectDataSignatureBuilder();
            builder.AddSymbol(this);

            string[] supportedAndUnsupportedSplit = _instructionSetsSupport.Split(',');

            string[] instructionSetsSupported = supportedAndUnsupportedSplit[0] == "" ? Array.Empty<string>() : supportedAndUnsupportedSplit[0].Split('+');
            string[] instructionSetsExplicitlyUnsupported = supportedAndUnsupportedSplit[1] == "" ? Array.Empty<string>() : supportedAndUnsupportedSplit[1].Split('-');

            // This type of fixup is not dependent on module
            builder.EmitByte(checked((byte)ReadyToRunFixupKind.Check_InstructionSetSupport));

            builder.EmitUInt((uint)(instructionSetsSupported.Length + instructionSetsExplicitlyUnsupported.Length));

            foreach (string instructionSetString in instructionSetsSupported)
            {
                uint valueToEmit = (((uint)InstructionSetFromString(instructionSetString)) << 1) | 1;
                builder.EmitUInt(valueToEmit);
            }

            foreach (string instructionSetString in instructionSetsExplicitlyUnsupported)
            {
                uint valueToEmit = (((uint)InstructionSetFromString(instructionSetString)) << 1) | 0;
                builder.EmitUInt(valueToEmit);
            }

            return builder.ToObjectData();
        }

        public override int ClassCode => 56557889;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("ReadyToRunInstructionSets_");
            sb.Append(_instructionSetsSupport);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _instructionSetsSupport.CompareTo(((ReadyToRunInstructionSetSupportSignature)other)._instructionSetsSupport);
        }
    }
}
