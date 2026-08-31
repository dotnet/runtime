/////////////////////////////////// Test Overview ////////////////////////////////
// 
// Two threads are started from Main, which allocates 10 static mutexes.
// The first thread locks each mutex in turn, with a delay of 2000ms between
// locks.
// The second thread recursively locks mutex no. 5 10 times, blocking the
// progress of the first thread as this second thread has a delay of 4500ms
// between each lock.When the second thread has called ReleaseMutex on the mutex 10
// times it terminates and the first thread can carry on its cycle of locking and
// releasing mutexes until it exits.
//
/////////////////////////////////////////////////////////////////////////////////


using System;
using System.Threading;

class MutexTest
{  
	public static Mutex[] m;

    // Code for first thread
	public static void ThreadMethod_A()
    {	
 	Console.WriteLine("[Thread A] - Started.....");
	
	for (int i=0;i<10;i++)
		{
		Console.WriteLine("[Thread A] - Trying to lock mutex "+i+"...");
		m[i].WaitOne();
		Console.WriteLine("[Thread A] - m["+i+"] Locked!");   	
		Console.WriteLine("[Thread A] - Now using  mutex ["+i+"]");   	
		Thread.Sleep(2000);
		m[i].ReleaseMutex();
		Console.WriteLine("[Thread A] - Unlocked the mutex ["+i+"]");
		}
	
	Console.WriteLine("[Thread A] - exiting.....");
    }
    
    // Code for second thread
    public static void ThreadMethod_B()
    {
 	Console.WriteLine("[Thread B] - Started.....");

	for (int h=0;h<10;h++)
		{
		int i=5;		
		Console.WriteLine("[Thread B] - Trying to lock mutex "+i+" for "+h+" time...");
		m[i].WaitOne();
		Console.WriteLine("[Thread B] - m["+i+"] Locked recursively ["+h+"] times!");   	
		Thread.Sleep(4500);
		}
	for (int h=0;h<10;h++)
		{
		int i=5;		
		m[i].ReleaseMutex();
		Console.WriteLine("[Thread B] - Unlocked the mutex ["+i+"] for ["+h+"] times");
		}

 	Console.WriteLine("[Thread B] - Finished.....");
    }
    
    
    public static void Main()
    {     	
    	m = new Mutex[10];
    	for (int i = 0 ; i<10 ; i++ ) 
    		m[i] = new Mutex();
    	
        // Create the first thread
        Console.WriteLine("[  Main  ] - Creating first thread..");
        ThreadStart Thread_1 = new ThreadStart(ThreadMethod_A);
        
        // Create the second thread
        Console.WriteLine("[  Main  ] - Creating second thread..");
        ThreadStart Thread_2 = new ThreadStart(ThreadMethod_B);

        Thread A = new Thread(Thread_1);
        Thread B = new Thread(Thread_2);
        A.Start();
    	B.Start();
    	
    	Thread.Sleep(500);
    	Console.WriteLine("[  Main  ] - Test Ended");
    }
}
