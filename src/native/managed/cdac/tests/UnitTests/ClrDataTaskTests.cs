// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataTaskTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetIdentityAndFlags(MockTarget.Architecture arch)
    {
        TargetPointer taskAddress = new(0x5000);
        IXCLRDataTask task = CreateTask(arch, CreateThreadData(taskAddress, id: 42, osId: 1234));

        ulong uniqueId;
        Assert.Equal(HResults.S_OK, task.GetUniqueID(&uniqueId));
        Assert.Equal(42u, uniqueId);

        uint osId;
        Assert.Equal(HResults.S_OK, task.GetOSThreadID(&osId));
        Assert.Equal(1234u, osId);

        uint flags;
        Assert.Equal(HResults.S_OK, task.GetFlags(&flags));
        Assert.Equal(0u, flags);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0xbaadf00du)]
    public void GetOSThreadID_InvalidSentinelReturnsSFalse(uint storedId)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = true };
        TargetPointer taskAddress = new(0x5000);
        IXCLRDataTask task = CreateTask(arch, CreateThreadData(taskAddress, id: 42, osId: storedId));

        uint osId = uint.MaxValue;
        Assert.Equal(HResults.S_FALSE, task.GetOSThreadID(&osId));
        Assert.Equal(0u, osId);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentAppDomain(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);

        ulong appDomainGlobalPtrAddr = 0x1000;
        ulong expectedAppDomain = 0x2000;

        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        byte[] appDomainPtrData = new byte[helpers.PointerSize];
        helpers.WritePointer(appDomainPtrData, expectedAppDomain);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = appDomainGlobalPtrAddr,
            Data = appDomainPtrData,
            Name = "AppDomainGlobalPointer"
        });

        var target = targetBuilder
            .AddGlobals(
                (Constants.Globals.AppDomain, appDomainGlobalPtrAddr))
            .AddContract<Contracts.ILoader>("c1")
            .Build();

        TargetPointer taskAddress = new TargetPointer(0x5000);
        IXCLRDataTask task = new ClrDataTask(taskAddress, target, legacyImpl: null);
        DacComNullableByRef<IXCLRDataAppDomain> appDomain = new(isNullRef: false);
        int hr = task.GetCurrentAppDomain(appDomain);

        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(appDomain.Interface);
        ClrDataAppDomain clrAppDomain = Assert.IsType<ClrDataAppDomain>(appDomain.Interface);
        Assert.Equal(new TargetPointer(expectedAppDomain), clrAppDomain.Address);
    }

    private static IXCLRDataTask CreateTask(MockTarget.Architecture arch, ThreadData threadData)
    {
        var thread = new Mock<IThread>();
        thread.Setup(t => t.GetThreadData(threadData.ThreadAddress)).Returns(threadData);

        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(thread)
            .Build();

        return new ClrDataTask(threadData.ThreadAddress, target, legacyImpl: null);
    }

    internal static ThreadData CreateThreadData(
        TargetPointer address,
        uint id,
        uint osId,
        TargetPointer nextThread = default,
        ThreadState state = default)
        => new(
            ThreadAddress: address,
            Id: id,
            OSId: new TargetNUInt(osId),
            State: state,
            PreemptiveGCDisabled: false,
            AllocContextPointer: TargetPointer.Null,
            AllocContextLimit: TargetPointer.Null,
            Frame: TargetPointer.Null,
            FirstNestedException: TargetPointer.Null,
            ExposedObjectHandle: TargetPointer.Null,
            LastThrownObjectHandle: TargetPointer.Null,
            CurrentCustomDebuggerNotificationHandle: TargetPointer.Null,
            LastThrownObjectIsUnhandled: false,
            HasUnhandledException: false,
            NextThread: nextThread,
            ThreadHandle: TargetPointer.Null,
            IsInteropDebuggingHijacked: false,
            DebuggerFilterContext: TargetPointer.Null,
            GCFrame: TargetPointer.Null,
            IsExceptionInProgress: false,
            OSExceptionRecord: TargetPointer.Null,
            OSExceptionContextRecord: TargetPointer.Null);
}
