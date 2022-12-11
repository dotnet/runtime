// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
