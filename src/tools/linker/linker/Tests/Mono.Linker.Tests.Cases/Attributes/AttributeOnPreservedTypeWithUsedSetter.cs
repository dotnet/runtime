using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes {
	[Foo (Val = 1)]
	class AttributeOnPreservedTypeWithUsedSetter {
		public static void Main ()
		{
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (System.Attribute))]
		class FooAttribute : Attribute {
			[Kept]
			[KeptBackingField]
			public int Val { get; [Kept] set; }
		}
	}
}