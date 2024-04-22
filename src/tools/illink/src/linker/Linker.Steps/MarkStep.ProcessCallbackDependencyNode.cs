// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;

namespace Mono.Linker.Steps
{

	public partial class MarkStep
	{
		sealed class ProcessCallbackDependencyNode : DependencyNodeCore<MarkStepNodeFactory>
		{
			Func<bool> _processAction;
			DependencyList? _dependencies;

			public ProcessCallbackDependencyNode (Func<bool> action) => _processAction = action;

			public void Process ()
			{
				_dependencies = new DependencyList ();
				if (_processAction ()) {
					_dependencies.Add (new ProcessCallbackDependencyNode (_processAction), "Some processing was done, continuation required");
				}
			}

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => _dependencies != null;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (MarkStepNodeFactory context) => _dependencies;

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (MarkStepNodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<MarkStepNodeFactory>> markedNodes, int firstNode, MarkStepNodeFactory context) => null;
			protected override string GetName (MarkStepNodeFactory context) => "Process";
		}
	}
}
