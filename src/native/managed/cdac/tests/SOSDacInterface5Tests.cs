// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class SOSDacInterface5Tests
{
    private const int S_OK = 0;
    private const int S_FALSE = 1;

    private static readonly TargetPointer s_methodDescAddr = new(0x1000_0000);
    private static readonly TargetPointer s_moduleAddr = new(0x2000_0000);
    private static readonly TargetPointer s_methodTableAddr = new(0x3000_0000);

    private record struct VersionInfo(
        TargetCodePointer NativeCode,
        TargetPointer CodeVersionNodeAddress,
        OptimizationTier Tier);

    private static ISOSDacInterface5 CreateDac5(
        MockTarget.Architecture arch,
        VersionInfo[]? versions = null,
        bool isEligibleForTieredCompilation = false,
        bool isReadyToRun = false,
        TargetPointer r2rBase = default,
        uint r2rSize = 0,
        int rejitId = 0)
    {
        var mockCodeVersions = new Mock<ICodeVersions>();
        var mockRts = new Mock<IRuntimeTypeSystem>();
        var mockLoader = new Mock<ILoader>();
        var mockReJIT = new Mock<IReJIT>();

        ILCodeVersionHandle ilCodeVersion = ILCodeVersionHandle.CreateSynthetic(s_moduleAddr, 0x06000001);
        MethodDescHandle methodDescHandle = new MethodDescHandle(s_methodDescAddr);
        TypeHandle typeHandle = new TypeHandle(s_methodTableAddr);
        Contracts.ModuleHandle moduleHandle = new Contracts.ModuleHandle(s_moduleAddr);

        mockCodeVersions
            .Setup(c => c.GetILCodeVersions(s_methodDescAddr))
            .Returns(new[] { ilCodeVersion });

        mockReJIT
            .Setup(r => r.GetRejitId(It.IsAny<ILCodeVersionHandle>()))
            .Returns(new TargetNUInt((ulong)rejitId));

        mockRts
            .Setup(r => r.GetMethodDescHandle(s_methodDescAddr))
            .Returns(methodDescHandle);
        mockRts
            .Setup(r => r.GetMethodTable(methodDescHandle))
            .Returns(s_methodTableAddr);
        mockRts
            .Setup(r => r.GetTypeHandle(s_methodTableAddr))
            .Returns(typeHandle);
        mockRts
            .Setup(r => r.GetModule(typeHandle))
            .Returns(s_moduleAddr);
        mockRts
            .Setup(r => r.IsEligibleForTieredCompilation(methodDescHandle))
            .Returns(isEligibleForTieredCompilation);

        mockLoader
            .Setup(l => l.GetModuleHandleFromModulePtr(s_moduleAddr))
            .Returns(moduleHandle);
        mockLoader
            .Setup(l => l.IsReadyToRun(moduleHandle))
            .Returns(isReadyToRun);
        if (isReadyToRun)
        {
            uint imageFlags = 0;
            TargetPointer outBase = r2rBase;
            mockLoader
                .Setup(l => l.TryGetLoadedImageContents(moduleHandle, out outBase, out r2rSize, out imageFlags))
                .Returns(true);
        }

        versions ??= [];

        var nativeVersionHandles = new NativeCodeVersionHandle[versions.Length];
        for (int i = 0; i < versions.Length; i++)
        {
            NativeCodeVersionHandle handle = versions[i].CodeVersionNodeAddress != TargetPointer.Null
                ? NativeCodeVersionHandle.CreateExplicit(versions[i].CodeVersionNodeAddress)
                : NativeCodeVersionHandle.CreateSynthetic(s_methodDescAddr);

            nativeVersionHandles[i] = handle;

            mockCodeVersions
                .Setup(c => c.GetNativeCode(handle))
                .Returns(versions[i].NativeCode);
            mockCodeVersions
                .Setup(c => c.GetOptimizationTier(handle))
                .Returns(versions[i].Tier);
        }

        mockCodeVersions
            .Setup(c => c.GetNativeCodeVersions(s_methodDescAddr, It.IsAny<ILCodeVersionHandle>()))
            .Returns(nativeVersionHandles);

        var target = new TestPlaceholderTarget(
            arch,
            (_, _) => -1,
            types: []);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.CodeVersions == mockCodeVersions.Object
                && c.RuntimeTypeSystem == mockRts.Object
                && c.Loader == mockLoader.Object
                && c.ReJIT == mockReJIT.Object));

        return new SOSDacImpl(target, legacyObj: null);
    }

    private static int CallGetTieredVersions(ISOSDacInterface5 dac5, DacpTieredVersionData[] buffer, out int count, int rejitId = 0)
    {
        int localCount;
        int hr = dac5.GetTieredVersions((ClrDataAddress)s_methodDescAddr.Value, rejitId, buffer, buffer.Length, &localCount);
        count = localCount;
        return hr;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTieredVersions_ZeroMethodDesc(MockTarget.Architecture arch)
    {
        ISOSDacInterface5 dac5 = CreateDac5(arch);
        var buffer = new DacpTieredVersionData[1];
        int count;
        int hr = dac5.GetTieredVersions(0, 0, buffer, 1, &count);
        Assert.NotEqual(S_OK, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTieredVersions_ZeroBufferSize(MockTarget.Architecture arch)
    {
        ISOSDacInterface5 dac5 = CreateDac5(arch);
        int count;
        int hr = dac5.GetTieredVersions((ClrDataAddress)s_methodDescAddr.Value, 0, null, 0, &count);
        Assert.NotEqual(S_OK, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTieredVersions_NullOutputPtr(MockTarget.Architecture arch)
    {
        ISOSDacInterface5 dac5 = CreateDac5(arch);
        var buffer = new DacpTieredVersionData[1];
        int hr = dac5.GetTieredVersions((ClrDataAddress)s_methodDescAddr.Value, 0, buffer, 1, null);
        Assert.NotEqual(S_OK, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTieredVersions_InvalidRejitId(MockTarget.Architecture arch)
    {
        ISOSDacInterface5 dac5 = CreateDac5(arch, rejitId: 0);
        int hr = CallGetTieredVersions(dac5, new DacpTieredVersionData[1], out _, rejitId: 999);
        Assert.NotEqual(S_OK, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTieredVersions_ReadyToRun(MockTarget.Architecture arch)
    {
        var r2rBase = new TargetPointer(0x5000_0000);
        uint r2rSize = 0x1000;
        var versions = new[]
        {
            new VersionInfo(new TargetCodePointer(0x5000_0100), new TargetPointer(0x7000_0001), OptimizationTier.OptimizationTierOptimized),
        };

        ISOSDacInterface5 dac5 = CreateDac5(arch, versions, isReadyToRun: true, r2rBase: r2rBase, r2rSize: r2rSize);
        var buffer = new DacpTieredVersionData[2];
        int hr = CallGetTieredVersions(dac5, buffer, out int count);

        Assert.Equal(S_OK, hr);
        Assert.Equal(1, count);
        Assert.Equal(DacpTieredVersionData.OptimizationTier.ReadyToRun, buffer[0].optimizationTier);
    }

    public static IEnumerable<object[]> TierMappingData
    {
        get
        {
            (OptimizationTier, DacpTieredVersionData.OptimizationTier)[] tiers =
            [
                (OptimizationTier.OptimizationTier0, DacpTieredVersionData.OptimizationTier.QuickJitted),
                (OptimizationTier.OptimizationTier1, DacpTieredVersionData.OptimizationTier.OptimizedTier1),
                (OptimizationTier.OptimizationTier1OSR, DacpTieredVersionData.OptimizationTier.OptimizedTier1OSR),
                (OptimizationTier.OptimizationTierOptimized, DacpTieredVersionData.OptimizationTier.Optimized),
                (OptimizationTier.OptimizationTier0Instrumented, DacpTieredVersionData.OptimizationTier.QuickJittedInstrumented),
                (OptimizationTier.OptimizationTier1Instrumented, DacpTieredVersionData.OptimizationTier.OptimizedTier1Instrumented),
                (OptimizationTier.OptimizationTierUnknown, DacpTieredVersionData.OptimizationTier.Unknown),
            ];

            foreach (var arch in new MockTarget.StdArch())
            {
                foreach (var (internalTier, expectedTier) in tiers)
                {
                    yield return [(MockTarget.Architecture)arch[0], internalTier, expectedTier];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TierMappingData))]
    public void GetTieredVersions_TieredCompilation(
        MockTarget.Architecture arch,
        OptimizationTier internalTier,
        DacpTieredVersionData.OptimizationTier expectedTier)
    {
        var versions = new[]
        {
            new VersionInfo(new TargetCodePointer(0x6000_0100), new TargetPointer(0x7000_0001), internalTier),
        };

        ISOSDacInterface5 dac5 = CreateDac5(arch, versions, isEligibleForTieredCompilation: true);
        var buffer = new DacpTieredVersionData[2];
        int hr = CallGetTieredVersions(dac5, buffer, out int count);

        Assert.Equal(S_OK, hr);
        Assert.Equal(1, count);
        Assert.Equal(expectedTier, buffer[0].optimizationTier);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTieredVersions_NotEligibleForTieredCompilation(MockTarget.Architecture arch)
    {
        var versions = new[]
        {
            new VersionInfo(new TargetCodePointer(0x6000_0100), new TargetPointer(0x7000_0001), OptimizationTier.OptimizationTierOptimized),
        };

        ISOSDacInterface5 dac5 = CreateDac5(arch, versions);
        var buffer = new DacpTieredVersionData[2];
        int hr = CallGetTieredVersions(dac5, buffer, out int count);

        Assert.Equal(S_OK, hr);
        Assert.Equal(1, count);
        Assert.Equal(DacpTieredVersionData.OptimizationTier.Unknown, buffer[0].optimizationTier);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTieredVersions_BufferTooSmall(MockTarget.Architecture arch)
    {
        var versions = new[]
        {
            new VersionInfo(new TargetCodePointer(0x6000_0100), new TargetPointer(0x7000_0001), OptimizationTier.OptimizationTier0),
            new VersionInfo(new TargetCodePointer(0x6000_0200), new TargetPointer(0x7000_0002), OptimizationTier.OptimizationTier1),
            new VersionInfo(new TargetCodePointer(0x6000_0300), new TargetPointer(0x7000_0003), OptimizationTier.OptimizationTierOptimized),
        };

        ISOSDacInterface5 dac5 = CreateDac5(arch, versions, isEligibleForTieredCompilation: true);
        var buffer = new DacpTieredVersionData[2];
        int hr = CallGetTieredVersions(dac5, buffer, out int count);

        Assert.Equal(S_FALSE, hr);
        Assert.Equal(2, count);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTieredVersions_PopulatesCodeAddrAndNodePtr(MockTarget.Architecture arch)
    {
        var codeAddr = new TargetCodePointer(0x6000_0100);
        var nodeAddr = new TargetPointer(0x7000_0001);
        var versions = new[]
        {
            new VersionInfo(codeAddr, nodeAddr, OptimizationTier.OptimizationTierOptimized),
        };

        ISOSDacInterface5 dac5 = CreateDac5(arch, versions);
        var buffer = new DacpTieredVersionData[2];
        int hr = CallGetTieredVersions(dac5, buffer, out int count);

        Assert.Equal(S_OK, hr);
        Assert.Equal(1, count);
        Assert.Equal((ulong)codeAddr.Value, (ulong)buffer[0].nativeCodeAddr);
        Assert.Equal(nodeAddr.Value, (ulong)buffer[0].nativeCodeVersionNodePtr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTieredVersions_NoVersions(MockTarget.Architecture arch)
    {
        ISOSDacInterface5 dac5 = CreateDac5(arch, versions: []);
        int hr = CallGetTieredVersions(dac5, new DacpTieredVersionData[1], out int count);

        Assert.Equal(S_OK, hr);
        Assert.Equal(0, count);
    }
}
