using System;

namespace TestCases.Linker.Simple
{
	public class Library
	{
		private int _pouet;
		[AssertLinked] private int _hey;

		public Library ()
		{
			_pouet = 1;
		}

		[AssertLinked]
		public Library (int pouet)
		{
			_pouet = pouet;
		}

		public int Hello ()
		{
			Console.WriteLine ("Hello");
			return _pouet;
		}

		[AssertLinked]
		public void Hey (int hey)
		{
			_hey = hey;
			Console.WriteLine (_hey);
		}
	}

	[AssertLinked]
	public class Toy
	{
	}
}