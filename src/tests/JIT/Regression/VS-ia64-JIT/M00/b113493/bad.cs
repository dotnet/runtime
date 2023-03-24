// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace DefaultNamespace
{

    using System.Threading;
    using System.Runtime.InteropServices;
    using System;
    using System.IO;

    internal class ExternalClass
    {
        internal ExternalException ee = new ExternalException();

        public virtual void ThrowException()
        {
            throw ee;
        }
    }


    public class ExternalException : Exception
    {
        static int ExitCode = 0;

        [Fact]
        public static int TestEntryPoint()
        {
            Thread mv_Thread;
            ExternalException ee = new ExternalException();
            ExitCode = (100);
            for (int i = 0; i < 2; i++)
            {
                mv_Thread = new Thread(new ThreadStart(ee.runtest));
                try
                {
                    if (i == 0)
                    {
                        mv_Thread.Name = "" + i;
                    }
                    else
                    {
                        mv_Thread.Name = i + "";
                    }
                    mv_Thread.Start();
                }
                catch (Exception)
                {
                    Console.Out.WriteLine("Exception was caught in main");
                }
            }
            return ExitCode;
        }

        internal virtual void runtest()
        {
            int counter = 0;
            for (int j = 0; j < 10; j++)
            {
                try
                {
                    if (Thread.CurrentThread.Name.Equals("0"))
                    {
                        Console.WriteLine("THREAD " + Thread.CurrentThread.Name + " COUNTER = " + j);
                        counter = j / (j - counter);
                    }
                    else
                    {
                        Console.WriteLine("Thread " + Thread.CurrentThread.Name + " counter = " + j);
                        counter = j / (j - j);
                    }
                }
                catch (Exception)
                {
                    if (Thread.CurrentThread.Name.Equals("0"))
                    {
                        counter++;
                    }
                    else
                    {
                        counter += 2;
                    }
                }
            }

            if (Thread.CurrentThread.Name.Equals("0"))
            {
                if (counter == 10)
                {
                    Console.Out.WriteLine("Test Passed (only if the lines aren't jumbled");
                }
                else
                {
                    Console.Out.WriteLine("TryCatch Test Failed, counter = " + counter);
                    ExitCode += (1);
                }
            }
            else
            {
                if (counter == 20)
                {
                    Console.Out.WriteLine("Test Passed (only if the lines aren't jumbled)");
                }
                else
                {
                    Console.Out.WriteLine("Test Failed, counter = " + counter);
                    ExitCode += (1);
                }
            }
        }

        internal virtual void recurse(int counter, int i)
        {
            char[] abc = new char[100];

            if (counter == 100)
            {
                if (Thread.CurrentThread.Name.Equals("0"))
                {
                    Console.WriteLine("THREAD " + Thread.CurrentThread.Name + " COUNTER = " + i + " : THROWING EXTERNAL EX");
                }
                else
                {
                    Console.WriteLine("Thread " + Thread.CurrentThread.Name + " counter = " + i + " : Throwing external ex ");
                }
                (new ExternalClass()).ThrowException();
            }
            else
                recurse(++counter, i);
        }
    }
}
