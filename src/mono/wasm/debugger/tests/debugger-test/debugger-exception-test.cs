// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
namespace DebuggerTests
{
    public class ExceptionTestsClass
    {
        public class TestCaughtException
        {
            public void run()
            {
                try
                {
                    throw new CustomException("not implemented caught");
                }
                catch
                {
                    Console.WriteLine("caught exception");
                }
            }
        }

        public class TestUncaughtException
        {
            public void run()
            {
                throw new CustomException("not implemented uncaught");
            }
        }

        public static void TestExceptions()
        {
            TestCaughtException f = new TestCaughtException();
            f.run();

            TestUncaughtException g = new TestUncaughtException();
            g.run();
        }

    }

    public class CustomException : Exception
    {
        // Using this name to match with what js has.
        // helps with the tests
        public string message;
        public CustomException(string message)
            : base(message)
        {
            this.message = message;
        }
    }

    public class ExceptionTestsClassDefault
    {
        public class TestCaughtException
        {
            public void run()
            {
                try
                {
                    throw new Exception("not implemented caught");
                }
                catch
                {
                    Console.WriteLine("caught exception");
                }
            }
        }

        public class TestUncaughtException
        {
            public void run()
            {
                throw new Exception("not implemented uncaught");
            }
        }

        public static void TestExceptions()
        {
            TestCaughtException f = new TestCaughtException();
            f.run();

            TestUncaughtException g = new TestUncaughtException();
            g.run();
        }

    }
}
