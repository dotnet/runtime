// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class SOSDacInterface7ReJITTests
{
    private const ulong MethodDescAddress = 0x0101_aaa0;
    private const ulong ILAddress = 0x0202_bbb0;
    private const int RejitId = 7;
    private const int S_OK = 0;

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetReJITInformation_GettingReJITParameters_ReportsUnknown(MockTarget.Architecture arch)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockReJITBuilder rejitBuilder = new(targetBuilder.MemoryBuilder, rejitOnAttachEnabled: true);
        ILCodeVersionHandle ilCodeVersion = ILCodeVersionHandle.CreateExplicit(rejitBuilder.AddExplicitILCodeVersionNode(RejitId, MockReJITBuilder.RejitFlags.kStateGettingReJITParameters).Address);

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
        Assert.Equal(DacpReJitData2.Flags.kUnknown, data.flags);
        Assert.Equal((uint)RejitId, data.rejitID);
    }
}
