// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;


public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;
    const string Apple = "apple";
    const string Orange = "orange";

    public static int Main()
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
