// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class AA
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool b = false;
        b = (b ? (object)b : (object)new AA()) ==
            (b ? new AA() : (b ? new AA() : null));
        return 100;
    }
}
