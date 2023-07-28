using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	class AttributeOnParameterInUsedMethodIsKept
	{
		static void Main ()
		{
			Method ("Bar");
		}

		[Kept]
		static void Method ([Foo][KeptAttributeAttribute (typeof (FooAttribute))] string arg)
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Attribute))]
		class FooAttribute : Attribute
		{
		}
	}
}
