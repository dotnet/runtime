// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.Tests.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

using MockRTS = MockDescriptors.RuntimeTypeSystem;

public class MethodTableTests
{
    internal static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockRTS rtsBuilder)
        => new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.MethodTable] = TargetTestHelpers.CreateTypeInfo(rtsBuilder.MethodTableLayout),
            [DataType.EEClass] = TargetTestHelpers.CreateTypeInfo(rtsBuilder.EEClassLayout),
            [DataType.MethodTableAuxiliaryData] = TargetTestHelpers.CreateTypeInfo(rtsBuilder.MethodTableAuxiliaryDataLayout),
            [DataType.TypeDesc] = TargetTestHelpers.CreateTypeInfo(rtsBuilder.TypeDescLayout),
            [DataType.FnPtrTypeDesc] = TargetTestHelpers.CreateTypeInfo(rtsBuilder.FnPtrTypeDescLayout),
            [DataType.ParamTypeDesc] = TargetTestHelpers.CreateTypeInfo(rtsBuilder.ParamTypeDescLayout),
            [DataType.TypeVarTypeDesc] = TargetTestHelpers.CreateTypeInfo(rtsBuilder.TypeVarTypeDescLayout),
            [DataType.GCCoverageInfo] = TargetTestHelpers.CreateTypeInfo(rtsBuilder.GCCoverageInfoLayout),
            [DataType.ContinuationObject] = new Target.TypeInfo { Size = rtsBuilder.ContinuationObjectSize },
        };

    internal static (string Name, ulong Value)[] CreateContractGlobals(MockRTS rtsBuilder)
        =>
        [
            (nameof(Constants.Globals.FreeObjectMethodTable), rtsBuilder.FreeObjectMethodTableGlobalAddress),
            (nameof(Constants.Globals.ContinuationMethodTable), rtsBuilder.ContinuationMethodTableGlobalAddress),
            (nameof(Constants.Globals.MethodDescAlignment), rtsBuilder.MethodDescAlignment),
            (nameof(Constants.Globals.ArrayBaseSize), rtsBuilder.ArrayBaseSize),
        ];

    public static IEnumerable<object[]> StdArchBool()
    {
        foreach (object[] arch in new MockTarget.StdArch())
        {
            yield return [.. arch, true];
            yield return [.. arch, false];
        }
    }

    internal static TestPlaceholderTarget CreateTarget(MockTarget.Architecture arch, Action<MockRTS> configure)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockRTS rtsBuilder = new(targetBuilder.MemoryBuilder);

        configure?.Invoke(rtsBuilder);

        var target = targetBuilder
            .AddTypes(CreateContractTypes(rtsBuilder))
            .AddGlobals(CreateContractGlobals(rtsBuilder))
            .AddContract<IRuntimeTypeSystem>(version: "c1")
            .Build();
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void HasRuntimeTypeSystemContract(MockTarget.Architecture arch)
    {
        TargetPointer freeObjectMethodTableAddress = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            builder => freeObjectMethodTableAddress = builder.FreeObjectMethodTableAddress);

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle handle = contract.GetTypeHandle(freeObjectMethodTableAddress);
        Assert.NotEqual(TargetPointer.Null, handle.Address);
        Assert.True(contract.IsFreeObjectMethodTable(handle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateSystemObjectMethodTable(MockTarget.Architecture arch)
    {
        TargetPointer systemObjectMethodTablePtr = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder => systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address);

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle systemObjectTypeHandle = contract.GetTypeHandle(systemObjectMethodTablePtr);
        Assert.Equal(systemObjectMethodTablePtr.Value, systemObjectTypeHandle.Address.Value);
        Assert.False(contract.IsFreeObjectMethodTable(systemObjectTypeHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateSystemStringMethodTable(MockTarget.Architecture arch)
    {
        TargetPointer systemStringMethodTablePtr = default;
        TargetPointer systemStringEEClassPtr = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetPointer systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;

                System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Sealed;
                const int numMethods = 37; // Arbitrary. Not trying to exactly match  the real System.String
                const int numInterfaces = 8; // Arbitrary
                const int numVirtuals = 3; // at least as many as System.Object
                uint mtflags = (uint)MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize | 2;
                MockEEClass systemStringEEClass = rtsBuilder.AddEEClass("System.String");
                systemStringEEClass.CorTypeAttr = (uint)typeAttributes;
                systemStringEEClass.NumMethods = numMethods;
                systemStringEEClassPtr = systemStringEEClass.Address;

                MockMethodTable systemStringMethodTable = rtsBuilder.AddMethodTable("System.String");
                systemStringMethodTable.MTFlags = mtflags;
                systemStringMethodTable.BaseSize = rtsBuilder.Builder.TargetTestHelpers.StringBaseSize;
                systemStringMethodTable.ParentMethodTable = systemObjectMethodTablePtr;
                systemStringMethodTable.NumInterfaces = numInterfaces;
                systemStringMethodTable.NumVirtuals = numVirtuals;
                systemStringMethodTablePtr = systemStringMethodTable.Address;
                systemStringEEClass.MethodTable = systemStringMethodTable.Address;
                systemStringMethodTable.EEClassOrCanonMT = systemStringEEClass.Address;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle systemStringTypeHandle = contract.GetTypeHandle(systemStringMethodTablePtr);
        Assert.Equal(systemStringMethodTablePtr.Value, systemStringTypeHandle.Address.Value);
        Assert.False(contract.IsFreeObjectMethodTable(systemStringTypeHandle));
        Assert.True(contract.IsString(systemStringTypeHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MethodTableEEClassInvalidThrows(MockTarget.Architecture arch)
    {
        TargetPointer badMethodTablePtr = default;
        TargetPointer badMethodTableEEClassPtr = new TargetPointer(0x00000010_afafafafa0); // bad address
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetPointer systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;
                MockMethodTable badMethodTable = rtsBuilder.AddMethodTable("Bad MethodTable");
                badMethodTable.BaseSize = rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                badMethodTable.ParentMethodTable = systemObjectMethodTablePtr;
                badMethodTable.NumVirtuals = 3;
                badMethodTablePtr = badMethodTable.Address;
                badMethodTable.EEClassOrCanonMT = badMethodTableEEClassPtr;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Assert.Throws<ArgumentException>(() => contract.GetTypeHandle(badMethodTablePtr));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateGenericInstMethodTable(MockTarget.Architecture arch)
    {
        TargetPointer genericDefinitionMethodTablePtr = default;
        TargetPointer genericInstanceMethodTablePtr = default;

        const int numMethods = 17;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
                TargetPointer systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;

                System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class;
                const int numVirtuals = 3;
                const uint gtd_mtflags = 0x00000030; // TODO: GenericsMask_TypicalInst
                MockEEClass genericDefinitionEEClass = rtsBuilder.AddEEClass("EEClass GenericDefinition");
                genericDefinitionEEClass.CorTypeAttr = (uint)typeAttributes;
                genericDefinitionEEClass.NumMethods = numMethods;

                MockMethodTable genericDefinitionMethodTable = rtsBuilder.AddMethodTable("MethodTable GenericDefinition");
                genericDefinitionMethodTable.MTFlags = gtd_mtflags;
                genericDefinitionMethodTable.BaseSize = targetTestHelpers.ObjectBaseSize;
                genericDefinitionMethodTable.ParentMethodTable = systemObjectMethodTablePtr;
                genericDefinitionMethodTable.NumVirtuals = numVirtuals;
                genericDefinitionMethodTablePtr = genericDefinitionMethodTable.Address;
                genericDefinitionEEClass.MethodTable = genericDefinitionMethodTable.Address;
                genericDefinitionMethodTable.EEClassOrCanonMT = genericDefinitionEEClass.Address;

                const uint ginst_mtflags = 0x00000010; // TODO: GenericsMask_GenericInst
                MockMethodTable genericInstanceMethodTable = rtsBuilder.AddMethodTable("MethodTable GenericInstance");
                genericInstanceMethodTable.MTFlags = ginst_mtflags;
                genericInstanceMethodTable.BaseSize = targetTestHelpers.ObjectBaseSize;
                genericInstanceMethodTable.ParentMethodTable = genericDefinitionMethodTablePtr;
                genericInstanceMethodTable.NumVirtuals = numVirtuals;
                genericInstanceMethodTablePtr = genericInstanceMethodTable.Address;
                genericInstanceMethodTable.EEClassOrCanonMT = genericDefinitionMethodTable.Address | 1;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle genericInstanceTypeHandle = contract.GetTypeHandle(genericInstanceMethodTablePtr);
        Assert.Equal(genericInstanceMethodTablePtr.Value, genericInstanceTypeHandle.Address.Value);
        Assert.False(contract.IsFreeObjectMethodTable(genericInstanceTypeHandle));
        Assert.False(contract.IsString(genericInstanceTypeHandle));
        Assert.Equal(numMethods, contract.GetNumMethods(genericInstanceTypeHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateArrayInstMethodTable(MockTarget.Architecture arch)
    {
        TargetPointer arrayInstanceMethodTablePtr = default;

        const uint arrayInstanceComponentSize = 392;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
                TargetPointer systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;
                const ushort systemArrayNumInterfaces = 4;
                const ushort systemArrayNumMethods = 37; // Arbitrary. Not trying to exactly match  the real System.Array
                const uint systemArrayCorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);

                MockEEClass systemArrayEEClass = rtsBuilder.AddEEClass("EEClass System.Array");
                systemArrayEEClass.CorTypeAttr = systemArrayCorTypeAttr;
                systemArrayEEClass.NumMethods = systemArrayNumMethods;

                MockMethodTable systemArrayMethodTable = rtsBuilder.AddMethodTable("MethodTable System.Array");
                systemArrayMethodTable.BaseSize = targetTestHelpers.ObjectBaseSize;
                systemArrayMethodTable.ParentMethodTable = systemObjectMethodTablePtr;
                systemArrayMethodTable.NumInterfaces = systemArrayNumInterfaces;
                systemArrayMethodTable.NumVirtuals = 3;
                systemArrayEEClass.MethodTable = systemArrayMethodTable.Address;
                systemArrayMethodTable.EEClassOrCanonMT = systemArrayEEClass.Address;

                const uint arrayInst_mtflags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize | MethodTableFlags_1.WFLAGS_HIGH.Category_Array) | arrayInstanceComponentSize;
                const uint arrayInstCorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Sealed);

                MockEEClass arrayInstanceEEClass = rtsBuilder.AddEEClass("EEClass ArrayInstance");
                arrayInstanceEEClass.CorTypeAttr = arrayInstCorTypeAttr;
                arrayInstanceEEClass.NumMethods = systemArrayNumMethods;

                MockMethodTable arrayInstanceMethodTable = rtsBuilder.AddMethodTable("MethodTable ArrayInstance");
                arrayInstanceMethodTable.MTFlags = arrayInst_mtflags;
                arrayInstanceMethodTable.BaseSize = targetTestHelpers.ObjectBaseSize;
                arrayInstanceMethodTable.ParentMethodTable = systemArrayMethodTable.Address;
                arrayInstanceMethodTable.NumInterfaces = systemArrayNumInterfaces;
                arrayInstanceMethodTable.NumVirtuals = 3;
                arrayInstanceMethodTablePtr = arrayInstanceMethodTable.Address;
                arrayInstanceEEClass.MethodTable = arrayInstanceMethodTable.Address;
                arrayInstanceMethodTable.EEClassOrCanonMT = arrayInstanceEEClass.Address;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle arrayInstanceTypeHandle = contract.GetTypeHandle(arrayInstanceMethodTablePtr);
        Assert.Equal(arrayInstanceMethodTablePtr.Value, arrayInstanceTypeHandle.Address.Value);
        Assert.False(contract.IsFreeObjectMethodTable(arrayInstanceTypeHandle));
        Assert.False(contract.IsString(arrayInstanceTypeHandle));
        Assert.Equal(arrayInstanceComponentSize, contract.GetComponentSize(arrayInstanceTypeHandle));

    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public unsafe void GetMethodTableDataReturnsEInvalidArgWhenEEClassPartiallyReadable(MockTarget.Architecture arch)
    {
        // Reproduces the HResult mismatch from dotnet/diagnostics CI where the cDAC returned
        // CORDBG_E_READVIRTUAL_FAILURE (0x80131c49) instead of E_INVALIDARG (0x80070057)
        // when GetMethodTableData was called with an address whose MethodTable validation
        // passes but subsequent EEClass reads fail.
        TargetPointer methodTablePtr = default;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;

                MockMethodTable methodTable = rtsBuilder.AddMethodTable("PartialEEClass MT");
                methodTable.BaseSize = helpers.ObjectBaseSize;
                methodTable.NumVirtuals = 3;
                methodTablePtr = methodTable.Address;

                int pointerSize = helpers.PointerSize;
                MockMemorySpace.HeapFragment tinyEEClass = rtsBuilder.TypeSystemAllocator.Allocate(
                    (ulong)pointerSize, "Tiny EEClass (MethodTable field only)");
                helpers.WritePointer(tinyEEClass.Data, methodTablePtr);
                methodTable.EEClassOrCanonMT = tinyEEClass.Address;
            });

        ISOSDacInterface sosDac = new SOSDacImpl(target, legacyObj: null);
        DacpMethodTableData mtData = default;
        int hr = sosDac.GetMethodTableData(new ClrDataAddress(methodTablePtr), &mtData);
        AssertHResult(HResults.E_INVALIDARG, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsContinuationReturnsTrueForContinuationType(MockTarget.Architecture arch)
    {
        TargetPointer continuationInstanceMethodTablePtr = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
                TargetPointer systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;
                MockMethodTable continuationBaseMethodTable = rtsBuilder.ContinuationMethodTable;

                MockEEClass continuationInstanceEEClass = rtsBuilder.AddEEClass("ContinuationInstance");
                MockMethodTable continuationInstanceMethodTable = rtsBuilder.AddMethodTable("ContinuationInstance");
                continuationInstanceMethodTable.BaseSize = targetTestHelpers.ObjectBaseSize;
                continuationInstanceMethodTable.ParentMethodTable = continuationBaseMethodTable.Address;
                continuationInstanceMethodTable.NumVirtuals = 3;
                continuationInstanceMethodTablePtr = continuationInstanceMethodTable.Address;
                continuationInstanceEEClass.MethodTable = continuationInstanceMethodTable.Address;
                continuationInstanceMethodTable.EEClassOrCanonMT = continuationInstanceEEClass.Address;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle continuationTypeHandle = contract.GetTypeHandle(continuationInstanceMethodTablePtr);
        Assert.True(contract.IsContinuation(continuationTypeHandle));
        Assert.False(contract.IsFreeObjectMethodTable(continuationTypeHandle));
        Assert.False(contract.IsString(continuationTypeHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public unsafe void GetMethodTableDataReturnsEInvalidArgWhenMethodTablePartiallyReadable(MockTarget.Architecture arch)
    {
        // Analogous to the EEClass test above but for the MethodTable itself.
        // If a MethodTable is only partially readable (enough for validation fields
        // like MTFlags, BaseSize, MTFlags2, EEClassOrCanonMT) but not the full
        // structure (Module, ParentMethodTable, etc.), validation currently passes
        // because NonValidatedMethodTable reads fields lazily. The subsequent
        // Data.MethodTable construction then fails with VirtualReadException,
        // surfacing CORDBG_E_READVIRTUAL_FAILURE instead of E_INVALIDARG.
        TargetPointer tinyMethodTableAddr = default;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;
                Target.TypeInfo mtTypeInfo = TargetTestHelpers.CreateTypeInfo(rtsBuilder.MethodTableLayout);

            // Create a valid EEClass that will point back to our tiny MethodTable
            MockEEClass eeClass = rtsBuilder.AddEEClass("PartialMT EEClass");
            TargetPointer eeClassPtr = eeClass.Address;

            // Allocate a tiny MethodTable fragment — only enough for the fields that
            // validation reads (MTFlags, BaseSize, MTFlags2, EEClassOrCanonMT) but
            // not the full MethodTable. Fields like Module, ParentMethodTable, etc.
            // at subsequent offsets will be unreadable.
            int eeClassOrCanonMTOffset = mtTypeInfo.Fields[nameof(Data.MethodTable.EEClassOrCanonMT)].Offset;
            ulong partialSize = (ulong)(eeClassOrCanonMTOffset + helpers.PointerSize);
            MockMemorySpace.HeapFragment tinyMT = rtsBuilder.TypeSystemAllocator.Allocate(
                partialSize, "Tiny MethodTable (validation fields only)");

            Span<byte> dest = tinyMT.Data;
            helpers.Write(dest.Slice(mtTypeInfo.Fields[nameof(Data.MethodTable.MTFlags)].Offset), (uint)0);
            helpers.Write(dest.Slice(mtTypeInfo.Fields[nameof(Data.MethodTable.BaseSize)].Offset), (uint)helpers.ObjectBaseSize);
            helpers.Write(dest.Slice(mtTypeInfo.Fields[nameof(Data.MethodTable.MTFlags2)].Offset), (uint)0);
            helpers.WritePointer(dest.Slice(eeClassOrCanonMTOffset), eeClassPtr);

            tinyMethodTableAddr = tinyMT.Address;

            // Point the EEClass back at the tiny MethodTable to pass validation
            Target.TypeInfo eeClassTypeInfo = TargetTestHelpers.CreateTypeInfo(rtsBuilder.EEClassLayout);
            Span<byte> eeClassBytes = rtsBuilder.Builder.BorrowAddressRange(
                eeClassPtr, (int)eeClassTypeInfo.Size.Value);
                helpers.WritePointer(
                    eeClassBytes.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset,
                    helpers.PointerSize), tinyMethodTableAddr);
            });

        ISOSDacInterface sosDac = new SOSDacImpl(target, legacyObj: null);
        DacpMethodTableData mtData = default;
        int hr = sosDac.GetMethodTableData(new ClrDataAddress(tinyMethodTableAddr), &mtData);
        AssertHResult(HResults.E_INVALIDARG, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateMultidimArrayRank(MockTarget.Architecture arch)
    {
        TargetPointer rank4MethodTablePtr = default;
        TargetPointer rank1MultiDimMethodTablePtr = default;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
                const uint multidimArrayCorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed);
                const uint multidimFlags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize | MethodTableFlags_1.WFLAGS_HIGH.Category_Array);

                uint baseSize4 = targetTestHelpers.ArrayBaseBaseSize + 4 * sizeof(uint) * 2;
                MockEEClass eeClass4 = rtsBuilder.AddEEClass("EEClass int[,,,]");
                eeClass4.CorTypeAttr = multidimArrayCorTypeAttr;
                MockMethodTable rank4MethodTable = rtsBuilder.AddMethodTable("MethodTable int[,,,]");
                rank4MethodTable.MTFlags = multidimFlags;
                rank4MethodTable.BaseSize = baseSize4;
                rank4MethodTablePtr = rank4MethodTable.Address;
                eeClass4.MethodTable = rank4MethodTable.Address;
                rank4MethodTable.EEClassOrCanonMT = eeClass4.Address;

                uint baseSize1 = targetTestHelpers.ArrayBaseBaseSize + 1 * sizeof(uint) * 2;
                MockEEClass eeClass1 = rtsBuilder.AddEEClass("EEClass int[*]");
                eeClass1.CorTypeAttr = multidimArrayCorTypeAttr;
                MockMethodTable rank1MultiDimMethodTable = rtsBuilder.AddMethodTable("MethodTable int[*]");
                rank1MultiDimMethodTable.MTFlags = multidimFlags;
                rank1MultiDimMethodTable.BaseSize = baseSize1;
                rank1MultiDimMethodTablePtr = rank1MultiDimMethodTable.Address;
                eeClass1.MethodTable = rank1MultiDimMethodTable.Address;
                rank1MultiDimMethodTable.EEClassOrCanonMT = eeClass1.Address;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle rank4Handle = contract.GetTypeHandle(rank4MethodTablePtr);
        Assert.True(contract.IsArray(rank4Handle, out uint rank4));
        Assert.Equal(4u, rank4);

        Contracts.TypeHandle rank1Handle = contract.GetTypeHandle(rank1MultiDimMethodTablePtr);
        Assert.True(contract.IsArray(rank1Handle, out uint rank1));
        Assert.Equal(1u, rank1);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsContinuationReturnsFalseForRegularType(MockTarget.Architecture arch)
    {
        TargetPointer systemObjectMethodTablePtr = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder => systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address);

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle objectTypeHandle = contract.GetTypeHandle(systemObjectMethodTablePtr);
        Assert.False(contract.IsContinuation(objectTypeHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsContinuationReturnsFalseWhenGlobalIsNull(MockTarget.Architecture arch)
    {
        TargetPointer systemObjectMethodTablePtr = default;
        TargetPointer childMethodTablePtr = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
                systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;
                rtsBuilder.SetContinuationMethodTable(0);

                MockEEClass childEEClass = rtsBuilder.AddEEClass("ChildType");
                MockMethodTable childMethodTable = rtsBuilder.AddMethodTable("ChildType");
                childMethodTable.BaseSize = targetTestHelpers.ObjectBaseSize;
                childMethodTable.ParentMethodTable = systemObjectMethodTablePtr;
                childMethodTable.NumVirtuals = 3;
                childMethodTablePtr = childMethodTable.Address;
                childEEClass.MethodTable = childMethodTable.Address;
                childMethodTable.EEClassOrCanonMT = childEEClass.Address;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle objectTypeHandle = contract.GetTypeHandle(systemObjectMethodTablePtr);
        Assert.False(contract.IsContinuation(objectTypeHandle));

        Contracts.TypeHandle childTypeHandle = contract.GetTypeHandle(childMethodTablePtr);
        Assert.False(contract.IsContinuation(childTypeHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateContinuationMethodTablePointer(MockTarget.Architecture arch)
    {
        TargetPointer continuationInstanceMethodTablePtr = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
                TargetPointer systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;
                MockMethodTable continuationBaseMethodTable = rtsBuilder.ContinuationMethodTable;

                MockEEClass sharedEEClass = rtsBuilder.AddEEClass("SubContinuation");
                MockMethodTable sharedCanonMT = rtsBuilder.AddMethodTable("SubContinuationCanon");
                sharedCanonMT.BaseSize = targetTestHelpers.ObjectBaseSize;
                sharedCanonMT.ParentMethodTable = continuationBaseMethodTable.Address;
                sharedCanonMT.NumVirtuals = 3;
                sharedEEClass.MethodTable = sharedCanonMT.Address;
                sharedCanonMT.EEClassOrCanonMT = sharedEEClass.Address;

                MockMethodTable continuationInstanceMethodTable = rtsBuilder.AddMethodTable("ContinuationInstance");
                continuationInstanceMethodTable.BaseSize = targetTestHelpers.ObjectBaseSize;
                continuationInstanceMethodTable.ParentMethodTable = continuationBaseMethodTable.Address;
                continuationInstanceMethodTable.NumVirtuals = 3;
                continuationInstanceMethodTablePtr = continuationInstanceMethodTable.Address;
                continuationInstanceMethodTable.EEClassOrCanonMT = sharedCanonMT.Address | 1;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle continuationTypeHandle = contract.GetTypeHandle(continuationInstanceMethodTablePtr);
        Assert.Equal(continuationInstanceMethodTablePtr.Value, continuationTypeHandle.Address.Value);
        Assert.True(contract.IsContinuation(continuationTypeHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsValueTypeReturnsTrueForValueTypeCategories(MockTarget.Architecture arch)
    {
        TargetPointer valueTypeMTPtr = default;
        TargetPointer nullableMTPtr = default;
        TargetPointer primitiveValueTypeMTPtr = default;
        TargetPointer truePrimitiveMTPtr = default;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetPointer systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;

                MockEEClass vtEEClass = rtsBuilder.AddEEClass("ValueTypeEEClass");
                MockMethodTable vtMT = rtsBuilder.AddMethodTable("ValueType");
                vtMT.MTFlags = (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_ValueType;
                vtMT.BaseSize = rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                vtMT.ParentMethodTable = systemObjectMethodTablePtr;
                vtMT.NumVirtuals = 3;
                vtEEClass.MethodTable = vtMT.Address;
                vtMT.EEClassOrCanonMT = vtEEClass.Address;
                valueTypeMTPtr = vtMT.Address;

                MockEEClass nullableEEClass = rtsBuilder.AddEEClass("NullableEEClass");
                MockMethodTable nullableMT = rtsBuilder.AddMethodTable("Nullable");
                nullableMT.MTFlags = (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_Nullable;
                nullableMT.BaseSize = rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                nullableMT.ParentMethodTable = systemObjectMethodTablePtr;
                nullableMT.NumVirtuals = 3;
                nullableEEClass.MethodTable = nullableMT.Address;
                nullableMT.EEClassOrCanonMT = nullableEEClass.Address;
                nullableMTPtr = nullableMT.Address;

                MockEEClass pvtEEClass = rtsBuilder.AddEEClass("PrimitiveValueTypeEEClass");
                MockMethodTable pvtMT = rtsBuilder.AddMethodTable("PrimitiveValueType");
                pvtMT.MTFlags = (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_PrimitiveValueType;
                pvtMT.BaseSize = rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                pvtMT.ParentMethodTable = systemObjectMethodTablePtr;
                pvtMT.NumVirtuals = 3;
                pvtEEClass.MethodTable = pvtMT.Address;
                pvtMT.EEClassOrCanonMT = pvtEEClass.Address;
                primitiveValueTypeMTPtr = pvtMT.Address;

                MockEEClass tpEEClass = rtsBuilder.AddEEClass("TruePrimitiveEEClass");
                MockMethodTable tpMT = rtsBuilder.AddMethodTable("TruePrimitive");
                tpMT.MTFlags = (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_TruePrimitive;
                tpMT.BaseSize = rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                tpMT.ParentMethodTable = systemObjectMethodTablePtr;
                tpMT.NumVirtuals = 3;
                tpEEClass.MethodTable = tpMT.Address;
                tpMT.EEClassOrCanonMT = tpEEClass.Address;
                truePrimitiveMTPtr = tpMT.Address;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;

        Assert.True(contract.IsValueType(contract.GetTypeHandle(valueTypeMTPtr)));
        Assert.True(contract.IsValueType(contract.GetTypeHandle(nullableMTPtr)));
        Assert.True(contract.IsValueType(contract.GetTypeHandle(primitiveValueTypeMTPtr)));
        Assert.True(contract.IsValueType(contract.GetTypeHandle(truePrimitiveMTPtr)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsValueTypeReturnsFalseForNonValueTypes(MockTarget.Architecture arch)
    {
        TargetPointer systemObjectMethodTablePtr = default;
        TargetPointer interfaceMTPtr = default;
        TargetPointer arrayMTPtr = default;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
                systemObjectMethodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;

                MockEEClass ifaceEEClass = rtsBuilder.AddEEClass("InterfaceEEClass");
                MockMethodTable ifaceMT = rtsBuilder.AddMethodTable("Interface");
                ifaceMT.MTFlags = (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_Interface;
                ifaceMT.BaseSize = targetTestHelpers.ObjectBaseSize;
                ifaceMT.ParentMethodTable = systemObjectMethodTablePtr;
                ifaceMT.NumVirtuals = 3;
                ifaceEEClass.MethodTable = ifaceMT.Address;
                ifaceMT.EEClassOrCanonMT = ifaceEEClass.Address;
                interfaceMTPtr = ifaceMT.Address;

                MockEEClass arrayEEClass = rtsBuilder.AddEEClass("ArrayEEClass");
                MockMethodTable arrayMT = rtsBuilder.AddMethodTable("Array");
                arrayMT.MTFlags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize | MethodTableFlags_1.WFLAGS_HIGH.Category_Array) | 4;
                arrayMT.BaseSize = targetTestHelpers.ObjectBaseSize;
                arrayMT.ParentMethodTable = systemObjectMethodTablePtr;
                arrayMT.NumVirtuals = 3;
                arrayEEClass.MethodTable = arrayMT.Address;
                arrayMT.EEClassOrCanonMT = arrayEEClass.Address;
                arrayMTPtr = arrayMT.Address;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;

        Assert.False(contract.IsValueType(contract.GetTypeHandle(systemObjectMethodTablePtr)));
        Assert.False(contract.IsValueType(contract.GetTypeHandle(interfaceMTPtr)));
        Assert.False(contract.IsValueType(contract.GetTypeHandle(arrayMTPtr)));
    }

    [Theory]
    [MemberData(nameof(StdArchBool))]
    public void RequiresAlign8(MockTarget.Architecture arch, bool flagSet)
    {
        TargetPointer methodTablePtr = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                if (flagSet)
                {
                    MockEEClass eeClass = rtsBuilder.AddEEClass("Align8Type");
                    eeClass.CorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);

                    MockMethodTable methodTable = rtsBuilder.AddMethodTable("Align8Type");
                    methodTable.MTFlags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.Category_ValueType | MethodTableFlags_1.WFLAGS_HIGH.RequiresAlign8);
                    methodTable.BaseSize = rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                    methodTable.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
                    methodTable.NumVirtuals = 3;
                    methodTablePtr = methodTable.Address;
                    eeClass.MethodTable = methodTable.Address;
                    methodTable.EEClassOrCanonMT = eeClass.Address;
                }
                else
                {
                    methodTablePtr = rtsBuilder.SystemObjectMethodTable.Address;
                }
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle typeHandle = contract.GetTypeHandle(methodTablePtr);
        Assert.Equal(flagSet, contract.RequiresAlign8(typeHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCDescSeriesReturnsEmptyForNonMethodTable(MockTarget.Architecture arch)
    {
        // TypeDesc handles should yield no series
        TargetPointer typeDescAddress = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                MockParamTypeDesc typeDesc = rtsBuilder.AddParamTypeDesc();
                typeDescAddress = typeDesc.Address | (ulong)RuntimeTypeSystem_1.TypeHandleBits.TypeDesc;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle typeDescHandle = contract.GetTypeHandle(typeDescAddress);
        Assert.Empty(contract.GetGCDescSeries(typeDescHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCDescSeriesReturnsEmptyWhenNoGCPointers(MockTarget.Architecture arch)
    {
        TargetPointer mtPtr = default;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                MockEEClass eeClass = rtsBuilder.AddEEClass("NoGCPointers");
                MockMethodTable mt = rtsBuilder.AddMethodTable("NoGCPointers");
                uint baseSize = rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                mt.BaseSize = baseSize;
                mt.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
                mt.NumVirtuals = 3;
                eeClass.MethodTable = mt.Address;
                mt.EEClassOrCanonMT = eeClass.Address;
                // MTFlags does NOT have ContainsGCPointers (0x01000000) set
                mtPtr = mt.Address;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle typeHandle = contract.GetTypeHandle(mtPtr);
        Assert.False(contract.ContainsGCPointers(typeHandle));
        Assert.Empty(contract.GetGCDescSeries(typeHandle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCDescSeriesReturnsSingleSeries(MockTarget.Architecture arch)
    {
        TargetPointer mtPtr = default;
        uint expectedSeriesOffset = 0;
        uint expectedSeriesSize = 0;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;
                uint pointerSize = (uint)helpers.PointerSize;

                // Object layout: [ObjHeader][MT*][ref1]
                // BaseSize = ObjHeader + MT* + ref1 = 3 * pointerSize
                uint baseSize = helpers.ObjHeaderSize + 2u * pointerSize;

                // One series covering the single reference field.
                // seriessize is stored as (actualSize - baseSize); for one pointer: pointerSize - baseSize
                ulong rawSeriesSize = pointerSize - baseSize;  // stored as size_t (wraps to large value)
                ulong rawSeriesOffset = helpers.ObjHeaderSize + pointerSize; // after ObjHeader+MT*

                MockEEClass eeClass = rtsBuilder.AddEEClass("SingleRef");
                MockMethodTable mt = rtsBuilder.AddMethodTableWithGCDesc(
                    "SingleRef",
                    baseSize,
                    [(rawSeriesSize, rawSeriesOffset)]);
                mt.MTFlags |= 0x01000000u; // ContainsGCPointers
                mt.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
                mt.NumVirtuals = 3;
                eeClass.MethodTable = mt.Address;
                mt.EEClassOrCanonMT = eeClass.Address;

                mtPtr = mt.Address;
                expectedSeriesOffset = (uint)rawSeriesOffset;
                // After normalization: rawSeriesSize + baseSize = pointerSize (one pointer-sized run)
                expectedSeriesSize = pointerSize;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle typeHandle = contract.GetTypeHandle(mtPtr);
        Assert.True(contract.ContainsGCPointers(typeHandle));

        (uint Offset, uint Size)[] series = contract.GetGCDescSeries(typeHandle).ToArray();
        Assert.Single(series);
        Assert.Equal(expectedSeriesOffset, series[0].Offset);
        Assert.Equal(expectedSeriesSize, series[0].Size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCDescSeriesReturnsMultipleSeriesInOrder(MockTarget.Architecture arch)
    {
        TargetPointer mtPtr = default;
        (uint Offset, uint Size)[] expectedSeries = [];

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;
                uint pointerSize = (uint)helpers.PointerSize;

                // Two separate GC reference runs in the object.
                // Object layout: [ObjHeader][MT*][ref1][nonref][ref2]
                uint baseSize = helpers.ObjHeaderSize + 4u * pointerSize;

                ulong series0Offset = helpers.ObjHeaderSize + pointerSize;            // ref1 field
                ulong series0Size = pointerSize - baseSize;                            // raw stored size

                ulong series1Offset = helpers.ObjHeaderSize + 3u * pointerSize;       // ref2 field
                ulong series1Size = pointerSize - baseSize;

                MockEEClass eeClass = rtsBuilder.AddEEClass("TwoRefs");
                MockMethodTable mt = rtsBuilder.AddMethodTableWithGCDesc(
                    "TwoRefs",
                    baseSize,
                    // Ordered highest (lowest index) to lowest as required by AddMethodTableWithGCDesc
                    [(series0Size, series0Offset), (series1Size, series1Offset)]);
                mt.MTFlags |= 0x01000000u; // ContainsGCPointers
                mt.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
                mt.NumVirtuals = 3;
                eeClass.MethodTable = mt.Address;
                mt.EEClassOrCanonMT = eeClass.Address;

                mtPtr = mt.Address;
                // After normalization (rawSize + baseSize), each series covers one pointer
                expectedSeries =
                [
                    ((uint)series0Offset, pointerSize),
                    ((uint)series1Offset, pointerSize),
                ];
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle typeHandle = contract.GetTypeHandle(mtPtr);

        (uint Offset, uint Size)[] series = contract.GetGCDescSeries(typeHandle).ToArray();
        Assert.Equal(expectedSeries.Length, series.Length);
        for (int i = 0; i < expectedSeries.Length; i++)
        {
            Assert.Equal(expectedSeries[i].Offset, series[i].Offset);
            Assert.Equal(expectedSeries[i].Size, series[i].Size);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCDescSeriesReturnsSingleValueClassSeries(MockTarget.Architecture arch)
    {
        // A negative NumSeries indicates a value-class (repeating) series layout.
        // This models a 1-element array of struct { ref field; }: one val_serie_item with nptrs=1, skip=0.
        TargetPointer mtPtr = default;
        uint expectedOffset = 0;
        uint expectedSize = 0;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;
                uint pointerSize = (uint)helpers.PointerSize;

                // Array of structs each containing one GC ref.
                // startoffset is relative to the object pointer (MT* slot), past MT* + length.
                uint startOffset = 2u * pointerSize; // past MT* + length
                uint componentSize = pointerSize;    // element is struct { ref field; }
                uint baseSize = helpers.ObjHeaderSize + 2u * pointerSize; // ObjHeader + MT* + length

                MockEEClass eeClass = rtsBuilder.AddEEClass("ValueClassArray_1ref");
                MockMethodTable mt = rtsBuilder.AddMethodTableWithValueClassGCDesc(
                    "ValueClassArray_1ref",
                    baseSize,
                    startOffset,
                    [(1, 0)]); // nptrs=1, skip=0
                // Set array flags with componentSize so GetComponentSize returns the element size.
                mt.MTFlags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize
                    | MethodTableFlags_1.WFLAGS_HIGH.Category_Array)
                    | componentSize
                    | 0x01000000u; // ContainsGCPointers
                mt.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
                mt.NumVirtuals = 3;
                eeClass.MethodTable = mt.Address;
                mt.EEClassOrCanonMT = eeClass.Address;
                mtPtr = mt.Address;

                expectedOffset = startOffset;
                expectedSize = 1u * pointerSize;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle typeHandle = contract.GetTypeHandle(mtPtr);
        Assert.True(contract.ContainsGCPointers(typeHandle));

        // Pass numComponents=1 because value-class GCDesc iterates one element per component.
        (uint Offset, uint Size)[] series = contract.GetGCDescSeries(typeHandle, 1).ToArray();
        Assert.Single(series);
        Assert.Equal(expectedOffset, series[0].Offset);
        Assert.Equal(expectedSize, series[0].Size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCDescSeriesReturnsMultipleValueClassSeries(MockTarget.Architecture arch)
    {
        // Two val_serie_items: [2 ptrs, skip 2*ptrSize] [1 ptr, skip 0 bytes]
        // This models a 1-element array of struct { ref a; ref b; int pad1; int pad2; ref c; }
        TargetPointer mtPtr = default;
        (uint Offset, uint Size)[] expectedSeries = [];

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;
                uint pointerSize = (uint)helpers.PointerSize;

                // startoffset is relative to the object pointer (MT* slot), past MT* + length.
                uint startOffset = 2u * pointerSize; // past MT* + length
                uint baseSize = helpers.ObjHeaderSize + 2u * pointerSize; // ObjHeader + MT* + length

                uint skip = 2u * pointerSize; // two pointer-sized non-ref fields between runs
                // Element layout: [ref a (ptr)][ref b (ptr)][pad1 (ptr)][pad2 (ptr)][ref c (ptr)] = 5 * pointerSize
                uint componentSize = (2u + 2u + 1u) * pointerSize;

                MockEEClass eeClass = rtsBuilder.AddEEClass("ValueClassArray_2runs");
                MockMethodTable mt = rtsBuilder.AddMethodTableWithValueClassGCDesc(
                    "ValueClassArray_2runs",
                    baseSize,
                    startOffset,
                    [(2, skip), (1, 0)]); // first: 2 ptrs then skip, second: 1 ptr no skip
                // Set array flags with componentSize so GetComponentSize returns the element size.
                mt.MTFlags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize
                    | MethodTableFlags_1.WFLAGS_HIGH.Category_Array)
                    | componentSize
                    | 0x01000000u; // ContainsGCPointers
                mt.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
                mt.NumVirtuals = 3;
                eeClass.MethodTable = mt.Address;
                mt.EEClassOrCanonMT = eeClass.Address;
                mtPtr = mt.Address;

                expectedSeries =
                [
                    (startOffset, 2u * pointerSize),                                      // first run: 2 ptrs
                    (startOffset + 2u * pointerSize + skip, 1u * pointerSize),            // second run: 1 ptr after skip
                ];
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle typeHandle = contract.GetTypeHandle(mtPtr);

        // Pass numComponents=1 because value-class GCDesc iterates one element per component.
        (uint Offset, uint Size)[] series = contract.GetGCDescSeries(typeHandle, 1).ToArray();
        Assert.Equal(expectedSeries.Length, series.Length);
        for (int i = 0; i < expectedSeries.Length; i++)
        {
            Assert.Equal(expectedSeries[i].Offset, series[i].Offset);
            Assert.Equal(expectedSeries[i].Size, series[i].Size);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCDescSeriesRegularSeriesWithArrayNumComponents(MockTarget.Architecture arch)
    {
        // object[] has a single regular series. When numComponents > 0, the series size
        // should extend across all elements: rawSeriesSize + baseSize + numComponents * componentSize.
        TargetPointer mtPtr = default;
        uint expectedSeriesOffset = 0;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;
                uint pointerSize = (uint)helpers.PointerSize;

                // object[] layout: [ObjHeader][MT*][Length][elem0][elem1][elem2]
                // baseSize covers the header: ObjHeader + MT* + Length = 3 * pointerSize
                // componentSize = pointerSize (each element is a reference)
                uint baseSize = helpers.ObjHeaderSize + 2u * pointerSize; // ObjHeader + MT* + Length field
                uint componentSize = pointerSize;

                // One series starting after ObjHeader+MT*+Length, covering element slots.
                // rawSeriesSize is stored as (actualRunForOneElement - baseSize).
                // For object[], the series covers from first element to end: actualRun = pointerSize (per-element).
                // But the raw value encodes (pointerSize - baseSize) so that rawSeriesSize + objectSize gives total span.
                ulong rawSeriesSize = pointerSize - baseSize; // wraps unsigned
                ulong seriesOffset = helpers.ObjHeaderSize + 2u * pointerSize; // after header + length

                MockEEClass eeClass = rtsBuilder.AddEEClass("ObjectArray");
                MockMethodTable mt = rtsBuilder.AddMethodTableWithGCDesc(
                    "ObjectArray",
                    baseSize,
                    [(rawSeriesSize, seriesOffset)]);
                // Set array flags: HasComponentSize | Category_Array | componentSize in low bits
                mt.MTFlags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize
                    | MethodTableFlags_1.WFLAGS_HIGH.Category_Array)
                    | componentSize
                    | 0x01000000u; // ContainsGCPointers
                mt.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
                mt.NumVirtuals = 3;
                eeClass.MethodTable = mt.Address;
                mt.EEClassOrCanonMT = eeClass.Address;

                mtPtr = mt.Address;
                expectedSeriesOffset = (uint)seriesOffset;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle typeHandle = contract.GetTypeHandle(mtPtr);
        Assert.True(contract.ContainsGCPointers(typeHandle));
        uint pointerSz = (uint)target.PointerSize;

        // With 0 components, series size = rawSeriesSize + baseSize = pointerSize (one element worth)
        (uint Offset, uint Size)[] series0 = contract.GetGCDescSeries(typeHandle, 0).ToArray();
        Assert.Single(series0);
        Assert.Equal(expectedSeriesOffset, series0[0].Offset);
        Assert.Equal(pointerSz, series0[0].Size);

        // With 3 components, objectSize = baseSize + 3*pointerSize, so series size = pointerSize - baseSize + objectSize = 4*pointerSize
        uint numComponents = 3;
        (uint Offset, uint Size)[] series3 = contract.GetGCDescSeries(typeHandle, numComponents).ToArray();
        Assert.Single(series3);
        Assert.Equal(expectedSeriesOffset, series3[0].Offset);
        Assert.Equal((numComponents + 1) * pointerSz, series3[0].Size);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCDescSeriesValueClassRepeatingWithArrayNumComponents(MockTarget.Architecture arch)
    {
        // Array of structs where each element has one GC ref (nptrs=1, skip=pointerSize for a non-ref field).
        // With numComponents > 0, the repeating pattern should iterate across multiple elements.
        TargetPointer mtPtr = default;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            rtsBuilder =>
            {
                TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;
                uint pointerSize = (uint)helpers.PointerSize;

                // Array layout: [ObjHeader][MT*][Length][elem0.ref][elem0.int][elem1.ref][elem1.int]...
                // Each element is { ref field, int field } = 2 * pointerSize.
                uint baseSize = helpers.ObjHeaderSize + 2u * pointerSize; // header + length
                uint componentSize = 2u * pointerSize;
                uint startOffset = helpers.ObjHeaderSize + 2u * pointerSize; // first element starts after header

                MockEEClass eeClass = rtsBuilder.AddEEClass("StructArray");
                MockMethodTable mt = rtsBuilder.AddMethodTableWithValueClassGCDesc(
                    "StructArray",
                    baseSize,
                    startOffset,
                    [(1, pointerSize)]); // nptrs=1 ref, skip=pointerSize (non-ref field)
                mt.MTFlags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize
                    | MethodTableFlags_1.WFLAGS_HIGH.Category_Array)
                    | componentSize
                    | 0x01000000u; // ContainsGCPointers
                mt.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
                mt.NumVirtuals = 3;
                eeClass.MethodTable = mt.Address;
                mt.EEClassOrCanonMT = eeClass.Address;

                mtPtr = mt.Address;
            });

        IRuntimeTypeSystem contract = target.Contracts.RuntimeTypeSystem;
        Contracts.TypeHandle typeHandle = contract.GetTypeHandle(mtPtr);
        Assert.True(contract.ContainsGCPointers(typeHandle));
        uint elemSize = 2 * (uint)target.PointerSize;
        uint startOff = 3u * (uint)target.PointerSize;

        // With 0 components, the for loop runs 0 times so the result is always empty.
        (uint Offset, uint Size)[] series0 = contract.GetGCDescSeries(typeHandle, 0).ToArray();
        Assert.Empty(series0);

        // With 2 components, objectSize = baseSize + 2 * elemSize = baseSize + 4*ptr.
        // The loop should produce 2 runs (one per element), each at the ref field of that element.
        uint numComponents = 2;
        (uint Offset, uint Size)[] series2 = contract.GetGCDescSeries(typeHandle, numComponents).ToArray();
        Assert.Equal(2, series2.Length);
        Assert.Equal(startOff, series2[0].Offset);
        Assert.Equal((uint)target.PointerSize, series2[0].Size);
        Assert.Equal(startOff + elemSize, series2[1].Offset);
        Assert.Equal((uint)target.PointerSize, series2[1].Size);
    }
}
