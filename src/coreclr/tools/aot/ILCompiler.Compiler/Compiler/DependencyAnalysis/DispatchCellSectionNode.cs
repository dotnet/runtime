// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Runtime;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a section of the executable where dispatch cells are stored.
    /// </summary>
    public abstract class DispatchCellSectionNode : ObjectNode, ISymbolDefinitionNode
    {
        protected abstract bool ShouldEmitCell(DispatchCellNode cell);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());

            ArrayBuilder<ISymbolDefinitionNode> symbols = default;
            symbols.Add(this);

            int totalSize = 0;
            foreach (DispatchCellNode node in factory.MetadataManager.GetDispatchCells())
            {
                if (!ShouldEmitCell(node))
                    continue;

                symbols.Add(node);
                totalSize += node.Size;
            }

            return new ObjectData(new byte[totalSize], Array.Empty<Relocation>(), factory.Target.PointerSize, symbols.ToArray());
        }

        public abstract void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.BssSection;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public int Offset => 0;
        public override bool IsShareable => false;
        public override bool StaticDependenciesAreComputed => true;
    }

    public class InterfaceDispatchCellSectionNode : DispatchCellSectionNode
    {
        protected override bool ShouldEmitCell(DispatchCellNode cell) => !cell.TargetMethod.HasInstantiation;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            => sb.Append(nameMangler.CompilationUnitPrefix).Append("__InterfaceDispatchCellSection_Start"u8);

        public override int ClassCode => (int)ObjectNodeOrder.InterfaceDispatchCellSection;
    }

    public class GvmDispatchCellSectionNode : DispatchCellSectionNode
    {
        protected override bool ShouldEmitCell(DispatchCellNode cell) => cell.TargetMethod.HasInstantiation;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            => sb.Append(nameMangler.CompilationUnitPrefix).Append("__GvmDispatchCellSection_Start"u8);

        public override int ClassCode => (int)ObjectNodeOrder.GvmDispatchCellSection;
    }
}
