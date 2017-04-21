namespace TestCases.Linker.MultipleReferences
{
	public class Baz
	{
		public void Chain (Foo f)
		{
			f.b.Bang ();
		}

		[AssertLinked]
		public void Lurman ()
		{
		}
	}
}
