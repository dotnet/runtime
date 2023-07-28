using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[KeptDelegateCacheField ("0", nameof (Tmp_Something))]
	class UnusedAttributeTypeOnEventIsRemoved
	{
		static void Main ()
		{
			var tmp = new Bar ();
			tmp.Something += Tmp_Something;
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
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> Something { [Foo] add { } [Foo] remove { } }
		}

		class FooAttribute : Attribute
		{
		}
	}
}
