// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

public sealed class GlobalTypeValidationTests
{
    [Fact]
    public void GlobalTypeCompatibility_RejectsMismatches()
    {
        Assert.False(TargetTypeHelpers.IsCompatiblePrimitiveType<byte>("uint32"));
        Assert.False(TargetTypeHelpers.IsCompatiblePrimitiveType<uint>("pointer"));
        Assert.False(TargetTypeHelpers.IsCompatiblePointerType("uint32"));
        Assert.True(TargetTypeHelpers.IsCompatiblePointerType("pointer"));
        Assert.True(TargetTypeHelpers.IsCompatiblePointerType("nint"));
        Assert.True(TargetTypeHelpers.IsCompatiblePointerType("nuint"));
    }
}
