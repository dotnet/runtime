// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure.ContractDescriptor;
using Microsoft.Diagnostics.Runtime;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe partial class EnumMemoryTests
{
    [Theory]
    [InlineData(false, 0x1234_5678_9000ul)]
    [InlineData(true, 0x1234_5678_9000ul)]
    [InlineData(false, 0ul)]
    [InlineData(true, 0ul)]
    public void RuntimeImageBase_PreservesProvidedAddressAcrossFlush(bool nativeDescriptor, ulong address)
    {
        TargetTestHelpers helpers = new(new() { IsLittleEndian = true, Is64Bit = true });
        ContractDescriptorBuilder builder = new(helpers);
        ulong descriptorAddress = new ContractDescriptorBuilder.DescriptorBuilder(builder).CreateSubDescriptor(0x20000, 0x21000, 0x22000);
        MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
        ContractDescriptorTarget target = nativeDescriptor
            ? ContractDescriptorTarget.Create(descriptorAddress, memory.ReadFromTarget, memory.WriteToTarget,
                (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
                (ulong _, out ulong allocated) => { allocated = 0; return HResults.E_NOTIMPL; }, [], runtimeImageBase: address)
            : ContractDescriptorTarget.Create(ContractDescriptorParser.ParseCompact("""{"version":2}"""u8), [],
                memory.ReadFromTarget, memory.WriteToTarget, (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
                (ulong _, out ulong allocated) => { allocated = 0; return HResults.E_NOTIMPL; },
                isLittleEndian: true, pointerSize: 8, runtimeImageBase: address);

        bool expected = address != 0;
        Assert.Equal(expected, target.TryGetRuntimeImageBase(out TargetPointer actual));
        Assert.Equal(new TargetPointer(address), actual);
        target.Flush(FlushScope.All);
        Assert.Equal(expected, target.TryGetRuntimeImageBase(out actual));
        Assert.Equal(new TargetPointer(address), actual);
    }

    [Theory]
    [InlineData("locator", false)]
    [InlineData("pe-exports", false)]
    [InlineData("explicit", false)]
    [InlineData("collector-locator", false)]
    [InlineData("fallback", false)]
    [InlineData("locator", true)]
    [InlineData("pe-exports", true)]
    [InlineData("explicit", true)]
    [InlineData("collector-locator", true)]
    [InlineData("fallback", true)]
    public void Activation_DiscoversDescriptorAndRejectsShortReads(string discovery, bool shortRead)
    {
        TargetTestHelpers helpers = new(new() { IsLittleEndian = true, Is64Bit = true });
        ContractDescriptorBuilder builder = new(helpers);
        builder.AddHeapFragment(new() { Address = 0x10000, Data = CreateRuntimeImage(is64Bit: true), Name = "RuntimeImage" });
        ulong descriptorAddress = new ContractDescriptorBuilder.DescriptorBuilder(builder)
            .SetContracts(ContractDescriptor.TargetTests.s_requiredDataAccessContracts)
            .SetGlobals([("OperatingSystem", null, "unix", "string")])
            .CreateSubDescriptor(0x12000, 0x13000, 0x14000);
        MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
        RuntimeDataTarget dataTarget = discovery == "pe-exports"
            ? new RuntimeDataTarget([]) { ReadMemory = memory.ReadFromTarget, ShortRead = shortRead }
            : new ContractLocatorDataTarget { ContractAddress = descriptorAddress, ReadMemory = memory.ReadFromTarget, ShortRead = shortRead };
        int hr = Activate(dataTarget, typeof(ICLRDataEnumMemoryRegions).GUID, out nint instance,
            discovery == "explicit" ? descriptorAddress : 0, discovery);

        if (shortRead)
        {
            Assert.True(hr < 0);
            Assert.Equal(0, instance);
        }
        else
        {
            Assert.Equal(HResults.S_OK, hr);
            Assert.NotEqual(0, instance);
            try
            {
                Assert.Equal(HResults.S_OK, Marshal.QueryInterface(instance, typeof(IXCLRDataProcess).GUID, out nint process));
                Marshal.Release(process);
                using RecordingCallback callback = new(supportsUpdates: false);
                var enumerate = (delegate* unmanaged[MemberFunction]<nint, void*, uint, CLRDataEnumMemoryFlags, int>)(*(nint**)instance)[3];
                Assert.Equal(HResults.S_OK, enumerate(instance, (void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
                Assert.Equal(discovery is "pe-exports" or "collector-locator", callback.Ranges.Contains((dataTarget.ImageBase, 0x300u)));
            }
            finally
            {
                Marshal.Release(instance);
            }
        }

        Assert.Equal(discovery is "pe-exports" or "collector-locator" ? 1 : 0, dataTarget.ImageBaseLookups);
        if (dataTarget is ContractLocatorDataTarget locator)
            Assert.Equal(discovery == "explicit" ? 0 : 1, locator.ContractLookups);
    }

    [Theory]
    [InlineData(0ul)]
    [InlineData(0x10000ul)]
    public void Activation_HelperUsesProvidedImageBaseWithoutLookup(ulong runtimeImageBase)
    {
        ContractDescriptorBuilder builder = new(new(new() { IsLittleEndian = true, Is64Bit = true }));
        builder.AddHeapFragment(new() { Address = 0x10000, Data = CreateRuntimeImage(is64Bit: true), Name = "RuntimeImage" });
        ulong descriptorAddress = new ContractDescriptorBuilder.DescriptorBuilder(builder)
            .SetContracts(ContractDescriptor.TargetTests.s_requiredDataAccessContracts)
            .SetGlobals([("OperatingSystem", null, "unix", "string")])
            .CreateSubDescriptor(0x12000, 0x13000, 0x14000);
        RuntimeDataTarget dataTarget = new([]) { ReadMemory = builder.GetMemoryContext().ReadFromTarget };
        void* target = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToUnmanaged(dataTarget);
        try
        {
            Guid iid = typeof(ICLRDataEnumMemoryRegions).GUID;
            void* instance = null;
            Assert.Equal(HResults.S_OK, EntrypointHelpers.CreateInstance(&iid, (nint)target, 0, &instance, descriptorAddress, runtimeImageBase));
            Assert.True(instance != null);
            try
            {
                using RecordingCallback callback = new(supportsUpdates: false);
                var enumerate = (delegate* unmanaged[MemberFunction]<void*, void*, uint, CLRDataEnumMemoryFlags, int>)(*(nint**)instance)[3];
                Assert.Equal(HResults.S_OK, enumerate(instance, (void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
                Assert.Equal(runtimeImageBase != 0, callback.Ranges.Contains((0x10000ul, 0x300u)));
            }
            finally
            {
                Marshal.Release((nint)instance);
            }
            Assert.Equal(0, dataTarget.ImageBaseLookups);
        }
        finally
        {
            ComInterfaceMarshaller<ICLRDataTarget>.Free(target);
        }
    }

    [Theory]
    [InlineData(HResults.E_FAIL, 0x10000ul, true)]
    [InlineData(HResults.E_NOTIMPL, 0x10000ul, true)]
    [InlineData(HResults.S_FALSE, 0x10000ul, true)]
    [InlineData(HResults.S_OK, 0ul, true)]
    [InlineData(HResults.E_FAIL, 0x10000ul, false)]
    [InlineData(HResults.E_NOTIMPL, 0x10000ul, false)]
    [InlineData(HResults.S_FALSE, 0x10000ul, false)]
    [InlineData(HResults.S_OK, 0ul, false)]
    public void Activation_CollectorRejectsUnavailableRuntimeImageBase(int result, ulong imageBase, bool hasContractLocator)
    {
        ContractDescriptorBuilder builder = new(new(new() { IsLittleEndian = true, Is64Bit = true }));
        ulong descriptorAddress = new ContractDescriptorBuilder.DescriptorBuilder(builder)
            .SetContracts(ContractDescriptor.TargetTests.s_requiredDataAccessContracts)
            .CreateSubDescriptor(0x12000, 0x13000, 0x14000);
        RuntimeDataTarget dataTarget = hasContractLocator
            ? new ContractLocatorDataTarget { ContractAddress = descriptorAddress, ReadMemory = builder.GetMemoryContext().ReadFromTarget }
            : new RuntimeDataTarget([]);
        dataTarget.LookupResult = result;
        dataTarget.ImageBase = imageBase;
        int hr = Activate(dataTarget, typeof(ICLRDataEnumMemoryRegions).GUID, out nint instance, entrypoint: "collector-locator");

        try
        {
            Assert.Equal(HResults.E_FAIL, hr);
            Assert.Equal(0, instance);
            Assert.Equal(1, dataTarget.ImageBaseLookups);
            Assert.Equal(0, dataTarget.ReadCount);
            if (dataTarget is ContractLocatorDataTarget locator)
                Assert.Equal(0, locator.ContractLookups);
        }
        finally
        {
            if (instance != 0)
                Marshal.Release(instance);
        }
    }

    [Theory]
    [InlineData("locator", null, 0x10000ul)]
    [InlineData("explicit", null, 0x10000ul)]
    [InlineData("fallback", null, 0x10000ul)]
    [InlineData("locator", HResults.S_OK, 0x10000ul)]
    [InlineData("explicit", HResults.S_OK, 0x10000ul)]
    [InlineData("fallback", HResults.S_OK, 0x10000ul)]
    [InlineData("locator", HResults.E_FAIL, 0x10000ul)]
    [InlineData("explicit", HResults.E_FAIL, 0x10000ul)]
    [InlineData("fallback", HResults.E_FAIL, 0x10000ul)]
    [InlineData("locator", HResults.S_FALSE, 0x10000ul)]
    [InlineData("explicit", HResults.S_FALSE, 0x10000ul)]
    [InlineData("fallback", HResults.S_FALSE, 0x10000ul)]
    [InlineData("locator", HResults.S_OK, 0ul)]
    [InlineData("explicit", HResults.S_OK, 0ul)]
    [InlineData("fallback", HResults.S_OK, 0ul)]
    public void Activation_UniversalUsesRuntimeLocatorWithoutImageBaseFallback(string entrypoint, int? locatorResult, ulong imageBase)
    {
        ContractDescriptorBuilder builder = new(new(new() { IsLittleEndian = true, Is64Bit = true }));
        builder.AddHeapFragment(new() { Address = 0x10000, Data = CreateRuntimeImage(is64Bit: true), Name = "RuntimeImage" });
        ulong descriptorAddress = new ContractDescriptorBuilder.DescriptorBuilder(builder)
            .SetContracts(ContractDescriptor.TargetTests.s_requiredDataAccessContracts)
            .SetGlobals([("OperatingSystem", null, "unix", "string")])
            .CreateSubDescriptor(0x12000, 0x13000, 0x14000);
        MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
        ContractLocatorDataTarget dataTarget = locatorResult is int result
            ? new RuntimeAndContractLocatorDataTarget
            {
                ContractAddress = descriptorAddress,
                ReadMemory = memory.ReadFromTarget,
                RuntimeLookupResult = result,
                ImageBase = imageBase,
            }
            : new ContractLocatorDataTarget { ContractAddress = descriptorAddress, ReadMemory = memory.ReadFromTarget };

        Assert.Equal(HResults.S_OK, Activate(dataTarget, typeof(ICLRDataEnumMemoryRegions).GUID, out nint instance,
            entrypoint == "explicit" ? descriptorAddress : 0, entrypoint));
        Assert.NotEqual(0, instance);
        try
        {
            using RecordingCallback callback = new(supportsUpdates: false);
            var enumerate = (delegate* unmanaged[MemberFunction]<nint, void*, uint, CLRDataEnumMemoryFlags, int>)(*(nint**)instance)[3];
            Assert.Equal(HResults.S_OK, enumerate(instance, (void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
            Assert.Equal(locatorResult == HResults.S_OK && imageBase != 0, callback.Ranges.Contains((0x10000ul, 0x300u)));
            Assert.Equal(0, dataTarget.ImageBaseLookups);
            Assert.Equal(locatorResult.HasValue ? 1 : 0, dataTarget.RuntimeBaseLookups);
        }
        finally
        {
            Marshal.Release(instance);
        }
    }

    [Theory]
    [InlineData(HResults.S_OK, 0ul, "locator")]
    [InlineData(HResults.S_FALSE, 0x12000ul, "locator")]
    [InlineData(HResults.E_FAIL, 0x12000ul, "locator")]
    [InlineData(HResults.S_OK, 0ul, "fallback")]
    [InlineData(HResults.S_FALSE, 0x12000ul, "fallback")]
    [InlineData(HResults.E_FAIL, 0x12000ul, "fallback")]
    public void Activation_UniversalRejectsUnavailableLocatorWithoutFallingBack(int result, ulong address, string entrypoint)
    {
        ContractLocatorDataTarget dataTarget = new() { ContractResult = result, ContractAddress = address };
        Assert.Equal(CdacHResults.CDAC_E_DESCRIPTOR_NOT_FOUND,
            Activate(dataTarget, typeof(ICLRDataEnumMemoryRegions).GUID, out nint instance, entrypoint: entrypoint));
        Assert.Equal(0, instance);
        Assert.Equal(1, dataTarget.ContractLookups);
        Assert.Equal(0, dataTarget.ImageBaseLookups);
        Assert.Equal(0, dataTarget.ReadCount);
    }

    [Theory]
    [InlineData("locator")]
    [InlineData("fallback")]
    public void Activation_UniversalRequiresContractLocator(string entrypoint)
    {
        RuntimeDataTarget dataTarget = new(CreateRuntimeImage(is64Bit: true));
        Assert.Equal(CdacHResults.CDAC_E_DESCRIPTOR_NOT_FOUND,
            Activate(dataTarget, typeof(ICLRDataEnumMemoryRegions).GUID, out nint instance, entrypoint: entrypoint));
        Assert.Equal(0, instance);
        Assert.Equal(0, dataTarget.ImageBaseLookups);
        Assert.Equal(0, dataTarget.ReadCount);
    }

    [Theory]
    [InlineData("iid")]
    [InlineData("target")]
    [InlineData("output")]
    [InlineData("contract")]
    public void Activation_HelperRejectsInvalidArguments(string argument)
    {
        RuntimeDataTarget dataTarget = new([]);
        void* target = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToUnmanaged(dataTarget);
        try
        {
            Guid iid = typeof(ICLRDataEnumMemoryRegions).GUID;
            void* instance = (void*)1;
            Assert.Equal(HResults.E_INVALIDARG, EntrypointHelpers.CreateInstance(
                argument == "iid" ? null : &iid,
                argument == "target" ? IntPtr.Zero : (nint)target,
                IntPtr.Zero,
                argument == "output" ? null : &instance,
                argument == "contract" ? 0ul : 0x12000ul));
            if (argument != "output")
                Assert.Equal(0, (nint)instance);
            Assert.Equal(0, dataTarget.ImageBaseLookups);
            Assert.Equal(0, dataTarget.ReadCount);
        }
        finally
        {
            ComInterfaceMarshaller<ICLRDataTarget>.Free(target);
        }
    }

    [Theory]
    [InlineData("iid", "locator")]
    [InlineData("target", "locator")]
    [InlineData("output", "locator")]
    [InlineData("iid", "collector-locator")]
    [InlineData("target", "collector-locator")]
    [InlineData("output", "collector-locator")]
    [InlineData("iid", "fallback")]
    [InlineData("target", "fallback")]
    [InlineData("output", "fallback")]
    public void Activation_EntrypointRejectsNullArguments(string argument, string entrypoint)
    {
        RuntimeDataTarget dataTarget = new([]);
        void* target = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToUnmanaged(dataTarget);
        try
        {
            Guid iid = typeof(ICLRDataEnumMemoryRegions).GUID;
            void* instance = (void*)1;
            Assert.Equal(HResults.E_INVALIDARG, InvokeActivation(
                argument == "iid" ? null : &iid,
                argument == "target" ? IntPtr.Zero : (nint)target,
                argument == "output" ? null : &instance, 0, entrypoint));
            if (argument != "output")
                Assert.Equal(0, (nint)instance);
            Assert.Equal(0, dataTarget.ImageBaseLookups);
            Assert.Equal(0, dataTarget.ReadCount);
        }
        finally
        {
            ComInterfaceMarshaller<ICLRDataTarget>.Free(target);
        }
    }

    private static int Activate(RuntimeDataTarget dataTarget, Guid iid, out nint instance, ulong contractAddress = 0, string entrypoint = "locator")
    {
        void* target = ComInterfaceMarshaller<ICLRDataTarget>.ConvertToUnmanaged(dataTarget);
        try
        {
            void* result = (void*)1;
            int hr = InvokeActivation(&iid, (nint)target, &result, contractAddress, entrypoint);
            instance = (nint)result;
            return hr;
        }
        finally
        {
            ComInterfaceMarshaller<ICLRDataTarget>.Free(target);
        }
    }

    private static int InvokeActivation(Guid* iid, nint target, void** instance, ulong contractAddress, string entrypoint)
    {
        Assembly assembly = entrypoint is "pe-exports" or "collector-locator"
            ? Assembly.Load(OperatingSystem.IsWindows() ? "mscordaccore" : "libmscordaccore")
            : typeof(Entrypoints).Assembly;
        string methodName = entrypoint switch
        {
            "explicit" => "DbgShimCreateInstanceFromContractDescriptor",
            "fallback" => "CLRDataCreateInstanceWithFallback",
            _ => "CLRDataCreateInstance",
        };
        Type type = assembly.GetType("Microsoft.Diagnostics.DataContractReader.Entrypoints", throwOnError: true)!;
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.NotNull(method);
        nint address = method.MethodHandle.GetFunctionPointer();
        return entrypoint switch
        {
            "explicit" => ((delegate* unmanaged<Guid*, nint, ulong, void**, int>)address)(iid, target, contractAddress, instance),
            "fallback" => ((delegate* unmanaged<Guid*, nint, nint, void**, int>)address)(iid, target, 0, instance),
            _ => ((delegate* unmanaged<Guid*, nint, void**, int>)address)(iid, target, instance),
        };
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void RuntimeImageBase_CorDebugTarget_PreservesProvidedBase(MockTarget.Architecture arch)
    {
        ulong expectedBase = arch.Is64Bit ? 0x1234_5678_9000UL : 0x9000_0000UL;
        TargetTestHelpers helpers = new(arch);
        ContractDescriptorBuilder builder = new(helpers);
        ulong descriptorAddress = new ContractDescriptorBuilder.DescriptorBuilder(builder).CreateSubDescriptor(0x30000, 0x31000, 0x32000);
        CorDebugDataTarget dataTarget = new(builder.GetMemoryContext());

        Target target = Entrypoints.CreateTargetFromCorDebugDataTarget(dataTarget, descriptorAddress, expectedBase);

        Assert.True(target.TryGetRuntimeImageBase(out TargetPointer imageBase));
        Assert.Equal(expectedBase, imageBase.Value);
        target.Flush(FlushScope.All);
        Assert.True(target.TryGetRuntimeImageBase(out imageBase));
        Assert.Equal(expectedBase, imageBase.Value);
    }

    [Theory]
    [InlineData(4, @"C:\runtime\coreclr.dll")]
    [InlineData(8, @"C:\runtime\coreclr.dll")]
    [InlineData(4, "/runtime/libcoreclr.so")]
    [InlineData(8, "/runtime/libcoreclr.so")]
    [InlineData(4, "/runtime/libcoreclr.dylib")]
    [InlineData(8, "/runtime/libcoreclr.dylib")]
    public void RuntimeImageBase_ClrMdLookup_UsesDescriptorModule(int pointerSize, string runtimeModuleName)
    {
        const ulong RawImageBase = 0x1234_5678_9000_0000;
        const ulong RawDescriptorAddress = RawImageBase + 0x1000;
        var reader = new Mock<IDataReader>();
        reader.SetupGet(r => r.PointerSize).Returns(pointerSize);
        reader.SetupGet(r => r.Architecture).Returns(pointerSize == 4 ? Architecture.X86 : Architecture.X64);
        reader.SetupGet(r => r.TargetPlatform).Returns(OSPlatform.Windows);
        reader.Setup(r => r.EnumerateModules()).Returns(
        [
            CreateModule(0x10000, "application", 0x11000),
            CreateModule(0x20000, runtimeModuleName, 0),
            CreateModule(RawImageBase, runtimeModuleName, RawDescriptorAddress),
        ]);
        using DataTarget dataTarget = new(reader.Object, new DataTargetOptions());
        ClrMdDumpHost host = Assert.IsType<ClrMdDumpHost>(Activator.CreateInstance(
            typeof(ClrMdDumpHost), BindingFlags.Instance | BindingFlags.NonPublic, binder: null,
            args: ["test.dmp", dataTarget, Array.Empty<string>()], culture: null));

        ulong descriptorAddress = host.FindContractDescriptorAddress(out ulong imageBase);
        ulong mask = pointerSize == 4 ? uint.MaxValue : ulong.MaxValue;
        Assert.Equal(RawDescriptorAddress & mask, descriptorAddress);
        Assert.Equal(RawImageBase & mask, imageBase);
        reader.Verify(r => r.EnumerateModules(), Times.Once);
    }

    private static ModuleInfo CreateModule(ulong imageBase, string fileName, ulong descriptorAddress)
    {
        var module = new Mock<ModuleInfo>(imageBase, fileName);
        module.Setup(m => m.GetExportSymbolAddress("DotNetRuntimeContractDescriptor")).Returns(descriptorAddress);
        return module.Object;
    }

    private sealed class CorDebugDataTarget(MockMemorySpace.MemoryContext memory) : ICorDebugDataTarget
    {
        public int GetPlatform(int* platform) => throw new NotImplementedException();

        public int GetThreadContext(uint threadId, uint contextFlags, uint contextSize, byte* context)
            => throw new NotImplementedException();

        public int ReadVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesRead)
        {
            int hr = memory.ReadFromTarget(address, new Span<byte>(buffer, checked((int)bytesRequested)));
            *bytesRead = hr >= 0 ? bytesRequested : 0;
            return hr;
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Enumeration_ReadsRuntimeImageAtProvidedBaseOnEveryCall(MockTarget.Architecture arch)
    {
        const ulong ImageBase = 0x10000;
        TargetTestHelpers helpers = new(arch);
        ContractDescriptorBuilder builder = new(helpers);
        byte[] image = CreateRuntimeImage(arch.Is64Bit);
        builder.AddHeapFragment(new() { Address = ImageBase, Data = image, Name = "RuntimeImage" });
        ulong descriptorAddress = new ContractDescriptorBuilder.DescriptorBuilder(builder).CreateSubDescriptor(0x30000, 0x31000, 0x32000);
        MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
        Lock apiLock = new();
        int imageReadCount = 0;
        bool imageReadUnderLock = true;
        ContractDescriptorTarget target = ContractDescriptorTarget.Create(descriptorAddress,
            (address, buffer) =>
            {
                if (address >= ImageBase && address < ImageBase + (ulong)image.Length)
                {
                    imageReadCount++;
                    imageReadUnderLock &= apiLock.IsHeldByCurrentThread;
                }
                return memory.ReadFromTarget(address, buffer);
            },
            memory.WriteToTarget, (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; },
            [], runtimeImageBase: ImageBase);
        ICLRDataEnumMemoryRegions impl = new SOSDacImpl(target, legacyObj: null, apiLock);
        Assert.Equal(0, imageReadCount);

        for (int i = 0; i < 2; i++)
        {
            using RecordingCallback callback = new(supportsUpdates: false);
            Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
            Assert.Equal(3 * (i + 1), imageReadCount);
            Assert.True(imageReadUnderLock);
            Assert.Contains((ImageBase, 64u), callback.Ranges);
            Assert.Contains((ImageBase + 0x80, 264u), callback.Ranges);
            Assert.Contains((ImageBase, 0x300u), callback.Ranges);
            Assert.Contains((ImageBase + 0x400, 0x100u), callback.Ranges);
            Assert.Contains((ImageBase + 0x600, 0x40u), callback.Ranges);
            Assert.Contains((ImageBase + 0x700, 0x20u), callback.Ranges);
        }

        using RecordingCallback cancelled = new(supportsUpdates: false) { CancelAtAddress = ImageBase };
        Assert.Equal(HResults.COR_E_OPERATIONCANCELED, impl.EnumMemoryRegions((void*)cancelled.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.Equal(7, imageReadCount);
        using RecordingCallback afterCancellation = new(supportsUpdates: false);
        Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)afterCancellation.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.Contains((ImageBase, 0x300u), afterCancellation.Ranges);
        Assert.Equal(10, imageReadCount);
    }

    [Theory]
    [InlineData(false, HResults.S_OK, HResults.S_OK, 0x10000ul, true)]
    [InlineData(true, HResults.S_OK, HResults.E_FAIL, 0x10000ul, true)]
    [InlineData(false, HResults.S_OK, HResults.E_FAIL, 0x10000ul, false)]
    [InlineData(true, HResults.E_FAIL, HResults.E_FAIL, 0x10000ul, false)]
    [InlineData(false, HResults.S_OK, HResults.S_FALSE, 0x10000ul, false)]
    [InlineData(true, HResults.S_FALSE, HResults.S_FALSE, 0x10000ul, false)]
    [InlineData(false, HResults.S_OK, HResults.S_OK, 0ul, false)]
    [InlineData(true, HResults.S_OK, HResults.S_OK, 0ul, false)]
    [InlineData(true, HResults.E_FAIL, HResults.S_OK, 0x10000ul, true)]
    [InlineData(true, HResults.S_FALSE, HResults.S_OK, 0x10000ul, true)]
    [InlineData(true, HResults.S_FALSE, HResults.E_FAIL, 0x10000ul, false)]
    [InlineData(true, HResults.E_FAIL, HResults.S_OK, 0ul, false)]
    public void RuntimeImageLookup_UsesHostCapabilityWithoutReadingHeaders(bool hasLocator, int locatorResult, int fallbackResult, ulong imageBase, bool expected)
    {
        RuntimeDataTarget dataTarget = hasLocator
            ? new RuntimeLocatorDataTarget([]) { RuntimeLookupResult = locatorResult }
            : new RuntimeDataTarget([]);
        dataTarget.LookupResult = fallbackResult;
        dataTarget.ImageBase = imageBase;

        bool actualResult = EntrypointHelpers.TryGetRuntimeImageBase(dataTarget, out TargetPointer address);
        bool useFallback = !hasLocator || locatorResult != HResults.S_OK;
        Assert.Equal(expected, actualResult);
        Assert.Equal(expected ? new TargetPointer(imageBase) : TargetPointer.Null, address);
        Assert.Equal(useFallback ? 1 : 0, dataTarget.ImageBaseLookups);
        Assert.Equal(hasLocator ? 1 : 0, dataTarget.RuntimeBaseLookups);
        Assert.Equal(0, dataTarget.ReadCount);
    }

    [Theory]
    [InlineData(false, false, null)]
    [InlineData(true, false, null)]
    [InlineData(false, true, null)]
    [InlineData(true, true, null)]
    [InlineData(false, false, HResults.S_OK)]
    [InlineData(true, false, HResults.S_OK)]
    [InlineData(false, true, HResults.S_OK)]
    [InlineData(true, true, HResults.S_OK)]
    [InlineData(false, false, HResults.S_FALSE)]
    [InlineData(true, false, HResults.S_FALSE)]
    [InlineData(false, true, HResults.S_FALSE)]
    [InlineData(true, true, HResults.S_FALSE)]
    [InlineData(false, false, HResults.E_FAIL)]
    [InlineData(true, false, HResults.E_FAIL)]
    [InlineData(false, true, HResults.E_FAIL)]
    [InlineData(true, true, HResults.E_FAIL)]
    public void RuntimeDescriptorBootstrap_FallsBackToPeExportsAndRejectsShortReads(bool is64Bit, bool shortRead, int? locatorResult)
    {
        ContractDescriptorBuilder builder = new(new(new() { IsLittleEndian = true, Is64Bit = is64Bit }));
        builder.AddHeapFragment(new() { Address = 0x10000, Data = CreateRuntimeImage(is64Bit), Name = "RuntimeImage" });
        new ContractDescriptorBuilder.DescriptorBuilder(builder)
            .SetContracts(ContractDescriptor.TargetTests.s_requiredDataAccessContracts)
            .CreateSubDescriptor(0x12000, 0x13000, 0x14000);
        RuntimeDataTarget dataTarget = locatorResult is int result
            ? new ContractLocatorDataTarget
            {
                ContractResult = result,
                ContractAddress = result == HResults.S_OK ? 0ul : 0x12000ul,
                ReadMemory = builder.GetMemoryContext().ReadFromTarget,
                ShortRead = shortRead,
            }
            : new RuntimeDataTarget([]) { ReadMemory = builder.GetMemoryContext().ReadFromTarget, ShortRead = shortRead };

        int hr = Activate(dataTarget, typeof(ICLRDataEnumMemoryRegions).GUID, out nint instance, entrypoint: "pe-exports");
        try
        {
            Assert.Equal(shortRead ? CdacHResults.CDAC_E_DESCRIPTOR_NOT_FOUND : HResults.S_OK, hr);
            Assert.Equal(!shortRead, instance != 0);
            Assert.Equal(1, dataTarget.ImageBaseLookups);
            if (dataTarget is ContractLocatorDataTarget locator)
                Assert.Equal(1, locator.ContractLookups);
            if (shortRead)
                Assert.Equal(1, dataTarget.ReadCount);
        }
        finally
        {
            if (instance != 0)
                Marshal.Release(instance);
        }
    }

    [Fact]
    public void RuntimeDescriptorBootstrap_RejectsMissingImageBaseWithoutReading()
    {
        RuntimeDataTarget dataTarget = new(CreateRuntimeImage(is64Bit: true)) { ImageBase = 0 };

        Assert.Equal(HResults.E_FAIL, Activate(dataTarget, typeof(ICLRDataEnumMemoryRegions).GUID, out nint instance, entrypoint: "pe-exports"));
        Assert.Equal(0, instance);
        Assert.Equal(1, dataTarget.ImageBaseLookups);
        Assert.Equal(0, dataTarget.ReadCount);
    }

    [Theory]
    [InlineData("no-base", 0)]
    [InlineData("unreadable-dos", 1)]
    [InlineData("unreadable-pe", 2)]
    [InlineData("not-pe", 1)]
    [InlineData("negative-offset", 1)]
    [InlineData("bad-signature", 2)]
    [InlineData("bad-optional-header", 2)]
    [InlineData("zero-headers", 2)]
    public void RuntimeImageDiscovery_HandlesMissingOrInvalidImages(string scenario, int expectedReads)
    {
        const ulong ImageBase = 0x10000;
        byte[] image = CreateRuntimeImage(is64Bit: true);
        switch (scenario)
        {
            case "not-pe":
                image[0] = 0;
                break;
            case "negative-offset":
                BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0x3c), -1);
                break;
            case "bad-signature":
                image[0x80] = 0;
                break;
            case "bad-optional-header":
                BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(0x80 + 24), 0);
                break;
            case "zero-headers":
                BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(0x80 + 24 + 60), 0);
                break;
        }

        ContractDescriptorBuilder builder = new(new(new() { Is64Bit = true, IsLittleEndian = true }));
        builder.AddHeapFragment(new() { Address = ImageBase, Data = image, Name = "RuntimeImage" });
        ulong descriptorAddress = new ContractDescriptorBuilder.DescriptorBuilder(builder).CreateSubDescriptor(0x20000, 0x21000, 0x22000);
        MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
        int reads = 0;
        ContractDescriptorTarget target = ContractDescriptorTarget.Create(descriptorAddress,
            (address, buffer) =>
            {
                if (address >= ImageBase && address < ImageBase + (ulong)image.Length)
                {
                    reads++;
                    if ((scenario == "unreadable-dos" && address == ImageBase)
                        || (scenario == "unreadable-pe" && address == ImageBase + 0x80))
                    {
                        return HResults.E_FAIL;
                    }
                }
                return memory.ReadFromTarget(address, buffer);
            },
            memory.WriteToTarget, (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; }, [],
            runtimeImageBase: scenario == "no-base" ? TargetPointer.Null : new(ImageBase));

        Assert.Equal(0, reads);
        PEImageCollector imageCollector = new(target);
        using RecordingCallback callback = new(supportsUpdates: false);
        MemoryRegionEmitter emitter = new(callback.Address, 8);
        if (scenario == "no-base")
            Assert.Equal(HResults.S_OK, MemoryEnumerator.Enumerate(target, emitter, DumpType.Mini));
        else if (scenario is "unreadable-dos" or "unreadable-pe")
            Assert.Throws<VirtualReadException>(() => imageCollector.EnumerateMemoryRegions(ImageBase, (uint)image.Length, isMapped: true, emitter, DumpType.Mini));
        else
            imageCollector.EnumerateMemoryRegions(ImageBase, (uint)image.Length, isMapped: true, emitter, DumpType.Mini);
        Assert.Equal(expectedReads, reads);
        if (scenario == "no-base")
            Assert.DoesNotContain(callback.Ranges, range => range.Address >= ImageBase && range.Address < ImageBase + (uint)image.Length);
        else
            Assert.Empty(callback.Ranges);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RuntimeImageRegions_ReturnsNonemptyRangesWithoutReading(bool is64Bit, bool emptyDirectories)
    {
        const ulong ImageBase = 0x10000;
        byte[] image = CreateRuntimeImage(is64Bit);
        if (emptyDirectories)
        {
            int directoryOffset = 0x80 + 24 + (is64Bit ? 112 : 96);
            image.AsSpan(directoryOffset, 8).Clear();
            image.AsSpan(directoryOffset + 2 * 8, 4).Clear();
            image.AsSpan(directoryOffset + 6 * 8 + 4, 4).Clear();
        }

        int reads = 0;
        Assert.True(PEImageInfo.TryCreate(ImageBase, (uint)image.Length, isMapped: true, (address, buffer) =>
        {
            reads++;
            image.AsSpan(checked((int)(address - ImageBase)), buffer.Length).CopyTo(buffer);
            return true;
        }, out PEImageInfo module));
        Assert.Equal(2, reads);
        List<TargetSpan> expected = [new(ImageBase, 0x300)];
        if (!emptyDirectories)
        {
            expected.Add(new(ImageBase + 0x700, 0x20));
            expected.Add(new(ImageBase + 0x600, 0x40));
            expected.Add(new(ImageBase + 0x400, 0x100));
        }

        IEnumerable<TargetSpan> regions = module.EnumerateMemoryRegions();
        Assert.Equal(expected, regions);
        Assert.Equal(expected, regions);
        Assert.Equal(2, reads);
    }

    public static IEnumerable<object[]> ModuleDebugPayloadCases()
    {
        foreach (bool is64Bit in new[] { false, true })
        foreach (bool mapped in new[] { false, true })
        foreach (bool triage in new[] { false, true })
        foreach (bool supportsUpdates in new[] { false, true })
            yield return [is64Bit, mapped, triage, supportsUpdates, @"C:\private\symbols\module.pdb"];

        foreach (string path in new[] { "/private/symbols/module.pdb", "module.pdb", "", "/private/" + new string('a', 300) + "/module.pdb", "/private/" + new string('a', 300) + ".pdb" })
            yield return [true, true, true, true, path];
    }

    [Theory]
    [MemberData(nameof(ModuleDebugPayloadCases))]
    public void ModuleCollection_IncludesDebugPayloads(bool is64Bit, bool mapped, bool triage, bool supportsUpdates, string pdbPath)
    {
        const ulong ImageBase = 0x10000;
        byte[] image = CreateManagedDebugImage(is64Bit, mapped, pdbPath);
        ContractDescriptorTarget target = CreateManagedImageTarget(image, is64Bit, mapped);
        using RecordingCallback callback = new(supportsUpdates);
        MemoryRegionEmitter emitter = new(callback.Address, is64Bit ? 8u : 4u);
        Assert.Equal(HResults.S_OK, MemoryEnumerator.Enumerate(target, emitter, triage ? DumpType.Triage : DumpType.Mini));

        uint sectionOffset = mapped ? 0x1000u : 0x400u;
        ulong debugAddress = ImageBase + sectionOffset;
        ulong payloadAddress = debugAddress + 0x400;
        uint payloadSize = 24 + (uint)Encoding.UTF8.GetByteCount(pdbPath) + 1;
        Assert.Contains((ImageBase, 0x300u), callback.Ranges);
        Assert.Contains((debugAddress, 56u), callback.Ranges);
        Assert.Contains((payloadAddress, payloadSize), callback.Ranges);
        Assert.Contains((debugAddress + 0x200, 8u), callback.Ranges);
        Assert.DoesNotContain(callback.Ranges, r => r.Address == ImageBase && r.Size == image.Length);
        Assert.DoesNotContain(callback.Ranges, r => r.Address <= debugAddress + 0x300 && r.Address + r.Size > debugAddress + 0x300);
        if (triage && supportsUpdates)
        {
            byte[] path = image.AsSpan((int)(sectionOffset + 0x400 + 24), (int)payloadSize - 24).ToArray();
            foreach ((ulong address, byte[] update) in callback.Updates)
                update.CopyTo(path.AsSpan(checked((int)(address - payloadAddress - 24))));
            string fileName = pdbPath[(Math.Max(pdbPath.LastIndexOf('/'), pdbPath.LastIndexOf('\\')) + 1)..];
            byte[] expected = new byte[path.Length];
            Encoding.UTF8.GetBytes(fileName, expected);
            Assert.Equal(expected, path);
        }
        else
        {
            Assert.Empty(callback.Updates);
        }
        Assert.Equal(HResults.S_OK, emitter.Result);

        TestPlaceholderTarget dumpTarget = new TestPlaceholderTarget.Builder(new() { IsLittleEndian = true, Is64Bit = is64Bit })
            .UseReader((ulong address, Span<byte> buffer) =>
            {
                int length = buffer.Length;
                if (!callback.Ranges.Exists(r => address >= r.Address && address - r.Address <= r.Size && (ulong)length <= r.Size - (address - r.Address)))
                    return HResults.E_FAIL;

                image.AsSpan(checked((int)(address - ImageBase)), buffer.Length).CopyTo(buffer);
                foreach ((ulong updateAddress, byte[] update) in callback.Updates)
                {
                    ulong start = Math.Max(address, updateAddress);
                    ulong end = Math.Min(address + (uint)buffer.Length, updateAddress + (uint)update.Length);
                    if (start < end)
                        update.AsSpan((int)(start - updateAddress), (int)(end - start)).CopyTo(buffer[(int)(start - address)..]);
                }
                return HResults.S_OK;
            })
            .Build();
        using PEReader reader = new(new TargetStream(dumpTarget, ImageBase, image.Length), mapped ? PEStreamOptions.IsLoadedImage : PEStreamOptions.Default);
        ImmutableArray<DebugDirectoryEntry> entries = reader.ReadDebugDirectory();
        Assert.Equal(2, entries.Length);
        CodeViewDebugDirectoryData codeView = reader.ReadCodeViewDebugDirectoryData(entries[0]);
        string expectedPath = triage && supportsUpdates
            ? pdbPath[(Math.Max(pdbPath.LastIndexOf('/'), pdbPath.LastIndexOf('\\')) + 1)..]
            : pdbPath;
        Assert.Equal(expectedPath, codeView.Path);
    }

    private static ContractDescriptorTarget CreateManagedImageTarget(byte[] image, bool is64Bit, bool mapped)
    {
        const ulong ImageBase = 0x10000;
        TargetPointer imageBase = new(ImageBase);
        uint imageSize = (uint)image.Length;
        uint imageFlags = mapped ? 1u : 0u;
        Contracts.ModuleHandle module = new(new TargetPointer(0x8000));
        Mock<ILoader> loader = new();
        loader.Setup(l => l.GetModuleHandles(It.IsAny<TargetPointer>(), It.IsAny<AssemblyIterationFlags>())).Returns([module]);
        loader.Setup(l => l.TryGetLoadedImageContents(module, out imageBase, out imageSize, out imageFlags)).Returns(true);
        return ContractDescriptorTarget.Create(
            ContractDescriptorParser.ParseCompact("""{"version":2,"contracts":{"Loader":"c1","EcmaMetadata":"c1"}}"""u8), [],
            (ulong address, Span<byte> buffer) =>
            {
                if (address < ImageBase || address - ImageBase > (ulong)image.Length
                    || (ulong)buffer.Length > (ulong)image.Length - (address - ImageBase))
                    return HResults.E_FAIL;

                image.AsSpan((int)(address - ImageBase), buffer.Length).CopyTo(buffer);
                return HResults.S_OK;
            },
            (_, _) => HResults.E_NOTIMPL, (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; },
            isLittleEndian: true, pointerSize: is64Bit ? 8 : 4,
            contractRegistrations: [registry =>
            {
                registry.Register<ILoader>("c1", _ => loader.Object);
                registry.Register<IEcmaMetadata>("c1", _ => new Mock<IEcmaMetadata>().Object);
            }]);
    }

    [Theory]
    [InlineData(false, false, "valid")]
    [InlineData(false, true, "valid")]
    [InlineData(true, false, "valid")]
    [InlineData(true, true, "valid")]
    [InlineData(true, false, "unreadable-directory")]
    [InlineData(true, true, "unreadable-directory")]
    [InlineData(true, false, "invalid-payload")]
    [InlineData(true, true, "invalid-payload")]
    [InlineData(true, false, "empty-payload")]
    [InlineData(true, true, "empty-payload")]
    public void ImageDebugEntries_ReadLazilyAndValidatePayloadRanges(bool is64Bit, bool mapped, string scenario)
    {
        const ulong ImageBase = 0x10000;
        byte[] image = CreateManagedDebugImage(is64Bit, mapped, "module.pdb");
        int sectionOffset = mapped ? 0x1000 : 0x400;
        if (scenario == "invalid-payload")
            BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(sectionOffset + 20), uint.MaxValue - 8);
        if (scenario == "empty-payload")
            image.AsSpan(sectionOffset + 16, 4).Clear();

        List<ulong> reads = [];
        bool ReadMemory(ulong address, Span<byte> buffer)
        {
            reads.Add(address);
            if (scenario == "unreadable-directory" && address == ImageBase + (uint)sectionOffset)
                return false;
            image.AsSpan(checked((int)(address - ImageBase)), buffer.Length).CopyTo(buffer);
            return true;
        }

        Assert.True(PEImageInfo.TryCreate(ImageBase, (uint)image.Length, mapped, ReadMemory, out PEImageInfo module));
        Assert.DoesNotContain(ImageBase + (uint)sectionOffset, reads);
        IEnumerable<PEImageInfo.DebugEntry> entries = module.EnumerateDebugEntries(ReadMemory);
        Assert.DoesNotContain(ImageBase + (uint)sectionOffset, reads);
        if (scenario is "unreadable-directory" or "invalid-payload")
        {
            Assert.Throws<InvalidOperationException>(() => new List<PEImageInfo.DebugEntry>(entries));
        }
        else
        {
            List<PEImageInfo.DebugEntry> expected = [];
            if (scenario != "empty-payload")
                expected.Add(new(DebugDirectoryEntryType.CodeView, new TargetSpan(ImageBase + (uint)sectionOffset + 0x400, 35)));
            expected.Add(new(DebugDirectoryEntryType.PdbChecksum, new TargetSpan(ImageBase + (uint)sectionOffset + 0x200, 8)));
            Assert.Equal(expected, entries);
            Assert.DoesNotContain(ImageBase + (uint)sectionOffset + 0x400, reads);
        }
    }

    [Theory]
    [InlineData(false, 0x400u)]
    [InlineData(false, 0x800u)]
    [InlineData(true, 0x1000u)]
    [InlineData(true, 0x1400u)]
    public void ModuleCollection_PropagatesImageCancellation(bool mapped, uint offset)
    {
        const ulong ImageBase = 0x10000;
        byte[] image = CreateManagedDebugImage(is64Bit: true, mapped, "module.pdb");
        ContractDescriptorTarget target = CreateManagedImageTarget(image, is64Bit: true, mapped);
        using RecordingCallback callback = new(supportsUpdates: false) { CancelAtAddress = ImageBase + offset };
        MemoryRegionEmitter emitter = new(callback.Address, 8);
        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() =>
            MemoryEnumerator.Enumerate(target, emitter, DumpType.Mini));
        Assert.Equal(HResults.COR_E_OPERATIONCANCELED, exception.HResult);
    }

    private static byte[] CreateManagedDebugImage(bool is64Bit, bool mapped, string pdbPath)
    {
        byte[] image = new byte[0x3000];
        CreateRuntimeImage(is64Bit).AsSpan(0, 0x300).CopyTo(image);
        Span<byte> peHeader = image.AsSpan(0x80);
        ushort optionalHeaderSize = is64Bit ? (ushort)240 : (ushort)224;
        BinaryPrimitives.WriteUInt16LittleEndian(peHeader[6..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(peHeader[20..], optionalHeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(peHeader[(24 + 56)..], (uint)image.Length);
        int directoryOffset = 24 + (is64Bit ? 112 : 96);
        BinaryPrimitives.WriteUInt32LittleEndian(peHeader[(directoryOffset - 4)..], 16);
        peHeader.Slice(directoryOffset, 16 * 8).Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(peHeader[(directoryOffset + 6 * 8)..], 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(peHeader[(directoryOffset + 6 * 8 + 4)..], 56);
        Span<byte> section = peHeader[(24 + optionalHeaderSize)..];
        BinaryPrimitives.WriteUInt32LittleEndian(section[8..], 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(section[12..], 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(section[16..], 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(section[20..], 0x400);
        int sectionOffset = mapped ? 0x1000 : 0x400;
        Span<byte> debug = image.AsSpan(sectionOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(debug[12..], (uint)DebugDirectoryEntryType.CodeView);
        BinaryPrimitives.WriteUInt32LittleEndian(debug[16..], 24 + (uint)Encoding.UTF8.GetByteCount(pdbPath) + 1);
        BinaryPrimitives.WriteUInt32LittleEndian(debug[20..], 0x1400);
        BinaryPrimitives.WriteUInt32LittleEndian(debug[24..], 0x800);
        BinaryPrimitives.WriteUInt32LittleEndian(debug[(28 + 12)..], (uint)DebugDirectoryEntryType.PdbChecksum);
        BinaryPrimitives.WriteUInt32LittleEndian(debug[(28 + 16)..], 8);
        BinaryPrimitives.WriteUInt32LittleEndian(debug[(28 + 20)..], 0x1200);
        BinaryPrimitives.WriteUInt32LittleEndian(debug[(28 + 24)..], 0x600);
        "RSDS"u8.CopyTo(debug[0x400..]);
        Encoding.UTF8.GetBytes(pdbPath, debug[(0x400 + 24)..]);
        return image;
    }

    private static byte[] CreateRuntimeImage(bool is64Bit)
    {
        byte[] image = new byte[0x1000];
        BinaryPrimitives.WriteUInt16LittleEndian(image, 0x5a4d);
        BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0x3c), 0x80);
        Span<byte> peHeader = image.AsSpan(0x80);
        BinaryPrimitives.WriteUInt32LittleEndian(peHeader, 0x00004550);
        BinaryPrimitives.WriteUInt16LittleEndian(peHeader.Slice(24), (ushort)(is64Bit ? PEMagic.PE32Plus : PEMagic.PE32));
        BinaryPrimitives.WriteUInt32LittleEndian(peHeader.Slice(24 + 60), 0x300);
        int directoryOffset = 24 + (is64Bit ? 112 : 96);
        foreach ((int index, uint rva, uint size) in new[] { (0, 0x400u, 0x100u), (2, 0x600u, 0x40u), (6, 0x700u, 0x20u) })
        {
            BinaryPrimitives.WriteUInt32LittleEndian(peHeader.Slice(directoryOffset + index * 8), rva);
            BinaryPrimitives.WriteUInt32LittleEndian(peHeader.Slice(directoryOffset + index * 8 + 4), size);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(0x400 + 20), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(0x400 + 24), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(0x400 + 28), 0x440);
        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(0x400 + 32), 0x444);
        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(0x400 + 36), 0x448);
        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(0x440), 0x2000);
        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(0x444), 0x450);
        "DotNetRuntimeContractDescriptor\0"u8.CopyTo(image.AsSpan(0x450));
        return image;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Enumeration_SharesComIdentityWithAnalysisInterfaces(bool enumerateFirst)
    {
        Lock apiLock = new();
        TargetTestHelpers helpers = new(new() { IsLittleEndian = true, Is64Bit = true });
        ContractDescriptorBuilder builder = new(helpers);
        ContractDescriptorTarget target = builder.CreateTarget(new(builder));
        SOSDacImpl impl = new(target, legacyObj: null, apiLock);
        Assert.Equal(HResults.E_INVALIDARG, ((ICLRDataEnumMemoryRegions)impl).EnumMemoryRegions(null, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.Equal(HResults.E_POINTER, ((IXCLRDataProcess3)impl).GetFunctionTable(default, 0, null, null, null));

        void* entry = enumerateFirst
            ? ComInterfaceMarshaller<ICLRDataEnumMemoryRegions>.ConvertToUnmanaged(impl)
            : ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(impl);
        try
        {
            Guid iidIUnknown = new("00000000-0000-0000-C000-000000000046");
            Assert.Equal(HResults.S_OK, Marshal.QueryInterface((nint)entry, in iidIUnknown, out nint identity));
            try
            {
                using RecordingCallback callback = new(supportsUpdates: false, apiLock);
                foreach (Type interfaceType in new[]
                {
                    typeof(ICLRDataEnumMemoryRegions), typeof(IXCLRDataProcess), typeof(IXCLRDataProcess2),
                    typeof(IXCLRDataProcess3), typeof(ISOSDacInterface), typeof(ISOSDacInterface17),
                })
                {
                    Guid iid = interfaceType.GUID;
                    Assert.Equal(HResults.S_OK, Marshal.QueryInterface((nint)entry, in iid, out nint iface));
                    try
                    {
                        Assert.Equal(HResults.S_OK, Marshal.QueryInterface(iface, in iidIUnknown, out nint interfaceIdentity));
                        try
                        {
                            Assert.Equal(identity, interfaceIdentity);
                        }
                        finally
                        {
                            Marshal.Release(interfaceIdentity);
                        }

                        Guid enumerationIid = typeof(ICLRDataEnumMemoryRegions).GUID;
                        Assert.Equal(HResults.S_OK, Marshal.QueryInterface(iface, in enumerationIid, out nint enumeration));
                        try
                        {
                            var enumerate = (delegate* unmanaged[MemberFunction]<nint, void*, uint, CLRDataEnumMemoryFlags, int>)(*(nint**)enumeration)[3];
                            Assert.Equal(HResults.E_INVALIDARG, enumerate(enumeration, null, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
                            callback.Regions.Clear();
                            Assert.Equal(HResults.S_OK, enumerate(enumeration, (void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
                            Assert.NotEmpty(callback.Regions);
                            Assert.True(callback.WasLockHeld);
                            Assert.False(apiLock.IsHeldByCurrentThread);
                        }
                        finally
                        {
                            Marshal.Release(enumeration);
                        }
                    }
                    finally
                    {
                        Marshal.Release(iface);
                    }
                }
            }
            finally
            {
                Marshal.Release(identity);
            }
        }
        finally
        {
            if (enumerateFirst)
                ComInterfaceMarshaller<ICLRDataEnumMemoryRegions>.Free(entry);
            else
                ComInterfaceMarshaller<IXCLRDataProcess>.Free(entry);
        }
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void Enumeration_FlushesCachesAndRestoresCallbacksOnEveryCall(bool nativeDescriptor, bool littleEndian, bool is64Bit)
    {
        const ulong PointerAddress = 0x1000;
        TargetTestHelpers helpers = new(new() { IsLittleEndian = littleEndian, Is64Bit = is64Bit });
        ContractDescriptorBuilder builder = new(helpers);
        byte[] pointerBytes = new byte[helpers.PointerSize];
        helpers.WritePointer(pointerBytes, 0x9000);
        builder.AddHeapFragment(new() { Address = PointerAddress, Data = pointerBytes, Name = "CachedPointer" });
        ContractDescriptorBuilder.DescriptorBuilder descriptor = new(builder);
        descriptor.SetContracts(["RuntimeTypeSystem"]);
        ulong descriptorAddress = descriptor.CreateSubDescriptor(0x2000, 0x3000, 0x4000);
        MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
        Action<ContractRegistry>[] registrations =
        [
            registry => registry.Register<IRuntimeTypeSystem>("c1", target =>
            {
                TargetPointer? cached = null;
                Mock<IRuntimeTypeSystem> types = new();
                types.Setup(t => t.GetWellKnownMethodTable(It.IsAny<WellKnownMethodTable>()))
                    .Returns(() => cached ??= target.ProcessedData.GetOrAdd<CachedPointer>(new(PointerAddress)).Value);
                types.Setup(t => t.Flush(FlushScope.All)).Callback(() => cached = null);
                return types.Object;
            })
        ];
        ContractDescriptorTarget target = nativeDescriptor
            ? ContractDescriptorTarget.Create(descriptorAddress, memory.ReadFromTarget, memory.WriteToTarget,
                (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
                (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; }, [])
            : ContractDescriptorTarget.Create(
                ContractDescriptorParser.ParseCompact("""{"version":2,"contracts":{"RuntimeTypeSystem":"c1"}}"""u8),
                [], memory.ReadFromTarget, memory.WriteToTarget,
                (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
                (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; },
                littleEndian, helpers.PointerSize);

        Assert.False(target.TryGetRuntimeImageBase(out TargetPointer imageBase));
        Assert.Equal(TargetPointer.Null, imageBase);

        // Registrations added after target construction must also participate in collection.
        foreach (Action<ContractRegistry> register in registrations)
            register(target.Contracts);

        Assert.Equal(new TargetPointer(0x9000), target.Contracts.RuntimeTypeSystem.GetWellKnownMethodTable(WellKnownMethodTable.Object));
        IRuntimeTypeSystem originalContract = target.Contracts.RuntimeTypeSystem;
        CachedPointer originalData = target.ProcessedData.GetOrAdd<CachedPointer>(new(PointerAddress));
        ICLRDataEnumMemoryRegions impl = new SOSDacImpl(target, legacyObj: null, new());
        for (int i = 0; i < 2; i++)
        {
            using RecordingCallback callback = new(supportsUpdates: false);
            Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_HEAP2));
            Assert.Contains(PointerAddress, callback.Regions);
            Assert.Same(originalContract, target.Contracts.RuntimeTypeSystem);
            CachedPointer currentData = target.ProcessedData.GetOrAdd<CachedPointer>(new(PointerAddress));
            Assert.NotSame(originalData, currentData);
            originalData = currentData;
            Mock.Get(originalContract).Verify(contract => contract.Flush(FlushScope.All), Times.Exactly(i + 1));
            if (nativeDescriptor)
                Assert.Contains(descriptorAddress, callback.Regions);

            int count = callback.Ranges.Count;
            Assert.Equal(new TargetPointer(0x9000), target.ReadPointer(PointerAddress));
            Assert.Equal(count, callback.Ranges.Count);
        }

        using RecordingCallback cancelled = new(supportsUpdates: false);
        cancelled.Result = HResults.COR_E_OPERATIONCANCELED;
        Assert.Equal(HResults.COR_E_OPERATIONCANCELED, impl.EnumMemoryRegions((void*)cancelled.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        using RecordingCallback afterCancellation = new(supportsUpdates: false);
        Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)afterCancellation.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.Contains(PointerAddress, afterCancellation.Regions);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void DescriptorMemory_ReportsCachedRangesWithoutReading(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        ContractDescriptorBuilder builder = new(helpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptor = new(builder);
        descriptor.SetGlobals([("Value", null, 0, null, null)], [0x9000]);
        ulong descriptorAddress = descriptor.CreateSubDescriptor(0x2000, 0x3000, 0x4000);
        MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
        List<ulong> reads = [];
        ContractDescriptorTarget target = ContractDescriptorTarget.Create(descriptorAddress,
            (address, buffer) =>
            {
                reads.Add(address);
                return memory.ReadFromTarget(address, buffer);
            },
            memory.WriteToTarget, (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; }, []);

        uint jsonSize = target.Read<uint>(descriptorAddress + 12);
        for (int i = 0; i < 2; i++)
        {
            reads.Clear();
            using IDisposable readScope = target.RegisterReadCallback((_, _) => Assert.Fail("Descriptor enumeration must not read target memory."));
            Assert.Collection(target.EnumerateDescriptorMemory(),
                range =>
                {
                    Assert.Equal(new TargetPointer(descriptorAddress), range.Address);
                    Assert.Equal((ulong)ContractDescriptorHelpers.Size(arch.Is64Bit), range.Size);
                },
                range =>
                {
                    Assert.Equal(new TargetPointer(0x3000), range.Address);
                    Assert.Equal((ulong)jsonSize, range.Size);
                },
                range =>
                {
                    Assert.Equal(new TargetPointer(0x4000), range.Address);
                    Assert.Equal((ulong)helpers.PointerSize, range.Size);
                });
            Assert.Equal(new TargetPointer(0x9000), target.ReadGlobalPointer("Value"));
            Assert.Empty(reads);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Enumeration_RejectsNonDescriptorTargetWithoutChangingCaches(MockTarget.Architecture arch)
    {
        const ulong PointerAddress = 0x1000;
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        byte[] pointerBytes = new byte[helpers.PointerSize];
        helpers.WritePointer(pointerBytes, 0x9000);
        builder.AddHeapFragment(new() { Address = PointerAddress, Data = pointerBytes, Name = nameof(CachedPointer) });
        TestPlaceholderTarget target = new(arch, builder.GetMemoryContext().ReadFromTarget);
        TestPlaceholderTarget.TestContractRegistry registry = target.SetupContractRegistry(registry =>
            registry.Register<IRuntimeTypeSystem>("c1", boundTarget =>
            {
                Mock<IRuntimeTypeSystem> types = new();
                types.Setup(t => t.GetWellKnownMethodTable(It.IsAny<WellKnownMethodTable>()))
                    .Returns(() => boundTarget.ProcessedData.GetOrAdd<CachedPointer>(new(PointerAddress)).Value);
                return types.Object;
            }));
        registry.SetVersion<IRuntimeTypeSystem>("c1");
        IRuntimeTypeSystem analysisContract = target.Contracts.RuntimeTypeSystem;
        Assert.Equal(new TargetPointer(0x9000), analysisContract.GetWellKnownMethodTable(WellKnownMethodTable.Object));
        CachedPointer analysisData = target.ProcessedData.GetOrAdd<CachedPointer>(new(PointerAddress));
        ICLRDataEnumMemoryRegions impl = new SOSDacImpl(target, legacyObj: null, new());

        for (int i = 0; i < 2; i++)
        {
            using RecordingCallback callback = new(supportsUpdates: false);
            Assert.Equal(HResults.E_NOTIMPL, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
            Assert.Empty(callback.Regions);
            Assert.Same(analysisContract, target.Contracts.RuntimeTypeSystem);
            CachedPointer collectionData = target.ProcessedData.GetOrAdd<CachedPointer>(new(PointerAddress));
            Assert.Same(analysisData, collectionData);
            Mock.Get(analysisContract).Verify(contract => contract.Flush(It.IsAny<FlushScope>()), Times.Never);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadShim_ReportsEverySuccessfulMemoryRead(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        ContractDescriptorBuilder builder = new(helpers);
        const ulong ValueAddress = 0x1000;
        const ulong Utf8Address = 0x2000;
        const ulong Utf16Address = 0x3000;
        const ulong EmptyAddress = 0x4000;
        const ulong InvalidUtf8Address = 0x5000;
        const ulong UnterminatedAddress = 0x6000;
        const ulong UnreadableAddress = 0x9000;
        const string Text = "text \u00e9 \uD83D\uDE00";
        Encoding utf16 = arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
        AddBytes(ValueAddress, [1, 2, 3, 4, 5, 6, 7, 8]);
        AddBytes(Utf8Address, Encoding.UTF8.GetBytes(Text + '\0'));
        AddBytes(Utf16Address, utf16.GetBytes(Text + '\0'));
        AddBytes(EmptyAddress, [0, 0]);
        AddBytes(InvalidUtf8Address, [0xc0, 0xaf, 0]);
        AddBytes(UnterminatedAddress, [0x61, 0x62]);
        string longText = new('a', 1100);
        AddBytes(0x7000, Encoding.UTF8.GetBytes(longText + '\0'));
        AddBytes(0x8000, utf16.GetBytes(longText + '\0'));

        uint codePointerSize = arch.Is64Bit ? 4u : 8u;
        ContractDescriptorBuilder.DescriptorBuilder descriptor = new(builder);
        descriptor.SetTypes(new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.CodePointer] = new() { Size = codePointerSize, Fields = new Dictionary<string, Target.FieldInfo>() },
        });
        descriptor.SetGlobals([("Number", 42, null, null), ("Pointer", 0x9876, null, null), ("Text", null, "hello", null)]);
        ulong descriptorAddress = descriptor.CreateSubDescriptor(0x10000, 0x11000, 0x12000);
        MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
        List<(ulong Address, ulong Size)> underlyingReads = [];
        List<(ulong Address, ulong Size)> recordedReads = [];
        ContractDescriptorTarget recording = ContractDescriptorTarget.Create(descriptorAddress,
            (address, buffer) =>
            {
                int hr = memory.ReadFromTarget(address, buffer);
                if (hr >= 0)
                    underlyingReads.Add((address, (ulong)buffer.Length));
                return hr;
            },
            memory.WriteToTarget, (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0x1234; return HResults.S_OK; }, []);
        bool cancelReads = false;
        using IDisposable readScope = recording.RegisterReadCallback((address, size) =>
        {
            if (cancelReads)
                throw new OperationCanceledException();
            recordedReads.Add((address, size));
        });
        ulong value32 = arch.IsLittleEndian ? 0x04030201u : 0x01020304u;
        ulong value64 = arch.IsLittleEndian ? 0x0807060504030201ul : 0x0102030405060708ul;
        ulong pointerValue = arch.Is64Bit ? value64 : value32;
        ulong codePointerValue = codePointerSize == 4 ? value32 : value64;

        VerifyRead(() => Assert.Equal((uint)value32, recording.Read<uint>(ValueAddress)));
        VerifyRead(() => Assert.Equal(value64, recording.Read<ulong>(ValueAddress)));
        VerifyRead(() =>
        {
            Assert.True(recording.TryRead(ValueAddress, out uint value));
            Assert.Equal((uint)value32, value);
        });
        VerifyRead(() => Assert.Equal(0x04030201u, recording.ReadLittleEndian<uint>(ValueAddress)));
        VerifyRead(() => Assert.Equal(new TargetPointer(pointerValue), recording.ReadPointer(ValueAddress)));
        VerifyRead(() =>
        {
            Assert.True(recording.TryReadPointer(ValueAddress, out TargetPointer value));
            Assert.Equal(new TargetPointer(pointerValue), value);
        });
        VerifyRead(() => Assert.Equal(new TargetCodePointer(codePointerValue), recording.ReadCodePointer(ValueAddress)));
        VerifyRead(() =>
        {
            Assert.True(recording.TryReadCodePointer(ValueAddress, out TargetCodePointer value));
            Assert.Equal(new TargetCodePointer(codePointerValue), value);
        });
        VerifyRead(() => Assert.Equal(new TargetNUInt(pointerValue), recording.ReadNUInt(ValueAddress)));
        VerifyRead(() => Assert.Equal(new TargetNInt((long)pointerValue), recording.ReadNInt(ValueAddress)));
        VerifyRead(() =>
        {
            byte[] bytes = new byte[3];
            recording.ReadBuffer(ValueAddress, bytes);
            Assert.Equal(new byte[] { 1, 2, 3 }, bytes);
        });
        VerifyRead(() => Assert.Equal(Text, recording.ReadUtf8String(Utf8Address)));
        VerifyRead(() => Assert.Equal(Text, recording.ReadUtf8String(Utf8Address, strict: true)));
        VerifyRead(() => Assert.Equal(Text, recording.ReadUtf16String(Utf16Address)));
        VerifyRead(() => Assert.Equal(longText, recording.ReadUtf8String(0x7000)));
        VerifyRead(() => Assert.Equal(longText, recording.ReadUtf16String(0x8000)));
        VerifyRead(() => Assert.Empty(recording.ReadUtf8String(EmptyAddress)));
        VerifyRead(() => Assert.Empty(recording.ReadUtf16String(EmptyAddress)));
        VerifyRead(() => Assert.Equal("\ufffd\ufffd", recording.ReadUtf8String(InvalidUtf8Address)));
        VerifyRead(() => Assert.Throws<DecoderFallbackException>(() => recording.ReadUtf8String(InvalidUtf8Address, strict: true)));
        VerifyRead(() => Assert.Throws<VirtualReadException>(() => recording.ReadUtf8String(UnterminatedAddress)));

        VerifyRead(() => Assert.False(recording.TryRead<uint>(UnreadableAddress, out _)), expectReads: false);
        VerifyRead(() => Assert.False(recording.TryReadPointer(UnreadableAddress, out _)), expectReads: false);
        VerifyRead(() => Assert.False(recording.TryReadCodePointer(UnreadableAddress, out _)), expectReads: false);
        VerifyRead(() => Assert.Throws<VirtualReadException>(() => recording.Read<uint>(UnreadableAddress)), expectReads: false);
        VerifyRead(() => Assert.Throws<VirtualReadException>(() => recording.ReadLittleEndian<uint>(UnreadableAddress)), expectReads: false);
        VerifyRead(() => Assert.Throws<VirtualReadException>(() => recording.ReadNUInt(UnreadableAddress)), expectReads: false);
        VerifyRead(() => Assert.Throws<VirtualReadException>(() => recording.ReadNInt(UnreadableAddress)), expectReads: false);
        VerifyRead(() => Assert.Throws<VirtualReadException>(() => recording.ReadPointer(UnreadableAddress)), expectReads: false);
        VerifyRead(() => Assert.Throws<VirtualReadException>(() => recording.ReadCodePointer(UnreadableAddress)), expectReads: false);
        VerifyRead(() => Assert.Throws<VirtualReadException>(() => recording.ReadBuffer(UnreadableAddress, new byte[1])), expectReads: false);

        VerifyRead(() =>
        {
            Assert.Equal(42u, recording.ReadGlobal<uint>("Number"));
            Assert.True(recording.TryReadGlobal<uint>("Number", out uint? number));
            Assert.Equal(42u, number);
            Assert.Equal(new TargetPointer(0x9876), recording.ReadGlobalPointer("Pointer"));
            Assert.True(recording.TryReadGlobalPointer("Pointer", out TargetPointer? pointer));
            Assert.Equal(new TargetPointer(0x9876), pointer);
            Assert.Equal("hello", recording.ReadGlobalString("Text"));
            Assert.True(recording.TryReadGlobalString("Text", out string? text));
            Assert.Equal("hello", text);
            Assert.False(recording.TryReadGlobal<uint>("Missing", out _));
            Assert.False(recording.TryReadGlobalPointer("Missing", out _));
            Assert.False(recording.TryReadGlobalString("Missing", out _));
            Assert.Equal(codePointerSize, recording.GetTypeInfo(nameof(DataType.CodePointer)).Size);
            Assert.True(recording.TryGetTypeInfo(nameof(DataType.CodePointer), out _));
            Assert.False(recording.TryGetTypeInfo("Missing", out _));
            Assert.Equal(new TargetPointer(pointerValue), recording.ReadPointerFromSpan([1, 2, 3, 4, 5, 6, 7, 8]));
            Assert.True(recording.IsAlignedToPointerSize(new TargetPointer(ValueAddress)));
            Assert.False(recording.IsAlignedToPointerSize(new TargetPointer(ValueAddress + 1)));
            Assert.Equal(new TargetPointer(0x1234), recording.AllocateMemory(16));
        }, expectReads: false);

        VerifyRead(() => recording.Write<int>(ValueAddress, -1), expectReads: false);
        VerifyRead(() => Assert.Equal(-1, recording.Read<int>(ValueAddress)));
        VerifyRead(() => recording.WritePointer(ValueAddress, new(0x9876)), expectReads: false);
        VerifyRead(() => Assert.Equal(new TargetPointer(0x9876), recording.ReadPointer(ValueAddress)));
        VerifyRead(() => recording.WriteNUInt(ValueAddress, new(0x7654)), expectReads: false);
        VerifyRead(() => Assert.Equal(new TargetNUInt(0x7654), recording.ReadNUInt(ValueAddress)));
        VerifyRead(() => recording.WriteBuffer(ValueAddress, [0xff]), expectReads: false);
        VerifyRead(() => Assert.Equal(byte.MaxValue, recording.Read<byte>(ValueAddress)));

        cancelReads = true;
        Assert.Throws<OperationCanceledException>(() => recording.TryReadPointer(ValueAddress, out _));
        Assert.Throws<OperationCanceledException>(() => recording.TryRead<uint>(ValueAddress, out _));
        Assert.Throws<OperationCanceledException>(() => recording.TryReadCodePointer(ValueAddress, out _));
        readScope.Dispose();
        Assert.True(recording.TryReadPointer(ValueAddress, out _));

        void AddBytes(ulong address, byte[] bytes) => builder.AddHeapFragment(new() { Address = address, Data = bytes, Name = $"Data_{address:x}" });

        void VerifyRead(Action action, bool expectReads = true)
        {
            underlyingReads.Clear();
            recordedReads.Clear();
            action();
            Assert.Equal(underlyingReads, recordedReads);
            if (expectReads)
                Assert.NotEmpty(recordedReads);
            else
                Assert.Empty(recordedReads);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Enumeration_IncludesNativeMetadataAndNewlyPublishedSubDescriptors(MockTarget.Architecture arch)
    {
        foreach (bool nativeMainDescriptor in new[] { false, true })
        {
            TargetTestHelpers helpers = new(arch);
            ContractDescriptorBuilder builder = new(helpers);
            const ulong SlotAddress = 0x1000;
            builder.AddHeapFragment(new() { Address = SlotAddress, Data = new byte[helpers.PointerSize], Name = "SubDescriptorSlot" });
            ContractDescriptorBuilder.DescriptorBuilder main = new(builder);
            main.SetSubDescriptors([("GC", 0)]);
            main.SetIndirectValues([SlotAddress]);
            ulong mainAddress = main.CreateSubDescriptor(0x2000, 0x3000, 0x4000);
            ContractDescriptorBuilder.DescriptorBuilder child = new(builder);
            child.SetIndirectValues([0x9876]);
            child.SetGlobals([("ChildValue", 0x2345, null)]);
            ulong childAddress = child.CreateSubDescriptor(0x5000, 0x6000, 0x7000);
            MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
            ContractDescriptorTarget target = nativeMainDescriptor
                ? ContractDescriptorTarget.Create(mainAddress, memory.ReadFromTarget, memory.WriteToTarget,
                    (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
                    (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; }, [])
                : ContractDescriptorTarget.Create(ContractDescriptorParser.ParseCompact("""{"version":2,"subDescriptors":{"GC":[0]}}"""u8),
                    [new(SlotAddress)], memory.ReadFromTarget, memory.WriteToTarget,
                    (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
                    (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; },
                    arch.IsLittleEndian, helpers.PointerSize);
            ICLRDataEnumMemoryRegions impl = new SOSDacImpl(target, legacyObj: null, new());
            using RecordingCallback callback = new(supportsUpdates: false);
            Assert.False(target.IsSubDescriptorResolved("GC"));
            Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
            Assert.Contains((SlotAddress, (uint)helpers.PointerSize), callback.Ranges);
            Assert.DoesNotContain(childAddress, callback.Regions);
            Assert.Equal(nativeMainDescriptor, callback.Regions.Contains(mainAddress));

            target.WritePointer(SlotAddress, new(childAddress));
            callback.Regions.Clear();
            callback.Ranges.Clear();
            Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
            Assert.True(target.IsSubDescriptorResolved("GC"));
            Assert.Equal(0x2345u, target.ReadGlobal<uint>("ChildValue"));
            Assert.Contains((SlotAddress, (uint)helpers.PointerSize), callback.Ranges);
            AssertFullyReported(callback, childAddress, (uint)ContractDescriptorHelpers.Size(arch.Is64Bit));
            Assert.Contains((0x7000ul, (uint)helpers.PointerSize), callback.Ranges);
            Assert.Contains((0x6000ul, target.Read<uint>(childAddress + 12)), callback.Ranges);
            Assert.Equal(nativeMainDescriptor, callback.Regions.Contains(mainAddress));
            if (nativeMainDescriptor)
            {
                AssertFullyReported(callback, mainAddress, (uint)ContractDescriptorHelpers.Size(arch.Is64Bit));
                Assert.Contains((0x4000ul, (uint)helpers.PointerSize), callback.Ranges);
                Assert.Contains((0x3000ul, target.Read<uint>(mainAddress + 12)), callback.Ranges);
            }

            callback.Regions.Clear();
            Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
            Assert.Contains(childAddress, callback.Regions);
            Assert.Contains(0x6000ul, callback.Regions);
            Assert.Contains(0x7000ul, callback.Regions);
        }
    }

    [Fact]
    public void Enumeration_PreservesContractInstancesAndVersionResolution()
    {
        ContractDescriptorTarget target = ContractDescriptorTarget.Create(
            ContractDescriptorParser.ParseCompact("""{"version":2,"contracts":{"GC":"unsupported","Loader":"unknown"}}"""u8), [],
            (_, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL, (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; }, isLittleEndian: true, pointerSize: 8);
        target.Contracts.RegisterUnsupported<IGC>("unsupported");
        target.Contracts.Register<IFeatureFlags>(string.Empty, _ => new Mock<IFeatureFlags>().Object);
        IFeatureFlags original = target.Contracts.FeatureFlags;
        ICLRDataEnumMemoryRegions impl = new SOSDacImpl(target, legacyObj: null, new());
        using RecordingCallback callback = new(supportsUpdates: false);
        Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.Same(original, target.Contracts.FeatureFlags);
        Mock.Get(original).Verify(contract => contract.Flush(FlushScope.All), Times.Once);
        Assert.Throws<ContractObsoleteException>(() => target.Contracts.GC);
        Assert.Throws<ContractUnrecognizedException>(() => target.Contracts.Loader);
        Assert.Throws<ContractMissingException>(() => target.Contracts.Thread);
        target.Contracts.Register<IGC>("unsupported", _ => new Mock<IGC>().Object);
        IGC gc = target.Contracts.GC;
        Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.Same(gc, target.Contracts.GC);
        Assert.Same(original, target.Contracts.FeatureFlags);
        Mock.Get(gc).Verify(contract => contract.Flush(FlushScope.All), Times.Once);
        Mock.Get(original).Verify(contract => contract.Flush(FlushScope.All), Times.Exactly(2));
    }

    [Theory]
    [InlineData(0x5000ul)]
    [InlineData(0x6000ul)]
    [InlineData(0x7000ul)]
    [InlineData(0x1100ul)]
    public void Enumeration_CompletesDescriptorDiscoveryBeforeReportingCancellation(ulong cancelAtAddress)
    {
        TargetTestHelpers helpers = new(new() { IsLittleEndian = true, Is64Bit = true });
        ContractDescriptorBuilder builder = new(helpers);
        const ulong MainSlot = 0x1000;
        const ulong ChildSlot = 0x1100;
        builder.AddHeapFragment(new() { Address = MainSlot, Data = new byte[helpers.PointerSize], Name = "MainSlot" });
        builder.AddHeapFragment(new() { Address = ChildSlot, Data = new byte[helpers.PointerSize], Name = "ChildSlot" });
        ContractDescriptorBuilder.DescriptorBuilder main = new(builder);
        main.SetSubDescriptors([("Child", 0)]);
        main.SetIndirectValues([MainSlot]);
        ulong mainAddress = main.CreateSubDescriptor(0x2000, 0x3000, 0x4000);
        ContractDescriptorBuilder.DescriptorBuilder child = new(builder);
        child.SetSubDescriptors([("Grandchild", 0)]);
        child.SetIndirectValues([ChildSlot]);
        child.SetGlobals([("ChildValue", 0x2345, null)]);
        ulong childAddress = child.CreateSubDescriptor(0x5000, 0x6000, 0x7000);
        MockMemorySpace.MemoryContext memory = builder.GetMemoryContext();
        ContractDescriptorTarget target = ContractDescriptorTarget.Create(mainAddress, memory.ReadFromTarget, memory.WriteToTarget,
            (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; }, []);
        target.WritePointer(MainSlot, new(childAddress));
        ICLRDataEnumMemoryRegions impl = new SOSDacImpl(target, legacyObj: null, new());
        using RecordingCallback cancelled = new(supportsUpdates: false) { CancelAtAddress = cancelAtAddress };
        Assert.Equal(HResults.COR_E_OPERATIONCANCELED, impl.EnumMemoryRegions((void*)cancelled.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.True(target.IsSubDescriptorResolved("Child"));
        int count = cancelled.Ranges.Count;
        Assert.Equal(new TargetPointer(childAddress), target.ReadPointer(MainSlot));
        Assert.Equal(count, cancelled.Ranges.Count);

        using RecordingCallback callback = new(supportsUpdates: false);
        Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.True(target.IsSubDescriptorResolved("Child"));
        Assert.False(target.IsSubDescriptorResolved("Grandchild"));
        Assert.Equal(0x2345u, target.ReadGlobal<uint>("ChildValue"));
        AssertFullyReported(callback, childAddress, (uint)ContractDescriptorHelpers.Size(is64Bit: true));
        Assert.Contains((0x6000ul, target.Read<uint>(childAddress + 12)), callback.Ranges);
        Assert.Contains((0x7000ul, (uint)helpers.PointerSize), callback.Ranges);
        Assert.Contains((ChildSlot, (uint)helpers.PointerSize), callback.Ranges);
    }

    [Theory]
    [InlineData(HResults.S_OK)]
    [InlineData(HResults.E_FAIL)]
    [InlineData(HResults.COR_E_OPERATIONCANCELED)]
    public void Enumeration_RecordsContractReadsAndRestoresReader(int result)
    {
        const ulong PointerAddress = 0x1000;
        ContractDescriptorTarget target = ContractDescriptorTarget.Create(
            ContractDescriptorParser.ParseCompact("""{"version":2,"contracts":{"RuntimeTypeSystem":"c1"}}"""u8), [],
            (_, buffer) => { buffer.Clear(); return HResults.S_OK; },
            (_, _) => HResults.E_NOTIMPL, (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; },
            isLittleEndian: true, pointerSize: 8);
        Lock apiLock = new();
        target.Contracts.Register<IRuntimeTypeSystem>("c1", boundTarget =>
        {
            Assert.True(apiLock.IsHeldByCurrentThread);
            Assert.Same(target, boundTarget);
            TargetPointer? cached = boundTarget.ReadPointer(PointerAddress);
            Mock<IRuntimeTypeSystem> types = new();
            types.Setup(t => t.GetWellKnownMethodTable(It.IsAny<WellKnownMethodTable>()))
                .Returns(() => cached ??= boundTarget.ReadPointer(PointerAddress));
            types.Setup(t => t.Flush(FlushScope.All)).Callback(() => cached = null);
            return types.Object;
        });
        ICLRDataEnumMemoryRegions impl = new SOSDacImpl(target, legacyObj: null, apiLock);
        using RecordingCallback callback = new(supportsUpdates: false, apiLock) { Result = result };

        Assert.Equal(result, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.Contains((PointerAddress, (uint)target.PointerSize), callback.Ranges);
        Assert.True(callback.WasLockHeld);
        Assert.False(apiLock.IsHeldByCurrentThread);
        int count = callback.Ranges.Count;
        Assert.Equal(TargetPointer.Null, target.ReadPointer(PointerAddress));
        Assert.Equal(count, callback.Ranges.Count);

        using RecordingCallback next = new(supportsUpdates: false);
        Assert.Equal(HResults.S_OK, impl.EnumMemoryRegions((void*)next.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.Contains(PointerAddress, next.Regions);
        Assert.Equal(count, callback.Ranges.Count);
    }

    [Fact]
    public void ReadShimScopes_RestorePreviousReadersWithoutChangingCaches()
    {
        ContractDescriptorTarget target = ContractDescriptorTarget.Create(
            ContractDescriptorParser.ParseCompact("""{"version":2}"""u8), [],
            (_, buffer) => { buffer.Clear(); return HResults.S_OK; },
            (_, _) => HResults.E_NOTIMPL, (_, _, _) => HResults.E_NOTIMPL, (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; },
            isLittleEndian: true, pointerSize: 8);
        int firstReads = 0;
        int secondReads = 0;
        Assert.Empty(target.EnumerateDescriptorMemory());
        CachedPointer originalData = target.ProcessedData.GetOrAdd<CachedPointer>(new(0x1000));
        IDisposable first = target.RegisterReadCallback((_, _) => firstReads++);
        using (first)
        {
            Assert.Equal(TargetPointer.Null, target.ReadPointer(0x1000));
            using (target.RegisterReadCallback((_, _) => secondReads++))
                Assert.Equal(TargetPointer.Null, target.ReadPointer(0x1000));

            Assert.Equal(TargetPointer.Null, target.ReadPointer(0x1000));
        }
        using (target.RegisterReadCallback((_, _) => secondReads++))
        {
            first.Dispose();
            Assert.Equal(TargetPointer.Null, target.ReadPointer(0x1000));
        }
        Assert.Same(originalData, target.ProcessedData.GetOrAdd<CachedPointer>(new(0x1000)));
        Assert.Equal(TargetPointer.Null, target.ReadPointer(0x1000));
        Assert.Equal(3, firstReads);
        Assert.Equal(2, secondReads);
    }

    [Fact]
    public void Enumeration_RejectsOtherTargetImplementationsAtMemoryEnumeratorBoundary()
    {
        Mock<Target> target = new();
        target.SetupGet(t => t.PointerSize).Returns(8);
        ICLRDataEnumMemoryRegions impl = new SOSDacImpl(target.Object, legacyObj: null, new());
        using RecordingCallback callback = new(supportsUpdates: false);

        MemoryRegionEmitter emitter = new(callback.Address, (uint)target.Object.PointerSize);
        Assert.Equal(HResults.E_NOTIMPL, MemoryEnumerator.Enumerate(target.Object, emitter, DumpType.Mini));
        Assert.Equal(HResults.E_NOTIMPL, impl.EnumMemoryRegions((void*)callback.Address, 0, CLRDataEnumMemoryFlags.CLRDATA_ENUM_MEM_DEFAULT));
        Assert.Empty(callback.Ranges);
        target.Verify(t => t.Flush(It.IsAny<FlushScope>()), Times.Never);
    }

    [Fact]
    public void ReadShim_PreservesDataAccessAndRuntimeImageBase()
    {
        const ulong RuntimeImageBase = 0x9000;
        ContractDescriptorTarget target = ContractDescriptorTarget.Create(
            ContractDescriptorParser.ParseCompact("""{"version":2}"""u8), [],
            (address, buffer) =>
            {
                Assert.Equal(0x1000ul, address);
                buffer.Fill(0x11);
                return HResults.S_OK;
            },
            (address, buffer) =>
            {
                Assert.Equal(0x2000ul, address);
                Assert.Equal(new byte[] { 0x5a }, buffer.ToArray());
                return HResults.S_OK;
            },
            (threadId, flags, buffer) =>
            {
                Assert.Equal(5u, threadId);
                Assert.Equal(0x10u, flags);
                buffer.Fill(0x44);
                return HResults.S_OK;
            },
            (threadId, context) =>
            {
                Assert.Equal(6u, threadId);
                Assert.Equal(new byte[] { 0x99 }, context.ToArray());
                return HResults.S_OK;
            },
            (ulong size, out ulong address) =>
            {
                Assert.Equal(16ul, size);
                address = 0x8000;
                return HResults.S_OK;
            },
            isLittleEndian: true, pointerSize: 8, runtimeImageBase: RuntimeImageBase);
        List<(ulong Address, ulong Size)> reads = [];
        using IDisposable readScope = target.RegisterReadCallback((address, size) => reads.Add((address, size)));
        Assert.Equal((byte)0x11, target.Read<byte>(0x1000));
        target.WriteBuffer(0x2000, [0x5a]);
        byte[] context = new byte[16];
        Assert.True(target.TryGetThreadContext(5, 0x10, context));
        Assert.All(context, value => Assert.Equal((byte)0x44, value));
        Assert.True(target.TrySetThreadContext(6, [0x99]));
        Assert.Equal(new TargetPointer(0x8000), target.AllocateMemory(16));
        Assert.True(target.TryGetRuntimeImageBase(out TargetPointer imageBase));
        Assert.Equal(new TargetPointer(RuntimeImageBase), imageBase);
        readScope.Dispose();
        Assert.True(target.TryGetRuntimeImageBase(out imageBase));
        Assert.Equal(new TargetPointer(RuntimeImageBase), imageBase);
        Assert.Equal((byte)0x11, target.Read<byte>(0x1000));
        Assert.Equal(new (ulong, ulong)[] { (0x1000, 1) }, reads);
    }

    private static void AssertFullyReported(RecordingCallback callback, ulong address, uint size)
    {
        for (uint offset = 0; offset < size; offset++)
        {
            ulong current = address + offset;
            Assert.Contains(callback.Ranges, range => current >= range.Address && current - range.Address < range.Size);
        }
    }

    [Theory]
    [InlineData(0, nameof(DumpType.Mini))]
    [InlineData(0x200, nameof(DumpType.Heap))]
    [InlineData(0x100000, nameof(DumpType.Triage))]
    [InlineData(0x100200, nameof(DumpType.Heap))]
    [InlineData(0x8000, nameof(DumpType.Mini))]
    [InlineData(0x108000, nameof(DumpType.Mini))]
    [InlineData(0x108200, nameof(DumpType.Heap))]
    public void DumpMode_UsesNativeMiniDumpFlagPrecedence(uint miniDumpFlags, string expected)
    {
        Assert.Equal(Enum.Parse<DumpType>(expected), SOSDacImpl.GetDumpType(miniDumpFlags));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   at Program.Main()", "   at Program.Main()")]
    [InlineData("   at Program.Main() in C:\\private\\Program.cs:line 42", "   at Program.Main()")]
    [InlineData("   at A.M() in /private/a.cs:line 1\n   at B.M() in /private/b.cs:line 2\n", "   at A.M()\n   at B.M()")]
    [InlineData("   at A.M() dans /private/a.cs:ligne 1\r\n   at B.M()\r\n", "   at A.M()\r\n   at B.M()")]
    [InlineData("   at A.M(F(Int32)) in secret.cs:line 1", "   at A.M(F(Int32))")]
    [InlineData("   at A.M(\uD83D\uDE00) in secret.cs:line 1", "   at A.M(\uD83D\uDE00)")]
    [InlineData("   at A.M()\n--- End of stack trace ---\n", "   at A.M()")]
    [InlineData("unstructured text", "")]
    public void TriageStackTrace_RemovesFileInfoAndZerosRemainingCharacters(string original, string expected)
    {
        char[] buffer = original.ToCharArray();
        Sanitizer.StripFileInfoFromStackTrace(buffer);
        Assert.Equal(expected.PadRight(original.Length, '\0'), new string(buffer));
    }

    [Theory]
    [InlineData(false, false, false, true, true)]
    [InlineData(false, true, true, true, true)]
    [InlineData(true, false, false, true, true)]
    [InlineData(true, true, false, true, true)]
    [InlineData(true, true, true, true, true)]
    [InlineData(true, true, false, false, true)]
    [InlineData(true, false, false, true, false)]
    [InlineData(true, true, true, true, false)]
    public void ExceptionCollection_UsesTriagePolicy(bool triage, bool derived, bool overridesStackTrace, bool littleEndian, bool supportsUpdates)
    {
        TargetPointer exception = new(0x1000);
        TargetPointer innerException = new(0x1100);
        TargetPointer message = new(0x2000);
        TargetPointer stackTrace = new(0x3000);
        TargetPointer remoteStackTrace = new(0x4000);
        TargetPointer exceptionTable = new(0x100);
        TargetPointer derivedTable = new(0x200);
        TargetPointer stringTable = new(0x300);
        TargetPointer objectTable = new(0x400);
        TargetPointer getter = new(0x500);
        const uint StringOffset = 12;
        const string Original = "   at Program.Main() in /private/Program.cs:line 42";
        const string Sanitized = "   at Program.Main()";
        Encoding encoding = littleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
        byte[] stringBytes = encoding.GetBytes(Original);

        Mock<IObject> objects = new();
        objects.Setup(o => o.GetSize(It.IsAny<TargetPointer>())).Returns(128);
        objects.Setup(o => o.GetMethodTableAddress(It.IsAny<TargetPointer>())).Returns(stringTable);
        objects.Setup(o => o.GetMethodTableAddress(exception)).Returns(derived ? derivedTable : exceptionTable);
        objects.Setup(o => o.GetMethodTableAddress(innerException)).Returns(exceptionTable);
        objects.Setup(o => o.GetStringValue(It.IsAny<TargetPointer>())).Returns(Original);
        uint stringLength = (uint)Original.Length;
        uint stringOffset = StringOffset;
        objects.Setup(o => o.GetStringData(It.IsAny<TargetPointer>(), out stringLength, out stringOffset));

        Mock<IRuntimeTypeSystem> types = new();
        types.Setup(t => t.GetTypeHandle(It.IsAny<TargetPointer>())).Returns((TargetPointer p) => new TestTypeHandle(p));
        types.Setup(t => t.GetWellKnownMethodTable(WellKnownMethodTable.Exception)).Returns(exceptionTable);
        types.Setup(t => t.GetWellKnownMethodTable(WellKnownMethodTable.String)).Returns(stringTable);
        types.Setup(t => t.GetWellKnownMethodTable(WellKnownMethodTable.Object)).Returns(objectTable);
        types.Setup(t => t.GetParentMethodTable(It.Is<ITypeHandle>(t => t.Address == derivedTable))).Returns(exceptionTable);
        types.Setup(t => t.GetNumVtableSlots(It.Is<ITypeHandle>(t => t.Address == exceptionTable))).Returns(2);
        types.Setup(t => t.GetNumVtableSlots(It.Is<ITypeHandle>(t => t.Address == objectTable))).Returns(1);
        types.Setup(t => t.GetMethodDescForSlot(It.Is<ITypeHandle>(t => t.Address == exceptionTable), 1)).Returns(getter);
        types.Setup(t => t.GetMethodDescForSlot(It.Is<ITypeHandle>(t => t.Address == derivedTable), 1))
            .Returns(overridesStackTrace ? new TargetPointer(0x600) : getter);
        types.Setup(t => t.GetMethodDescHandle(getter)).Returns(new MethodDescHandle(getter));
        types.Setup(t => t.GetMethodToken(It.IsAny<MethodDescHandle>())).Returns(0x06000001);

        MetadataBuilder metadata = new();
        metadata.AddModule(0, metadata.GetOrAddString("TestModule"), default, default, default);
        metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Virtual, MethodImplAttributes.IL,
            metadata.GetOrAddString("get_StackTrace"), default, 0, default);
        BlobBuilder blob = new();
        new MetadataRootBuilder(metadata).Serialize(blob, 0, 0);
        using MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(blob.ToArray()));
        Mock<IEcmaMetadata> ecmaMetadata = new();
        ecmaMetadata.Setup(m => m.GetMetadata(It.IsAny<Contracts.ModuleHandle>())).Returns(provider.GetMetadataReader());

        Mock<IException> exceptions = new();
        exceptions.Setup(e => e.GetExceptionData(exception))
            .Returns(new ExceptionData(message, innerException, TargetPointer.Null, TargetPointer.Null, stackTrace, remoteStackTrace, 0, 0));
        exceptions.Setup(e => e.GetExceptionData(innerException))
            .Returns(new ExceptionData(message, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null, 0, 0));
        exceptions.Setup(e => e.GetExceptionStackFrames(It.IsAny<TargetPointer>())).Returns([]);
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(new() { IsLittleEndian = littleEndian, Is64Bit = true })
            .AddMockContract(objects.Object)
            .AddMockContract(types.Object)
            .AddMockContract(exceptions.Object)
            .AddMockContract(ecmaMetadata.Object)
            .AddMockContract(new Mock<ILoader>().Object)
            .AddMockContract(new Mock<IFeatureFlags>().Object)
            .UseReader((ulong address, Span<byte> buffer) =>
            {
                Assert.True(address == stackTrace.Value + StringOffset || address == remoteStackTrace.Value + StringOffset);
                stringBytes.CopyTo(buffer);
                return 0;
            })
            .Build();

        using RecordingCallback callback = new(supportsUpdates);
        MemoryRegionEmitter emitter = new(callback.Address, 8);
        new ObjectCollector(target, emitter, new MethodCollector(target, emitter), triage ? DumpType.Triage : DumpType.Mini).EnumerateObject(exception);

        Assert.Contains(exception.Value, callback.Regions);
        Assert.Contains(innerException.Value, callback.Regions);
        Assert.Equal(!triage, callback.Regions.Contains(message.Value));
        Assert.Contains(stackTrace.Value, callback.Regions);
        Assert.Equal(!triage || !overridesStackTrace, callback.Regions.Contains(remoteStackTrace.Value));
        if (triage && supportsUpdates)
        {
            byte[] expected = encoding.GetBytes(Sanitized.PadRight(Original.Length, '\0'));
            Assert.Equal(expected, callback.Updates[stackTrace.Value + StringOffset]);
            if (!overridesStackTrace)
                Assert.Equal(expected, callback.Updates[remoteStackTrace.Value + StringOffset]);
            Assert.Equal(overridesStackTrace ? 1 : 2, callback.Updates.Count);
        }
        else
        {
            Assert.Empty(callback.Updates);
        }
        Assert.Equal(0, emitter.Result);
    }

    [Theory]
    [InlineData(false, nameof(DumpType.Mini))]
    [InlineData(true, nameof(DumpType.Mini))]
    [InlineData(true, nameof(DumpType.Heap))]
    [InlineData(true, nameof(DumpType.Triage))]
    [InlineData(false, nameof(DumpType.Mini), CodePointerFlags.HasArm32ThumbBit)]
    public void ExceptionCollection_EnumeratesInstructionPointersForSavedFrames(bool is64Bit, string dumpType, CodePointerFlags codePointerFlags = default)
    {
        TargetPointer exception = new(0x1000);
        TargetPointer exceptionTable = new(0x2000);
        TargetPointer savedIp = new(0x8001);
        ulong expectedIp = codePointerFlags == CodePointerFlags.HasArm32ThumbBit ? 0x8000ul : 0x8001ul;
        uint pointerSize = is64Bit ? 8u : 4u;
        Mock<IObject> objects = new();
        objects.Setup(o => o.GetSize(exception)).Returns(128);
        objects.Setup(o => o.GetMethodTableAddress(exception)).Returns(exceptionTable);
        Mock<IRuntimeTypeSystem> types = new();
        types.Setup(t => t.GetTypeHandle(exceptionTable)).Returns(new TestTypeHandle(exceptionTable));
        types.Setup(t => t.GetWellKnownMethodTable(WellKnownMethodTable.Exception)).Returns(exceptionTable);
        Mock<IException> exceptions = new();
        exceptions.Setup(e => e.GetExceptionStackFrames(exception)).Returns(
        [
            new ExceptionStackFrameInfo(savedIp, TargetPointer.Null, false),
            new ExceptionStackFrameInfo(new TargetPointer(savedIp.Value + 0x100), TargetPointer.Null, false),
        ]);
        Mock<IPlatformMetadata> platformMetadata = new();
        platformMetadata.Setup(p => p.GetCodePointerFlags()).Returns(codePointerFlags);
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(new() { IsLittleEndian = true, Is64Bit = is64Bit })
            .AddMockContract(objects.Object)
            .AddMockContract(types.Object)
            .AddMockContract(exceptions.Object)
            .AddMockContract(platformMetadata.Object)
            .AddMockContract(new Mock<IExecutionManager>().Object)
            .AddMockContract(new Mock<IFeatureFlags>().Object)
            .UseReader((ulong _, Span<byte> _) => -1)
            .Build();
        using RecordingCallback callback = new(supportsUpdates: false);
        MemoryRegionEmitter emitter = new(callback.Address, (uint)target.PointerSize);
        new ObjectCollector(target, emitter, new MethodCollector(target, emitter), Enum.Parse<DumpType>(dumpType)).EnumerateObject(exception);

        Assert.Contains((expectedIp, pointerSize), callback.Ranges);
        Assert.Contains((expectedIp + 0x100, pointerSize), callback.Ranges);
        Assert.Equal(HResults.S_OK, emitter.Result);
    }

    [Fact]
    public void MethodCollection_EnumeratesInstructionPointersBeforeCodeBlockDeduplication()
    {
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(It.IsAny<TargetCodePointer>()))
            .Returns(new CodeBlockHandle(new TargetPointer(0x2000)));
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(new() { IsLittleEndian = true, Is64Bit = true })
            .AddMockContract(new Mock<IPlatformMetadata>().Object)
            .AddMockContract(executionManager.Object)
            .AddMockContract(new Mock<IDebugInfo>().Object)
            .Build();
        using RecordingCallback callback = new(supportsUpdates: false);
        MemoryRegionEmitter emitter = new(callback.Address, (uint)target.PointerSize);
        MethodCollector methods = new(target, emitter);

        methods.CaptureMethod(TargetPointer.Null, new TargetCodePointer(0x8000));
        methods.CaptureMethod(TargetPointer.Null, new TargetCodePointer(0x8100));
        methods.CaptureMethod(TargetPointer.Null, new TargetCodePointer(0x8000));
        methods.CaptureMethod(TargetPointer.Null, TargetCodePointer.Null);

        Assert.Equal([(0x8000ul, 8u), (0x8100ul, 8u), (0x8000ul, 8u)], callback.Ranges);
        Assert.Single(executionManager.Invocations, invocation => invocation.Method.Name == nameof(IExecutionManager.GetGCInfo));
    }

    [Theory]
    [InlineData(false, 0x8000ul)]
    [InlineData(true, 0x8000ul)]
    [InlineData(false, 7ul)]
    [InlineData(true, 7ul)]
    [InlineData(false, 0xf0000000ul)]
    public void MethodCollection_CapturesOnlyInstructionPointerBytes(bool is64Bit, ulong instructionPointer)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(new() { IsLittleEndian = true, Is64Bit = is64Bit })
            .AddMockContract(new Mock<IPlatformMetadata>().Object)
            .AddMockContract(new Mock<IExecutionManager>().Object)
            .UseReader((ulong _, Span<byte> _) => throw new InvalidOperationException("IP collection must not read or decode instructions."))
            .Build();
        using RecordingCallback callback = new(supportsUpdates: false);
        MemoryRegionEmitter emitter = new(callback.Address, (uint)target.PointerSize);
        new MethodCollector(target, emitter).CaptureMethod(TargetPointer.Null, new TargetCodePointer(instructionPointer));

        ulong expectedAddress = is64Bit ? instructionPointer : unchecked((ulong)(long)(int)instructionPointer);
        Assert.Equal([(expectedAddress, is64Bit ? 8u : 4u)], callback.Ranges);
        Assert.Equal(HResults.S_OK, emitter.Result);
    }

    [Theory]
    [InlineData(true, 0ul)]
    [InlineData(true, ulong.MaxValue)]
    [InlineData(false, uint.MaxValue)]
    public void MethodCollection_InvalidInstructionPointerIsSkipped(bool is64Bit, ulong instructionPointer)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(new() { IsLittleEndian = true, Is64Bit = is64Bit })
            .AddMockContract(new Mock<IPlatformMetadata>().Object)
            .AddMockContract(new Mock<IExecutionManager>().Object)
            .Build();
        using RecordingCallback callback = new(supportsUpdates: false);
        MemoryRegionEmitter emitter = new(callback.Address, (uint)target.PointerSize);
        new MethodCollector(target, emitter).CaptureMethod(TargetPointer.Null, new TargetCodePointer(instructionPointer));

        Assert.Empty(callback.Ranges);
        Assert.Equal(HResults.S_OK, emitter.Result);
    }

    [Fact]
    public void MethodCollection_InstructionPointerCancellationPropagates()
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(new() { IsLittleEndian = true, Is64Bit = true })
            .AddMockContract(new Mock<IPlatformMetadata>().Object)
            .Build();
        using RecordingCallback callback = new(supportsUpdates: false) { Result = HResults.COR_E_OPERATIONCANCELED };
        MemoryRegionEmitter emitter = new(callback.Address, (uint)target.PointerSize);

        Assert.Throws<OperationCanceledException>(() =>
            new MethodCollector(target, emitter).CaptureMethod(TargetPointer.Null, new TargetCodePointer(0x8000)));
    }

    private sealed record TestTypeHandle(TargetPointer Address) : ITypeHandle;

    private sealed record CachedPointer(TargetPointer Value) : Data.IData<CachedPointer>
    {
        public static CachedPointer Create(Target target, TargetPointer address) => new(target.ReadPointer(address));
    }

    [GeneratedComClass]
    private partial class RuntimeDataTarget(byte[] image) : ICLRDataTarget
    {
        public ContractDescriptorTarget.ReadFromTargetDelegate? ReadMemory { get; init; }
        public ulong ImageBase { get; set; } = 0x10000;
        public int LookupResult { get; set; }
        public bool ShortRead { get; init; }
        public int ImageBaseLookups { get; private set; }
        public int RuntimeBaseLookups { get; protected set; }
        public int ReadCount { get; private set; }

        public int GetImageBase(string imagePath, ulong* baseAddress)
        {
            Assert.Equal("coreclr.dll", imagePath);
            ImageBaseLookups++;
            *baseAddress = ImageBase;
            return LookupResult;
        }

        public int ReadVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesRead)
        {
            ReadCount++;
            *bytesRead = 0;
            if (ReadMemory is not null)
            {
                int hr = ReadMemory(address, new Span<byte>(buffer, checked((int)bytesRequested)));
                if (hr >= 0)
                    *bytesRead = ShortRead && bytesRequested != 0 ? bytesRequested - 1 : bytesRequested;
                return hr;
            }

            if (address < ImageBase || address - ImageBase > (ulong)image.Length
                || bytesRequested > (ulong)image.Length - (address - ImageBase))
            {
                return HResults.E_FAIL;
            }

            image.AsSpan((int)(address - ImageBase), (int)bytesRequested).CopyTo(new Span<byte>(buffer, (int)bytesRequested));
            *bytesRead = ShortRead && bytesRequested != 0 ? bytesRequested - 1 : bytesRequested;
            return HResults.S_OK;
        }

        public int GetMachineType(uint* machineType) => throw new NotImplementedException();
        public int GetPointerSize(uint* pointerSize) => throw new NotImplementedException();
        public int WriteVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesWritten) => throw new NotImplementedException();
        public int GetTLSValue(uint threadID, uint index, ulong* value) => throw new NotImplementedException();
        public int SetTLSValue(uint threadID, uint index, ulong value) => throw new NotImplementedException();
        public int GetCurrentThreadID(uint* threadID) => throw new NotImplementedException();
        public int GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte* context)
            => throw new NotImplementedException();
        public int SetThreadContext(uint threadID, uint contextSize, byte* context) => throw new NotImplementedException();
        public int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer) => throw new NotImplementedException();
    }

    private sealed class RuntimeLocatorDataTarget(byte[] image) : RuntimeDataTarget(image), ICLRRuntimeLocator
    {
        public int RuntimeLookupResult { get; init; }

        public int GetRuntimeBase(ulong* baseAddress)
        {
            RuntimeBaseLookups++;
            *baseAddress = ImageBase;
            return RuntimeLookupResult;
        }
    }

    [GeneratedComClass]
    private partial class ContractLocatorDataTarget() : RuntimeDataTarget([]), ICLRContractLocator
    {
        public ulong ContractAddress { get; init; }
        public int ContractResult { get; init; }
        public int ContractLookups { get; private set; }

        public int GetContractDescriptor(ulong* contractAddress)
        {
            ContractLookups++;
            *contractAddress = ContractAddress;
            return ContractResult;
        }
    }

    [GeneratedComClass]
    private sealed partial class RuntimeAndContractLocatorDataTarget : ContractLocatorDataTarget, ICLRRuntimeLocator
    {
        public int RuntimeLookupResult { get; init; }

        public int GetRuntimeBase(ulong* baseAddress)
        {
            RuntimeBaseLookups++;
            *baseAddress = ImageBase;
            return RuntimeLookupResult;
        }
    }

    private sealed class RecordingCallback : IDisposable
    {
        private readonly nint* _instance;
        private readonly nint* _vtable;
        private readonly GCHandle _handle;
        private readonly bool _supportsUpdates;
        private readonly Lock? _apiLock;
        public bool WasLockHeld { get; private set; } = true;
        public int Result { get; set; }
        public ulong? CancelAtAddress { get; set; }
        public HashSet<ulong> Regions { get; } = [];
        public List<(ulong Address, uint Size)> Ranges { get; } = [];
        public Dictionary<ulong, byte[]> Updates { get; } = [];
        public nint Address => (nint)_instance;

        public RecordingCallback(bool supportsUpdates, Lock? apiLock = null)
        {
            _supportsUpdates = supportsUpdates;
            _apiLock = apiLock;
            _handle = GCHandle.Alloc(this);
            _vtable = (nint*)NativeMemory.AllocZeroed(5, (nuint)sizeof(nint));
            _vtable[0] = (nint)(delegate* unmanaged[MemberFunction]<nint, Guid*, nint*, int>)&QueryInterface;
            _vtable[2] = (nint)(delegate* unmanaged[MemberFunction]<nint, uint>)&Release;
            _vtable[3] = (nint)(delegate* unmanaged[MemberFunction]<nint, ulong, uint, int>)&Enumerate;
            _vtable[4] = (nint)(delegate* unmanaged[MemberFunction]<nint, ulong, uint, byte*, int>)&Update;
            _instance = (nint*)NativeMemory.Alloc(2, (nuint)sizeof(nint));
            _instance[0] = (nint)_vtable;
            _instance[1] = GCHandle.ToIntPtr(_handle);
        }

        private static RecordingCallback Get(nint self) => (RecordingCallback)GCHandle.FromIntPtr(((nint*)self)[1]).Target!;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int QueryInterface(nint self, Guid* iid, nint* result)
        {
            *result = Get(self)._supportsUpdates && *iid == new Guid("3721A26F-8B91-4D98-A388-DB17B356FADB") ? self : 0;
            return *result != 0 ? 0 : unchecked((int)0x80004002);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static uint Release(nint self) => 1;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int Enumerate(nint self, ulong address, uint size)
        {
            RecordingCallback callback = Get(self);
            callback.Regions.Add(address);
            callback.Ranges.Add((address, size));
            callback.WasLockHeld &= callback._apiLock?.IsHeldByCurrentThread ?? true;
            return callback.CancelAtAddress == address ? HResults.COR_E_OPERATIONCANCELED : callback.Result;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int Update(nint self, ulong address, uint size, byte* bytes)
        {
            Get(self).Updates[address] = new ReadOnlySpan<byte>(bytes, checked((int)size)).ToArray();
            return 0;
        }

        public void Dispose()
        {
            NativeMemory.Free(_instance);
            NativeMemory.Free(_vtable);
            _handle.Free();
        }
    }
}
