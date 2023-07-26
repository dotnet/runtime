// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct MyStruct
{
    public string str;
    public static MyStruct MakeString_Inline(string st)
    {
        MyStruct ss;
        ss.str = st;
        return ss;
    }
}

public class ReturnStruct
{
    [Fact]
    public static int TestEntryPoint()
    {
        int iret = 100;
        MyStruct st = MyStruct.MakeString_Inline("Hello!");
        Console.WriteLine("st=" + st.str);
        return iret;
    }
}


