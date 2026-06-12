// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataTaskTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentAppDomain(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);

        ulong appDomainGlobalPtrAddr = 0x1000;
        ulong systemDomainGlobalPtrAddr = 0x1100;
        ulong expectedAppDomain = 0x2000;
        ulong expectedSystemDomain = 0x3000;

        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        byte[] appDomainPtrData = new byte[helpers.PointerSize];
        helpers.WritePointer(appDomainPtrData, expectedAppDomain);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = appDomainGlobalPtrAddr,
            Data = appDomainPtrData,
            Name = "AppDomainGlobalPointer"
        });
        byte[] systemDomainPtrData = new byte[helpers.PointerSize];
        helpers.WritePointer(systemDomainPtrData, expectedSystemDomain);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = systemDomainGlobalPtrAddr,
            Data = systemDomainPtrData,
            Name = "SystemDomainGlobalPointer"
        });

        var target = targetBuilder
            .AddGlobals(
                (Constants.Globals.AppDomain, appDomainGlobalPtrAddr),
                (Constants.Globals.SystemDomain, systemDomainGlobalPtrAddr),
                (Constants.Globals.DefaultADID, 1ul))
            .AddContract<Contracts.ILoader>("c1")
            .Build();

        TargetPointer taskAddress = new TargetPointer(0x5000);
        IXCLRDataTask task = new ClrDataTask(taskAddress, target, legacyImpl: null);
        DacComNullableByRef<IXCLRDataAppDomain> appDomain = new(isNullRef: false);
        int hr = task.GetCurrentAppDomain(appDomain);

        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(appDomain.Interface);
        ClrDataAppDomain clrAppDomain = Assert.IsType<ClrDataAppDomain>(appDomain.Interface);
        Assert.Equal(new TargetPointer(expectedAppDomain), clrAppDomain.Address);
    }
}
