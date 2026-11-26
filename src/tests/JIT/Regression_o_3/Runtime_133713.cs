// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_133713;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133713
{
    private sealed class Data
    {
        public int V;
    }

    private sealed class Node
    {
        public int Get(Data data) => data.V;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Data data = new() { V = 123 };
            _ = Foo(data, null);
        }
        catch (NullReferenceException)
        {
            return 100;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Foo(Data data, Node node) => node.Get(data);
}
