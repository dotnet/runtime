﻿using Mono.Cecil;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class ObjectFactory {
		public virtual TestCaseSandbox CreateSandbox (TestCase testCase)
		{
			return new TestCaseSandbox (testCase);
		}

		public virtual TestCaseCompiler CreateCompiler ()
		{
			return new TestCaseCompiler ();
		}

		public virtual LinkerDriver CreateLinker ()
		{
			return new LinkerDriver ();
		}
		
		public virtual TestCaseMetadaProvider CreateMetadataProvider (TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
		{
			return new TestCaseMetadaProvider (testCase, fullTestCaseAssemblyDefinition);
		}

		public virtual LinkerArgumentBuilder CreateLinkerArgumentBuilder ()
		{
			return new LinkerArgumentBuilder ();
		}
	}
}