// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal class CompilerIdentifierNode : HeaderTableNode
    {
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

        private string GetCompilerVersion()
        {
            return Assembly
                   .GetExecutingAssembly()
                   .GetCustomAttribute<AssemblyFileVersionAttribute>()
                   .Version;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            string compilerIdentifier = $"Crossgen2 {GetCompilerVersion()}";
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);
            builder.EmitBytes(Encoding.ASCII.GetBytes(compilerIdentifier));
            return builder.ToObjectData();
        }
    }
}
