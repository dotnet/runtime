using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo]
	[KeptAttributeAttribute (typeof (FooAttribute))]
	class AttributeOnPreservedTypeIsKept
	{
		public static void Main ()
		{
		}

		[KeptBaseType (typeof (System.Attribute))]
		class FooAttribute : Attribute
		{
			[Kept]
			public FooAttribute ()
			{
			}
		}
	}
}