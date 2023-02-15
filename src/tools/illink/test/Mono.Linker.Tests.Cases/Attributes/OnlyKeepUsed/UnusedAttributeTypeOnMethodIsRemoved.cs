using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	// System.Core.dll is referenced by System.dll in the .NET FW class libraries. Our GetType reflection marking code
	// detects a GetType("SHA256CryptoServiceProvider") in System.dll, which then causes a type in System.Core.dll to be marked.
	// PeVerify fails on the original GAC copy of System.Core.dll so it's expected that it will also fail on the stripped version we output
	[SkipPeVerify ("System.Core.dll")]
	class UnusedAttributeTypeOnMethodIsRemoved
	{
		static void Main ()
		{
			new Bar ().Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar
		{
			[Foo]
			[Kept]
			public void Method ()
			{
			}
		}

		class FooAttribute : Attribute
		{
		}
	}
}
