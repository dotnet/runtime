using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class UsedAttributeTypeOnMethodIsKept
	{
		static void Main ()
		{
			new Bar ().Method ();
			var tmp = new Bar ();
			var str = typeof (FooAttribute).ToString ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar
		{
			[Foo]
			[Kept]
			[KeptAttributeAttribute (typeof (FooAttribute))]
			public void Method ()
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
