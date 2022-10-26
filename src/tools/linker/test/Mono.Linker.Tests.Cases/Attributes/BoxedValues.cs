using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	public class BoxedValues
	{
		// mcs bug
		//        [TestAttribute ((object)typeof (Enum_2))]
		//		[Kept]
		//		[KeptAttributeAttribute (typeof (TestAttribute))]
		//		public void Test_1 ()
		//        {
		//        }

		[TestAttribute (TestProperty = Enum_2.B)]
		[Kept]
		[KeptAttributeAttribute (typeof (TestAttribute))]
		public void Test_2 ()
		{
		}

		[TestAttribute (TestField = Enum_3.C)]
		[Kept]
		[KeptAttributeAttribute (typeof (TestAttribute))]
		public void Test_3 ()
		{
		}

		[TestAttribute (TestProperty = new object[] { Enum_4.B, null, typeof (Enum_5) })]
		[Kept]
		[KeptAttributeAttribute (typeof (TestAttribute))]
		public void Test_4 ()
		{
		}

		static void Main ()
		{
			typeof (BoxedValues).GetMethod ("Test_1").GetCustomAttributes (false);
			typeof (BoxedValues).GetMethod ("Test_2").GetCustomAttributes (false);
			typeof (BoxedValues).GetMethod ("Test_3").GetCustomAttributes (false);
			typeof (BoxedValues).GetMethod ("Test_4").GetCustomAttributes (false);
		}
	}

	[KeptBaseType (typeof (System.Attribute))]
	public class TestAttribute : Attribute
	{
		[Kept]
		public TestAttribute ()
		{
		}

		//[Kept]
		public TestAttribute (object arg)
		{
		}

		[KeptBackingField]
		[Kept]
		public object TestProperty { get; [Kept] set; }

		[Kept]
		public object TestField;
	}

	public enum Enum_1
	{
		A = 1,
		B,
		C
	}

	[Kept]
	[KeptMember ("value__")]
	[KeptBaseType (typeof (Enum))]
	public enum Enum_2
	{
		[Kept]
		A = 1,
		[Kept]
		B,
		[Kept]
		C
	}

	[Kept]
	[KeptMember ("value__")]
	[KeptBaseType (typeof (Enum))]
	public enum Enum_3
	{
		[Kept]
		C
	}

	[Kept]
	[KeptMember ("value__")]
	[KeptBaseType (typeof (Enum))]
	public enum Enum_4
	{
		[Kept]
		B
	}

	[Kept]
	[KeptMember ("value__")]
	[KeptBaseType (typeof (Enum))]
	public enum Enum_5
	{
		[Kept]
		B
	}
}