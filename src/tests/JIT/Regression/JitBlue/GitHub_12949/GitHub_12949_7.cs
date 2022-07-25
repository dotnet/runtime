// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public struct V
{
    public V(int x)
    {
        Token = x;
    }

    public int Token;
}

class M
{
    static int F(int x, object a)
    {
        int result = 0;

        if (a is V)
        {
            int token = ((V)a).Token;
            Console.WriteLine("F: Token is {0}", token);
            result = x + token;
        }

        return result;
    }

    static int G(object a, int x)
    {
        return F(x, a);
    }

    static int Trouble(ref V v)
    {
        Console.WriteLine("T: Token is {0}", v.Token);
        int result = v.Token;
        v.Token++;
        return result;
    }

    public static int Main()
    {
        // Ensure we get right order of side effects from boxes
        // now that we are splitting them into multiple statements.
        V v1 = new V(11);
        int result1 = F(Trouble(ref v1), v1);
        V v2 = new V(11);
        int result2 = G(v2, Trouble(ref v2));
        Console.WriteLine("Result1 = {0}; Result2 = {1}", result1, result2);
        return result1 + result2 + 55;
    }
}

