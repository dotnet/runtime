// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public partial class MarkStep
	{
		internal sealed class EventDefinitionNode : DependencyNodeCore<NodeFactory>
		{
			EventDefinition _event;
			public EventDefinitionNode (EventDefinition @event) => _event = @event;

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => true;

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (NodeFactory context) => null;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (NodeFactory context)
			{
				var eventOrigin = new MessageOrigin (_event);
				context.MarkStep.MarkCustomAttributes (_event, new DependencyInfo (DependencyKind.CustomAttribute, _event), eventOrigin);
				context.MarkStep.DoAdditionalEventProcessing (_event);
				return null;
			}

			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

			protected override string GetName (NodeFactory context) => _event.GetDisplayName ();
		}
	}
}
