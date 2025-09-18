// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

public class HashMapTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetValue(MockTarget.Architecture arch)
    {
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockDescriptors.HashMap hashMap = new(builder);
        (TargetPointer Key, TargetPointer Value)[] entries =
        [
            (0x100, 0x10),
            (0x200, 0x20),
            (0x300, 0x30),
            (0x400, 0x40),
        ];
        TargetPointer mapAddress = hashMap.CreateMap(entries);
        TargetPointer ptrMapAddress = hashMap.CreatePtrMap(entries);

        Target target = new TestPlaceholderTarget(builder.TargetTestHelpers.Arch, builder.GetMemoryContext().ReadFromTarget, hashMap.Types, hashMap.Globals);

        var lookup = HashMapLookup.Create(target);
        var ptrLookup = PtrHashMapLookup.Create(target);
        foreach (var entry in entries)
        {
            TargetPointer value = lookup.GetValue(mapAddress, entry.Key);
            Assert.Equal(entry.Value, value);

            TargetPointer ptrValue = ptrLookup.GetValue(ptrMapAddress, entry.Key);
            Assert.Equal(entry.Value, ptrValue);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetValue_Collision(MockTarget.Architecture arch)
    {
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockDescriptors.HashMap hashMap = new(builder);

        // Keys are chosen to result in a collision based on HashMapLookup.HashFunction and the size
        // of the map (based on the number of entries - see MockDescriptors.HashMap.PopulateMap).
        // They result in the same seed and there are more entries than HashMapSlotsPerBucket
        (TargetPointer Key, TargetPointer Value) firstEntryDuplicateKey = (0x04, 0x40);
        (TargetPointer Key, TargetPointer Value)[] entries =
        [
            firstEntryDuplicateKey,
            (0x04, 0x41),
            (0x05, 0x50),
            (0x06, 0x60),
            (0x07, 0x70),
        ];
        TargetPointer mapAddress = hashMap.CreateMap(entries);
        TargetPointer ptrMapAddress = hashMap.CreatePtrMap(entries);

        Target target = new TestPlaceholderTarget(builder.TargetTestHelpers.Arch, builder.GetMemoryContext().ReadFromTarget, hashMap.Types, hashMap.Globals);

        var lookup = HashMapLookup.Create(target);
        var ptrLookup = PtrHashMapLookup.Create(target);
        foreach (var entry in entries)
        {
            TargetPointer expectedValue = entry.Key == firstEntryDuplicateKey.Key ? firstEntryDuplicateKey.Value : entry.Value;
            TargetPointer value = lookup.GetValue(mapAddress, entry.Key);
            Assert.Equal(expectedValue, value);

            TargetPointer ptrValue = ptrLookup.GetValue(ptrMapAddress, entry.Key);
            Assert.Equal(expectedValue, ptrValue);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetValue_NoMatch(MockTarget.Architecture arch)
    {
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockDescriptors.HashMap hashMap = new(builder);
        (TargetPointer Key, TargetPointer Value)[] entries = [(0x100, 0x010)];
        TargetPointer mapAddress = hashMap.CreateMap(entries);
        TargetPointer ptrMapAddress = hashMap.CreatePtrMap(entries);

        Target target = new TestPlaceholderTarget(builder.TargetTestHelpers.Arch, builder.GetMemoryContext().ReadFromTarget, hashMap.Types, hashMap.Globals);

        {
            var lookup = HashMapLookup.Create(target);
            TargetPointer value = lookup.GetValue(mapAddress, 0x101);
            Assert.Equal((uint)HashMapLookup.SpecialKeys.InvalidEntry, value);
        }
        {
            var lookup = PtrHashMapLookup.Create(target);
            TargetPointer value = lookup.GetValue(ptrMapAddress, 0x101);
            Assert.Equal((uint)HashMapLookup.SpecialKeys.InvalidEntry, value);
        }
    }
}
