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
                builder.Append(instructionSetSupported));
            }
            builder.Append(',');

            addDelimeter = false;
            foreach (string instructionSetUnsupported in explicitlyUnsupportedInstructionSets)
            {
                if (addDelimeter)
                    builder.Append('-');
                addDelimeter = true;
                builder.Append(instructionSetUnsupported));
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

            string[] instructionSetsSupported = supportedAndUnsupportedSplit[0].Split('+');
            string[] instructionSetsExplicitlyUnsupported = supportedAndUnsupportedSplit[1].Split('-');
            builder.EmitInt(instructionSetsSupported.Length + instructionSetsExplicitlyUnsupported.Length);

            foreach (string instructionSetString in instructionSetsSupported)
            {
                int valueToEmit = (((int)InstructionSetFromString(instructionSetString)) << 1) | 1;
                builder.EmitInt(valueToEmit);
            }

            foreach (string instructionSetString in instructionSetsExplicitlyUnsupported)
            {
                int valueToEmit = (((int)InstructionSetFromString(instructionSetString)) << 1) | 0;
                builder.EmitInt(valueToEmit);
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
