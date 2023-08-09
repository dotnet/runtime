// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_72081
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Vector128<UInt128> d1 = default;
            Vector128<UInt128> d2 = Vector128.Create<UInt128>(111);
            Vector128<UInt128> x = d1 + d2 * d1;
            return d1.GetHashCode() + x.GetHashCode();
        }
        catch (NotSupportedException)
        {
            return 100;
        }
    }
}
