namespace TestCases.Xml.PreserveFieldsRequired
{
	public class Foo
	{
		public Foo ()
		{
			new NotRequiredButUsedNotPreserved ();
			new NotRequiredButUsedAndFieldsPreserved ();
		}
	}

	public class NotRequiredButUsedNotPreserved
	{

		[AssertLinked] public int foo;
		[AssertLinked] public int bar;
	}

	public class NotRequiredButUsedAndFieldsPreserved
	{
		public int foo;
		public int bar;

		[AssertLinked]
		public int FooBar ()
		{
			return foo + bar;
		}
	}

	[AssertLinked]
	public class NotRequiredAndNotUsed
	{
	}
}
