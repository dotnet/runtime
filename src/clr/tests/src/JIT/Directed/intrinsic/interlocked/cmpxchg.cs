// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;
public class CMPXCHG
{
    public static int g_static = -1;
    public static void Function(int bit, bool value)
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
    public static int Main()
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
