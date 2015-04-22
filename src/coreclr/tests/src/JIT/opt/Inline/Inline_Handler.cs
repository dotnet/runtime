// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

class MainApp
{

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void PrintA_NoInline()
    {
        Console.WriteLine("A_NoInline");
        throw new Exception("throw in method PrintA_Inline");
    }
    public static void PrintA_Inline()
    {
        Console.WriteLine("A");

    }

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void PrintB_Inline()
    {
        Console.WriteLine("B");
    }

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void PrintC_Inline()
    {
        Console.WriteLine("C");
    }

    public static int Main()
    {
        int retval = 101;
        try
        {
            PrintA_Inline();    //this call should be inlined.
            PrintA_NoInline();
            retval = 100;       // This call should not be inlined.       
        }
        catch (Exception)
        {
            Console.WriteLine("Caught exception thrown by method PrintA_NoInline");
            PrintB_Inline();
            retval = 100;       // This call should NOT be inlined.                 
        }
        finally
        {
            PrintC_Inline();    // This call should be inlined. 

        }
        return retval;
    }

}


