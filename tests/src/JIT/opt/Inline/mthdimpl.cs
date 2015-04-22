// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// MethodImplAttribute
using System;
using System.Runtime.CompilerServices;
class MthdImpl
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int f(int a)
    {
        return a + 3;
    }

    public static int Main()
    {
        int retval = f(97);
        return retval;
    }
}
