// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class mytest {
	[Fact]
	public static int TestEntryPoint() {
		int         rValue = 100;
		WaitHandle  wh     = null;
		
		Console.WriteLine("Test AutoResetEvent for expected NullRef Exceptions");
		Console.WriteLine( );


// 		try {
// #pragma warning disable 618
// 			wh.Handle = new IntPtr(1);
// #pragma warning restore 618
// 			rValue = 1;
// 		}
// 		catch (NullReferenceException) {
// 			Console.WriteLine("Caught NullReferenceException   (wh.Handle(new IntPtr(1)))");
// 		}
// 		try {
// #pragma warning disable 618
// 			IntPtr iptr = wh.Handle;
// #pragma warning restore 618
// 			rValue = 2;
// 		}
// 		catch (NullReferenceException) {
// 			Console.WriteLine("Caught NullReferenceException   (IntPtr iptr = wh.Handle)");
// 		}
	
		// try {
		// 	wh.Close();
		// 	rValue = 3;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (wh.Close())");
		// }
		
		try {
			wh.Equals(new ManualResetEvent(true));
			rValue = 4;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (wh.Equals(new ManualResetEvent()))");
		}

		try {
			wh.GetHashCode();
			rValue = 5;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (wh.GetHasCode())");
		}

		// try {
		// 	wh.GetLifetimeService();
		// 	rValue = 6;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (wh.GetLifetimeService())");
		// }		

		try {
			wh.GetType();
			rValue = 7;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (wh.GetType())");
		}

		// try {
		// 	wh.InitializeLifetimeService();
		// 	rValue = 8;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (wh.InitializeLifeTimeService())");
		// }

		try {
			wh.ToString();
			rValue = 11;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (wh.ToString())");
		}

		try {
			wh.WaitOne();
			rValue = 12;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (wh.WaitOne())");
		}

		try {
			wh.WaitOne(1000);//,true);
			rValue = 13;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (wh.WaitOne(int))");
		}

		// try {
		// 	wh.WaitOne(1000,false);
		// 	rValue = 14;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (wh.WaitOne(int,bool))");
		// }

		try {
			wh.WaitOne(new TimeSpan(1000));//,true);
			rValue = 15;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (wh.WaitOne(TimeSpan,bool))");
		}

		// try {
		// 	wh.WaitOne(new TimeSpan(1000),false);
		// 	rValue = 16;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (wh.WaitOne(TimeSpan,bool))");
		// }		

		Console.WriteLine("Return Code == {0}",rValue);
		return rValue;
	}
}
