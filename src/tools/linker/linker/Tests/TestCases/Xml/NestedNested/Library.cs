namespace TestCases.Xml.NestedNested
{
	public class Foo
	{
	}

	[AssertLinked]
	public class Bar
	{
		[AssertLinked]
		public class Baz
		{
			[AssertLinked]
			public class Gazonk
			{
			}
		}
	}
}