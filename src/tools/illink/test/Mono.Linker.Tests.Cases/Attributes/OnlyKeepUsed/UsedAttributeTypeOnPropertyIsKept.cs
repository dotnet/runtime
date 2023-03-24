using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class UsedAttributeTypeOnPropertyIsKept
	{
		static void Main ()
		{
			var bar = new Bar ();
			bar.Value = "Hello";
			var tmp = bar.Value;
			var str = typeof (FooAttribute).ToString ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar
		{
			[Foo]
			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (FooAttribute))]
			public string Value { [Foo][Kept][KeptAttributeAttribute (typeof (FooAttribute))] get; [Foo][Kept][KeptAttributeAttribute (typeof (FooAttribute))] set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Attribute))]
		class FooAttribute : Attribute
		{
		}
	}
}
