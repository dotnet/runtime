// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Compatibility.UnitTests;
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
