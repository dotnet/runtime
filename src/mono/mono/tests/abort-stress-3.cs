//
// This is:
//
// http://bugzilla.ximian.com/show_bug.cgi?id=76047
//

using System;
using System.Threading;

class Tests
{
	static int loops = 20;
	static int threads = 100;
	
	static void Empty () {}
   
	static void Create () {
		Thread t = new Thread (new ThreadStart (Empty));
		t.Start ();
		
		Thread.Sleep(1000);
		
		t.Abort ();
	}
   
	static void doit () {
		for (int i = 0; i < threads; i++)
			new Thread (new ThreadStart (Create)).Start ();
	}

	public static void Main (String[] args) {
	  if (args.Length > 0)
		  loops = int.Parse (args [0]);
	  if (args.Length > 1)
		  threads = int.Parse (args [1]);
	  for (int i = 0; i < loops; ++i) {
		  Console.Write ('.');
		  doit ();
	  }
  }  
}


