// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class ReJITTests
{
    private readonly record struct ReJITContractContext(IReJIT ReJIT, Target Target);

    internal static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockReJITBuilder rejitBuilder)
        => new()
        {
            [DataType.ProfControlBlock] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.ProfControlBlockLayout),
            [DataType.MethodDescVersioningState] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.MethodDescVersioningStateLayout),
            [DataType.NativeCodeVersionNode] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.NativeCodeVersionNodeLayout),
            [DataType.ILCodeVersioningState] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.ILCodeVersioningStateLayout),
            [DataType.ILCodeVersionNode] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.ILCodeVersionNodeLayout),
            [DataType.InstrumentedILOffsetMapping] = TargetTestHelpers.CreateTypeInfo(rejitBuilder.InstrumentedILOffsetMappingLayout),
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
            .AddContract<IReJIT>(version: "c1")
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
    public void GetRejitState_GettingReJITParameters_IsReportedNotThrown(MockTarget.Architecture arch)
    {
        // RejitFlags::kStateGettingReJITParameters: ReJIT parameter configuration is in progress.
        ILCodeVersionHandle handle = default;
        ReJITContractContext context = CreateReJITContract(arch, rejitBuilder =>
        {
            handle = ILCodeVersionHandle.CreateExplicit(
                rejitBuilder.AddExplicitILCodeVersionNode(1, MockReJITBuilder.RejitFlags.kStateGettingReJITParameters).Address);
        });

        Assert.Equal(RejitState.GettingReJITParameters, context.ReJIT.GetRejitState(handle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRejitIds_SkipsVersionsStillGettingReJITParameters(MockTarget.Architecture arch)
    {
        Mock<ICodeVersions> codeVersions = new();
        List<ILCodeVersionHandle> handles = [];
        ReJITContractContext context = CreateReJITContract(arch, rejitBuilder =>
        {
            handles.Add(ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(1, MockReJITBuilder.RejitFlags.kStateActive).Address));
            handles.Add(ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(2, MockReJITBuilder.RejitFlags.kStateGettingReJITParameters).Address));
        }, mockCodeVersions: codeVersions);
        TargetPointer methodDesc = new(0x0101_aaa0);
        codeVersions.Setup(cv => cv.GetILCodeVersions(methodDesc)).Returns(handles);
        foreach (ILCodeVersionHandle handle in handles)
            codeVersions.Setup(cv => cv.GetSource(handle)).Returns(CodeVersionSource.ReJIT);

        Assert.Equal([new TargetNUInt(1)], context.ReJIT.GetRejitIds(context.Target, methodDesc).ToList());
    }

    // Every value of the state nibble (RejitFlags::kStateMask), with the flag bits above it (kSuppressParams and
    // seeded arbitrary bits): the defined states map regardless of the flags, any other nibble value is rejected.
    public static IEnumerable<object[]> StateNibbleCases()
    {
        Random random = new(135450);
        uint[] flagBits = [0, (uint)MockReJITBuilder.RejitFlags.kSuppressParams, (uint)random.Next() & ~0xFu, (uint)random.Next() & ~0xFu];
        foreach (object[] arch in new MockTarget.StdArch())
        {
            foreach (uint flags in flagBits)
            {
                for (uint state = 0; state <= 0xF; state++)
                {
                    yield return [arch[0], state | flags];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(StateNibbleCases))]
    public void GetRejitState_MapsTheStateNibbleAndIgnoresFlagBits(MockTarget.Architecture arch, uint rawRejitState)
    {
        ILCodeVersionHandle handle = default;
        ReJITContractContext context = CreateReJITContract(arch, rejitBuilder =>
        {
            handle = ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(1, (MockReJITBuilder.RejitFlags)rawRejitState).Address);
        });

        RejitState? expected = (rawRejitState & 0xF) switch
        {
            0 => RejitState.Requested,
            1 => RejitState.GettingReJITParameters,
            2 => RejitState.Active,
            _ => null,
        };
        if (expected is RejitState state)
        {
            Assert.Equal(state, context.ReJIT.GetRejitState(handle));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => context.ReJIT.GetRejitState(handle));
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRejitIds_SyntheticAndExplicit_Success(MockTarget.Architecture arch)
    {
        Mock<ICodeVersions> mockCodeVersions = new Mock<ICodeVersions>();

        // Only explicit ReJIT versions that are active are returned. The synthetic default version
        // (id 0) and requested-but-not-active versions are excluded (matches native GetReJITIDs).
        List<ulong> expectedRejitIds = [1];
        expectedRejitIds.Sort();

        List<ILCodeVersionHandle> ilCodeVersionHandles =
        [
            // synthetic ILCodeVersionHandle
            ILCodeVersionHandle.CreateSynthetic(new TargetPointer(/* arbitrary */ 0x100), /* arbitrary */ 100),
        ];

        TargetPointer methodDesc = new TargetPointer(/* arbitrary */ 0x200);
        mockCodeVersions.Setup(cv => cv.GetILCodeVersions(methodDesc))
            .Returns(ilCodeVersionHandles);
        // Explicit versions in this test are ReJIT versions; the synthetic version is not.
        mockCodeVersions.Setup(cv => cv.GetSource(It.IsAny<ILCodeVersionHandle>()))
            .Returns((ILCodeVersionHandle h) => h.IsExplicit ? CodeVersionSource.ReJIT : CodeVersionSource.Unknown);
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

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsDeoptimized_Synthetic(MockTarget.Architecture arch)
    {
        ReJITContractContext context = CreateReJITContract(arch, _ => { });
        ILCodeVersionHandle synthetic = ILCodeVersionHandle.CreateSynthetic(new TargetPointer(0x100), 100);
        Assert.False(context.ReJIT.IsDeoptimized(synthetic));
    }

    public static IEnumerable<object[]> ArchWithDeoptimized()
    {
        foreach (object[] stdArch in new MockTarget.StdArch())
        {
            yield return [stdArch[0], true];
            yield return [stdArch[0], false];
        }
    }

    [Theory]
    [MemberData(nameof(ArchWithDeoptimized))]
    public void IsDeoptimized_Explicit(MockTarget.Architecture arch, bool deoptimized)
    {
        ILCodeVersionHandle explicitHandle = ILCodeVersionHandle.Invalid;
        ReJITContractContext context = CreateReJITContract(
            arch,
            rejitBuilder =>
            {
                var node = rejitBuilder.AddExplicitILCodeVersionNode(1, MockReJITBuilder.RejitFlags.kStateActive, deoptimized: deoptimized);
                explicitHandle = ILCodeVersionHandle.CreateExplicit(node.Address);
            });
        Assert.Equal(deoptimized, context.ReJIT.IsDeoptimized(explicitHandle));
    }
}
