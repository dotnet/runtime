// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class MarkStepContext : MarkContext
	{

		public List<Action<AssemblyDefinition>> MarkAssemblyActions { get; }
		public List<Action<TypeDefinition>> MarkTypeActions { get; }
		public List<Action<MethodDefinition>> MarkMethodActions { get; }

		public MarkStepContext ()
		{
			MarkAssemblyActions = new List<Action<AssemblyDefinition>> ();
			MarkTypeActions = new List<Action<TypeDefinition>> ();
			MarkMethodActions = new List<Action<MethodDefinition>> ();
		}

		public override void RegisterMarkAssemblyAction (Action<AssemblyDefinition> action)
		{
			MarkAssemblyActions.Add (action);
		}

		public override void RegisterMarkTypeAction (Action<TypeDefinition> action)
		{
			MarkTypeActions.Add (action);
		}

		public override void RegisterMarkMethodAction (Action<MethodDefinition> action)
		{
			MarkMethodActions.Add (action);
		}
	}
}
