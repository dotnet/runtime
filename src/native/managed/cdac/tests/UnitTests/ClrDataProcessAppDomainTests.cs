// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataProcessAppDomainTests
{
    private static readonly MockTarget.Architecture s_arch = new() { IsLittleEndian = true, Is64Bit = true };

    [Fact]
    public void EnumAppDomains_UsesSingleDomainHandleProgression()
    {
        TargetPointer appDomainAddress = new(0x4000);
        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetAppDomain()).Returns(appDomainAddress);

        IXCLRDataProcess process = CreateProcess(loader: loader);
        ulong handle;
        Assert.Equal(HResults.S_OK, process.StartEnumAppDomains(&handle));
        Assert.Equal(1u, handle);

        void* appDomainPointer = null;
        Assert.Equal(HResults.S_OK, process.EnumAppDomain(&handle, &appDomainPointer));
        Assert.Equal(0u, handle);
        Assert.NotEqual(0, (nint)appDomainPointer);
        IXCLRDataAppDomain appDomain = ComInterfaceMarshaller<IXCLRDataAppDomain>.ConvertToManaged(appDomainPointer);
        ComInterfaceMarshaller<IXCLRDataAppDomain>.Free(appDomainPointer);
        ulong id;
        Assert.Equal(HResults.S_OK, appDomain.GetUniqueID(&id));
        Assert.Equal(1u, id);

        appDomainPointer = null;
        Assert.Equal(HResults.S_FALSE, process.EnumAppDomain(&handle, &appDomainPointer));
        Assert.Equal(0, (nint)appDomainPointer);
        Assert.Equal(HResults.S_FALSE, process.EnumAppDomain(&handle, null));
        Assert.Equal(HResults.S_OK, process.EndEnumAppDomains(handle));
    }

    [Fact]
    public void GetAppDomainByUniqueID_AcceptsOnlyOne()
    {
        TargetPointer appDomainAddress = new(0x4000);
        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetAppDomain()).Returns(appDomainAddress);

        IXCLRDataProcess process = CreateProcess(loader: loader);
        void* appDomainPointer = null;
        Assert.Equal(HResults.S_OK, process.GetAppDomainByUniqueID(1, &appDomainPointer));
        Assert.NotEqual(0, (nint)appDomainPointer);
        ComInterfaceMarshaller<IXCLRDataAppDomain>.Free(appDomainPointer);

        appDomainPointer = null;
        Assert.Equal(HResults.E_INVALIDARG, process.GetAppDomainByUniqueID(2, &appDomainPointer));
        Assert.Equal(0, (nint)appDomainPointer);
        Assert.Equal(HResults.E_INVALIDARG, process.GetAppDomainByUniqueID(2, null));
    }

    private static IXCLRDataProcess CreateProcess(Mock<ILoader> loader)
    {
        var builder = new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1);
        builder.AddMockContract(loader);
        return new SOSDacImpl(builder.Build(), legacyObj: null);
    }
}
