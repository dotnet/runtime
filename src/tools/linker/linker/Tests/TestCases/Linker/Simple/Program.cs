namespace TestCases.Linker.Simple
{
	public class Program
	{
		[Mark]
		public static int Test ()
		{
			Program p = new Program ();
			return p.Run ();
		}

		int Run ()
		{
			Library lib = new Library ();
			return lib.Hello ();
		}
	}
}