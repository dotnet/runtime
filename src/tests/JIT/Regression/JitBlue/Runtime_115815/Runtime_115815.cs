// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_115815
{
    [Fact]
    public static void TestEntryPoint()
    {
        var destination = new KeyValuePair<Container, double>[1_000];

        // loop to make this method fully interruptible + to get into OSR version
        for (int i = 0; i < destination.Length * 1000; i++)
        {
            destination[i / 1000] = default;
        }

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < destination.Length; j++)
            {
                destination[j] = GetValue(j);
            }

            Thread.Sleep(10);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static KeyValuePair<Container, double> GetValue(int i)
        => KeyValuePair.Create(new Container(i.ToString()), (double)i);

    private struct Container
    {
        public string Name;
        public Container(string name) { this.Name = name; }
    }
}
