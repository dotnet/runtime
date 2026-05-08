// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.Tests.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for IXCLRDataAppDomain methods.
/// Obtains an AppDomain via ClrDataFrame.GetAppDomain from the StackWalk dump.
/// </summary>
public unsafe class IXCLRDataAppDomainDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";

    // ========== GetName ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_QueryLengthOnly_ReturnsSizeWithoutBuffer(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataAppDomain appDomain = GetAppDomain();

        uint nameLen;
        int hr = appDomain.GetName(0, &nameLen, null);

        AssertHResult(HResults.S_OK, hr);
        Assert.True(nameLen > 1, "Expected nameLen > 1 (name + null terminator)");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_FullRetrieval_ReturnsNonEmptyName(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataAppDomain appDomain = GetAppDomain();

        uint nameLen;
        int hr = appDomain.GetName(0, &nameLen, null);
        AssertHResult(HResults.S_OK, hr);
        Assert.True(nameLen <= 1024, "AppDomain name unexpectedly long");

        char[] nameBuf = new char[nameLen];
        uint nameLen2;
        fixed (char* pName = nameBuf)
        {
            hr = appDomain.GetName(nameLen, &nameLen2, pName);
        }

        AssertHResult(HResults.S_OK, hr);
        string name = new string(nameBuf, 0, (int)nameLen2 - 1);
        Assert.False(string.IsNullOrEmpty(name), "Expected non-empty AppDomain name");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_NameIsNullTerminated(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataAppDomain appDomain = GetAppDomain();

        uint nameLen;
        int hr = appDomain.GetName(0, &nameLen, null);
        AssertHResult(HResults.S_OK, hr);

        char[] nameBuf = new char[nameLen];
        fixed (char* pName = nameBuf)
        {
            hr = appDomain.GetName(nameLen, null, pName);
            AssertHResult(HResults.S_OK, hr);
            Assert.Equal('\0', pName[nameLen - 1]);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_TruncatedBuffer_ReturnsSFalse(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataAppDomain appDomain = GetAppDomain();

        uint fullLen;
        int hr = appDomain.GetName(0, &fullLen, null);
        AssertHResult(HResults.S_OK, hr);
        Assert.True(fullLen > 2, "Need a name long enough to truncate");

        uint truncLen = fullLen - 1;
        char[] nameBuf = new char[truncLen];
        uint reportedLen;
        fixed (char* pName = nameBuf)
        {
            hr = appDomain.GetName(truncLen, &reportedLen, pName);
            AssertHResult(HResults.S_FALSE, hr);
        }

        // neededLen is always the full size, even on truncation.
        Assert.Equal(fullLen, reportedLen);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_TruncatedOutput_IsStillNullTerminated(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataAppDomain appDomain = GetAppDomain();

        uint fullLen;
        int hr = appDomain.GetName(0, &fullLen, null);
        AssertHResult(HResults.S_OK, hr);
        Assert.True(fullLen > 2, "Need a name long enough to truncate");

        uint truncLen = fullLen - 1;
        char[] nameBuf = new char[truncLen];
        fixed (char* pName = nameBuf)
        {
            appDomain.GetName(truncLen, null, pName);
            Assert.Equal('\0', pName[truncLen - 1]);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_NeededLenConsistentAcrossCalls(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataAppDomain appDomain = GetAppDomain();

        uint len1;
        int hr = appDomain.GetName(0, &len1, null);
        AssertHResult(HResults.S_OK, hr);

        uint len2;
        char[] nameBuf = new char[len1];
        fixed (char* pName = nameBuf)
        {
            hr = appDomain.GetName(len1, &len2, pName);
            AssertHResult(HResults.S_OK, hr);
        }

        Assert.Equal(len1, len2);
    }

    // ========== GetUniqueID ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetUniqueID_ReturnsOne(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataAppDomain appDomain = GetAppDomain();

        ulong id;
        int hr = appDomain.GetUniqueID(&id);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal(1ul, id);
    }

    // ========== GetFlags ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetFlags_ReturnsDefaultZero(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataAppDomain appDomain = GetAppDomain();

        uint flags;
        int hr = appDomain.GetFlags(&flags);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal(0u, flags);
    }

    // ========== Helpers ==========

    private IXCLRDataAppDomain GetAppDomain()
    {
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IStackDataFrameHandle? managedFrame = null;
        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md != TargetPointer.Null)
            {
                managedFrame = dataFrame;
                break;
            }
        }

        Assert.NotNull(managedFrame);

        ClrDataFrame frame = new ClrDataFrame(Target, managedFrame, legacyImpl: null);
        IXCLRDataFrame xclrFrame = frame;

        DacComNullableByRef<IXCLRDataAppDomain> appDomainOut = new(isNullRef: false);
        int hr = xclrFrame.GetAppDomain(appDomainOut);
        AssertHResult(HResults.S_OK, hr);

        return appDomainOut.Interface!;
    }
}
