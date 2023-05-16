// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
// Found by Antigen

public class Runtime_63942
{
    [Fact]
    public static int TestEntryPoint()
    {
        var _ = 3.14.ToString();
        return 100;
    }
}
