// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#pragma warning disable 8500

public unsafe class Program
{
    static void AssertEqual<T>(T actual, T expected)
    {
        if (!actual.Equals(expected))
            throw new Exception($"Failed Scenario. Actual = {actual}. Expected = {expected}");
    }

    public static Type GrabArray<T>() => typeof(T[]);
    public static Type GrabPtr<T>() => typeof(T*);
    public static Type GrabFnptr<T>() => typeof(delegate*<T>);

    public static int Main()
    {
        AssertEqual(GrabArray<int>().GetElementType(), typeof(int));
        AssertEqual(GrabArray<string>().GetElementType(), typeof(string));

        AssertEqual(GrabPtr<uint>().GetElementType(), typeof(uint));
        AssertEqual(GrabPtr<object>().GetElementType(), typeof(object));

        AssertEqual(GrabFnptr<DateTime>().GetFunctionPointerReturnType(), typeof(DateTime));
        AssertEqual(GrabFnptr<Action>().GetFunctionPointerReturnType(), typeof(Action));

        return 100;
    }
}
