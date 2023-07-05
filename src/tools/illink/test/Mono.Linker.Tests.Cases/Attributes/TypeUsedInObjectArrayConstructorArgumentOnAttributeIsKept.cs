using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo (EnumThatShouldBeKept.Two)]
	[KeptAttributeAttribute (typeof (FooAttribute))]
	public class TypeUsedInObjectArrayConstructorArgumentOnAttributeIsKept
	{
		public static void Main ()
		{
		}
	}

	[KeptBaseType (typeof (System.Attribute))]
	class FooAttribute : Attribute
	{
		[Kept]
		public FooAttribute ([KeptAttributeAttribute (typeof (ParamArrayAttribute))] params object[] parameters)
		{
		}
	}

	[Kept]
	[KeptMember ("value__")]
	[KeptBaseType (typeof (Enum))]
	enum EnumThatShouldBeKept
	{
		[Kept]
		One = 1,
		[Kept]
		Two = 2,
	}
}