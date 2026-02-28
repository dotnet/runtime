// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Moq;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.Tests.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

using MockRTS = MockDescriptors.RuntimeTypeSystem;

public class MethodTableTests
{
    private static void RTSContractHelper(MockTarget.Architecture arch, Action<MockRTS> configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        MockMemorySpace.Builder builder = new(targetTestHelpers);
        MockRTS rtsBuilder = new(builder);

        configure?.Invoke(rtsBuilder);

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, rtsBuilder.Types, rtsBuilder.Globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.RuntimeTypeSystem == ((IContractFactory<IRuntimeTypeSystem>)new RuntimeTypeSystemFactory()).CreateContract(target, 1)));

        testCase(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void HasRuntimeTypeSystemContract(MockTarget.Architecture arch)
    {
        TargetPointer freeObjectMethodTableAddress = default;
        RTSContractHelper(arch,
        (builder) =>
        {
            freeObjectMethodTableAddress = builder.FreeObjectMethodTableAddress;
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem metadataContract = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(metadataContract);
            Contracts.TypeHandle handle = metadataContract.GetTypeHandle(freeObjectMethodTableAddress);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(metadataContract.IsFreeObjectMethodTable(handle));
        });
    }

    internal static (TargetPointer MethodTable, TargetPointer EEClass) AddSystemObjectMethodTable(MockRTS rtsBuilder)
    {
        MockMemorySpace.Builder builder = rtsBuilder.Builder;
        TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
        System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class;
        const int numMethods = 8; // System.Object has 8 methods
        const int numVirtuals = 3; // System.Object has 3 virtual methods
        TargetPointer systemObjectEEClassPtr = rtsBuilder.AddEEClass("System.Object", attr: (uint)typeAttributes, numMethods: numMethods, numNonVirtualSlots: 0);
        TargetPointer systemObjectMethodTablePtr = rtsBuilder.AddMethodTable("System.Object",
                                mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals: numVirtuals);
        rtsBuilder.SetEEClassAndCanonMTRefs(systemObjectEEClassPtr, systemObjectMethodTablePtr);
        return (MethodTable: systemObjectMethodTablePtr, EEClass: systemObjectEEClassPtr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateSystemObjectMethodTable(MockTarget.Architecture arch)
    {
        TargetPointer systemObjectMethodTablePtr = default;
        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            systemObjectMethodTablePtr = AddSystemObjectMethodTable(rtsBuilder).MethodTable;
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem metadataContract = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(metadataContract);
            Contracts.TypeHandle systemObjectTypeHandle = metadataContract.GetTypeHandle(systemObjectMethodTablePtr);
            Assert.Equal(systemObjectMethodTablePtr.Value, systemObjectTypeHandle.Address.Value);
            Assert.False(metadataContract.IsFreeObjectMethodTable(systemObjectTypeHandle));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateSystemStringMethodTable(MockTarget.Architecture arch)
    {
        TargetPointer systemStringMethodTablePtr = default;
        TargetPointer systemStringEEClassPtr = default;
        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            TargetPointer systemObjectMethodTablePtr = AddSystemObjectMethodTable(rtsBuilder).MethodTable;

            System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Sealed;
            const int numMethods = 37; // Arbitrary. Not trying to exactly match  the real System.String
            const int numInterfaces = 8; // Arbitrary
            const int numVirtuals = 3; // at least as many as System.Object
            uint mtflags = (uint)MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize | /*componentSize: */2;
            systemStringEEClassPtr = rtsBuilder.AddEEClass("System.String", attr: (uint)typeAttributes, numMethods: numMethods, numNonVirtualSlots: 0);
            systemStringMethodTablePtr = rtsBuilder.AddMethodTable("System.String",
                                    mtflags: mtflags, mtflags2: default, baseSize: rtsBuilder.Builder.TargetTestHelpers.StringBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: numInterfaces, numVirtuals: numVirtuals);
            rtsBuilder.SetEEClassAndCanonMTRefs(systemStringEEClassPtr, systemStringMethodTablePtr);
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem metadataContract = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(metadataContract);
            Contracts.TypeHandle systemStringTypeHandle = metadataContract.GetTypeHandle(systemStringMethodTablePtr);
            Assert.Equal(systemStringMethodTablePtr.Value, systemStringTypeHandle.Address.Value);
            Assert.False(metadataContract.IsFreeObjectMethodTable(systemStringTypeHandle));
            Assert.True(metadataContract.IsString(systemStringTypeHandle));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MethodTableEEClassInvalidThrows(MockTarget.Architecture arch)
    {
        TargetPointer badMethodTablePtr = default;
        TargetPointer badMethodTableEEClassPtr = new TargetPointer(0x00000010_afafafafa0); // bad address
        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            TargetPointer systemObjectMethodTablePtr = AddSystemObjectMethodTable(rtsBuilder).MethodTable;
            badMethodTablePtr = rtsBuilder.AddMethodTable("Bad MethodTable", mtflags: default, mtflags2: default, baseSize: rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize, module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: 0, numVirtuals: 3);
            // make the method table point at a bad EEClass
            rtsBuilder.SetMethodTableEEClassOrCanonMTRaw(badMethodTablePtr, badMethodTableEEClassPtr);
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem metadataContract = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(metadataContract);
            Assert.Throws<ArgumentException>(() => metadataContract.GetTypeHandle(badMethodTablePtr));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateGenericInstMethodTable(MockTarget.Architecture arch)
    {
        TargetPointer genericDefinitionMethodTablePtr = default;
        TargetPointer genericInstanceMethodTablePtr = default;

        const int numMethods = 17;

        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
            TargetPointer systemObjectMethodTablePtr = AddSystemObjectMethodTable(rtsBuilder).MethodTable;

            System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class;
            const int numInterfaces = 0;
            const int numVirtuals = 3;
            const uint gtd_mtflags = 0x00000030; // TODO: GenericsMask_TypicalInst
            TargetPointer genericDefinitionEEClassPtr = rtsBuilder.AddEEClass("EEClass GenericDefinition", attr: (uint)typeAttributes, numMethods: numMethods, numNonVirtualSlots: 0);
            genericDefinitionMethodTablePtr = rtsBuilder.AddMethodTable("MethodTable GenericDefinition",
                                    mtflags: gtd_mtflags, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: numInterfaces, numVirtuals: numVirtuals);
            rtsBuilder.SetEEClassAndCanonMTRefs(genericDefinitionEEClassPtr, genericDefinitionMethodTablePtr);

            const uint ginst_mtflags = 0x00000010; // TODO: GenericsMask_GenericInst
            genericInstanceMethodTablePtr = rtsBuilder.AddMethodTable("MethodTable GenericInstance",
                                    mtflags: ginst_mtflags, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: genericDefinitionMethodTablePtr, numInterfaces: numInterfaces, numVirtuals: numVirtuals);
            rtsBuilder.SetMethodTableCanonMT(genericInstanceMethodTablePtr, genericDefinitionMethodTablePtr);

        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem metadataContract = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(metadataContract);
            Contracts.TypeHandle genericInstanceTypeHandle = metadataContract.GetTypeHandle(genericInstanceMethodTablePtr);
            Assert.Equal(genericInstanceMethodTablePtr.Value, genericInstanceTypeHandle.Address.Value);
            Assert.False(metadataContract.IsFreeObjectMethodTable(genericInstanceTypeHandle));
            Assert.False(metadataContract.IsString(genericInstanceTypeHandle));
            Assert.Equal(numMethods, metadataContract.GetNumMethods(genericInstanceTypeHandle));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateArrayInstMethodTable(MockTarget.Architecture arch)
    {
        TargetPointer arrayInstanceMethodTablePtr = default;

        const uint arrayInstanceComponentSize = 392;

        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
            TargetPointer systemObjectMethodTablePtr = AddSystemObjectMethodTable(rtsBuilder).MethodTable;
            const ushort systemArrayNumInterfaces = 4;
            const ushort systemArrayNumMethods = 37; // Arbitrary. Not trying to exactly match  the real System.Array
            const uint systemArrayCorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);

            TargetPointer systemArrayEEClassPtr = rtsBuilder.AddEEClass("EEClass System.Array", attr: systemArrayCorTypeAttr, numMethods: systemArrayNumMethods, numNonVirtualSlots: 0);
            TargetPointer systemArrayMethodTablePtr = rtsBuilder.AddMethodTable("MethodTable System.Array",
                                    mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: systemArrayNumInterfaces, numVirtuals: 3);
            rtsBuilder.SetEEClassAndCanonMTRefs(systemArrayEEClassPtr, systemArrayMethodTablePtr);

            const uint arrayInst_mtflags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize | MethodTableFlags_1.WFLAGS_HIGH.Category_Array) | arrayInstanceComponentSize;
            const uint arrayInstCorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Sealed);

            TargetPointer arrayInstanceEEClassPtr = rtsBuilder.AddEEClass("EEClass ArrayInstance", attr: arrayInstCorTypeAttr, numMethods: systemArrayNumMethods, numNonVirtualSlots: 0);
            arrayInstanceMethodTablePtr = rtsBuilder.AddMethodTable("MethodTable ArrayInstance",
                                    mtflags: arrayInst_mtflags, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemArrayMethodTablePtr, numInterfaces: systemArrayNumInterfaces, numVirtuals: 3);
            rtsBuilder.SetEEClassAndCanonMTRefs(arrayInstanceEEClassPtr, arrayInstanceMethodTablePtr);
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem metadataContract = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(metadataContract);
            Contracts.TypeHandle arrayInstanceTypeHandle = metadataContract.GetTypeHandle(arrayInstanceMethodTablePtr);
            Assert.Equal(arrayInstanceMethodTablePtr.Value, arrayInstanceTypeHandle.Address.Value);
            Assert.False(metadataContract.IsFreeObjectMethodTable(arrayInstanceTypeHandle));
            Assert.False(metadataContract.IsString(arrayInstanceTypeHandle));
            Assert.Equal(arrayInstanceComponentSize, metadataContract.GetComponentSize(arrayInstanceTypeHandle));
        });

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

        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;

            // Create a full MethodTable that will pass validation
            methodTablePtr = rtsBuilder.AddMethodTable("PartialEEClass MT",
                mtflags: default, mtflags2: default, baseSize: helpers.ObjectBaseSize,
                module: TargetPointer.Null, parentMethodTable: TargetPointer.Null,
                numInterfaces: 0, numVirtuals: 3);

            // Create a tiny EEClass fragment — only the MethodTable pointer field.
            // Previously, validation only read NonValidatedEEClass.MethodTable (at offset 0),
            // so this minimal fragment was sufficient for validation to pass while later
            // EEClass reads (for MethodDescChunk, NumMethods, etc.) at subsequent offsets
            // would fail with VirtualReadException. After the TypeValidation fix, the full
            // EEClass is eagerly read during validation, so this now correctly reports
            // E_INVALIDARG instead of surfacing VirtualReadException from those later reads.
            int pointerSize = helpers.PointerSize;
            MockMemorySpace.HeapFragment tinyEEClass = rtsBuilder.TypeSystemAllocator.Allocate(
                (ulong)pointerSize, "Tiny EEClass (MethodTable field only)");
            helpers.WritePointer(tinyEEClass.Data, methodTablePtr);
            rtsBuilder.Builder.AddHeapFragment(tinyEEClass);

            // Point the MethodTable's EEClassOrCanonMT at the tiny EEClass
            rtsBuilder.SetMethodTableEEClassOrCanonMTRaw(methodTablePtr, tinyEEClass.Address);
        },
        (target) =>
        {
            ISOSDacInterface sosDac = new SOSDacImpl(target, legacyObj: null);

            DacpMethodTableData mtData = default;
            int hr = sosDac.GetMethodTableData(new ClrDataAddress(methodTablePtr), &mtData);

            // Should return E_INVALIDARG to match legacy DAC behavior
            AssertHResult(HResults.E_INVALIDARG, hr);
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsContinuationReturnsTrueForContinuationType(MockTarget.Architecture arch)
    {
        TargetPointer continuationInstanceMethodTablePtr = default;
        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
            TargetPointer systemObjectMethodTablePtr = AddSystemObjectMethodTable(rtsBuilder).MethodTable;

            // Create the base Continuation class (parent is System.Object)
            TargetPointer continuationBaseEEClassPtr = rtsBuilder.AddEEClass("Continuation", attr: 0, numMethods: 0, numNonVirtualSlots: 0);
            TargetPointer continuationBaseMethodTablePtr = rtsBuilder.AddMethodTable("Continuation",
                                    mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: 0, numVirtuals: 3);
            rtsBuilder.SetEEClassAndCanonMTRefs(continuationBaseEEClassPtr, continuationBaseMethodTablePtr);

            // Set the global to point to the base continuation MT
            rtsBuilder.SetContinuationMethodTable(continuationBaseMethodTablePtr);

            // Create a derived continuation instance MT (shares EEClass with the base, parent is the base continuation MT)
            TargetPointer continuationInstanceEEClassPtr = rtsBuilder.AddEEClass("ContinuationInstance", attr: 0, numMethods: 0, numNonVirtualSlots: 0);
            continuationInstanceMethodTablePtr = rtsBuilder.AddMethodTable("ContinuationInstance",
                                    mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: continuationBaseMethodTablePtr, numInterfaces: 0, numVirtuals: 3);
            // Continuation instances share the EEClass with the base, similar to arrays
            rtsBuilder.SetEEClassAndCanonMTRefs(continuationInstanceEEClassPtr, continuationInstanceMethodTablePtr);
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(rts);
            Contracts.TypeHandle continuationTypeHandle = rts.GetTypeHandle(continuationInstanceMethodTablePtr);
            Assert.True(rts.IsContinuation(continuationTypeHandle));
            Assert.False(rts.IsFreeObjectMethodTable(continuationTypeHandle));
            Assert.False(rts.IsString(continuationTypeHandle));
        });
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

        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;
            Target.TypeInfo mtTypeInfo = rtsBuilder.Types[DataType.MethodTable];

            // Create a valid EEClass that will point back to our tiny MethodTable
            TargetPointer eeClassPtr = rtsBuilder.AddEEClass("PartialMT EEClass",
                attr: 0, numMethods: 0, numNonVirtualSlots: 0);

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
            Target.TypeInfo eeClassTypeInfo = rtsBuilder.Types[DataType.EEClass];
            Span<byte> eeClassBytes = rtsBuilder.Builder.BorrowAddressRange(
                eeClassPtr, (int)eeClassTypeInfo.Size.Value);
            helpers.WritePointer(
                eeClassBytes.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset,
                helpers.PointerSize), tinyMethodTableAddr);
        },
        (target) =>
        {
            ISOSDacInterface sosDac = new SOSDacImpl(target, legacyObj: null);

            DacpMethodTableData mtData = default;
            int hr = sosDac.GetMethodTableData(new ClrDataAddress(tinyMethodTableAddr), &mtData);

            // Should return E_INVALIDARG to match legacy DAC behavior
            AssertHResult(HResults.E_INVALIDARG, hr);
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsContinuationReturnsFalseForRegularType(MockTarget.Architecture arch)
    {
        TargetPointer systemObjectMethodTablePtr = default;
        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            systemObjectMethodTablePtr = AddSystemObjectMethodTable(rtsBuilder).MethodTable;
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(rts);
            Contracts.TypeHandle objectTypeHandle = rts.GetTypeHandle(systemObjectMethodTablePtr);
            Assert.False(rts.IsContinuation(objectTypeHandle));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsContinuationReturnsFalseWhenGlobalIsNull(MockTarget.Architecture arch)
    {
        TargetPointer systemObjectMethodTablePtr = default;
        TargetPointer childMethodTablePtr = default;
        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
            systemObjectMethodTablePtr = AddSystemObjectMethodTable(rtsBuilder).MethodTable;

            // Don't set the continuation global (it remains null)
            // Create a child type with System.Object as parent
            TargetPointer childEEClassPtr = rtsBuilder.AddEEClass("ChildType", attr: 0, numMethods: 0, numNonVirtualSlots: 0);
            childMethodTablePtr = rtsBuilder.AddMethodTable("ChildType",
                                    mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: 0, numVirtuals: 3);
            rtsBuilder.SetEEClassAndCanonMTRefs(childEEClassPtr, childMethodTablePtr);
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(rts);

            // System.Object has ParentMethodTable == Null, same as the null continuation global.
            // Verify the null guard prevents a false positive match.
            Contracts.TypeHandle objectTypeHandle = rts.GetTypeHandle(systemObjectMethodTablePtr);
            Assert.False(rts.IsContinuation(objectTypeHandle));

            Contracts.TypeHandle childTypeHandle = rts.GetTypeHandle(childMethodTablePtr);
            Assert.False(rts.IsContinuation(childTypeHandle));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateContinuationMethodTablePointer(MockTarget.Architecture arch)
    {
        TargetPointer continuationInstanceMethodTablePtr = default;
        RTSContractHelper(arch,
        (rtsBuilder) =>
        {
            TargetTestHelpers targetTestHelpers = rtsBuilder.Builder.TargetTestHelpers;
            TargetPointer systemObjectMethodTablePtr = AddSystemObjectMethodTable(rtsBuilder).MethodTable;

            // Create the base Continuation class
            TargetPointer continuationBaseEEClassPtr = rtsBuilder.AddEEClass("Continuation", attr: 0, numMethods: 0, numNonVirtualSlots: 0);
            TargetPointer continuationBaseMethodTablePtr = rtsBuilder.AddMethodTable("Continuation",
                                    mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: 0, numVirtuals: 3);
            rtsBuilder.SetEEClassAndCanonMTRefs(continuationBaseEEClassPtr, continuationBaseMethodTablePtr);
            rtsBuilder.SetContinuationMethodTable(continuationBaseMethodTablePtr);

            // Create a derived continuation instance
            // Continuation instances share the EEClass with the singleton sub-continuation class
            TargetPointer sharedEEClassPtr = rtsBuilder.AddEEClass("SubContinuation", attr: 0, numMethods: 0, numNonVirtualSlots: 0);
            TargetPointer sharedCanonMTPtr = rtsBuilder.AddMethodTable("SubContinuationCanon",
                                    mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: continuationBaseMethodTablePtr, numInterfaces: 0, numVirtuals: 3);
            rtsBuilder.SetEEClassAndCanonMTRefs(sharedEEClassPtr, sharedCanonMTPtr);

            // The actual continuation instance MT points to the shared EEClass via CanonMT
            continuationInstanceMethodTablePtr = rtsBuilder.AddMethodTable("ContinuationInstance",
                                    mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: continuationBaseMethodTablePtr, numInterfaces: 0, numVirtuals: 3);
            rtsBuilder.SetMethodTableCanonMT(continuationInstanceMethodTablePtr, sharedCanonMTPtr);
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(rts);
            // Validation should succeed - the MT→CanonMT→EEClass→MT roundtrip is handled for continuations
            Contracts.TypeHandle continuationTypeHandle = rts.GetTypeHandle(continuationInstanceMethodTablePtr);
            Assert.Equal(continuationInstanceMethodTablePtr.Value, continuationTypeHandle.Address.Value);
            Assert.True(rts.IsContinuation(continuationTypeHandle));
        });
    }
}
