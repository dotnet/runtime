//
// This test merely creates a Win32Exception that triggers the
// code in mono/io-layer/message.c that validates that the
// error table is propertly sorted
//
// If there is output on stderr, we have an error
//
using System;
using System.ComponentModel;

class X {
	static string msg (int c)
	{
		return new Win32Exception (c).Message;
	}

	static void check (int c, string s)
	{
		if (msg (c) != s)
			Console.WriteLine ("For {0} expected {1} got {2}", c, s, msg (c));
	}
	
	static void Main ()
	{
		//
		// All this test does is instantiate two Win32Exceptions
		// one with no known text, so it triggers a linear search
		// And one with a known message, to trigger a validation
		//
		// If stderr gets any output, there is a sorting error
		// in mono/io-layer/messages.c
		//
		Exception a = new Win32Exception (99999);
		a = new Win32Exception (9805);

		check (2, "Cannot find the specified file");

	}
	
}