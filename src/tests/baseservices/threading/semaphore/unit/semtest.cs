// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

 
using System;
using System.Threading;

class TestSemaphore
{
    const int intialCount = 10;
    const int maximumCount = 10;

    int successes;
    int failures;
    int expected;

    public TestSemaphore()
    {
	successes = 0;
        failures  = 0;
        expected  = 8;
    }

    static int Main()
    {
	TestSemaphore myTest = new TestSemaphore();
	myTest.NegativeTest();

	myTest.FuncTest1();
	myTest.FuncTest2();
	
	myTest.FuncTest3();

	return (myTest.CheckSuccess());
    }

    private int CheckSuccess()
    {
	if(successes == expected && failures == 0)
		return 100;
	return -1;
    }

    private void Failure(string message)
    {
	Console.WriteLine(message);
	failures++;
    }
    private void Success()
    {	
	successes++;
    }
    
    public void NegativeTest() {
	try {
	    new Semaphore(-1, 0);
	    Failure("new Semaphore(-1, 0) Expected Exception is not thrown");
	}
	catch(ArgumentOutOfRangeException) {
	    Success();	  
	}
	catch {
	    Failure("UnExpected Exception thrown");
	}

	try {
	    new Semaphore(0, -1);
	    Failure("new Semaphore(0, -1) Expected Exception is not thrown");
	}
	catch(ArgumentOutOfRangeException) {
	    Success();	  
	}
	catch {
	    Failure("UnExpected Exception thrown");
	}

	try {
	    new Semaphore(1, 0);
	    Failure("new Semaphore(1, 0) Expected Exception is not thrown");
	}
	catch(ArgumentException) {
	    Success();	  
	}
	catch {
	    Failure("UnExpected Exception thrown");
	}

    }		

    public void FuncTest1() {
	using (Semaphore sem = new Semaphore(intialCount, maximumCount))
        {
    		sem.WaitOne();
		int previousCount = sem.Release();
		Console.WriteLine("Previous Count is {0}", previousCount);
		if(previousCount != intialCount -1) {
	   	    Failure("Previous Count is not correct");	    
		}
		else
		    Success();

		try {
		    sem.Release();
		    Failure("Expected Exception is not thrown");
		}
		catch(SemaphoreFullException) {
			Success();	  
		}
	}

     } 	

    public void FuncTest2() {
	using (Semaphore sem2 = new Semaphore(intialCount, maximumCount, "Semaphore_TESTSEM"))
	{
		bool createdNew;
		using (Semaphore sem3 = new Semaphore(intialCount, maximumCount, "Semaphore_TESTSEM", out createdNew))
		{
			if( createdNew ) {
			     Failure("Error: we are not expecting a new semaphore here");
			}
			else
				Success();
		}

		using (Semaphore sem4 = Semaphore.OpenExisting("Semaphore_TESTSEM"))
		{
			sem4.WaitOne();
			sem4.WaitOne();
			int previousCount = sem4.Release(2);
			Console.WriteLine("Previous Count is {0}", previousCount);
			if(previousCount != intialCount -2) {
   			    Failure("Previous Count is not correct");	    
			}
			else
				Success();
		}
	}
    }		

    public void FuncTest3() {
	
	Thread t = new Thread(new ThreadStart(Create));
	Thread t2 = new Thread(new ThreadStart(Open));
	__flag = false;
	t.Start();
	t2.Start();
 	t.Join();
	t2.Join();
        

   }
   public bool __flag;

   public void Create()
   {
	using (Semaphore sem = new Semaphore(intialCount, maximumCount, "Semaphore_TESTSEMOPEN"))
	{
		__flag = true;
		while(__flag)
			Thread.Sleep(0);
	}
   }

   public void Open()
   {
	Semaphore sem4;
	try{
		while(!__flag)
			Thread.Sleep(0);
		using (sem4 = Semaphore.OpenExisting("Semaphore_TESTSEMOPEN"))
		{
			__flag = false;
			Console.WriteLine("Opened on new thread");
			Success();	
			sem4.WaitOne();
			sem4.WaitOne();
		}
	}
	catch(Exception e)
	{
		Failure(e.ToString());
	}
	finally
	{
		__flag = false;
	}
   }
}
