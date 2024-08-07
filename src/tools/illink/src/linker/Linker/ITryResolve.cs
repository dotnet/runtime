// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

#nullable enable

namespace Mono.Linker
{
	internal interface ITryResolveMetadata
	{
		MethodDefinition? TryResolve (MethodReference methodReference);
		TypeDefinition? TryResolve (TypeReference typeReference);
		TypeDefinition? TryResolve (ExportedType exportedType);
	}

	internal interface ITryResolveAssemblyName
	{
		AssemblyDefinition? TryResolve (string assemblyName);
	}
}
