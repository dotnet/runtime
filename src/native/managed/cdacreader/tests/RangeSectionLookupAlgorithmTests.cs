// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class RangeSectionLookupAlgorithmTests
{


    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestLookupFail(MockTarget.Architecture arch)
    {
        Assert.True(arch.Is64Bit);
    }
}
