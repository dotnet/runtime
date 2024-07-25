// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public unsafe class MethodTableTests
{
    const ulong TestFreeObjectMethodTableGlobalAddress = 0x00000000_7a0000a0;
    const ulong TestFreeObjectMethodTableAddress = 0x00000000_7a0000a8;

    private static readonly Target.TypeInfo MethodTableTypeInfo = new()
    {
        Fields = {
            { nameof(Data.MethodTable.MTFlags), new() { Offset = 4, Type = DataType.uint32}},
            { nameof(Data.MethodTable.BaseSize), new() { Offset = 8, Type = DataType.uint32}},
            { nameof(Data.MethodTable.MTFlags2), new() { Offset = 12, Type = DataType.uint32}},
            { nameof(Data.MethodTable.EEClassOrCanonMT), new () { Offset = 16, Type = DataType.nuint}},
            { nameof(Data.MethodTable.Module), new () { Offset = 24, Type = DataType.pointer}},
            { nameof(Data.MethodTable.ParentMethodTable), new () { Offset = 40, Type = DataType.pointer}},
            { nameof(Data.MethodTable.NumInterfaces), new () { Offset = 48, Type = DataType.uint16}},
            { nameof(Data.MethodTable.NumVirtuals), new () { Offset = 50, Type = DataType.uint16}},
            { nameof(Data.MethodTable.PerInstInfo), new () { Offset = 56, Type = DataType.pointer}},
        }
    };

    private static readonly Target.TypeInfo EEClassTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { nameof (Data.EEClass.MethodTable), new () { Offset = 8, Type = DataType.pointer}},
            { nameof (Data.EEClass.CorTypeAttr), new () { Offset = 16, Type = DataType.uint32}},
            { nameof (Data.EEClass.NumMethods), new () { Offset = 20, Type = DataType.uint16}},
            { nameof (Data.EEClass.InternalCorElementType), new () { Offset = 22, Type = DataType.uint8}},
            { nameof (Data.EEClass.NumNonVirtualSlots), new () { Offset = 24, Type = DataType.uint16}},
        }
    };

    private static readonly Target.TypeInfo ArrayClassTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { nameof (Data.ArrayClass.Rank), new () { Offset = 0x70, Type = DataType.uint8}},
        }
    };

    internal static readonly (DataType Type, Target.TypeInfo Info)[] RTSTypes =
    [
        (DataType.MethodTable, MethodTableTypeInfo),
        (DataType.EEClass, EEClassTypeInfo),
        (DataType.ArrayClass, ArrayClassTypeInfo),
    ];

    internal static readonly (string Name, ulong Value, string? Type)[] RTSGlobals =
    [
        (nameof(Constants.Globals.FreeObjectMethodTable), TestFreeObjectMethodTableGlobalAddress, null),
        (nameof(Constants.Globals.MethodDescAlignment), 8, nameof(DataType.uint64)),
    ];

    internal static MockMemorySpace.Builder AddFreeObjectMethodTable(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder)
    {
        MockMemorySpace.HeapFragment globalAddr = new() { Name = "Address of Free Object Method Table", Address = TestFreeObjectMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
        targetTestHelpers.WritePointer(globalAddr.Data, TestFreeObjectMethodTableAddress);
        return builder.AddHeapFragments([
            globalAddr,
            new () { Name = "Free Object Method Table", Address = TestFreeObjectMethodTableAddress, Data = new byte[targetTestHelpers.SizeOfTypeInfo(MethodTableTypeInfo)] }
        ]);
    }

    internal static MockMemorySpace.Builder AddEEClass(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer eeClassPtr, string name, TargetPointer canonMTPtr, uint attr, ushort numMethods, ushort numNonVirtualSlots)
    {
        MockMemorySpace.HeapFragment eeClassFragment = new() { Name = $"EEClass '{name}'", Address = eeClassPtr, Data = new byte[targetTestHelpers.SizeOfTypeInfo(EEClassTypeInfo)] };
        Span<byte> dest = eeClassFragment.Data;
        targetTestHelpers.WritePointer(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset), canonMTPtr);
        targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
        targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
        targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
        return builder.AddHeapFragment(eeClassFragment);
    }

    internal static MockMemorySpace.Builder AddArrayClass(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer eeClassPtr, string name, TargetPointer canonMTPtr, uint attr, ushort numMethods, ushort numNonVirtualSlots, byte rank)
    {
        int size = targetTestHelpers.SizeOfTypeInfo(EEClassTypeInfo) + targetTestHelpers.SizeOfTypeInfo(ArrayClassTypeInfo);
        MockMemorySpace.HeapFragment eeClassFragment = new() { Name = $"ArrayClass '{name}'", Address = eeClassPtr, Data = new byte[size] };
        Span<byte> dest = eeClassFragment.Data;
        targetTestHelpers.WritePointer(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset), canonMTPtr);
        targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
        targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
        targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
        targetTestHelpers.Write(dest.Slice(ArrayClassTypeInfo.Fields[nameof(Data.ArrayClass.Rank)].Offset), rank);
        return builder.AddHeapFragment(eeClassFragment);
    }

    internal static MockMemorySpace.Builder AddMethodTable(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer methodTablePtr, string name, TargetPointer eeClassOrCanonMT, uint mtflags, uint mtflags2, uint baseSize,
                                                        TargetPointer module, TargetPointer parentMethodTable, ushort numInterfaces, ushort numVirtuals)
    {
        MockMemorySpace.HeapFragment methodTableFragment = new() { Name = $"MethodTable '{name}'", Address = methodTablePtr, Data = new byte[targetTestHelpers.SizeOfTypeInfo(MethodTableTypeInfo)] };
        Span<byte> dest = methodTableFragment.Data;
        targetTestHelpers.WritePointer(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.EEClassOrCanonMT)].Offset), eeClassOrCanonMT);
        targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags)].Offset), mtflags);
        targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags2)].Offset), mtflags2);
        targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.BaseSize)].Offset), baseSize);
        targetTestHelpers.WritePointer(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.Module)].Offset), module);
        targetTestHelpers.WritePointer(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.ParentMethodTable)].Offset), parentMethodTable);
        targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.NumInterfaces)].Offset), numInterfaces);
        targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.NumVirtuals)].Offset), numVirtuals);

        // TODO fill in the rest of the fields
        return builder.AddHeapFragment(methodTableFragment);
    }

    // a delegate for adding more heap fragments to the context builder
    private delegate MockMemorySpace.Builder ConfigureContextBuilder(MockMemorySpace.Builder builder);

    private static void RTSContractHelper(MockTarget.Architecture arch, ConfigureContextBuilder configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        string metadataTypesJson = TargetTestHelpers.MakeTypesJson(RTSTypes);
        string metadataGlobalsJson = TargetTestHelpers.MakeGlobalsJson(RTSGlobals);
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
        targetTestHelpers.ContractDescriptorFill(descriptor, json.Length, RTSGlobals.Length);

        int pointerSize = targetTestHelpers.PointerSize;
        Span<byte> pointerData = stackalloc byte[RTSGlobals.Length * pointerSize];
        for (int i = 0; i < RTSGlobals.Length; i++)
        {
            var (_, value, _) = RTSGlobals[i];
            targetTestHelpers.WritePointer(pointerData.Slice(i * pointerSize), value);
        }

        fixed (byte* jsonPtr = json)
        {
            MockMemorySpace.Builder builder = new();

            builder = builder.SetDescriptor(descriptor)
                    .SetJson(json)
                    .SetPointerData(pointerData);

            builder = AddFreeObjectMethodTable(targetTestHelpers, builder);

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
            Contracts.TypeHandle handle = metadataContract.GetTypeHandle(TestFreeObjectMethodTableAddress);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(metadataContract.IsFreeObjectMethodTable(handle));
        });
    }

    private static MockMemorySpace.Builder AddSystemObject(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer systemObjectMethodTablePtr, TargetPointer systemObjectEEClassPtr)
    {
        System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class;
        const int numMethods = 8; // System.Object has 8 methods
        const int numVirtuals = 3; // System.Object has 3 virtual methods
        builder = AddEEClass(targetTestHelpers, builder, systemObjectEEClassPtr, "System.Object", systemObjectMethodTablePtr, attr: (uint)typeAttributes, numMethods: numMethods, numNonVirtualSlots: 0);
        builder = AddMethodTable(targetTestHelpers, builder, systemObjectMethodTablePtr, "System.Object", systemObjectEEClassPtr,
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
            builder = AddEEClass(targetTestHelpers, builder, systemStringEEClassPtr, "System.String", systemStringMethodTablePtr, attr: (uint)typeAttributes, numMethods: numMethods, numNonVirtualSlots: 0);
            builder = AddMethodTable(targetTestHelpers, builder, systemStringMethodTablePtr, "System.String", systemStringEEClassPtr,
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
            builder = AddMethodTable(targetTestHelpers, builder, badMethodTablePtr, "Bad MethodTable", badMethodTableEEClassPtr, mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize, module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: 0, numVirtuals: 3);
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
            builder = AddEEClass(targetTestHelpers, builder, genericDefinitionEEClassPtr, "EEClass GenericDefinition", genericDefinitionMethodTablePtr, attr: (uint)typeAttributes, numMethods: numMethods, numNonVirtualSlots: 0);
            builder = AddMethodTable(targetTestHelpers, builder, genericDefinitionMethodTablePtr, "MethodTable GenericDefinition", genericDefinitionEEClassPtr,
                                    mtflags: gtd_mtflags, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: numInterfaces, numVirtuals: numVirtuals);

            const uint ginst_mtflags = 0x00000010; // TODO: GenericsMask_GenericInst
            TargetPointer ginstCanonMT = new TargetPointer(genericDefinitionMethodTablePtr.Value | (ulong)1);
            builder = AddMethodTable(targetTestHelpers, builder, genericInstanceMethodTablePtr, "MethodTable GenericInstance", eeClassOrCanonMT: ginstCanonMT,
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

            builder = AddEEClass(targetTestHelpers, builder, systemArrayEEClassPtr, "EEClass System.Array", systemArrayMethodTablePtr, attr: systemArrayCorTypeAttr, numMethods: systemArrayNumMethods, numNonVirtualSlots: 0);
            builder = AddMethodTable(targetTestHelpers, builder, systemArrayMethodTablePtr, "MethodTable System.Array", systemArrayEEClassPtr,
                                    mtflags: default, mtflags2: default, baseSize: targetTestHelpers.ObjectBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: systemArrayNumInterfaces, numVirtuals: 3);

            const uint arrayInst_mtflags = (uint)(RuntimeTypeSystem_1.WFLAGS_HIGH.HasComponentSize | RuntimeTypeSystem_1.WFLAGS_HIGH.Category_Array) | arrayInstanceComponentSize;
            const uint arrayInstCorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Sealed);

            builder = AddEEClass(targetTestHelpers, builder, arrayInstanceEEClassPtr, "EEClass ArrayInstance", arrayInstanceMethodTablePtr, attr: arrayInstCorTypeAttr, numMethods: systemArrayNumMethods, numNonVirtualSlots: 0);
            builder = AddMethodTable(targetTestHelpers, builder, arrayInstanceMethodTablePtr, "MethodTable ArrayInstance", arrayInstanceEEClassPtr,
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
