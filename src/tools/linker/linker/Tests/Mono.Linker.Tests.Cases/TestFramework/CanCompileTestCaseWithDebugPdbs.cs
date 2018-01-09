using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

// Because we compiled with debug information, this attribute should exist
[assembly: KeptAttributeAttribute (typeof (System.Diagnostics.DebuggableAttribute))]

namespace Mono.Linker.Tests.Cases.TestFramework {
	[SetupCompileArgument ("/debug:pdbonly")]
	class CanCompileTestCaseWithDebugPdbs {
		static void Main ()
		{
			new Foo ().Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Foo {
			[Kept]
			public void Method ()
			{
			}
		}
	}
}
