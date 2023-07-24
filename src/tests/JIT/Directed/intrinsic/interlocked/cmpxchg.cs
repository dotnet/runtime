// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;
public class CMPXCHG
{
    public static int g_static = -1;
    internal static void Function(int bit, bool value)
    {
        for (; ;)
        {
            int oldData = g_static;
            int newData;
            if (value)
            {
                newData = oldData | bit;
            }
            else
            {
                newData = oldData & ~bit;
            }

#pragma warning disable 0420
            int result = Interlocked.CompareExchange(ref g_static, newData, oldData);
#pragma warning restore 0420

            if (result == oldData)
            {
                return;
            }
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        for (int i = 0; i < 10; ++i)
        {
            if (g_static < 10)
            {
                Function(7, true);
            }
            if (g_static < 9)
            {
                Function(11, false);
            }
            if (g_static < 8)
                Function(12, false);
        }
        return 100;
        //If we dont reach here, we have a problem!
    }
}
