namespace TestCases.Linker.MultipleReferences
{
	public class Foo
	{
		public Bar b;

		public Foo (Bar b)
		{
			this.b = b;
		}

		public void UseBar ()
		{
			b.Bang ();
		}

		[AssertLinked]
		public void Blam ()
		{
		}
	}
}
