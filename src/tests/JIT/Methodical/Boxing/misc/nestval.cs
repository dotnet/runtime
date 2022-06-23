// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace BoxTest_nestval_cs
{
    internal struct MyBool { public bool val; }
    internal struct MyInt { public int val; }

    internal struct ArgInfo
    {
        public MyBool m_flag;
        public MyInt m_num;
    }

    public class Test
    {
        protected object Fibonacci(object args)
        {
            if (args.GetType() != typeof(ArgInfo))
                throw new Exception();
            return FibonacciImpl(args);
        }

        protected object FibonacciImpl(object args)
        {
            object N;
            if (((ArgInfo)args).m_num.val <= 1)
                N = ((ArgInfo)args).m_num.val;
            else
            {
                ArgInfo newargs1 = new ArgInfo();
                newargs1.m_num.val = ((ArgInfo)args).m_num.val - 2;
                newargs1.m_flag.val = false;
                ArgInfo newargs2 = new ArgInfo();
                newargs2.m_num.val = ((ArgInfo)args).m_num.val - 1;
                newargs2.m_flag.val = ((ArgInfo)args).m_flag.val;
                N = (int)Fibonacci(newargs1) + (int)Fibonacci(newargs2);
            }
            if (((ArgInfo)args).m_flag.val)
            {
                if ((((ArgInfo)args).m_num.val % 2) == 0)
                    Console.Write(N.ToString() + " ");
                else
                    Console.Write(N.ToString() + " ");
            }
            return N;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            ArgInfo args = new ArgInfo();
            args.m_flag.val = true;
            args.m_num.val = 20;
            new Test().Fibonacci(args);
            Console.WriteLine();
            Console.WriteLine("*** PASSED ***");
            return 100;
        }
    }
}
