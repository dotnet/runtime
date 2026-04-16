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
    private sealed class MockAuxiliarySymbolInfo : TypedView
    {
        private const string AddressFieldName = "Address";
        private const string NameFieldName = "Name";

        public static Layout<MockAuxiliarySymbolInfo> CreateLayout(MockTarget.Architecture architecture)
            => new SequentialLayoutBuilder("AuxiliarySymbolInfo", architecture)
                .AddPointerField(AddressFieldName)
                .AddPointerField(NameFieldName)
                .Build<MockAuxiliarySymbolInfo>();

        public ulong AddressValue
        {
            get => ReadPointerField(AddressFieldName);
            set => WritePointerField(AddressFieldName, value);
        }

        public ulong Name
        {
            get => ReadPointerField(NameFieldName);
            set => WritePointerField(NameFieldName, value);
        }
    }

    private static Target CreateTarget(
        MockTarget.Architecture arch,
        (ulong Address, string Name)[] helpers)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockMemorySpace.Builder builder = targetBuilder.MemoryBuilder;
        TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
        Layout<MockAuxiliarySymbolInfo> auxiliarySymbolInfoLayout = MockAuxiliarySymbolInfo.CreateLayout(arch);

        Dictionary<DataType, Target.TypeInfo> types = new()
        {
            [DataType.AuxiliarySymbolInfo] = TargetTestHelpers.CreateTypeInfo(auxiliarySymbolInfoLayout),
        };

        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x1000_0000, 0x2000_0000);

        ulong arrayAddress = 0;
        if (helpers.Length > 0)
        {
            // Allocate the array (only if non-empty)
            MockMemorySpace.HeapFragment arrayFragment =
                allocator.Allocate((ulong)(auxiliarySymbolInfoLayout.Size * helpers.Length), "AuxiliarySymbolInfoArray");
            // Write each entry
            for (int i = 0; i < helpers.Length; i++)
            {
                ulong entryAddress = arrayFragment.Address + (ulong)(i * auxiliarySymbolInfoLayout.Size);
                Memory<byte> entryMemory =
                    arrayFragment.Data.AsMemory(i * auxiliarySymbolInfoLayout.Size, auxiliarySymbolInfoLayout.Size);
                MockAuxiliarySymbolInfo entry = auxiliarySymbolInfoLayout.Create(entryMemory, entryAddress);

                // Write the code pointer address
                entry.AddressValue = helpers[i].Address;

                // Allocate and write the UTF-8 name string
                byte[] nameBytes = Encoding.UTF8.GetBytes(helpers[i].Name + '\0');
                MockMemorySpace.HeapFragment nameFragment = allocator.Allocate((ulong)nameBytes.Length, $"Name_{helpers[i].Name}");
                nameBytes.CopyTo(nameFragment.Data.AsSpan());
                entry.Name = nameFragment.Address;
            }

            arrayAddress = arrayFragment.Address;
        }

        // Allocate global for the count
        MockMemorySpace.HeapFragment countFragment = allocator.Allocate(sizeof(uint), "HelperCount");
        targetTestHelpers.Write(countFragment.Data, (uint)helpers.Length);

        return targetBuilder
            .AddTypes(types)
            .AddGlobals(
                (Constants.Globals.AuxiliarySymbols, arrayAddress),
                (Constants.Globals.AuxiliarySymbolCount, countFragment.Address))
            .AddMockContract<IPlatformMetadata>(Mock.Of<IPlatformMetadata>(p => p.GetCodePointerFlags() == default(CodePointerFlags)))
            .AddContract<IAuxiliarySymbols>(version: 1)
            .Build();
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetAuxiliarySymbolName_MatchFound_ReturnsTrue(MockTarget.Architecture arch)
    {
        ulong writeBarrierAddr = 0x7FFF_0100;
        ulong checkedBarrierAddr = 0x7FFF_0200;

        var target = CreateTarget(arch,
        [
            (writeBarrierAddr, "@WriteBarrier"),
            (checkedBarrierAddr, "@CheckedWriteBarrier"),
        ]);

        bool found = target.Contracts.AuxiliarySymbols.TryGetAuxiliarySymbolName(
            new TargetPointer(writeBarrierAddr), out string? name);

        Assert.True(found);
        Assert.Equal("@WriteBarrier", name);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetAuxiliarySymbolName_NoMatch_ReturnsFalse(MockTarget.Architecture arch)
    {
        ulong writeBarrierAddr = 0x7FFF_0100;

        var target = CreateTarget(arch,
        [
            (writeBarrierAddr, "@WriteBarrier"),
        ]);

        bool found = target.Contracts.AuxiliarySymbols.TryGetAuxiliarySymbolName(
            new TargetPointer(0xDEAD_BEEF), out string? name);

        Assert.False(found);
        Assert.Null(name);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetAuxiliarySymbolName_EmptyArray_ReturnsFalse(MockTarget.Architecture arch)
    {
        var target = CreateTarget(arch, []);

        bool found = target.Contracts.AuxiliarySymbols.TryGetAuxiliarySymbolName(
            new TargetPointer(0x7FFF_0100), out string? name);

        Assert.False(found);
        Assert.Null(name);
    }
}
