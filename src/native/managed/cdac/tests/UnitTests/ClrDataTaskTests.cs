// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataTaskTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentAppDomain(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);

        ulong globalPtrAddr = 0x1000;
        ulong expectedAppDomain = 0x2000;

        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        byte[] ptrData = new byte[helpers.PointerSize];
        helpers.WritePointer(ptrData, expectedAppDomain);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = globalPtrAddr,
            Data = ptrData,
            Name = "AppDomainGlobalPointer"
        });

        var target = targetBuilder
            .AddGlobals((Constants.Globals.AppDomain, globalPtrAddr))
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
