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
        }
    };

    private static readonly Target.TypeInfo EEClassTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { nameof (Data.EEClass.MethodTable), new () { Offset = 8, Type = DataType.pointer}},
            { nameof (Data.EEClass.CorTypeAttr), new () { Offset = 16, Type = DataType.uint32}},
            { nameof (Data.EEClass.NumMethods), new () { Offset = 20, Type = DataType.uint16}},
        }
    };

    private static readonly (DataType Type, Target.TypeInfo Info)[] RTSTypes =
    [
        (DataType.MethodTable, MethodTableTypeInfo),
        (DataType.EEClass, EEClassTypeInfo),
    ];


    private static readonly (string Name, ulong Value, string? Type)[] RTSGlobals =
    [
        (nameof(Constants.Globals.FreeObjectMethodTable), TestFreeObjectMethodTableGlobalAddress, null),
    ];

    private static MockMemorySpace.Builder AddFreeObjectMethodTable(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder)
    {
        MockMemorySpace.HeapFragment globalAddr = new() { Name = "Address of Free Object Method Table", Address = TestFreeObjectMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
        targetTestHelpers.WritePointer(globalAddr.Data, TestFreeObjectMethodTableAddress);
        return builder.AddHeapFragments([
            globalAddr,
            new () { Name = "Free Object Method Table", Address = TestFreeObjectMethodTableAddress, Data = new byte[targetTestHelpers.SizeOfTypeInfo(MethodTableTypeInfo)] }
        ]);
    }

    private static MockMemorySpace.Builder AddEEClass(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer eeClassPtr, string name, TargetPointer canonMTPtr, uint attr, ushort numMethods)
    {
        MockMemorySpace.HeapFragment eeClassFragment = new() { Name = $"EEClass '{name}'", Address = eeClassPtr, Data = new byte[targetTestHelpers.SizeOfTypeInfo(EEClassTypeInfo)] };
        Span<byte> dest = eeClassFragment.Data;
        targetTestHelpers.WritePointer(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset), canonMTPtr);
        targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
        targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
        return builder.AddHeapFragment(eeClassFragment);

    }

    private static MockMemorySpace.Builder AddMethodTable(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer methodTablePtr, string name, TargetPointer eeClassOrCanonMT, uint mtflags, uint mtflags2, uint baseSize,
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
            Contracts.MethodTableHandle handle = metadataContract.GetMethodTableHandle(TestFreeObjectMethodTableAddress);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
            Assert.True(metadataContract.IsFreeObjectMethodTable(handle));
        });
    }

    private static MockMemorySpace.Builder AddSystemObject(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer systemObjectMethodTablePtr, TargetPointer systemObjectEEClassPtr)
    {
        System.Reflection.TypeAttributes typeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class;
        const int numMethods = 8; // System.Object has 8 methods
        const int numVirtuals = 3; // System.Object has 3 virtual methods
        builder = AddEEClass(targetTestHelpers, builder, systemObjectEEClassPtr, "System.Object", systemObjectMethodTablePtr, attr: (uint)typeAttributes, numMethods: numMethods);
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
            Contracts.MethodTableHandle systemObjectMethodTableHandle = metadataContract.GetMethodTableHandle(systemObjectMethodTablePtr);
            Assert.Equal(systemObjectMethodTablePtr.Value, systemObjectMethodTableHandle.Address.Value);
            Assert.False(metadataContract.IsFreeObjectMethodTable(systemObjectMethodTableHandle));
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
            builder = AddEEClass(targetTestHelpers, builder, systemStringEEClassPtr, "System.String", systemStringMethodTablePtr, attr: (uint)typeAttributes, numMethods: numMethods);
            builder = AddMethodTable(targetTestHelpers, builder, systemStringMethodTablePtr, "System.String", systemStringEEClassPtr,
                                    mtflags: mtflags, mtflags2: default, baseSize: targetTestHelpers.StringBaseSize,
                                    module: TargetPointer.Null, parentMethodTable: systemObjectMethodTablePtr, numInterfaces: numInterfaces, numVirtuals: numVirtuals);
            return builder;
        },
        (target) =>
        {
            Contracts.IRuntimeTypeSystem metadataContract = target.Contracts.RuntimeTypeSystem;
            Assert.NotNull(metadataContract);
            Contracts.MethodTableHandle systemStringMethodTableHandle = metadataContract.GetMethodTableHandle(systemStringMethodTablePtr);
            Assert.Equal(systemStringMethodTablePtr.Value, systemStringMethodTableHandle.Address.Value);
            Assert.False(metadataContract.IsFreeObjectMethodTable(systemStringMethodTableHandle));
            Assert.True(metadataContract.IsString(systemStringMethodTableHandle));
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
            Assert.Throws<InvalidOperationException>(() => metadataContract.GetMethodTableHandle(badMethodTablePtr));
        });
    }
}
