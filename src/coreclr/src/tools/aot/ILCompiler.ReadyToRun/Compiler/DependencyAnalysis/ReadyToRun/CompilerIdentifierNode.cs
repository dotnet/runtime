// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal class CompilerIdentifierNode : HeaderTableNode
    {
        private static readonly string _compilerIdentifier = "CoreRT Ready-To-Run Compiler";

        public override ObjectNodeSection Section => ObjectNodeSection.ReadOnlyDataSection;

        public override int ClassCode => 230053202;

        public CompilerIdentifierNode(TargetDetails target)
            : base(target)
        {
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__ReadyToRunHeader_CompilerIdentifier");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);
            builder.EmitBytes(Encoding.ASCII.GetBytes(_compilerIdentifier));
            return builder.ToObjectData();
        }
    }
}
