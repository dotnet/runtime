// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class mytest {
	[Fact]
	public static int TestEntryPoint() {
		int 		  rValue = 100;
		ManualResetEvent  mre  = null;
		
		Console.WriteLine("Test ManualResetEvent for expected NullRef Exceptions");
		Console.WriteLine( );


// 		try {
// #pragma warning disable 618
// 			mre.Handle = new IntPtr(1);
// #pragma warning restore 618
// 			rValue = 1;
// 		}
// 		catch (NullReferenceException) {
// 			Console.WriteLine("Caught NullReferenceException   (mre.Handle(new IntPtr(1)))");
// 		}
// 		try {
// #pragma warning disable 618
// 			IntPtr iptr = mre.Handle;
// #pragma warning restore 618
// 			rValue = 2;
// 		}
// 		catch (NullReferenceException) {
// 			Console.WriteLine("Caught NullReferenceException   (IntPtr iptr = mre.Handle)");
// 		}
	
		// try {
		// 	mre.Close();
		// 	rValue = 3;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mre.Close())");
		// }
		
		try {
			mre.Equals(new ManualResetEvent(true));
			rValue = 4;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mre.Equals(new ManualResetEvent()))");
		}

		try {
			mre.GetHashCode();
			rValue = 5;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mre.GetHasCode())");
		}

		// try {
		// 	mre.GetLifetimeService();
		// 	rValue = 6;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mre.GetLifetimeService())");
		// }		

		try {
			mre.GetType();
			rValue = 7;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mre.GetType())");
		}

		// try {
		// 	mre.InitializeLifetimeService();
		// 	rValue = 8;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mre.InitializeLifeTimeService())");
		// }
	
		try {
			mre.Reset();
			rValue = 9;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mre.Reset())");
		}

		try {
			mre.Set();
			rValue = 10;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mre.Set())");
		}

		try {
			mre.ToString();
			rValue = 11;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mre.ToString())");
		}

		try {
			mre.WaitOne();
			rValue = 12;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mre.WaitOne())");
		}

		try {
			mre.WaitOne(1000);//,true);
			rValue = 13;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mre.WaitOne(int)");
		}

		// try {
		// 	mre.WaitOne(1000);//,false);
		// 	rValue = 14;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mre.WaitOne(int,bool))");
		// }

		try {
			mre.WaitOne(new TimeSpan(1000));//,true);
			rValue = 15;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (mre.WaitOne(TimeSpan)");
		}

		// try {
		// 	mre.WaitOne(new TimeSpan(1000));//,false);
		// 	rValue = 16;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (mre.WaitOne(TimeSpan,bool))");
		// }		

		Console.WriteLine("Return Code == {0}",rValue);
		return rValue;
	}
}
