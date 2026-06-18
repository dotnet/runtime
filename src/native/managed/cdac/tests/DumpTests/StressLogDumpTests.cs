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

        System.Text.StringBuilder diag = new();
        diag.AppendLine($"Thread count: {threads.Count}, PointerSize: {Target.PointerSize}");

        bool foundMessages = false;
        foreach (ThreadStressLogData thread in threads)
        {
            int msgIndex = 0;
            diag.AppendLine($"  Thread 0x{thread.ThreadId:X}: WriteHasWrapped={thread.WriteHasWrapped}, CurrentPointer=0x{thread.CurrentPointer:X}");
            foreach (StressMsgData message in stressLog.GetStressMessages(thread).Take(10))
            {
                diag.AppendLine($"    Msg[{msgIndex}]: Timestamp={message.Timestamp}, FormatString=0x{(ulong)message.FormatString:X}, Facility={message.Facility}, ArgCount={message.Args.Count}");
                if (message.Timestamp != 0 && message.FormatString != TargetPointer.Null)
                {
                    foundMessages = true;
                    break;
                }
                msgIndex++;
            }
            if (foundMessages)
                break;
        }
        Assert.True(foundMessages, $"Expected at least one thread with stress log messages.\nDiagnostics:\n{diag}");
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
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void ISOSDacInterface17_GetStressLogThreadEnumerator(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface17 sosDac = (ISOSDacInterface17)new SOSDacImpl(Target, legacyObj: null);

        DacComNullableByRef<ISOSStressLogThreadEnum> ppEnum = new(isNullRef: false);
        int hr = sosDac.GetStressLogThreadEnumerator(ppEnum);
        Assert.Equal(System.HResults.S_OK, hr);

        ISOSStressLogThreadEnum? threadEnum = ppEnum.Interface;
        Assert.NotNull(threadEnum);

        uint count;
        hr = threadEnum.GetCount(&count);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.True(count > 0, "Expected at least one thread with stress log");

        SOSThreadStressLogData[] threads = new SOSThreadStressLogData[count];
        uint fetched;
        hr = threadEnum.Next(count, threads, &fetched);
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

        DacComNullableByRef<ISOSStressLogThreadEnum> ppThreadEnum = new(isNullRef: false);
        int hr = sosDac.GetStressLogThreadEnumerator(ppThreadEnum);
        Assert.Equal(System.HResults.S_OK, hr);

        ISOSStressLogThreadEnum? threadEnum = ppThreadEnum.Interface;
        Assert.NotNull(threadEnum);

        uint threadCount;
        hr = threadEnum.GetCount(&threadCount);
        Assert.Equal(System.HResults.S_OK, hr);

        SOSThreadStressLogData[] threads = new SOSThreadStressLogData[threadCount];
        uint fetched;
        hr = threadEnum.Next(threadCount, threads, &fetched);
        Assert.True(hr == System.HResults.S_OK || hr == System.HResults.S_FALSE);

        bool foundMessages = false;
        for (uint i = 0; i < fetched; i++)
        {
            DacComNullableByRef<ISOSStressLogMsgEnum> ppMsgEnum = new(isNullRef: false);
            hr = sosDac.GetStressLogMessageEnumerator(threads[i].ThreadLogAddress, ppMsgEnum);
            Assert.Equal(System.HResults.S_OK, hr);

            ISOSStressLogMsgEnum? msgEnum = ppMsgEnum.Interface;
            Assert.NotNull(msgEnum);

            SOSStressMsgData[] messages = new SOSStressMsgData[10];
            uint msgFetched;
            hr = msgEnum.Next(10, messages, &msgFetched);
            Assert.True(hr == System.HResults.S_OK || hr == System.HResults.S_FALSE);

            if (msgFetched > 0)
            {
                // Find a message with valid timestamp and format string
                for (uint m = 0; m < msgFetched; m++)
                {
                    if (messages[m].Timestamp != 0 && messages[m].FormatString != (ClrDataAddress)0)
                    {
                        foundMessages = true;

                        if (messages[m].ArgumentCount > 0)
                        {
                            ClrDataAddress[] args = new ClrDataAddress[messages[m].ArgumentCount];
                            uint argFetched;
                            hr = msgEnum.GetArguments(m, messages[m].ArgumentCount, args, &argFetched);
                            Assert.Equal(System.HResults.S_OK, hr);
                            Assert.Equal(messages[m].ArgumentCount, argFetched);
                        }
                        break;
                    }
                }
                if (foundMessages)
                    break;
            }
        }
        Assert.True(foundMessages, "Expected at least one thread with stress log messages via ISOSDacInterface17");
    }
}
