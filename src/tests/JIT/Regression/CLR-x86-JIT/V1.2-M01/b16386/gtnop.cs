// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class gtnop
{
    [Fact]
    public static int TestEntryPoint()
    {
        byte[] arr = new byte[1];
        short i = 3;
        try { arr[(byte)(20u) * i] = 0; }
        catch (IndexOutOfRangeException) { return 100; }
        return 1;
    }
}
