using System;
using System.Threading;

public class Program {

	public static void Main (string[] args) {
		int caughts = 0;
		int finallys = 0;
		try {
			try {
				throw new Exception ();
			} finally {
				finallys++;
				throw new Exception ();
			}
		} catch (Exception) {
			caughts++;
			Console.WriteLine ("Caught");
		}
		if (caughts != 1)
			Environment.Exit (1);
		if (finallys != 1)
			Environment.Exit (2);
		Console.WriteLine ("Exit");
	}
}
