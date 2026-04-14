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

public class ReJITTests
{
    private readonly record struct ReJITContractContext(IReJIT ReJIT, Target Target);

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockReJITBuilder rejitBuilder)
        => new()
        {
            [DataType.ProfControlBlock] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.ProfControlBlockLayout),
            [DataType.MethodDescVersioningState] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.MethodDescVersioningStateLayout),
            [DataType.NativeCodeVersionNode] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.NativeCodeVersionNodeLayout),
            [DataType.ILCodeVersioningState] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.ILCodeVersioningStateLayout),
            [DataType.ILCodeVersionNode] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.ILCodeVersionNodeLayout),
            [DataType.GCCoverageInfo] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.GCCoverageInfoLayout),
        };

    private static ReJITContractContext CreateReJITContract(
        MockTarget.Architecture arch,
        Action<MockReJITBuilder> configure,
        bool rejitOnAttachEnabled = true,
        Mock<ICodeVersions>? mockCodeVersions = null)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockReJITBuilder rejitBuilder = new(targetBuilder.MemoryBuilder, rejitOnAttachEnabled);
        configure(rejitBuilder);
        mockCodeVersions ??= new Mock<ICodeVersions>();

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(CreateContractTypes(rejitBuilder))
            .AddGlobals((nameof(Constants.Globals.ProfilerControlBlock), rejitBuilder.ProfilerControlBlockGlobalAddress))
            .AddContract<IReJIT>(version: 1)
            .AddMockContract(mockCodeVersions)
            .Build();
        return new ReJITContractContext(target.Contracts.ReJIT, target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsEnabled_RejitOnAttachEnabled_ReturnsTrue(MockTarget.Architecture arch)
    {
        ReJITContractContext context = CreateReJITContract(
            arch,
            _ => { },
            rejitOnAttachEnabled: true,
            mockCodeVersions: new Mock<ICodeVersions>());
        Assert.True(context.ReJIT.IsEnabled());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsEnabled_RejitOnAttachDisabled_NoProfiler_ReturnsFalse(MockTarget.Architecture arch)
    {
        ReJITContractContext context = CreateReJITContract(
            arch,
            _ => { },
            rejitOnAttachEnabled: false);
        Assert.False(context.ReJIT.IsEnabled());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRejitId_SyntheticAndExplicit_Success(MockTarget.Architecture arch)
    {
        Dictionary<ILCodeVersionHandle, TargetNUInt> expectedRejitIds = new()
        {
            // synthetic ILCodeVersionHandle
            { ILCodeVersionHandle.CreateSynthetic(new TargetPointer(/* arbitrary */ 0x100), /* arbitrary */ 100), new TargetNUInt(0) },
        };

        ReJITContractContext context = CreateReJITContract(
            arch,
            rejitBuilder =>
            {
                expectedRejitIds.Add(ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(1, MockReJITBuilder.RejitFlags.kStateActive).Address), new TargetNUInt(1));
                expectedRejitIds.Add(ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(2, MockReJITBuilder.RejitFlags.kStateRequested).Address), new TargetNUInt(2));
                expectedRejitIds.Add(ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(3, MockReJITBuilder.RejitFlags.kStateRequested).Address), new TargetNUInt(3));
            });

        foreach (var (ilCodeVersionHandle, expectedRejitId) in expectedRejitIds)
        {
            TargetNUInt rejitState = context.ReJIT.GetRejitId(ilCodeVersionHandle);
            Assert.Equal(expectedRejitId, rejitState);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRejitState_SyntheticAndExplicit_Success(MockTarget.Architecture arch)
    {
        Dictionary<ILCodeVersionHandle, RejitState> expectedRejitStates = new()
        {
            // synthetic ILCodeVersionHandle
            { ILCodeVersionHandle.CreateSynthetic(new TargetPointer(/* arbitrary */ 0x100), /* arbitrary */ 100), RejitState.Active },
        };

        ReJITContractContext context = CreateReJITContract(
            arch,
            rejitBuilder =>
            {
                expectedRejitStates.Add(ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(1, MockReJITBuilder.RejitFlags.kStateActive).Address), RejitState.Active);
                expectedRejitStates.Add(ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(2, MockReJITBuilder.RejitFlags.kStateRequested).Address), RejitState.Requested);
            });

        foreach (var (ilCodeVersionHandle, expectedRejitState) in expectedRejitStates)
        {
            RejitState rejitState = context.ReJIT.GetRejitState(ilCodeVersionHandle);
            Assert.Equal(expectedRejitState, rejitState);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRejitIds_SyntheticAndExplicit_Success(MockTarget.Architecture arch)
    {
        Mock<ICodeVersions> mockCodeVersions = new Mock<ICodeVersions>();

        List<ulong> expectedRejitIds = [0, 1];
        expectedRejitIds.Sort();

        List<ILCodeVersionHandle> ilCodeVersionHandles =
        [
            // synthetic ILCodeVersionHandle
            ILCodeVersionHandle.CreateSynthetic(new TargetPointer(/* arbitrary */ 0x100), /* arbitrary */ 100),
        ];

        TargetPointer methodDesc = new TargetPointer(/* arbitrary */ 0x200);
        mockCodeVersions.Setup(cv => cv.GetILCodeVersions(methodDesc))
            .Returns(ilCodeVersionHandles);
        ReJITContractContext context = CreateReJITContract(
            arch,
            rejitBuilder =>
            {
                ilCodeVersionHandles.Add(ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(1, MockReJITBuilder.RejitFlags.kStateActive).Address));
                ilCodeVersionHandles.Add(ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(2, MockReJITBuilder.RejitFlags.kStateRequested).Address));
            },
            mockCodeVersions: mockCodeVersions);

        List<ulong> rejitIds = context.ReJIT.GetRejitIds(context.Target, methodDesc)
            .Select(e => e.Value)
            .ToList();
        rejitIds.Sort();

        Assert.Equal(expectedRejitIds, rejitIds);
    }
}
