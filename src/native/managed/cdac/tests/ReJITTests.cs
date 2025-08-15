// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

using MockReJIT = MockDescriptors.ReJIT;

public class ReJITTests
{
    internal static Target CreateTarget(
        MockTarget.Architecture arch,
        MockReJIT builder,
        Mock<ICodeVersions> mockCodeVersions = null)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget(arch, builder.Builder.GetMemoryContext().ReadFromTarget, builder.Types, builder.Globals);

        mockCodeVersions ??= new Mock<ICodeVersions>();

        IContractFactory<IReJIT> rejitFactory = new ReJITFactory();

        ContractRegistry reg = Mock.Of<ContractRegistry>(
            c => c.ReJIT == rejitFactory.CreateContract(target, 1)
                && c.CodeVersions == mockCodeVersions.Object);
        target.SetContracts(reg);
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRejitId_SyntheticAndExplicit_Success(MockTarget.Architecture arch)
    {
        MockReJIT mockRejit = new MockReJIT(arch);

        Dictionary<ILCodeVersionHandle, TargetNUInt> expectedRejitIds = new()
        {
            // synthetic ILCodeVersionHandle
            { ILCodeVersionHandle.CreateSynthetic(new TargetPointer(/* arbitrary */ 0x100), /* arbitrary */ 100), new TargetNUInt(0) },
            { mockRejit.AddExplicitILCodeVersion(new TargetNUInt(1), MockReJIT.RejitFlags.kStateActive), new TargetNUInt(1) },
            { mockRejit.AddExplicitILCodeVersion(new TargetNUInt(2), MockReJIT.RejitFlags.kStateRequested), new TargetNUInt(2) },
            { mockRejit.AddExplicitILCodeVersion(new TargetNUInt(3), MockReJIT.RejitFlags.kStateRequested), new TargetNUInt(3) }
        };

        var target = CreateTarget(arch, mockRejit);

        // TEST

        var rejit = target.Contracts.ReJIT;
        Assert.NotNull(rejit);

        foreach (var (ilCodeVersionHandle, expectedRejitId) in expectedRejitIds)
        {
            TargetNUInt rejitState = rejit.GetRejitId(ilCodeVersionHandle);
            Assert.Equal(expectedRejitId, rejitState);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRejitState_SyntheticAndExplicit_Success(MockTarget.Architecture arch)
    {
        MockReJIT mockRejit = new MockReJIT(arch);

        Dictionary<ILCodeVersionHandle, RejitState> expectedRejitStates = new()
        {
            // synthetic ILCodeVersionHandle
            { ILCodeVersionHandle.CreateSynthetic(new TargetPointer(/* arbitrary */ 0x100), /* arbitrary */ 100), RejitState.Active },
            { mockRejit.AddExplicitILCodeVersion(new TargetNUInt(1), MockReJIT.RejitFlags.kStateActive), RejitState.Active },
            { mockRejit.AddExplicitILCodeVersion(new TargetNUInt(2), MockReJIT.RejitFlags.kStateRequested), RejitState.Requested },
            { mockRejit.AddExplicitILCodeVersion(new TargetNUInt(3), MockReJIT.RejitFlags.kSuppressParams | MockReJIT.RejitFlags.kStateRequested), RejitState.Requested }
        };

        var target = CreateTarget(arch, mockRejit);

        // TEST

        var rejit = target.Contracts.ReJIT;
        Assert.NotNull(rejit);

        foreach (var (ilCodeVersionHandle, expectedRejitState) in expectedRejitStates)
        {
            RejitState rejitState = rejit.GetRejitState(ilCodeVersionHandle);
            Assert.Equal(expectedRejitState, rejitState);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRejitIds_SyntheticAndExplicit_Success(MockTarget.Architecture arch)
    {
        MockReJIT mockRejit = new MockReJIT(arch);
        Mock<ICodeVersions> mockCodeVersions = new Mock<ICodeVersions>();

        List<ulong> expectedRejitIds = [0, 1];
        expectedRejitIds.Sort();

        List<ILCodeVersionHandle> ilCodeVersionHandles =
        [
            // synthetic ILCodeVersionHandle
            ILCodeVersionHandle.CreateSynthetic(new TargetPointer(/* arbitrary */ 0x100), /* arbitrary */ 100),
            mockRejit.AddExplicitILCodeVersion(new TargetNUInt(1), MockReJIT.RejitFlags.kStateActive),
            mockRejit.AddExplicitILCodeVersion(new TargetNUInt(2), MockReJIT.RejitFlags.kStateRequested)
        ];

        TargetPointer methodDesc = new TargetPointer(/* arbitrary */ 0x200);
        mockCodeVersions.Setup(cv => cv.GetILCodeVersions(methodDesc))
            .Returns(ilCodeVersionHandles);
        var target = CreateTarget(arch, mockRejit, mockCodeVersions);

        // TEST

        var rejit = target.Contracts.ReJIT;
        Assert.NotNull(rejit);

        List<ulong> rejitIds = rejit.GetRejitIds(target, methodDesc)
            .Select(e => e.Value)
            .ToList();
        rejitIds.Sort();

        Assert.Equal(expectedRejitIds, rejitIds);
    }
}
