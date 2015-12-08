

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace BoxTest
{
    internal abstract class BaseTest
    {
        protected abstract object Fibonacci2(object num, object flag);
    }

    internal class Test : BaseTest
    {
        private object _num;

        protected object Fibonacci(object num, object flag)
        {
            if (num.GetType() != typeof(float) ||
                flag.GetType() != typeof(bool))
                throw new Exception();

            return Fibonacci2(num, flag);
        }

        protected override object Fibonacci2(object num, object flag)
        {
            object N;
            if ((float)num < 1.1)
                N = num;
            else
                N = (float)Fibonacci((float)num - 2.0f, false) + (float)Fibonacci((float)num - 1.0f, flag);
            if ((bool)flag)
                Console.Write(N.ToString() + " ");
            return N;
        }

        public Test(object num)
        {
            _num = (float)(double)num;
        }

        public object Print()
        {
            Fibonacci(_num, true);
            Console.WriteLine();
            return _num;
        }

        private static int Main()
        {
            Test test = new Test(20.0d);
            test.Print();
            Console.WriteLine("*** PASSED ***");
            return 100;
        }
    }
}
