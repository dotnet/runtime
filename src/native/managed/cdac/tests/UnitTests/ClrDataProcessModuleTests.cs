// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataProcessModuleTests
{
    private static readonly MockTarget.Architecture s_arch = new() { IsLittleEndian = true, Is64Bit = true };

    [Fact]
    public void EnumModules_UsesLoadedExecutionOrderAndExhausts()
    {
        TargetPointer appDomain = new(0x4000);
        ModuleHandle firstHandle = new(new TargetPointer(0x5000));
        ModuleHandle secondHandle = new(new TargetPointer(0x6000));
        TargetPointer firstModule = new(0x7000);
        TargetPointer secondModule = new(0x8000);
        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetAppDomain()).Returns(appDomain);
        loader.Setup(l => l.GetModuleHandles(
                appDomain,
                AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution))
            .Returns([firstHandle, secondHandle]);
        loader.Setup(l => l.GetModule(firstHandle)).Returns(firstModule);
        loader.Setup(l => l.GetModule(secondHandle)).Returns(secondModule);

        IXCLRDataProcess process = CreateProcess(loader);
        ulong handle;
        Assert.Equal(HResults.S_OK, process.StartEnumModules(&handle));
        Assert.NotEqual(0u, handle);

        DacComNullableByRef<IXCLRDataModule> moduleOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, process.EnumModule(&handle, moduleOut));
        Assert.Equal(firstModule, Assert.IsType<ClrDataModule>(moduleOut.Interface).Address);

        moduleOut = new DacComNullableByRef<IXCLRDataModule>(isNullRef: false);
        Assert.Equal(HResults.S_OK, process.EnumModule(&handle, moduleOut));
        Assert.Equal(secondModule, Assert.IsType<ClrDataModule>(moduleOut.Interface).Address);

        moduleOut = new DacComNullableByRef<IXCLRDataModule>(isNullRef: false);
        Assert.Equal(HResults.S_FALSE, process.EnumModule(&handle, moduleOut));
        Assert.Null(moduleOut.Interface);
        Assert.Equal(
            HResults.S_FALSE,
            process.EnumModule(&handle, new DacComNullableByRef<IXCLRDataModule>(isNullRef: true)));
        Assert.Equal(HResults.S_OK, process.EndEnumModules(handle));
    }

    [Fact]
    public void GetModuleByAddress_SkipsNoImageAndUsesInteriorRange()
    {
        TargetPointer appDomain = new(0x4000);
        ModuleHandle noImageHandle = new(new TargetPointer(0x5000));
        ModuleHandle imageHandle = new(new TargetPointer(0x6000));
        TargetPointer imageModule = new(0x7000);
        TargetPointer noImageBase = TargetPointer.Null;
        uint noImageSize = 0;
        uint noImageFlags = 0;
        TargetPointer imageBase = new(0x1000_0000);
        uint imageSize = 0x2000;
        uint imageFlags = 0;

        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetAppDomain()).Returns(appDomain);
        loader.Setup(l => l.GetModuleHandles(
                appDomain,
                AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution))
            .Returns([noImageHandle, imageHandle]);
        loader.Setup(l => l.TryGetLoadedImageContents(noImageHandle, out noImageBase, out noImageSize, out noImageFlags))
            .Returns(false);
        loader.Setup(l => l.TryGetLoadedImageContents(imageHandle, out imageBase, out imageSize, out imageFlags))
            .Returns(true);
        loader.Setup(l => l.GetModule(imageHandle)).Returns(imageModule);

        IXCLRDataProcess process = CreateProcess(loader);
        DacComNullableByRef<IXCLRDataModule> moduleOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, process.GetModuleByAddress(new ClrDataAddress(imageBase.Value + 0x100), moduleOut));
        Assert.Equal(imageModule, Assert.IsType<ClrDataModule>(moduleOut.Interface).Address);

        moduleOut = new DacComNullableByRef<IXCLRDataModule>(isNullRef: false);
        Assert.Equal(HResults.S_FALSE, process.GetModuleByAddress(new ClrDataAddress(imageBase.Value + imageSize), moduleOut));
        Assert.Null(moduleOut.Interface);
    }

    private static IXCLRDataProcess CreateProcess(Mock<ILoader> loader)
    {
        var builder = new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1);
        builder.AddMockContract(loader);
        return new SOSDacImpl(builder.Build(), legacyObj: null);
    }
}
