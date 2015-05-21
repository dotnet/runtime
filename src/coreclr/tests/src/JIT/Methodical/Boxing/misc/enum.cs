// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace BoxTest
{
    internal enum ToPrintOrNotToPrint
    {
        Print,
        DoNotPrint
    }

    internal class Test
    {
        protected object Fibonacci(object num, object flag)
        {
            if ((ToPrintOrNotToPrint)flag == ToPrintOrNotToPrint.DoNotPrint)
                return Fibonacci2(num, flag);
            if (((int)num % 2) == 0)
                return Fibonacci2(num, flag);
            return Fibonacci2(num, flag);
        }

        protected object Fibonacci2(object num, object flag)
        {
            int N;
            if ((int)num <= 1)
                N = (int)num;
            else
                N = (int)Fibonacci((int)num - 2,
                        ToPrintOrNotToPrint.DoNotPrint) + (int)Fibonacci((int)num - 1, flag);
            if ((ToPrintOrNotToPrint)flag == ToPrintOrNotToPrint.Print)
                Console.Write(N.ToString() + " ");
            return N;
        }

        private static int Main()
        {
            new Test().Fibonacci(20, ToPrintOrNotToPrint.Print);
            Console.WriteLine();
            Console.WriteLine("*** PASSED ***");
            return 100;
        }
    }
}
