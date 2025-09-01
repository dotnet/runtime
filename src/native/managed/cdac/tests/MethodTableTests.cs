// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Moq;
using Xunit;

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
}
