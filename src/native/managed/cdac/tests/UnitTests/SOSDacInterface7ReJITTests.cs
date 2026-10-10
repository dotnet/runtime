// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

// The SOS ReJIT queries over the real ReJIT contract. The legacy DAC reports an IL code version whose ReJIT is still
// in progress (RejitFlags::kStateGettingReJITParameters) as kUnknown and succeeds (src/coreclr/debug/daccess/request.cpp,
// GetReJITInformation and CopyNativeCodeVersionToReJitData).
public unsafe class SOSDacInterface7ReJITTests
{
    private const ulong MethodDescAddress = 0x0101_aaa0;
    private const ulong ILAddress = 0x0202_bbb0;
    private const int RejitId = 7;
    private const int S_OK = 0;
    private const int S_FALSE = 1;

    public static IEnumerable<object[]> StateCases()
    {
        foreach (object[] arch in new MockTarget.StdArch())
        {
            yield return [arch[0], (uint)MockReJITBuilder.RejitFlags.kStateRequested, DacpReJitData2.Flags.kRequested];
            yield return [arch[0], (uint)MockReJITBuilder.RejitFlags.kStateGettingReJITParameters, DacpReJitData2.Flags.kUnknown];
            yield return [arch[0], (uint)MockReJITBuilder.RejitFlags.kStateActive, DacpReJitData2.Flags.kActive];
        }
    }

