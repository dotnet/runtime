// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ObjectFactory
	{
		public virtual TestCaseSandbox CreateSandbox (TestCase testCase)
		{
			return new TestCaseSandbox (testCase);
		}

		public virtual TestCaseCompiler CreateCompiler (TestCaseSandbox sandbox, TestCaseCompilationMetadataProvider metadataProvider)
		{
			return new TestCaseCompiler (sandbox, metadataProvider);
		}

		public virtual LinkerDriver CreateLinker ()
		{
			return new LinkerDriver ();
		}

		public virtual TestCaseMetadataProvider CreateMetadataProvider (TestCase testCase, AssemblyDefinition expectationsAssemblyDefinition)
		{
			return new TestCaseMetadataProvider (testCase, expectationsAssemblyDefinition);
		}

		public virtual TestCaseCompilationMetadataProvider CreateCompilationMetadataProvider (TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
		{
			return new TestCaseCompilationMetadataProvider (testCase, fullTestCaseAssemblyDefinition);
		}

		public virtual LinkerArgumentBuilder CreateLinkerArgumentBuilder (TestCaseMetadataProvider metadataProvider)
		{
			return new LinkerArgumentBuilder (metadataProvider);
		}
	}
}