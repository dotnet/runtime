// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class DacDbiImplTests
{
    private static DacDbiImpl CreateDacDbi(MockTarget.Architecture arch)
    {
        var builder = new TestPlaceholderTarget.Builder(arch);
        TestPlaceholderTarget target = builder.Build();
        return new DacDbiImpl(target, legacyObj: null);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public unsafe void IsMatchingParentFrame_EqualFramePointers_ReturnsTrue(MockTarget.Architecture arch)
    {
        DacDbiImpl dbi = CreateDacDbi(arch);

        Interop.BOOL result;
        int hr = dbi.IsMatchingParentFrame(0x1000, 0x1000, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(Interop.BOOL.TRUE, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public unsafe void IsMatchingParentFrame_DifferentFramePointers_ReturnsFalse(MockTarget.Architecture arch)
    {
        DacDbiImpl dbi = CreateDacDbi(arch);

        Interop.BOOL result;
        int hr = dbi.IsMatchingParentFrame(0x1000, 0x2000, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(Interop.BOOL.FALSE, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public unsafe void IsMatchingParentFrame_BothZero_ReturnsTrue(MockTarget.Architecture arch)
    {
        DacDbiImpl dbi = CreateDacDbi(arch);

        Interop.BOOL result;
        int hr = dbi.IsMatchingParentFrame(0, 0, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(Interop.BOOL.TRUE, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public unsafe void IsMatchingParentFrame_MaxValue_ReturnsTrue(MockTarget.Architecture arch)
    {
        DacDbiImpl dbi = CreateDacDbi(arch);

        Interop.BOOL result;
        int hr = dbi.IsMatchingParentFrame(ulong.MaxValue, ulong.MaxValue, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(Interop.BOOL.TRUE, result);
    }
}
