// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	/// <summary>
	/// Context which can be used to register actions to call during MarkStep
	/// when various members are marked.
	/// </summary>
	public abstract class MarkContext
	{
		/// <summary>
		/// Register a callback that will be invoked once for each marked assembly.
		/// </summary>
		public abstract void RegisterMarkAssemblyAction (Action<AssemblyDefinition> action);

		/// <summary>
		/// Register a callback that will be invoked once for each marked type.
		/// </summary>
		public abstract void RegisterMarkTypeAction (Action<TypeDefinition> action);

		/// <summary>
		/// Register a callback that will be invoked once for each marked method.
		/// </summary>
		public abstract void RegisterMarkMethodAction (Action<MethodDefinition> action);
	}
}