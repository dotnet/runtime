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

            var builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            builder.RequireInitialAlignment(factory.Target.PointerSize);

            int currentDispatchCellOffset = 0;
            foreach (DispatchCellNode node in new SortedSet<DispatchCellNode>(factory.MetadataManager.GetDispatchCells(), new DispatchCellComparer()))
            {
                MethodDesc targetMethod = node.TargetMethod;
                if (targetMethod.HasInstantiation)
                    continue;

                int targetSlot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, targetMethod.OwningType);

                node.InitializeOffset(currentDispatchCellOffset);

                IEETypeNode interfaceType = GetInterfaceTypeNode(factory, targetMethod);
                if (factory.Target.SupportsRelativePointers)
                {
                    builder.EmitReloc(interfaceType, RelocType.IMAGE_REL_BASED_RELPTR32);
                    builder.EmitInt(targetSlot);
                }
                else
                {
                    builder.EmitPointerReloc(interfaceType);
                    builder.EmitNaturalInt(targetSlot);
                }

                currentDispatchCellOffset += node.Size;
            }

            return builder.ToObjectData();
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
