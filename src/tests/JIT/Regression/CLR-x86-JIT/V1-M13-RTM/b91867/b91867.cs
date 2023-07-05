// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class CC
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool b = false;
        object local19 = b ? null : (object)new CC();
#pragma warning disable 1718
        String[] local21 = (b == b ? b : b) ? new string[1] : null;
#pragma warning restore 1718
        return 100;
    }
}
