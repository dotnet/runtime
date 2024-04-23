// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public partial class MarkStep
	{
		internal sealed class MethodDefinitionDependencyNode : DependencyNodeCore<MarkStepNodeFactory>
		{
			readonly MethodDefinition method;
			readonly MessageOrigin origin;
			readonly DependencyInfo reason;

			public MethodDefinitionDependencyNode (MethodDefinition method, DependencyInfo reason, MessageOrigin origin)
			{
				this.method = method;
				this.origin = origin;
				this.reason = reason;
			}

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => true;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (MarkStepNodeFactory context)
			{
				context.MarkStep.ProcessMethod (method, reason, origin);
				return null;
			}

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (MarkStepNodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<MarkStepNodeFactory>> markedNodes, int firstNode, MarkStepNodeFactory context) => null;
			protected override string GetName (MarkStepNodeFactory context) => "MethodDefinition";
		}
	}
}
