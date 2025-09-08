using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Program
{
	static bool failure;

	[DllImport ("__Internal")]
	static extern string mono_path_canonicalize (string input);

	static void CanonicalizeAssert (string input, string expected)
	{
		string actual = mono_path_canonicalize (input);
		if (expected != actual) {
			failure = true;
			Console.WriteLine ("ERROR: Expected canonicalization of '{0}' to be '{1}', but it was '{2}'.", input, expected, actual);
		} else {
			Console.WriteLine ("SUCCESS: Canonicalization of '{0}' => '{1}'", input, actual);
		}
	}
	
	static void CanonicalizeTest ()
	{
		bool isWindows = !(((int)Environment.OSVersion.Platform == 4) || ((int)Environment.OSVersion.Platform == 128));

		if (!isWindows) {
			CanonicalizeAssert ("", Environment.CurrentDirectory);
			CanonicalizeAssert ("/", "/");
			CanonicalizeAssert ("/..", "/");
			CanonicalizeAssert ("/foo", "/foo");
			CanonicalizeAssert ("/foo/././", "/foo");
			CanonicalizeAssert ("/../../foo", "/foo");
			CanonicalizeAssert ("/foo/", "/foo");
			CanonicalizeAssert ("/foo/../../../", "/");
			CanonicalizeAssert ("/foo/../../..", "/");
		}
	}
	
	public static int Main()
	{
		CanonicalizeTest ();
		return failure ? 1 : 0;
	}
}