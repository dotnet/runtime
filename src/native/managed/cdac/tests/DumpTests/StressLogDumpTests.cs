// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the StressLog contract.
/// Uses the StressLog debuggee dump, which enables stress logging via environment
/// variables, performs allocations and GC, then crashes.
/// </summary>
public class StressLogDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StressLog";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void StressLogIsAvailable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStressLog stressLog = Target.Contracts.StressLog;
        Assert.True(stressLog.HasStressLog());
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void StressLogDataIsValid(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStressLog stressLog = Target.Contracts.StressLog;
        StressLogData data = stressLog.GetStressLogData();

        Assert.NotEqual(0UL, data.TickFrequency);
        Assert.NotEqual(0UL, data.StartTimestamp);
        Assert.NotEqual(0UL, data.StartTime);
        Assert.NotEqual(TargetPointer.Null, data.Logs);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void CanEnumerateThreadsAndMessages(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStressLog stressLog = Target.Contracts.StressLog;
        StressLogData data = stressLog.GetStressLogData();

        var threads = stressLog.GetThreadStressLogs(data.Logs).ToList();
        Assert.NotEmpty(threads);

        bool foundMessages = false;
        foreach (ThreadStressLogData thread in threads)
        {
            var messages = stressLog.GetStressMessages(thread).Take(10).ToList();
            if (messages.Count > 0)
            {
                foundMessages = true;
                Assert.NotEqual(0UL, messages[0].Timestamp);
                Assert.NotEqual(TargetPointer.Null, messages[0].FormatString);
                break;
            }
        }
        Assert.True(foundMessages, "Expected at least one thread with stress log messages");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void ISOSDacInterface17_GetStressLogData(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface17 sosDac = (ISOSDacInterface17)new SOSDacImpl(Target, legacyObj: null);

        SOSStressLogData data;
        int hr = sosDac.GetStressLogData(&data);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.NotEqual(0UL, data.TickFrequency);
        Assert.NotEqual(0UL, data.StartTimestamp);
        Assert.NotEqual(0UL, data.StartTime);
        Assert.NotEqual((ClrDataAddress)0, data.Logs);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void ISOSDacInterface17_GetStressLogThreadEnumerator(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface17 sosDac = (ISOSDacInterface17)new SOSDacImpl(Target, legacyObj: null);

        var ppEnum = new DacComNullableByRef<ISOSStressLogThreadEnum>(isNullRef: false);
        int hr = sosDac.GetStressLogThreadEnumerator(ppEnum);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.NotNull(ppEnum.Interface);

        uint count;
        hr = ppEnum.Interface.GetCount(&count);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.True(count > 0, "Expected at least one thread with stress log");

        SOSThreadStressLogData[] threads = new SOSThreadStressLogData[count];
        uint fetched;
        hr = ppEnum.Interface.Next(count, threads, &fetched);
        Assert.True(hr == System.HResults.S_OK || hr == System.HResults.S_FALSE);
        Assert.True(fetched > 0);
        Assert.NotEqual((ClrDataAddress)0, threads[0].ThreadLogAddress);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void ISOSDacInterface17_GetStressLogMessageEnumerator(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface17 sosDac = (ISOSDacInterface17)new SOSDacImpl(Target, legacyObj: null);

        var ppThreadEnum = new DacComNullableByRef<ISOSStressLogThreadEnum>(isNullRef: false);
        int hr = sosDac.GetStressLogThreadEnumerator(ppThreadEnum);
        Assert.Equal(System.HResults.S_OK, hr);

        uint threadCount;
        ppThreadEnum.Interface.GetCount(&threadCount);

        SOSThreadStressLogData[] threads = new SOSThreadStressLogData[threadCount];
        uint fetched;
        ppThreadEnum.Interface.Next(threadCount, threads, &fetched);

        bool foundMessages = false;
        for (uint i = 0; i < fetched; i++)
        {
            var ppMsgEnum = new DacComNullableByRef<ISOSStressLogMsgEnum>(isNullRef: false);
            hr = sosDac.GetStressLogMessageEnumerator(threads[i].ThreadLogAddress, ppMsgEnum);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.NotNull(ppMsgEnum.Interface);

            SOSStressMsgData[] messages = new SOSStressMsgData[10];
            uint msgFetched;
            hr = ppMsgEnum.Interface.Next(10, messages, &msgFetched);
            Assert.True(hr == System.HResults.S_OK || hr == System.HResults.S_FALSE);

            if (msgFetched > 0)
            {
                foundMessages = true;
                Assert.NotEqual(0UL, messages[0].Timestamp);
                Assert.NotEqual((ClrDataAddress)0, messages[0].FormatString);

                if (messages[0].ArgumentCount > 0)
                {
                    ClrDataAddress[] args = new ClrDataAddress[messages[0].ArgumentCount];
                    uint argFetched;
                    hr = ppMsgEnum.Interface.GetArguments(0, messages[0].ArgumentCount, args, &argFetched);
                    Assert.Equal(System.HResults.S_OK, hr);
                    Assert.Equal(messages[0].ArgumentCount, argFetched);
                }
                break;
            }
        }
        Assert.True(foundMessages, "Expected at least one thread with stress log messages via ISOSDacInterface17");
    }
}
