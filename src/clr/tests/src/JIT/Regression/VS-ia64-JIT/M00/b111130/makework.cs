// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class test
{
    public static int Main()
    {
        try
        {
            Console.WriteLine("In try1");

            throw new Exception();
        }
        catch (Exception)
        {
            Console.WriteLine("In catch1");

            try
            {
                Console.WriteLine("In try2");

                try
                {
                    Console.WriteLine("In try3");

                    throw new Exception();
                }
                catch
                {
                    Console.WriteLine("In catch3");
                    goto L;
                }
            }
            finally
            {
                Console.WriteLine("In finally2");
            }
        }
        finally
        {
            Console.WriteLine("In finally1");
        }


        Console.WriteLine("Never executed");
        return 1;
    L:
        Console.WriteLine("Done");
        return 100;


    }
}