using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessTest_ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].Equals("infinite"))
                {
                    // To avoid potential issues with orphaned processes (say, the test exits before
                    // exiting the process), we'll say "infinite" is actually 100 seconds.
                    Sleep(100 * 1000);
                }
                else if (args[0].Equals("error"))
                {
                    Console.Error.WriteLine("ProcessTest_ConsoleApp.exe error stream");
                    Sleep();
                }
                else if (args[0].Equals("input"))
                {
                    string str = Console.ReadLine();
                }
                else if (args[0].Equals("stream"))
                {
                    Console.WriteLine("ProcessTest_ConsoleApp.exe started");
                    Console.WriteLine("ProcessTest_ConsoleApp.exe closed");
                    Sleep();
                }
                else
                {
                    Console.WriteLine(string.Join(" ", args));
                }
            }
            return 100;
        }

        private static void Sleep()
        {
            Sleep(100d);
        }

        private static void Sleep(double timeinMs)
        {
            Task.Delay(TimeSpan.FromMilliseconds(timeinMs)).Wait();
        }
    }
}
