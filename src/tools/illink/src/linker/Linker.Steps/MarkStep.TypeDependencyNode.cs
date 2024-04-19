// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// MarkStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
// (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
				// Add other types that are marked in MarkType
				yield break;
			}

			public override IEnumerable<CombinedDependencyListEntry>? GetConditionalStaticDependencies (MarkStepNodeFactory context) => null;
			public override IEnumerable<CombinedDependencyListEntry>? SearchDynamicDependencies (List<DependencyNodeCore<MarkStepNodeFactory>> markedNodes, int firstNode, MarkStepNodeFactory context) => null;
			protected override string GetName (MarkStepNodeFactory context) => "TypeNode";
			protected override void OnMarked (MarkStepNodeFactory context)
			{
				using MarkScopeStack.LocalScope? localScope = origin.HasValue ? context.MarkStep.ScopeStack.PushLocalScope (origin.Value) : null;
				context.MarkStep.MarkTypeImpl (type, reason, origin);
			}
		}
	}
}
