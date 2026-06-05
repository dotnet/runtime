// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
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

    private const uint MdtMethodDef = 0x06000000;

    private static DacDbiImpl CreateDacDbiWithMockContracts(
        MockTarget.Architecture arch,
        Mock<ILoader> mockLoader,
        Mock<ICodeVersions> mockCodeVersions,
        Mock<IReJIT> mockReJIT)
    {
        var target = new TestPlaceholderTarget.Builder(arch)
            .UseReader((_, _) => -1)
            .AddMockContract(mockLoader)
            .AddMockContract(mockCodeVersions)
            .AddMockContract(mockReJIT)
            .Build();
        return new DacDbiImpl(target, legacyObj: null);
    }

    private static Mock<ILoader> SetupMockLoader(TargetPointer modulePtr, uint methodTk, TargetPointer methodDesc)
    {
        var mockLoader = new Mock<ILoader>();
        var moduleHandle = new Contracts.ModuleHandle(modulePtr);
        var lookupTables = new ModuleLookupTables { MethodDefToDesc = new TargetPointer(0x4000) };
        mockLoader.Setup(l => l.GetModuleHandleFromModulePtr(modulePtr)).Returns(moduleHandle);
        mockLoader.Setup(l => l.GetLookupTables(moduleHandle)).Returns(lookupTables);
        mockLoader.Setup(l => l.GetModuleLookupMapElement(lookupTables.MethodDefToDesc, methodTk, out It.Ref<TargetNUInt>.IsAny))
            .Returns(methodDesc);
        return mockLoader;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void AreOptimizationsDisabled_NullOutput_ReturnsError(MockTarget.Architecture arch)
    {
        var dacDbi = CreateDacDbiWithMockContracts(
            arch, new Mock<ILoader>(), new Mock<ICodeVersions>(), new Mock<IReJIT>());
        int hr = dacDbi.AreOptimizationsDisabled(0x1000, MdtMethodDef | 1, null);
        Assert.NotEqual(System.HResults.S_OK, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void AreOptimizationsDisabled_InvalidToken_ReturnsError(MockTarget.Architecture arch)
    {
        var dacDbi = CreateDacDbiWithMockContracts(
            arch, new Mock<ILoader>(), new Mock<ICodeVersions>(), new Mock<IReJIT>());
        Interop.BOOL result;
        int hr = dacDbi.AreOptimizationsDisabled(0x1000, 0x01000001, &result);
        Assert.NotEqual(System.HResults.S_OK, hr);
    }

    public static IEnumerable<object[]> ArchWithDeoptimized()
    {
        foreach (object[] stdArch in new MockTarget.StdArch())
        {
            yield return [stdArch[0], true];
            yield return [stdArch[0], false];
        }
    }

    [Theory]
    [MemberData(nameof(ArchWithDeoptimized))]
    public void AreOptimizationsDisabled_WithMethodDesc(MockTarget.Architecture arch, bool deoptimized)
    {
        TargetPointer modulePtr = new(0x1000);
        uint methodTk = MdtMethodDef | 1;
        TargetPointer methodDesc = new(0x2000);
        var ilCodeVersion = ILCodeVersionHandle.CreateExplicit(new TargetPointer(0x3000));

        Mock<ILoader> mockLoader = SetupMockLoader(modulePtr, methodTk, methodDesc);

        var mockCodeVersions = new Mock<ICodeVersions>();
        mockCodeVersions.Setup(cv => cv.GetActiveILCodeVersion(methodDesc)).Returns(ilCodeVersion);

        var mockReJIT = new Mock<IReJIT>();
        mockReJIT.Setup(r => r.IsDeoptimized(ilCodeVersion)).Returns(deoptimized);

        var dacDbi = CreateDacDbiWithMockContracts(arch, mockLoader, mockCodeVersions, mockReJIT);
        Interop.BOOL result;
        int hr = dacDbi.AreOptimizationsDisabled(modulePtr.Value, methodTk, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(deoptimized ? Interop.BOOL.TRUE : Interop.BOOL.FALSE, result);
    }

    private delegate void TryGetLockInfoCallback(TargetPointer syncBlock, out uint owningThreadId, out uint recursion);

    public static IEnumerable<object[]> GetThreadOwningMonitorLockData()
    {
        foreach (var arch in new MockTarget.StdArch())
        {
            yield return new object[] { arch[0], false, false, 0u, 0ul, 0u };
            yield return new object[] { arch[0], true, false, 0u, 0ul, 0u };
            yield return new object[] { arch[0], true, true, 3u, 0x7000ul, 4u };
        }
    }

    [Theory]
    [MemberData(nameof(GetThreadOwningMonitorLockData))]
    public void GetThreadOwningMonitorLock(MockTarget.Architecture arch, bool hasSyncBlock, bool isLockHeld, uint recursionCount, ulong expectedOwner, uint expectedAcquisitionCount)
    {
        const ulong ObjectAddr = 0x5000;
        const uint OwnerThreadId = 42;
        TargetPointer syncBlockAddr = new(0x6000);
        TargetPointer ownerThreadPtr = new(0x7000);

        var mockObject = new Mock<IObject>();
        mockObject.Setup(o => o.GetSyncBlockAddress(new TargetPointer(ObjectAddr)))
            .Returns(hasSyncBlock ? syncBlockAddr : TargetPointer.Null);

        var builder = new TestPlaceholderTarget.Builder(arch)
            .UseReader((_, _) => -1)
            .AddMockContract(mockObject);

        if (hasSyncBlock)
        {
            var mockSyncBlock = new Mock<ISyncBlock>();
            var lockSetup = mockSyncBlock
                .Setup(s => s.TryGetLockInfo(syncBlockAddr, out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny));
            if (isLockHeld)
            {
                lockSetup
                    .Callback(new TryGetLockInfoCallback((TargetPointer _, out uint threadId, out uint recursion) =>
                    {
                        threadId = OwnerThreadId;
                        recursion = recursionCount;
                    }))
                    .Returns(true);
            }
            else
            {
                lockSetup.Returns(false);
            }
            builder.AddMockContract(mockSyncBlock);
        }

        if (isLockHeld)
        {
            var mockThread = new Mock<IThread>();
            mockThread.Setup(t => t.IdToThread(OwnerThreadId))
                .Returns(ownerThreadPtr);
            builder.AddMockContract(mockThread);
        }

        var dacDbi = new DacDbiImpl(builder.Build(), legacyObj: null);

        DacDbiMonitorLockInfo result;
        int hr = dacDbi.GetThreadOwningMonitorLock(ObjectAddr, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(expectedOwner, result.lockOwner);
        Assert.Equal(expectedAcquisitionCount, result.acquisitionCount);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void AreOptimizationsDisabled_NullMethodDesc_ReturnsFalse(MockTarget.Architecture arch)
    {
        TargetPointer modulePtr = new(0x1000);
        uint methodTk = MdtMethodDef | 1;

        Mock<ILoader> mockLoader = SetupMockLoader(modulePtr, methodTk, TargetPointer.Null);

        var dacDbi = CreateDacDbiWithMockContracts(arch, mockLoader, new Mock<ICodeVersions>(), new Mock<IReJIT>());
        Interop.BOOL result;
        int hr = dacDbi.AreOptimizationsDisabled(modulePtr.Value, methodTk, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(Interop.BOOL.FALSE, result);
    }

    private static (DacDbiImpl DacDbi, MockThread Thread, MockFrameBuilder FrameBuilder) CreateManagedStoppedContextDacDbi(
        MockTarget.Architecture arch,
        Action<MockFrameBuilder>? configureFrames = null)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockThreadBuilder threadBuilder = new(targetBuilder.MemoryBuilder);
        MockFrameBuilder frameBuilder = new(targetBuilder.MemoryBuilder);

        MockThread thread = threadBuilder.AddThread(1, 1234);
        ulong terminator = arch.Is64Bit ? ulong.MaxValue : uint.MaxValue;
        thread.Frame = terminator;

        configureFrames?.Invoke(frameBuilder);

        targetBuilder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.ExceptionInfo] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ExceptionInfoLayout),
                [DataType.Thread] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ThreadLayout),
                [DataType.ThreadStore] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ThreadStoreLayout),
                [DataType.GCAllocContext] = TargetTestHelpers.CreateTypeInfo(threadBuilder.GCAllocContextLayout),
                [DataType.EEAllocContext] = TargetTestHelpers.CreateTypeInfo(threadBuilder.EEAllocContextLayout),
                [DataType.RuntimeThreadLocals] = TargetTestHelpers.CreateTypeInfo(threadBuilder.RuntimeThreadLocalsLayout),
                [DataType.Frame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.FrameLayout),
                [DataType.ResumableFrame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.ResumableFrameLayout),
            })
            .AddGlobals(
                (nameof(Constants.Globals.ThreadStore), threadBuilder.ThreadStoreGlobalAddress),
                (nameof(Constants.Globals.FinalizerThread), threadBuilder.FinalizerThreadGlobalAddress),
                (nameof(Constants.Globals.GCThread), threadBuilder.GCThreadGlobalAddress),
                ("RedirectedThreadFrameIdentifier", MockFrameBuilder.RedirectedThreadFrameIdentifierValue))
            .AddMockContract(new Mock<IExecutionManager>())
            .AddMockContract(new Mock<IGCInfo>())
            .AddContract<IThread>(version: "c1")
            .AddContract<IStackWalk>(version: "c1");
        TestPlaceholderTarget target = targetBuilder.Build();
        DacDbiImpl dacDbi = new(target, legacyObj: null);
        return (dacDbi, thread, frameBuilder);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetManagedStoppedContext_InteropDebuggingHijacked(MockTarget.Architecture arch)
    {
        var (dacDbi, thread, _) = CreateManagedStoppedContextDacDbi(arch);
        thread.InteropDebuggingHijacked = 1;

        ulong retVal;
        int hr = dacDbi.GetManagedStoppedContext(thread.Address, &retVal);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(0UL, retVal);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetManagedStoppedContext_FilterContextSet(MockTarget.Architecture arch)
    {
        const ulong filterContextAddr = 0x0009_0000;
        var (dacDbi, thread, _) = CreateManagedStoppedContextDacDbi(arch);
        thread.DebuggerFilterContext = filterContextAddr;

        ulong retVal;
        int hr = dacDbi.GetManagedStoppedContext(thread.Address, &retVal);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(filterContextAddr, retVal);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetManagedStoppedContext_RedirectedThread(MockTarget.Architecture arch)
    {
        const ulong contextAddr = 0x000A_0000;
        MockResumableFrame? redirectedFrame = null;
        var (dacDbi, thread, _) = CreateManagedStoppedContextDacDbi(arch, frameBuilder =>
        {
            redirectedFrame = frameBuilder.AddRedirectedThreadFrame(contextAddr);
        });

        thread.Frame = redirectedFrame!.Address;

        ulong retVal;
        int hr = dacDbi.GetManagedStoppedContext(thread.Address, &retVal);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(contextAddr, retVal);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetManagedStoppedContext_NoContextAvailable(MockTarget.Architecture arch)
    {
        var (dacDbi, thread, _) = CreateManagedStoppedContextDacDbi(arch);

        ulong retVal;
        int hr = dacDbi.GetManagedStoppedContext(thread.Address, &retVal);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(0UL, retVal);
    }

    private const int ConditionWaitersHeadOffset = 8;
    private const int WaiterNextOffset = 8;

    private static (DacDbiImpl DacDbi, TestPlaceholderTarget Target) CreateMonitorWaitListDacDbi(
        MockTarget.Architecture arch,
        TargetPointer objectAddr,
        TargetPointer syncBlockAddr,
        TargetPointer conditionTableAddr,
        bool cwtFindsCondition,
        (TargetPointer Thread, bool IsWaiting)[]? threads = null)
    {
        var helpers = new TargetTestHelpers(arch);
        int ptrSize = arch.Is64Bit ? 8 : 4;
        var builder = new TestPlaceholderTarget.Builder(arch);
        var allocator = builder.MemoryBuilder.CreateAllocator(0x0040_0000, 0x0040_F000);
        var mockMts = new Mock<IManagedTypeSource>();

        var globalFragment = allocator.Allocate((ulong)ptrSize, "ConditionTableGlobal");
        helpers.WritePointer(globalFragment.Data, conditionTableAddr.Value);
        TargetPointer conditionTableSlotAddr = new(globalFragment.Address);
        mockMts.Setup(m => m.TryGetStaticFieldAddress(
                "System.Threading.Monitor", "s_conditionTable", out conditionTableSlotAddr))
            .Returns(true);

        mockMts.Setup(m => m.GetTypeInfo("System.Threading.Condition")).Returns(new Target.TypeInfo
        {
            Size = (uint)(ConditionWaitersHeadOffset + ptrSize),
            Fields = new Dictionary<string, Target.FieldInfo>
            {
                ["_waitersHead"] = new Target.FieldInfo { Offset = ConditionWaitersHeadOffset, TypeName = null },
            }
        });
        mockMts.Setup(m => m.GetTypeInfo("System.Threading.Condition+Waiter")).Returns(new Target.TypeInfo
        {
            Size = (uint)(WaiterNextOffset + ptrSize),
            Fields = new Dictionary<string, Target.FieldInfo>
            {
                ["next"] = new Target.FieldInfo { Offset = WaiterNextOffset, TypeName = null },
            }
        });

        var waitingThreads = threads?.Where(t => t.IsWaiting).ToArray() ?? [];
        var waiterFragments = new MockMemorySpace.HeapFragment[waitingThreads.Length];
        for (int i = 0; i < waitingThreads.Length; i++)
        {
            waiterFragments[i] = allocator.Allocate((ulong)(WaiterNextOffset + ptrSize), $"Waiter_{i}");
        }

        for (int i = 0; i < waiterFragments.Length; i++)
        {
            ulong nextAddr = i + 1 < waiterFragments.Length ? waiterFragments[i + 1].Address : 0;
            helpers.WritePointer(waiterFragments[i].Data.AsSpan(WaiterNextOffset), nextAddr);
        }

        var conditionFragment = allocator.Allocate((ulong)(ConditionWaitersHeadOffset + ptrSize), "Condition");
        ulong waitersHeadValue = waiterFragments.Length > 0 ? waiterFragments[0].Address : 0;
        helpers.WritePointer(conditionFragment.Data.AsSpan(ConditionWaitersHeadOffset), waitersHeadValue);

        var mockObject = new Mock<IObject>();
        mockObject.Setup(o => o.GetSyncBlockAddress(objectAddr)).Returns(syncBlockAddr);

        var mockCwt = new Mock<IConditionalWeakTable>();
        TargetPointer cwtOutCondition = new(conditionFragment.Address);
        mockCwt.Setup(c => c.TryGetValue(conditionTableAddr, objectAddr, out cwtOutCondition))
            .Returns(cwtFindsCondition);

        var mockThread = new Mock<IThread>();
        if (threads is not null && threads.Length > 0)
        {
            var threadStore = new ThreadStoreData(threads.Length, threads[0].Thread, TargetPointer.Null, TargetPointer.Null);
            mockThread.Setup(t => t.GetThreadStoreData()).Returns(threadStore);
            for (int i = 0; i < threads.Length; i++)
            {
                TargetPointer nextThread = i + 1 < threads.Length ? threads[i + 1].Thread : TargetPointer.Null;
                var threadData = new ThreadData(
                    threads[i].Thread, 0, default, default, false,
                    TargetPointer.Null, TargetPointer.Null, TargetPointer.Null,
                    TargetPointer.Null, TargetPointer.Null, TargetPointer.Null,
                    TargetPointer.Null, false, false, nextThread,
                    TargetPointer.Null, false, TargetPointer.Null);
                mockThread.Setup(t => t.GetThreadData(threads[i].Thread)).Returns(threadData);
            }
        }
        else
        {
            var threadStore = new ThreadStoreData(0, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);
            mockThread.Setup(t => t.GetThreadStoreData()).Returns(threadStore);
        }

        int waiterIdx = 0;
        if (threads is not null)
        {
            foreach (var (thread, isWaiting) in threads)
            {
                if (isWaiting)
                {
                    var slotFragment = allocator.Allocate((ulong)ptrSize, $"WaiterSlot_{thread.Value:x}");
                    helpers.WritePointer(slotFragment.Data, waiterFragments[waiterIdx].Address);

                    TargetPointer outAddr = new(slotFragment.Address);
                    mockMts.Setup(m => m.TryGetThreadStaticFieldAddress(
                            "System.Threading.Condition", "t_waiterForCurrentThread", thread, out outAddr))
                        .Returns(true);
                    waiterIdx++;
                }
                else
                {
                    TargetPointer nullAddr = TargetPointer.Null;
                    mockMts.Setup(m => m.TryGetThreadStaticFieldAddress(
                            "System.Threading.Condition", "t_waiterForCurrentThread", thread, out nullAddr))
                        .Returns(false);
                }
            }
        }

        builder.AddMockContract(mockObject);
        builder.AddMockContract(mockCwt);
        builder.AddMockContract(mockThread);
        builder.AddMockContract(mockMts);

        var target = builder.Build();
        var dacDbi = new DacDbiImpl(target, legacyObj: null);
        return (dacDbi, target);
    }

    [UnmanagedCallersOnly]
    private static void CollectThreadCallback(ulong value, nint pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr(pUserData);
        ((List<ulong>)handle.Target!).Add(value);
    }

    private static (int Hr, List<ulong> Threads) RunEnumerateMonitorEventWaitList(DacDbiImpl dacDbi, ulong vmObject)
    {
        List<ulong> threads = new();
        GCHandle gcHandle = GCHandle.Alloc(threads);
        int hr = dacDbi.EnumerateMonitorEventWaitList(
            vmObject,
            (nint)(delegate* unmanaged<ulong, nint, void>)&CollectThreadCallback,
            GCHandle.ToIntPtr(gcHandle));
        gcHandle.Free();
        return (hr, threads);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateMonitorEventWaitList_NoSyncBlock(MockTarget.Architecture arch)
    {
        var mockObject = new Mock<IObject>();
        mockObject.Setup(o => o.GetSyncBlockAddress(new TargetPointer(0x1000))).Returns(TargetPointer.Null);

        var target = new TestPlaceholderTarget.Builder(arch)
            .UseReader((_, _) => -1)
            .AddMockContract(mockObject)
            .Build();
        var dacDbi = new DacDbiImpl(target, legacyObj: null);

        var (hr, threads) = RunEnumerateMonitorEventWaitList(dacDbi, 0x1000);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Empty(threads);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateMonitorEventWaitList_NoConditionOrEmptyList(MockTarget.Architecture arch)
    {
        // Object not found in ConditionalWeakTable, so no waiters are returned.
        var (dacDbi1, _) = CreateMonitorWaitListDacDbi(arch, new(0x1000), new(0x2000), new(0x3000), cwtFindsCondition: false);
        var (hr1, threads1) = RunEnumerateMonitorEventWaitList(dacDbi1, 0x1000);
        Assert.Equal(System.HResults.S_OK, hr1);
        Assert.Empty(threads1);

        // Condition exists but waiter list is empty, so no waiters are returned.
        var (dacDbi2, _) = CreateMonitorWaitListDacDbi(arch, new(0x1000), new(0x2000), new(0x3000), cwtFindsCondition: true);
        var (hr2, threads2) = RunEnumerateMonitorEventWaitList(dacDbi2, 0x1000);
        Assert.Equal(System.HResults.S_OK, hr2);
        Assert.Empty(threads2);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateMonitorEventWaitList_NullCallback(MockTarget.Architecture arch)
    {
        var mockObject = new Mock<IObject>();
        mockObject.Setup(o => o.GetSyncBlockAddress(new TargetPointer(0x1000))).Returns(new TargetPointer(0x2000));
        var target = new TestPlaceholderTarget.Builder(arch)
            .UseReader((_, _) => -1)
            .AddMockContract(mockObject)
            .Build();
        var dacDbi = new DacDbiImpl(target, legacyObj: null);
        int hr = dacDbi.EnumerateMonitorEventWaitList(0x1000, 0, 0);
        Assert.NotEqual(System.HResults.S_OK, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EnumerateMonitorEventWaitList_WaitersReturnMatchingThreads(MockTarget.Architecture arch)
    {
        TargetPointer thread1 = new(0x6000);
        TargetPointer thread2 = new(0x6100);

        // Single waiter
        var (dacDbi1, _) = CreateMonitorWaitListDacDbi(arch, new(0x1000), new(0x2000), new(0x3000),
            cwtFindsCondition: true, threads: [(thread1, true)]);
        var (hr1, result1) = RunEnumerateMonitorEventWaitList(dacDbi1, 0x1000);
        Assert.Equal(System.HResults.S_OK, hr1);
        Assert.Equal(new[] { thread1.Value }, result1);

        // Two waiters
        var (dacDbi2, _) = CreateMonitorWaitListDacDbi(arch, new(0x1000), new(0x2000), new(0x3000),
            cwtFindsCondition: true, threads: [(thread1, true), (thread2, true)]);
        var (hr2, result2) = RunEnumerateMonitorEventWaitList(dacDbi2, 0x1000);
        Assert.Equal(System.HResults.S_OK, hr2);
        Assert.Equal(new[] { thread1.Value, thread2.Value }, result2);

        // Non-waiting thread skipped
        var (dacDbi3, _) = CreateMonitorWaitListDacDbi(arch, new(0x1000), new(0x2000), new(0x3000),
            cwtFindsCondition: true, threads: [(thread1, true), (thread2, false)]);
        var (hr3, result3) = RunEnumerateMonitorEventWaitList(dacDbi3, 0x1000);
        Assert.Equal(System.HResults.S_OK, hr3);
        Assert.Equal(new[] { thread1.Value }, result3);
    }
}
