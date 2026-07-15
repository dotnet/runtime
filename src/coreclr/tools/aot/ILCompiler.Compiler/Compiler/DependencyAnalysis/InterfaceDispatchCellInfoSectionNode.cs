// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
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
        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());

            var cells = new List<DispatchCellNode>();
            foreach (DispatchCellNode node in new SortedSet<DispatchCellNode>(factory.MetadataManager.GetDispatchCells(), new DispatchCellComparer()))
            {
                if (node.TargetMethod.HasInstantiation)
                    continue;

                node.InitializeOffset(checked(cells.Count * node.Size));
                cells.Add(node);
            }

            var descriptorIndices = new Dictionary<MethodDesc, int>();
            var descriptors = new List<MethodDesc>();
            var cellDescriptorIndices = new int[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                MethodDesc targetMethod = cells[i].TargetMethod;
                if (!descriptorIndices.TryGetValue(targetMethod, out int descriptorIndex))
                {
                    descriptorIndex = descriptors.Count;
                    descriptorIndices.Add(targetMethod, descriptorIndex);
                    descriptors.Add(targetMethod);
                }

                cellDescriptorIndices[i] = descriptorIndex;
            }

            var builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            builder.RequireInitialAlignment(factory.Target.PointerSize);

            int pointerSize = factory.Target.SupportsRelativePointers ? sizeof(int) : factory.Target.PointerSize;
            int descriptorSize = checked(pointerSize + sizeof(ushort));
            int directSize = checked(cells.Count * descriptorSize);
            int dictionarySize = DispatchCellInfoEncoding.GetDictionarySize(cells.Count, descriptors.Count, pointerSize, descriptorSize);

            if (dictionarySize < directSize)
            {
                builder.EmitUInt(checked((uint)descriptors.Count));

                int indexSize = DispatchCellInfoEncoding.GetIndexSize(descriptors.Count);
                foreach (int descriptorIndex in cellDescriptorIndices)
                    DispatchCellInfoEncoding.EmitIndex(ref builder, descriptorIndex, indexSize);

                builder.PadAlignment(pointerSize);

                foreach (MethodDesc targetMethod in descriptors)
                    EmitInterfaceType(ref builder, factory, targetMethod);

                foreach (MethodDesc targetMethod in descriptors)
                    EmitSlot(ref builder, factory, targetMethod);
            }
            else
            {
                foreach (DispatchCellNode cell in cells)
                    EmitInterfaceType(ref builder, factory, cell.TargetMethod);

                foreach (DispatchCellNode cell in cells)
                    EmitSlot(ref builder, factory, cell.TargetMethod);
            }

            return builder.ToObjectData();
        }

        private static void EmitInterfaceType(ref ObjectDataBuilder builder, NodeFactory factory, MethodDesc targetMethod)
        {
            IEETypeNode interfaceType = GetInterfaceTypeNode(factory, targetMethod);
            if (factory.Target.SupportsRelativePointers)
                builder.EmitReloc(interfaceType, RelocType.IMAGE_REL_BASED_RELPTR32);
            else
                builder.EmitPointerReloc(interfaceType);
        }

        private static void EmitSlot(ref ObjectDataBuilder builder, NodeFactory factory, MethodDesc targetMethod)
        {
            int targetSlot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);
            builder.EmitUShort(checked((ushort)targetSlot));
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

        /// <summary>
        /// Comparer that groups interface dispatch cells by their callsite.
        /// </summary>
        private sealed class DispatchCellComparer : IComparer<DispatchCellNode>
        {
            private readonly CompilerComparer _comparer = CompilerComparer.Instance;

            public int Compare(DispatchCellNode x, DispatchCellNode y)
            {
                int result = _comparer.Compare(x.CallSiteIdentifier, y.CallSiteIdentifier);
                if (result != 0)
                    return result;

                MethodDesc methodX = x.TargetMethod;
                MethodDesc methodY = y.TargetMethod;

                result = _comparer.Compare(methodX, methodY);
                if (result != 0)
                    return result;

                Debug.Assert(x == y);
                return 0;
            }
        }
    }
}
