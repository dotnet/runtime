// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using ILCompiler.DependencyAnalysisFramework;
using Mono.Linker.Steps;

namespace Mono.Linker.Steps
{
	public partial class MarkStep
	{
		internal sealed class EdgeVisitor : IDependencyAnalyzerLogEdgeVisitor<NodeFactory>
		{
			MarkStep _markStep;
			public EdgeVisitor (MarkStep markStep) => _markStep = markStep;

			private static bool ShouldBeLogged (DependencyNodeCore<NodeFactory> node, out object? dependencyObject)
			{
				switch (node) {
				case ITracingNode ltn:
					dependencyObject = ltn.DependencyObject;
					return true;
				default:
					dependencyObject = null;
					return false;
				};
			}

			public void VisitEdge (DependencyNodeCore<NodeFactory> nodeDepender, DependencyNodeCore<NodeFactory> nodeDependedOn, string reason)
			{
				if (!(ShouldBeLogged (nodeDependedOn, out var dependee) && ShouldBeLogged (nodeDepender, out var depender)))
					return;
				Debug.Assert (nodeDependedOn is not RootTracingNode);
				DependencyInfo depInfo = new (NodeFactory.StringToDependencyKindMap[reason], depender);
				_markStep.Context.Tracer.AddDirectDependency (dependee!, depInfo, true);
			}

			public void VisitEdge (string root, DependencyNodeCore<NodeFactory> dependedOn)
			{
				Debug.Assert (dependedOn is RootTracingNode or not ITracingNode);
			}

			public void VisitEdge (DependencyNodeCore<NodeFactory> nodeDepender, DependencyNodeCore<NodeFactory> nodeDependerOther, DependencyNodeCore<NodeFactory> nodeDependedOn, string reason)
			{
				if (!(ShouldBeLogged (nodeDependedOn, out var dependee) && ShouldBeLogged (nodeDepender, out var depender)))
					return;
				DependencyInfo depInfo = new (NodeFactory.StringToDependencyKindMap[reason], depender);
				_markStep.Context.Tracer.AddDirectDependency (dependee!, depInfo, true);
			}
		}
	}
}
