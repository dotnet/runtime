// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class HotColdMapNode : HeaderTableNode
    {
        public uint[] mapping;

        public HotColdMapNode(NodeFactory nodeFactory)
            : base(nodeFactory.Target)
        {
        }

        public override int ClassCode => 28963035;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__HotColdMap");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            foreach (uint m in this.mapping)
            {
                builder.EmitUInt(m);
            }
            return builder.ToObjectData();
        }
    }
}
