// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.Tests.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for IXCLRDataFrame methods.
/// </summary>
public unsafe class IXCLRDataFrameDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

    // ========== GetContext ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_ReturnsNonEmptyContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataFrame frame = CreateFrameForFirstManagedFrame();

        byte[] contextBuf = new byte[4096];
        uint contextSize;
        int hr = frame.GetContext(0, (uint)contextBuf.Length, &contextSize, contextBuf);

        AssertHResult(HResults.S_OK, hr);
        Assert.True(contextSize > 0, "Expected non-zero context size");
        Assert.True(contextBuf.Take((int)contextSize).Any(b => b != 0), "Expected non-zero context bytes");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_ContextSizeMatchesRawContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackDataFrameHandle dataFrame = GetFirstManagedFrame();
        IXCLRDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);

        byte[] contextBuf = new byte[4096];
        uint contextSize;
        int hr = frame.GetContext(0, (uint)contextBuf.Length, &contextSize, contextBuf);

        AssertHResult(HResults.S_OK, hr);

        byte[] rawContext = Target.Contracts.StackWalk.GetRawContext(dataFrame);
        Assert.Equal((uint)rawContext.Length, contextSize);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_ExactSizeBuffer_CopiesAllBytes(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackDataFrameHandle dataFrame = GetFirstManagedFrame();
        IXCLRDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);

        byte[] rawContext = Target.Contracts.StackWalk.GetRawContext(dataFrame);
        byte[] contextBuf = new byte[rawContext.Length];
        uint contextSize;
        int hr = frame.GetContext(0, (uint)contextBuf.Length, &contextSize, contextBuf);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal((uint)rawContext.Length, contextSize);
        Assert.True(contextBuf.SequenceEqual(rawContext), "Context bytes should match raw context exactly");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_BufferTooSmall_ReturnsInvalidArg_ButSetsContextSize(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackDataFrameHandle dataFrame = GetFirstManagedFrame();
        IXCLRDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);

        byte[] rawContext = Target.Contracts.StackWalk.GetRawContext(dataFrame);
        Assert.True(rawContext.Length > 0, "Raw context should not be empty for this test.");

        byte[] tinyBuf = new byte[rawContext.Length - 1];
        uint contextSize;
        int hr = frame.GetContext(0, (uint)tinyBuf.Length, &contextSize, tinyBuf);

        AssertHResult(HResults.E_INVALIDARG, hr);
        Assert.Equal((uint)rawContext.Length, contextSize);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_OversizedBuffer_SucceedsWithoutCorruptingExtra(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackDataFrameHandle dataFrame = GetFirstManagedFrame();
        IXCLRDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);

        byte[] rawContext = Target.Contracts.StackWalk.GetRawContext(dataFrame);
        int oversized = rawContext.Length + 128;
        byte[] contextBuf = new byte[oversized];
        // Fill extra region with a sentinel to verify it's untouched.
        byte sentinel = 0xAB;
        for (int i = rawContext.Length; i < oversized; i++)
            contextBuf[i] = sentinel;

        uint contextSize;
        int hr = frame.GetContext(0, (uint)oversized, &contextSize, contextBuf);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal((uint)rawContext.Length, contextSize);
        Assert.True(contextBuf.AsSpan(0, rawContext.Length).SequenceEqual(rawContext), "Copied bytes should match raw context");
        for (int i = rawContext.Length; i < oversized; i++)
            Assert.Equal(sentinel, contextBuf[i]);
    }

    // ========== GetAppDomain ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetAppDomain_ReturnsNonNullAppDomain(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataFrame frame = CreateFrameForFirstManagedFrame();

        DacComNullableByRef<IXCLRDataAppDomain> appDomainOut = new(isNullRef: false);
        int hr = frame.GetAppDomain(appDomainOut);

        AssertHResult(HResults.S_OK, hr);
        Assert.NotNull(appDomainOut.Interface);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetAppDomain_ReturnedObjectHasNonNullAddress(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataFrame frame = CreateFrameForFirstManagedFrame();

        DacComNullableByRef<IXCLRDataAppDomain> appDomainOut = new(isNullRef: false);
        int hr = frame.GetAppDomain(appDomainOut);

        AssertHResult(HResults.S_OK, hr);
        ClrDataAppDomain appDomain = Assert.IsType<ClrDataAppDomain>(appDomainOut.Interface);
        Assert.NotEqual(TargetPointer.Null, appDomain.Address);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetAppDomain_ConsistentAcrossCalls(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataFrame frame = CreateFrameForFirstManagedFrame();

        DacComNullableByRef<IXCLRDataAppDomain> out1 = new(isNullRef: false);
        DacComNullableByRef<IXCLRDataAppDomain> out2 = new(isNullRef: false);
        int hr1 = frame.GetAppDomain(out1);
        int hr2 = frame.GetAppDomain(out2);

        AssertHResult(HResults.S_OK, hr1);
        AssertHResult(HResults.S_OK, hr2);
        ClrDataAppDomain ad1 = Assert.IsType<ClrDataAppDomain>(out1.Interface);
        ClrDataAppDomain ad2 = Assert.IsType<ClrDataAppDomain>(out2.Interface);
        Assert.Equal(ad1.Address, ad2.Address);
    }

    // ========== GetNumArguments ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetNumArguments_ReturnsCountMatchingMetadata(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not "MethodA")
                continue;

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;
            uint numArgs;
            int hr = xclrFrame.GetNumArguments(&numArgs);

            // MethodA(int depth) is static with 1 parameter -> numArgs == 1.
            AssertHResult(HResults.S_OK, hr);
            Assert.Equal(1u, numArgs);

            return;
        }

        Assert.Fail("MethodA not found on the crashing thread's stack");
    }

    // ========== GetNumLocalVariables ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetNumLocalVariables_ReturnsCountForILMethod(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not "MethodB")
                continue;

            IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
            MethodDescHandle mdh = rts.GetMethodDescHandle(md);
            Assert.True(rts.IsIL(mdh), "MethodB should be an IL method");

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;
            uint numLocals;
            int hr = xclrFrame.GetNumLocalVariables(&numLocals);

            AssertHResult(HResults.S_OK, hr);
            Assert.True(numLocals >= 1, $"MethodB should have at least 1 local variable, got {numLocals}");

            return;
        }

        Assert.Fail("MethodB not found on the crashing thread's stack");
    }

    // ========== GetMethodInstance ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetMethodInstance_ReturnsNonNullForManagedFrame(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;

            DacComNullableByRef<IXCLRDataMethodInstance> methodOut = new(isNullRef: false);
            int hr = xclrFrame.GetMethodInstance(methodOut);

            AssertHResult(HResults.S_OK, hr);
            Assert.NotNull(methodOut.Interface);

            return; // One check is enough.
        }

        Assert.Fail("No managed frames with MethodDesc found");
    }

    // ========== GetArgumentByIndex ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetArgumentByIndex_ReturnsValueForMethodADepthArg(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not "MethodA")
                continue;

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;

            // MethodA(int depth) is static with 1 argument
            DacComNullableByRef<IXCLRDataValue> argOut = new(isNullRef: false);
            int hr = xclrFrame.GetArgumentByIndex(0, argOut, 0, null, null);

            AssertHResult(HResults.S_OK, hr);
            Assert.NotNull(argOut.Interface);

            return;
        }

        Assert.Fail("MethodA not found on the crashing thread's stack");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetArgumentByIndex_ReturnsParameterName(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not "MethodA")
                continue;

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;

            // Get the name of the first (and only) argument: "depth"
            char* nameBuf = stackalloc char[256];
            uint nameLen;
            DacComNullableByRef<IXCLRDataValue> argOut = new(isNullRef: false);
            int hr = xclrFrame.GetArgumentByIndex(0, argOut, 256, &nameLen, nameBuf);

            AssertHResult(HResults.S_OK, hr);
            string argName = new string(nameBuf);
            Assert.Equal("depth", argName);

            return;
        }

        Assert.Fail("MethodA not found on the crashing thread's stack");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetArgumentByIndex_InvalidIndex_ReturnsError(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not "MethodA")
                continue;

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;

            // MethodA has 1 argument, so index 1 should be out of range
            DacComNullableByRef<IXCLRDataValue> argOut = new(isNullRef: false);
            int hr = xclrFrame.GetArgumentByIndex(1, argOut, 0, null, null);

            Assert.True(hr < 0, $"Expected failure HRESULT for out-of-range index, got 0x{hr:X8}");

            return;
        }

        Assert.Fail("MethodA not found on the crashing thread's stack");
    }

    // ========== GetLocalVariableByIndex ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetLocalVariableByIndex_ReturnsValueForMethodBLocal(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not "MethodB")
                continue;

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;

            // MethodB has at least 1 local variable (localObj)
            DacComNullableByRef<IXCLRDataValue> localOut = new(isNullRef: false);
            int hr = xclrFrame.GetLocalVariableByIndex(0, localOut, 0, null, null);

            AssertHResult(HResults.S_OK, hr);
            Assert.NotNull(localOut.Interface);

            return;
        }

        Assert.Fail("MethodB not found on the crashing thread's stack");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetLocalVariableByIndex_InvalidIndex_ReturnsError(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not "MethodB")
                continue;

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;

            // Get actual local count, then use an out-of-range index
            uint numLocals;
            int countHr = xclrFrame.GetNumLocalVariables(&numLocals);
            AssertHResult(HResults.S_OK, countHr);

            DacComNullableByRef<IXCLRDataValue> localOut = new(isNullRef: false);
            int hr = xclrFrame.GetLocalVariableByIndex(numLocals, localOut, 0, null, null);

            Assert.True(hr < 0, $"Expected failure HRESULT for out-of-range index, got 0x{hr:X8}");

            return;
        }

        Assert.Fail("MethodB not found on the crashing thread's stack");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetLocalVariableByIndex_LocalNameIsEmpty(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name is not "MethodB")
                continue;

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;

            // Local variable names are not available - name should be empty
            char* nameBuf = stackalloc char[256];
            uint nameLen;
            DacComNullableByRef<IXCLRDataValue> localOut = new(isNullRef: false);
            int hr = xclrFrame.GetLocalVariableByIndex(0, localOut, 256, &nameLen, nameBuf);

            AssertHResult(HResults.S_OK, hr);
            Assert.Equal('\0', nameBuf[0]);

            return;
        }

        Assert.Fail("MethodB not found on the crashing thread's stack");
    }

    // ========== Helpers ==========

    private IStackDataFrameHandle GetFirstManagedFrame()
    {
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md != TargetPointer.Null)
                return dataFrame;
        }

        Assert.Fail("No managed frames with MethodDesc found on the crashing thread's stack");
        throw new InvalidOperationException("Unreachable");
    }

    private IXCLRDataFrame CreateFrameForFirstManagedFrame()
    {
        IStackDataFrameHandle dataFrame = GetFirstManagedFrame();
        return new ClrDataFrame(Target, dataFrame, legacyImpl: null);
    }
}
