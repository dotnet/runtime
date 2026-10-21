// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class NativeContainerTests
{
    [Theory]
    [InlineData("vector")]
    [InlineData("ptr-vector")]
    [InlineData("fwd_list")]
    [InlineData("list")]
    [InlineData("queue")]
    [InlineData("umap")]
    public static void RunSuite(string group)
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeContainerTestsDriver", group);
        Console.Write(output);
        Console.Error.Write(error);
        Assert.Equal(0, exitCode);
    }
}
