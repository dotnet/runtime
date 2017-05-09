using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes {
	[Foo (typeof (A))]
	class AttributeOnPreservedTypeWithTypeUsedInConstructorIsKept {
		public static void Main ()
		{
		}

		[KeptBaseType (typeof (System.Attribute))]
		class FooAttribute : Attribute {
			[Kept]
			public FooAttribute (Type val)
			{
			}
		}

		[Kept]
		class A {
			public A ()
			{
			}
		}
	}
}