using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class UnusedAttributeTypeOnPropertyIsRemoved
	{
		static void Main ()
		{
			var bar = new Bar ();
			bar.Value = "Hello";
			var tmp = bar.Value;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar
		{
			[Foo]
			[Kept]
			[KeptBackingField]
			public string Value { [Foo][Kept] get; [Foo][Kept] set; }
		}

		class FooAttribute : Attribute
		{
		}
	}
}
