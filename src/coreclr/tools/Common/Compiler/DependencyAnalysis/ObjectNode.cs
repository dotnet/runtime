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

        public abstract ObjectNodeSection GetSection(NodeFactory factory);

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

        public sealed override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            ComputeNonRelocationBasedDependencies(sink, factory);
            Relocation[] relocs = GetData(factory, true).Relocs;

            if (relocs != null)
            {
                foreach (Relocation reloc in relocs)
                {
                    sink.Add(reloc.Target, "reloc");
                }
            }

            if (factory.Target.IsWasm && this is IMethodCodeNodeWithTypeSignature wasmMethodCodeNode)
            {
                WasmTypeNode wasmTypeNode = factory.WasmTypeNode(wasmMethodCodeNode.Method);
                sink.Add(wasmTypeNode, "Wasm Method Code Nodes Require Signature");
            }
        }

        protected virtual void ComputeNonRelocationBasedDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
        }

        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory) { }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory factory) { }
    }
}
