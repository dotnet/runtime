// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the BuiltInCOM contract.
/// Uses the BuiltInCOM debuggee dump, which creates a simple managed program
/// without COM objects, allowing validation of the TraverseRCWCleanupList API.
/// </summary>
public class BuiltInCOMDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BuiltInCOM";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "BuiltInCOM contract is only available on Windows")]
    public void BuiltInCOM_RCWCleanupList_EmptyForNonCOMProgram(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM contract = Target.Contracts.BuiltInCOM;
        Assert.NotNull(contract);

        // For a non-COM program, the RCW cleanup list should be empty.
        // Pass TargetPointer.Null to use the global g_pRCWCleanupList.
        IEnumerable<RCWCleanupInfo> items = contract.GetRCWCleanupList(TargetPointer.Null);
        Assert.Empty(items);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnOS(IncludeOnly = "windows", Reason = "BuiltInCOM contract is only available on Windows")]
    public void BuiltInCOM_RCWCleanupList_ItemsHaveValidFields(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IBuiltInCOM contract = Target.Contracts.BuiltInCOM;
        Assert.NotNull(contract);

        // Traverse the global cleanup list; for a non-COM program it will be empty,
        // but if any items exist they should have non-null RCW addresses.
        foreach (RCWCleanupInfo info in contract.GetRCWCleanupList(TargetPointer.Null))
        {
            Assert.NotEqual(TargetPointer.Null, info.RCW);
        }
    }
}
