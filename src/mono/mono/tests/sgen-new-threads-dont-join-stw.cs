using System;
using System.Timers;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

class T {

    static int count = 0;
    static object count_lock = new object();

    const long N = 500000;
    const int num_threads = 8;

    static void UseMemory () {
        
        for (int i = 0; i < N; ++i) {

            var l1 = new ArrayList ();
            l1.Add(""+i);
            var l2 = new ArrayList ();
            l2.Add(""+(i+1));
            var l3 = new ArrayList ();
            l3.Add(""+(i+2));
            var l4 = new ArrayList ();
            l4.Add(""+(i+3));
        }
       
        
        lock (count_lock)
        {
            count++;
            Monitor.PulseAll(count_lock);
        }
    }

    static void Timer_Elapsed(object sender, EventArgs e)
    {
        HashSet<string> h = new HashSet<string>();
        for (int j = 0; j < 10000; j++)
        {
            h.Add(""+j+""+j);
        }
    }

    static void Main (string[] args) {
        int iterations = 0;

        for (TestTimeout timeout = TestTimeout.Start(TimeSpan.FromSeconds(TestTimeout.IsStressTest ? 120 : 5)); timeout.HaveTimeLeft;)
        {
            count = 0;

            List<Thread> threads = new List<Thread>();
            List<System.Timers.Timer> timers = new List<System.Timers.Timer>();

            for (int i = 0; i < num_threads; i++)
            {
                Thread t3 = new Thread (delegate () { 
                    UseMemory();
                    });

                t3.Start ();

                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Elapsed += Timer_Elapsed;
                timer.AutoReset = false;
                timer.Interval = 1000;
                timer.Start();
                timers.Add(timer);
            }
            
            for (int i = 0; i < 4000; i++)
            {
                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Elapsed += Timer_Elapsed;
                timer.AutoReset = false;
                timer.Interval = 500;
                timer.Start();
                timers.Add(timer);
            }

            lock (count_lock)
            {
                while (count < num_threads)
                {
                    Console.Write (".");
                    Monitor.Wait(count_lock);
                }
            }

            foreach (var t in threads)
            {
                t.Join();
            }

            Console.WriteLine ();
            iterations += 1;
        }

        Console.WriteLine ($"done {iterations} iterations");
    }
}
