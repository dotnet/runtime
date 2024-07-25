// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

using MockRTS = MockDescriptors.RuntimeTypeSystem;

public unsafe class MethodTableTests
{
    // a delegate for adding more heap fragments to the context builder
    private delegate MockMemorySpace.Builder ConfigureContextBuilder(MockMemorySpace.Builder builder);

    private static void RTSContractHelper(MockTarget.Architecture arch, ConfigureContextBuilder configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        string metadataTypesJson = TargetTestHelpers.MakeTypesJson(MockRTS.Types);
        string metadataGlobalsJson = TargetTestHelpers.MakeGlobalsJson(MockRTS.Globals);
        byte[] json = Encoding.UTF8.GetBytes($$"""
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {
                "{{nameof(Contracts.RuntimeTypeSystem)}}": 1
            },
            "types": { {{metadataTypesJson}} },
            "globals": { {{metadataGlobalsJson}} }
        }
        """);
        Span<byte> descriptor = stackalloc byte[targetTestHelpers.ContractDescriptorSize];
        targetTestHelpers.ContractDescriptorFill(descriptor, json.Length, MockRTS.Globals.Length);

        int pointerSize = targetTestHelpers.PointerSize;
        Span<byte> pointerData = stackalloc byte[MockRTS.Globals.Length * pointerSize];
        for (int i = 0; i < MockRTS.Globals.Length; i++)
        {
            var (_, value, _) = MockRTS.Globals[i];
            targetTestHelpers.WritePointer(pointerData.Slice(i * pointerSize), value);
        }

        fixed (byte* jsonPtr = json)
        {
            MockMemorySpace.Builder builder = new();

            builder = builder.SetDescriptor(descriptor)
                    .SetJson(json)
                    .SetPointerData(pointerData);

            builder = MockRTS.AddGlobalPointers(targetTestHelpers, builder);

            if (configure != null)
            {
                builder = configure(builder);
            }

            using MockMemorySpace.ReadContext context = builder.Create();

            bool success = MockMemorySpace.TryCreateTarget(&context, out Target? target);
            Assert.True(success);

            testCase(target);
        }
        GC.KeepAlive(json);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void HasRuntimeTypeSystemContract(MockTarget.Architecture arch)
    {
        RTSContractHelper(arch, default, (target) =>
        {
            Contracts.IRuntimeTypeSystem metadataContract = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(metadataContract);
            Contracts.TypeHandle handle = metadataContract.GetTypeHandle(MockRTS.TestFreeObjectMethodTableAddress);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(metadataContract.IsFreeObjectMethodTable(handle));
        });
    }

    private static MockMemorySpace.Builder AddSystemObject(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer systemObjectMethodTablePtr, TargetPointer systemObjectEEClassPtr)
    {
        System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class;
        const int numMethods = 8; // System.Object has 8 methods
        const int numVirtuals = 3; // System.Object has 3 virtual methods
        builder = MockRTS.AddEEClass(targetTestHelpers, builder, systemObjectEEClassPtr, "System.Object", systemObjectMethodTablePtr, attr: (uint)typeAttributes, numMethods: numMethods, numNonVirtualSlots: 0);
        builder = MockRTS.AddMethodTable(targetTestHelpers, builder, systemObjectMethodTablePtr, "System.Object", systemObjectEEClassPtr,
                                mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals: numVirtuals);
        return builder;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateSystemObjectMethodTable(MockTarget.Architecture arch)
    {
        const ulong SystemObjectMethodTableAddress = 0x00000000_7c000010;
        const ulong SystemObjectEEClassAddress = 0x00000000_7c0000d0;
        TargetPointer systemObjectMethodTablePtr = new TargetPointer(SystemObjectMethodTableAddress);
        TargetPointer systemObjectEEClassPtr = new TargetPointer(SystemObjectEEClassAddress);
        TargetTestHelpers targetTestHelpers = new(arch);
        RTSContractHelper(arch,
        (builder) =>
        {
            builder = AddSystemObject(targetTestHelpers, builder, systemObjectMethodTablePtr, systemObjectEEClassPtr);
            return builder;
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
        const ulong SystemObjectMethodTableAddress = 0x00000000_7c000010;
        const ulong SystemObjectEEClassAddress = 0x00000000_7c0000d0;
        TargetPointer systemObjectMethodTablePtr = new TargetPointer(SystemObjectMethodTableAddress);
        TargetPointer systemObjectEEClassPtr = new TargetPointer(SystemObjectEEClassAddress);

        const ulong SystemStringMethodTableAddress = 0x00000000_7c002010;
        const ulong SystemStringEEClassAddress = 0x00000000_7c0020d0;
        TargetPointer systemStringMethodTablePtr = new TargetPointer(SystemStringMethodTableAddress);
        TargetPointer systemStringEEClassPtr = new TargetPointer(SystemStringEEClassAddress);
        TargetTestHelpers targetTestHelpers = new(arch);
        RTSContractHelper(arch,
        (builder) =>
        {
            builder = AddSystemObject(targetTestHelpers, builder, systemObjectMethodTablePtr, systemObjectEEClassPtr);
            System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Sealed;
            const int numMethods = 37; // Arbitrary. Not trying to exactly match  the real System.String
            const int numInterfaces = 8; // Arbitrary
            const int numVirtuals = 3; // at least as many as System.Object
            uint mtflags = (uint)RuntimeTypeSystem_1.WFLAGS_HIGH.HasComponentSize | /*componentSize: */2;
            builder = MockRTS.AddEEClass(targetTestHelpers, builder, systemStringEEClassPtr, "System.String", systemStringMethodTablePtr, attr: (uint)typeAttributes, numMethods: numMethods, numNonVirtualSlots: 0);
            builder = MockRTS.AddMethodTable(targetTestHelpers, builder, systemStringMethodTablePtr, "System.String", systemStringEEClassPtr,
                                    mtflags: mtflags, mtflags2: default, baseSize: targetTestHelpers.StringBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: numInterfaces, numVirtuals: numVirtuals);
            return builder;
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
        TargetTestHelpers targetTestHelpers = new(arch);
        const ulong SystemObjectMethodTableAddress = 0x00000000_7c000010;
        const ulong SystemObjectEEClassAddress = 0x00000000_7c0000d0;
        TargetPointer systemObjectMethodTablePtr = new TargetPointer(SystemObjectMethodTableAddress);
        TargetPointer systemObjectEEClassPtr = new TargetPointer(SystemObjectEEClassAddress);

        const ulong badMethodTableAddress = 0x00000000_4a000100; // place a normal-looking MethodTable here
        const ulong badMethodTableEEClassAddress = 0x00000010_afafafafa0; // bad address
        TargetPointer badMethodTablePtr = new TargetPointer(badMethodTableAddress);
        TargetPointer badMethodTableEEClassPtr = new TargetPointer(badMethodTableEEClassAddress);
        RTSContractHelper(arch,
        (builder) =>
        {
            builder = AddSystemObject(targetTestHelpers, builder, systemObjectMethodTablePtr, systemObjectEEClassPtr);
            builder = MockRTS.AddMethodTable(targetTestHelpers, builder, badMethodTablePtr, "Bad MethodTable", badMethodTableEEClassPtr, mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize, module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: 0, numVirtuals: 3);
            return builder;
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem metadataContract = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(metadataContract);
            Assert.Throws<InvalidOperationException>(() => metadataContract.GetTypeHandle(badMethodTablePtr));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateGenericInstMethodTable(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        const ulong SystemObjectMethodTableAddress = 0x00000000_7c000010;
        const ulong SystemObjectEEClassAddress = 0x00000000_7c0000d0;
        TargetPointer systemObjectMethodTablePtr = new TargetPointer(SystemObjectMethodTableAddress);
        TargetPointer systemObjectEEClassPtr = new TargetPointer(SystemObjectEEClassAddress);

        const ulong genericDefinitionMethodTableAddress = 0x00000000_5d004040;
        const ulong genericDefinitionEEClassAddress = 0x00000000_5d0040c0;
        TargetPointer genericDefinitionMethodTablePtr = new TargetPointer(genericDefinitionMethodTableAddress);
        TargetPointer genericDefinitionEEClassPtr = new TargetPointer(genericDefinitionEEClassAddress);

        const ulong genericInstanceMethodTableAddress = 0x00000000_330000a0;
        TargetPointer genericInstanceMethodTablePtr = new TargetPointer(genericInstanceMethodTableAddress);

        const int numMethods = 17;

        RTSContractHelper(arch,
        (builder) =>
        {
            builder = AddSystemObject(targetTestHelpers, builder, systemObjectMethodTablePtr, systemObjectEEClassPtr);

            System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class;
            const int numInterfaces = 0;
            const int numVirtuals = 3;
            const uint gtd_mtflags = 0x00000030; // TODO: GenericsMask_TypicalInst
            builder = MockRTS.AddEEClass(targetTestHelpers, builder, genericDefinitionEEClassPtr, "EEClass GenericDefinition", genericDefinitionMethodTablePtr, attr: (uint)typeAttributes, numMethods: numMethods, numNonVirtualSlots: 0);
            builder = MockRTS.AddMethodTable(targetTestHelpers, builder, genericDefinitionMethodTablePtr, "MethodTable GenericDefinition", genericDefinitionEEClassPtr,
                                    mtflags: gtd_mtflags, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: numInterfaces, numVirtuals: numVirtuals);

            const uint ginst_mtflags = 0x00000010; // TODO: GenericsMask_GenericInst
            TargetPointer ginstCanonMT = new TargetPointer(genericDefinitionMethodTablePtr.Value | (ulong)1);
            builder = MockRTS.AddMethodTable(targetTestHelpers, builder, genericInstanceMethodTablePtr, "MethodTable GenericInstance", eeClassOrCanonMT: ginstCanonMT,
                                    mtflags: ginst_mtflags, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: genericDefinitionMethodTablePtr, numInterfaces: numInterfaces, numVirtuals: numVirtuals);

            return builder;
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
        TargetTestHelpers targetTestHelpers = new(arch);
        const ulong SystemObjectMethodTableAddress = 0x00000000_7c000010;
        const ulong SystemObjectEEClassAddress = 0x00000000_7c0000d0;
        TargetPointer systemObjectMethodTablePtr = new TargetPointer(SystemObjectMethodTableAddress);
        TargetPointer systemObjectEEClassPtr = new TargetPointer(SystemObjectEEClassAddress);

        const ulong SystemArrayMethodTableAddress = 0x00000000_7c00a010;
        const ulong SystemArrayEEClassAddress = 0x00000000_7c00a0d0;
        TargetPointer systemArrayMethodTablePtr = new TargetPointer(SystemArrayMethodTableAddress);
        TargetPointer systemArrayEEClassPtr = new TargetPointer(SystemArrayEEClassAddress);

        const ulong arrayInstanceMethodTableAddress = 0x00000000_330000a0;
        const ulong arrayInstanceEEClassAddress = 0x00000000_330001d0;
        TargetPointer arrayInstanceMethodTablePtr = new TargetPointer(arrayInstanceMethodTableAddress);
        TargetPointer arrayInstanceEEClassPtr = new TargetPointer(arrayInstanceEEClassAddress);

        const uint arrayInstanceComponentSize = 392;

        RTSContractHelper(arch,
        (builder) =>
        {
            builder = AddSystemObject(targetTestHelpers, builder, systemObjectMethodTablePtr, systemObjectEEClassPtr);
            const ushort systemArrayNumInterfaces = 4;
            const ushort systemArrayNumMethods = 37; // Arbitrary. Not trying to exactly match  the real System.Array
            const uint systemArrayCorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);

            builder = MockRTS.AddEEClass(targetTestHelpers, builder, systemArrayEEClassPtr, "EEClass System.Array", systemArrayMethodTablePtr, attr: systemArrayCorTypeAttr, numMethods: systemArrayNumMethods, numNonVirtualSlots: 0);
            builder = MockRTS.AddMethodTable(targetTestHelpers, builder, systemArrayMethodTablePtr, "MethodTable System.Array", systemArrayEEClassPtr,
                                    mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: systemArrayNumInterfaces, numVirtuals: 3);

            const uint arrayInst_mtflags = (uint)(RuntimeTypeSystem_1.WFLAGS_HIGH.HasComponentSize | RuntimeTypeSystem_1.WFLAGS_HIGH.Category_Array) | arrayInstanceComponentSize;
            const uint arrayInstCorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Sealed);

            builder = MockRTS.AddEEClass(targetTestHelpers, builder, arrayInstanceEEClassPtr, "EEClass ArrayInstance", arrayInstanceMethodTablePtr, attr: arrayInstCorTypeAttr, numMethods: systemArrayNumMethods, numNonVirtualSlots: 0);
            builder = MockRTS.AddMethodTable(targetTestHelpers, builder, arrayInstanceMethodTablePtr, "MethodTable ArrayInstance", arrayInstanceEEClassPtr,
                                    mtflags: arrayInst_mtflags, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemArrayMethodTablePtr, numInterfaces: systemArrayNumInterfaces, numVirtuals: 3);

            return builder;
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
