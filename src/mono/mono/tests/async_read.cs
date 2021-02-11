using System;
using System.IO;
using System.Threading;

class Test {

	static int sum = 0;
	static int count = 0;
	
	static void async_callback (IAsyncResult ar)
	{
		byte [] buf = (byte [])ar.AsyncState;
		Interlocked.Add (ref sum, buf [0]);
		Interlocked.Increment (ref count);
	}

	static int Main () {
		byte [] buf = new byte [1];
		AsyncCallback ac = new AsyncCallback (async_callback);
		IAsyncResult ar;
		int sum0 = 0;
		int count0 = 0;
		
		FileStream s = new FileStream ("async_read.exe",  FileMode.Open, FileAccess.Read);

		s.Position = 0;
		while (s.Read (buf, 0, 1) == 1) {
			sum0 += buf [0];
			count0 ++;
		}
		
		s.Position = 0;
		
		do {
			buf = new byte [1];
			ar = s.BeginRead (buf, 0, 1, ac, buf);
		} while (s.EndRead (ar) == 1);
		
		Thread.Sleep (100);
		
		s.Close ();

		count0 ++;  // async_callback is invoked for the "finished reading" case too
		Console.WriteLine ("CSUM: " + sum + " " + sum0);
		Console.WriteLine ("Count: " + count + " " + count0);
		if (sum != sum0 || count != count0)
			return 1;
		
		return 0;
	}
}
