// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for DacDbiImpl object handle methods.
/// Uses the BasicThreads debuggee (heap dump).
/// </summary>
public class DacDbiObjectDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "BasicThreads";
    protected override string DumpType => "full";

    private DacDbiImpl CreateDacDbi() => new DacDbiImpl(Target, legacyObj: null);

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetVmObjectHandle_IsIdentity(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong testAddr = 0x12345678;
        ulong result;
        int hr = dbi.GetVmObjectHandle(testAddr, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(testAddr, result);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetHandleAddressFromVmHandle_IsIdentity(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        ulong testAddr = 0xABCDEF00;
        ulong result;
        int hr = dbi.GetHandleAddressFromVmHandle(testAddr, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(testAddr, result);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetTypeLayout_Object_CrossValidatesContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        TargetPointer objectMT = Target.ReadPointer(Target.ReadGlobalPointer("ObjectMethodTable"));
        TypeHandle objectHandle = Target.Contracts.RuntimeTypeSystem.GetTypeHandle(objectMT);

        COR_TYPE_LAYOUT layout;
        int hr = dbi.GetTypeLayout(objectMT.Value, &layout);
        Assert.Equal(System.HResults.S_OK, hr);

        Assert.Equal(Target.Contracts.RuntimeTypeSystem.GetParentMethodTable(objectHandle).Value, layout.parentID.token1);
        Assert.Equal(Target.Contracts.RuntimeTypeSystem.GetBaseSize(objectHandle), layout.objectSize);
        Assert.Equal(Target.Contracts.RuntimeTypeSystem.GetNumInstanceFields(objectHandle), layout.numFields);
        Assert.Equal(0u, layout.boxOffset);
        Assert.Equal((int)CorElementType.Class, layout.type);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetArrayLayout_ObjectArray_CrossValidatesContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer arrayMT = Target.ReadPointer(Target.ReadGlobalPointer("ObjectArrayMethodTable"));
        TypeHandle arrayHandle = rts.GetTypeHandle(arrayMT);
        TypeHandle componentHandle = rts.GetTypeParam(arrayHandle);
        Assert.True(rts.IsArray(arrayHandle, out uint rank));

        COR_ARRAY_LAYOUT layout;
        int hr = dbi.GetArrayLayout(arrayMT.Value, &layout);
        Assert.Equal(System.HResults.S_OK, hr);

        CorElementType expectedComponentType = rts.IsString(componentHandle)
            ? CorElementType.String
            : rts.GetSignatureCorElementType(componentHandle);

        Assert.Equal(componentHandle.Address.Value, layout.componentID.token1);
        Assert.Equal(expectedComponentType, layout.componentType);
        Assert.Equal((uint)Target.PointerSize, layout.elementSize);
        Assert.Equal((uint)Target.PointerSize, layout.countOffset);
        Assert.Equal((uint)sizeof(uint), layout.rankSize);
        Assert.Equal(rank, layout.numRanks);
        Assert.Equal((uint)Target.PointerSize, layout.rankOffset);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetArrayLayout_String_HasExpectedLayout(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer stringMT = Target.ReadPointer(Target.ReadGlobalPointer("StringMethodTable"));
        COR_ARRAY_LAYOUT layout;
        int hr = dbi.GetArrayLayout(stringMT.Value, &layout);
        Assert.Equal(System.HResults.S_OK, hr);

        Assert.Equal(rts.GetPrimitiveType(CorElementType.Char).Address.Value, layout.componentID.token1);
        Assert.Equal(CorElementType.Char, layout.componentType);
        Assert.Equal((uint)Target.PointerSize + sizeof(uint), layout.firstElementOffset);
        Assert.Equal((uint)sizeof(char), layout.elementSize);
        Assert.Equal((uint)Target.PointerSize, layout.countOffset);
        Assert.Equal((uint)sizeof(uint), layout.rankSize);
        Assert.Equal(1u, layout.numRanks);
        Assert.Equal((uint)Target.PointerSize, layout.rankOffset);
    }

}
