using System;
using System.Security.Cryptography;
using System.Text;

class Test {

	static void Main ()
	{
		byte [] foo = Encoding.UTF8.GetBytes ("foobared");

		HashAlgorithm ha = MD5.Create ();
		byte [] hash = ha.ComputeHash (foo);

		Console.WriteLine (Encoding.UTF8.GetString (hash));
	}
}
