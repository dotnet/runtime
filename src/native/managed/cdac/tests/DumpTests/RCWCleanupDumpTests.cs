// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the RCW cleanup list traversal via the ComWrappers contract.
/// Uses the BasicThreads debuggee dump, which does not involve COM interop,
/// so the cleanup list is expected to be empty (or the feature is disabled on non-Windows).
/// </summary>
public class RCWCleanupDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "RCW data types were added after net10.0")]
    public void GetRCWCleanupList_DoesNotThrow(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IComWrappers comWrappersContract = Target.Contracts.ComWrappers;

        // Calling GetRCWCleanupList should complete without throwing.
        // For non-COM dumps the list will be empty; on non-Windows FeatureCOMInterop=0
        // so the iterator yields nothing.
        List<RCWCleanupData> entries = [.. comWrappersContract.GetRCWCleanupList(TargetPointer.Null)];

        // Every returned entry must have a non-null RCW address.
        foreach (RCWCleanupData entry in entries)
        {
            Assert.NotEqual(TargetPointer.Null, entry.RCW);
        }
    }
}
