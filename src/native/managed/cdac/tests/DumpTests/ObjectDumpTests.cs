// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the Object contract.
/// Uses the GCRoots debuggee dump, which pins objects and creates GC handles.
/// </summary>
public class ObjectDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "GCRoots";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void Object_ContractIsAvailable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IObject objectContract = Target.Contracts.Object;
        Assert.NotNull(objectContract);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void Object_StringMethodTableHasCorrectComponentSize(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        TargetPointer stringMTGlobal = Target.ReadGlobalPointer("StringMethodTable");
        TargetPointer stringMT = Target.ReadPointer(stringMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(stringMT);

        uint componentSize = rts.GetComponentSize(handle);
        Assert.Equal(2u, componentSize);
    }
}
