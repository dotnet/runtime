using System;

public class fall_through {

	static void test (string str) {
			Console.WriteLine("testing: '{0}', interned: {1}", str, String.IsInterned(str) != null);

			switch(str)
			{
				case "test":
					Console.WriteLine("passed");
   					break;

				default:
					return;
	    		}
	}
	public static void Main(string[] args)
	{
		char[] c = {'t', 'e', 's', 't'};
		string s = new String (c);
		string s2 = new String (c);
		Console.WriteLine("testing built string (interned = {0}) (equal strings: {1})", String.IsInterned(s) != null, (object)s == (object)s2);
		test (s);
		Console.WriteLine("after test 1 (interned = {0}) (equal strings: {1})", String.IsInterned(s) != null, (object)s == (object)s2);
		Console.WriteLine("after test (interned = {0}) (interned2 = {1})", String.IsInterned(s) != null, String.IsInterned(s2) != null);
		foreach(string str in args)
		{
			test (str);
		}
	}
}

