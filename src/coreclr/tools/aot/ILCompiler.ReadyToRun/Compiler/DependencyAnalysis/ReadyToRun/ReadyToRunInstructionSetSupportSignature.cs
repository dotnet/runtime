// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Internal.Text;
using Internal.ReadyToRunConstants;
using System.Text;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ReadyToRunInstructionSetSupportSignature : Signature
    {
        string _instructionSetsSupport;

        public static string ToInstructionSetSupportString(InstructionSetSupport instructionSetSupport)
        {
            StringBuilder builder = new StringBuilder();
            InstructionSet[] supportedInstructionSets = instructionSetSupport.SupportedFlags.ToArray();
            Array.Sort(supportedInstructionSets);
            InstructionSet[] explicitlyUnsupportedInstructionSets = instructionSetSupport.ExplicitlyUnsupportedFlags.ToArray();
            Array.Sort(explicitlyUnsupportedInstructionSets);

            bool addDelimiter = false;
            var r2rAlreadyEmitted = new HashSet<ReadyToRunInstructionSet>();
            foreach (var instructionSetSupported in supportedInstructionSets)
            {
                var r2rInstructionSet = instructionSetSupported.R2RInstructionSet(instructionSetSupport.Architecture);
                if (r2rInstructionSet == null)
                    continue;

                if (r2rAlreadyEmitted.Add(r2rInstructionSet.Value))
                {
                    if (addDelimiter)
                        builder.Append('+');
                    addDelimiter = true;
                    builder.Append(r2rInstructionSet.Value.ToString());
                }
            }

            builder.Append(',');
            r2rAlreadyEmitted.Clear();

            addDelimiter = false;
            foreach (var instructionSetUnsupported in explicitlyUnsupportedInstructionSets)
            {
                var r2rInstructionSet = instructionSetUnsupported.R2RInstructionSet(instructionSetSupport.Architecture);
                if (r2rInstructionSet == null)
                    continue;

                if (r2rAlreadyEmitted.Add(r2rInstructionSet.Value))
                {
                    if (addDelimiter)
                        builder.Append('-');
                    addDelimiter = true;
                    builder.Append(r2rInstructionSet.Value.ToString());
                }
            }

            return builder.ToString();
        }

        public ReadyToRunInstructionSetSupportSignature(string instructionSetsSupport)
        {
            _instructionSetsSupport = instructionSetsSupport;
        }

        private ReadyToRunInstructionSet InstructionSetFromString(string instructionSetString)
        {
            return Enum.Parse<ReadyToRunInstructionSet>(instructionSetString);
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
