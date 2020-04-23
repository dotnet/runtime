using System;
using Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[module: UsedAttributeTypeOnModuleIsKept.Foo]
[module: KeptAttributeAttribute (typeof (UsedAttributeTypeOnModuleIsKept.FooAttribute))]

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class UsedAttributeTypeOnModuleIsKept
	{
		static void Main ()
		{
			var str = typeof (FooAttribute).ToString ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Attribute))]
		public class FooAttribute : Attribute
		{
		}
	}
}
