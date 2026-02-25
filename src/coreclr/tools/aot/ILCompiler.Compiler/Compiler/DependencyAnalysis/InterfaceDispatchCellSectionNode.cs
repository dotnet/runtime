// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a section of the executable where interface dispatch cells
    /// are stored.
    /// </summary>
    public class InterfaceDispatchCellSectionNode : ObjectNode, ISymbolDefinitionNode, INodeWithSize
    {
        private int _size;

        public int Size => _size;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());

            ArrayBuilder<ISymbolDefinitionNode> symbols = default;
            symbols.Add(this);

            int totalSize = 0;
            foreach (InterfaceDispatchCellNode node in factory.MetadataManager.GetInterfaceDispatchCells())
            {
                symbols.Add(node);
                totalSize += node.Size;
            }

            _size = totalSize;

            return new ObjectData(new byte[totalSize], Array.Empty<Relocation>(), factory.Target.PointerSize, symbols.ToArray());
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            => sb.Append(nameMangler.CompilationUnitPrefix).Append("__InterfaceDispatchCellSection_Start"u8);
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.BssSection;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.InterfaceDispatchCellSection;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override bool StaticDependenciesAreComputed => true;
    }
}
