using System;

namespace TestCases.Xml.ReferenceInAttributes
{
	public class BarAttribute : Attribute
	{
		public BarAttribute ()
		{
		}

		public BarAttribute (Type type)
		{
		}

		public Type FieldType;

		public Type PropertyType {
			[AssertLinked]
			get { return null; }
			set { }
		}
	}

	[Bar (typeof (Guy_A))]
	public class Foo
	{

		[Bar (FieldType = typeof (Guy_B))]
		public Foo a;

		[Bar (PropertyType = typeof (Guy_C))]
		public Foo b;

		[LibLib (LibLibType = typeof (BilBil))]
		public Foo c;

		[LibLib (LibLibType = typeof (Guy_D))]
		public Foo d;
	}

	public class Guy_A
	{

		[AssertLinked]
		public Guy_A ()
		{
		}
	}

	public class Guy_B
	{

		[AssertLinked]
		public Guy_B ()
		{
		}
	}

	public class Guy_C
	{

		[AssertLinked]
		public Guy_C ()
		{
		}
	}

	public class Guy_D
	{

		[AssertLinked]
		public Guy_D ()
		{
		}
	}

	[AssertLinked]
	public class Baz
	{
	}
}
