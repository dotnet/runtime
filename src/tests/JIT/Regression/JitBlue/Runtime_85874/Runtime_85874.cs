// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_85874
{
    [Fact]
    public static int Exposed()
    {
        ListElement e1;
        e1.Next = &e1;
        ListElement e2;
        e2.Next = null;
        e2.Value = 100;
        *e1.Next = e2;
        return e1.Value;
    }

    [Fact]
    public static int DestinationIsAddress()
    {
        ListElement e1;
        ListElement e2 = default;
        e2.Value = 100;
        e1.Next = &e2;
        e1.Value = 1234;
        Consume(e1);
        e1 = *e1.Next;
        Consume(e1);
        return e1.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume<T>(T val)
    {
    }

    struct ListElement
    {
        public ListElement* Next;
        public int Value;
    }

}
