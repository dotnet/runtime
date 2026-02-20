// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the Object contract.
/// Uses the GCRoots debuggee dump, which pins objects and creates GC handles.
/// </summary>
public abstract class ObjectDumpTestsBase : DumpTestBase
{
    protected override string DebuggeeName => "GCRoots";

    [ConditionalFact]
    public void Object_ContractIsAvailable()
    {
        IObject objectContract = Target.Contracts.Object;
        Assert.NotNull(objectContract);
    }

    [ConditionalFact]
    public void Object_StringMethodTableHasCorrectComponentSize()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        TargetPointer stringMTGlobal = Target.ReadGlobalPointer("StringMethodTable");
        TargetPointer stringMT = Target.ReadPointer(stringMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(stringMT);

        uint componentSize = rts.GetComponentSize(handle);
        Assert.Equal(2u, componentSize);
    }
}

public class ObjectDumpTests_Local : ObjectDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

public class ObjectDumpTests_Net10 : ObjectDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
