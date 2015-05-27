// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

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

class Tester
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