// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class ComWrappersTests
{
    public enum ObjectKind
    {
        Ordinary,
        DirectSubclass,
        IndirectSubclass,
        BaseTypeNotLoaded
    }

    public static IEnumerable<object[]> RcwCases()
    {
        foreach (object[] architecture in new MockTarget.StdArch())
        {
            foreach (string version in new[] { "c1", "c2" })
            {
                foreach (ObjectKind kind in Enum.GetValues<ObjectKind>())
                {
                    yield return [architecture[0], version, kind, false];
                    yield return [architecture[0], version, kind, true];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(RcwCases))]
    public void GetComWrappersRCWForObject_UsesRegistrationStorage(MockTarget.Architecture arch, string version, ObjectKind kind, bool registered)
    {
        const ulong ObjectAddress = 0x10000;
        const ulong FieldWrapperAddress = 0x20000;
        const ulong TableWrapperAddress = 0x30000;
        bool usesField = version == "c2" && kind is ObjectKind.DirectSubclass or ObjectKind.IndirectSubclass;

        IComWrappers contract = CreateContract(arch, version, kind, registered, out Mock<IConditionalWeakTable> table);

        TargetPointer wrapper = contract.GetComWrappersRCWForObject(ObjectAddress);

        Assert.Equal(registered ? new TargetPointer(usesField ? FieldWrapperAddress : TableWrapperAddress) : TargetPointer.Null, wrapper);
        table.Verify(t => t.TryGetValue(It.IsAny<TargetPointer>(), It.IsAny<TargetPointer>(), out It.Ref<TargetPointer>.IsAny),
            usesField ? Times.Never() : Times.Once());
    }

    private static IComWrappers CreateContract(MockTarget.Architecture arch, string version, ObjectKind kind, bool registered, out Mock<IConditionalWeakTable> table)
    {
        const ulong BaseMethodTable = 0x1000;
        const ulong DirectMethodTable = 0x2000;
        const ulong IndirectMethodTable = 0x3000;
        const ulong OrdinaryMethodTable = 0x4000;
        const ulong ObjectMethodTable = 0x5000;
        const ulong ObjectAddress = 0x10000;
        const ulong FieldWrapperAddress = 0x20000;
        const ulong TableWrapperAddress = 0x30000;
        const ulong TableFieldAddress = 0x40000;
        const ulong TableAddress = 0x50000;

        var builder = new TestPlaceholderTarget.Builder(arch);
        TargetTestHelpers helpers = builder.MemoryBuilder.TargetTestHelpers;
        TargetPointer methodTable = kind switch
        {
            ObjectKind.DirectSubclass => DirectMethodTable,
            ObjectKind.IndirectSubclass => IndirectMethodTable,
            _ => OrdinaryMethodTable
        };

        byte[] objectData = new byte[2 * helpers.PointerSize];
        helpers.WritePointer(objectData, methodTable.Value);
        helpers.WritePointer(objectData.AsSpan(helpers.PointerSize), registered ? FieldWrapperAddress : 0);
        builder.MemoryBuilder.AddHeapFragment(new() { Address = ObjectAddress, Data = objectData });

        byte[] staticData = new byte[helpers.PointerSize];
        helpers.WritePointer(staticData, TableAddress);
        builder.MemoryBuilder.AddHeapFragment(new() { Address = TableFieldAddress, Data = staticData });

        builder.AddTypes(new Dictionary<string, Target.TypeInfo>
        {
            [Data.ComWrappersObject.ManagedTypeName] = new()
            {
                Fields = new Dictionary<string, Target.FieldInfo>
                {
                    ["_nativeObjectWrapper"] = new() { Offset = helpers.PointerSize, TypeName = "pointer" }
                }
            }
        });
        builder.AddGlobals(("System.Runtime.InteropServices.ComWrappers.s_nativeObjectWrapperTable", TableFieldAddress));

        var objects = new Mock<IObject>();
        objects.Setup(o => o.GetMethodTableAddress(new TargetPointer(ObjectAddress))).Returns(methodTable);

        var parents = new Dictionary<TargetPointer, TargetPointer>
        {
            [DirectMethodTable] = BaseMethodTable,
            [IndirectMethodTable] = DirectMethodTable,
            [BaseMethodTable] = ObjectMethodTable,
            [OrdinaryMethodTable] = ObjectMethodTable,
            [ObjectMethodTable] = TargetPointer.Null
        };
        var types = new Mock<IRuntimeTypeSystem>();
        types.Setup(t => t.GetTypeHandle(It.IsAny<TargetPointer>()))
            .Returns((TargetPointer address) => Mock.Of<ITypeHandle>(h => h.Address == address));
        types.Setup(t => t.GetParentMethodTable(It.IsAny<ITypeHandle>()))
            .Returns((ITypeHandle handle) => parents[handle.Address]);

        var managedTypes = new Mock<IManagedTypeSource>();
        ITypeHandle? baseType = kind == ObjectKind.BaseTypeNotLoaded
            ? null
            : Mock.Of<ITypeHandle>(h => h.Address == new TargetPointer(BaseMethodTable));
        managedTypes.Setup(t => t.TryGetTypeHandle(Data.ComWrappersObject.ManagedTypeName, out baseType))
            .Returns(baseType is not null);

        table = new Mock<IConditionalWeakTable>();
        TargetPointer tableWrapper = registered ? TableWrapperAddress : TargetPointer.Null;
        table.Setup(t => t.TryGetValue(new TargetPointer(TableAddress), new TargetPointer(ObjectAddress), out tableWrapper))
            .Returns(registered);

        return builder
            .AddContract<IComWrappers>(version)
            .AddMockContract(objects)
            .AddMockContract(types)
            .AddMockContract(managedTypes)
            .AddMockContract(table)
            .Build().Contracts.ComWrappers;
    }
}
