// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;


public class BringUpTest_TypedReference
{
    const int Pass = 100;
    const int Fail = -1;
    const string Apple = "apple";
    const string Orange = "orange";

    [Fact]
    public static int TestEntryPoint()
    {
        int i = Fail;
        F(__makeref(i));

        if (i != Pass) return Fail;

        string j = Apple;
        G(__makeref(j));
        
        if (j != Orange) return Fail;

        return Pass;
    }

    static void F(System.TypedReference t)
    {
        __refvalue(t, int) = Pass;
    }

    static void G(System.TypedReference t)
    {
        __refvalue(t, string) = Orange;
    }

}    
