using System;
using System.Threading;

class Program
{
    static void Main ()
    {
        var d = AppDomain.CreateDomain("Foo");
        d.ProcessExit += AppDomain_ProcessExit;

        AppDomain.CurrentDomain.ProcessExit += new EventHandler (AppDomain_ProcessExit);
        ThreadPool.QueueUserWorkItem (new WaitCallback (Proc));
        Thread.Sleep (1000);
    }

    static void Proc (object unused)
    {
        Thread.CurrentThread.IsBackground = false;
        Thread.Sleep (5000);
        Console.WriteLine ("done");
    }

    static void AppDomain_ProcessExit (object sender, EventArgs e)
    {
        Console.WriteLine ("exit");

        // No messages should be printed, as when this event is fired the
        // ThreadPool has been shutdown, thus `a.BeginInvoke()` has no effect.
        Action a = () => {
            int i = 0;
            while (true)
                Console.WriteLine ("Ha! {0}", i++);
        };
        a.BeginInvoke (null, null);
    }
}
