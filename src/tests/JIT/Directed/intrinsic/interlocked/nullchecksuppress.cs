// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;
public class NCS
{
    public int field;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Increment(ref int value)
    {
        return Interlocked.Increment(ref value);
    }
    public static int Decrement(ref int value)
    {
        return Interlocked.Decrement(ref value);
    }
    public static int Add(ref int value, int other)
    {
        return Interlocked.Add(ref value, other);
    }
    public static int CompareExchange(ref int value, int newData, int oldData)
    {
        return Interlocked.CompareExchange(ref value, newData, oldData);
    }

    public static int Main()
    {
        NCS ncs = new NCS();
        ncs.field = 99;
        CompareExchange(ref ncs.field, 105, 99);
        Decrement(ref ncs.field);
        Add(ref ncs.field, -5);
        Increment(ref ncs.field);
        return ncs.field;
    }
}
