// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace test
{

    class Class1
    {

        static int Main()
        {
            try
            {
                Console.WriteLine(" try 1");
                try
                {
                    Console.WriteLine("\t try 1.1");
                    Console.WriteLine("\t throwing an exception here!");
                    throw new System.ArithmeticException("My ArithmeticException");
                }
                catch (Exception)
                {
                    Console.WriteLine("\t catch 1.1");
                    goto inner_try;
                throw_exception:
                    Console.WriteLine("\t throwing another exception here!");
                    throw new System.ArithmeticException("My ArithmeticException");
                inner_try:
                    try
                    {
                        Console.WriteLine("\t\t try 1.1.1");
                    }
                    finally
                    {
                        Console.WriteLine("\t\t finally 1.1.1");
                    }
                    goto throw_exception;
                }
            }
            catch (Exception)
            {
                Console.WriteLine(" catch 1");
            }
            finally
            {
                Console.WriteLine(" finally 1");
            }
            return 100;
        }
    }
}
