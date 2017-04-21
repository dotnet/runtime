namespace TestCases.Linker.VirtualCall
{
	public class Library
	{
		public Library ()
		{
		}

		public int Shebang ()
		{
			return Bang ();
		}

		protected virtual int Bang ()
		{
			return 1;
		}
	}

	public class PowerFulLibrary : Library
	{
		protected override int Bang ()
		{
			return 0;
		}
	}
}