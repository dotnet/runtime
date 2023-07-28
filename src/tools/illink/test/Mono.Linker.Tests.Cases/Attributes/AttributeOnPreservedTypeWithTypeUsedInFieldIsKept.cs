using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo (Val = typeof (A))]
	[KeptAttributeAttribute (typeof (FooAttribute))]
	class AttributeOnPreservedTypeWithTypeUsedInFieldIsKept
	{
		public static void Main ()
		{
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (System.Attribute))]
		class FooAttribute : Attribute
		{
			[Kept]
			public Type Val;
		}

		[Kept]
		class A
		{
			public A ()
			{
			}
		}
	}
}