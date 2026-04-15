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

        ulong[] testValues = [0x1000, 0, ulong.MaxValue, 0x7FFF_FFFF_FFFF_FFFFul];
        foreach (ulong value in testValues)
        {
            Interop.BOOL result;
            int hr = dbi.IsMatchingParentFrame(value, value, &result);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(Interop.BOOL.TRUE, result);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public unsafe void IsMatchingParentFrame_DifferentFramePointers_ReturnsFalse(MockTarget.Architecture arch)
    {
        DacDbiImpl dbi = CreateDacDbi(arch);

        (ulong fpToCheck, ulong fpParent)[] testCases =
        [
            (0x1000, 0x2000),
            (0, 1),
            (ulong.MaxValue, 0),
        ];
        foreach ((ulong fpToCheck, ulong fpParent) in testCases)
        {
            Interop.BOOL result;
            int hr = dbi.IsMatchingParentFrame(fpToCheck, fpParent, &result);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(Interop.BOOL.FALSE, result);
        }
    }
}
