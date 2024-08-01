// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using Xunit;

public class Program
{
    public int value = 100;

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

        FieldInfo targetField = typeof(Delegate).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(targetField);
        targetField.SetValue(secondLevel, null);

        try
        {
            secondLevel(23);
        }
        catch (NullReferenceException)
        {
            return prog.value;
        }

        return 101;
    }
}
