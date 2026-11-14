// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer
{
    internal sealed class AnalyzerCompilationState
    {
        private readonly ConditionalWeakTable<INamedTypeSymbol, HierarchyInfo> _interestingTypes = new();
        private readonly ConditionalWeakTable<ControlFlowGraph, GraphInfo> _graphs = new();

        public FlowAnnotations FlowAnnotations { get; }

        public AnalyzerCompilationState() => FlowAnnotations = new FlowAnnotations(this);

        public bool IsTypeInterestingForDataflow(ITypeSymbol type, bool isByRef)
        {
            if (type.SpecialType != SpecialType.None)
            {
                return type.SpecialType == SpecialType.System_String && !isByRef;
            }

            return type is INamedTypeSymbol namedType &&
                _interestingTypes.GetValue(namedType.OriginalDefinition,
                    static definition => new HierarchyInfo(ITypeSymbolExtensions.IsNamedTypeInterestingForDataflow(definition))).Interesting;
        }

        public ControlFlowGraphProxy GetControlFlowGraph(ControlFlowGraph graph) =>
            _graphs.GetValue(graph, static cfg => new GraphInfo(cfg)).Graph;

        private sealed class GraphInfo(ControlFlowGraph graph)
        {
            public ControlFlowGraphProxy Graph { get; } = new(graph);
        }

        private sealed class HierarchyInfo(bool interesting)
        {
            public bool Interesting { get; } = interesting;
        }
    }
}
