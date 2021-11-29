// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

class TlsStaticsAreNotSupported
{
    public static int Main()
    {
        if (typeof(TlsStaticsAreNotSupported).Assembly.Location is "")
        {
            Console.WriteLine("Skipping the test: Assembly.Location returned empty string, assuming dynamic loading is problematic");
            return 100;
        }
        
        Exception exception = null;
        try
        {
            Assembly.LoadFrom("data-tls.dll");
        }
        catch (System.BadImageFormatException e)
        {
            return 100;
        }
        catch (Exception e)
        {
            exception = e;
        }

        Console.WriteLine($"Expected: BadImageFormatException, actual: {exception?.GetType().ToString() ?? "none"}");
        return 101;
    }
}
