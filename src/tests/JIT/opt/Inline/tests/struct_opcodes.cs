// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

internal struct SimpleStruct
{
    public int i;
    public string str;
}

internal struct newStruct
{
    public SimpleStruct S;
}

public struct X
{
    public SingleInt f;
}


public class SingleInt
{
    public int i1;
}

public class TestStruct
{
    internal static SimpleStruct aMethod_Inline(SimpleStruct Struct)
    {
        Struct.i = 10;
        Struct.str = "abc";
        int x = Struct.i;
        string str1 = Struct.str;
        return Struct;
    }

    internal static newStruct bMethod_Inline()
    {
        newStruct nStruct;
        nStruct.S.i = 1;
        nStruct.S.str = "newstruct";
        SimpleStruct St;
        St.i = 20;
        St.str = "def";
        return nStruct;
    }

    internal static newStruct cMethod_Inline()
    {
        newStruct nStruct;
        nStruct.S.i = 1;
        nStruct.S.str = "newstruct";
        return nStruct;
    }

    internal static void dMethod_Inline()
    {
        X x;
        x.f = new SingleInt();
        x.f.i1 = 77;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        SimpleStruct st;
        newStruct newst;
        st.i = 4;
        st.str = "xyz";
        Console.WriteLine(st.i);
        st = aMethod_Inline(st);
        Console.WriteLine(st.i);
        newst = bMethod_Inline();
        Console.WriteLine(st.i);
        newst = cMethod_Inline();
        Console.WriteLine(st.i);
        dMethod_Inline();
        return 100;
    }
}

