// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataProcessAppDomainTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumAppDomains_UsesSingleElementEnumeration(MockTarget.Architecture arch)
    {
        TargetPointer appDomainAddress = new(0x4000);
        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetAppDomain()).Returns(appDomainAddress);

        IXCLRDataProcess process = CreateProcess(arch, loader);
        ulong handle;
        Assert.Equal(HResults.S_OK, process.StartEnumAppDomains(&handle));
        Assert.NotEqual(0u, handle);
        ulong enumHandle = handle;

        Assert.Equal(
            HResults.E_POINTER,
            process.EnumAppDomain(&handle, new DacComNullableByRef<IXCLRDataAppDomain>(isNullRef: true)));
        Assert.Equal(enumHandle, handle);

        DacComNullableByRef<IXCLRDataAppDomain> appDomainOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, process.EnumAppDomain(&handle, appDomainOut));
        Assert.Equal(enumHandle, handle);
        IXCLRDataAppDomain appDomain = Assert.IsType<ClrDataAppDomain>(appDomainOut.Interface);
        ulong id;
        Assert.Equal(HResults.S_OK, appDomain.GetUniqueID(&id));
        Assert.Equal(1u, id);

        DacComNullableByRef<IXCLRDataAppDomain> exhaustedOut = new(isNullRef: false);
        Assert.Equal(HResults.S_FALSE, process.EnumAppDomain(&handle, exhaustedOut));
        Assert.Equal(enumHandle, handle);
        Assert.Equal(HResults.S_FALSE, process.EnumAppDomain(&handle, new DacComNullableByRef<IXCLRDataAppDomain>(isNullRef: true)));
        Assert.Equal(HResults.S_OK, process.EndEnumAppDomains(handle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetAppDomainByUniqueID_AcceptsOnlyOne(MockTarget.Architecture arch)
    {
        TargetPointer appDomainAddress = new(0x4000);
        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetAppDomain()).Returns(appDomainAddress);

        IXCLRDataProcess process = CreateProcess(arch, loader);
        DacComNullableByRef<IXCLRDataAppDomain> appDomainOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, process.GetAppDomainByUniqueID(1, appDomainOut));
        Assert.NotNull(appDomainOut.Interface);

        Assert.Equal(
            HResults.E_INVALIDARG,
            process.GetAppDomainByUniqueID(2, new DacComNullableByRef<IXCLRDataAppDomain>(isNullRef: false)));
        Assert.Equal(
            HResults.E_INVALIDARG,
            process.GetAppDomainByUniqueID(2, new DacComNullableByRef<IXCLRDataAppDomain>(isNullRef: true)));
    }

    private static IXCLRDataProcess CreateProcess(MockTarget.Architecture arch, Mock<ILoader> loader)
    {
        var builder = new TestPlaceholderTarget.Builder(arch)
            .UseReader((ulong _, Span<byte> _) => -1);
        builder.AddMockContract(loader);
        return new SOSDacImpl(builder.Build(), legacyObj: null);
    }
}
