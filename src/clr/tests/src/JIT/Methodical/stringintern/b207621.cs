// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

internal class Test
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static string FooBar()
    {
        return "Hello World";
    }

    static public int Main()
    {
        string a = null;
        string b = null;

        bool failure = false;
        try
        {
            try
            {
                a = "Hello World";
                b = FooBar();
                throw new Exception();
            }
            catch (System.Exception)
            {
            }

            Console.Write("Dynamic interning of non fixed up string...");
            if ((object)a != (object)b)
            {
                failure = true;
                Console.WriteLine("FAILED");
            }
            else
            {
                Console.WriteLine("PASSED");
            }

            try
            {
                a = "Hello World";
                b = FooBar();
                throw new Exception();
            }
            catch (System.Exception)
            {
            }

            Console.Write("Dynamic interning of fixed up string...");
            if ((object)a != (object)b)
            {
                failure = true;
                Console.WriteLine("FAILED");
            }
            else
            {
                Console.WriteLine("PASSED");
            }


            try
            {
                a = "Hello World non fixed up";
                b = "Hello World non fixed up";
                throw new Exception();
            }
            catch (System.Exception)
            {
            }

            Console.Write("Dynamic interning of string that is not in fixup list...");
            if ((object)a != (object)b)
            {
                failure = true;
                Console.WriteLine("FAILED");
            }
            else
            {
                Console.WriteLine("PASSED");
            }
        }
        catch (Exception)
        {
            failure = true;
        }

        if (failure)
        {
            return 999;
        }
        else
        {
            return 100;
        }
    }
}
