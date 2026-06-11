// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
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
}
