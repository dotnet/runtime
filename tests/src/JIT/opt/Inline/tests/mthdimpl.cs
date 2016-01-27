// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
internal class MthdImpl
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
