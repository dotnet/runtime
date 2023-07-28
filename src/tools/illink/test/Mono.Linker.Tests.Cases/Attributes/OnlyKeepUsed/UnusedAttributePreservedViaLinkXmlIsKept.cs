using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[SetupLinkerDescriptorFile ("UnusedAttributePreservedViaLinkXmlIsKept.xml")]
	class UnusedAttributePreservedViaLinkXmlIsKept
	{
		static void Main ()
		{
			var tmp = new Bar ();
			tmp.Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (FooAttribute))]
		[Foo]
		class Bar
		{
			[Kept]
			[KeptAttributeAttribute (typeof (FooAttribute))]
			[Foo]
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
