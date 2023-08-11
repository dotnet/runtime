// Created by manually decompiling the previous
// src\tests\JIT\Regression\CLR-x86-JIT\V1.2-M01\b08046\SyncGCHole.il
//
// Changes:
// - Rename Main to TestEntryPoint (standard change for merged test groups).
// - Remove the Environment.Exit call at the end of Main. Exit doesn't wait
//   for foreground threads to complete, so the test becomes a race that is
//   typically lost.
// - Write a local static instead of Environment.ExitCode for compatibility
//   with merged test groups.
// - Don't allow a successful thread to overwrite the exit value of a failing
//   one. Retain the writes for successes (to 'Ignored') to keep the shape of
//   the code as similar to the original as possible. It is unclear what
//   aspect of the code caused the original problem.
// - Don't bother catching the exception in the outer method as the test
//   infrastructure will handle it.

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
    public static int ExitCode { get; set; }
    public static int Ignored { get; set; }

    [Fact]
    public static int TestEntryPoint()
    {
        ExitCode = 100;

        ExternalException v1 = new ExternalException();

        for (int v2 = 0; v2 < 10; v2++)
        {
            Thread v0 = new Thread(new ThreadStart(v1.runtest));
            v0.Start();
        }

        return ExitCode;
    }

    private void runtest()
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
                Console.WriteLine("TryCatch Thread Passed");
                ExternalException.Ignored = 100;
            }
        }
        else
        {
            lock(this)
            {
                Console.WriteLine("TryCatch Thread Failed");
                Console.WriteLine(0);
                ExternalException.ExitCode = 1;
            }
        }
    }

    private void recurse(int counter)
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
