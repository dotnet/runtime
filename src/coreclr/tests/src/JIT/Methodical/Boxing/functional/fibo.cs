

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BoxTest
{
    internal class Test
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

        private static int Main()
        {
            new Test().Fibonacci(20, true);
            Console.WriteLine();
            Console.WriteLine("*** PASSED ***");
            return 100;
        }
    }
}
