// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        MethodInfo invokeMethod = typeof(Action).GetMethod("Invoke");
        Action<Action> wrapperDelegate = invokeMethod.CreateDelegate<Action<Action>>();
        Assert.NotNull(wrapperDelegate);

        try
        {
            wrapperDelegate(null);
        }
        catch (NullReferenceException)
        {
            return 100;
        }

        return 101;
    }
}
