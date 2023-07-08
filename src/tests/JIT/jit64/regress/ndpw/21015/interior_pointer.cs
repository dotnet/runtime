// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

internal struct T
{
    public string S;
    public string SomeOtherString;

    public T(string _S)
    {
        S = _S;
        SomeOtherString = null;
    }
    //
    //For the testcase to fail, get_TheString must be inlined 
    //into bar() which our current heuristics do
    //
    public string TheString
    {
        get
        {
            return (S != null ? S : "<nothing>");
        }
    }
}

public class Tester
{
    [Fact]
    public static int TestEntryPoint()
    {
        T t1, t2;

        t1 = new T();
        t2 = new T("passed.");

        bar(t1);
        bar(t2);
        return 100;
    }

    private static void bar(T t)
    {
        Console.WriteLine(t.TheString);
    }
}
