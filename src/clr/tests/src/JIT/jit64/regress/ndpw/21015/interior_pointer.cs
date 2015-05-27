// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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

internal class Tester
{
    public static int Main()
    {
        T t1, t2;

        t1 = new T();
        t2 = new T("passed.");

        bar(t1);
        bar(t2);
        return 100;
    }

    public static void bar(T t)
    {
        Console.WriteLine(t.TheString);
    }
}
