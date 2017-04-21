namespace TestCases.Xml.Interface
{
	public class Foo : IFoo
	{
		public void Gazonk ()
		{
		}
	}

	public interface IFoo : IBar
	{
	}

	public interface IBar
	{
		[AssertLinked]
		void Gazonk ();
	}

	[AssertLinked]
	public class Baz : IBaz
	{
	}

	[AssertLinked]
	public interface IBaz
	{
	}
}
