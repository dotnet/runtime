// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class mytest {
	[Fact]
	public static int TestEntryPoint() {
		int    rValue = 100;
		Mutex  mut    = null;
		
		Console.WriteLine("Test Mutex for expected NullRef Exceptions");
		Console.WriteLine( );


// 		try {
// #pragma warning disable 618
// 			mut.Handle = new IntPtr(1);
// #pragma warning restore 618
// 			rValue = 1;
// 		}
// 		catch (NullReferenceException) {
// 			Console.WriteLine("Caught NullReferenceException   (mut.Handle(new IntPtr(1)))");
// 		}
// 		try {
// #pragma warning disable 618
// 			IntPtr iptr = mut.Handle;
// #pragma warning restore 618
// 			rValue = 2;
// 		}
// 		catch (NullReferenceException) {
// 			Console.WriteLine("Caught NullReferenceException   (IntPtr iptr = mut.Handle)");
// 		}
	
		// try {
		// 	mut.Close();
		// 	rValue = 3;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mut.Close())");
		// }
		
		try {
			mut.Equals(new ManualResetEvent(true));
			rValue = 4;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mut.Equals(new ManualResetEvent()))");
		}

		try {
			mut.GetHashCode();
			rValue = 5;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mut.GetHasCode())");
		}

		// try {
		// 	mut.GetLifetimeService();
		// 	rValue = 6;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mut.GetLifetimeService())");
		// }		

		try {
			mut.GetType();
			rValue = 7;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mut.GetType())");
		}

		// try {
		// 	mut.InitializeLifetimeService();
		// 	rValue = 8;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mut.InitializeLifeTimeService())");
		// }
	
		try {
			mut.ReleaseMutex();
			rValue = 9;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mut.ReleaseMutex())");
		}

		try {
			mut.ToString();
			rValue = 11;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mut.ToString())");
		}

		try {
			mut.WaitOne();
			rValue = 12;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mut.WaitOne())");
		}

		try {
			mut.WaitOne(1000);//,true);
			rValue = 13;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mut.WaitOne(int)");
		}

		// try {
		// 	mut.WaitOne(1000,false);
		// 	rValue = 14;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mut.WaitOne(int,bool))");
		// }

		try {
			mut.WaitOne(new TimeSpan(1000));//,true);
			rValue = 15;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mut.WaitOne(TimeSpan))");
		}

		// try {
		// 	mut.WaitOne(new TimeSpan(1000),false);
		// 	rValue = 16;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mut.WaitOne(TimeSpan,bool))");
		// }		

		Console.WriteLine("Return Code == {0}",rValue);
		return rValue;
	}
}
