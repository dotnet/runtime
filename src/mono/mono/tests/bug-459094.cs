using System;
using System.IO;

class Driver
{
	static int Main ()
	{
		string step = "abcde12345abcde12345abcde12345abcde12345";
		string expected = Directory.GetCurrentDirectory();
		string current = "";
		/*if (Directory.Exists (step)) FIXME this doesn't work on linux 
			Directory.Delete (step, true);*/

		try {
			for (int i = 0; i < 4000; ++i) {
				current = Directory.GetCurrentDirectory ();
				if (!current.Equals (expected)) {
					Console.WriteLine ("expected dir {0} but got {1}", expected, current);
					return 1;
				}
				Console.WriteLine("I={0} DIR={1}",i,Directory.GetCurrentDirectory().Length);
				Directory.CreateDirectory (step);
				Directory.SetCurrentDirectory (step);
				expected += Path.DirectorySeparatorChar + step;
			}
		} catch (PathTooLongException) {
			Console.WriteLine ("ok, got PathTooLongException");
			return 0;
		}

		Console.WriteLine ("Max path not reached");
		return 2;
	}
}

