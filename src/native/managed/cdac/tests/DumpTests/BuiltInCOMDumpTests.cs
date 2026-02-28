// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the BuiltInCOM contract.
/// Uses the BuiltInCOM debuggee, which creates STA-context Shell.Link COM
/// objects on a thread with eager cleanup disabled so that g_pRCWCleanupList
/// is populated at crash time, giving TraverseRCWCleanupList real data to walk.
/// </summary>
public class BuiltInCOMDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BuiltInCOM";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "BuiltInCOM contract is only available on Windows")]
    public void BuiltInCOM_RCWCleanupList_HasEntries(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM contract = Target.Contracts.BuiltInCOM;
        Assert.NotNull(contract);

        List<RCWCleanupInfo> items = contract.GetRCWCleanupList(TargetPointer.Null).ToList();

        // The STA thread created Shell.Link COM objects with eager cleanup disabled,
        // so the cleanup list must be non-empty.
        Assert.NotEmpty(items);

        foreach (RCWCleanupInfo info in items)
        {
            // Every cleanup entry must have a valid RCW address.
            Assert.NotEqual(TargetPointer.Null, info.RCW);

            // Shell.Link is an STA-affiliated, non-free-threaded COM object,
            // so the STA thread pointer must be set and IsFreeThreaded must be false.
            Assert.False(info.IsFreeThreaded);
            Assert.NotEqual(TargetPointer.Null, info.STAThread);
        }
    }
}
