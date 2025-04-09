// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using JitTest;
using Xunit;

public class WrappedException
{
    // C# wrapper method calling IL method
    // that throws a non-exception based object
    //
    [Fact]
    public static void Problem()
    {
        Test.Main();
    }
}
