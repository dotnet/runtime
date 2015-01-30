// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Add1(int x) { return x+1; }

    public static int Main()
    {
        int y = Add1(1);
        if (y == 2) return Pass;
        else return Fail;
    }
}