    [Theory]
    [MemberData(nameof(StateCases))]
    public void GetReJITInformation_ReportsEachNativeState(MockTarget.Architecture arch, uint state, DacpReJitData2.Flags expected)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockReJITBuilder rejitBuilder = new(targetBuilder.MemoryBuilder, rejitOnAttachEnabled: true);
        ILCodeVersionHandle ilCodeVersion = ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(RejitId, (MockReJITBuilder.RejitFlags)state).Address);

        Mock<ICodeVersions> codeVersions = new();
        codeVersions.Setup(cv => cv.GetILCodeVersions(new TargetPointer(MethodDescAddress))).Returns([ilCodeVersion]);
        codeVersions.Setup(cv => cv.GetSource(ilCodeVersion)).Returns(CodeVersionSource.ReJIT);
        codeVersions.Setup(cv => cv.GetIL(ilCodeVersion)).Returns(new TargetPointer(ILAddress));

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(ReJITTests.CreateContractTypes(rejitBuilder))
            .AddGlobals((nameof(Constants.Globals.ProfilerControlBlock), rejitBuilder.ProfilerControlBlockGlobalAddress))
            .AddContract<IReJIT>(version: "c1")
            .AddMockContract(codeVersions)
            .Build();
        ISOSDacInterface7 sos = new SOSDacImpl(target, legacyObj: null, new());

        DacpReJitData2 data = default;
        int hr = sos.GetReJITInformation((ClrDataAddress)MethodDescAddress, RejitId, &data);

        Assert.Equal(S_OK, hr);
        Assert.Equal(expected, data.flags);
        Assert.Equal((uint)RejitId, data.rejitID);
    }

    public static IEnumerable<object[]> PendingCases()
    {
        foreach (object[] arch in new MockTarget.StdArch())
        {
            yield return [arch[0], (uint)MockReJITBuilder.RejitFlags.kStateRequested, S_OK];
            yield return [arch[0], (uint)MockReJITBuilder.RejitFlags.kStateGettingReJITParameters, S_FALSE];
            yield return [arch[0], (uint)MockReJITBuilder.RejitFlags.kStateActive, S_FALSE];
        }
    }

    // The legacy DAC reports a pending ReJIT ID only for kStateRequested and returns S_FALSE otherwise
    // (src/coreclr/debug/daccess/request.cpp, GetPendingReJITID).
    [Theory]
    [MemberData(nameof(PendingCases))]
    public void GetPendingReJITID_ReportsOnlyRequestedVersions(MockTarget.Architecture arch, uint state, int expectedHr)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockReJITBuilder rejitBuilder = new(targetBuilder.MemoryBuilder, rejitOnAttachEnabled: true);
        ILCodeVersionHandle ilCodeVersion = ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(RejitId, (MockReJITBuilder.RejitFlags)state).Address);

        Mock<ICodeVersions> codeVersions = new();
        codeVersions.Setup(cv => cv.GetActiveILCodeVersion(new TargetPointer(MethodDescAddress))).Returns(ilCodeVersion);
        codeVersions.Setup(cv => cv.GetSource(ilCodeVersion)).Returns(CodeVersionSource.ReJIT);

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(ReJITTests.CreateContractTypes(rejitBuilder))
            .AddGlobals((nameof(Constants.Globals.ProfilerControlBlock), rejitBuilder.ProfilerControlBlockGlobalAddress))
            .AddContract<IReJIT>(version: "c1")
            .AddMockContract(codeVersions)
            .Build();
        ISOSDacInterface7 sos = new SOSDacImpl(target, legacyObj: null, new());

        int pendingId = 0;
        int hr = sos.GetPendingReJITID((ClrDataAddress)MethodDescAddress, &pendingId);

        Assert.Equal(expectedHr, hr);
        if (expectedHr == S_OK)
        {
            Assert.Equal(RejitId, pendingId);
        }
    }

    public static IEnumerable<object[]> MethodDescStateCases()
    {
        foreach (object[] arch in new MockTarget.StdArch())
        {
            yield return [arch[0], (uint)MockReJITBuilder.RejitFlags.kStateRequested, DacpReJitData.Flags.kRequested];
            yield return [arch[0], (uint)MockReJITBuilder.RejitFlags.kStateGettingReJITParameters, DacpReJitData.Flags.kUnknown];
            yield return [arch[0], (uint)MockReJITBuilder.RejitFlags.kStateActive, DacpReJitData.Flags.kActive];
        }
    }

    // GetMethodDescData fills its ReJIT data through CopyNativeCodeVersionToReJitData. For the active native code
    // version of a ReJIT IL version, the legacy DAC reports the state as kUnknown when it is neither Requested nor
    // Active (src/coreclr/debug/daccess/request.cpp, CopyNativeCodeVersionToReJitData).
    [Theory]
    [MemberData(nameof(MethodDescStateCases))]
    public void CopyNativeCodeVersionToReJitData_ReportsEachNativeState(MockTarget.Architecture arch, uint state, DacpReJitData.Flags expected)
    {
        const ulong NativeCodeVersionNodeAddress = 0x0303_ccc0;
        const ulong NativeCodeAddress = 0x0404_ddd0;
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockReJITBuilder rejitBuilder = new(targetBuilder.MemoryBuilder, rejitOnAttachEnabled: true);
        ILCodeVersionHandle ilCodeVersion = ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(RejitId, (MockReJITBuilder.RejitFlags)state).Address);
        NativeCodeVersionHandle nativeCodeVersion = NativeCodeVersionHandle.CreateExplicit(new TargetPointer(NativeCodeVersionNodeAddress));

        Mock<ICodeVersions> codeVersions = new();
        codeVersions.Setup(cv => cv.GetILCodeVersion(nativeCodeVersion)).Returns(ilCodeVersion);
        codeVersions.Setup(cv => cv.GetNativeCode(nativeCodeVersion)).Returns(new TargetCodePointer(NativeCodeAddress));
        codeVersions.Setup(cv => cv.GetSource(ilCodeVersion)).Returns(CodeVersionSource.ReJIT);
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(em => em.GetDiagnosticCodeStartFromEntryPoint(new TargetCodePointer(NativeCodeAddress))).Returns(new TargetCodePointer(NativeCodeAddress));

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(ReJITTests.CreateContractTypes(rejitBuilder))
            .AddGlobals((nameof(Constants.Globals.ProfilerControlBlock), rejitBuilder.ProfilerControlBlockGlobalAddress))
            .AddContract<IReJIT>(version: "c1")
            .AddMockContract(codeVersions)
            .AddMockContract(executionManager)
            .Build();
        SOSDacImpl sos = new(target, legacyObj: null, new());

        DacpReJitData data = default;
        sos.CopyNativeCodeVersionToReJitData(nativeCodeVersion, nativeCodeVersion, &data);

        Assert.Equal(expected, data.flags);
        Assert.Equal((ClrDataAddress)(ulong)RejitId, data.rejitID);
        Assert.Equal((ClrDataAddress)NativeCodeAddress, data.NativeCodeAddr);
    }
}
