// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using Xunit;

public class bug
{
    [Fact]
    public static int TestEntryPoint()
    {
        return 100;
    }
}
