using System;
using Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: UsedAttributeTypeOnAssemblyIsKept.Foo]
[assembly: KeptAttributeAttribute (typeof (UsedAttributeTypeOnAssemblyIsKept.FooAttribute))]

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	class UsedAttributeTypeOnAssemblyIsKept
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
