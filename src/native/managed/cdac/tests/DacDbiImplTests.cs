// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class DacDbiImplTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetAssemblyInfo(MockTarget.Architecture arch)
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

        DacDbiImpl dbi = new DacDbiImpl(target, legacyObj: null);

        ulong vmAssembly = 0x3000;
        DacDbiAssemblyInfo info;
        int hr = dbi.GetAssemblyInfo(vmAssembly, &info);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(vmAssembly, info.vmAssembly);
        Assert.Equal(expectedAppDomain, info.vmAppDomain);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetAssemblyInfo_ZeroAssembly(MockTarget.Architecture arch)
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

        DacDbiImpl dbi = new DacDbiImpl(target, legacyObj: null);

        DacDbiAssemblyInfo info;
        int hr = dbi.GetAssemblyInfo(0, &info);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0UL, info.vmAssembly);
        Assert.Equal(expectedAppDomain, info.vmAppDomain);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetAssemblyInfo_MissingGlobal(MockTarget.Architecture arch)
    {
        var target = new TestPlaceholderTarget.Builder(arch).Build();

        DacDbiImpl dbi = new DacDbiImpl(target, legacyObj: null);

        DacDbiAssemblyInfo info;
        int hr = dbi.GetAssemblyInfo(0x3000, &info);

        Assert.NotEqual(HResults.S_OK, hr);
    }
}
