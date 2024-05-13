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
		internal sealed class MethodDefinitionNode : DependencyNodeCore<NodeFactory>
		{
			readonly MethodDefinition method;
			readonly DependencyInfo reason;

			public MethodDefinitionNode (MethodDefinition method, DependencyInfo reason)
			{
				this.method = method;
				this.reason = reason;
			}

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => true;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (NodeFactory context)
			{
				using (_ = context.MarkStep.ScopeStack.PushLocalScope (new MessageOrigin (method))) {
					if (method.HasMetadataParameters ()) {
#pragma warning disable RS0030 // MethodReference.Parameters is banned. It's easiest to leave the code as is for now
						foreach (ParameterDefinition pd in method.Parameters) {
							var type = context.MarkStep.MarkType (pd.ParameterType, new DependencyInfo (DependencyKind.ParameterType, method), null, false);
							if (type is not null)
								yield return new (context.GetTypeNode (type), nameof (DependencyKind.ParameterType));
							context.MarkStep.MarkCustomAttributes (pd, new DependencyInfo (DependencyKind.ParameterAttribute, method));
							context.MarkStep.MarkMarshalSpec (pd, new DependencyInfo (DependencyKind.ParameterMarshalSpec, method));
						}
#pragma warning restore RS0030
					}
				}
				context.MarkStep.ProcessMethod (method, reason);
			}

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (NodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
			protected override string GetName (NodeFactory context) => method.GetDisplayName ();
		}
	}
}
