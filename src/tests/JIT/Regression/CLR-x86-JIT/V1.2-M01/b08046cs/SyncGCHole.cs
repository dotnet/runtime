// Created by manually decompiling the previous
// src\tests\JIT\Regression\CLR-x86-JIT\V1.2-M01\b08046\SyncGCHole.il
//
// Changes:
// - Remove the [Fact] from Main because it eventually leads to
//   "CS7022 The entry point of the program is global code; ignoring 'Main()' entry point."
//   [Fact] will be added again as part of the test merging work.
// - Remove the Environment.Exit call at the end of Main.  Exit doesn't wait
//   for foreground threads to complete, so the test becomes a race that is
//   typically lost.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

class ExternalClass
{
    ExternalException ee;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void ThrowException() => throw ee;

    public ExternalClass()
    {
        ee = new ExternalException();
    }
}

public class ExternalException : Exception
{
    public static void Main()
    {
        ExternalException v1 = new ExternalException();

        for (int v2 = 0; v2 < 10; v2++)
        {
            Thread v0 = new Thread(new ThreadStart(v1.runtest));

            try
            {
                v0.Start();
            }
            catch (Exception)
            {
                Console.WriteLine("Exception was caught in main");
            }
        }
    }

    public void runtest()
    {
        int v0 = 0;

        for (int v1 = 0; v1 < 100; ++v1)
        {
            try
            {
                try
                {
                    if ((v1 % 2) == 0)
                    {
                        v0 = v1 / (v1 % 2);
                    }
                    else
                    {
                        recurse(0);
                    }
                }
                catch (ArithmeticException)
                {
                    v0++;
                }
                catch (ExternalException)
                {
                    v0--;
                }
            }
            finally
            {
                v0++;
            }
        }

        if (v0 == 100)
        {
            lock(this)
            {
                Console.WriteLine("TryCatch Test Passed");
                Environment.ExitCode = 100;
            }
        }
        else
        {
            lock(this)
            {
                Console.WriteLine("TryCatch Test Failed");
                Console.WriteLine(0);
                Environment.ExitCode = 1;
            }
        }
    }

    public void recurse(int counter)
    {
        char[] v0 = new char[100];

        if (counter == 100)
        {
            new ExternalClass().ThrowException();
        }
        else
        {
            recurse(++counter);
        }
    }

    public ExternalException()
    {
    }
}
