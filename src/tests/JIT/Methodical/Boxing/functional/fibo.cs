// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace BoxTest_fibo_cs
{
    public class Test
    {
        protected object Fibonacci(object num, object flag)
        {
            if (num.GetType() != typeof(int) ||
                flag.GetType() != typeof(bool))
                throw new Exception();

            return Fibonacci2(num, flag);
        }

        protected object Fibonacci2(object num, object flag)
        {
            object N;
            if ((int)num <= 1)
                N = num;
            else
                N = (int)Fibonacci((int)num - 2, false) + (int)Fibonacci((int)num - 1, flag);
            if ((bool)flag)
            {
                if (((int)num % 2) == 0)
                    Console.Write(N.ToString() + " ");
                else
                    Console.Write(N.ToString() + " ");
            }
            return N;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            new Test().Fibonacci(20, true);
            Console.WriteLine();
            Console.WriteLine("*** PASSED ***");
            return 100;
        }
    }
}
