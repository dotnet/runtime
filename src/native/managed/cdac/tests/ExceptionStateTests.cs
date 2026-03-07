// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Moq;
using Xunit;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ExceptionStateTests
{
    private static (TestPlaceholderTarget Target, TargetPointer ThrownObjectHandle) CreateTargetWithException(
        MockTarget.Architecture arch,
        TargetPointer messageAddr,
        string? messageString)
    {
        TargetPointer exceptionObjectAddr = new TargetPointer(0x5000);
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        var allocator = builder.CreateAllocator(0x1_0000, 0x2_0000);

        MockMemorySpace.HeapFragment handleFragment = allocator.Allocate((ulong)helpers.PointerSize, "ThrownObjectHandle");
        helpers.WritePointer(handleFragment.Data, exceptionObjectAddr);
        builder.AddHeapFragment(handleFragment);

        TargetPointer thrownObjectHandle = new TargetPointer(handleFragment.Address);

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget);

        var mockException = new Mock<IException>();
        mockException.Setup(e => e.GetExceptionData(exceptionObjectAddr)).Returns(new ExceptionData(
            Message: messageAddr,
            InnerException: TargetPointer.Null,
            StackTrace: TargetPointer.Null,
            WatsonBuckets: TargetPointer.Null,
            StackTraceString: TargetPointer.Null,
            RemoteStackTraceString: TargetPointer.Null,
            HResult: 0,
            XCode: 0));

        var mockObject = new Mock<IObject>();
        if (messageAddr != TargetPointer.Null && messageString is not null)
            mockObject.Setup(o => o.GetStringValue(messageAddr)).Returns(messageString);

        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.Exception == mockException.Object
                && c.Object == mockObject.Object));

        return (target, thrownObjectHandle);
    }

    [Theory]
    [InlineData((uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT, false, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT)]
    [InlineData((uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT, true, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_NESTED)]
    [InlineData((uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_PARTIAL, true, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_PARTIAL | (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_NESTED)]
    public void GetFlags(uint inputFlags, bool hasNestedException, uint expectedFlags)
    {
        TargetPointer previousExInfo = hasNestedException ? new TargetPointer(0x3000) : TargetPointer.Null;
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target: null!,
            threadAddress: new TargetPointer(0x1000),
            flags: inputFlags,
            thrownObjectHandle: new TargetPointer(0x2000),
            previousExInfoAddress: previousExInfo,
            legacyImpl: null);

        uint flags;
        int hr = exceptionState.GetFlags(&flags);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(expectedFlags, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetString_WithMessage(MockTarget.Architecture arch)
    {
        string expectedMessage = "Test exception message";
        TargetPointer messageAddr = new TargetPointer(0x6000);
        (TestPlaceholderTarget target, TargetPointer thrownObjectHandle) = CreateTargetWithException(arch, messageAddr, expectedMessage);
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target, new TargetPointer(0x1000), (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle, TargetPointer.Null, null);

        uint bufferSize = 256;
        char[] buffer = new char[bufferSize];
        uint strLen;
        int hr;
        fixed (char* ptr = buffer)
        {
            hr = exceptionState.GetString(bufferSize, &strLen, ptr);
        }
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)expectedMessage.Length + 1, strLen);
        Assert.Equal(expectedMessage, new string(buffer, 0, expectedMessage.Length));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetString_NullMessage(MockTarget.Architecture arch)
    {
        (TestPlaceholderTarget target, TargetPointer thrownObjectHandle) = CreateTargetWithException(arch, TargetPointer.Null, null);
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target, new TargetPointer(0x1000), (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle, TargetPointer.Null, null);

        uint bufferSize = 256;
        char[] buffer = new char[bufferSize];
        uint strLen;
        int hr;
        fixed (char* ptr = buffer)
        {
            hr = exceptionState.GetString(bufferSize, &strLen, ptr);
        }
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0u, strLen);
        Assert.Equal('\0', buffer[0]);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetString_BufferOverflow(MockTarget.Architecture arch)
    {
        string expectedMessage = "A long exception message";
        TargetPointer messageAddr = new TargetPointer(0x6000);
        (TestPlaceholderTarget target, TargetPointer thrownObjectHandle) = CreateTargetWithException(arch, messageAddr, expectedMessage);
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target, new TargetPointer(0x1000), (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle, TargetPointer.Null, null);

        uint bufferSize = 5;
        char[] buffer = new char[bufferSize];
        uint strLen;
        int hr;
        fixed (char* ptr = buffer)
        {
            hr = exceptionState.GetString(bufferSize, &strLen, ptr);
        }
        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal((uint)expectedMessage.Length + 1, strLen);
    }

    [Theory]
    [InlineData((uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION, 1u, 4u)]
    [InlineData((uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION, 0u, 0u)]
    [InlineData((uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION, 0u, 8u)]
    [InlineData(0x12345678u, 0u, 4u)]
    public void Request_NullInBuffer_InvalidArgs(uint reqCode, uint inBufferSize, uint outBufferSize)
    {
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target: null!,
            threadAddress: default,
            flags: (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle: default,
            previousExInfoAddress: default,
            legacyImpl: null);

        byte[] outBuffer = new byte[8];
        fixed (byte* outPtr = outBuffer)
        {
            int hr = exceptionState.Request(reqCode, inBufferSize, null, outBufferSize, outPtr);
            Assert.Equal(HResults.E_INVALIDARG, hr);
        }
    }

    [Fact]
    public void Request_NonNullInBuffer_InvalidArgs()
    {
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target: null!,
            threadAddress: default,
            flags: (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle: default,
            previousExInfoAddress: default,
            legacyImpl: null);

        byte inByte = 0;
        uint outBufferSize = sizeof(uint);
        byte[] outBuffer = new byte[outBufferSize];
        fixed (byte* outPtr = outBuffer)
        {
            int hr = exceptionState.Request((uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION, 0, &inByte, outBufferSize, outPtr);
            Assert.Equal(HResults.E_INVALIDARG, hr);
        }
    }

    [Fact]
    public void Request_Success()
    {
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target: null!,
            threadAddress: default,
            flags: (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle: default,
            previousExInfoAddress: default,
            legacyImpl: null);

        uint outBufferSize = sizeof(uint);
        byte[] outBuffer = new byte[outBufferSize];
        fixed (byte* outPtr = outBuffer)
        {
            int hr = exceptionState.Request((uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION, 0, null, outBufferSize, outPtr);
            Assert.Equal(HResults.S_OK, hr);
            Assert.Equal(2u, *(uint*)outPtr);
        }
    }

    [Fact]
    public void GetTask()
    {
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target: null!,
            threadAddress: new TargetPointer(0x1000),
            flags: (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle: new TargetPointer(0x2000),
            previousExInfoAddress: TargetPointer.Null,
            legacyImpl: null);

        DacComNullableByRef<IXCLRDataTask> task = new(isNullRef: false);
        int hr = exceptionState.GetTask(task);
        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(task.Interface);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionState_NestedException(MockTarget.Architecture arch)
    {
        TargetPointer threadAddr = new TargetPointer(0x1000);
        TargetPointer thrownObjectHandle = new TargetPointer(0x2000);
        TargetPointer firstNestedException = new TargetPointer(0x3000);

        var mockThread = new Mock<IThread>();
        mockThread.Setup(t => t.GetCurrentExceptionHandle(threadAddr)).Returns(thrownObjectHandle);
        mockThread.Setup(t => t.GetThreadData(threadAddr)).Returns(new ThreadData(
            Id: 1,
            OSId: new TargetNUInt(1234),
            State: default,
            PreemptiveGCDisabled: false,
            AllocContextPointer: TargetPointer.Null,
            AllocContextLimit: TargetPointer.Null,
            Frame: TargetPointer.Null,
            FirstNestedException: firstNestedException,
            TEB: TargetPointer.Null,
            LastThrownObjectHandle: TargetPointer.Null,
            NextThread: TargetPointer.Null));

        var target = new TestPlaceholderTarget(arch, (ulong _, Span<byte> _) => -1);
        target.SetContracts(Mock.Of<ContractRegistry>(c => c.Thread == mockThread.Object));

        IXCLRDataTask task = new ClrDataTask(threadAddr, target, null);
        int hr = task.GetCurrentExceptionState(out IXCLRDataExceptionState? exception);

        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(exception);

        uint flags;
        int flagsHr = exception.GetFlags(&flags);
        Assert.Equal(HResults.S_OK, flagsHr);
        Assert.Equal((uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_NESTED, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionState_WithException(MockTarget.Architecture arch)
    {
        TargetPointer threadAddr = new TargetPointer(0x1000);
        TargetPointer thrownObjectHandle = new TargetPointer(0x2000);

        var mockThread = new Mock<IThread>();
        mockThread.Setup(t => t.GetCurrentExceptionHandle(threadAddr)).Returns(thrownObjectHandle);
        mockThread.Setup(t => t.GetThreadData(threadAddr)).Returns(new ThreadData(
            Id: 1,
            OSId: new TargetNUInt(1234),
            State: default,
            PreemptiveGCDisabled: false,
            AllocContextPointer: TargetPointer.Null,
            AllocContextLimit: TargetPointer.Null,
            Frame: TargetPointer.Null,
            FirstNestedException: TargetPointer.Null,
            TEB: TargetPointer.Null,
            LastThrownObjectHandle: TargetPointer.Null,
            NextThread: TargetPointer.Null));

        var target = new TestPlaceholderTarget(arch, (ulong _, Span<byte> _) => -1);
        target.SetContracts(Mock.Of<ContractRegistry>(c => c.Thread == mockThread.Object));

        IXCLRDataTask task = new ClrDataTask(threadAddr, target, null);
        int hr = task.GetCurrentExceptionState(out IXCLRDataExceptionState? exception);

        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(exception);

        uint flags;
        int flagsHr = exception.GetFlags(&flags);
        Assert.Equal(HResults.S_OK, flagsHr);
        Assert.Equal((uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionState_NoException(MockTarget.Architecture arch)
    {
        TargetPointer threadAddr = new TargetPointer(0x1000);

        var mockThread = new Mock<IThread>();
        mockThread.Setup(t => t.GetCurrentExceptionHandle(threadAddr)).Returns(TargetPointer.Null);

        var target = new TestPlaceholderTarget(arch, (ulong _, Span<byte> _) => -1);
        target.SetContracts(Mock.Of<ContractRegistry>(c => c.Thread == mockThread.Object));

        IXCLRDataTask task = new ClrDataTask(threadAddr, target, null);
        int hr = task.GetCurrentExceptionState(out IXCLRDataExceptionState? exception);

        Assert.Equal(HResults.COR_E_INVALIDCAST, hr);
        Assert.Null(exception);
    }
}
