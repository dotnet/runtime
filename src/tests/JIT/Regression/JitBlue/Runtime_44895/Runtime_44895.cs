// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct Text
{
    private readonly string _value;
    public Text(string value) => _value = value;
    public string Value => _value ?? string.Empty; 
}

public class TextProperty 
{
    public Text GetValue(Text? a = null, Text? b = null, Text? c = null, Text? d = null)
    {
        if (a.HasValue) return a.Value;
        if (b.HasValue) return b.Value;
        if (c.HasValue) return c.Value;
        if (d.HasValue) return d.Value;
        return default;
    }
}

public class Repro
{
    [Fact]
    public static int TestEntryPoint()
    {
        string test = "test";
        TextProperty t = new TextProperty();
        Text gv = t.GetValue(new Text(test));
        bool result = test.Equals(gv.Value);
        Console.WriteLine(result ? "Pass" : "Fail");
        if (!result) Console.WriteLine($"got '{gv.Value}', expected '{test}'");
        return result ? 100 : -1;
    }
}