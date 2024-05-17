// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Mono.Cecil;

namespace Mono.Linker.Steps
{

	public partial class MarkStep
	{
		internal sealed class PropertyDefinitionNode : DependencyNodeCore<NodeFactory>
		{
			PropertyDefinition _property;
			public PropertyDefinitionNode(PropertyDefinition property)
			{
				_property = property;
			}

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => true;

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (NodeFactory context) => null;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (NodeFactory context)
			{
				var propertyOrigin = new MessageOrigin (_property);

				// Consider making this more similar to MarkEvent method?
				context.MarkStep.MarkCustomAttributes (_property, new DependencyInfo (DependencyKind.CustomAttribute, _property), propertyOrigin);
				context.MarkStep.DoAdditionalPropertyProcessing (_property);
				return null;
			}

			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

			protected override string GetName (NodeFactory context) => _property.GetDisplayName ();
		}
	}
}
