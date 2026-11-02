// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Test136814
{
    [Fact]
    public static int TestEntryPoint()
    {
        return DelegateCaller.Invoke(new Test136814().Target);
    }

    private int Target(DayOfWeek? value)
    {
        return 100;
    }
}
