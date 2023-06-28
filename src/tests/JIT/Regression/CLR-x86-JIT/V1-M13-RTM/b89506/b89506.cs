// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class AA
{
    [Fact]
    public static int TestEntryPoint() { Main1(); return 100; }

    internal static void Main1()
    {
        (new float[1, 1, 1, 1])[0, 0, 0, 0] -= (new float[1, 1])[0, 0];
    }
}
