// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using TestLibrary;

public class Runtime_121736
{
    [ActiveIssue("needs triage", TestRuntimes.Mono)]
    [Fact]
    public static void Test()
    {
        HashSet<Fake> pureSelectedObjects = [new Fake(TestEnum.One)];
        try
        {
            for (var i = 0; i < 10_000_000; i++)
            {
                TestEnum txt = pureSelectedObjects.Select(x => (TestEnum)x.GetSourceItem()).First();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        Console.WriteLine("Passed");
    }
}

public enum TestEnum
{
    One
}

public class Fake
{
    private readonly TestEnum src;

    public Fake(TestEnum t)
    {
        src = t;
    }

    public object GetSourceItem()
    {
        return src;
    }
}
