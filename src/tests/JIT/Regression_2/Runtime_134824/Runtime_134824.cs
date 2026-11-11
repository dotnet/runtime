// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134824
{
    private static Vector4 s_vec;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 Compute(Vector4 unusedParam)
    {
        return s_vec;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        bool neverTrue = default;
        Vector4 local = default;
        if (neverTrue)
        {
            try
            {
                Sink(local);
            }
            catch
            {
            }
        }

        Sink(Compute(default));
        Sink(neverTrue);
        Sink(local);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Sink(object value)
    {
    }
}
