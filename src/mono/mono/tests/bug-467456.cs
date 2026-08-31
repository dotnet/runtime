using System;
using System.Collections.Generic;

class Program
{
	static void Trigger () {
		List<string> inners = new List<string> ();
		inners.Add ("Failed to run update to completion");
		throw new Exception ();
	}

	static int Main (string[] args)
	{
		try {
			Trigger ();
		} catch (TypeLoadException e) {
			return 1;
		} catch (Exception e) {
			return 0;
		}
		return 1;
	}
}
