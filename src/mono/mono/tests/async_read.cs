using System;
using System.IO;
using System.Threading;

class Test {

	static int sum = 0;
	
	static void async_callback (IAsyncResult ar)
	{
		byte [] buf = (byte [])ar.AsyncState;
		sum += buf [0];
	}

	static int Main () {
		byte [] buf = new byte [1];
		AsyncCallback ac = new AsyncCallback (async_callback);
		IAsyncResult ar;
		int sum0 = 0;
		
		FileStream s = new FileStream ("async_read.cs",  FileMode.Open);

		s.Position = 0;
		sum0 = 0;
		while (s.Read (buf, 0, 1) == 1)
			sum0 += buf [0];
		
		s.Position = 0;
		
		do {
			ar = s.BeginRead (buf, 0, 1, ac, buf);
		} while (s.EndRead (ar) == 1);
		sum -= buf [0];
		
		Thread.Sleep (100);
		
		s.Close ();

		Console.WriteLine ("CSUM: " + sum + " " + sum0);
		if (sum != sum0)
			return 1;
		
		return 0;
	}
}
