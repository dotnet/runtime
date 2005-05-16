using System;
using System.Security;

class Program {

	static int Main (string[] args)
	{
		try {
			Console.WriteLine (Environment.UserName);
			return 0;
		}
		catch (SecurityException se) {
			Console.WriteLine ("---{0}{1}{0}---", Environment.NewLine, se);
			return 1;
		}
	}
}
