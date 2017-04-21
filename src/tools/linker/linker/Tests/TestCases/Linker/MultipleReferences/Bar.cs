using System;

namespace TestCases.Linker.MultipleReferences
{
	public class Bar
	{
		string bang = "bang !";

		public Bar ()
		{
		}

		public void Bang ()
		{
			Console.WriteLine (bang);
		}
	}
}
