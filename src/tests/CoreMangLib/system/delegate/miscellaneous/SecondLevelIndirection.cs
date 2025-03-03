// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

public class Program
{
    public int value = 23;

    public void Update(int num)
    {
        value += num;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Program prog = new Program();

        Action<int> action = prog.Update;
        Action<int> secondLevel = action.Invoke;

        secondLevel(77);

        // Update should be invoked exactly once
        return prog.value;
    }
}
