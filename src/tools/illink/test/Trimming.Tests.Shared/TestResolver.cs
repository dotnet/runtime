// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker.Tests.TestCasesRunner
{
	struct TestResolver : ITryResolveMetadata
	{
		public MethodDefinition TryResolve (MethodReference methodReference) => methodReference.Resolve ();

		public TypeDefinition TryResolve (TypeReference typeReference) => typeReference.Resolve ();

		public TypeDefinition TryResolve (ExportedType exportedType) => exportedType.Resolve ();
	}
}
