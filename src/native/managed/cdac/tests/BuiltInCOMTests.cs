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
    // Flag values matching the C++ runtime
    private const ulong IsLayoutCompleteFlag = 0x10;

    // LinkedWrapperTerminator: (PTR_ComCallWrapper)-1, all bits set
    private const ulong LinkedWrapperTerminator = ulong.MaxValue;

    private const uint NumVtablePtrs = 5;
    private const ulong ComRefcountMask = 0x000000007FFFFFFF;

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
        (string Name, ulong Value)[] globals =
        [
            (Constants.Globals.ComRefcountMask, ComRefcountMask),
            (Constants.Globals.CCWNumInterfaces, NumVtablePtrs),
            (Constants.Globals.CCWThisMask, GetCCWThisMask(P)),
            (Constants.Globals.TearOffAddRef, TearOffAddRefAddr),
            (Constants.Globals.TearOffAddRefSimple, TearOffAddRefSimpleAddr),
            (Constants.Globals.TearOffAddRefSimpleInner, TearOffAddRefSimpleInnerAddr),
        ];
        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, types, globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.BuiltInCOM == ((IContractFactory<IBuiltInCOM>)new BuiltInCOMFactory()).CreateContract(target, 1)));
        return target;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRefCount_ReturnsRefCountMasked(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;

        // SimpleComCallWrapper: RefCount with high bit set (above the mask)
        ulong simpleWrapperAddr = 0x5000;
        ulong rawRefCount = 0x0000_0000_1234_5678UL | 0x80000000UL;
        var simpleWrapperFragment = new MockMemorySpace.HeapFragment
        {
            Name = "SimpleComCallWrapper",
            Address = simpleWrapperAddr,
            Data = new byte[12 + 2 * P],
        };
        helpers.Write(simpleWrapperFragment.Data.AsSpan(0, 8), rawRefCount);
        // MainWrapper at offset 12: point back to the CCW (set below)
        ulong ccwAddr = 0x4000;
        helpers.WritePointer(simpleWrapperFragment.Data.AsSpan(12, P), ccwAddr);
        builder.AddHeapFragment(simpleWrapperFragment);

        // ComCallWrapper pointing at the simple wrapper
        var ccwFragment = new MockMemorySpace.HeapFragment
        {
            Name = "ComCallWrapper",
            Address = ccwAddr,
            Data = new byte[7 * P],
        };
        helpers.WritePointer(ccwFragment.Data.AsSpan(0, P), simpleWrapperAddr);
        // Next = LinkedWrapperTerminator (last in chain)
        helpers.WritePointer(ccwFragment.Data.AsSpan(6 * P, P), LinkedWrapperTerminator);
        builder.AddHeapFragment(ccwFragment);

        Target target = CreateTarget(arch, builder, CreateTypeInfos(P));

        ulong refCount = target.Contracts.BuiltInCOM.GetRefCount(new TargetPointer(ccwAddr));
        Assert.Equal(rawRefCount & ComRefcountMask, refCount);
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
        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 2 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        // ComCallWrapper: SimpleWrapper + 5 slots + Next = 7 pointers
        var ccwFrag = allocator.Allocate((ulong)(7 * P), "ComCallWrapper");
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
        Assert.Equal(ccwFrag.Address + (ulong)P, interfaces[0].InterfacePointer.Value);
        Assert.Equal(TargetPointer.Null.Value, interfaces[0].MethodTable.Value);

        // Slot 2: at offset 3*P from CCW base (IPtr + 2*P)
        Assert.Equal(ccwFrag.Address + (ulong)(3 * P), interfaces[1].InterfacePointer.Value);
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
        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 2 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        // First CCW: slot 0 = IUnknown; slots 1-4 = null; Next -> second CCW
        var ccw1Frag = allocator.Allocate((ulong)(7 * P), "ComCallWrapper[1]");
        builder.AddHeapFragment(ccw1Frag);

        var cmt1_0Frag = allocator.Allocate(cmtSize, "ComMethodTable ccw1[0]");
        builder.AddHeapFragment(cmt1_0Frag);
        ulong vtable1_0 = cmt1_0Frag.Address + cmtSize;

        // Second CCW: slot 0 = IClassX, slot 2 = interface; Next = terminator
        var ccw2Frag = allocator.Allocate((ulong)(7 * P), "ComCallWrapper[2]");
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
        Assert.Equal(ccw1Frag.Address + (ulong)P, interfaces[0].InterfacePointer.Value);
        Assert.Equal(TargetPointer.Null.Value, interfaces[0].MethodTable.Value);

        // Second wrapper, slot 0: IClassX - has a MethodTable (not first wrapper)
        Assert.Equal(ccw2Frag.Address + (ulong)P, interfaces[1].InterfacePointer.Value);
        Assert.Equal(expectedMT_slot0, interfaces[1].MethodTable.Value);

        // Second wrapper, slot 2
        Assert.Equal(ccw2Frag.Address + (ulong)(3 * P), interfaces[2].InterfacePointer.Value);
        Assert.Equal(expectedMT_slot2, interfaces[2].MethodTable.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWInterfaces_LinkedWrapper_NavigatesToStartWrapper(MockTarget.Architecture arch)
    {
        var helpers = new TargetTestHelpers(arch);
        var builder = new MockMemorySpace.Builder(helpers);
        int P = helpers.PointerSize;
        uint cmtSize = (uint)(2 * P);

        var allocator = builder.CreateAllocator(AllocationStart, AllocationEnd);

        // SimpleComCallWrapper; MainWrapper → first CCW (set up below)
        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 2 * P), "SimpleComCallWrapper");
        builder.AddHeapFragment(simpleWrapperFrag);

        // First (start) CCW: one interface in slot 0 (IUnknown), Next → second CCW
        var ccw1Frag = allocator.Allocate((ulong)(7 * P), "ComCallWrapper[1]");
        builder.AddHeapFragment(ccw1Frag);

        var cmt1Frag = allocator.Allocate(cmtSize, "ComMethodTable[1]");
        builder.AddHeapFragment(cmt1Frag);
        ulong vtable1 = cmt1Frag.Address + cmtSize;

        // Second (linked) CCW: one interface in slot 1, Next = terminator
        var ccw2Frag = allocator.Allocate((ulong)(7 * P), "ComCallWrapper[2]");
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

        // Pass the address of the SECOND (linked) wrapper; should navigate to the start (ccw1)
        List<COMInterfacePointerData> interfaces =
            target.Contracts.BuiltInCOM.GetCCWInterfaces(new TargetPointer(ccw2Frag.Address)).ToList();

        // Both wrappers' interfaces are enumerated, starting from ccw1
        Assert.Equal(2, interfaces.Count);

        // ccw1 slot 0: IUnknown → MethodTable = Null (first wrapper, slot 0)
        Assert.Equal(ccw1Frag.Address + (ulong)P, interfaces[0].InterfacePointer.Value);
        Assert.Equal(TargetPointer.Null.Value, interfaces[0].MethodTable.Value);

        // ccw2 slot 1
        Assert.Equal(ccw2Frag.Address + (ulong)(2 * P), interfaces[1].InterfacePointer.Value);
        Assert.Equal(expectedMT, interfaces[1].MethodTable.Value);
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
        var simpleWrapperFrag = allocator.Allocate((ulong)(12 + 2 * P), "SimpleComCallWrapper");
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
            Data = new byte[7 * P],
        };
        builder.AddHeapFragment(ccwFrag);

        // sccw.MainWrapper = ccwFrag.Address (start wrapper)
        Span<byte> sccwData = builder.BorrowAddressRange(simpleWrapperFrag.Address, 12 + 2 * P);
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
        Span<byte> ccwData = builder.BorrowAddressRange(ccwFrag.Address, 7 * P);
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

        List<COMInterfacePointerData> ifacesDirect =
            target.Contracts.BuiltInCOM.GetCCWInterfaces(new TargetPointer(ccwFrag.Address)).ToList();
        List<COMInterfacePointerData> ifacesFromIP =
            target.Contracts.BuiltInCOM.GetCCWInterfaces(new TargetPointer(comIPAddr)).ToList();

        // Both paths should produce the same interfaces
        Assert.Equal(ifacesDirect.Count, ifacesFromIP.Count);
        for (int i = 0; i < ifacesDirect.Count; i++)
        {
            Assert.Equal(ifacesDirect[i].InterfacePointer.Value, ifacesFromIP[i].InterfacePointer.Value);
            Assert.Equal(ifacesDirect[i].MethodTable.Value, ifacesFromIP[i].MethodTable.Value);
        }
    }
}
