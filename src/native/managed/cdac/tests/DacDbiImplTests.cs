// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Moq;
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

    private static DacDbiImpl CreateDacDbiWithMockLoader(
        MockTarget.Architecture arch,
        Mock<ILoader> mockLoader)
    {
        var target = new TestPlaceholderTarget.Builder(arch)
            .UseReader((_, _) => -1)
            .AddMockContract(mockLoader)
            .Build();
        return new DacDbiImpl(target, legacyObj: null);
    }

    private static (DacDbiImpl DacDbi, TestPlaceholderTarget Target) CreateDacDbiWithExceptionMT(
        MockTarget.Architecture arch,
        TargetPointer exceptionMT,
        Mock<IObject> mockObject,
        Mock<IRuntimeTypeSystem> mockRts)
    {
        var builder = new TestPlaceholderTarget.Builder(arch);
        var allocator = builder.MemoryBuilder.CreateAllocator(0x0030_0000, 0x0030_1000);
        var globalFragment = allocator.Allocate((ulong)(arch.Is64Bit ? 8 : 4), "ExceptionMethodTable");
        new TargetTestHelpers(arch).WritePointer(globalFragment.Data, exceptionMT.Value);
        builder.AddGlobals((Constants.Globals.ExceptionMethodTable, globalFragment.Address));
        builder.AddMockContract(mockObject);
        builder.AddMockContract(mockRts);
        var target = builder.Build();
        var dacDbi = new DacDbiImpl(target, legacyObj: null);
        return (dacDbi, target);
    }

    public static IEnumerable<object[]> IsExceptionObjectData()
    {
        foreach (var arch in new MockTarget.StdArch())
        {
            // Exact exception type
            yield return new object[] { arch[0], 0, true };
            // Derived exception type
            yield return new object[] { arch[0], 1, true };
            // Deeply derived exception type
            yield return new object[] { arch[0], 2, true };
            // Non-exception type (no parent)
            yield return new object[] { arch[0], 0, false };
            // Non-exception type (with parent)
            yield return new object[] { arch[0], 1, false };
        }
    }

    [Theory]
    [MemberData(nameof(IsExceptionObjectData))]
    public void IsExceptionObject(MockTarget.Architecture arch, int inheritanceDepth, bool isException)
    {
        TargetPointer exceptionMT = new(0x1000);
        TargetPointer objectAddr = new(0x5000);

        var intermediateMTs = new TargetPointer[inheritanceDepth];
        for (int i = 0; i < inheritanceDepth; i++)
            intermediateMTs[i] = new TargetPointer((ulong)(0x2000 + i * 0x1000));

        TargetPointer objectMT = inheritanceDepth == 0 && isException
            ? exceptionMT
            : intermediateMTs.Length > 0 ? intermediateMTs[0] : new TargetPointer(0x2000);

        var mockObject = new Mock<IObject>();
        mockObject.Setup(o => o.GetMethodTableAddress(objectAddr)).Returns(objectMT);

        var mockRts = new Mock<IRuntimeTypeSystem>();
        if (intermediateMTs.Length == 0 && !isException)
        {
            mockRts.Setup(r => r.GetTypeHandle(objectMT)).Returns(new TypeHandle(objectMT));
            mockRts.Setup(r => r.GetParentMethodTable(new TypeHandle(objectMT))).Returns(TargetPointer.Null);
        }
        for (int i = 0; i < intermediateMTs.Length; i++)
        {
            TargetPointer current = intermediateMTs[i];
            TargetPointer parent = i + 1 < intermediateMTs.Length
                ? intermediateMTs[i + 1]
                : isException ? exceptionMT : TargetPointer.Null;

            mockRts.Setup(r => r.GetTypeHandle(current)).Returns(new TypeHandle(current));
            mockRts.Setup(r => r.GetParentMethodTable(new TypeHandle(current))).Returns(parent);
        }

        var (dacDbi, _) = CreateDacDbiWithExceptionMT(arch, exceptionMT, mockObject, mockRts);

        Interop.BOOL result;
        int hr = dacDbi.IsExceptionObject(objectAddr.Value, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(isException ? Interop.BOOL.TRUE : Interop.BOOL.FALSE, result);
    }

    [UnmanagedCallersOnly]
    private static unsafe void CollectAssemblyCallback(ulong value, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ((List<ulong>)handle.Target!).Add(value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateAssembliesInAppDomain_ZeroAppDomain(MockTarget.Architecture arch)
    {
        var mockLoader = new Mock<ILoader>();
        DacDbiImpl dacDbi = CreateDacDbiWithMockLoader(arch, mockLoader);

        List<ulong> assemblies = new();
        GCHandle gcHandle = GCHandle.Alloc(assemblies);
        int hr = dacDbi.EnumerateAssembliesInAppDomain(0, &CollectAssemblyCallback, GCHandle.ToIntPtr(gcHandle));
        gcHandle.Free();

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Empty(assemblies);
        mockLoader.Verify(
            l => l.GetModuleHandles(It.IsAny<TargetPointer>(), It.IsAny<AssemblyIterationFlags>()),
            Times.Never);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateAssembliesInAppDomain_NullCallback(MockTarget.Architecture arch)
    {
        var mockLoader = new Mock<ILoader>();
        DacDbiImpl dacDbi = CreateDacDbiWithMockLoader(arch, mockLoader);

        int hr = dacDbi.EnumerateAssembliesInAppDomain(0x1000, null, nint.Zero);

        Assert.NotEqual(System.HResults.S_OK, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateAssembliesInAppDomain_SingleAssembly_CallsCallback(MockTarget.Architecture arch)
    {
        ulong appDomainAddr = 0x1000;
        ulong assemblyAddr = 0x2000;
        TargetPointer moduleAddr = new(0x3000);

        var mockLoader = new Mock<ILoader>();
        mockLoader
            .Setup(l => l.GetModuleHandles(
                new TargetPointer(appDomainAddr),
                AssemblyIterationFlags.IncludeLoading | AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution))
            .Returns(new[] { new Contracts.ModuleHandle(moduleAddr) });
        mockLoader
            .Setup(l => l.GetAssembly(It.Is<Contracts.ModuleHandle>(h => h.Address == moduleAddr)))
            .Returns(new TargetPointer(assemblyAddr));

        DacDbiImpl dacDbi = CreateDacDbiWithMockLoader(arch, mockLoader);

        List<ulong> assemblies = new();
        GCHandle gcHandle = GCHandle.Alloc(assemblies);
        int hr = dacDbi.EnumerateAssembliesInAppDomain(appDomainAddr, &CollectAssemblyCallback, GCHandle.ToIntPtr(gcHandle));
        gcHandle.Free();

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Single(assemblies);
        Assert.Equal(assemblyAddr, assemblies[0]);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateAssembliesInAppDomain_MultipleAssemblies(MockTarget.Architecture arch)
    {
        ulong appDomainAddr = 0x1000;
        ulong[] expectedAssemblies = [0x2000, 0x3000, 0x4000];
        TargetPointer[] moduleAddrs = [new(0x5000), new(0x6000), new(0x7000)];

        var mockLoader = new Mock<ILoader>();
        mockLoader
            .Setup(l => l.GetModuleHandles(
                new TargetPointer(appDomainAddr),
                AssemblyIterationFlags.IncludeLoading | AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution))
            .Returns(new[]
            {
                new Contracts.ModuleHandle(moduleAddrs[0]),
                new Contracts.ModuleHandle(moduleAddrs[1]),
                new Contracts.ModuleHandle(moduleAddrs[2]),
            });

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            mockLoader
                .Setup(l => l.GetAssembly(It.Is<Contracts.ModuleHandle>(h => h.Address == moduleAddrs[index])))
                .Returns(new TargetPointer(expectedAssemblies[index]));
        }

        DacDbiImpl dacDbi = CreateDacDbiWithMockLoader(arch, mockLoader);

        List<ulong> assemblies = new();
        GCHandle gcHandle = GCHandle.Alloc(assemblies);
        int hr = dacDbi.EnumerateAssembliesInAppDomain(appDomainAddr, &CollectAssemblyCallback, GCHandle.ToIntPtr(gcHandle));
        gcHandle.Free();

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(expectedAssemblies, assemblies.ToArray());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateAssembliesInAppDomain_NoAssemblies(MockTarget.Architecture arch)
    {
        ulong appDomainAddr = 0x1000;

        var mockLoader = new Mock<ILoader>();
        mockLoader
            .Setup(l => l.GetModuleHandles(
                new TargetPointer(appDomainAddr),
                AssemblyIterationFlags.IncludeLoading | AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution))
            .Returns(Array.Empty<Contracts.ModuleHandle>());

        DacDbiImpl dacDbi = CreateDacDbiWithMockLoader(arch, mockLoader);

        List<ulong> assemblies = new();
        GCHandle gcHandle = GCHandle.Alloc(assemblies);
        int hr = dacDbi.EnumerateAssembliesInAppDomain(appDomainAddr, &CollectAssemblyCallback, GCHandle.ToIntPtr(gcHandle));
        gcHandle.Free();

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Empty(assemblies);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSymbolsBuffer_NoStream(MockTarget.Architecture arch)
    {
        TargetPointer moduleAddr = TargetPointer.Null;
        var (dacDbi, _) = CreateDacDbiWithLoader(arch, (loader, _) =>
        {
            moduleAddr = new TargetPointer(loader.AddModule().Address);
        });

        DacDbiTargetBuffer targetBuffer;
        SymbolFormat symbolFormat;
        int hr = dacDbi.GetSymbolsBuffer(moduleAddr, &targetBuffer, &symbolFormat);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(0UL, targetBuffer.pAddress);
        Assert.Equal(0u, targetBuffer.cbSize);
        Assert.Equal(SymbolFormat.None, symbolFormat);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSymbolsBuffer_WithSymbols(MockTarget.Architecture arch)
    {
        byte[] symbolBytes = [0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE];
        TargetPointer moduleAddr = TargetPointer.Null;
        ulong expectedBufferAddr = 0;
        var (dacDbi, _) = CreateDacDbiWithLoader(arch, (loader, _) =>
        {
            MockLoaderModule module = loader.AddModule();
            MockCGrowableSymbolStream stream = loader.AddInMemorySymbolStream(module, symbolBytes);
            moduleAddr = new TargetPointer(module.Address);
            expectedBufferAddr = stream.Buffer;
        });

        DacDbiTargetBuffer targetBuffer;
        SymbolFormat symbolFormat;
        int hr = dacDbi.GetSymbolsBuffer(moduleAddr, &targetBuffer, &symbolFormat);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(expectedBufferAddr, targetBuffer.pAddress);
        Assert.Equal((uint)symbolBytes.Length, targetBuffer.cbSize);
        Assert.Equal(SymbolFormat.Pdb, symbolFormat);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSymbolsBuffer_EmptyStream(MockTarget.Architecture arch)
    {
        // Stream object exists but contains no bytes - treated like no symbols.
        TargetPointer moduleAddr = TargetPointer.Null;
        var (dacDbi, _) = CreateDacDbiWithLoader(arch, (loader, _) =>
        {
            MockLoaderModule module = loader.AddModule();
            loader.AddInMemorySymbolStream(module, symbols: null);
            moduleAddr = new TargetPointer(module.Address);
        });

        DacDbiTargetBuffer targetBuffer;
        SymbolFormat symbolFormat;
        int hr = dacDbi.GetSymbolsBuffer(moduleAddr, &targetBuffer, &symbolFormat);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(0UL, targetBuffer.pAddress);
        Assert.Equal(0u, targetBuffer.cbSize);
        Assert.Equal(SymbolFormat.None, symbolFormat);
    }

    public static IEnumerable<object[]> TargetArchitectures()
    {
        string[] architectures = ["x64", "arm64", "arm", "x86", "loongarch64", "riscv64"];
        foreach (object[] stdArch in new MockTarget.StdArch())
        {
            foreach (string archName in architectures)
            {
                yield return [stdArch[0], archName];
            }
        }
    }

    public static IEnumerable<object[]> TargetArchitectures_SpRange()
    {
        foreach (object[] archData in TargetArchitectures())
        {
            yield return [archData[0], archData[1], (ulong)0x6000, System.HResults.S_OK];
            yield return [archData[0], archData[1], (ulong)0x2000, CorDbgHResults.CORDBG_E_NON_MATCHING_CONTEXT];
            yield return [archData[0], archData[1], (ulong)0x8000, CorDbgHResults.CORDBG_E_NON_MATCHING_CONTEXT];
        }
    }

    [Theory]
    [MemberData(nameof(TargetArchitectures_SpRange))]
    public void CheckContext_WithControlFlag_ValidatesSpRange(MockTarget.Architecture arch, string targetArch, ulong sp, int expectedHr)
    {
        const ulong ThreadAddr = 0x1000;
        var (dacDbi, target) = CreateCheckContextDacDbi(arch, targetArch, ThreadAddr, stackBase: 0x8000, stackLimit: 0x4000);

        IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(target);
        ctx.RawContextFlags = ctx.ContextControlFlags;
        ctx.StackPointer = new TargetPointer(sp);
        byte[] bytes = ctx.GetBytes();

        fixed (byte* pCtx = bytes)
        {
            int hr = dacDbi.CheckContext(ThreadAddr, (nint)pCtx);
            Assert.Equal(expectedHr, hr);
        }
    }

    [Theory]
    [MemberData(nameof(TargetArchitectures))]
    public void CheckContext_NoControlFlag_SkipsSpCheck(MockTarget.Architecture arch, string targetArch)
    {
        const ulong ThreadAddr = 0x1000;
        var mockThread = new Mock<IThread>();

        var target = new TestPlaceholderTarget.Builder(arch)
            .AddGlobalStrings((Constants.Globals.Architecture, targetArch))
            .AddContract<IRuntimeInfo>(version: "c1")
            .AddMockContract(mockThread)
            .Build();
        var dacDbi = new DacDbiImpl(target, legacyObj: null);

        IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(target);
        ctx.RawContextFlags = 0;
        ctx.StackPointer = new TargetPointer(0x2000);
        byte[] bytes = ctx.GetBytes();

        fixed (byte* pCtx = bytes)
        {
            int hr = dacDbi.CheckContext(ThreadAddr, (nint)pCtx);
            Assert.Equal(System.HResults.S_OK, hr);
        }

        mockThread.Verify(
            t => t.GetStackLimitData(It.IsAny<TargetPointer>(), out It.Ref<TargetPointer>.IsAny, out It.Ref<TargetPointer>.IsAny, out It.Ref<TargetPointer>.IsAny),
            Times.Never);
    }

    private static (DacDbiImpl DacDbi, Target Target) CreateCheckContextDacDbi(MockTarget.Architecture arch, string targetArch, ulong threadAddr, ulong stackBase, ulong stackLimit)
    {
        var mockThread = new Mock<IThread>();
        mockThread
            .Setup(t => t.GetStackLimitData(new TargetPointer(threadAddr), out It.Ref<TargetPointer>.IsAny, out It.Ref<TargetPointer>.IsAny, out It.Ref<TargetPointer>.IsAny))
            .Callback(new GetStackLimitDataCallback((TargetPointer _, out TargetPointer sb, out TargetPointer sl, out TargetPointer fa) =>
            {
                sb = new TargetPointer(stackBase);
                sl = new TargetPointer(stackLimit);
                fa = TargetPointer.Null;
            }));

        var target = new TestPlaceholderTarget.Builder(arch)
            .AddGlobalStrings((Constants.Globals.Architecture, targetArch))
            .AddContract<IRuntimeInfo>(version: "c1")
            .AddMockContract(mockThread)
            .Build();

        return (new DacDbiImpl(target, legacyObj: null), target);
    }

    private delegate void GetStackLimitDataCallback(TargetPointer threadPointer, out TargetPointer stackBase, out TargetPointer stackLimit, out TargetPointer frameAddress);
}
