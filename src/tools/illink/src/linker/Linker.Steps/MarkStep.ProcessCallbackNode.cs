// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<Mono.Linker.Steps.MarkStep.NodeFactory>.DependencyList;

namespace Mono.Linker.Steps
{
	public partial class MarkStep
	{
		sealed class ProcessCallbackNode : DependencyNodeCore<NodeFactory>
		{
			public delegate bool ProcessAction(ref DependencyList dependencies);
			ProcessAction _processAction;
			DependencyList? _dependencies;

			public ProcessCallbackNode (ProcessAction action) => _processAction = action;

			public void Process ()
			{
				_dependencies = new DependencyList ();
				if (_processAction (ref _dependencies)) {
					_dependencies.Add (new ProcessCallbackNode (_processAction), "Some processing was done, continuation required");
				}
			}

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => _dependencies != null;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (NodeFactory context) => _dependencies;

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (NodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
			protected override string GetName (NodeFactory context) => "Process";
		}
	}
}
