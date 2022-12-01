using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[KeptDelegateCacheField ("0", nameof (Tmp_Something))]
	class UsedAttributeTypeOnEventIsKept
	{
		static void Main ()
		{
			var tmp = new Bar ();
			tmp.Something += Tmp_Something;
			var str = typeof (FooAttribute).ToString ();
		}

		[Kept]
		private static void Tmp_Something (object sender, EventArgs e)
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Bar
		{
			[Foo]
			[Kept]
			[KeptAttributeAttribute (typeof (FooAttribute))]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> Something { [Foo][KeptAttributeAttribute (typeof (FooAttribute))] add { } [Foo][KeptAttributeAttribute (typeof (FooAttribute))] remove { } }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Attribute))]
		class FooAttribute : Attribute
		{
		}
	}
}
