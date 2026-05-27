// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.DotNet.XUnitExtensions;
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
    public unsafe void IsValidObject_HandleObjects_AreValid(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IGC gc = Target.Contracts.GC;

        int validCount = 0;
        foreach (HandleData handleData in gc.GetHandles([HandleType.Strong]))
        {
            TargetPointer objectAddress = Target.ReadPointer(handleData.Handle);
            if (objectAddress == TargetPointer.Null)
                continue;

            Interop.BOOL result;
            int hr = dbi.IsValidObject(objectAddress.Value, &result);
            Assert.Equal(System.HResults.S_OK, hr);
            Assert.Equal(Interop.BOOL.TRUE, result);
            validCount++;
        }

        Assert.True(validCount > 0, "Expected at least one valid object from strong handles.");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void IsValidObject_InvalidAddress_ReturnsFalse(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        Interop.BOOL result;
        int hr = dbi.IsValidObject(0x12345678, &result);
        Assert.Equal(System.HResults.S_OK, hr);
        Assert.Equal(Interop.BOOL.FALSE, result);
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

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetObjectFields_NullId_ReturnsClassNotLoaded(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        uint fetched = 0;
        int hr = dbi.GetObjectFields(0, 0, null, &fetched);
        Assert.Equal(CorDbgHResults.CORDBG_E_CLASS_NOT_LOADED, hr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetObjectFields_TypeDescId_ReturnsInvalidArg(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        TargetPointer objectMT = Target.ReadPointer(Target.ReadGlobalPointer("ObjectMethodTable"));
        // Set the TypeDesc bit (bit 1) so the input looks like a TypeDesc-encoded handle.
        ulong typeDescId = objectMT.Value | 2;

        uint fetched = 0;
        int hr = dbi.GetObjectFields(typeDescId, 0, null, &fetched);
        Assert.Equal(System.HResults.E_INVALIDARG, hr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetObjectFields_Object_HasNoIntroducedFields(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();

        TargetPointer objectMT = Target.ReadPointer(Target.ReadGlobalPointer("ObjectMethodTable"));

        uint fetched = uint.MaxValue;
        int hr = dbi.GetObjectFields(objectMT.Value, 0, null, &fetched);
        Assert.Equal(System.HResults.S_FALSE, hr);
        Assert.Equal(0u, fetched);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetObjectFields_NullLayout_QueriesIntroducedFieldCount(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer stringMT = Target.ReadPointer(Target.ReadGlobalPointer("StringMethodTable"));
        TypeHandle stringHandle = rts.GetTypeHandle(stringMT);
        uint expectedCount = GetIntroducedInstanceFieldCount(rts, stringHandle);

        uint fetched = 0;
        int hr = dbi.GetObjectFields(stringMT.Value, 0, null, &fetched);
        Assert.Equal(System.HResults.S_FALSE, hr);
        Assert.Equal(expectedCount, fetched);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public unsafe void GetObjectFields_String_CrossValidatesContract(TestConfiguration config)
    {
        InitializeDumpTest(config);
        DacDbiImpl dbi = CreateDacDbi();
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer stringMT = Target.ReadPointer(Target.ReadGlobalPointer("StringMethodTable"));
        TypeHandle stringHandle = rts.GetTypeHandle(stringMT);
        uint cFields = GetIntroducedInstanceFieldCount(rts, stringHandle);
        Assert.True(cFields >= 1, $"Expected System.String to have at least one introduced instance field, got {cFields}");

        COR_FIELD[] fields = new COR_FIELD[cFields];
        uint fetched = 0;
        int hr;
        fixed (COR_FIELD* fieldsPtr = fields)
        {
            hr = dbi.GetObjectFields(stringMT.Value, cFields, fieldsPtr, &fetched);
        }
        Assert.Equal(System.HResults.S_OK, hr);
        // Native DAC sets pceltFetched to the input capacity, not the count actually written.
        Assert.Equal(cFields, fetched);

        TargetPointer[] fieldDescList = rts.GetFieldDescList(stringHandle).Take((int)cFields).ToArray();
        uint firstFieldOffset = rts.IsObjRef(stringHandle) ? Target.GetTypeInfo(DataType.Object).Size!.Value : 0;

        for (uint i = 0; i < cFields; i++)
        {
            TargetPointer fieldDescPtr = fieldDescList[i];
            uint expectedToken = rts.GetFieldDescMemberDef(fieldDescPtr);

            Assert.Equal(expectedToken, fields[i].token);
            Assert.True(fields[i].offset >= firstFieldOffset, $"field[{i}].offset {fields[i].offset} should be >= firstFieldOffset {firstFieldOffset}");
            Assert.NotEqual(0, fields[i].fieldType);
            Assert.NotEqual(0UL, fields[i].id.token1);
            Assert.Equal(0UL, fields[i].id.token2);
        }
    }

    private static uint GetIntroducedInstanceFieldCount(IRuntimeTypeSystem rts, TypeHandle handle)
    {
        uint count = rts.GetNumInstanceFields(handle);
        TargetPointer parentMT = rts.GetParentMethodTable(handle);
        if (parentMT != TargetPointer.Null)
        {
            TypeHandle parentHandle = rts.GetTypeHandle(parentMT);
            count -= rts.GetNumInstanceFields(parentHandle);
        }
        return count;
    }
}
