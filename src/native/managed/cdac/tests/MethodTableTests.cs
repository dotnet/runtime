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
        };

    internal static (string Name, ulong Value)[] CreateContractGlobals(MockRTS rtsBuilder)
        =>
        [
            (nameof(Constants.Globals.FreeObjectMethodTable), rtsBuilder.FreeObjectMethodTableGlobalAddress),
            (nameof(Constants.Globals.ContinuationMethodTable), rtsBuilder.ContinuationMethodTableGlobalAddress),
            (nameof(Constants.Globals.MethodDescAlignment), rtsBuilder.MethodDescAlignment),
            (nameof(Constants.Globals.ArrayBaseSize), rtsBuilder.ArrayBaseSize),
        ];

    internal static TestPlaceholderTarget CreateTarget(MockTarget.Architecture arch, Action<MockRTS> configure)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockRTS rtsBuilder = new(targetBuilder.MemoryBuilder);

        configure?.Invoke(rtsBuilder);

        var target = targetBuilder
            .AddTypes(CreateContractTypes(rtsBuilder))
            .AddGlobals(CreateContractGlobals(rtsBuilder))
            .AddContract<IRuntimeTypeSystem>(version: 1)
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
                rtsBuilder.Builder.AddHeapFragment(tinyEEClass);
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

            rtsBuilder.Builder.AddHeapFragment(tinyMT);
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
}
