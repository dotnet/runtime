// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

using NibbleMap = Contracts.ExecutionManager_1.NibbleMap;

public class NibbleMapTests
{
    internal class NibbleMapTestBuilder
    {
        // This is the base address of the memory range that the map covers.
        // The map works on code pointers as offsets from this address
        // For testing we don't actually place anything into this space
        private readonly TargetPointer MapBase;


        private readonly MockTarget.Architecture Arch;
        // this is the target memory representation of the nibble map itself
        public readonly MockMemorySpace.HeapFragment NibbleMapFragment;

        public NibbleMapTestBuilder(TargetPointer mapBase, TargetPointer mapStart, MockTarget.Architecture arch)
        {
            MapBase = mapBase;
            Arch = arch;
            const uint NibbleMapSize = 0x1000;
            NibbleMapFragment = new MockMemorySpace.HeapFragment {
                Address = mapStart,
                Data = new byte[NibbleMapSize],
                Name = "Nibble Map",
            };
        }

        public void AllocateCodeChunk(TargetCodePointer codeStart, int codeSize)
        {
            throw new NotImplementedException("TODO");
        }

        public void Create()
        {
            // write stuff into the map
            throw new NotImplementedException();
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestNibbleMapOneItem(MockTarget.Architecture arch)
    {
        // SETUP:

        TestPlaceholderTarget target = new(arch);
        // this is the beginning of the address range where code pointers might point
        TargetPointer mapBase = new(0x5f5f_0000u);
        // this is the beginning of the nibble map itself
        TargetPointer mapStart = new(0x0456_1000u);
        NibbleMapTestBuilder builder = new(mapBase, mapStart, arch);

        TargetCodePointer inputPC = new(0x5f5f_0030u);
        int codeSize = 0x80;
        builder.AllocateCodeChunk (inputPC, codeSize); // 128 bytes
        builder.Create();

        // TODO: some kind of memory in the placeholder target
        //target.AddHeapFragment(builder.NibbleMapFragment);

        // TESTCASE:

        NibbleMap map = NibbleMap.Create(target);
        Assert.NotNull(map);

        TargetPointer methodCode = map.FindMethodCode(mapBase, mapStart, inputPC);
        Assert.Equal(inputPC.Value, methodCode.Value);

        for (int i = 0; i < codeSize; i++)
        {
            methodCode = map.FindMethodCode(mapBase, mapStart, inputPC.Value + (uint)i);
            // we should always find the beginning of the method
            Assert.Equal(inputPC.Value, methodCode.Value);
        }

    }
}
