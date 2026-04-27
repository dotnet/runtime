// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class LoaderTests
{
    private const uint IsJitOptimizationDisabled = 0x00000002;
    private const uint IsEditAndContinue = 0x00000008;
    private const uint IsEncCapable = 0x00000200;
    private const uint DebuggerAllowJitOptsPriv = 0x00000800;
    private const uint DebuggerEncEnabledPriv = 0x00002000;

    internal static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockLoaderBuilder loader)
        => new()
        {
            [DataType.Module] = TargetTestHelpers.CreateTypeInfo(loader.ModuleLayout),
            [DataType.Assembly] = TargetTestHelpers.CreateTypeInfo(loader.AssemblyLayout),
            [DataType.EEConfig] = TargetTestHelpers.CreateTypeInfo(loader.EEConfigLayout),
        };

    private static ILoader CreateLoaderContract(MockTarget.Architecture arch, Action<MockLoaderBuilder> configure)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockLoaderBuilder loader = new(targetBuilder.MemoryBuilder);

        configure(loader);

        var target = targetBuilder
            .AddTypes(CreateContractTypes(loader))
            .AddContract<ILoader>(version: "c1")
            .Build();
        return target.Contracts.Loader;
    }

    internal static (ILoader Contract, TestPlaceholderTarget Target) CreateLoaderContractWithTarget(
        MockTarget.Architecture arch,
        Action<MockLoaderBuilder, TestPlaceholderTarget.Builder> configure)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockLoaderBuilder loader = new(targetBuilder.MemoryBuilder);

        configure(loader, targetBuilder);

        targetBuilder.AddTypes(CreateContractTypes(loader));
        targetBuilder.AddContract<ILoader>(version: "c1");
        var target = targetBuilder.Build();
        return (target.Contracts.Loader, target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetPath(MockTarget.Architecture arch)
    {
        string expected = $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}TestModule.dll";
        TargetPointer moduleAddr = TargetPointer.Null;
        TargetPointer moduleAddrEmptyPath = TargetPointer.Null;

        ILoader contract = CreateLoaderContract(arch, loader =>
        {
            moduleAddr = loader.AddModule(path: expected).Address;
            moduleAddrEmptyPath = loader.AddModule().Address;
        });

        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
            string actual = contract.GetPath(handle);
            Assert.Equal(expected, actual);
        }
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddrEmptyPath);
            string actual = contract.GetFileName(handle);
            Assert.Equal(string.Empty, actual);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFileName(MockTarget.Architecture arch)
    {
        string expected = $"TestModule.dll";
        TargetPointer moduleAddr = TargetPointer.Null;
        TargetPointer moduleAddrEmptyName = TargetPointer.Null;

        ILoader contract = CreateLoaderContract(arch, loader =>
        {
            moduleAddr = loader.AddModule(fileName: expected).Address;
            moduleAddrEmptyName = loader.AddModule().Address;
        });

        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
            string actual = contract.GetFileName(handle);
            Assert.Equal(expected, actual);
        }
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddrEmptyName);
            string actual = contract.GetFileName(handle);
            Assert.Equal(string.Empty, actual);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetSimpleName(MockTarget.Architecture arch)
    {
        string expected = "TestModule";
        TargetPointer moduleAddr = TargetPointer.Null;
        TargetPointer moduleAddrEmptyName = TargetPointer.Null;

        ILoader contract = CreateLoaderContract(arch, loader =>
        {
            moduleAddr = loader.AddModule(simpleName: expected).Address;
            moduleAddrEmptyName = loader.AddModule().Address;
        });

        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
            bool result = contract.TryGetSimpleName(handle, out string actual);
            Assert.True(result);
            Assert.Equal(expected, actual);
        }
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddrEmptyName);
            bool result = contract.TryGetSimpleName(handle, out string actual);
            Assert.False(result);
            Assert.Equal(string.Empty, actual);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetSimpleName_InvalidUtf8(MockTarget.Architecture arch)
    {
        // 0xFF is not valid UTF-8
        byte[] invalidUtf8 = [0xFF, 0xFE];
        TargetPointer moduleAddr = TargetPointer.Null;
        ILoader contract = CreateLoaderContract(arch, loader =>
        {
            moduleAddr = loader.AddModule(simpleNameBytes: invalidUtf8).Address;
        });

        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
        Assert.Throws<DecoderFallbackException>(() => contract.TryGetSimpleName(handle, out _));
    }

    private static readonly Dictionary<string, TargetPointer> MockHeapDictionary = new()
    {
        ["LowFrequencyHeap"] = new(0x1000),
        ["HighFrequencyHeap"] = new(0x2000),
        ["StaticsHeap"] = new(0x3000),
        ["StubHeap"] = new(0x4000),
        ["ExecutableHeap"] = new(0x5000),
        ["FixupPrecodeHeap"] = new(0x6000),
        ["NewStubPrecodeHeap"] = new(0x7000),
        ["IndcellHeap"] = new(0x8000),
        ["CacheEntryHeap"] = new(0x9000),
    };

    private static ISOSDacInterface13 CreateSOSDacInterface13ForHeapTests(MockTarget.Architecture arch)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockLoaderBuilder loader = new(targetBuilder.MemoryBuilder);
        var types = new Dictionary<DataType, Target.TypeInfo>(CreateContractTypes(loader));

        // Register LoaderAllocator and VirtualCallStubManager type infos so that
        // GetCanonicalHeapNameEntries() can determine which heap names exist.
        var dummyField = new Target.FieldInfo { Offset = 0 };
        types[DataType.LoaderAllocator] = new Target.TypeInfo
        {
            Fields = new Dictionary<string, Target.FieldInfo>
            {
                ["LowFrequencyHeap"] = dummyField,
                ["HighFrequencyHeap"] = dummyField,
                ["StaticsHeap"] = dummyField,
                ["StubHeap"] = dummyField,
                ["ExecutableHeap"] = dummyField,
                ["FixupPrecodeHeap"] = dummyField,
                ["NewStubPrecodeHeap"] = dummyField,
            }
        };
        types[DataType.VirtualCallStubManager] = new Target.TypeInfo
        {
            Fields = new Dictionary<string, Target.FieldInfo>
            {
                ["IndcellHeap"] = dummyField,
                ["CacheEntryHeap"] = dummyField,
            }
        };

        var target = targetBuilder
            .AddTypes(types)
            .AddMockContract<ILoader>(Mock.Of<ILoader>(
                l => l.GetLoaderAllocatorHeaps(It.IsAny<TargetPointer>()) == (IReadOnlyDictionary<string, TargetPointer>)MockHeapDictionary
                && l.GetGlobalLoaderAllocator() == new TargetPointer(0x100)))
            .Build();
        return new SOSDacImpl(target, null);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeapNames_GetCount(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacInterface13ForHeapTests(arch);

        int needed;
        int hr = impl.GetLoaderAllocatorHeapNames(0, null, &needed);

        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeapNames_GetNames(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacInterface13ForHeapTests(arch);

        int needed;
        int hr = impl.GetLoaderAllocatorHeapNames(0, null, &needed);
        Assert.Equal(MockHeapDictionary.Count, needed);

        char** names = stackalloc char*[needed];
        hr = impl.GetLoaderAllocatorHeapNames(needed, names, &needed);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
        HashSet<string> expectedNames = new(MockHeapDictionary.Keys);
        for (int i = 0; i < needed; i++)
        {
            string actual = Marshal.PtrToStringAnsi((nint)names[i])!;
            Assert.Contains(actual, expectedNames);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeapNames_InsufficientBuffer(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacInterface13ForHeapTests(arch);

        int needed;
        char** names = stackalloc char*[2];
        int hr = impl.GetLoaderAllocatorHeapNames(2, names, &needed);

        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
        HashSet<string> expectedNames = new(MockHeapDictionary.Keys);
        for (int i = 0; i < 2; i++)
        {
            string actual = Marshal.PtrToStringAnsi((nint)names[i])!;
            Assert.Contains(actual, expectedNames);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeapNames_NullPNeeded(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacInterface13ForHeapTests(arch);

        int hr = impl.GetLoaderAllocatorHeapNames(0, null, null);
        Assert.Equal(HResults.S_FALSE, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeaps_GetCount(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacInterface13ForHeapTests(arch);

        int needed;
        int hr = impl.GetLoaderAllocatorHeaps(new ClrDataAddress(0x100), 0, null, null, &needed);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeaps_GetHeaps(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacInterface13ForHeapTests(arch);

        int needed;
        impl.GetLoaderAllocatorHeapNames(0, null, &needed);

        char** names = stackalloc char*[needed];
        impl.GetLoaderAllocatorHeapNames(needed, names, &needed);

        ClrDataAddress* heaps = stackalloc ClrDataAddress[needed];
        int* kinds = stackalloc int[needed];
        int hr = impl.GetLoaderAllocatorHeaps(new ClrDataAddress(0x100), needed, heaps, kinds, &needed);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
        for (int i = 0; i < needed; i++)
        {
            string name = Marshal.PtrToStringAnsi((nint)names[i])!;
            Assert.Equal((ulong)MockHeapDictionary[name], (ulong)heaps[i]);
            Assert.Equal(0, kinds[i]); // LoaderHeapKindNormal
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeaps_InsufficientBuffer(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacInterface13ForHeapTests(arch);

        ClrDataAddress* heaps = stackalloc ClrDataAddress[2];
        int* kinds = stackalloc int[2];
        int needed;
        int hr = impl.GetLoaderAllocatorHeaps(new ClrDataAddress(0x100), 2, heaps, kinds, &needed);

        Assert.Equal(HResults.E_INVALIDARG, hr);
        Assert.Equal(MockHeapDictionary.Count, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLoaderAllocatorHeaps_NullAddress(MockTarget.Architecture arch)
    {
        ISOSDacInterface13 impl = CreateSOSDacInterface13ForHeapTests(arch);

        int hr = impl.GetLoaderAllocatorHeaps(new ClrDataAddress(0), 0, null, null, null);

        Assert.Equal(HResults.E_INVALIDARG, hr);
    }

    private readonly record struct SectionDef(uint VirtualSize, uint VirtualAddress, uint SizeOfRawData, uint PointerToRawData);

    private static (TestPlaceholderTarget Target, TargetPointer PEAssemblyAddr, TargetPointer ImageBase) CreateWebcilTarget(
        MockTarget.Architecture arch,
        ushort coffSections,
        SectionDef[] sections,
        ushort versionMajor = 0)
    {
        TargetTestHelpers helpers = new(arch);
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.Builder builder = targetBuilder.MemoryBuilder;
        var allocator = builder.CreateAllocator(0x0010_0000, 0x0020_0000);

        var probeExtLayout = helpers.LayoutFields([
            new(nameof(Data.ProbeExtensionResult.Type), DataType.int32),
        ]);
        var peAssemblyLayout = helpers.LayoutFields([
            new(nameof(Data.PEAssembly.PEImage), DataType.pointer),
            new(nameof(Data.PEAssembly.AssemblyBinder), DataType.pointer),
        ]);
        var peImageLayout = helpers.LayoutFields([
            new(nameof(Data.PEImage.LoadedImageLayout), DataType.pointer),
            new(nameof(Data.PEImage.ProbeExtensionResult), DataType.ProbeExtensionResult, probeExtLayout.Stride),
        ]);
        var imageLayoutLayout = helpers.LayoutFields([
            new(nameof(Data.PEImageLayout.Base), DataType.pointer),
            new(nameof(Data.PEImageLayout.Size), DataType.uint32),
            new(nameof(Data.PEImageLayout.Flags), DataType.uint32),
            new(nameof(Data.PEImageLayout.Format), DataType.uint32),
        ]);

        List<TargetTestHelpers.Field> webcilHeaderFields =
        [
            new("Id_0", DataType.uint8),
            new("Id_1", DataType.uint8),
            new("Id_2", DataType.uint8),
            new("Id_3", DataType.uint8),
            new("VersionMajor", DataType.uint16),
            new("VersionMinor", DataType.uint16),
            new(nameof(Data.WebcilHeader.CoffSections), DataType.uint16),
            new("Reserved0", DataType.uint16),
            new("PeCliHeaderRva", DataType.uint32),
            new("PeCliHeaderSize", DataType.uint32),
            new("PeDebugRva", DataType.uint32),
            new("PeDebugSize", DataType.uint32),
        ];
        if (versionMajor >= 1)
        {
            webcilHeaderFields.Add(new("TableBase", DataType.uint32));
        }
        var webcilHeaderLayout = helpers.LayoutFields(webcilHeaderFields.ToArray());
        var webcilSectionLayout = helpers.LayoutFields([
            new(nameof(Data.WebcilSectionHeader.VirtualSize), DataType.uint32),
            new(nameof(Data.WebcilSectionHeader.VirtualAddress), DataType.uint32),
            new(nameof(Data.WebcilSectionHeader.SizeOfRawData), DataType.uint32),
            new(nameof(Data.WebcilSectionHeader.PointerToRawData), DataType.uint32),
        ]);

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.PEAssembly] = new() { Fields = peAssemblyLayout.Fields, Size = peAssemblyLayout.Stride },
            [DataType.PEImage] = new() { Fields = peImageLayout.Fields, Size = peImageLayout.Stride },
            [DataType.PEImageLayout] = new() { Fields = imageLayoutLayout.Fields, Size = imageLayoutLayout.Stride },
            [DataType.ProbeExtensionResult] = new() { Fields = probeExtLayout.Fields, Size = probeExtLayout.Stride },
            [DataType.WebcilHeader] = new() { Fields = webcilHeaderLayout.Fields, Size = webcilHeaderLayout.Stride },
            [DataType.WebcilSectionHeader] = new() { Fields = webcilSectionLayout.Fields, Size = webcilSectionLayout.Stride },
        };

        uint headerStride = webcilHeaderLayout.Stride;
        uint sectionStride = webcilSectionLayout.Stride;
        uint webcilImageSize = headerStride + sectionStride * (uint)sections.Length;
        var webcilImage = allocator.Allocate(webcilImageSize, "WebcilImage");

        helpers.Write(
            webcilImage.Data.AsSpan().Slice(webcilHeaderLayout.Fields["VersionMajor"].Offset, sizeof(ushort)),
            versionMajor);
        helpers.Write(
            webcilImage.Data.AsSpan().Slice(webcilHeaderLayout.Fields[nameof(Data.WebcilHeader.CoffSections)].Offset, sizeof(ushort)),
            coffSections);

        for (int i = 0; i < sections.Length; i++)
        {
            int baseOffset = (int)headerStride + i * (int)sectionStride;
            var sf = webcilSectionLayout.Fields;
            helpers.Write(webcilImage.Data.AsSpan().Slice(baseOffset + sf[nameof(Data.WebcilSectionHeader.VirtualSize)].Offset, sizeof(uint)), sections[i].VirtualSize);
            helpers.Write(webcilImage.Data.AsSpan().Slice(baseOffset + sf[nameof(Data.WebcilSectionHeader.VirtualAddress)].Offset, sizeof(uint)), sections[i].VirtualAddress);
            helpers.Write(webcilImage.Data.AsSpan().Slice(baseOffset + sf[nameof(Data.WebcilSectionHeader.SizeOfRawData)].Offset, sizeof(uint)), sections[i].SizeOfRawData);
            helpers.Write(webcilImage.Data.AsSpan().Slice(baseOffset + sf[nameof(Data.WebcilSectionHeader.PointerToRawData)].Offset, sizeof(uint)), sections[i].PointerToRawData);
        }

        var layoutFrag = allocator.Allocate(imageLayoutLayout.Stride, "PEImageLayout");
        helpers.WritePointer(layoutFrag.Data.AsSpan().Slice(imageLayoutLayout.Fields[nameof(Data.PEImageLayout.Base)].Offset, helpers.PointerSize), webcilImage.Address);
        helpers.Write(layoutFrag.Data.AsSpan().Slice(imageLayoutLayout.Fields[nameof(Data.PEImageLayout.Size)].Offset, sizeof(uint)), webcilImageSize);
        helpers.Write(layoutFrag.Data.AsSpan().Slice(imageLayoutLayout.Fields[nameof(Data.PEImageLayout.Flags)].Offset, sizeof(uint)), 0u);
        helpers.Write(layoutFrag.Data.AsSpan().Slice(imageLayoutLayout.Fields[nameof(Data.PEImageLayout.Format)].Offset, sizeof(uint)), 1u);

        var peImageFrag = allocator.Allocate(peImageLayout.Stride, "PEImage");
        helpers.WritePointer(peImageFrag.Data.AsSpan().Slice(peImageLayout.Fields[nameof(Data.PEImage.LoadedImageLayout)].Offset, helpers.PointerSize), layoutFrag.Address);

        var peAssemblyFrag = allocator.Allocate(peAssemblyLayout.Stride, "PEAssembly");
        helpers.WritePointer(peAssemblyFrag.Data.AsSpan().Slice(peAssemblyLayout.Fields[nameof(Data.PEAssembly.PEImage)].Offset, helpers.PointerSize), peImageFrag.Address);

        var target = targetBuilder
            .AddTypes(types)
            .AddContract<ILoader>(version: "c1")
            .Build();

        return (target, new TargetPointer(peAssemblyFrag.Address), new TargetPointer(webcilImage.Address));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetILAddr_WebcilRvaToOffset(MockTarget.Architecture arch)
    {
        SectionDef[] sections =
        [
            new(VirtualSize: 0x2000, VirtualAddress: 0x1000, SizeOfRawData: 0x2000, PointerToRawData: 0x200),
            new(VirtualSize: 0x1000, VirtualAddress: 0x4000, SizeOfRawData: 0x1000, PointerToRawData: 0x2200),
        ];
        var (target, peAssemblyAddr, imageBase) = CreateWebcilTarget(arch, (ushort)sections.Length, sections);
        ILoader contract = target.Contracts.Loader;

        // RVA in first section: offset = (0x1100 - 0x1000) + 0x200 = 0x300
        Assert.Equal((TargetPointer)(imageBase + 0x300u), contract.GetILAddr(peAssemblyAddr, 0x1100));

        // RVA at start of first section: offset = (0x1000 - 0x1000) + 0x200 = 0x200
        Assert.Equal((TargetPointer)(imageBase + 0x200u), contract.GetILAddr(peAssemblyAddr, 0x1000));

        // RVA in second section: offset = (0x4500 - 0x4000) + 0x2200 = 0x2700
        Assert.Equal((TargetPointer)(imageBase + 0x2700u), contract.GetILAddr(peAssemblyAddr, 0x4500));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetILAddr_WebcilNegativeRvaThrows(MockTarget.Architecture arch)
    {
        SectionDef[] sections =
        [
            new(VirtualSize: 0x2000, VirtualAddress: 0x1000, SizeOfRawData: 0x2000, PointerToRawData: 0x200),
        ];
        var (target, peAssemblyAddr, _) = CreateWebcilTarget(arch, 1, sections);
        ILoader contract = target.Contracts.Loader;

        Assert.Throws<InvalidOperationException>(() => contract.GetILAddr(peAssemblyAddr, -1));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetILAddr_WebcilInvalidSectionCountThrows(MockTarget.Architecture arch)
    {
        var (targetZero, addrZero, _) = CreateWebcilTarget(arch, coffSections: 0, []);
        Assert.Throws<InvalidOperationException>(() => targetZero.Contracts.Loader.GetILAddr(addrZero, 0x1000));

        var (targetExcessive, addrExcessive, _) = CreateWebcilTarget(arch, coffSections: 17, []);
        Assert.Throws<InvalidOperationException>(() => targetExcessive.Contracts.Loader.GetILAddr(addrExcessive, 0x1000));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetILAddr_WebcilRvaNotInAnySectionThrows(MockTarget.Architecture arch)
    {
        SectionDef[] sections =
        [
            new(VirtualSize: 0x1000, VirtualAddress: 0x1000, SizeOfRawData: 0x1000, PointerToRawData: 0x200),
        ];
        var (target, peAssemblyAddr, _) = CreateWebcilTarget(arch, 1, sections);
        ILoader contract = target.Contracts.Loader;

        Assert.Throws<InvalidOperationException>(() => contract.GetILAddr(peAssemblyAddr, 0x5000));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetILAddr_WebcilV1RvaToOffset(MockTarget.Architecture arch)
    {
        SectionDef[] sections =
        [
            new(VirtualSize: 0x2000, VirtualAddress: 0x1000, SizeOfRawData: 0x2000, PointerToRawData: 0x200),
            new(VirtualSize: 0x1000, VirtualAddress: 0x4000, SizeOfRawData: 0x1000, PointerToRawData: 0x2200),
        ];
        var (target, peAssemblyAddr, imageBase) = CreateWebcilTarget(arch, (ushort)sections.Length, sections, versionMajor: 1);
        ILoader contract = target.Contracts.Loader;

        // RVA in first section: offset = (0x1100 - 0x1000) + 0x200 = 0x300
        Assert.Equal((TargetPointer)(imageBase + 0x300u), contract.GetILAddr(peAssemblyAddr, 0x1100));

        // RVA at start of first section: offset = (0x1000 - 0x1000) + 0x200 = 0x200
        Assert.Equal((TargetPointer)(imageBase + 0x200u), contract.GetILAddr(peAssemblyAddr, 0x1000));

        // RVA in second section: offset = (0x4500 - 0x4000) + 0x2200 = 0x2700
        Assert.Equal((TargetPointer)(imageBase + 0x2700u), contract.GetILAddr(peAssemblyAddr, 0x4500));
    }

    public static IEnumerable<object[]> IsModuleMappedData()
    {
        foreach (object[] archData in new MockTarget.StdArch())
        {
            var arch = (MockTarget.Architecture)archData[0];
            // PE format (0), FLAG_MAPPED (1) → true
            yield return [arch, 0u, 1u, true];
            // PE format (0), no flags → false
            yield return [arch, 0u, 0u, false];
            // Webcil format (1), FLAG_MAPPED set → still false
            yield return [arch, 1u, 1u, false];
            // Webcil format (1), no flags → false
            yield return [arch, 1u, 0u, false];
        }
    }

    public static IEnumerable<object[]> GetDebuggerInfoBitsData()
    {
        foreach (var arch in new MockTarget.StdArch())
        {
            yield return [0u, DebuggerAssemblyControlFlags.DACF_NONE, arch[0]];
            yield return [DebuggerAllowJitOptsPriv, DebuggerAssemblyControlFlags.DACF_ALLOW_JIT_OPTS, arch[0]];
            yield return [DebuggerEncEnabledPriv, DebuggerAssemblyControlFlags.DACF_ENC_ENABLED, arch[0]];
        }
    }

    [Theory]
    [MemberData(nameof(IsModuleMappedData))]
    public void IsModuleMapped_ReturnsExpected(MockTarget.Architecture arch, uint format, uint flags, bool expected)
    {
        TargetTestHelpers helpers = new(arch);
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.Builder builder = targetBuilder.MemoryBuilder;
        MockLoaderBuilder loader = new(builder);
        var allocator = builder.CreateAllocator(0x0010_0000, 0x0020_0000);

        MockLoaderModule module = loader.AddModule();

        var probeExtLayout = helpers.LayoutFields([
            new(nameof(Data.ProbeExtensionResult.Type), DataType.int32),
        ]);
        var peAssemblyLayout = helpers.LayoutFields([
            new(nameof(Data.PEAssembly.PEImage), DataType.pointer),
            new(nameof(Data.PEAssembly.AssemblyBinder), DataType.pointer),
        ]);
        var peImageLayout = helpers.LayoutFields([
            new(nameof(Data.PEImage.LoadedImageLayout), DataType.pointer),
            new(nameof(Data.PEImage.ProbeExtensionResult), DataType.ProbeExtensionResult, probeExtLayout.Stride),
        ]);
        var imageLayoutLayout = helpers.LayoutFields([
            new(nameof(Data.PEImageLayout.Base), DataType.pointer),
            new(nameof(Data.PEImageLayout.Size), DataType.uint32),
            new(nameof(Data.PEImageLayout.Flags), DataType.uint32),
            new(nameof(Data.PEImageLayout.Format), DataType.uint32),
        ]);

        MockMemorySpace.HeapFragment Allocate(uint size, string name)
        {
            MockMemorySpace.HeapFragment frag = allocator.Allocate(size, name);
            return frag;
        }

        var layoutFrag = Allocate(imageLayoutLayout.Stride, "PEImageLayout");
        helpers.Write(layoutFrag.Data.AsSpan().Slice(imageLayoutLayout.Fields[nameof(Data.PEImageLayout.Flags)].Offset, sizeof(uint)), flags);
        helpers.Write(layoutFrag.Data.AsSpan().Slice(imageLayoutLayout.Fields[nameof(Data.PEImageLayout.Format)].Offset, sizeof(uint)), format);

        var peImageFrag = Allocate(peImageLayout.Stride, "PEImage");
        helpers.WritePointer(peImageFrag.Data.AsSpan().Slice(peImageLayout.Fields[nameof(Data.PEImage.LoadedImageLayout)].Offset, helpers.PointerSize), layoutFrag.Address);

        var peAssemblyFrag = Allocate(peAssemblyLayout.Stride, "PEAssembly");
        helpers.WritePointer(peAssemblyFrag.Data.AsSpan().Slice(peAssemblyLayout.Fields[nameof(Data.PEAssembly.PEImage)].Offset, helpers.PointerSize), peImageFrag.Address);

        module.PEAssembly = peAssemblyFrag.Address;

        var types = CreateContractTypes(loader);
        types[DataType.PEAssembly] = new() { Fields = peAssemblyLayout.Fields, Size = peAssemblyLayout.Stride };
        types[DataType.PEImage] = new() { Fields = peImageLayout.Fields, Size = peImageLayout.Stride };
        types[DataType.PEImageLayout] = new() { Fields = imageLayoutLayout.Fields, Size = imageLayoutLayout.Stride };
        types[DataType.ProbeExtensionResult] = new() { Fields = probeExtLayout.Fields, Size = probeExtLayout.Stride };

        var target = targetBuilder
            .AddTypes(types)
            .AddContract<ILoader>(version: "c1")
            .Build();

        ILoader contract = target.Contracts.Loader;
        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(new TargetPointer(module.Address));
        Assert.Equal(expected, contract.IsModuleMapped(handle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsModuleMapped_NoPEAssembly_ReturnsFalse(MockTarget.Architecture arch)
    {
        TargetPointer moduleAddr = TargetPointer.Null;

        ILoader contract = CreateLoaderContract(arch, loader =>
        {
            moduleAddr = loader.AddModule().Address;
        });

        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
        Assert.False(contract.IsModuleMapped(handle));
    }

    [Theory]
    [MemberData(nameof(GetDebuggerInfoBitsData))]
    public void GetDebuggerInfoBits(uint rawFlags, DebuggerAssemblyControlFlags expectedBits, MockTarget.Architecture arch)
    {
        TargetPointer moduleAddr = TargetPointer.Null;

        ILoader contract = CreateLoaderContract(arch, loader =>
        {
            moduleAddr = loader.AddModule(flags: rawFlags).Address;
        });

        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
        DebuggerAssemblyControlFlags actual = contract.GetDebuggerInfoBits(handle);
        Assert.Equal(expectedBits, actual);
    }

    public static IEnumerable<object[]> SetDebuggerInfoBitsData()
    {
        foreach (var arch in new MockTarget.StdArch())
        {
            // IS_JIT_OPTIMIZATION_DISABLED is set when DACF_ALLOW_JIT_OPTS is absent
            yield return [DebuggerAssemblyControlFlags.DACF_NONE, IsJitOptimizationDisabled, arch[0]];
            yield return [DebuggerAssemblyControlFlags.DACF_ALLOW_JIT_OPTS, DebuggerAllowJitOptsPriv, arch[0]];
            yield return [DebuggerAssemblyControlFlags.DACF_ENC_ENABLED, DebuggerEncEnabledPriv | IsJitOptimizationDisabled, arch[0]];
        }
    }

    [Theory]
    [MemberData(nameof(SetDebuggerInfoBitsData))]
    public void SetDebuggerInfoBits(DebuggerAssemblyControlFlags newBits, uint expectedRawFlags, MockTarget.Architecture arch)
    {
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (contract, target) = CreateLoaderContractWithTarget(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.None);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            moduleAddr = loader.AddModule().Address;
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
        contract.SetDebuggerInfoBits(handle, newBits);

        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.Equal(expectedRawFlags, rawFlags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetDebuggerInfoBits_PreservesOtherFlags(MockTarget.Architecture arch)
    {
        uint initialFlags = (uint)(ModuleFlags.Tenured | ModuleFlags.ReflectionEmit);
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (contract, target) = CreateLoaderContractWithTarget(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.None);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            moduleAddr = loader.AddModule(flags: initialFlags).Address;
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);

        DebuggerAssemblyControlFlags debuggerBits = DebuggerAssemblyControlFlags.DACF_ALLOW_JIT_OPTS;
        int debuggerInfoShift = 10;
        contract.SetDebuggerInfoBits(handle, debuggerBits);

        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.Equal(initialFlags | ((uint)debuggerBits << debuggerInfoShift), rawFlags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetDebuggerInfoBits_UpdatesJitOptimizationDisabledState(MockTarget.Architecture arch)
    {
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (contract, target) = CreateLoaderContractWithTarget(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.None);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            moduleAddr = loader.AddModule().Address;
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);

        // Setting debugger bits without DACF_ALLOW_JIT_OPTS should set IS_JIT_OPTIMIZATION_DISABLED
        contract.SetDebuggerInfoBits(handle, DebuggerAssemblyControlFlags.DACF_NONE);

        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.True((rawFlags & IsJitOptimizationDisabled) != 0, "IS_JIT_OPTIMIZATION_DISABLED should be set when DACF_ALLOW_JIT_OPTS is not set");

        // Setting debugger bits WITH DACF_ALLOW_JIT_OPTS should clear IS_JIT_OPTIMIZATION_DISABLED
        contract.SetDebuggerInfoBits(handle, DebuggerAssemblyControlFlags.DACF_ALLOW_JIT_OPTS);

        rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.True((rawFlags & IsJitOptimizationDisabled) == 0, "IS_JIT_OPTIMIZATION_DISABLED should be cleared when DACF_ALLOW_JIT_OPTS is set");
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetDebuggerInfoBits_DoesNotEnableEnC(MockTarget.Architecture arch)
    {
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (contract, target) = CreateLoaderContractWithTarget(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.Debug);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            moduleAddr = loader.AddModule().Address;
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
        contract.SetDebuggerInfoBits(handle, DebuggerAssemblyControlFlags.DACF_NONE);

        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.True((rawFlags & IsEditAndContinue) == 0, "IS_EDIT_AND_CONTINUE should NOT be set when module is not EnC-capable");
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetDebuggerInfoBits_EnablesEnC_DisabledJitOpts(MockTarget.Architecture arch)
    {
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (contract, target) = CreateLoaderContractWithTarget(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.Debug);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            moduleAddr = loader.AddModule(flags: IsEncCapable).Address;
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
        contract.SetDebuggerInfoBits(handle, DebuggerAssemblyControlFlags.DACF_NONE);

        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.True((rawFlags & IsEditAndContinue) != 0, "IS_EDIT_AND_CONTINUE should be set when module is EnC-capable, config is Debug, and JIT opts are disabled");
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void SetDebuggerInfoBits_EnablesEnC_ExplicitFlag(MockTarget.Architecture arch)
    {
        TargetPointer moduleAddr = TargetPointer.Null;
        int flagsOffset = 0;

        var (contract, target) = CreateLoaderContractWithTarget(arch, (loader, builder) =>
        {
            var config = loader.AddEEConfig((uint)ClrModifiableAssemblies.Debug);
            builder.AddGlobals((Constants.Globals.EEConfig, config.Address));
            moduleAddr = loader.AddModule(flags: IsEncCapable).Address;
            flagsOffset = loader.ModuleLayout.GetField(nameof(Data.Module.Flags)).Offset;
        });

        Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
        contract.SetDebuggerInfoBits(handle, DebuggerAssemblyControlFlags.DACF_ALLOW_JIT_OPTS | DebuggerAssemblyControlFlags.DACF_ENC_ENABLED);

        uint rawFlags = target.Read<uint>(moduleAddr + (ulong)flagsOffset);
        Assert.True((rawFlags & IsEditAndContinue) != 0, "IS_EDIT_AND_CONTINUE should be set when DACF_ENC_ENABLED is explicitly requested");
    }

    public static IEnumerable<object[]> GetCompilerFlagsData()
    {
        foreach (var arch in new MockTarget.StdArch())
        {
            yield return [IsJitOptimizationDisabled | IsEditAndContinue, Interop.BOOL.FALSE, Interop.BOOL.TRUE, arch[0]];
            yield return [0u, Interop.BOOL.TRUE, Interop.BOOL.FALSE, arch[0]];
            yield return [IsJitOptimizationDisabled, Interop.BOOL.FALSE, Interop.BOOL.FALSE, arch[0]];
            yield return [IsEditAndContinue, Interop.BOOL.TRUE, Interop.BOOL.TRUE, arch[0]];
            // Debugger allows JIT opts but profiler disables them (IS_JIT_OPTIMIZATION_DISABLED wins)
            yield return [DebuggerAllowJitOptsPriv | IsJitOptimizationDisabled, Interop.BOOL.FALSE, Interop.BOOL.FALSE, arch[0]];
        }
    }

    [Theory]
    [MemberData(nameof(GetCompilerFlagsData))]
    public void GetCompilerFlags(uint rawFlags, Interop.BOOL expectedAllowJITOpts, Interop.BOOL expectedEnableEnC, MockTarget.Architecture arch)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockLoaderBuilder loader = new(targetBuilder.MemoryBuilder);

        MockLoaderModule module = loader.AddModule(flags: rawFlags);
        ulong assemblyAddr = module.Assembly;

        var target = targetBuilder
            .AddTypes(CreateContractTypes(loader))
            .AddContract<ILoader>(version: "c1")
            .Build();

        DacDbiImpl dbi = new(target, legacyObj: null);

        Interop.BOOL allowJITOpts;
        Interop.BOOL enableEnC;
        int hr = dbi.GetCompilerFlags(assemblyAddr, &allowJITOpts, &enableEnC);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(expectedAllowJITOpts, allowJITOpts);
        Assert.Equal(expectedEnableEnC, enableEnC);
    }
}
