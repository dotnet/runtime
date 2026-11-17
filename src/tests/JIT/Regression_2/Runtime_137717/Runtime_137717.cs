// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_137717
{
    [Fact]
    public static void TestEntryPoint()
    {
        Random random = new Random(1234);

        if (AvxVnni.IsSupported)
        {
            Assert.Equal(0, VnniRegisterPressure.CountWrongAvxVnniResults(random));
        }

        if (AvxVnniInt8.IsSupported)
        {
            Assert.Equal(0, VnniRegisterPressure.CountWrongAvxVnniInt8Results(random));
        }

        if (AvxVnniInt16.IsSupported)
        {
            Assert.Equal(0, VnniRegisterPressure.CountWrongAvxVnniInt16Results(random));
        }
    }
}
