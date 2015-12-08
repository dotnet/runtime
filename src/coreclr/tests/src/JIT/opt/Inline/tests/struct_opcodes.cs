// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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

internal class TestStruct
{
    public static SimpleStruct aMethod_Inline(SimpleStruct Struct)
    {
        Struct.i = 10;
        Struct.str = "abc";
        int x = Struct.i;
        string str1 = Struct.str;
        return Struct;
    }

    public static newStruct bMethod_Inline()
    {
        newStruct nStruct;
        nStruct.S.i = 1;
        nStruct.S.str = "newstruct";
        SimpleStruct St;
        St.i = 20;
        St.str = "def";
        return nStruct;
    }

    public static newStruct cMethod_Inline()
    {
        newStruct nStruct;
        nStruct.S.i = 1;
        nStruct.S.str = "newstruct";
        return nStruct;
    }

    public static void dMethod_Inline()
    {
        X x;
        x.f = new SingleInt();
        x.f.i1 = 77;
    }
    public static int Main()
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

