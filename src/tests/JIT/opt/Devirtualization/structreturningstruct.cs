// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Xunit;

// Runtime issue 52975.
//
// If we devirtualize an interface call on a struct,      ** and **
// update the call site to invoke the unboxed entry,      ** and **
// the method returns a struct via hidden buffer pointer, ** and **
// the unboxed method requires a type context arg,
//
// we need to be careful to pass the type context argument
// in the right spot in the arglist.
//
// The test below is set up to devirtualize under PGO.
//
// DOTNET_TieredPGO=1
// DOTNET_TC_QuickJitForLoopsO=1
//
public class X
{
    static int F(IDictionary i)
    {
        int r = 0;
        IDictionaryEnumerator e = i.GetEnumerator();
        while (e.MoveNext())
        {
            // This is the critical call.
            //
            DictionaryEntry entry = e.Entry;
            r++;
        }
        return r;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Dictionary<string, string> s = new Dictionary<string, string>();
        s["hello"] = "world";
        int r = 0;

        for (int i = 0; i < 50; i++)
        {
            r += F(s);
            Thread.Sleep(15);
        }

        int iter = 100;

        for (int i = 0; i < iter; i++)
        {
            r += F(s);
        }

        int result = 2 * (r - iter);
        Console.WriteLine($"Result={result}");
        return result;
    }
}
