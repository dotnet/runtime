// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        var allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x1_0000, 0x2_0000);

        MockMemorySpace.HeapFragment handleFragment = allocator.Allocate((ulong)helpers.PointerSize, "ThrownObjectHandle");
        helpers.WritePointer(handleFragment.Data, exceptionObjectAddr);

        TargetPointer thrownObjectHandle = new TargetPointer(handleFragment.Address);

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

        var target = targetBuilder
            .AddMockContract(mockException)
            .AddMockContract(mockObject)
            .Build();

        return (target, thrownObjectHandle);
    }

    private delegate void GetNestedExceptionInfoCallback(TargetPointer addr, out TargetPointer nextNestedException, out TargetPointer thrownObjectHandle);

    private static void SetupGetNestedExceptionInfo(
        Mock<IException> mock,
        TargetPointer address,
        TargetPointer nextNestedException,
        TargetPointer thrownObjectHandle)
    {
        mock.Setup(e => e.GetNestedExceptionInfo(address, out It.Ref<TargetPointer>.IsAny, out It.Ref<TargetPointer>.IsAny))
            .Callback(new GetNestedExceptionInfoCallback((TargetPointer addr, out TargetPointer next, out TargetPointer handle) =>
            {
                next = nextNestedException;
                handle = thrownObjectHandle;
            }));
    }

    private static (TestPlaceholderTarget Target, IXCLRDataTask Task) CreateTargetWithThread(
        MockTarget.Architecture arch,
        TargetPointer threadAddr,
        TargetPointer thrownObjectHandle,
        TargetPointer firstNestedException,
        TargetPointer lastThrownObjectHandle = default)
    {
        var mockThread = new Mock<IThread>();
        mockThread.Setup(t => t.GetCurrentExceptionHandle(threadAddr)).Returns(thrownObjectHandle);
        mockThread.Setup(t => t.GetThreadData(threadAddr)).Returns(new ThreadData(
            ThreadAddress: threadAddr,
            Id: 1,
            OSId: new TargetNUInt(1234),
            State: default,
            PreemptiveGCDisabled: false,
            AllocContextPointer: TargetPointer.Null,
            AllocContextLimit: TargetPointer.Null,
            Frame: TargetPointer.Null,
            FirstNestedException: firstNestedException,
            LastThrownObjectHandle: lastThrownObjectHandle,
            NextThread: TargetPointer.Null));

        var target = new TestPlaceholderTarget.Builder(arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(mockThread)
            .Build();

        IXCLRDataTask task = new ClrDataTask(threadAddr, target, null);
        return (target, task);
    }

    private static void AssertFlags(IXCLRDataExceptionState exceptionState, uint expectedFlags)
    {
        uint flags;
        int hr = exceptionState.GetFlags(&flags);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(expectedFlags, flags);
    }

    private static (int Hr, uint StrLen, char[] Buffer) CallGetString(IXCLRDataExceptionState exceptionState, uint bufLen)
    {
        char[] buffer = new char[bufLen];
        uint strLen;
        int hr;
        fixed (char* str = buffer)
        {
            hr = exceptionState.GetString(bufLen, &strLen, str);
        }
        return (hr, strLen, buffer);
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

        AssertFlags(exceptionState, expectedFlags);
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

        (int hr, uint strLen, char[] buffer) = CallGetString(exceptionState, bufLen: 256);
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

        (int hr, uint strLen, char[] buffer) = CallGetString(exceptionState, bufLen: 256);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(0u, strLen);
        Assert.Equal('\0', buffer[0]);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetString_NullMessageNonEmptyBuffer(MockTarget.Architecture arch)
    {
        (TestPlaceholderTarget target, TargetPointer thrownObjectHandle) = CreateTargetWithException(arch, TargetPointer.Null, null);
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target, new TargetPointer(0x1000), (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle, TargetPointer.Null, null);

        uint bufferSize = 256;
        char* str = null;
        uint strLen;
        int hr;
        hr = exceptionState.GetString(bufferSize, &strLen, str);
        Assert.Equal(HResults.E_INVALIDARG, hr);
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

        (int hr, uint strLen, _) = CallGetString(exceptionState, bufLen: 5);
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
        (_, IXCLRDataTask task) = CreateTargetWithThread(
            arch,
            threadAddr: new TargetPointer(0x1000),
            thrownObjectHandle: new TargetPointer(0x2000),
            firstNestedException: new TargetPointer(0x3000));

        DacComNullableByRef<IXCLRDataExceptionState> exception = new(isNullRef: false);
        int hr = task.GetCurrentExceptionState(exception);

        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(exception.Interface);

        AssertFlags(exception.Interface, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_NESTED);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionState_WithException(MockTarget.Architecture arch)
    {
        (_, IXCLRDataTask task) = CreateTargetWithThread(
            arch,
            threadAddr: new TargetPointer(0x1000),
            thrownObjectHandle: new TargetPointer(0x2000),
            firstNestedException: TargetPointer.Null);

        DacComNullableByRef<IXCLRDataExceptionState> exception = new(isNullRef: false);
        int hr = task.GetCurrentExceptionState(exception);

        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(exception.Interface);

        AssertFlags(exception.Interface, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCurrentExceptionState_NoException(MockTarget.Architecture arch)
    {
        (_, IXCLRDataTask task) = CreateTargetWithThread(
            arch,
            threadAddr: new TargetPointer(0x1000),
            thrownObjectHandle: TargetPointer.Null,
            firstNestedException: TargetPointer.Null);

        DacComNullableByRef<IXCLRDataExceptionState> exception = new(isNullRef: false);
        int hr = task.GetCurrentExceptionState(exception);

        Assert.Equal(HResults.COR_E_INVALIDCAST, hr);
        Assert.Null(exception.Interface);
    }

    [Fact]
    public void GetPrevious_NoPrevious()
    {
        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target: null!,
            threadAddress: new TargetPointer(0x1000),
            flags: (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle: new TargetPointer(0x2000),
            previousExInfoAddress: TargetPointer.Null,
            legacyImpl: null);

        DacComNullableByRef<IXCLRDataExceptionState> previous = new(isNullRef: false);
        int hr = exceptionState.GetPrevious(previous);
        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Null(previous.Interface);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetPrevious_HasPrevious_ReturnsState(MockTarget.Architecture arch)
    {
        TargetPointer previousExInfoAddr = new TargetPointer(0x3000);
        TargetPointer prevThrownObjectHandle = new TargetPointer(0x4000);
        TargetPointer nextNestedException = new TargetPointer(0x5000);

        var mockException = new Mock<IException>();
        SetupGetNestedExceptionInfo(mockException, previousExInfoAddr, nextNestedException, prevThrownObjectHandle);

        var target = new TestPlaceholderTarget.Builder(arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(mockException)
            .Build();

        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target,
            threadAddress: new TargetPointer(0x1000),
            flags: (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle: new TargetPointer(0x2000),
            previousExInfoAddress: previousExInfoAddr,
            legacyImpl: null);

        DacComNullableByRef<IXCLRDataExceptionState> previous = new(isNullRef: false);
        int hr = exceptionState.GetPrevious(previous);
        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(previous.Interface);

        AssertFlags(previous.Interface,
            (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT | (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_NESTED);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetPrevious_NestedExceptionsChain(MockTarget.Architecture arch)
    {
        TargetPointer firstNestedAddr = new TargetPointer(0x3000);
        TargetPointer firstHandle = new TargetPointer(0x4000);
        TargetPointer secondNestedAddr = new TargetPointer(0x5000);
        TargetPointer secondHandle = new TargetPointer(0x6000);

        var mockException = new Mock<IException>();
        SetupGetNestedExceptionInfo(mockException, firstNestedAddr, secondNestedAddr, firstHandle);
        SetupGetNestedExceptionInfo(mockException, secondNestedAddr, TargetPointer.Null, secondHandle);

        var target = new TestPlaceholderTarget.Builder(arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(mockException)
            .Build();

        IXCLRDataExceptionState exceptionState = new ClrDataExceptionState(
            target,
            threadAddress: new TargetPointer(0x1000),
            flags: (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT,
            thrownObjectHandle: new TargetPointer(0x2000),
            previousExInfoAddress: firstNestedAddr,
            legacyImpl: null);

        DacComNullableByRef<IXCLRDataExceptionState> first = new(isNullRef: false);
        int hr1 = exceptionState.GetPrevious(first);
        Assert.Equal(HResults.S_OK, hr1);
        Assert.NotNull(first.Interface);

        AssertFlags(first.Interface,
            (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT | (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_NESTED);

        DacComNullableByRef<IXCLRDataExceptionState> second = new(isNullRef: false);
        int hr2 = first.Interface.GetPrevious(second);
        Assert.Equal(HResults.S_OK, hr2);
        Assert.NotNull(second.Interface);

        AssertFlags(second.Interface, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_DEFAULT);

        DacComNullableByRef<IXCLRDataExceptionState> third = new(isNullRef: false);
        int hr3 = second.Interface.GetPrevious(third);
        Assert.Equal(HResults.S_FALSE, hr3);
        Assert.Null(third.Interface);
    }

    private static (IXCLRDataTask Task, string ExpectedMessage) CreateTargetWithLastException(
        MockTarget.Architecture arch,
        TargetPointer firstNestedException)
    {
        string expectedMessage = "Last thrown exception message";
        TargetPointer messageAddr = new TargetPointer(0x6000);
        TargetPointer exceptionObjectAddr = new TargetPointer(0x5000);
        TargetPointer threadAddr = new TargetPointer(0x1000);

        TargetTestHelpers helpers = new(arch);
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        var allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x1_0000, 0x2_0000);

        MockMemorySpace.HeapFragment handleFragment = allocator.Allocate((ulong)helpers.PointerSize, "LastThrownObjectHandle");
        helpers.WritePointer(handleFragment.Data, exceptionObjectAddr);
        TargetPointer lastThrownObjectHandle = new TargetPointer(handleFragment.Address);

        var mockThread = new Mock<IThread>();
        mockThread.Setup(t => t.GetThreadData(threadAddr)).Returns(new ThreadData(
            ThreadAddress: threadAddr,
            Id: 1,
            OSId: new TargetNUInt(1234),
            State: default,
            PreemptiveGCDisabled: false,
            AllocContextPointer: TargetPointer.Null,
            AllocContextLimit: TargetPointer.Null,
            Frame: TargetPointer.Null,
            FirstNestedException: firstNestedException,
            LastThrownObjectHandle: lastThrownObjectHandle,
            NextThread: TargetPointer.Null));

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
        mockObject.Setup(o => o.GetStringValue(messageAddr)).Returns(expectedMessage);
        var target = targetBuilder
            .AddMockContract(mockThread)
            .AddMockContract(mockException)
            .AddMockContract(mockObject)
            .Build();

        IXCLRDataTask task = new ClrDataTask(threadAddr, target, null);
        return (task, expectedMessage);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLastExceptionState_NoException(MockTarget.Architecture arch)
    {
        (_, IXCLRDataTask task) = CreateTargetWithThread(
            arch,
            threadAddr: new TargetPointer(0x1000),
            thrownObjectHandle: TargetPointer.Null,
            firstNestedException: TargetPointer.Null,
            lastThrownObjectHandle: TargetPointer.Null);
        DacComNullableByRef<IXCLRDataExceptionState> exception = new(isNullRef: false);
        int hr = task.GetLastExceptionState(exception);

        Assert.Equal(HResults.COR_E_INVALIDCAST, hr);
        Assert.Null(exception.Interface);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLastExceptionState_WithException(MockTarget.Architecture arch)
    {
        (IXCLRDataTask task, string expectedMessage) = CreateTargetWithLastException(arch, firstNestedException: TargetPointer.Null);
        DacComNullableByRef<IXCLRDataExceptionState> exception = new(isNullRef: false);
        int hr = task.GetLastExceptionState(exception);

        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(exception.Interface);
        AssertFlags(exception.Interface, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_PARTIAL);

        (int hrStr, _, char[] buffer) = CallGetString(exception.Interface, bufLen: 256);
        Assert.Equal(HResults.S_OK, hrStr);
        Assert.Equal(expectedMessage, new string(buffer, 0, expectedMessage.Length));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLastExceptionState_NoPreviousExceptionInfo(MockTarget.Architecture arch)
    {
        (IXCLRDataTask task, string expectedMessage) = CreateTargetWithLastException(arch, firstNestedException: new TargetPointer(0x3000));
        DacComNullableByRef<IXCLRDataExceptionState> exception = new(isNullRef: false);
        int hr = task.GetLastExceptionState(exception);

        Assert.Equal(HResults.S_OK, hr);
        Assert.NotNull(exception.Interface);

        // GetLastExceptionState should always pass Null for previousExInfoAddress,
        // so the NESTED flag should NOT be set even when FirstNestedException is non-null.
        AssertFlags(exception.Interface, (uint)CLRDataExceptionStateFlag.CLRDATA_EXCEPTION_PARTIAL);

        (int hrStr, _, char[] buffer) = CallGetString(exception.Interface, bufLen: 256);
        Assert.Equal(HResults.S_OK, hrStr);
        Assert.Equal(expectedMessage, new string(buffer, 0, expectedMessage.Length));

        DacComNullableByRef<IXCLRDataExceptionState> previous = new(isNullRef: false);
        int hrPrev = exception.Interface.GetPrevious(previous);
        Assert.Equal(HResults.S_FALSE, hrPrev);
        Assert.Null(previous.Interface);
    }
}
