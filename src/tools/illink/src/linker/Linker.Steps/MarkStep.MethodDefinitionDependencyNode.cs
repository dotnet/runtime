// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Mono.Cecil;

namespace Mono.Linker.Steps
{

	public partial class MarkStep
	{
		/// <summary>
		/// A dummy node to postpone processing of a method in the current call stack. The analyzer will process the MethodDefinitionNode dependency later.
		/// </summary>
		public class PostPoneMethodProcessingNode : DependencyNodeCore<MarkStepNodeFactory>
		{
			readonly MethodDefinition method;
			readonly DependencyInfo reason;
			readonly MessageOrigin origin;

			public PostPoneMethodProcessingNode (MethodDefinition method, DependencyInfo reason, MessageOrigin origin)
			{
				this.method = method;
				this.reason = reason;
				this.origin = origin;
			}

			public override bool InterestingForDynamicDependencyAnalysis => false;

			public override bool HasDynamicDependencies => false;

			public override bool HasConditionalStaticDependencies => false;

			public override bool StaticDependenciesAreComputed => true;

			public override IEnumerable<DependencyListEntry>? GetStaticDependencies (MarkStepNodeFactory context)
			{
				yield return new DependencyListEntry(context.MarkStep.GetMethodDefinitionNode (method, reason, origin), "Needed by dummy node");
			}

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (MarkStepNodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<MarkStepNodeFactory>> markedNodes, int firstNode, MarkStepNodeFactory context) => null;
			protected override string GetName (MarkStepNodeFactory context) => "PostPoneMethodMarkingProxy";
		}

		public class MethodDefinitionDependencyNode : DependencyNodeCore<MarkStepNodeFactory>
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
				// Add other types that are marked in MarkType
				yield break;
			}

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (MarkStepNodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<MarkStepNodeFactory>> markedNodes, int firstNode, MarkStepNodeFactory context) => null;
			protected override string GetName (MarkStepNodeFactory context) => "MethodDefinition";
			protected override void OnMarked (MarkStepNodeFactory context)
			{
				context.MarkStep.ProcessMethod (method, reason, origin);
			}
		}
	}
}
