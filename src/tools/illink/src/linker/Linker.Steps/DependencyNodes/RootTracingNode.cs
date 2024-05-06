// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;

namespace Mono.Linker.Steps
{
	public partial class MarkStep
	{
		/// <summary>
		/// This node represents a dependency from an item that does not yet have a node to an item that does, in order to enable dependency tracing.
		/// Dependencies from this node and the dependee will be traced in the dependency analyzer, and the logger will forward the dependency (depender > dependee) to the trimmer dependency tracing loggers.
		/// </summary>
		internal sealed class RootTracingNode : DependencyNodeCore<NodeFactory>, ITracingNode
		{
			readonly ITracingNode _dependee;
			readonly string _reason;
			readonly object? _depender;

			public RootTracingNode (ITracingNode dep, string reason, object? depender)
			{
				this._dependee = dep;
				this._reason = reason;
				this._depender = depender;
			}

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => true;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (NodeFactory context)
			{
				return [new DependencyListEntry (_dependee, _reason)];
			}

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (NodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
			protected override string GetName (NodeFactory context) => _depender?.ToString () ?? "Unknown";
			object? ITracingNode.DependencyObject => _depender;
		}
	}
}
