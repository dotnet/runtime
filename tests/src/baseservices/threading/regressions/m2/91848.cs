// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;


public class TestRudeAbort {

	public int rValue;

	public TestRudeAbort()
	{
		rValue = 0;
	}

	public static int Main() {
	
		TestRudeAbort myTest = new TestRudeAbort();
		myTest.ExecuteTest();
		Console.WriteLine("Test {0}",myTest.rValue == 100 ? "paSSed":"failed");
		return myTest.rValue;

	}

	public void ExecuteTest()
	{
		Thread t = new Thread(new ThreadStart(this.ThrowInStatic));
		t.Start();
		t.Join();
		try{
			//Thread is dead - Try to access Static Method
			//   This call should fail since the constructor never ran
			SBad.MyMethod();
			//If we got here, we accessed an unitialized class
			Console.WriteLine("ERRROR -- Accessed the class");
			lock(this) this.rValue = -1;
			SBad.Obj.ToString();
			//If we got here, we accessed an unitialized class
			Console.WriteLine("ERRROR -- Accessed the class");
			lock(this) this.rValue = -2;
		}
		catch(TypeInitializationException){
			//Caught TypeInit Exception as expected
			Interlocked.CompareExchange(ref this.rValue,100,0);
		}
	}

	public void ThrowInStatic()
	{
		try{
			SBad.MyMethod();
		}
		catch(TypeInitializationException)
		{
			//This is expected
		}
		Console.WriteLine("ERROR --- This line will never print since the thread aborted()");
		lock(this) this.rValue = -5;
	}	
}

public class SBad
{
	public static Object Obj = null;
	static SBad(){
		//Console.WriteLine("In the Constructor");
		ThreadEx.Abort(Thread.CurrentThread);
		Console.WriteLine("After Abort");
		Obj = new Object();
	}

	public static void MyMethod()
	{
		Console.WriteLine("ERROR --- Should have been type Load");
	}
}