// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


namespace DevDiv2_10623;

using System;
using Xunit;
public class Program
{
    public static bool IsGuid(object item)
    {
        return item is Guid;
    }
    [OuterLoop]
    [Fact]
    public static int TestEntryPoint()
    {
        if (IsGuid(Guid.NewGuid()))
            return 100;
        else
            return 99;
    }
}
