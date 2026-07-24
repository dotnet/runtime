// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using HResults = System.HResults;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class IXCLRDataProcessTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void AppDomains(MockTarget.Architecture arch)
    {
        TargetPointer appDomainAddress = new(0x1000);
        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetAppDomain()).Returns(appDomainAddress);
        IXCLRDataProcess process = CreateProcess(arch, loader: loader.Object);

        ulong handle;
        int hr = process.StartEnumAppDomains(&handle);
        Assert.Equal(HResults.S_OK, hr);
        Assert.NotEqual(0ul, handle);

        try
        {
            DacComNullableByRef<IXCLRDataAppDomain> appDomainOut = new(isNullRef: false);
            hr = process.EnumAppDomain(&handle, appDomainOut);
            Assert.Equal(HResults.S_OK, hr);
            ClrDataAppDomain appDomain = Assert.IsType<ClrDataAppDomain>(appDomainOut.Interface);
            Assert.Equal(appDomainAddress, appDomain.Address);

            DacComNullableByRef<IXCLRDataAppDomain> endOut = new(isNullRef: false);
            hr = process.EnumAppDomain(&handle, endOut);
            Assert.Equal(HResults.S_FALSE, hr);
        }
        finally
        {
            hr = process.EndEnumAppDomains(handle);
            Assert.Equal(HResults.S_OK, hr);
        }

        DacComNullableByRef<IXCLRDataAppDomain> byIdOut = new(isNullRef: false);
        hr = process.GetAppDomainByUniqueID(ClrDataAppDomain.DefaultAppDomainId, byIdOut);
        Assert.Equal(HResults.S_OK, hr);
        Assert.IsType<ClrDataAppDomain>(byIdOut.Interface);

        DacComNullableByRef<IXCLRDataAppDomain> invalidIdOut = new(isNullRef: false);
        hr = process.GetAppDomainByUniqueID(ClrDataAppDomain.DefaultAppDomainId + 1, invalidIdOut);
        Assert.Equal(HResults.E_INVALIDARG, hr);

        ulong emptyHandle = 0;
        DacComNullableByRef<IXCLRDataAppDomain> emptyOut = new(isNullRef: false);
        Assert.Equal(HResults.S_FALSE, process.EnumAppDomain(&emptyHandle, emptyOut));
        Assert.Equal(HResults.S_OK, process.EndEnumAppDomains(emptyHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Modules(MockTarget.Architecture arch)
    {
        TargetPointer appDomainAddress = new(0x1000);
        ModuleHandle firstModule = new(new TargetPointer(0x2000));
        ModuleHandle secondModule = new(new TargetPointer(0x3000));
        IReadOnlyList<ModuleHandle> modules = [firstModule, secondModule];
        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetAppDomain()).Returns(appDomainAddress);
        loader.Setup(l => l.GetModuleHandles(
            appDomainAddress,
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution)).Returns(modules);
        TargetPointer firstBase = new(0x10000);
        uint firstSize = 0x100;
        uint firstFlags = 0;
        loader.Setup(l => l.TryGetLoadedImageContents(firstModule, out firstBase, out firstSize, out firstFlags)).Returns(false);
        TargetPointer secondBase = new(0x20000);
        uint secondSize = 0x200;
        uint secondFlags = 0;
        loader.Setup(l => l.TryGetLoadedImageContents(secondModule, out secondBase, out secondSize, out secondFlags)).Returns(true);
        IXCLRDataProcess process = CreateProcess(arch, loader: loader.Object);

        ulong handle;
        int hr = process.StartEnumModules(&handle);
        Assert.Equal(HResults.S_OK, hr);
        Assert.NotEqual(0ul, handle);

        try
        {
            foreach (ModuleHandle expected in modules)
            {
                DacComNullableByRef<IXCLRDataModule> moduleOut = new(isNullRef: false);
                hr = process.EnumModule(&handle, moduleOut);
                Assert.Equal(HResults.S_OK, hr);
                ClrDataModule module = Assert.IsType<ClrDataModule>(moduleOut.Interface);
                Assert.Equal(expected.Address, module.Address);
            }

            DacComNullableByRef<IXCLRDataModule> endOut = new(isNullRef: false);
            hr = process.EnumModule(&handle, endOut);
            Assert.Equal(HResults.S_FALSE, hr);
        }
        finally
        {
            hr = process.EndEnumModules(handle);
            Assert.Equal(HResults.S_OK, hr);
        }

        DacComNullableByRef<IXCLRDataModule> byAddressOut = new(isNullRef: false);
        hr = process.GetModuleByAddress(secondBase.Value + 0x80, byAddressOut);
        Assert.Equal(HResults.S_OK, hr);
        ClrDataModule byAddress = Assert.IsType<ClrDataModule>(byAddressOut.Interface);
        Assert.Equal(secondModule.Address, byAddress.Address);

        DacComNullableByRef<IXCLRDataModule> missingOut = new(isNullRef: false);
        hr = process.GetModuleByAddress(secondBase.Value + secondSize, missingOut);
        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal(HResults.S_OK, process.EndEnumModules(0));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTaskByUniqueID(MockTarget.Architecture arch)
    {
        TargetPointer threadAddress = new(0x5000);
        Mock<IThread> thread = new(MockBehavior.Strict);
        thread.Setup(t => t.IdToThread(42)).Returns(threadAddress);
        thread.Setup(t => t.IdToThread(43)).Returns(TargetPointer.Null);
        IXCLRDataProcess process = CreateProcess(arch, thread: thread.Object);
        DacComNullableByRef<IXCLRDataTask> taskOut = new(isNullRef: false);

        int hr = process.GetTaskByUniqueID(0x1_0000_002a, taskOut);

        Assert.Equal(HResults.S_OK, hr);
        Assert.IsType<ClrDataTask>(taskOut.Interface);

        DacComNullableByRef<IXCLRDataTask> missingOut = new(isNullRef: false);
        hr = process.GetTaskByUniqueID(43, missingOut);
        Assert.Equal(HResults.E_INVALIDARG, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetAddressType(MockTarget.Architecture arch)
    {
        const ulong CodeAddress = 0x7000;
        (CodeKind Kind, CLRDataAddressType Expected)[] cases =
        [
            (CodeKind.Unknown, CLRDataAddressType.CLRDATA_ADDRESS_UNRECOGNIZED),
            (CodeKind.Jitted, CLRDataAddressType.CLRDATA_ADDRESS_MANAGED_METHOD),
            (CodeKind.ReadyToRun, CLRDataAddressType.CLRDATA_ADDRESS_MANAGED_METHOD),
            (CodeKind.Interpreter, CLRDataAddressType.CLRDATA_ADDRESS_MANAGED_METHOD),
            (CodeKind.JumpStub, CLRDataAddressType.CLRDATA_ADDRESS_RUNTIME_UNMANAGED_STUB),
            (CodeKind.StubPrecode, CLRDataAddressType.CLRDATA_ADDRESS_RUNTIME_UNMANAGED_STUB),
            (CodeKind.VSD_DispatchStub, CLRDataAddressType.CLRDATA_ADDRESS_RUNTIME_UNMANAGED_STUB),
            (CodeKind.ThePreStub, CLRDataAddressType.CLRDATA_ADDRESS_RUNTIME_UNMANAGED_STUB),
        ];

        foreach ((CodeKind kind, CLRDataAddressType expected) in cases)
        {
            Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
            executionManager.Setup(e => e.GetCodeKind(new TargetCodePointer(CodeAddress))).Returns(kind);
            IXCLRDataProcess process = CreateProcess(
                arch,
                executionManager: executionManager.Object,
                readableAddress: CodeAddress);
            CLRDataAddressType type;

            int hr = process.GetAddressType(CodeAddress, &type);

            Assert.Equal(HResults.S_OK, hr);
            Assert.Equal(expected, type);
        }

        IXCLRDataProcess unreadableProcess = CreateProcess(arch);
        CLRDataAddressType unreadableType;
        int unreadableHr = unreadableProcess.GetAddressType(CodeAddress, &unreadableType);
        Assert.Equal(HResults.S_OK, unreadableHr);
        Assert.Equal(CLRDataAddressType.CLRDATA_ADDRESS_UNRECOGNIZED, unreadableType);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDataByAddress(MockTarget.Architecture arch)
    {
        IXCLRDataProcess process = CreateProcess(arch);
        DacComNullableByRef<IXCLRDataValue> value = new(isNullRef: false);

        int hr = process.GetDataByAddress(0, 0, null, null, 0, null, null, value, null);
        Assert.Equal(HResults.E_NOTIMPL, hr);

        hr = process.GetDataByAddress(0, 1, null, null, 0, null, null, value, null);
        Assert.Equal(HResults.E_INVALIDARG, hr);
    }

    private static IXCLRDataProcess CreateProcess(
        MockTarget.Architecture arch,
        ILoader? loader = null,
        IThread? thread = null,
        IExecutionManager? executionManager = null,
        ulong? readableAddress = null)
    {
        TestPlaceholderTarget.Builder builder = new(arch);
        if (loader is not null)
            builder.AddMockContract(loader);
        if (thread is not null)
            builder.AddMockContract(thread);
        if (executionManager is not null)
            builder.AddMockContract(executionManager);
        if (readableAddress is not null)
        {
            builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
            {
                Address = readableAddress.Value,
                Data = [0],
                Name = nameof(readableAddress),
            });
        }

        return new SOSDacImpl(builder.Build(), legacyObj: null);
    }
}
