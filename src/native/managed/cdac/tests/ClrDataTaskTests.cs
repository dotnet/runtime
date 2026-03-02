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
        MockMemorySpace.Builder builder = new(helpers);

        ulong globalPtrAddr = 0x1000;
        ulong expectedAppDomain = 0x2000;

        byte[] ptrData = new byte[helpers.PointerSize];
        helpers.WritePointer(ptrData, expectedAppDomain);
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = globalPtrAddr,
            Data = ptrData,
            Name = "AppDomainGlobalPointer"
        });

        var target = new TestPlaceholderTarget(
            arch,
            builder.GetMemoryContext().ReadFromTarget,
            globals: [(Constants.Globals.AppDomain, globalPtrAddr)]);

        TargetPointer taskAddress = new TargetPointer(0x5000);
        IXCLRDataTask task = new ClrDataTask(taskAddress, target, legacyImpl: null);
        int hr = task.GetCurrentAppDomain(out IXCLRDataAppDomain? appDomain);

        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(appDomain);
        Assert.IsType<ClrDataAppDomain>(appDomain);
    }
}
