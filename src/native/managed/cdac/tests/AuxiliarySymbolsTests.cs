// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class AuxiliarySymbolsTests
{
    private static readonly MockDescriptors.TypeFields JitHelperInfoFields = new()
    {
        DataType = DataType.JitHelperInfo,
        Fields =
        [
            new(nameof(Data.JitHelperInfo.Address), DataType.pointer),
            new(nameof(Data.JitHelperInfo.Name), DataType.pointer),
        ]
    };

    private static Target CreateTarget(
        MockTarget.Architecture arch,
        (ulong Address, string Name)[] helpers)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        MockMemorySpace.Builder builder = new(targetTestHelpers);

        Dictionary<DataType, Target.TypeInfo> types =
            MockDescriptors.GetTypesForTypeFields(targetTestHelpers, [JitHelperInfoFields]);
        uint entrySize = types[DataType.JitHelperInfo].Size!.Value;

        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x1000_0000, 0x2000_0000);

        // Allocate the array
        MockMemorySpace.HeapFragment arrayFragment = allocator.Allocate(entrySize * (ulong)helpers.Length, "JitHelperInfoArray");

        // Write each entry
        Target.TypeInfo typeInfo = types[DataType.JitHelperInfo];
        for (int i = 0; i < helpers.Length; i++)
        {
            int addressOffset = typeInfo.Fields[nameof(Data.JitHelperInfo.Address)].Offset;
            int nameOffset = typeInfo.Fields[nameof(Data.JitHelperInfo.Name)].Offset;
            Span<byte> entryData = arrayFragment.Data.AsSpan((int)(i * entrySize), (int)entrySize);

            // Write the code pointer address
            targetTestHelpers.WritePointer(entryData.Slice(addressOffset), helpers[i].Address);

            // Allocate and write the UTF-16 name string
            byte[] nameBytes = (arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode).GetBytes(helpers[i].Name + '\0');
            MockMemorySpace.HeapFragment nameFragment = allocator.Allocate((ulong)nameBytes.Length, $"Name_{helpers[i].Name}");
            nameBytes.CopyTo(nameFragment.Data.AsSpan());
            builder.AddHeapFragment(nameFragment);

            targetTestHelpers.WritePointer(entryData.Slice(nameOffset), nameFragment.Address);
        }
        builder.AddHeapFragment(arrayFragment);

        // Allocate global for the count
        MockMemorySpace.HeapFragment countFragment = allocator.Allocate(sizeof(int), "HelperCount");
        targetTestHelpers.Write(countFragment.Data, helpers.Length);
        builder.AddHeapFragment(countFragment);

        (string Name, ulong Value)[] globals =
        [
            (Constants.Globals.InterestingJitHelpers, arrayFragment.Address),
            (Constants.Globals.InterestingJitHelperCount, countFragment.Address),
        ];

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types, globals);

        Mock<IPlatformMetadata> platformMetadata = new();
        platformMetadata.Setup(p => p.GetCodePointerFlags()).Returns(default(CodePointerFlags));

        IContractFactory<IAuxiliarySymbols> factory = new AuxiliarySymbolsFactory();
        Mock<ContractRegistry> reg = new();
        reg.SetupGet(c => c.PlatformMetadata).Returns(platformMetadata.Object);
        reg.SetupGet(c => c.AuxiliarySymbols).Returns(() => factory.CreateContract(target, 1));
        target.SetContracts(reg.Object);

        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetJitHelperName_MatchFound_ReturnsTrue(MockTarget.Architecture arch)
    {
        ulong writeBarrierAddr = 0x7FFF_0100;
        ulong checkedBarrierAddr = 0x7FFF_0200;

        var target = CreateTarget(arch,
        [
            (writeBarrierAddr, "@WriteBarrier"),
            (checkedBarrierAddr, "@CheckedWriteBarrier"),
        ]);

        bool found = target.Contracts.AuxiliarySymbols.TryGetJitHelperName(
            new TargetPointer(writeBarrierAddr), out string? name);

        Assert.True(found);
        Assert.Equal("@WriteBarrier", name);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetJitHelperName_NoMatch_ReturnsFalse(MockTarget.Architecture arch)
    {
        ulong writeBarrierAddr = 0x7FFF_0100;

        var target = CreateTarget(arch,
        [
            (writeBarrierAddr, "@WriteBarrier"),
        ]);

        bool found = target.Contracts.AuxiliarySymbols.TryGetJitHelperName(
            new TargetPointer(0xDEAD_BEEF), out string? name);

        Assert.False(found);
        Assert.Null(name);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetJitHelperName_EmptyArray_ReturnsFalse(MockTarget.Architecture arch)
    {
        var target = CreateTarget(arch, []);

        bool found = target.Contracts.AuxiliarySymbols.TryGetJitHelperName(
            new TargetPointer(0x7FFF_0100), out string? name);

        Assert.False(found);
        Assert.Null(name);
    }
}
