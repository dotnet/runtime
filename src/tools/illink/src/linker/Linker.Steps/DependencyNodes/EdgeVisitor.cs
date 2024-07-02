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

			private bool ShouldBeLogged (DependencyNodeCore<NodeFactory> o)
			{
				if (o is ProcessCallbackNode)
					return false;

				if (!_markStep.Context.EnableReducedTracing)
					return true;

				var context = _markStep.Context;

				if (o is TypeDefinitionNode t)
					return DependencyRecorderHelper.WillAssemblyBeModified (context, t.Type.Module.Assembly);

				return true;
			}

			public void VisitEdge (DependencyNodeCore<NodeFactory> nodeDepender, DependencyNodeCore<NodeFactory> nodeDependedOn, string reason)
			{
				if (!(ShouldBeLogged (nodeDependedOn) && ShouldBeLogged (nodeDepender)))
					return;
				var dependerName = DependencyNodeCore<NodeFactory>.GetNodeName (nodeDepender, null!);
				var dependeeName = DependencyNodeCore<NodeFactory>.GetNodeName (nodeDependedOn, null!);
				DependencyInfo depInfo = new (NodeFactory.StringToDependencyKindMap[reason], dependerName);
				_markStep.Context.Tracer.AddDirectDependency (dependeeName!, depInfo, true);
			}

			public void VisitEdge (string root, DependencyNodeCore<NodeFactory> dependedOn)
			{
				// Root nodes will be traced in MarkStep.Mark[Type|Method|Field] and not here
			}

			public void VisitEdge (DependencyNodeCore<NodeFactory> nodeDepender, DependencyNodeCore<NodeFactory> nodeDependerOther, DependencyNodeCore<NodeFactory> nodeDependedOn, string reason)
			{
				if (!(ShouldBeLogged (nodeDependedOn) && ShouldBeLogged (nodeDepender)))
					return;

				var dependerName = DependencyNodeCore<NodeFactory>.GetNodeName (nodeDepender, null!);
				var dependeeName = DependencyNodeCore<NodeFactory>.GetNodeName (nodeDependedOn, null!);
				DependencyInfo depInfo = new (NodeFactory.StringToDependencyKindMap[reason], dependerName);
				_markStep.Context.Tracer.AddDirectDependency (dependeeName!, depInfo, true);
			}
		}
	}
}
