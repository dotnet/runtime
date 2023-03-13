using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	class AttributeOnUsedMethodIsKept
	{
		public static void Main ()
		{
			new A ().Method ();
		}

		[KeptMember (".ctor()")]
		class A
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
		[KeptBaseType (typeof (System.Attribute))]
		class FooAttribute : Attribute
		{
		}
	}
}