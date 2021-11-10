// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

public class Program
{
    public static int Main()
    {
        if (System.Globalization.CultureInfo.CurrentCulture.Name != "en-US")
        {
            Console.WriteLine("Non-US English platforms are not supported.\nPassing without running tests");

            Console.WriteLine("--- Success");
            return 100;
        }

        try
        {
            Console.WriteLine("Validating char marshalling...");
            Char.PInvoke_Default.RunTest();
            Char.PInvoke_False_False.RunTest();
            Char.PInvoke_False_True.RunTest();
            Char.PInvoke_True_False.RunTest();
            Char.PInvoke_True_True.RunTest();

            Console.WriteLine("Validating LPStr marshalling...");
            LPStr.PInvoke_Default.RunTest();
            LPStr.PInvoke_False_False.RunTest();
            LPStr.PInvoke_False_True.RunTest();
            LPStr.PInvoke_True_False.RunTest();
            LPStr.PInvoke_True_True.RunTest();
            return 100;
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
