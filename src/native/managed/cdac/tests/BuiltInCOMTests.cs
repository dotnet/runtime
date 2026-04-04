// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class BuiltInCOMTests
{
    private const ulong AllocationRangeStart = 0x00000000_20000000;
    private const ulong AllocationRangeEnd   = 0x00000000_30000000;

    private const uint TestRCWInterfaceCacheSize = 8;

    private static readonly MockDescriptors.TypeFields RCWFields = new MockDescriptors.TypeFields()
    {
        DataType = DataType.RCW,
        Fields =
        [
            new(nameof(Data.RCW.NextCleanupBucket), DataType.pointer),
            new(nameof(Data.RCW.NextRCW), DataType.pointer),
            new(nameof(Data.RCW.Flags), DataType.uint32),
            new(nameof(Data.RCW.CtxCookie), DataType.pointer),
            new(nameof(Data.RCW.CtxEntry), DataType.pointer),
            new(nameof(Data.RCW.InterfaceEntries), DataType.pointer),
            new(nameof(Data.RCW.IdentityPointer), DataType.pointer),
            new(nameof(Data.RCW.SyncBlockIndex), DataType.uint32),
            new(nameof(Data.RCW.VTablePtr), DataType.pointer),
            new(nameof(Data.RCW.CreatorThread), DataType.pointer),
            new(nameof(Data.RCW.RefCount), DataType.uint32),
            new(nameof(Data.RCW.UnknownPointer), DataType.pointer),
        ]
    };

    private static readonly MockDescriptors.TypeFields InterfaceEntryFields = new MockDescriptors.TypeFields()
    {
        DataType = DataType.InterfaceEntry,
        Fields =
        [
            new(nameof(Data.InterfaceEntry.MethodTable), DataType.pointer),
            new(nameof(Data.InterfaceEntry.Unknown), DataType.pointer),
        ]
    };

    private static readonly MockDescriptors.TypeFields CtxEntryFields = new MockDescriptors.TypeFields()
    {
        DataType = DataType.CtxEntry,
        Fields =
        [
            new(nameof(Data.CtxEntry.STAThread), DataType.pointer),
            new(nameof(Data.CtxEntry.CtxCookie), DataType.pointer),
        ]
    };

    private static void BuiltInCOMContractHelper(
        MockTarget.Architecture arch,
        Action<MockMemorySpace.Builder, TargetTestHelpers, Dictionary<DataType, Target.TypeInfo>> configure,
        Action<Target> testCase,
        ISyncBlock? syncBlock = null)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        MockMemorySpace.Builder builder = new(targetTestHelpers);

        Dictionary<DataType, Target.TypeInfo> types = MockDescriptors.GetTypesForTypeFields(
            targetTestHelpers,
            [RCWFields, InterfaceEntryFields, CtxEntryFields]);

        configure(builder, targetTestHelpers, types);

        (string Name, ulong Value)[] globals =
        [
            (nameof(Constants.Globals.RCWInterfaceCacheSize), TestRCWInterfaceCacheSize),
        ];

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types, globals);
        ISyncBlock syncBlockContract = syncBlock ?? Mock.Of<ISyncBlock>();
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.BuiltInCOM == ((IContractFactory<IBuiltInCOM>)new BuiltInCOMFactory()).CreateContract(target, 1)
              && c.SyncBlock == syncBlockContract));

        testCase(target);
    }

    /// <summary>
    /// Allocates an RCW mock with the interface entries embedded inline (matching the real C++ layout
    /// where m_aInterfaceEntries is an inline array within the RCW struct).
    /// Returns the address of the RCW.
    /// </summary>
    private static TargetPointer AddRCWWithInlineEntries(
        MockMemorySpace.Builder builder,
        TargetTestHelpers targetTestHelpers,
        Dictionary<DataType, Target.TypeInfo> types,
        MockMemorySpace.BumpAllocator allocator,
        (TargetPointer MethodTable, TargetPointer Unknown)[] entries,
        TargetPointer ctxCookie = default)
    {
        Target.TypeInfo rcwTypeInfo = types[DataType.RCW];
        Target.TypeInfo entryTypeInfo = types[DataType.InterfaceEntry];
        uint entrySize = entryTypeInfo.Size!.Value;
        uint entriesOffset = (uint)rcwTypeInfo.Fields[nameof(Data.RCW.InterfaceEntries)].Offset;

        // The RCW block must be large enough to hold the RCW header plus all inline entries
        uint totalSize = entriesOffset + entrySize * TestRCWInterfaceCacheSize;
        MockMemorySpace.HeapFragment fragment = allocator.Allocate(totalSize, "RCW with inline entries");
        Span<byte> data = fragment.Data;

        // Write RCW header fields
        targetTestHelpers.WritePointer(
            data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.CtxCookie)].Offset),
            ctxCookie);

        // Write the inline interface entries starting at entriesOffset
        for (int i = 0; i < entries.Length && i < TestRCWInterfaceCacheSize; i++)
        {
            Span<byte> entryData = data.Slice((int)(entriesOffset + i * entrySize));
            targetTestHelpers.WritePointer(
                entryData.Slice(entryTypeInfo.Fields[nameof(Data.InterfaceEntry.MethodTable)].Offset),
                entries[i].MethodTable);
            targetTestHelpers.WritePointer(
                entryData.Slice(entryTypeInfo.Fields[nameof(Data.InterfaceEntry.Unknown)].Offset),
                entries[i].Unknown);
        }

        builder.AddHeapFragment(fragment);
        return fragment.Address;
    }

    // Flag values matching the C++ runtime
    private const ulong IsLayoutCompleteFlag = 0x10;

    // LinkedWrapperTerminator: (PTR_ComCallWrapper)-1, all bits set
    private const ulong LinkedWrapperTerminator = ulong.MaxValue;

    private const uint NumVtablePtrs = 5;
    private const ulong ComRefcountMask = 0x000000007FFFFFFF;

    // Addresses of slots that hold the tear-off function pointers (the globals are indirect pointers)
    private const ulong TearOffAddRefSlot        = 0xE000_0000;
    private const ulong TearOffAddRefSimpleSlot  = 0xE000_0100;
    private const ulong TearOffAddRefSimpleInnerSlot = 0xE000_0200;

    // Fake tear-off function addresses used in tests (must not collide with real data values)
    private const ulong TearOffAddRefAddr        = 0xF000_0001;
    private const ulong TearOffAddRefSimpleAddr  = 0xF000_0002;
    private const ulong TearOffAddRefSimpleInnerAddr = 0xF000_0003;

    // CCWThisMask: ~0x3f on 64-bit, ~0x1f on 32-bit (matches enum_ThisMask in ComCallWrapper)
    private static ulong GetCCWThisMask(int pointerSize) => pointerSize == 8 ? ~0x3FUL : ~0x1FUL;

    private const ulong AllocationStart = 0x0001_0000;
    private const ulong AllocationEnd   = 0x0002_0000;

    /// <summary>
    /// Creates type infos for ComCallWrapper, SimpleComCallWrapper, and ComMethodTable
    /// using a simplified in-test layout:
    ///   ComCallWrapper:       SimpleWrapper at 0, IPtr (=m_rgpIPtr[0]) at P, Next at 6P
    ///   SimpleComCallWrapper: RefCount (uint64) at 0, Flags (uint32) at 8,
    ///                         MainWrapper (pointer) at 12, VTablePtr (pointer) at 12+P
    ///   ComCallWrapper:       SimpleWrapper (pointer) at 0, IPtr (pointer) at P, Next (pointer) at 6P, Handle (pointer) at 7P
    ///   SimpleComCallWrapper: RefCount (uint64) at 0, Flags (uint32) at 8, MainWrapper (pointer) at 12,
    ///                          VTablePtr (pointer) at 12+P, OuterIUnknown (pointer) at 12+2P
    ///   ComMethodTable:       Flags (nuint) at 0, MethodTable (pointer) at P, size = 2P
    /// </summary>
    private static Dictionary<DataType, Target.TypeInfo> CreateTypeInfos(int pointerSize)
    {
        int P = pointerSize;
        return new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.ComCallWrapper] = new Target.TypeInfo()
            {
                Fields = new Dictionary<string, Target.FieldInfo>()
                {
                    [nameof(Data.ComCallWrapper.SimpleWrapper)] = new Target.FieldInfo { Offset = 0, Type = DataType.pointer },
                    [nameof(Data.ComCallWrapper.IPtr)] = new Target.FieldInfo { Offset = P, Type = DataType.pointer },
                    [nameof(Data.ComCallWrapper.Next)] = new Target.FieldInfo { Offset = 6 * P, Type = DataType.pointer },
                    [nameof(Data.ComCallWrapper.Handle)] = new Target.FieldInfo { Offset = 7 * P, Type = DataType.pointer },
                }
            },
            [DataType.SimpleComCallWrapper] = new Target.TypeInfo()
            {
                Fields = new Dictionary<string, Target.FieldInfo>()
                {
                    [nameof(Data.SimpleComCallWrapper.RefCount)] = new Target.FieldInfo { Offset = 0, Type = DataType.uint64 },
                    [nameof(Data.SimpleComCallWrapper.Flags)] = new Target.FieldInfo { Offset = 8, Type = DataType.uint32 },
                    [nameof(Data.SimpleComCallWrapper.MainWrapper)] = new Target.FieldInfo { Offset = 12, Type = DataType.pointer },
                    [nameof(Data.SimpleComCallWrapper.VTablePtr)] = new Target.FieldInfo { Offset = 12 + P, Type = DataType.pointer },
                    [nameof(Data.SimpleComCallWrapper.OuterIUnknown)] = new Target.FieldInfo { Offset = 12 + 2 * P, Type = DataType.pointer },
                }
            },
            [DataType.ComMethodTable] = new Target.TypeInfo()
            {
                Size = (uint)(2 * P),
                Fields = new Dictionary<string, Target.FieldInfo>()
                {
                    [nameof(Data.ComMethodTable.Flags)] = new Target.FieldInfo { Offset = 0, Type = DataType.nuint },
                    [nameof(Data.ComMethodTable.MethodTable)] = new Target.FieldInfo { Offset = P, Type = DataType.pointer },
                }
            },
        };
    }

    private static Target CreateTarget(MockTarget.Architecture arch, MockMemorySpace.Builder builder, Dictionary<DataType, Target.TypeInfo> types)
    {
        int P = arch.Is64Bit ? 8 : 4;

        // The TearOff globals are indirect pointers: the global stores a pointer to a slot,
        // and the slot contains the actual function address.
        void AddIndirectPointer(MockMemorySpace.Builder b, TargetTestHelpers h, ulong slotAddr, ulong value)
        {
            var frag = new MockMemorySpace.HeapFragment
            {
                Name = $"TearOffSlot@0x{slotAddr:X}",
                Address = slotAddr,
                Data = new byte[h.PointerSize],
            };
            h.WritePointer(frag.Data, value);
            b.AddHeapFragment(frag);
        }

        var helpers = new TargetTestHelpers(arch);
        AddIndirectPointer(builder, helpers, TearOffAddRefSlot, TearOffAddRefAddr);
        AddIndirectPointer(builder, helpers, TearOffAddRefSimpleSlot, TearOffAddRefSimpleAddr);
        AddIndirectPointer(builder, helpers, TearOffAddRefSimpleInnerSlot, TearOffAddRefSimpleInnerAddr);

        (string Name, ulong Value)[] globals =
        [
            (Constants.Globals.CCWNumInterfaces, NumVtablePtrs),
            (Constants.Globals.CCWThisMask, GetCCWThisMask(P)),
            (Constants.Globals.TearOffAddRef, TearOffAddRefSlot),
            (Constants.Globals.TearOffAddRefSimple, TearOffAddRefSimpleSlot),
            (Constants.Globals.TearOffAddRefSimpleInner, TearOffAddRefSimpleInnerSlot),
        ];
        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types, globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.BuiltInCOM == ((IContractFactory<IBuiltInCOM>)new BuiltInCOMFactory()).CreateContract(target, 1)));
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_RefCount_ReturnsMaskedValue(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        // Raw refcount has CLEANUP_SENTINEL (bit 31) set plus a visible count of 0x1234_5678.
        ulong rawRefCount = 0x0000_0000_1234_5678UL | 0x80000000UL;
        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.Write(simpleWrapperFragment.Data.AsSpan(0, 8), rawRefCount);
        ulong ccwAddr = 0x4000;
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;
        SimpleComCallWrapperData data = builtInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        // RefCount should have the CLEANUP_SENTINEL bit stripped by the contract.
        Assert.Equal(rawRefCount & ComRefcountMask, data.RefCount);
        Assert.True(data.IsNeutered);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_IsHandleWeak_FlagSet_ReturnsTrue(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        const uint IsHandleWeakFlag = 0x4;
        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.Write(simpleWrapperFragment.Data.AsSpan(8, 4), IsHandleWeakFlag);
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;
        SimpleComCallWrapperData data = builtInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        Assert.True(data.IsHandleWeak);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_IsHandleWeak_FlagNotSet_ReturnsFalse(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;
        SimpleComCallWrapperData data = builtInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        Assert.False(data.IsHandleWeak);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_IsNeutered_SentinelBitSet_ReturnsTrue(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;
        ulong rawRefCount = 0x80000000UL; // CLEANUP_SENTINEL bit set

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.Write(simpleWrapperFragment.Data.AsSpan(0, 8), rawRefCount);
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;
        SimpleComCallWrapperData data = builtInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        Assert.True(data.IsNeutered);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_IsNeutered_SentinelBitClear_ReturnsFalse(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;
        ulong rawRefCount = 3UL; // non-zero ref count, no sentinel

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.Write(simpleWrapperFragment.Data.AsSpan(0, 8), rawRefCount);
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;
        SimpleComCallWrapperData data = builtInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        Assert.False(data.IsNeutered);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_IsAggregated_FlagSet_ReturnsTrue(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.Write(simpleWrapperFragment.Data.AsSpan(8, 4), (uint)0x1); // IsAggregated flag
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;
        SimpleComCallWrapperData data = builtInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        Assert.True(data.IsAggregated);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_IsAggregated_FlagNotSet_ReturnsFalse(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;
        SimpleComCallWrapperData data = builtInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        Assert.False(data.IsAggregated);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_IsExtendsCOMObject_FlagSet_ReturnsTrue(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.Write(simpleWrapperFragment.Data.AsSpan(8, 4), (uint)0x2); // IsExtendsCom flag
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;
        SimpleComCallWrapperData data = builtInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        Assert.True(data.IsExtendsCOMObject);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_IsExtendsCOMObject_FlagNotSet_ReturnsFalse(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;
        SimpleComCallWrapperData data = builtInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        Assert.False(data.IsExtendsCOMObject);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWInterfaces_SingleWrapper_SkipsNullAndIncompleteSlots(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;
        uint cmtSize = (uint)(2 * P);

        var allocator = builder.CreateAllocator(AllocationStart, AllocationEnd);

        // SimpleComCallWrapper (MainWrapper will point to the CCW below)
        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 3 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        // ComCallWrapper: SimpleWrapper + 5 slots + Next + Handle = 8 pointers
        var ccwFrag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper");
        builder.AddHeapFragment(ccwFrag);

        // Write sccw.MainWrapper = ccwFrag.Address (start wrapper)
        Span<byte> sccwData = builder.BorrowAddressRange(simpleWrapperFrag.Address, 12 + 2 * P);
        helpers.WritePointer(sccwData.Slice(12, P), ccwFrag.Address);

        // Slot 0: IUnknown (layout complete; MethodTable is Null per spec for first wrapper's slot 0)
        var cmt0Frag = allocator.Allocate(cmtSize, "ComMethodTable[0]");
        builder.AddHeapFragment(cmt0Frag);
        ulong vtable0 = cmt0Frag.Address + cmtSize;

        // Slot 1: incomplete layout (should be skipped)
        var cmt1Frag = allocator.Allocate(cmtSize, "ComMethodTable[1]");
        builder.AddHeapFragment(cmt1Frag);
        ulong vtable1 = cmt1Frag.Address + cmtSize;

        // Slot 2: layout complete with a MethodTable
        ulong expectedMethodTable2 = 0xdead_0002;
        var cmt2Frag = allocator.Allocate(cmtSize, "ComMethodTable[2]");
        builder.AddHeapFragment(cmt2Frag);
        ulong vtable2 = cmt2Frag.Address + cmtSize;

        // Write CCW
        Span<byte> ccw = builder.BorrowAddressRange(ccwFrag.Address, 7 * P);
        helpers.WritePointer(ccw.Slice(0, P), simpleWrapperFrag.Address); // SimpleWrapper
        helpers.WritePointer(ccw.Slice(1 * P, P), vtable0);               // slot 0
        helpers.WritePointer(ccw.Slice(2 * P, P), vtable1);               // slot 1 (incomplete)
        helpers.WritePointer(ccw.Slice(3 * P, P), vtable2);               // slot 2
        helpers.WritePointer(ccw.Slice(4 * P, P), 0);                     // slot 3 (null)
        helpers.WritePointer(ccw.Slice(5 * P, P), 0);                     // slot 4 (null)
        helpers.WritePointer(ccw.Slice(6 * P, P), LinkedWrapperTerminator); // Next = terminator

        // Write ComMethodTable data
        Span<byte> d0 = builder.BorrowAddressRange(cmt0Frag.Address, (int)cmtSize);
        helpers.WriteNUInt(d0.Slice(0, P), new TargetNUInt(IsLayoutCompleteFlag));
        helpers.WritePointer(d0.Slice(P, P), 0);

        Span<byte> d1 = builder.BorrowAddressRange(cmt1Frag.Address, (int)cmtSize);
        helpers.WriteNUInt(d1.Slice(0, P), new TargetNUInt(0)); // NOT layout complete

        Span<byte> d2 = builder.BorrowAddressRange(cmt2Frag.Address, (int)cmtSize);
        helpers.WriteNUInt(d2.Slice(0, P), new TargetNUInt(IsLayoutCompleteFlag));
        helpers.WritePointer(d2.Slice(P, P), expectedMethodTable2);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));

        List<COMInterfacePointerData> interfaces =
            target.Contracts.BuiltInCOM.GetCCWInterfaces(new TargetPointer(ccwFrag.Address)).ToList();

        // Only slot 0 and slot 2 appear: slot 1 is incomplete, slots 3/4 are null
        Assert.Equal(2, interfaces.Count);

        // Slot 0: IUnknown (first wrapper, index 0) => MethodTable = Null
        Assert.Equal(ccwFrag.Address + (ulong)P, interfaces[0].InterfacePointerAddress.Value);
        Assert.Equal(TargetPointer.Null.Value, interfaces[0].MethodTable.Value);

        // Slot 2: at offset 3*P from CCW base (IPtr + 2*P)
        Assert.Equal(ccwFrag.Address + (ulong)(3 * P), interfaces[1].InterfacePointerAddress.Value);
        Assert.Equal(expectedMethodTable2, interfaces[1].MethodTable.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWInterfaces_MultipleWrappers_WalksChain(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;
        uint cmtSize = (uint)(2 * P);

        var allocator = builder.CreateAllocator(AllocationStart, AllocationEnd);

        // SimpleComCallWrapper (shared by both wrappers; MainWrapper will point to first CCW)
        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 3 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        // First CCW: slot 0 = IUnknown; slots 1-4 = null; Next -> second CCW
        var ccw1Frag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper[1]");
        builder.AddHeapFragment(ccw1Frag);

        var cmt1_0Frag = allocator.Allocate(cmtSize, "ComMethodTable ccw1[0]");
        builder.AddHeapFragment(cmt1_0Frag);
        ulong vtable1_0 = cmt1_0Frag.Address + cmtSize;

        // Second CCW: slot 0 = IClassX, slot 2 = interface; Next = terminator
        var ccw2Frag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper[2]");
        builder.AddHeapFragment(ccw2Frag);

        ulong expectedMT_slot0 = 0xbbbb_0000;
        var cmt2_0Frag = allocator.Allocate(cmtSize, "ComMethodTable ccw2[0]");
        builder.AddHeapFragment(cmt2_0Frag);
        ulong vtable2_0 = cmt2_0Frag.Address + cmtSize;

        ulong expectedMT_slot2 = 0xcccc_0002;
        var cmt2_2Frag = allocator.Allocate(cmtSize, "ComMethodTable ccw2[2]");
        builder.AddHeapFragment(cmt2_2Frag);
        ulong vtable2_2 = cmt2_2Frag.Address + cmtSize;

        // Write sccw.MainWrapper = ccw1Frag.Address (start wrapper)
        Span<byte> sccwData = builder.BorrowAddressRange(simpleWrapperFrag.Address, 12 + 2 * P);
        helpers.WritePointer(sccwData.Slice(12, P), ccw1Frag.Address);

        // Write first CCW
        Span<byte> w1 = builder.BorrowAddressRange(ccw1Frag.Address, 7 * P);
        helpers.WritePointer(w1.Slice(0, P), simpleWrapperFrag.Address); // SimpleWrapper
        helpers.WritePointer(w1.Slice(1 * P, P), vtable1_0);             // slot 0
        helpers.WritePointer(w1.Slice(2 * P, P), 0);                     // slot 1
        helpers.WritePointer(w1.Slice(3 * P, P), 0);                     // slot 2
        helpers.WritePointer(w1.Slice(4 * P, P), 0);                     // slot 3
        helpers.WritePointer(w1.Slice(5 * P, P), 0);                     // slot 4
        helpers.WritePointer(w1.Slice(6 * P, P), ccw2Frag.Address);      // Next -> ccw2

        // Write second CCW
        Span<byte> w2 = builder.BorrowAddressRange(ccw2Frag.Address, 7 * P);
        helpers.WritePointer(w2.Slice(0, P), simpleWrapperFrag.Address); // SimpleWrapper
        helpers.WritePointer(w2.Slice(1 * P, P), vtable2_0);             // slot 0
        helpers.WritePointer(w2.Slice(2 * P, P), 0);                     // slot 1
        helpers.WritePointer(w2.Slice(3 * P, P), vtable2_2);             // slot 2
        helpers.WritePointer(w2.Slice(4 * P, P), 0);                     // slot 3
        helpers.WritePointer(w2.Slice(5 * P, P), 0);                     // slot 4
        helpers.WritePointer(w2.Slice(6 * P, P), LinkedWrapperTerminator); // Next = terminator

        // Write ComMethodTable for first CCW slot 0 (MethodTable unused for first-wrapper slot 0)
        Span<byte> d1_0 = builder.BorrowAddressRange(cmt1_0Frag.Address, (int)cmtSize);
        helpers.WriteNUInt(d1_0.Slice(0, P), new TargetNUInt(IsLayoutCompleteFlag));
        helpers.WritePointer(d1_0.Slice(P, P), 0);

        // Write ComMethodTable for second CCW slot 0
        Span<byte> d2_0 = builder.BorrowAddressRange(cmt2_0Frag.Address, (int)cmtSize);
        helpers.WriteNUInt(d2_0.Slice(0, P), new TargetNUInt(IsLayoutCompleteFlag));
        helpers.WritePointer(d2_0.Slice(P, P), expectedMT_slot0);

        // Write ComMethodTable for second CCW slot 2
        Span<byte> d2_2 = builder.BorrowAddressRange(cmt2_2Frag.Address, (int)cmtSize);
        helpers.WriteNUInt(d2_2.Slice(0, P), new TargetNUInt(IsLayoutCompleteFlag));
        helpers.WritePointer(d2_2.Slice(P, P), expectedMT_slot2);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));

        List<COMInterfacePointerData> interfaces =
            target.Contracts.BuiltInCOM.GetCCWInterfaces(new TargetPointer(ccw1Frag.Address)).ToList();

        // 3 interfaces: ccw1 slot0 (IUnknown), ccw2 slot0 (IClassX), ccw2 slot2 (interface)
        Assert.Equal(3, interfaces.Count);

        // First wrapper, slot 0: IUnknown => MethodTable = Null
        Assert.Equal(ccw1Frag.Address + (ulong)P, interfaces[0].InterfacePointerAddress.Value);
        Assert.Equal(TargetPointer.Null.Value, interfaces[0].MethodTable.Value);

        // Second wrapper, slot 0: IClassX - has a MethodTable (not first wrapper)
        Assert.Equal(ccw2Frag.Address + (ulong)P, interfaces[1].InterfacePointerAddress.Value);
        Assert.Equal(expectedMT_slot0, interfaces[1].MethodTable.Value);

        // Second wrapper, slot 2
        Assert.Equal(ccw2Frag.Address + (ulong)(3 * P), interfaces[2].InterfacePointerAddress.Value);
        Assert.Equal(expectedMT_slot2, interfaces[2].MethodTable.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWInterfaces_LinkedWrapper_WalksFullChainFromAnyWrapper(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;
        uint cmtSize = (uint)(2 * P);

        var allocator = builder.CreateAllocator(AllocationStart, AllocationEnd);

        // SimpleComCallWrapper; MainWrapper → first CCW (set up below)
        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 3 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        // First (start) CCW: one interface in slot 0 (IUnknown), Next → second CCW
        var ccw1Frag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper[1]");
        builder.AddHeapFragment(ccw1Frag);

        var cmt1Frag = allocator.Allocate(cmtSize, "ComMethodTable[1]");
        builder.AddHeapFragment(cmt1Frag);
        ulong vtable1 = cmt1Frag.Address + cmtSize;

        // Second (linked) CCW: one interface in slot 1, Next = terminator
        var ccw2Frag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper[2]");
        builder.AddHeapFragment(ccw2Frag);

        ulong expectedMT = 0xaaaa_0001;
        var cmt2Frag = allocator.Allocate(cmtSize, "ComMethodTable[2]");
        builder.AddHeapFragment(cmt2Frag);
        ulong vtable2 = cmt2Frag.Address + cmtSize;

        // sccw.MainWrapper = ccw1 (the start wrapper)
        Span<byte> sccwData = builder.BorrowAddressRange(simpleWrapperFrag.Address, 12 + 2 * P);
        helpers.WritePointer(sccwData.Slice(12, P), ccw1Frag.Address);

        // Write first CCW
        Span<byte> w1 = builder.BorrowAddressRange(ccw1Frag.Address, 7 * P);
        helpers.WritePointer(w1.Slice(0, P), simpleWrapperFrag.Address);
        helpers.WritePointer(w1.Slice(1 * P, P), vtable1);       // slot 0
        helpers.WritePointer(w1.Slice(2 * P, P), 0);
        helpers.WritePointer(w1.Slice(3 * P, P), 0);
        helpers.WritePointer(w1.Slice(4 * P, P), 0);
        helpers.WritePointer(w1.Slice(5 * P, P), 0);
        helpers.WritePointer(w1.Slice(6 * P, P), ccw2Frag.Address); // Next → ccw2

        // Write second CCW
        Span<byte> w2 = builder.BorrowAddressRange(ccw2Frag.Address, 7 * P);
        helpers.WritePointer(w2.Slice(0, P), simpleWrapperFrag.Address);
        helpers.WritePointer(w2.Slice(1 * P, P), 0);             // slot 0 null
        helpers.WritePointer(w2.Slice(2 * P, P), vtable2);       // slot 1
        helpers.WritePointer(w2.Slice(3 * P, P), 0);
        helpers.WritePointer(w2.Slice(4 * P, P), 0);
        helpers.WritePointer(w2.Slice(5 * P, P), 0);
        helpers.WritePointer(w2.Slice(6 * P, P), LinkedWrapperTerminator);

        // ComMethodTable for ccw1 slot 0 (IUnknown; MethodTable null for slot 0 of first wrapper)
        Span<byte> d1 = builder.BorrowAddressRange(cmt1Frag.Address, (int)cmtSize);
        helpers.WriteNUInt(d1.Slice(0, P), new TargetNUInt(IsLayoutCompleteFlag));
        helpers.WritePointer(d1.Slice(P, P), 0);

        // ComMethodTable for ccw2 slot 1
        Span<byte> d2 = builder.BorrowAddressRange(cmt2Frag.Address, (int)cmtSize);
        helpers.WriteNUInt(d2.Slice(0, P), new TargetNUInt(IsLayoutCompleteFlag));
        helpers.WritePointer(d2.Slice(P, P), expectedMT);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));

        // Passing the start CCW enumerates both wrappers' interfaces.
        List<COMInterfacePointerData> interfacesFromStart =
            target.Contracts.BuiltInCOM.GetCCWInterfaces(new TargetPointer(ccw1Frag.Address)).ToList();

        Assert.Equal(2, interfacesFromStart.Count);
        // ccw1 slot 0: IUnknown → MethodTable = Null (first wrapper, slot 0)
        Assert.Equal(ccw1Frag.Address + (ulong)P, interfacesFromStart[0].InterfacePointerAddress.Value);
        Assert.Equal(TargetPointer.Null.Value, interfacesFromStart[0].MethodTable.Value);
        // ccw2 slot 1
        Assert.Equal(ccw2Frag.Address + (ulong)(2 * P), interfacesFromStart[1].InterfacePointerAddress.Value);
        Assert.Equal(expectedMT, interfacesFromStart[1].MethodTable.Value);

        // Passing the second (non-start) CCW also navigates to the start and enumerates the full chain.
        List<COMInterfacePointerData> interfacesFromLinked =
            target.Contracts.BuiltInCOM.GetCCWInterfaces(new TargetPointer(ccw2Frag.Address)).ToList();

        Assert.Equal(interfacesFromStart.Count, interfacesFromLinked.Count);
        for (int i = 0; i < interfacesFromStart.Count; i++)
        {
            Assert.Equal(interfacesFromStart[i].InterfacePointerAddress.Value, interfacesFromLinked[i].InterfacePointerAddress.Value);
            Assert.Equal(interfacesFromStart[i].MethodTable.Value, interfacesFromLinked[i].MethodTable.Value);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWInterfaces_ReturnsFilledEntries(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        (TargetPointer MethodTable, TargetPointer Unknown)[] expectedEntries =
        [
            (new TargetPointer(0x1000), new TargetPointer(0x2000)),
            (new TargetPointer(0x3000), new TargetPointer(0x4000)),
        ];

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddRCWWithInlineEntries(builder, targetTestHelpers, types, allocator, expectedEntries);
            },
            (target) =>
            {
                IBuiltInCOM contract = target.Contracts.BuiltInCOM;
                Assert.NotNull(contract);

                List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
                    contract.GetRCWInterfaces(rcwAddress).ToList();

                Assert.Equal(expectedEntries.Length, results.Count);
                for (int i = 0; i < expectedEntries.Length; i++)
                {
                    Assert.Equal(expectedEntries[i].MethodTable, results[i].MethodTable);
                    Assert.Equal(expectedEntries[i].Unknown, results[i].Unknown);
                }
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWInterfaces_ComIpAddress_ResolvesToCCW(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;
        uint cmtSize = (uint)(2 * P);

        var allocator = builder.CreateAllocator(AllocationStart, AllocationEnd);

        // SimpleComCallWrapper; MainWrapper → CCW (aligned, set up below)
        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 3 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        // Place the CCW at a CCWThisMask-aligned address so that (ccwAddr + P) & thisMask == ccwAddr.
        // On 64-bit: alignment = 64 bytes; on 32-bit: alignment = 32 bytes.
        ulong thisMask = GetCCWThisMask(P);
        ulong alignment = ~thisMask + 1; // = 64 on 64-bit, 32 on 32-bit
        ulong alignedCCWAddr = (AllocationStart + 0x200 + alignment - 1) & thisMask;
        var ccwFrag = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = alignedCCWAddr,
            Data = new byte[8 * P],
        };
        builder.AddHeapFragment(ccwFrag);

        // sccw.MainWrapper = ccwFrag.Address (start wrapper)
        Span<byte> sccwData = builder.BorrowAddressRange(simpleWrapperFrag.Address, 12 + 3 * P);
        helpers.WritePointer(sccwData.Slice(12, P), ccwFrag.Address);

        // Allocate CMT and a vtable extension right after it so vtable = cmtFrag.Address + cmtSize.
        // The vtable extension holds vtable slots [0] (QI) and [1] (AddRef = TearOffAddRefAddr).
        var cmtFrag = allocator.Allocate(cmtSize, "ComMethodTable");
        builder.AddHeapFragment(cmtFrag);
        ulong vtable = cmtFrag.Address + cmtSize;
        var vtableExtFrag = new MockMemorySpace.HeapFragment
        {
            Name = "VtableExt",
            Address = vtable,
            Data = new byte[2 * P],
        };
        builder.AddHeapFragment(vtableExtFrag);
        Span<byte> vtableExtData = builder.BorrowAddressRange(vtable, 2 * P);
        helpers.WritePointer(vtableExtData.Slice(0, P), 0);                 // slot 0: QI
        helpers.WritePointer(vtableExtData.Slice(P, P), TearOffAddRefAddr); // slot 1: AddRef = tear-off

        // Write the CCW: slot 0 (IP[0]) = vtable, rest null, Next = terminator
        Span<byte> ccwData = builder.BorrowAddressRange(ccwFrag.Address, 8 * P);
        helpers.WritePointer(ccwData.Slice(0, P), simpleWrapperFrag.Address);
        helpers.WritePointer(ccwData.Slice(1 * P, P), vtable); // IP[0]
        helpers.WritePointer(ccwData.Slice(2 * P, P), 0);
        helpers.WritePointer(ccwData.Slice(3 * P, P), 0);
        helpers.WritePointer(ccwData.Slice(4 * P, P), 0);
        helpers.WritePointer(ccwData.Slice(5 * P, P), 0);
        helpers.WritePointer(ccwData.Slice(6 * P, P), LinkedWrapperTerminator);

        // Write CMT: LayoutComplete, MethodTable = null (slot 0 of first wrapper)
        Span<byte> cmtData = builder.BorrowAddressRange(cmtFrag.Address, (int)cmtSize);
        helpers.WriteNUInt(cmtData.Slice(0, P), new TargetNUInt(IsLayoutCompleteFlag));
        helpers.WritePointer(cmtData.Slice(P, P), 0);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));

        // COM IP = alignedCCWAddr + P (= address of IP[0] slot within the CCW).
        // *comIP = vtable; vtable[1] = TearOffAddRefAddr → detected as standard CCW IP.
        // (alignedCCWAddr + P) & thisMask = alignedCCWAddr (since P < alignment).
        ulong comIPAddr = alignedCCWAddr + (ulong)P;

        // GetCCWFromInterfacePointer resolves the COM IP to the start CCW pointer.
        TargetPointer startCCWFromIP = target.Contracts.BuiltInCOM.GetCCWFromInterfacePointer(new TargetPointer(comIPAddr));
        Assert.Equal(ccwFrag.Address, startCCWFromIP.Value);

        // A direct CCW pointer is not a COM IP; GetCCWFromInterfacePointer returns Null.
        TargetPointer nullResult = target.Contracts.BuiltInCOM.GetCCWFromInterfacePointer(new TargetPointer(ccwFrag.Address));
        Assert.Equal(TargetPointer.Null, nullResult);

        // GetCCWInterfaces works with either the resolved IP or the direct CCW pointer.
        List<COMInterfacePointerData> ifacesDirect =
            target.Contracts.BuiltInCOM.GetCCWInterfaces(new TargetPointer(ccwFrag.Address)).ToList();
        List<COMInterfacePointerData> ifacesFromIP =
            target.Contracts.BuiltInCOM.GetCCWInterfaces(startCCWFromIP).ToList();

        // Both paths should produce the same interfaces
        Assert.Equal(ifacesDirect.Count, ifacesFromIP.Count);
        for (int i = 0; i < ifacesDirect.Count; i++)
        {
            Assert.Equal(ifacesDirect[i].InterfacePointerAddress.Value, ifacesFromIP[i].InterfacePointerAddress.Value);
            Assert.Equal(ifacesDirect[i].MethodTable.Value, ifacesFromIP[i].MethodTable.Value);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWInterfaces_SkipsEntriesWithNullUnknown(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        // The IsFree() check uses only Unknown == null; entries with Unknown == null are skipped.
        (TargetPointer MethodTable, TargetPointer Unknown)[] entries =
        [
            (new TargetPointer(0x1000), new TargetPointer(0x2000)),
            (TargetPointer.Null, TargetPointer.Null),  // free entry (Unknown == null)
            (new TargetPointer(0x5000), new TargetPointer(0x6000)),
        ];

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddRCWWithInlineEntries(builder, targetTestHelpers, types, allocator, entries);
            },
            (target) =>
            {
                IBuiltInCOM contract = target.Contracts.BuiltInCOM;
                List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
                    contract.GetRCWInterfaces(rcwAddress).ToList();

                // Only the 2 entries with non-null Unknown are returned
                Assert.Equal(2, results.Count);
                Assert.Equal(new TargetPointer(0x1000), results[0].MethodTable);
                Assert.Equal(new TargetPointer(0x2000), results[0].Unknown);
                Assert.Equal(new TargetPointer(0x5000), results[1].MethodTable);
                Assert.Equal(new TargetPointer(0x6000), results[1].Unknown);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWInterfaces_EmptyCache_ReturnsEmpty(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddRCWWithInlineEntries(builder, targetTestHelpers, types, allocator, []);
            },
            (target) =>
            {
                IBuiltInCOM contract = target.Contracts.BuiltInCOM;
                List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
                    contract.GetRCWInterfaces(rcwAddress).ToList();

                Assert.Empty(results);
            });
    }

    // Bit-flag constants mirroring BuiltInCOM_1 internal constants, used to construct Flags for GetRCWData tests.
    private const uint RCWFlagAggregated   = 0x10u;   // URTAggregatedMask
    private const uint RCWFlagContained    = 0x20u;   // URTContainedMask
    private const uint RCWFlagFreeThreaded = 0x100u;  // MarshalingTypeFreeThreadedValue

    /// <summary>
    /// Allocates a full RCW mock with all fields needed for <see cref="IBuiltInCOM.GetRCWData"/>.
    /// </summary>
    private static TargetPointer AddFullRCW(
        MockMemorySpace.Builder builder,
        TargetTestHelpers helpers,
        Dictionary<DataType, Target.TypeInfo> types,
        MockMemorySpace.BumpAllocator allocator,
        TargetPointer identityPointer = default,
        TargetPointer unknownPointer = default,
        TargetPointer vtablePtr = default,
        TargetPointer creatorThread = default,
        TargetPointer ctxCookie = default,
        TargetPointer ctxEntry = default,
        uint syncBlockIndex = 0,
        uint refCount = 0,
        uint flags = 0)
    {
        Target.TypeInfo rcwTypeInfo = types[DataType.RCW];
        Target.TypeInfo entryTypeInfo = types[DataType.InterfaceEntry];
        uint entrySize = entryTypeInfo.Size!.Value;
        uint entriesOffset = (uint)rcwTypeInfo.Fields[nameof(Data.RCW.InterfaceEntries)].Offset;
        uint totalSize = entriesOffset + entrySize * TestRCWInterfaceCacheSize;

        MockMemorySpace.HeapFragment fragment = allocator.Allocate(totalSize, "Full RCW");
        Span<byte> data = fragment.Data;

        helpers.WritePointer(data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.IdentityPointer)].Offset), identityPointer);
        helpers.WritePointer(data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.UnknownPointer)].Offset), unknownPointer);
        helpers.WritePointer(data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.VTablePtr)].Offset), vtablePtr);
        helpers.WritePointer(data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.CreatorThread)].Offset), creatorThread);
        helpers.WritePointer(data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.CtxCookie)].Offset), ctxCookie);
        helpers.WritePointer(data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.CtxEntry)].Offset), ctxEntry);
        helpers.Write(data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.SyncBlockIndex)].Offset), syncBlockIndex);
        helpers.Write(data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.RefCount)].Offset), refCount);
        helpers.Write(data.Slice(rcwTypeInfo.Fields[nameof(Data.RCW.Flags)].Offset), flags);

        builder.AddHeapFragment(fragment);
        return fragment.Address;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_ReturnsScalarFields(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        TargetPointer expectedIdentity  = new TargetPointer(0x1000_0000);
        TargetPointer expectedVTable    = new TargetPointer(0x2000_0000);
        TargetPointer expectedThread    = new TargetPointer(0x3000_0000);
        TargetPointer expectedCookie    = new TargetPointer(0x4000_0000);
        uint          expectedRefCount  = 42;

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddFullRCW(builder, targetTestHelpers, types, allocator,
                    identityPointer: expectedIdentity,
                    vtablePtr: expectedVTable,
                    creatorThread: expectedThread,
                    ctxCookie: expectedCookie,
                    refCount: expectedRefCount);
            },
            (target) =>
            {
                RCWData result = target.Contracts.BuiltInCOM.GetRCWData(rcwAddress);

                Assert.Equal(expectedIdentity, result.IdentityPointer);
                Assert.Equal(expectedVTable, result.VTablePtr);
                Assert.Equal(expectedThread, result.CreatorThread);
                Assert.Equal(expectedCookie, result.CtxCookie);
                Assert.Equal(expectedRefCount, result.RefCount);
                Assert.Equal(TargetPointer.Null, result.ManagedObject);
                Assert.False(result.IsAggregated);
                Assert.False(result.IsContained);
                Assert.False(result.IsFreeThreaded);
                Assert.False(result.IsDisconnected);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_FlagsAggregatedAndContained(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddFullRCW(builder, targetTestHelpers, types, allocator,
                    flags: RCWFlagAggregated | RCWFlagContained);
            },
            (target) =>
            {
                RCWData result = target.Contracts.BuiltInCOM.GetRCWData(rcwAddress);

                Assert.True(result.IsAggregated);
                Assert.True(result.IsContained);
                Assert.False(result.IsFreeThreaded);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_FlagsFreeThreaded(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddFullRCW(builder, targetTestHelpers, types, allocator,
                    flags: RCWFlagFreeThreaded);
            },
            (target) =>
            {
                RCWData result = target.Contracts.BuiltInCOM.GetRCWData(rcwAddress);

                Assert.True(result.IsFreeThreaded);
                Assert.False(result.IsAggregated);
                Assert.False(result.IsContained);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_IsDisconnected_Sentinel(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        const ulong DisconnectedSentinel = 0xBADF00D;

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddFullRCW(builder, targetTestHelpers, types, allocator,
                    unknownPointer: new TargetPointer(DisconnectedSentinel));
            },
            (target) =>
            {
                RCWData result = target.Contracts.BuiltInCOM.GetRCWData(rcwAddress);

                Assert.True(result.IsDisconnected);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_IsDisconnected_CtxCookieMismatch(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);

                // Allocate a CtxEntry whose CtxCookie differs from the RCW's CtxCookie.
                Target.TypeInfo ctxTypeInfo = types[DataType.CtxEntry];
                MockMemorySpace.HeapFragment ctxFragment = allocator.Allocate(ctxTypeInfo.Size!.Value, "CtxEntry");
                TargetPointer ctxCookieInEntry = new TargetPointer(0xAAAA_0000);
                builder.TargetTestHelpers.WritePointer(
                    ctxFragment.Data.AsSpan().Slice(ctxTypeInfo.Fields[nameof(Data.CtxEntry.CtxCookie)].Offset),
                    ctxCookieInEntry);
                builder.AddHeapFragment(ctxFragment);

                TargetPointer ctxCookieInRcw = new TargetPointer(0xBBBB_0000);  // different from entry
                rcwAddress = AddFullRCW(builder, targetTestHelpers, types, allocator,
                    ctxCookie: ctxCookieInRcw,
                    ctxEntry: ctxFragment.Address);  // bit 0 clear → not null, not adjusted
            },
            (target) =>
            {
                RCWData result = target.Contracts.BuiltInCOM.GetRCWData(rcwAddress);

                Assert.True(result.IsDisconnected);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_ManagedObject_ResolvedViaSyncBlockIndex(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        TargetPointer expectedManagedObject = new TargetPointer(0xDEAD_BEEF_0000UL);
        const uint syncBlockIndex = 3;

        var mockSyncBlock = new Mock<ISyncBlock>();
        mockSyncBlock.Setup(s => s.GetSyncBlockObject(syncBlockIndex)).Returns(expectedManagedObject);

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddFullRCW(builder, targetTestHelpers, types, allocator,
                    syncBlockIndex: syncBlockIndex);
            },
            (target) =>
            {
                RCWData result = target.Contracts.BuiltInCOM.GetRCWData(rcwAddress);

                Assert.Equal(expectedManagedObject, result.ManagedObject);
            },
            syncBlock: mockSyncBlock.Object);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWContext_ReturnsCtxCookie(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        TargetPointer expectedCookie = new TargetPointer(0xC00C_1E00);

        BuiltInCOMContractHelper(arch,
            (builder, targetTestHelpers, types) =>
            {
                MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd);
                rcwAddress = AddRCWWithInlineEntries(builder, targetTestHelpers, types, allocator, [], expectedCookie);
            },
            (target) =>
            {
                IBuiltInCOM contract = target.Contracts.BuiltInCOM;
                TargetPointer result = contract.GetRCWContext(rcwAddress);

                Assert.Equal(expectedCookie, result);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWFromInterfacePointer_SCCWIp_ResolvesToStartCCW(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        var allocator = builder.CreateAllocator(AllocationStart, AllocationEnd);

        // SimpleComCallWrapper:
        //   Offset  0: RefCount (8 bytes)
        //   Offset  8: Flags (4 bytes)
        //   Offset 12: MainWrapper (pointer)
        //   Offset 12+P: VTablePtr array (at least two pointer-sized slots: kinds 0 and 1)
        var sccwFrag = allocator.Allocate((ulong)(12 + 3 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(sccwFrag);

        // ComCallWrapper (start wrapper, no interfaces, Next = terminator)
        var ccwFrag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper");
        builder.AddHeapFragment(ccwFrag);

        // Vtable data for interfaceKind = 1:
        //   [descBase = vtableAddr - P: stores interfaceKind as int32]
        //   [vtable[0]: QI slot (unused)]
        //   [vtable[1]: AddRef = TearOffAddRefSimpleAddr]
        int interfaceKind = 1;
        var vtableDataFrag = allocator.Allocate((ulong)(3 * P), "VtableData");
        builder.AddHeapFragment(vtableDataFrag);
        ulong vtableAddr = vtableDataFrag.Address + (ulong)P; // vtable = frag + P

        // Write SCCW: MainWrapper = ccw, VTablePtr slot 1 = vtableAddr
        ulong vtablePtrBase = sccwFrag.Address + 12 + (ulong)P; // = sccw.VTablePtr
        Span<byte> sccwData = builder.BorrowAddressRange(sccwFrag.Address, 12 + 3 * P);
        helpers.WritePointer(sccwData.Slice(12, P), ccwFrag.Address);           // MainWrapper
        helpers.WritePointer(sccwData.Slice(12 + P, P), 0);                    // kind 0 vtable (unused)
        helpers.WritePointer(sccwData.Slice(12 + 2 * P, P), vtableAddr);       // kind 1 vtable

        // Write vtable descriptor: interfaceKind at descBase, TearOffAddRefSimple at slot 1
        Span<byte> vtableData = builder.BorrowAddressRange(vtableDataFrag.Address, 3 * P);
        helpers.Write(vtableData.Slice(0, 4), interfaceKind);                   // descBase: kind = 1
        helpers.WritePointer(vtableData.Slice(P, P), 0);                       // vtable[0]: QI
        helpers.WritePointer(vtableData.Slice(2 * P, P), TearOffAddRefSimpleAddr); // vtable[1]: AddRef

        // Write CCW: SimpleWrapper = sccw, all IP slots null, Next = terminator
        Span<byte> ccwData = builder.BorrowAddressRange(ccwFrag.Address, 8 * P);
        helpers.WritePointer(ccwData.Slice(0, P), sccwFrag.Address);           // SimpleWrapper
        helpers.WritePointer(ccwData.Slice(6 * P, P), LinkedWrapperTerminator); // Next = terminator

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));

        // SCCW IP for interfaceKind=1 is the address of the vtable pointer slot in the SCCW.
        // Reading *sccwIP gives vtableAddr; reading *(vtableAddr + P) gives TearOffAddRefSimple.
        ulong sccwIP = vtablePtrBase + (ulong)(interfaceKind * P);

        TargetPointer startCCW = target.Contracts.BuiltInCOM.GetCCWFromInterfacePointer(new TargetPointer(sccwIP));
        Assert.Equal(ccwFrag.Address, startCCW.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectHandle_ReturnsHandleFromWrapper(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        var allocator = builder.CreateAllocator(AllocationStart, AllocationEnd);

        var handleFrag = allocator.Allocate((ulong)P, "Handle");
        builder.AddHeapFragment(handleFrag);

        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 3 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        var ccwFrag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper");
        builder.AddHeapFragment(ccwFrag);

        Span<byte> sccwData = builder.BorrowAddressRange(simpleWrapperFrag.Address, 12 + 3 * P);
        helpers.WritePointer(sccwData.Slice(12, P), ccwFrag.Address); // MainWrapper

        Span<byte> ccwData = builder.BorrowAddressRange(ccwFrag.Address, 8 * P);
        helpers.WritePointer(ccwData.Slice(0, P), simpleWrapperFrag.Address);    // SimpleWrapper
        helpers.WritePointer(ccwData.Slice(6 * P, P), LinkedWrapperTerminator); // Next = terminator
        helpers.WritePointer(ccwData.Slice(7 * P, P), handleFrag.Address);       // Handle

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        TargetPointer handle = target.Contracts.BuiltInCOM.GetObjectHandle(new TargetPointer(ccwFrag.Address));
        Assert.Equal(handleFrag.Address, handle.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectHandle_NullHandle_ReturnsNull(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        var allocator = builder.CreateAllocator(AllocationStart, AllocationEnd);

        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 3 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        var ccwFrag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper");
        builder.AddHeapFragment(ccwFrag);

        Span<byte> sccwData = builder.BorrowAddressRange(simpleWrapperFrag.Address, 12 + 3 * P);
        helpers.WritePointer(sccwData.Slice(12, P), ccwFrag.Address); // MainWrapper

        Span<byte> ccwData = builder.BorrowAddressRange(ccwFrag.Address, 8 * P);
        helpers.WritePointer(ccwData.Slice(0, P), simpleWrapperFrag.Address);    // SimpleWrapper
        helpers.WritePointer(ccwData.Slice(6 * P, P), LinkedWrapperTerminator); // Next = terminator
        // Handle at offset 7*P left as null (zero-initialized)

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        TargetPointer handle = target.Contracts.BuiltInCOM.GetObjectHandle(new TargetPointer(ccwFrag.Address));
        Assert.Equal(TargetPointer.Null, handle);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetStartWrapper_SingleWrapper_ReturnsSelf(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr); // MainWrapper
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        // Next = null (single wrapper, not linked)
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        Assert.Equal(ccwAddr, target.Contracts.BuiltInCOM.GetStartWrapper(new TargetPointer(ccwAddr)).Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetStartWrapper_LinkedWrapper_NavigatesToStart(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        var allocator = builder.CreateAllocator(AllocationStart, AllocationEnd);

        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 3 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        var ccw1Frag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper[1]");
        builder.AddHeapFragment(ccw1Frag);

        var ccw2Frag = allocator.Allocate((ulong)(8 * P), "ComCallWrapper[2]");
        builder.AddHeapFragment(ccw2Frag);

        // sccw.MainWrapper = ccw1 (start wrapper)
        Span<byte> sccwData = builder.BorrowAddressRange(simpleWrapperFrag.Address, 12 + 3 * P);
        helpers.WritePointer(sccwData.Slice(12, P), ccw1Frag.Address);

        // ccw1: SimpleWrapper = sccw, Next = ccw2
        Span<byte> w1 = builder.BorrowAddressRange(ccw1Frag.Address, 8 * P);
        helpers.WritePointer(w1.Slice(0, P), simpleWrapperFrag.Address);
        helpers.WritePointer(w1.Slice(6 * P, P), ccw2Frag.Address);

        // ccw2: SimpleWrapper = sccw, Next = terminator
        Span<byte> w2 = builder.BorrowAddressRange(ccw2Frag.Address, 8 * P);
        helpers.WritePointer(w2.Slice(0, P), simpleWrapperFrag.Address);
        helpers.WritePointer(w2.Slice(6 * P, P), LinkedWrapperTerminator);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        var builtInCOM = target.Contracts.BuiltInCOM;

        // From start wrapper, returns itself
        Assert.Equal(ccw1Frag.Address, builtInCOM.GetStartWrapper(new TargetPointer(ccw1Frag.Address)).Value);
        // From linked wrapper, navigates to start
        Assert.Equal(ccw1Frag.Address, builtInCOM.GetStartWrapper(new TargetPointer(ccw2Frag.Address)).Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_ReturnsAllFields(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;
        ulong outerIUnknownAddr = 0xBBBB_0000;
        ulong rawRefCount = 0x0000_0003UL;

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P],
        };
        helpers.Write(simpleWrapperFragment.Data.AsSpan(0, 8), rawRefCount);                             // RefCount
        helpers.Write(simpleWrapperFragment.Data.AsSpan(8, 4), (uint)0x2);                               // Flags: IsExtendsCom
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);                         // MainWrapper
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12 + 2 * P, P), outerIUnknownAddr);       // OuterIUnknown
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        SimpleComCallWrapperData data = target.Contracts.BuiltInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));
        Assert.Equal(rawRefCount & ComRefcountMask, data.RefCount);
        Assert.False(data.IsNeutered);
        Assert.False(data.IsAggregated);
        Assert.True(data.IsExtendsCOMObject);
        Assert.False(data.IsHandleWeak);
        Assert.Equal(outerIUnknownAddr, data.OuterIUnknown.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_ZeroFields_AreNull(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        ulong simpleWrapperAddr = 0x5000;
        ulong ccwAddr = 0x4000;

        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 3 * P], // all zero-initialized
        };
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr); // MainWrapper
        builder.AddHeapFragment(simpleWrapperFragment);

        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[8 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));
        SimpleComCallWrapperData data = target.Contracts.BuiltInCOM.GetSimpleComCallWrapperData(new TargetPointer(ccwAddr));

        Assert.Equal(0UL, data.RefCount);
        Assert.False(data.IsNeutered);
        Assert.False(data.IsAggregated);
        Assert.False(data.IsExtendsCOMObject);
        Assert.False(data.IsHandleWeak);
        Assert.Equal(TargetPointer.Null, data.OuterIUnknown);
    }
}
