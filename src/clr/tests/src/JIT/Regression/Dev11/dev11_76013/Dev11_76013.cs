// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;

namespace Test
{
    class MyException : Exception
    {
        int _code;

        public MyException(int code)
        {
            _code = code;
        }
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            bool flag = false;
            try
            {
                flag = true;
                Console.WriteLine("flag={0} (should be True)", flag);
                ThrowMyException(); // throws exception
            }
            catch (MyException)
            {
                Console.WriteLine("MyException Handled");
            }
            catch (Exception)
            {
                Console.WriteLine("Unknown Exception: We should never reach this line");
                flag = false;
            }
            Console.WriteLine("flag={0} (should be True)", flag);
            if (flag)
            {
                Console.WriteLine("PASSED");
                return 100;
            }
            else
            {
                Console.WriteLine("FAILED");
                return -1;
            }
        }

        private static void ThrowMyException()
        {
            throw new MyException(42);
        }
    }
}
