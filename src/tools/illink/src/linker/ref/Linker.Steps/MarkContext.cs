// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public abstract class MarkContext
	{
		public abstract void RegisterMarkAssemblyAction (Action<AssemblyDefinition> action);

		public abstract void RegisterMarkTypeAction (Action<TypeDefinition> action);

		public abstract void RegisterMarkMethodAction (Action<MethodDefinition> action);
	}
}