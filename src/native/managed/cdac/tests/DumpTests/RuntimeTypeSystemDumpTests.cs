// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for the RuntimeTypeSystem contract.
/// Uses the TypeHierarchy debuggee dump, which loads types with inheritance,
/// generics, and arrays.
/// </summary>
public abstract class RuntimeTypeSystemDumpTestsBase : DumpTestBase
{
    protected RuntimeTypeSystemDumpTestsBase()
    {
        LoadDump();
    }

    protected override string DebuggeeName => "TypeHierarchy";

    [ConditionalFact]
    [SkipOnRuntimeVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    [SkipOnRuntimeVersion("local", "Assembly type does not include IsLoaded field in current contract descriptor")]
    public void RuntimeTypeSystem_CanGetMethodTableFromModule()
    {
        ILoader loader = Target.Contracts.Loader;
        Assert.NotNull(loader);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        Assert.NotNull(rts);

        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);
        TargetPointer modulePtr = loader.GetModule(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, modulePtr);
    }

    [Fact]
    public void RuntimeTypeSystem_ObjectMethodTableIsValid()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        Assert.NotNull(rts);

        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);
        Assert.NotEqual(TargetPointer.Null, objectMT);

        TypeHandle handle = rts.GetTypeHandle(objectMT);
        Assert.False(rts.IsFreeObjectMethodTable(handle));
    }

    [Fact]
    public void RuntimeTypeSystem_FreeObjectMethodTableIsValid()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        Assert.NotNull(rts);

        TargetPointer freeObjMTGlobal = Target.ReadGlobalPointer("FreeObjectMethodTable");
        TargetPointer freeObjMT = Target.ReadPointer(freeObjMTGlobal);
        Assert.NotEqual(TargetPointer.Null, freeObjMT);

        TypeHandle handle = rts.GetTypeHandle(freeObjMT);
        Assert.True(rts.IsFreeObjectMethodTable(handle));
    }

    [Fact]
    public void RuntimeTypeSystem_StringMethodTableIsString()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        Assert.NotNull(rts);

        TargetPointer stringMTGlobal = Target.ReadGlobalPointer("StringMethodTable");
        TargetPointer stringMT = Target.ReadPointer(stringMTGlobal);
        Assert.NotEqual(TargetPointer.Null, stringMT);

        TypeHandle handle = rts.GetTypeHandle(stringMT);
        Assert.True(rts.IsString(handle));
    }
}

public class RuntimeTypeSystemDumpTests_Local : RuntimeTypeSystemDumpTestsBase
{
    protected override string RuntimeVersion => "local";
}

public class RuntimeTypeSystemDumpTests_Net10 : RuntimeTypeSystemDumpTestsBase
{
    protected override string RuntimeVersion => "net10.0";
}
