// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_1133
{
    static Guid s_dt;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Guid TestValueTypesInInlinedMethods()
    {
        var dt = new Guid();

        // This method, once inlined, should directly copy the newly created 'Guid' to s_dt.
        Method1(dt);

        return dt;
    }

    private static void Method1(Guid dt)
    {
        Method2(dt);
    }

    private static void Method2(Guid dt)
    {
        Method3(dt);
    }

    private static void Method3(Guid dt)
    {
        s_dt = dt;
    }
    
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 100;
        try
        {
            Guid g = TestValueTypesInInlinedMethods();
            if (g != s_dt)
            {
                result = -1;
            }
        }
        catch (Exception)
        {
            result = -1;
        }

        return result;
    }
}
