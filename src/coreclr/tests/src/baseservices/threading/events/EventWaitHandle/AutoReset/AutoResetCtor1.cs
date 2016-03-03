using System;
using System.Threading;

class AutoResetCtor
{
    EventWaitHandle ewh;

    public static int Main()
    {
        AutoResetCtor arc = new AutoResetCtor();
        return arc.Run();
    }

    private int Run()
    {
        // Testing the initialState = true for an AutoResetEvent
        int iRet = -1;
        ewh = new EventWaitHandle(true, EventResetMode.AutoReset);

        Thread t = new Thread(new ThreadStart(ThreadWorker));
        t.Start();
        t.Join();

        // when doing another wait, it should not return until set.
        Console.WriteLine("Main: Waiting...");
        bool b = ewh.WaitOne(5000);//, false);
        if(b)
            Console.WriteLine("WaitOne didn't reset!");
        else
            iRet = 100;

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }
    
    private void ThreadWorker()
    {
        Console.WriteLine("TW: Waiting...");
        // This should return immediately
        ewh.WaitOne();
    }
}