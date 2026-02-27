// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the SyncBlock contract.
/// Uses the SyncBlock debuggee dump, which creates lock contention
/// and COM interop state before crashing.
/// </summary>
public class SyncBlockDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "SyncBlock";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void SyncBlockContract_CanFindHeldMonitor(TestConfiguration config)
    {
        InitializeDumpTest(config);

        TargetPointer syncBlock = TargetPointer.Null;
        uint ownerThreadId = 0;
        uint recursion = 0;
        bool found = false;

        ISyncBlock syncBlockContract = Target.Contracts.SyncBlock;
        uint syncBlockCount = syncBlockContract.GetSyncBlockCount();

        for (uint index = 1; index <= syncBlockCount; index++)
        {
            if (syncBlockContract.IsSyncBlockFree(index))
                continue;

            TargetPointer candidate = syncBlockContract.GetSyncBlock(index);
            if (candidate == TargetPointer.Null)
                continue;

            if (!syncBlockContract.TryGetLockInfo(candidate, out ownerThreadId, out recursion))
                continue;

            syncBlock = candidate;
            found = true;
            break;
        }

        Assert.True(found, "Expected to find a sync block with a held monitor.");
        Assert.True(ownerThreadId != 0, "Expected non-zero lock owner thread id.");
        Assert.True(recursion >= 1, "Expected recursion count >= 1.");
    }
}
