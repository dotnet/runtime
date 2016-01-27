// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

// TypeConstructorName
public class ConstructorInfoTypeConstructorName
{
    // Positive Test 1: Ensure ConstructorInfo.TypeContructorName is correct
    [Fact]
    public void PosTest1()
    {
        Assert.Equal(ConstructorInfo.TypeConstructorName, ".cctor");
    }
}
