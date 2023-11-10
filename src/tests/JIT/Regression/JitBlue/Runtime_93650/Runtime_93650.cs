// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

public struct Holder
{
    internal StringBuilder.AppendInterpolatedStringHandler _h;
    public Holder() => _h = new(0, 0, new());

    internal StringBuilder GetBuilder() => Unsafe.As<StringBuilder.AppendInterpolatedStringHandler, StringBuilder>(ref _h);
}

public static class Runtime_93650
{
    static int N = 1;

    [Fact]
    public static int Problem()
    {
        var sb = new Holder();
        for (int i = 0; i < N; i++)
        {
            var s = Bind(ref sb);
            if (s.Length != 0)
            {
                Console.WriteLine("FAILED: StringBuilder.ToString() returned: " + s);
                return -1;
            }
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string Bind(ref Holder parameters) => GetString(parameters.GetBuilder());

    public static string GetString(StringBuilder sb) => sb.ToString();
}
