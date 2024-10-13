// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_103477
{
    static int s_count;

    [Fact]
    public static int Test()
    {
        int result = -1;
        try
        {
            Problem();
            result = 100;
        }
        catch (Exception)
        {
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Problem()
    {
        string s = "12151";
        int i = 0;

        char? a = null;
        int count = 0;
        while (true)
        {
            string? res = get(s, ref i, ref a);
            if (res != null)
            {
                Count(res);
                a = null; // !!! this line is removed from the published version
                continue;
            }

            if (i >= s.Length)
                break;

            a = '.';
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Count(string s)
    {
        s_count++;
        if (s_count > 5) throw new Exception();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string? get(string s, ref int index, ref char? a)
    {
        if (index >= s.Length)
            return null;

        if (a == '.')
            return ".";

        a ??= s[index++];
        return (a == '1') ? "1" : null;
    }
}
