using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class UnusedAttributeTypeOnTypeIsRemoved
	{
		static void Main ()
		{
			new Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[Foo]
		class Bar
		{
		}

		class FooAttribute : Attribute
		{
		}
	}
}
