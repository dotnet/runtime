// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker.Tests.TestCasesRunner
{
	struct TestAssemblyNameResolver : ITryResolveAssemblyName
	{
		readonly BaseAssemblyResolver _assemblyResolver;

		public TestAssemblyNameResolver (BaseAssemblyResolver resolver)
		{
			_assemblyResolver = resolver;
		}

		public AssemblyDefinition TryResolve (string assemblyName)
			=> _assemblyResolver.Resolve (new AssemblyNameReference (assemblyName, null), new ReaderParameters ());
	}
}
