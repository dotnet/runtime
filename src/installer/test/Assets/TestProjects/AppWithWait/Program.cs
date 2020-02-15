using System;
using System.IO;
using System.Threading;

namespace AppWithSubDirs
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.Write("Hello ");

            // If the caller wants the app to start and wait,
            // it provides the name of a lock-file to write.
            // In this case, this test app creates the lock file
            // and waits until the file is deleted.
            if (args.Length > 0)
            {
                string writeFile = args[0];

                var fs = File.Create(writeFile);
                fs.Close();

                Thread.Sleep(200);

                while (File.Exists(writeFile))
                {
                    Thread.Sleep(100);
                }
            }

            Console.WriteLine("World!");
        }
    }
}
