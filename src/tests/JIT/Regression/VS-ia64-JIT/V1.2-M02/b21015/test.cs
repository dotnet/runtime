// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct T
{
    public string S;
#pragma warning disable 0414
    public string SomeOtherString;
#pragma warning restore 0414

    public T(string _S)
    {
        S = _S;
        SomeOtherString = null;
    }

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

    static void bar(T t)
    {
        Console.WriteLine(t.TheString);
    }
}
