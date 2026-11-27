// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

public class DelegateThunkTrimming
{
    public static int Main()
    {
        Func<int, int> d = Identity;
        if (d(42) != 42 || d.Target is not null)
            return 1;

        MethodInfo getThunk = typeof(Delegate).GetMethod("GetThunk", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new Exception("GetThunk is missing");
        object multicastThunk = typeof(Delegate).GetField("MulticastThunk", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
        object objectArrayThunk = typeof(Delegate).GetField("ObjectArrayThunk", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
        foreach (object thunkKind in new[] { multicastThunk, objectArrayThunk })
        {
            if ((IntPtr)getThunk.Invoke(d, new object[] { thunkKind }) != IntPtr.Zero)
            {
                Console.WriteLine($"Unexpected thunk {thunkKind}");
                return 2;
            }
        }

        Console.WriteLine("Delegate thunk checks passed");
        return 100;
    }

    private static int Identity(int value) => value;
}
