// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class DacDbiImplTests
{
    private const uint IsEditAndContinue = 0x00000008;
    private const uint IsEncCapable = 0x00000200;
    private const uint DebuggerAllowJitOptsPriv = 0x00000800;
    private const uint DebuggerEncEnabledPriv = 0x00002000;
    private const uint DebuggerIgnorePdbsPriv = 0x00008000;

    private static (DacDbiImpl DacDbi, TestPlaceholderTarget Target) CreateDacDbiWithLoader(
        MockTarget.Architecture arch,
        Action<MockLoaderBuilder, TestPlaceholderTarget.Builder> configure)
    {
        var (_, target) = LoaderTests.CreateLoaderContractWithTarget(arch, configure);
        var dacDbi = new DacDbiImpl(target, legacyObj: null);
        return (dacDbi, target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetCompilerFlags_BothFlagsSet_EncCapable(MockTarget.Architecture arch)
    {
        ulong assemblyAddr = 0;
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (dacDbi, target) = CreateDacDbiWithLoader(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.Debug);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            var module = loader.AddModule(flags: IsEncCapable | DebuggerIgnorePdbsPriv);
            assemblyAddr = module.Assembly;
            moduleAddr = new TargetPointer(module.Address);
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        int hr = dacDbi.SetCompilerFlags(assemblyAddr, Interop.BOOL.TRUE, Interop.BOOL.TRUE);
        Assert.Equal(System.HResults.S_OK, hr);
        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.NotEqual(0u, rawFlags & DebuggerAllowJitOptsPriv);
        Assert.NotEqual(0u, rawFlags & IsEditAndContinue);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetCompilerFlags_BothFlagsUnset(MockTarget.Architecture arch)
    {
        ulong assemblyAddr = 0;
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (dacDbi, target) = CreateDacDbiWithLoader(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.None);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            var module = loader.AddModule();
            assemblyAddr = module.Assembly;
            moduleAddr = new TargetPointer(module.Address);
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        int hr = dacDbi.SetCompilerFlags(assemblyAddr, Interop.BOOL.FALSE, Interop.BOOL.FALSE);
        Assert.Equal(System.HResults.S_OK, hr);
        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.Equal(0u, rawFlags & DebuggerAllowJitOptsPriv);
        Assert.Equal(0u, rawFlags & IsEditAndContinue);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetCompilerFlags_EnCRequested_NotCapable(MockTarget.Architecture arch)
    {
        ulong assemblyAddr = 0;
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (dacDbi, target) = CreateDacDbiWithLoader(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.None);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            var module = loader.AddModule();
            assemblyAddr = module.Assembly;
            moduleAddr = new TargetPointer(module.Address);
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        int hr = dacDbi.SetCompilerFlags(assemblyAddr, Interop.BOOL.TRUE, Interop.BOOL.TRUE);
        Assert.Equal(CorDbgHResults.CORDBG_S_NOT_ALL_BITS_SET, hr);
        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.Equal(0u, rawFlags & IsEditAndContinue);
        Assert.Equal(0u, rawFlags & DebuggerEncEnabledPriv);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetCompilerFlags_EnCCapable_ModifiableAssembliesNone(MockTarget.Architecture arch)
    {
        ulong assemblyAddr = 0;
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (dacDbi, target) = CreateDacDbiWithLoader(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.None);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            var module = loader.AddModule(flags: IsEncCapable | DebuggerIgnorePdbsPriv);
            assemblyAddr = module.Assembly;
            moduleAddr = new TargetPointer(module.Address);
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        int hr = dacDbi.SetCompilerFlags(assemblyAddr, Interop.BOOL.TRUE, Interop.BOOL.TRUE);
        Assert.Equal(System.HResults.S_OK, hr);
        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.Equal(0u, rawFlags & IsEditAndContinue);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetCompilerFlags_EncCapable_NoPdbsIgnored(MockTarget.Architecture arch)
    {
        ulong assemblyAddr = 0;
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (dacDbi, target) = CreateDacDbiWithLoader(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.Debug);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            var module = loader.AddModule(flags: IsEncCapable);
            assemblyAddr = module.Assembly;
            moduleAddr = new TargetPointer(module.Address);
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        int hr = dacDbi.SetCompilerFlags(assemblyAddr, Interop.BOOL.TRUE, Interop.BOOL.TRUE);
        Assert.Equal(CorDbgHResults.CORDBG_S_NOT_ALL_BITS_SET, hr);
        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.Equal(0u, rawFlags & IsEditAndContinue);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetCompilerFlags_JitOptsToggling(MockTarget.Architecture arch)
    {
        ulong assemblyAddr = 0;
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (dacDbi, target) = CreateDacDbiWithLoader(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.None);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            var module = loader.AddModule();
            assemblyAddr = module.Assembly;
            moduleAddr = new TargetPointer(module.Address);
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        // Enable JIT opts
        int hr = dacDbi.SetCompilerFlags(assemblyAddr, Interop.BOOL.TRUE, Interop.BOOL.FALSE);
        Assert.Equal(System.HResults.S_OK, hr);
        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.NotEqual(0u, rawFlags & DebuggerAllowJitOptsPriv);

        // Disable JIT opts
        hr = dacDbi.SetCompilerFlags(assemblyAddr, Interop.BOOL.FALSE, Interop.BOOL.FALSE);
        Assert.Equal(System.HResults.S_OK, hr);
        rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.Equal(0u, rawFlags & DebuggerAllowJitOptsPriv);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetCompilerFlags_EnCBlocked_ProfilerPresent(MockTarget.Architecture arch)
    {
        ulong assemblyAddr = 0;
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var profLayout = MockProfControlBlock.CreateLayout(arch);
        var (dacDbi, target) = CreateDacDbiWithLoader(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.Debug);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            var module = loader.AddModule(flags: IsEncCapable | DebuggerIgnorePdbsPriv);
            assemblyAddr = module.Assembly;
            moduleAddr = new TargetPointer(module.Address);
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;

            var profFragment = builder.MemoryBuilder.CreateAllocator(0x0020_0000, 0x0020_1000).Allocate((ulong)profLayout.Size, "ProfControlBlock");
            MockProfControlBlock profBlock = profLayout.Create(profFragment);
            profBlock.GlobalEventMask = 0;
            profBlock.RejitOnAttachEnabled = 0;
            profBlock.MainProfilerProfInterface = 1;
            profBlock.NotificationProfilerCount = 0;
            builder.AddGlobals((Constants.Globals.ProfilerControlBlock, profFragment.Address));
            builder.AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.ProfControlBlock] = TargetTestHelpers.CreateTypeInfo(profLayout),
            });
        });

        int hr = dacDbi.SetCompilerFlags(assemblyAddr, Interop.BOOL.TRUE, Interop.BOOL.TRUE);
        Assert.Equal(CorDbgHResults.CORDBG_S_NOT_ALL_BITS_SET, hr);
        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.Equal(0u, rawFlags & IsEditAndContinue);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetCompilerFlags_EnCBlocked_NotificationProfiler(MockTarget.Architecture arch)
    {
        ulong assemblyAddr = 0;
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var profLayout = MockProfControlBlock.CreateLayout(arch);
        var (dacDbi, target) = CreateDacDbiWithLoader(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.Debug);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            var module = loader.AddModule(flags: IsEncCapable | DebuggerIgnorePdbsPriv);
            assemblyAddr = module.Assembly;
            moduleAddr = new TargetPointer(module.Address);
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;

            var profFragment = builder.MemoryBuilder.CreateAllocator(0x0020_0000, 0x0020_1000).Allocate((ulong)profLayout.Size, "ProfControlBlock");
            MockProfControlBlock profBlock = profLayout.Create(profFragment);
            profBlock.GlobalEventMask = 0;
            profBlock.RejitOnAttachEnabled = 0;
            profBlock.MainProfilerProfInterface = 0;
            profBlock.NotificationProfilerCount = 2;
            builder.AddGlobals((Constants.Globals.ProfilerControlBlock, profFragment.Address));
            builder.AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.ProfControlBlock] = TargetTestHelpers.CreateTypeInfo(profLayout),
            });
        });

        int hr = dacDbi.SetCompilerFlags(assemblyAddr, Interop.BOOL.TRUE, Interop.BOOL.TRUE);
        Assert.Equal(CorDbgHResults.CORDBG_S_NOT_ALL_BITS_SET, hr);
        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.Equal(0u, rawFlags & IsEditAndContinue);
    }
}
