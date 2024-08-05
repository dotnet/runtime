using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
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
