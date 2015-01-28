using System;
using System.Threading;

namespace ProcessTest_ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    if (args[0].Equals("infinite"))
                    {
                        // To avoid potential issues with orphaned processes (say, the test exits before
                        // exiting the process), we'll say "infinite" is actually 30 seconds.
                        
                        Console.WriteLine("ProcessTest_ConsoleApp.exe started with an endless loop");
                        Thread.Sleep(30 * 1000);
                    }
                    if (args[0].Equals("error"))
                    {
                        Exception ex = new Exception("Intentional Exception thrown");
                        throw ex;
                    }
                    if (args[0].Equals("input"))
                    {
                        string str = Console.ReadLine();
                    }
                }
                else
                {
                    Console.WriteLine("ProcessTest_ConsoleApp.exe started");
                    Console.WriteLine("ProcessTest_ConsoleApp.exe closed");
                }

                return 100;
            }
            catch (Exception toLog)
            {
                // We're testing STDERR streams, not the JIT debugger. 
                // This makes the process behave just like a crashing .NET app, but without the WER invocation
                // nor the blocking dialog that comes with it, or the need to suppress that.
                Console.Error.WriteLine(string.Format("Unhandled Exception: {0}", toLog.ToString()));
                return 1;
            }

        }
    }
}
