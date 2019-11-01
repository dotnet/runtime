// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

class Program
{
    static bool flag;

    static int Main(string[] args)
    {            
        flag = true;
        return Test();
    }

    public static int Test()
    {
        try
        {
            Repro();
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("Failure");
            return 101;                 
        }
        catch (Exception)
        {
            Console.WriteLine("Success");
            return 100;
        }

        Console.WriteLine("Failure");
        return 101;
    }

    private static void Repro()
    {
        string str = GetString();
        object info = null;

        if (str != null)
        {
            info = str.Length;
        }
        try
        {
            ThrowException();
            if (str != null)
            {
                Console.WriteLine(info);
            }
        }
        catch (Exception)
        {
            // The bug was that the jit was removing this check causing a NullReferenceException.
            if (str != null)
            {
                Console.WriteLine(str.Length);
            }
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowException()
    {
        if (flag)
        {
            throw new Exception();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string GetString()
    {
        return flag ? (string) null : "test";
    }
}
