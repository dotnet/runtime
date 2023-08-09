// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public interface IGetContents {
    (string, int, string) GetContents();
}

public struct MyStruct : IGetContents {
    public string s1;
    public int a;
    public string s2;

    public (string, int, string) GetContents()
    {
        return (s1, a, s2);
    }
}

public class Program {

    public delegate (string, int, string) MyDelegate(IGetContents arg);

    [Fact]
    public static int TestEntryPoint()
    {
        MyStruct str = new MyStruct();
        str.s1 = "test1";
        str.a = 42;
        str.s2 = "test2";

        MethodInfo mi = typeof(IGetContents).GetMethod("GetContents");
        MyDelegate func = (MyDelegate)mi.CreateDelegate(typeof(MyDelegate));

        (string c1, int c2, string c3) = func(str);
        if (c1 != "test1")
            return 1;
        if (c2 != 42)
            return 2;
        if (c3 != "test2")
            return 3;
        return 100;
    }
}
