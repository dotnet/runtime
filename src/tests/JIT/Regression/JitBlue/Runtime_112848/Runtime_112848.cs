// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_112848;

public class BaseClass { }

public class TestClass : BaseClass
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Test_LDLOC(object _obj, bool flag)
    {
        object obj = _obj;
        TestClass inst = null;
        if (flag)
        {
            inst = (TestClass)obj;
            return inst != null;
        }
        else
        {
            inst = (TestClass)obj;
            return obj == null;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            if (!Test_LDLOC(new BaseClass(), false))
            {
                Console.WriteLine("Failed => 103");
                return 103;
            }
            
            return 104;
        }
        catch (InvalidCastException)
        {
            Console.WriteLine("Caught InvalidCastException as expected");
        }
        Console.WriteLine("Done");
        return 100;
    }
}
