// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Mono.Cecil;

namespace Mono.Linker.Steps
{

	public partial class MarkStep
	{
		public class TypeDefinitionDependencyNode : DependencyNodeCore<MarkStepNodeFactory>
		{
			readonly TypeDefinition type;
			readonly MessageOrigin? origin;
			readonly DependencyInfo reason;

			public TypeDefinitionDependencyNode (TypeDefinition type, DependencyInfo reason, MessageOrigin? origin)
			{
				this.type = type;
				this.origin = origin;
				this.reason = reason;
			}

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => true;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (MarkStepNodeFactory context)
			{
				yield break;
			}

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (MarkStepNodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<MarkStepNodeFactory>> markedNodes, int firstNode, MarkStepNodeFactory context) => null;
			protected override string GetName (MarkStepNodeFactory context) => "TypeNode";
			protected override void OnMarked (MarkStepNodeFactory context)
			{
				using MarkScopeStack.LocalScope? localScope = origin.HasValue ? context.MarkStep.ScopeStack.PushLocalScope (origin.Value) : null;
				context.MarkStep.ProcessType (type, reason, origin);
			}
		}
	}
}
