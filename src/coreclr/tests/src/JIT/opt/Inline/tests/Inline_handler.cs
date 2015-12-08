// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

internal class MainApp
{
    public static void PrintA_NoInline()
    {
        Console.WriteLine("A_NoInline");
        throw new Exception("throw in method PrintA_Inline");
    }
    public static void PrintA_Inline()
    {
        Console.WriteLine("A");
    }

    public static void PrintB_Inline()
    {
        Console.WriteLine("B");
    }

    public static void PrintC_Inline()
    {
        Console.WriteLine("C");
    }

    public static int Main()
    {
        int retval = 101;
        try
        {
            PrintA_Inline();
            PrintA_NoInline();
            retval = 100;
        }
        catch (Exception)
        {
            Console.WriteLine("Caught exception thrown by method PrintA_NoInline");
            PrintB_Inline();
            retval = 100;
        }
        finally
        {
            PrintC_Inline();
        }
        return retval;
    }
}


