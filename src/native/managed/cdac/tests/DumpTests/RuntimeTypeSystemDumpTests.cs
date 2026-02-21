// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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
    protected override string DebuggeeName => "TypeHierarchy";
    protected override string DumpType => "full";

    [ConditionalFact]
    public void RuntimeTypeSystem_CanGetMethodTableFromModule()
    {
        SkipIfVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10");
        ILoader loader = Target.Contracts.Loader;
        Assert.NotNull(loader);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        Assert.NotNull(rts);

        TargetPointer rootAssembly = loader.GetRootAssembly();
        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);
        TargetPointer modulePtr = loader.GetModule(moduleHandle);
        Assert.NotEqual(TargetPointer.Null, modulePtr);
    }

    [ConditionalFact]
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

    [ConditionalFact]
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

    [ConditionalFact]
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

    [ConditionalFact]
    public void RuntimeTypeSystem_ObjectMethodTableHasParent()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);
        TypeHandle objectHandle = rts.GetTypeHandle(objectMT);

        // System.Object has no parent
        TargetPointer parent = rts.GetParentMethodTable(objectHandle);
        Assert.Equal(TargetPointer.Null, parent);
    }

    [ConditionalFact]
    public void RuntimeTypeSystem_StringHasObjectParent()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);

        TargetPointer stringMTGlobal = Target.ReadGlobalPointer("StringMethodTable");
        TargetPointer stringMT = Target.ReadPointer(stringMTGlobal);
        TypeHandle stringHandle = rts.GetTypeHandle(stringMT);

        // System.String's parent should be System.Object
        TargetPointer parent = rts.GetParentMethodTable(stringHandle);
        Assert.Equal(objectMT, parent);
    }

    [ConditionalFact]
    public void RuntimeTypeSystem_ObjectMethodTableHasReasonableBaseSize()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(objectMT);

        uint baseSize = rts.GetBaseSize(handle);
        Assert.True(baseSize > 0 && baseSize < 1024,
            $"Expected System.Object base size between 1 and 1024, got {baseSize}");
    }

    [ConditionalFact]
    public void RuntimeTypeSystem_StringHasNonZeroComponentSize()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer stringMTGlobal = Target.ReadGlobalPointer("StringMethodTable");
        TargetPointer stringMT = Target.ReadPointer(stringMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(stringMT);

        // String has a component size (char size = 2)
        uint componentSize = rts.GetComponentSize(handle);
        Assert.Equal(2u, componentSize);
    }

    [ConditionalFact]
    public void RuntimeTypeSystem_ObjectMethodTableContainsNoGCPointers()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(objectMT);

        // System.Object has no GC-tracked fields
        Assert.False(rts.ContainsGCPointers(handle));
    }

    [ConditionalFact]
    public void RuntimeTypeSystem_ObjectMethodTableHasValidToken()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(objectMT);

        uint token = rts.GetTypeDefToken(handle);
        // TypeDef tokens have the form 0x02xxxxxx
        Assert.Equal(0x02000000u, token & 0xFF000000u);
    }

    [ConditionalFact]
    public void RuntimeTypeSystem_ObjectMethodTableHasMethods()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(objectMT);

        ushort numMethods = rts.GetNumMethods(handle);
        // System.Object has ToString, Equals, GetHashCode, Finalize, etc.
        Assert.True(numMethods >= 4, $"Expected System.Object to have at least 4 methods, got {numMethods}");
    }

    [ConditionalFact]
    public void RuntimeTypeSystem_StringIsNotGenericTypeDefinition()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer stringMTGlobal = Target.ReadGlobalPointer("StringMethodTable");
        TargetPointer stringMT = Target.ReadPointer(stringMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(stringMT);

        Assert.False(rts.IsGenericTypeDefinition(handle));
    }

    [ConditionalFact]
    public void RuntimeTypeSystem_StringCorElementTypeIsClass()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer stringMTGlobal = Target.ReadGlobalPointer("StringMethodTable");
        TargetPointer stringMT = Target.ReadPointer(stringMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(stringMT);

        // GetSignatureCorElementType returns the MethodTable's stored CorElementType,
        // which is Class for System.String (not CorElementType.String)
        CorElementType corType = rts.GetSignatureCorElementType(handle);
        Assert.Equal(CorElementType.Class, corType);
    }

    [ConditionalFact]
    public void RuntimeTypeSystem_ObjectMethodTableHasIntroducedMethods()
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer objectMTGlobal = Target.ReadGlobalPointer("ObjectMethodTable");
        TargetPointer objectMT = Target.ReadPointer(objectMTGlobal);
        TypeHandle handle = rts.GetTypeHandle(objectMT);

        IEnumerable<TargetPointer> methodDescs = rts.GetIntroducedMethodDescs(handle);
        List<TargetPointer> methods = methodDescs.ToList();

        Assert.True(methods.Count >= 4, $"Expected System.Object to have at least 4 introduced methods, got {methods.Count}");

        // Each method desc should have a valid token
        foreach (TargetPointer mdPtr in methods)
        {
            MethodDescHandle mdHandle = rts.GetMethodDescHandle(mdPtr);
            uint token = rts.GetMethodToken(mdHandle);
            // MethodDef tokens have the form 0x06xxxxxx
            Assert.Equal(0x06000000u, token & 0xFF000000u);
        }
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
