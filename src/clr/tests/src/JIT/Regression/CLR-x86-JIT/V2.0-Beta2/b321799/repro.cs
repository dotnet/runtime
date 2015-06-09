// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

class Exception1 : Exception { }

class Exception2 : Exception { }

delegate void NoArg();

class SmallRepro
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Throws1()
    {
        throw new Exception1();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Throws2()
    {
        throw new Exception2();
    }


    static void Rethrows1()
    {
        try
        {
            Console.WriteLine("In Rethrows1");
            Throws1();
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught {0}, rethrowing", e);
            throw;
        }
    }

    static void CatchAll()
    {
        try
        {
            Console.WriteLine("In CatchAll");
            Throws2();
        }
        catch
        {
            Console.WriteLine("Caught something");
        }
    }

    static void Finally()
    {
        try
        {
            Console.WriteLine("In Finally");
            Rethrows1();
            Console.WriteLine("Unreached");
        }
        finally
        {
            Console.WriteLine("In Finally funclet (1), Exception1 should be in-flight");
            CatchAll();
            Console.WriteLine("In Finally funclet (2), Exception1 should be in-flight");
        }
    }

    static int Main()
    {
        bool bPassed = true;
        // Works
        Console.WriteLine("!!!!!!!!!!!!!!!!! Start Direct Call case !!!!!!!!!!!!!!!!!!!!!!!");
        try
        {
            Finally();
        }
        catch (Exception e)
        {
            if (e is Exception1)
            {
                Console.WriteLine("Caught Exception1");
                Console.WriteLine("Pass direct call");
            }
            else
            {
                Console.WriteLine("!!!! Fail direct call !!!!");
                Console.WriteLine("Caught {0}", e);
                bPassed = false;
            }
        }
        Console.WriteLine();
        Console.WriteLine();

        // Doesn't work
        Console.WriteLine("!!!!!!!!!!!!!!! Start Dynamic Invoke case !!!!!!!!!!!!!!!!!!!!!!");
        try
        {
            new NoArg(Finally).DynamicInvoke(null);
        }
        catch (Exception e)
        {
            if (e.InnerException is Exception1)
            {
                Console.WriteLine("Caught Exception1");
                Console.WriteLine("Pass Dynamic Invoke");
            }
            else
            {
                Console.WriteLine("!!!! Fail Dynamic Invoke !!!!");
                Console.WriteLine("Caught {0}", e.InnerException);
                bPassed = false;
            }
        }
        if (bPassed) return 100;
        return 1;
    }
}
