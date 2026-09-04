// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class InvalidProxyTypeMapNode(TypeDesc typeMapGroup, MethodDesc throwingMethodStub) : SortableDependencyNode, IProxyTypeMapNode
    {
        public TypeDesc TypeMapGroup { get; } = typeMapGroup;
        public MethodDesc ThrowingMethodStub { get; } = throwingMethodStub;

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory context) { }
        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory context)
        {
            sink.Add(context.MethodEntrypoint(ThrowingMethodStub), "Throwing method stub for invalid type map");
        }

        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory context) { }
        protected override string GetName(NodeFactory context) => $"Invalid proxy type map: {TypeMapGroup}";

        public override int ClassCode => 36910224;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer) => comparer.Compare(TypeMapGroup, ((InvalidProxyTypeMapNode)other).TypeMapGroup);
        public Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, INativeFormatTypeReferenceProvider externalReferences)
        {
            Vertex typeMapStateVertex = writer.GetUnsignedConstant(0); // Invalid type map state
            Vertex typeMapGroupVertex = externalReferences.EncodeReferenceToType(writer, TypeMapGroup, null);
            Vertex throwingMethodStubVertex = externalReferences.EncodeReferenceToMethod(writer, ThrowingMethodStub);
            Vertex tuple = writer.GetTuple(typeMapGroupVertex, typeMapStateVertex, throwingMethodStubVertex);
            return section.Place(tuple);
        }

        public IProxyTypeMapNode ToAnalysisBasedNode(NodeFactory factory) => new InvalidProxyTypeMapNode(TypeMapGroup, ThrowingMethodStub);
    }
}
