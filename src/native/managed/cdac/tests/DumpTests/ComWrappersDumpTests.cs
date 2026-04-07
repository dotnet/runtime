// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the ComWrappers contract's
/// <see cref="IComWrappers.GetComWrappersRCWForObject"/> and
/// <see cref="IComWrappers.GetMOWs"/> APIs.
/// Uses the ComWrappers debuggee which creates exactly one MOW and
/// exactly one RCW for a single managed object before crashing.
/// </summary>
public class ComWrappersDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "ComWrappers";
    protected override string DumpType => "full";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "ComWrappers cDAC support not available in .NET 10")]
    public void GetComWrappersRCWForObject_FindsExactlyOneRCW(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        IComWrappers comWrappersContract = Target.Contracts.ComWrappers;

        int rcwCount = 0;
        foreach (HandleData handleData in gcContract.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            TargetPointer rcw = comWrappersContract.GetComWrappersRCWForObject(objectAddress);
            if (rcw != TargetPointer.Null)
                rcwCount++;
        }

        Assert.Equal(1, rcwCount);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "ComWrappers cDAC support not available in .NET 10")]
    public void GetMOWs_FindsExactlyOneMOW(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IGC gcContract = Target.Contracts.GC;
        IComWrappers comWrappersContract = Target.Contracts.ComWrappers;

        int objectsWithMows = 0;
        int totalMowCount = 0;

        foreach (HandleData handleData in gcContract.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            List<TargetPointer> mows = comWrappersContract.GetMOWs(objectAddress, out bool hasMOWTable);
            if (hasMOWTable && mows.Count > 0)
            {
                objectsWithMows++;
                totalMowCount += mows.Count;

                foreach (TargetPointer mow in mows)
                {
                    Assert.NotEqual(TargetPointer.Null, mow);

                    TargetPointer identity = comWrappersContract.GetIdentityForMOW(mow);
                    Assert.NotEqual(TargetPointer.Null, identity);
                }
            }
        }

        Assert.Equal(1, objectsWithMows);
        Assert.Equal(1, totalMowCount);
    }
}
