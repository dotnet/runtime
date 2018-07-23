﻿using Mono.Cecil;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class ObjectFactory {
		public virtual TestCaseSandbox CreateSandbox (TestCase testCase)
		{
			return new TestCaseSandbox (testCase);
		}

		public virtual TestCaseCompiler CreateCompiler (TestCaseSandbox sandbox, TestCaseMetadaProvider metadataProvider)
		{
			return new TestCaseCompiler (sandbox, metadataProvider);
		}

		public virtual LinkerDriver CreateLinker ()
		{
			return new LinkerDriver ();
		}
		
		public virtual TestCaseMetadaProvider CreateMetadataProvider (TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
		{
			return new TestCaseMetadaProvider (testCase, fullTestCaseAssemblyDefinition);
		}

		public virtual LinkerArgumentBuilder CreateLinkerArgumentBuilder (TestCaseMetadaProvider metadataProvider)
		{
			return new LinkerArgumentBuilder (metadataProvider);
		}
	}
}