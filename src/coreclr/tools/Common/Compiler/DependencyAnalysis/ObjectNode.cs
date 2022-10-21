// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public abstract partial class ObjectNode : SortableDependencyNode
    {
        public class ObjectData
        {
            public ObjectData(byte[] data, Relocation[] relocs, int alignment, ISymbolDefinitionNode[] definedSymbols)
            {
                Data = data;
                Relocs = relocs;
                Alignment = alignment;
                DefinedSymbols = definedSymbols;
            }

            public readonly Relocation[] Relocs;
            public readonly byte[] Data;
            public readonly int Alignment;
            public readonly ISymbolDefinitionNode[] DefinedSymbols;
        }

        public virtual bool RepresentsIndirectionCell => false;

        public abstract ObjectData GetData(NodeFactory factory, bool relocsOnly = false);

        public abstract ObjectNodeSection Section { get; }

        /// <summary>
        /// Should identical symbols emitted into separate object files be Comdat folded when linked together?
        /// </summary>
        public abstract bool IsShareable { get; }

        /// <summary>
        /// Override this function to have a node which should be skipped when emitting
        /// to the object file. (For instance, if there are two nodes describing the same
        /// data structure, one of those nodes should return true here.)
        /// </summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        public virtual bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            return false;
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool HasDynamicDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public sealed override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = ComputeNonRelocationBasedDependencies(factory);
            Relocation[] relocs = GetData(factory, true).Relocs;

            if (relocs != null)
            {
                dependencies ??= new DependencyList();

                foreach (Relocation reloc in relocs)
                {
                    dependencies.Add(reloc.Target, "reloc");
                }
            }

            if (dependencies == null)
                return Array.Empty<DependencyListEntry>();
            else
                return dependencies;
        }

        protected virtual DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            return null;
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
