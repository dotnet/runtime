// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;


public class TestRudeAbort {

	public int rValue;

	public TestRudeAbort(){
	
		rValue = 0;
	}

	public static int Main() {
		
		//If Static Constructor Fails to run the struct should not get loaded

		TestRudeAbort tra = new TestRudeAbort();
		Thread t = new Thread(new ThreadStart(tra.Run));
		t.Start();
		t.Join();
		try{
			Console.WriteLine("Thread is dead - Accessing Static Method");
			SBad.MyMethod();
			SBad.Obj.ToString();
		}
		catch(TypeInitializationException){
			//Should get a TypeInitializationException since the 
			//     static constructor never ran.
			//If no failure i.e. rValue == 0 then set return to 100
			Interlocked.CompareExchange(ref tra.rValue,100,0);
		}
		Console.WriteLine("Test {0}",tra.rValue == 100?"Passed":"Failed");
		return tra.rValue;
	}

	public void Run()
	{
		try{
			SBad.MyMethod();
		}
		catch(TypeInitializationException)
		{
			//This should never print anything since the thread is Aborted
			this.rValue = -1;
		}
		Console.WriteLine("This line should never print since the thread aborted()");
		this.rValue = -2;
	}	
}

public struct SBad
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
		Console.WriteLine("Should have been type Load");
	}
}