// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Microsoft.Diagnostics.DataContractReader.Data;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

using MockGCCover = MockDescriptors.GCCover;

public class GCCoverTests
{
    internal static Target CreateTarget(
        MockTarget.Architecture arch,
        MockGCCover builder,
        Mock<IRuntimeTypeSystem> mockRuntimeTypeSystem = null)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget(arch, builder.Builder.GetReadContext().ReadFromTarget, builder.Types, builder.Globals);

        mockRuntimeTypeSystem ??= new Mock<IRuntimeTypeSystem>();

        IContractFactory<IGCCover> gcCoverFactory = new GCCoverFactory();

        ContractRegistry reg = Mock.Of<ContractRegistry>(
            c => c.GCCover == gcCoverFactory.CreateContract(target, 1)
                && c.RuntimeTypeSystem == mockRuntimeTypeSystem.Object);
        target.SetContracts(reg);
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCCoverageInfo_Explicit(MockTarget.Architecture arch)
    {
        GetGCCoverageInfoExplicitHelper(arch, new TargetPointer(0x1234_5678));
        GetGCCoverageInfoExplicitHelper(arch, TargetPointer.Null);
    }

    private void GetGCCoverageInfoExplicitHelper(MockTarget.Architecture arch, TargetPointer expectedGCCoverageInfo)
    {
        MockGCCover mockGCCover = new(arch);

        NativeCodeVersionHandle codeVersionHandle = mockGCCover.AddExplicitNativeCodeVersion(expectedGCCoverageInfo);

        var target = CreateTarget(arch, mockGCCover);

        // TEST

        var gcCover = target.Contracts.GCCover;
        Assert.NotNull(gcCover);

        TargetPointer? actualGCCoverageInfo = gcCover.GetGCCoverageInfo(codeVersionHandle);

        Assert.NotNull(actualGCCoverageInfo);
        Assert.Equal(expectedGCCoverageInfo, actualGCCoverageInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCCoverageInfo_Synthetic(MockTarget.Architecture arch)
    {
        GetGCCoverageInfoSyntheticHelper(arch, new TargetPointer(0x1234_5678));
        GetGCCoverageInfoSyntheticHelper(arch, TargetPointer.Null);
    }


    private void GetGCCoverageInfoSyntheticHelper(MockTarget.Architecture arch, TargetPointer expectedGCCoverageInfo)
    {
        Mock<IRuntimeTypeSystem> mockRTS = new Mock<IRuntimeTypeSystem>();

        TargetPointer methodDesc = new(/* arbitrary */ 0x1234_5678);
        MockSyntheticGCCoverageInfo(mockRTS, methodDesc, expectedGCCoverageInfo);

        NativeCodeVersionHandle codeVersionHandle = NativeCodeVersionHandle.OfSynthetic(methodDesc);

        var target = CreateTarget(arch, new MockGCCover(arch), mockRTS);

        // TEST

        var gcCover = target.Contracts.GCCover;
        Assert.NotNull(gcCover);

        TargetPointer? actualGCCoverageInfo = gcCover.GetGCCoverageInfo(codeVersionHandle);

        Assert.NotNull(actualGCCoverageInfo);
        Assert.Equal(expectedGCCoverageInfo, actualGCCoverageInfo);
    }

    private void MockSyntheticGCCoverageInfo(Mock<IRuntimeTypeSystem> mockRTS, TargetPointer methodDescAddress, TargetPointer expectedGCCoverageInfo)
    {
        MethodDescHandle methodDescHandle = new MethodDescHandle(methodDescAddress);
        mockRTS.Setup(rts => rts
            .GetMethodDescHandle(methodDescAddress))
            .Returns(methodDescHandle);
        mockRTS.Setup(rts => rts
            .GetGCCoverageInfo(methodDescHandle))
            .Returns(expectedGCCoverageInfo);
    }
}
