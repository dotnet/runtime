using System;
using System.Threading;

class T
{
	static void DoStuff () {
		Console.WriteLine ("DoStuff ()");
	}

	static void Main (string [] args) {
		try
		{
			Thread.CurrentThread.Abort ();
		} finally {
//LABEL1
			try
			{
				DoStuff ();
			} catch (Exception) {}
// This call will send us back up to label one; and we'll loop forver
			Thread.CurrentThread.Abort ();
		}
	}
}

