// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_115493
{
    [Fact]
    public static void Problem()
    {
        int checksum = 44444;
        int exitCode = 100;

        [MethodImpl(MethodImplOptions.NoInlining)]
        int ProcessValue(object val)
        {
            return val switch
            {
                int i => i,
                short s => s,
                _ => 0
            };
        }

        var a = new Vector<short>(25);
        a = Vector.SquareRoot(a);
        checksum = unchecked(checksum + ProcessValue(a[0]));
        if (a[0] != 5)
        {
            exitCode = 0;
        }

        Assert.Equal(44449, checksum);
        Assert.Equal(100, exitCode);
    }
}
