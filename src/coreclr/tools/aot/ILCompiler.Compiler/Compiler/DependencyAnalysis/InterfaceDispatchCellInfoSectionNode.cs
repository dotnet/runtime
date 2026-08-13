// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.NativeFormat;
using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using DependencyListEntry = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyListEntry;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a section of the executable where information about interface dispatch cells
    /// is stored.
    /// </summary>
    public class InterfaceDispatchCellInfoSectionNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly ExternalReferencesTableNode _externalReferences;

        public InterfaceDispatchCellInfoSectionNode(ExternalReferencesTableNode externalReferences)
        {
            _externalReferences = externalReferences;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());

            var cells = new List<DispatchCellNode>();
            foreach (DispatchCellNode node in new SortedSet<DispatchCellNode>(factory.MetadataManager.GetDispatchCells(), new DispatchCellInfoComparer()))
            {
                if (node.TargetMethod.HasInstantiation)
                    continue;

                node.InitializeOffset(checked(cells.Count * node.Size));
                cells.Add(node);
            }

            if (cells.Count == 0)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            var writer = new NativeWriter();
            Section section = writer.NewSection();
            var entries = new VertexArray(section);
            section.Place(entries);

            for (int firstCell = 0; firstCell < cells.Count;)
            {
                MethodDesc targetMethod = cells[firstCell].TargetMethod;
                int nextCell = firstCell + 1;
                while (nextCell < cells.Count && cells[nextCell].TargetMethod == targetMethod)
                    nextCell++;

                uint interfaceTypeIndex = _externalReferences.GetIndex(GetInterfaceTypeNode(factory, targetMethod));
                int targetSlot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);
                Vertex entry = writer.GetTuple(
                    writer.GetUnsignedConstant(interfaceTypeIndex),
                    writer.GetUnsignedConstant(checked((uint)targetSlot)));

                for (int cell = firstCell; cell < nextCell; cell += DispatchCellNode.MaxCellInfoLookupDistance)
                    entries.Set(cell, entry);

                firstCell = nextCell;
            }

            entries.ExpandLayout();

            return new ObjectData(writer.Save(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            => sb.Append(nameMangler.CompilationUnitPrefix).Append("__InterfaceDispatchCellInfoSection_Start"u8);
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.InterfaceDispatchCellInfoSection;

        public int Offset => 0;

        public override bool IsShareable => false;

        public override bool StaticDependenciesAreComputed => true;

        public static IEnumerable<DependencyListEntry> GetCellDependencies(NodeFactory factory, MethodDesc targetMethod)
        {
            DependencyList result = new DependencyList();

            if (!factory.VTable(targetMethod.OwningType).HasKnownVirtualMethodUse)
            {
                result.Add(factory.VirtualMethodUse(targetMethod), "Interface method use");
            }

            factory.MetadataManager.GetDependenciesDueToVirtualMethodReflectability(ref result, factory, targetMethod);

            result.Add(GetInterfaceTypeNode(factory, targetMethod), "Interface type");

            return result;
        }

        private static IEETypeNode GetInterfaceTypeNode(NodeFactory factory, MethodDesc targetMethod)
        {
            // If this dispatch cell is ever used with an object that implements IDynamicIntefaceCastable, user code will
            // see a RuntimeTypeHandle representing this interface.
            if (factory.DevirtualizationManager.CanHaveDynamicInterfaceImplementations(targetMethod.OwningType))
            {
                return factory.ConstructedTypeSymbol(targetMethod.OwningType);
            }
            else
            {
                return factory.NecessaryTypeSymbol(targetMethod.OwningType);
            }
        }

    }
}
