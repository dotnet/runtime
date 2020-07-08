// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
public class Foo 
{
        private static int n=0;

	public static void Bar(){
	  int i = 0;
          try  {
	       i = i/n ; 
	  }
          catch(DivideByZeroException)
          {}         
          finally  { n++;
		     Console.WriteLine("In finally  " + i); 
		     }
	  }
	
        public static int Main(String[] args) 
        {
	  String s = "Done";
	  Thread t = new Thread(new ThreadStart(Foo.Bar));
	  t.Start();
	  //Thread MainThread = Thread.CurrentThread;
	  Thread.Sleep(1000);
	  if (n == 2){
	     Console.WriteLine("Finally Test failed");
	     return 1;
	  }
	  else {
	      Console.WriteLine("Test Passed");
	      Console.WriteLine(s);
              return 100;
	  }  
       }
}
