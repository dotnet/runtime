// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class mytest {
	[Fact]
	public static int TestEntryPoint() {
		int 		  rValue = 100;
		AutoResetEvent    are  = null;
		
		Console.WriteLine("Test AutoResetEvent for expected NullRef Exceptions");
		Console.WriteLine( );


// 		try {
// #pragma warning disable 618
// 			are.Handle = new IntPtr(1);
// #pragma warning restore 618
// 			rValue = 1;
// 		}
// 		catch (NullReferenceException) {
// 			Console.WriteLine("Caught NullReferenceException   (are.Handle(new IntPtr(1)))");
// 		}
// 		try {
// #pragma warning disable 618
// 			IntPtr iptr = are.Handle;
// #pragma warning restore 618
// 			rValue = 2;
// 		}
// 		catch (NullReferenceException) {
// 			Console.WriteLine("Caught NullReferenceException   (IntPtr iptr = are.Handle)");
// 		}
	
		// try {
		// 	are.Close();
		// 	rValue = 3;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (are.Close())");
		// }
		
		try {
			are.Equals(new ManualResetEvent(true));
			rValue = 4;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (are.Equals(new ManualResetEvent()))");
		}

		try {
			are.GetHashCode();
			rValue = 5;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (are.GetHasCode())");
		}

		// try {
		// 	are.GetLifetimeService();
		// 	rValue = 6;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (are.GetLifetimeService())");
		// }		

		try {
			are.GetType();
			rValue = 7;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (are.GetType())");
		}

		// try {
		// 	are.InitializeLifetimeService();
		// 	rValue = 8;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (are.InitializeLifeTimeService())");
		// }
	
		try {
			are.Reset();
			rValue = 9;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (are.Reset())");
		}

		try {
			are.Set();
			rValue = 10;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (are.Set())");
		}

		try {
			are.ToString();
			rValue = 11;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (are.ToString())");
		}

		try {
			are.WaitOne();
			rValue = 12;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (are.WaitOne())");
		}

		try {
			are.WaitOne(1000);//,true);
			rValue = 13;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (are.WaitOne(int,bool))");
		}

		// try {
		// 	are.WaitOne(1000);//,false);
		// 	rValue = 14;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (are.WaitOne(int,bool))");
		// }

		try {
			are.WaitOne(new TimeSpan(1000));//,true);
			rValue = 15;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (are.WaitOne(TimeSpan,bool))");
		}

		// try {
		// 	are.WaitOne(new TimeSpan(1000));//,false);
		// 	rValue = 16;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (are.WaitOne(TimeSpan,bool))");
		// }		

		Console.WriteLine("Return Code == {0}",rValue);
		return rValue;
	}
}
