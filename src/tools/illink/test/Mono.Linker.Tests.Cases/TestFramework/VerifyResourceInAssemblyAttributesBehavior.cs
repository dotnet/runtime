using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	[SetupCompileResource ("Dependencies/VerifyResourceInAssemblyAttributesBehavior.txt")]
	[KeptResource ("VerifyResourceInAssemblyAttributesBehavior.txt")]
	// These are technically redundant, but the goal of this test is to verify the attributes are working which we can do
	// by using them on the test case assembly even though you would normally use these attributes for other checking other
	// supporting assemblies
	[KeptResourceInAssembly ("test.exe", "VerifyResourceInAssemblyAttributesBehavior.txt")]
	[RemovedResourceInAssembly ("test.exe", "NeverExistedResource.txt")]
	public class VerifyResourceInAssemblyAttributesBehavior
	{
		public static void Main ()
		{
		}
	}
}
