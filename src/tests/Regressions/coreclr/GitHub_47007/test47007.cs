// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

class Base<T1>
{
    public virtual Base<T1> Method() => null;
}
class Derived<T1, T2> : Base<T1>
{
    public override Derived<T1, T2> Method() => null;
}

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        _ = new Derived<string, int>();
    }
}
