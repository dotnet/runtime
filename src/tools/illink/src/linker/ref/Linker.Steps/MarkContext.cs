// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
