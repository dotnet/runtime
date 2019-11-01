// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Runtime.CompilerServices;

// When the implicit tail call optimization
// was done inside inlinee with several returns, the compiler created a return spill
// temp to merge all returns in one return expression and this spill was not expected
// during the tail call transformation.

class GitHub_14783
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string X(string s)
    {
        return s;
    }

    public static string X1(string s, string q)
    {
        for (int k = 0; k < 10; k++)
        {
            s = X(s);
        }

        return s;
    }

    public static string D(string x)
    {
        string a = x;
        string b = x;
        return X1(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string C(string x)
    {
        string s = x;

        if (s.Length > 3)
        {
            return D(s);
        }
        else
        {
            return X(s);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string B(string x)
    {
        return C(x);
    }

    public static string A(string x)
    {
        string s = x;

        for (int k = 0; k < 10; k++)
        {
            s = X(s);
        }

        return B(x);
    }

    public static int Main()
    {
        string v = A("Hello");
        return v.Length + 95;
    }
}
