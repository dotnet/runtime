// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
This test is for VSW 523654.

Before the fix we were doing the following:

take loader lock
look for a MethodDesc in the hash table
didn't find it so create a new MethodDesc (MD)
add it to the hash table
release the lock

But the problem with this is that during creation of MethodDesc we were looking at the type handle and 
calling managed code while holding the lock and that could potentially lead to a deadlock.

After the fix we do the following:

take loader lock
look for a MethodDesc in the hash table
didn't find it, release the lock

create a new MethodDesc

take the lock again
check that no one added the MethodDesc while we were creating it
If not there, add it to the hash table
Otherwise the MethodDesc is already in there
release the lock

There was a concern about race conditions for the scenario where we don't find the MD the first time 
but find it the 2nd time.
This test calls the same method from 10 threads so that we would hit this scenario. But this code path
is only hit non-deterministically some of the time.
*/

using System;
using System.Threading;
using Xunit;

public class A
{
	public static int i = 0;

	public void meth<T>()
	{
		Console.WriteLine(Thread.CurrentThread.Name + ": Inside meth<int>");
        Interlocked.Increment(ref i);
	}
}

public class Test_test532654_b
{
	public static void RunTestThread1()
	{
		A obj = new A();
		obj.meth<int>();
	}

	public static void RunTestThread2()
	{
		A obj = new A();
		obj.meth<int>();
	}
	
	public static void RunTestThread3()
	{
		A obj = new A();
		obj.meth<int>();
	}

	public static void RunTestThread4()
	{
		A obj = new A();
		obj.meth<int>();
	}

	public static void RunTestThread5()
	{
		A obj = new A();
		obj.meth<int>();
	}

	public static void RunTestThread6()
	{
		A obj = new A();
		obj.meth<int>();
	}

	public static void RunTestThread7()
	{
		A obj = new A();
		obj.meth<int>();
	}
	
	public static void RunTestThread8()
	{
		A obj = new A();
		obj.meth<int>();
	}

	public static void RunTestThread9()
	{
		A obj = new A();
		obj.meth<int>();
	}

	public static void RunTestThread10()
	{
		A obj = new A();
		obj.meth<int>();
	}


	[Fact]
	public static int TestEntryPoint()
	{
		
		Thread t1 = new Thread(RunTestThread1);
		t1.Name = "T1";

		Thread t2 = new Thread(RunTestThread2);
   		t2.Name = "T2";

		Thread t3 = new Thread(RunTestThread3);
		t3.Name = "T3";

		Thread t4 = new Thread(RunTestThread4);
	        t4.Name = "T4";
		
		Thread t5 = new Thread(RunTestThread5);
		t5.Name = "T5";

		Thread t6 = new Thread(RunTestThread6);
		t6.Name = "T6";

		Thread t7 = new Thread(RunTestThread7);
   		t7.Name = "T7";

		Thread t8 = new Thread(RunTestThread8);
		t8.Name = "T8";

		Thread t9 = new Thread(RunTestThread9);
	        t9.Name = "T9";
		
		Thread t10 = new Thread(RunTestThread10);
		t10.Name = "T10";

		t1.Start();
		t2.Start();
		t3.Start();
		t4.Start();
		t5.Start();

		t6.Start();
		t7.Start();
		t8.Start();
		t9.Start();
		t10.Start();


		t1.Join();
		t2.Join();		
		t3.Join();
		t4.Join();
		t5.Join();

		t6.Join();
		t7.Join();		
		t8.Join();
		t9.Join();
		t10.Join();


		Console.WriteLine("i should be 10");
		Console.WriteLine("i = " + A.i);

		if (A.i != 10)
		{
			Console.WriteLine("FAIL");
			return 	101;
		}	
		else
		{
			Console.WriteLine("PASS");
			return 	100;	
		}
	}

}
