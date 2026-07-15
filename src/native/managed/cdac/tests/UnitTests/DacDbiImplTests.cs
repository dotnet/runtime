// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

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
        Mock<IObject> mockObject,
        Mock<IRuntimeTypeSystem> mockRts)
    {
        var builder = new TestPlaceholderTarget.Builder(arch);
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
        mockRts.Setup(r => r.GetWellKnownMethodTable(WellKnownMethodTable.Exception)).Returns(exceptionMT);
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

        var (dacDbi, _) = CreateDacDbiWithExceptionMT(arch, mockObject, mockRts);

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

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ResolveTypeReference_TypeDef_PassesThrough(MockTarget.Architecture arch)
    {
        // A token that is already an mdtTypeDef resolves to (referencing module's assembly, same token).
        ulong assemblyAddr = 0;
        var (dacDbi, _) = CreateDacDbiWithLoader(arch, (loader, _) =>
        {
            MockLoaderModule module = loader.AddModule();
            assemblyAddr = module.Assembly;
        });

        uint typeDefToken = (uint)EcmaMetadataUtils.TokenType.mdtTypeDef | 0x000002;
        DacDbiTypeRefData input = new() { vmAssembly = assemblyAddr, typeToken = typeDefToken };
        DacDbiTypeRefData output = default;
        int hr = dacDbi.ResolveTypeReference(&input, &output);

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(assemblyAddr, output.vmAssembly);
        Assert.Equal(typeDefToken, output.typeToken);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ResolveTypeReference_TypeRef_NotCached_ReturnsClassNotLoaded(MockTarget.Architecture arch)
    {
        const ulong refAsmPtr = 0x100, refManifest = 0x9001;
        ModuleHandle refHandle = Mod(0x1000);

        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetModuleHandleFromAssemblyPtr(Ptr(refAsmPtr))).Returns(refHandle);
        loader.Setup(l => l.GetLookupTables(refHandle)).Returns(Tables(refManifest));
        SetupTypeRefCacheMiss(loader);

        Mock<IEcmaMetadata> ecma = new(MockBehavior.Strict);
        ecma.Setup(e => e.GetMetadata(refHandle)).Returns((MetadataReader?)null);

        DacDbiImpl dacDbi = CreateDacDbiWithMockContracts(arch, loader, ecma);

        DacDbiTypeRefData input = new() { vmAssembly = refAsmPtr, typeToken = MdtTypeRef | 3 };
        DacDbiTypeRefData output = default;
        int hr = dacDbi.ResolveTypeReference(&input, &output);

        Assert.Equal(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED, hr);
    }

    private const uint MdtTypeRef = (uint)EcmaMetadataUtils.TokenType.mdtTypeRef;
    private const uint MdtAssemblyRef = (uint)EcmaMetadataUtils.TokenType.mdtAssemblyRef;

    private static TargetPointer Ptr(ulong value) => new TargetPointer(value);
    private static ModuleHandle Mod(ulong address) => new ModuleHandle(new TargetPointer(address));

    private static (MetadataReader Reader, MetadataReaderProvider Provider) BuildMetadata(Action<MetadataBuilder> configure)
    {
        MetadataBuilder mb = new();
        mb.AddModule(0, mb.GetOrAddString("M"), mb.GetOrAddGuid(Guid.NewGuid()), default, default);
        mb.AddAssembly(mb.GetOrAddString("Asm"), new Version(1, 0, 0, 0), default, default, default, AssemblyHashAlgorithm.Sha1);

        // TypeDef row 1 must be the <Module> pseudo-type per ECMA-335.
        mb.AddTypeDefinition(default, default, mb.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        configure(mb);

        BlobBuilder blob = new();
        new MetadataRootBuilder(mb).Serialize(blob, 0, 0);
        MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(blob.ToArray()));
        return (provider.GetMetadataReader(), provider);
    }

    private static TypeDefinitionHandle AddTypeDef(MetadataBuilder mb, string @namespace, string name)
        => mb.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.Class,
            @namespace is null ? default : mb.GetOrAddString(@namespace),
            mb.GetOrAddString(name),
            default,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1));

    private static ModuleLookupTables Tables(ulong manifestModuleReferences)
        => new ModuleLookupTables(
            FieldDefToDesc: TargetPointer.Null,
            ManifestModuleReferences: Ptr(manifestModuleReferences),
            MemberRefToDesc: TargetPointer.Null,
            MethodDefToDesc: TargetPointer.Null,
            TypeDefToMethodTable: TargetPointer.Null,
            TypeRefToMethodTable: TargetPointer.Null,
            MethodDefToILCodeVersioningState: TargetPointer.Null,
            TableDataOffset: 0);

    private static void SetupLookupMap(Mock<ILoader> loader, ulong table, uint token, ulong result)
    {
        TargetNUInt flags = default;
        loader.Setup(l => l.GetModuleLookupMapElement(Ptr(table), token, out flags)).Returns(Ptr(result));
    }

    private static void SetupTypeRefCacheMiss(Mock<ILoader> loader)
    {
        // The referencing module's TypeRef->MethodTable cache is empty (TypeRefToMethodTable == Null),
        // so every Tier 1 lookup on the Null table misses.
        TargetNUInt flags = default;
        loader.Setup(l => l.GetModuleLookupMapElement(TargetPointer.Null, It.IsAny<uint>(), out flags)).Returns(TargetPointer.Null);
    }

    private static DacDbiImpl CreateDacDbiWithMockContracts(MockTarget.Architecture arch, Mock<ILoader> loader, Mock<IEcmaMetadata> ecma)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .AddMockContract(loader)
            .AddMockContract(ecma)
            .Build();
        return new DacDbiImpl(target, legacyObj: null);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ResolveTypeReference_TypeRef_AssemblyRefScope_ResolvesToTypeDef(MockTarget.Architecture arch)
    {
        // Referencing module: AssemblyRef "Target" (rid 1) and TypeRef "NS.Foo" scoped to it.
        var (refReader, refProvider) = BuildMetadata(mb =>
        {
            AssemblyReferenceHandle asmRef = mb.AddAssemblyReference(mb.GetOrAddString("Target"), new Version(1, 0, 0, 0), default, default, default, default);
            mb.AddTypeReference(asmRef, mb.GetOrAddString("NS"), mb.GetOrAddString("Foo"));
        });

        // Target module: TypeDef "NS.Foo" (row 2 -> token 0x02000002).
        var (targetReader, targetProvider) = BuildMetadata(mb => AddTypeDef(mb, "NS", "Foo"));

        const ulong refAsmPtr = 0x100, targetAsmPtr = 0x200, refManifest = 0x9001, targetModPtr = 0x4000;
        ModuleHandle refHandle = Mod(0x1000), targetHandle = Mod(0x2000);

        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetModuleHandleFromAssemblyPtr(Ptr(refAsmPtr))).Returns(refHandle);
        loader.Setup(l => l.GetLookupTables(refHandle)).Returns(Tables(refManifest));
        SetupTypeRefCacheMiss(loader);
        SetupLookupMap(loader, refManifest, MdtAssemblyRef | 1, targetModPtr);
        loader.Setup(l => l.GetModuleHandleFromModulePtr(Ptr(targetModPtr))).Returns(targetHandle);
        loader.Setup(l => l.GetAssembly(targetHandle)).Returns(Ptr(targetAsmPtr));

        Mock<IEcmaMetadata> ecma = new(MockBehavior.Strict);
        ecma.Setup(e => e.GetMetadata(refHandle)).Returns(refReader);
        ecma.Setup(e => e.GetMetadata(targetHandle)).Returns(targetReader);

        DacDbiImpl dacDbi = CreateDacDbiWithMockContracts(arch, loader, ecma);

        DacDbiTypeRefData input = new() { vmAssembly = refAsmPtr, typeToken = MdtTypeRef | 1 };
        DacDbiTypeRefData output = default;
        int hr = dacDbi.ResolveTypeReference(&input, &output);

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(targetAsmPtr, output.vmAssembly);
        Assert.Equal(0x02000002u, output.typeToken);

        System.GC.KeepAlive(refProvider);
        System.GC.KeepAlive(targetProvider);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ResolveTypeReference_TypeRef_TypeForwarder_FollowsExportedType(MockTarget.Architecture arch)
    {
        // Referencing module: AssemblyRef "A" and TypeRef "NS.Bar" scoped to A.
        var (refReader, refProvider) = BuildMetadata(mb =>
        {
            AssemblyReferenceHandle asmRefA = mb.AddAssemblyReference(mb.GetOrAddString("A"), new Version(1, 0, 0, 0), default, default, default, default);
            mb.AddTypeReference(asmRefA, mb.GetOrAddString("NS"), mb.GetOrAddString("Bar"));
        });

        // Module A: forwards "NS.Bar" to AssemblyRef "B" via an ExportedType (no TypeDef).
        var (readerA, providerA) = BuildMetadata(mb =>
        {
            AssemblyReferenceHandle asmRefB = mb.AddAssemblyReference(mb.GetOrAddString("B"), new Version(1, 0, 0, 0), default, default, default, default);
            mb.AddExportedType(TypeAttributes.Public, mb.GetOrAddString("NS"), mb.GetOrAddString("Bar"), asmRefB, 0);
        });

        // Module B: defines TypeDef "NS.Bar" (row 2 -> token 0x02000002).
        var (readerB, providerB) = BuildMetadata(mb => AddTypeDef(mb, "NS", "Bar"));

        const ulong refAsmPtr = 0x100, asmBPtr = 0x300;
        const ulong refManifest = 0x9001, manifestA = 0x9002, modAPtr = 0x4000, modBPtr = 0x5000;
        ModuleHandle refHandle = Mod(0x1000), handleA = Mod(0x2000), handleB = Mod(0x3000);

        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetModuleHandleFromAssemblyPtr(Ptr(refAsmPtr))).Returns(refHandle);
        loader.Setup(l => l.GetLookupTables(refHandle)).Returns(Tables(refManifest));
        loader.Setup(l => l.GetLookupTables(handleA)).Returns(Tables(manifestA));
        SetupTypeRefCacheMiss(loader);
        SetupLookupMap(loader, refManifest, MdtAssemblyRef | 1, modAPtr);
        SetupLookupMap(loader, manifestA, MdtAssemblyRef | 1, modBPtr);
        loader.Setup(l => l.GetModuleHandleFromModulePtr(Ptr(modAPtr))).Returns(handleA);
        loader.Setup(l => l.GetModuleHandleFromModulePtr(Ptr(modBPtr))).Returns(handleB);
        loader.Setup(l => l.GetAssembly(handleB)).Returns(Ptr(asmBPtr));

        Mock<IEcmaMetadata> ecma = new(MockBehavior.Strict);
        ecma.Setup(e => e.GetMetadata(refHandle)).Returns(refReader);
        ecma.Setup(e => e.GetMetadata(handleA)).Returns(readerA);
        ecma.Setup(e => e.GetMetadata(handleB)).Returns(readerB);

        DacDbiImpl dacDbi = CreateDacDbiWithMockContracts(arch, loader, ecma);

        DacDbiTypeRefData input = new() { vmAssembly = refAsmPtr, typeToken = MdtTypeRef | 1 };
        DacDbiTypeRefData output = default;
        int hr = dacDbi.ResolveTypeReference(&input, &output);

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(asmBPtr, output.vmAssembly);
        Assert.Equal(0x02000002u, output.typeToken);

        System.GC.KeepAlive(refProvider);
        System.GC.KeepAlive(providerA);
        System.GC.KeepAlive(providerB);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ResolveTypeReference_TypeRef_NestedType_ResolvesNestedTypeDef(MockTarget.Architecture arch)
    {
        // Referencing module: AssemblyRef "T", TypeRef "NS.Outer" scoped to T, and TypeRef "Inner"
        // whose scope is the "NS.Outer" TypeRef (nested reference).
        TypeReferenceHandle innerRefHandle = default;
        var (refReader, refProvider) = BuildMetadata(mb =>
        {
            AssemblyReferenceHandle asmRefT = mb.AddAssemblyReference(mb.GetOrAddString("T"), new Version(1, 0, 0, 0), default, default, default, default);
            TypeReferenceHandle outerRef = mb.AddTypeReference(asmRefT, mb.GetOrAddString("NS"), mb.GetOrAddString("Outer"));
            innerRefHandle = mb.AddTypeReference(outerRef, default, mb.GetOrAddString("Inner"));
        });

        // Target module: TypeDef "NS.Outer" (row 2) with nested TypeDef "Inner" (row 3 -> 0x02000003).
        var (targetReader, targetProvider) = BuildMetadata(mb =>
        {
            TypeDefinitionHandle outer = AddTypeDef(mb, "NS", "Outer");
            TypeDefinitionHandle inner = mb.AddTypeDefinition(
                TypeAttributes.NestedPublic | TypeAttributes.Class, default, mb.GetOrAddString("Inner"), default,
                MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
            mb.AddNestedType(inner, outer);
        });

        const ulong refAsmPtr = 0x100, targetAsmPtr = 0x200, refManifest = 0x9001, targetModPtr = 0x4000;
        ModuleHandle refHandle = Mod(0x1000), targetHandle = Mod(0x2000);
        uint innerToken = (uint)MetadataTokens.GetToken(innerRefHandle);

        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetModuleHandleFromAssemblyPtr(Ptr(refAsmPtr))).Returns(refHandle);
        loader.Setup(l => l.GetLookupTables(refHandle)).Returns(Tables(refManifest));
        SetupTypeRefCacheMiss(loader);
        SetupLookupMap(loader, refManifest, MdtAssemblyRef | 1, targetModPtr);
        loader.Setup(l => l.GetModuleHandleFromModulePtr(Ptr(targetModPtr))).Returns(targetHandle);
        loader.Setup(l => l.GetAssembly(targetHandle)).Returns(Ptr(targetAsmPtr));

        Mock<IEcmaMetadata> ecma = new(MockBehavior.Strict);
        ecma.Setup(e => e.GetMetadata(refHandle)).Returns(refReader);
        ecma.Setup(e => e.GetMetadata(targetHandle)).Returns(targetReader);

        DacDbiImpl dacDbi = CreateDacDbiWithMockContracts(arch, loader, ecma);

        DacDbiTypeRefData input = new() { vmAssembly = refAsmPtr, typeToken = innerToken };
        DacDbiTypeRefData output = default;
        int hr = dacDbi.ResolveTypeReference(&input, &output);

        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(targetAsmPtr, output.vmAssembly);
        Assert.Equal(0x02000003u, output.typeToken);

        System.GC.KeepAlive(refProvider);
        System.GC.KeepAlive(targetProvider);
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
            int hr = dacDbi.CheckContext(ThreadAddr, pCtx);
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
            int hr = dacDbi.CheckContext(ThreadAddr, pCtx);
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

    private delegate void TryGetInstrumentedILMapCallback(ILCodeVersionHandle handle, out uint mapEntryCount, out TargetPointer mapEntries);

    public static IEnumerable<object[]> GetILCodeVersionNodeDataValues()
    {
        foreach (object[] stdArch in new MockTarget.StdArch())
        {
            // arch, pbIL, hasMap, mapCount, mapEntries
            yield return [stdArch[0], 0x9000ul, true, 4u, 0xA000ul];
            yield return [stdArch[0], 0x9000ul, false, 0u, 0ul];
        }
    }

    [Theory]
    [MemberData(nameof(GetILCodeVersionNodeDataValues))]
    public void GetILCodeVersionNodeData_FillsData(MockTarget.Architecture arch, ulong pbIL, bool hasMap, uint mapCount, ulong mapEntries)
    {
        TargetPointer ilCodeVersionNode = new(0x3000);
        var ilCodeVersion = ILCodeVersionHandle.CreateExplicit(ilCodeVersionNode);

        var mockCodeVersions = new Mock<ICodeVersions>();
        mockCodeVersions.Setup(cv => cv.GetIL(ilCodeVersion)).Returns(new TargetPointer(pbIL));
        mockCodeVersions
            .Setup(cv => cv.TryGetInstrumentedILMap(ilCodeVersion, out It.Ref<uint>.IsAny, out It.Ref<TargetPointer>.IsAny))
            .Callback(new TryGetInstrumentedILMapCallback((ILCodeVersionHandle _, out uint count, out TargetPointer entries) =>
            {
                count = mapCount;
                entries = new TargetPointer(mapEntries);
            }))
            .Returns(hasMap);

        var dacDbi = CreateDacDbiWithMockContracts(arch, new Mock<ILoader>(), mockCodeVersions, new Mock<IReJIT>());

        DacDbiSharedReJitInfo data;
        int hr = dacDbi.GetILCodeVersionNodeData(ilCodeVersionNode.Value, &data);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(pbIL, data.pbIL);
        Assert.Equal(mapCount, data.cInstrumentedMapEntries);
        Assert.Equal(mapEntries, data.rgInstrumentedMapEntries);
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

    [Theory]
    [InlineData(DebugVarLocKind.Register, false, false, VarLocType.VLT_REG)]
    [InlineData(DebugVarLocKind.Register, false, true, VarLocType.VLT_REG_FP)]
    [InlineData(DebugVarLocKind.Register, true, false, VarLocType.VLT_REG_BYREF)]
    [InlineData(DebugVarLocKind.Stack, false, false, VarLocType.VLT_STK)]
    [InlineData(DebugVarLocKind.Stack, true, false, VarLocType.VLT_STK_BYREF)]
    [InlineData(DebugVarLocKind.RegisterRegister, false, false, VarLocType.VLT_REG_REG)]
    [InlineData(DebugVarLocKind.RegisterStack, false, false, VarLocType.VLT_REG_STK)]
    [InlineData(DebugVarLocKind.StackRegister, false, false, VarLocType.VLT_STK_REG)]
    [InlineData(DebugVarLocKind.DoubleStack, false, false, VarLocType.VLT_STK2)]
    public void ConvertToVarLoc_MapsVarLocTypeCorrectly(DebugVarLocKind kind, bool isByRef, bool isFloatingPoint, VarLocType expected)
    {
        var varInfo = new DebugVarInfo { Kind = kind, IsByRef = isByRef, IsFloatingPoint = isFloatingPoint };
        VarLoc result = DacDbiImpl.ConvertToVarLoc(varInfo);
        Assert.Equal(expected, result.vlType);
    }

    [Fact]
    public void ConvertToVarLoc_Register_SetsRegisterField()
    {
        var varInfo = new DebugVarInfo { Kind = DebugVarLocKind.Register, Register = 7 };
        VarLoc result = DacDbiImpl.ConvertToVarLoc(varInfo);
        Assert.Equal(7u, result.vlrReg);
    }

    [Fact]
    public void ConvertToVarLoc_RegisterFP_SetsRegisterField()
    {
        var varInfo = new DebugVarInfo { Kind = DebugVarLocKind.Register, IsFloatingPoint = true, Register = 9 };
        VarLoc result = DacDbiImpl.ConvertToVarLoc(varInfo);
        Assert.Equal(VarLocType.VLT_REG_FP, result.vlType);
        Assert.Equal(9u, result.vlrReg);
    }

    [Fact]
    public void ConvertToVarLoc_Stack_SetsBaseRegAndOffset()
    {
        var varInfo = new DebugVarInfo { Kind = DebugVarLocKind.Stack, BaseRegister = 5, StackOffset = -0x28 };
        VarLoc result = DacDbiImpl.ConvertToVarLoc(varInfo);
        Assert.Equal(5u, result.vlsBaseReg);
        Assert.Equal(-0x28, result.vlsOffset);
    }

    [Fact]
    public void ConvertToVarLoc_RegisterRegister_SetsBothRegisters()
    {
        var varInfo = new DebugVarInfo { Kind = DebugVarLocKind.RegisterRegister, Register = 3, Register2 = 4 };
        VarLoc result = DacDbiImpl.ConvertToVarLoc(varInfo);
        Assert.Equal(3u, result.vlrrReg1);
        Assert.Equal(4u, result.vlrrReg2);
    }

    [Fact]
    public void ConvertToVarLoc_RegisterStack_SetsRegAndStack()
    {
        var varInfo = new DebugVarInfo { Kind = DebugVarLocKind.RegisterStack, Register = 2, BaseRegister2 = 6, StackOffset2 = 0x10 };
        VarLoc result = DacDbiImpl.ConvertToVarLoc(varInfo);
        Assert.Equal(2u, result.vlrsReg);
        Assert.Equal(6u, result.vlrssBaseReg);
        Assert.Equal(0x10, result.vlrssOffset);
    }

    [Fact]
    public void ConvertToVarLoc_StackRegister_SetsStackAndReg()
    {
        var varInfo = new DebugVarInfo { Kind = DebugVarLocKind.StackRegister, BaseRegister = 5, StackOffset = -8, Register = 1 };
        VarLoc result = DacDbiImpl.ConvertToVarLoc(varInfo);
        Assert.Equal(5u, result.vlsrsBaseReg);
        Assert.Equal(-8, result.vlsrsOffset);
        Assert.Equal(1u, result.vlsrReg);
    }

    [Fact]
    public void ConvertToVarLoc_DoubleStack_SetsBaseRegAndOffset()
    {
        var varInfo = new DebugVarInfo { Kind = DebugVarLocKind.DoubleStack, BaseRegister = 4, StackOffset = 0x20 };
        VarLoc result = DacDbiImpl.ConvertToVarLoc(varInfo);
        Assert.Equal(VarLocType.VLT_STK2, result.vlType);
        Assert.Equal(4u, result.vlsBaseReg);
        Assert.Equal(0x20, result.vlsOffset);
    }

    [Theory]
    [InlineData(SourceTypes.Default, 0x00u)]
    [InlineData(SourceTypes.StackEmpty, 0x02u)]
    [InlineData(SourceTypes.CallInstruction, 0x10u)]
    [InlineData(SourceTypes.Async, 0x20u)]
    [InlineData(SourceTypes.StackEmpty | SourceTypes.CallInstruction, 0x12u)]
    [InlineData(SourceTypes.StackEmpty | SourceTypes.CallInstruction | SourceTypes.Async, 0x32u)]
    public void ConvertSourceTypesToNative_MapsCorrectly(SourceTypes source, uint expected)
    {
        DbiSourceTypes result = DacDbiImpl.ConvertSourceTypesToNative(source);
        Assert.Equal(expected, (uint)result);
    }

    [Fact]
    public void ConvertToNativeVarInfo_MapsAllFields()
    {
        var varInfo = new DebugVarInfo
        {
            Kind = DebugVarLocKind.Stack,
            IsByRef = false,
            StartOffset = 10,
            EndOffset = 50,
            CallReturnValueILOffset = 42,
            VarNumber = 3,
            BaseRegister = 5,
            StackOffset = -0x28,
        };
        NativeVarInfo nvi = DacDbiImpl.ConvertToNativeVarInfo(varInfo);
        Assert.Equal(10u, nvi.startOffset);
        Assert.Equal(50u, nvi.endOffset);
        Assert.Equal(42u, nvi.callReturnValueILOffset);
        Assert.Equal(3u, nvi.varNumber);
        Assert.Equal(VarLocType.VLT_STK, nvi.loc.vlType);
        Assert.Equal(5u, nvi.loc.vlsBaseReg);
        Assert.Equal(-0x28, nvi.loc.vlsOffset);
    }
}
