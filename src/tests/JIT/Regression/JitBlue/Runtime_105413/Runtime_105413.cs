// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_105413
{
    private static ushort[] s_field1;
    private static bool s_field2;

    [Fact]
    public static void TestEntryPoint()
    {
        s_field2 = true;
        Foo(2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo(int n)
    {
        for (int i = 0; i < n; i++)
        {
            int j = 0;
            do
            {
                s_field1 = [1];
                j++;
            } while (j < n);

            s_field2 = false;
            Bar(s_field2);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Bar(bool value)
    {
        Assert.False(value);
    }
}
