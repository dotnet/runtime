// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class mytest {
	[Fact]
	public static int TestEntryPoint() {
		
		int   rValue = 100;
		Timer time   = null;

		Console.WriteLine("Test that timer fields return correct null ref");

		try {
			time.Change(5,10);
			rValue = 1;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (time.Change(5,10)");
		}

		try {
			time.Change(5,10);
			rValue = 2;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (time.Change((long)5,(long)10)");
		}

		try {
			time.Change(new TimeSpan(500),new TimeSpan(100));
			rValue = 3;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (time.Change(new TimeSpan(500),new TimeSpan(100))");
		}

		try {
			time.Change(500,100);
			rValue = 4;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (time.Change((uint)500,(uint)100)");
		}

		try {
			time.Dispose();
			rValue = 5;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (time.Dispose( ))");
		}

		// try {
		// 	time.Dispose(new AutoResetEvent(true));
		// 	rValue = 6;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (time.Dispose(new WaitHandle( )))");
		// }

		try {
			time.Equals(new AutoResetEvent(true));
			rValue = 7;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (time.Equals(new WaitHandle( )))");
		}

		try {
			time.GetHashCode();
			rValue = 8;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (time.GetHashCode())");
		}

		// try {
		// 	time.GetLifetimeService();
		// 	rValue = 9;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (time.GetLifetimeService())");
		// }

		try {
			time.GetType();
			rValue = 10;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (time.GetType())");
		}

		// try {
		// 	time.InitializeLifetimeService();
		// 	rValue = 11;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (time.InitializeLifetimeService())");
		// }

		try {
			time.ToString();
			rValue = 12;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (time.ToString())");
		}

		Console.WriteLine("Return Code == {0}",rValue);
		return rValue;
	}
}
