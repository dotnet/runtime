// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public class X
{
    readonly static string S;
    readonly static string S_Expected;
    readonly static bool B0;
    readonly static bool B1;

    static X()
    {
        Console.WriteLine("Begin initializing class X");
        S = "0";
        B0 = Set("1", false);
        B1 = SetDirect("2", false);
        S_Expected = S;
        Console.WriteLine("Done initializing class X");
    }

    static bool Set(string s, bool shouldThrow)
    {
        var type = typeof(X);
        var field = type.GetField("S", BindingFlags.NonPublic | BindingFlags.Static);
        bool threw = false;
        bool unexpected = false;

        Console.WriteLine($"Attempting to update {field.Name} via SetValue, current value is '{S}', desired new value is '{s}'");

        try 
        {
            field.SetValue(null, s);
        }
        catch (FieldAccessException f)
        {
            Console.WriteLine("Caught {0}expected exception", shouldThrow ? "" : "un");
            Console.WriteLine(f);
            threw = true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception: {0}", e.GetType());
            Console.WriteLine(e);
            threw = true;
            unexpected = true;
        }

        if (!threw)
        {
            Console.WriteLine($"Updated {field.Name} to '{S}'");
        }

        return (shouldThrow == threw) && !unexpected;
    }

    static bool SetDirect(string s, bool shouldThrow)
    {
        int i = 0;
        TypedReference t = __makeref(i);
        var type = typeof(X);
        var field = type.GetField("S", BindingFlags.NonPublic | BindingFlags.Static);
        bool threw = false;
        bool unexpected = false;

        Console.WriteLine($"Attempting to update {field.Name} via SetValueDirect, current value is '{S}', desired new value is '{s}'");

        try 
        {
            field.SetValueDirect(t, s);
        }
        catch (FieldAccessException f)
        {
            Console.WriteLine("Caught {0}expected exception", shouldThrow ? "" : "un");
            Console.WriteLine(f);
            threw = true;

        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception: {0}", e.GetType());
            Console.WriteLine(e);
            unexpected = true;
            threw = true;
        }

        if (!threw)
        {
            Console.WriteLine($"Updated {field.Name} to '{S}'");
        }

        return (shouldThrow == threw) && !unexpected;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var s = S;
        bool b0 = Set("3", true);
        bool b1 = SetDirect("4", true);
        bool v = (S == S_Expected);
        if (!B0) Console.WriteLine("SetValue during class init unexpectedly threw");
        if (!B1) Console.WriteLine("SetValueDirect during class init unexpectedly threw");
        if (!b0) Console.WriteLine("SetValue after class init didn't throw as expected");
        if (!b1) Console.WriteLine("SetValueDirect after class init didn't throw as expected");
        Console.Write($"S is '{S}' ");
        if (v)
        {
            Console.WriteLine(" as expected");
        }
        else
        {
            Console.WriteLine($", should be '{S_Expected}'");
        }
        bool ok = B0 && B1 && b0 && b1 && v;

        Console.WriteLine(ok ? "PASS" : "FAIL");
        return ok ? 100 : -1;
    }
}


