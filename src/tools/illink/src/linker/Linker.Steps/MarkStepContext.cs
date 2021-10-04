// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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