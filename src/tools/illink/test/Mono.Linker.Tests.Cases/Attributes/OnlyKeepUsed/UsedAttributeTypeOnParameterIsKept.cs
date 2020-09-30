using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class UsedAttributeTypeOnParameterIsKept
	{
		static void Main ()
		{
			new Bar ().Method ("Hello");
			var str = typeof (FooAttribute).ToString ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar
		{
			[Kept]
			public void Method ([Foo][KeptAttributeAttribute (typeof (FooAttribute))] string arg)
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Attribute))]
		class FooAttribute : Attribute
		{
		}
	}
}
