// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataStackWalkTests
{
    private static readonly MockTarget.Architecture s_arch = new() { IsLittleEndian = true, Is64Bit = true };

    [Theory]
    [InlineData(StackWalkState.Frameless, 0x2u)]
    [InlineData(StackWalkState.Frame, 0x8u)]
    [InlineData(StackWalkState.SkippedFrame, 0x8u)]
    [InlineData(StackWalkState.Complete, 0x1u)]
    public void FrameGetFrameType_MapsSimpleTypeWithUnrecognizedDetail(StackWalkState state, uint expectedSimpleType)
    {
        TestFrame frame = new(state);
        IXCLRDataFrame clrFrame = new ClrDataFrame(CreateTarget(frame, isExceptionFrame: false), frame, legacyImpl: null);

        uint simpleType;
        uint detailedType;
        Assert.Equal(HResults.S_OK, clrFrame.GetFrameType(&simpleType, &detailedType));
        Assert.Equal(expectedSimpleType, simpleType);
        Assert.Equal(0u, detailedType);
    }

    [Fact]
    public void FrameGetFrameType_ExceptionFrameReportsExceptionFilter()
    {
        TestFrame frame = new(StackWalkState.Frame);
        IXCLRDataFrame clrFrame = new ClrDataFrame(CreateTarget(frame, isExceptionFrame: true), frame, legacyImpl: null);

        uint simpleType;
        uint detailedType;
        Assert.Equal(HResults.S_OK, clrFrame.GetFrameType(&simpleType, &detailedType));
        Assert.Equal(0x8u, simpleType);
        Assert.Equal(3u, detailedType); // CLRDATA_DETFRAME_EXCEPTION_FILTER
    }

    [Fact]
    public void FrameGetFrameType_NullOutputReturnsEPointer()
    {
        TestFrame frame = new(StackWalkState.Frameless);
        IXCLRDataFrame clrFrame = new ClrDataFrame(CreateTarget(frame, isExceptionFrame: false), frame, legacyImpl: null);

        uint simpleType;
        Assert.Equal(HResults.E_POINTER, clrFrame.GetFrameType(&simpleType, null));
    }

    [Fact]
    public void StackWalkGetFrameType_MapsVisibleFramesAndReturnsSFalseAfterEnd()
    {
        TestFrame frameless = new(StackWalkState.Frameless);
        TestFrame frame = new(StackWalkState.Frame);
        TestFrame skipped = new(StackWalkState.SkippedFrame);
        IStackDataFrameHandle[] frames = [frameless, frame, skipped];
        IXCLRDataStackWalk stackWalk = CreateStackWalk(frames, exceptionFrame: skipped);

        AssertFrameType(stackWalk, 0x2, 0);
        Assert.Equal(HResults.S_OK, stackWalk.Next());
        AssertFrameType(stackWalk, 0x8, 0);
        Assert.Equal(HResults.S_OK, stackWalk.Next());
        AssertFrameType(stackWalk, 0x8, 3); // skipped frame is an exception frame -> EXCEPTION_FILTER
        Assert.Equal(HResults.S_FALSE, stackWalk.Next());

        uint simpleType = uint.MaxValue;
        uint detailedType = uint.MaxValue;
        Assert.Equal(HResults.S_FALSE, stackWalk.GetFrameType(&simpleType, &detailedType));
        Assert.Equal(uint.MaxValue, simpleType);
        Assert.Equal(uint.MaxValue, detailedType);
    }

    [Fact]
    public void StackWalkGetFrameType_AllowsNullOutputs()
    {
        IXCLRDataStackWalk stackWalk = CreateStackWalk([new TestFrame(StackWalkState.Frameless)]);
        Assert.Equal(HResults.S_OK, stackWalk.GetFrameType(null, null));
    }

    private static TestPlaceholderTarget CreateTarget(IStackDataFrameHandle frame, bool isExceptionFrame)
    {
        var stackWalk = new Mock<IStackWalk>();
        stackWalk.Setup(s => s.IsExceptionFrame(frame)).Returns(isExceptionFrame);
        return new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(stackWalk)
            .Build();
    }

    private static IXCLRDataStackWalk CreateStackWalk(IStackDataFrameHandle[] frames, IStackDataFrameHandle? exceptionFrame = null)
    {
        TargetPointer threadAddress = new(0x1000);
        ThreadData threadData = default;
        var thread = new Mock<IThread>();
        thread.Setup(t => t.GetThreadData(threadAddress)).Returns(threadData);
        var stackWalk = new Mock<IStackWalk>();
        stackWalk.Setup(s => s.CreateStackWalk(threadData)).Returns(frames);
        stackWalk.Setup(s => s.IsExceptionFrame(It.IsAny<IStackDataFrameHandle>()))
            .Returns((IStackDataFrameHandle f) => exceptionFrame is not null && ReferenceEquals(f, exceptionFrame));

        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(thread)
            .AddMockContract(stackWalk)
            .Build();
        return new ClrDataStackWalk(threadAddress, flags: 0, target, legacyImpl: null);
    }

    private static void AssertFrameType(IXCLRDataStackWalk stackWalk, uint expectedSimpleType, uint expectedDetailedType)
    {
        uint simpleType;
        uint detailedType;
        Assert.Equal(HResults.S_OK, stackWalk.GetFrameType(&simpleType, &detailedType));
        Assert.Equal(expectedSimpleType, simpleType);
        Assert.Equal(expectedDetailedType, detailedType);
    }

    private sealed class TestFrame(StackWalkState state) : IStackDataFrameHandle
    {
        public StackWalkState State { get; } = state;
    }
}
