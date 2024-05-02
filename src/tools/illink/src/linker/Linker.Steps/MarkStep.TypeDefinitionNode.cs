// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public partial class MarkStep
	{
		internal sealed class TypeDefinitionNode : DependencyNodeCore<NodeFactory>
		{
			readonly TypeDefinition type;

			public TypeDefinitionNode (TypeDefinition type)
			{
				this.type = type;
			}

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => true;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (NodeFactory context)
			{
				context.MarkStep.ProcessType (type);
				return null;
			}

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (NodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
			protected override string GetName (NodeFactory context) => type.GetDisplayName();
		}
	}
}
