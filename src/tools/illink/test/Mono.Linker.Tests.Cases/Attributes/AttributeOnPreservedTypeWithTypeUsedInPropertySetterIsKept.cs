using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo (Val = typeof (A))]
	[KeptAttributeAttribute (typeof (FooAttribute))]
	class AttributeOnPreservedTypeWithTypeUsedInPropertySetterIsKept
	{
		public static void Main ()
		{
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (System.Attribute))]
		class FooAttribute : Attribute
		{
			[Kept]
			[KeptBackingField]
			public Type Val { get; [Kept] set; }
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