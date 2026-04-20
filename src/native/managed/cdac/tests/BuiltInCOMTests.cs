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

    private static IBuiltInCOM CreateBuiltInCOM(
        MockTarget.Architecture arch,
        Action<MockBuiltInComBuilder> configure,
        ISyncBlock? syncBlock = null,
        int minAlign = 16)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(AllocationRangeStart, AllocationRangeEnd, minAlign: minAlign);
        MockBuiltInComBuilder builtInCom = new(targetBuilder.MemoryBuilder, allocator, arch);
        configure(builtInCom);

        ISyncBlock syncBlockContract = syncBlock ?? Mock.Of<ISyncBlock>();
        var target = targetBuilder
            .AddTypes(CreateContractTypes(builtInCom))
            .AddGlobals(CreateContractGlobals(builtInCom))
            .AddContract<IBuiltInCOM>(version: "c1")
            .AddMockContract<ISyncBlock>(syncBlockContract)
            .Build();
        return target.Contracts.BuiltInCOM;
    }

    // Flag values matching the C++ runtime
    private const ulong CleanupSentinel = 0x80000000UL;
    private const uint IsAggregatedFlag = 0x1;
    private const uint IsExtendsComFlag = 0x2;
    private const uint IsHandleWeakFlag = 0x4;
    private const ulong IsLayoutCompleteFlag = 0x10;

    // LinkedWrapperTerminator: (PTR_ComCallWrapper)-1, all bits set
    private const ulong LinkedWrapperTerminator = ulong.MaxValue;

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockBuiltInComBuilder builtInCom)
        => new()
        {
            [DataType.ComCallWrapper] = TargetTestHelpers.CreateTypeInfo(builtInCom.ComCallWrapperLayout),
            [DataType.SimpleComCallWrapper] = TargetTestHelpers.CreateTypeInfo(builtInCom.SimpleComCallWrapperLayout),
            [DataType.ComMethodTable] = TargetTestHelpers.CreateTypeInfo(builtInCom.ComMethodTableLayout),
            [DataType.InterfaceEntry] = TargetTestHelpers.CreateTypeInfo(builtInCom.InterfaceEntryLayout),
            [DataType.CtxEntry] = TargetTestHelpers.CreateTypeInfo(builtInCom.CtxEntryLayout),
            [DataType.RCW] = TargetTestHelpers.CreateTypeInfo(builtInCom.RCWLayout),
        };

    private static (string Name, ulong Value)[] CreateContractGlobals(MockBuiltInComBuilder builtInCom)
        =>
        [
            (Constants.Globals.CCWNumInterfaces, builtInCom.CCWNumInterfaces),
            (Constants.Globals.CCWThisMask, builtInCom.CCWThisMask),
            (Constants.Globals.TearOffAddRef, builtInCom.TearOffAddRefGlobalAddress),
            (Constants.Globals.TearOffAddRefSimple, builtInCom.TearOffAddRefSimpleGlobalAddress),
            (Constants.Globals.TearOffAddRefSimpleInner, builtInCom.TearOffAddRefSimpleInnerGlobalAddress),
            (Constants.Globals.RCWInterfaceCacheSize, MockRCW.InterfaceEntryCacheSize),
        ];

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_ReturnsRefCountMasked(MockTarget.Architecture arch)
    {
        ulong rawRefCount = 0x0000_0000_1234_5678UL | CleanupSentinel;
        ulong wrapperAddress = 0;
        ulong comRefcountMask = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            comRefcountMask = builtInCom.ComRefcountMask;

            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.RefCount = rawRefCount;
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;
            wrapperAddress = wrapper.Address;
        });

        SimpleComCallWrapperData sccwData = contract.GetSimpleComCallWrapperData(new TargetPointer(wrapperAddress));
        Assert.Equal(rawRefCount & comRefcountMask, sccwData.RefCount);
        Assert.True(sccwData.IsNeutered);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_WeakFlagSet_ReturnsTrue(MockTarget.Architecture arch)
    {
        ulong wrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.Flags = IsHandleWeakFlag;
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;
            wrapperAddress = wrapper.Address;
        });

        SimpleComCallWrapperData sccwData = contract.GetSimpleComCallWrapperData(new TargetPointer(wrapperAddress));
        Assert.True(sccwData.IsHandleWeak);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_WeakFlagNotSet_ReturnsFalse(MockTarget.Architecture arch)
    {
        ulong wrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;
            wrapperAddress = wrapper.Address;
        });

        SimpleComCallWrapperData sccwData = contract.GetSimpleComCallWrapperData(new TargetPointer(wrapperAddress));
        Assert.False(sccwData.IsHandleWeak);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_AggregatedFlagSet_OnlySetsAggregated(MockTarget.Architecture arch)
    {
        ulong wrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.Flags = IsAggregatedFlag;
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;
            wrapperAddress = wrapper.Address;
        });

        SimpleComCallWrapperData sccwData = contract.GetSimpleComCallWrapperData(new TargetPointer(wrapperAddress));
        Assert.True(sccwData.IsAggregated);
        Assert.False(sccwData.IsExtendsCOMObject);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_ExtendsCOMObjectFlagSet_OnlySetsExtendsCOMObject(MockTarget.Architecture arch)
    {
        ulong wrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.Flags = IsExtendsComFlag;
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;
            wrapperAddress = wrapper.Address;
        });

        SimpleComCallWrapperData sccwData = contract.GetSimpleComCallWrapperData(new TargetPointer(wrapperAddress));
        Assert.False(sccwData.IsAggregated);
        Assert.True(sccwData.IsExtendsCOMObject);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_ReturnsAllFields(MockTarget.Architecture arch)
    {
        const ulong ExpectedOuterIUnknown = 0xBBBB_0000;
        const ulong RawRefCount = 3;
        ulong wrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.OuterIUnknown = ExpectedOuterIUnknown;
            simpleWrapper.RefCount = RawRefCount;
            simpleWrapper.Flags = IsAggregatedFlag | IsExtendsComFlag | IsHandleWeakFlag;
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;
            wrapperAddress = wrapper.Address;
        });

        SimpleComCallWrapperData sccwData = contract.GetSimpleComCallWrapperData(new TargetPointer(wrapperAddress));
        Assert.Equal(RawRefCount, sccwData.RefCount);
        Assert.False(sccwData.IsNeutered);
        Assert.True(sccwData.IsAggregated);
        Assert.True(sccwData.IsExtendsCOMObject);
        Assert.True(sccwData.IsHandleWeak);
        Assert.Equal(ExpectedOuterIUnknown, sccwData.OuterIUnknown.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetSimpleComCallWrapperData_ZeroFields_AreNull(MockTarget.Architecture arch)
    {
        ulong wrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;
            wrapperAddress = wrapper.Address;
        });

        SimpleComCallWrapperData sccwData = contract.GetSimpleComCallWrapperData(new TargetPointer(wrapperAddress));
        Assert.Equal(0UL, sccwData.RefCount);
        Assert.False(sccwData.IsNeutered);
        Assert.False(sccwData.IsAggregated);
        Assert.False(sccwData.IsExtendsCOMObject);
        Assert.False(sccwData.IsHandleWeak);
        Assert.Equal(TargetPointer.Null, sccwData.OuterIUnknown);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectHandle_ReturnsHandleFromWrapper(MockTarget.Architecture arch)
    {
        const ulong ExpectedHandle = 0x1234_5678;
        ulong wrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.Handle = ExpectedHandle;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;
            wrapperAddress = wrapper.Address;
        });

        TargetPointer handle = contract.GetObjectHandle(new TargetPointer(wrapperAddress));
        Assert.Equal(ExpectedHandle, handle.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectHandle_NullHandle_ReturnsNull(MockTarget.Architecture arch)
    {
        ulong wrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;
            wrapperAddress = wrapper.Address;
        });

        TargetPointer handle = contract.GetObjectHandle(new TargetPointer(wrapperAddress));
        Assert.Equal(TargetPointer.Null, handle);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetStartWrapper_SingleWrapper_ReturnsSelf(MockTarget.Architecture arch)
    {
        ulong wrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.MainWrapper = wrapper.Address;
            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapperAddress = wrapper.Address;
        });

        Assert.Equal(wrapperAddress, contract.GetStartWrapper(new TargetPointer(wrapperAddress)).Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetStartWrapper_LinkedWrapper_NavigatesToStart(MockTarget.Architecture arch)
    {
        ulong startWrapperAddress = 0;
        ulong linkedWrapperAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper startWrapper = builtInCom.AddComCallWrapper();
            MockComCallWrapper linkedWrapper = builtInCom.AddComCallWrapper();
            simpleWrapper.MainWrapper = startWrapper.Address;

            startWrapper.SimpleWrapper = simpleWrapper.Address;
            startWrapper.Next = linkedWrapper.Address;

            linkedWrapper.SimpleWrapper = simpleWrapper.Address;
            linkedWrapper.Next = LinkedWrapperTerminator;

            startWrapperAddress = startWrapper.Address;
            linkedWrapperAddress = linkedWrapper.Address;
        });

        Assert.Equal(startWrapperAddress, contract.GetStartWrapper(new TargetPointer(startWrapperAddress)).Value);
        Assert.Equal(startWrapperAddress, contract.GetStartWrapper(new TargetPointer(linkedWrapperAddress)).Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWInterfaces_SingleWrapper_SkipsNullAndIncompleteSlots(MockTarget.Architecture arch)
    {
        ulong expectedMethodTable2 = 0xdead_0002;
        int pointerSize = arch.Is64Bit ? sizeof(ulong) : sizeof(uint);
        ulong wrapperAddress = 0;
        ulong interfacePointerAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            MockComMethodTable cmt0 = builtInCom.AddComMethodTable();
            MockComMethodTable cmt1 = builtInCom.AddComMethodTable();
            MockComMethodTable cmt2 = builtInCom.AddComMethodTable();

            simpleWrapper.MainWrapper = wrapper.Address;

            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.InterfacePointers[0] = cmt0.VTable;
            wrapper.InterfacePointers[1] = cmt1.VTable;
            wrapper.InterfacePointers[2] = cmt2.VTable;
            wrapper.InterfacePointers[3] = 0;
            wrapper.InterfacePointers[4] = 0;
            wrapper.Next = LinkedWrapperTerminator;

            cmt0.Flags = IsLayoutCompleteFlag;
            cmt0.MethodTable = 0;
            cmt1.Flags = 0;
            cmt2.Flags = IsLayoutCompleteFlag;
            cmt2.MethodTable = expectedMethodTable2;

            wrapperAddress = wrapper.Address;
            interfacePointerAddress = wrapper.InterfacePointerAddress;
        });

        List<COMInterfacePointerData> interfaces =
            contract.GetCCWInterfaces(new TargetPointer(wrapperAddress)).ToList();

        // Only slot 0 and slot 2 appear: slot 1 is incomplete, slots 3/4 are null
        Assert.Equal(2, interfaces.Count);

        // Slot 0: IUnknown (first wrapper, index 0) => MethodTable = Null
        Assert.Equal(interfacePointerAddress, interfaces[0].InterfacePointerAddress.Value);
        Assert.Equal(TargetPointer.Null.Value, interfaces[0].MethodTable.Value);

        // Slot 2: at offset 3*P from CCW base (IPtr + 2*P)
        Assert.Equal(interfacePointerAddress + (ulong)(2 * pointerSize), interfaces[1].InterfacePointerAddress.Value);
        Assert.Equal(expectedMethodTable2, interfaces[1].MethodTable.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWInterfaces_MultipleWrappers_WalksChain(MockTarget.Architecture arch)
    {
        ulong expectedMT_slot0 = 0xbbbb_0000;
        ulong expectedMT_slot2 = 0xcccc_0002;
        int pointerSize = arch.Is64Bit ? sizeof(ulong) : sizeof(uint);
        ulong wrapper1Address = 0;
        ulong wrapper1InterfacePointerAddress = 0;
        ulong wrapper2InterfacePointerAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper1 = builtInCom.AddComCallWrapper();
            MockComCallWrapper wrapper2 = builtInCom.AddComCallWrapper();
            MockComMethodTable cmt1_0 = builtInCom.AddComMethodTable();
            MockComMethodTable cmt2_0 = builtInCom.AddComMethodTable();
            MockComMethodTable cmt2_2 = builtInCom.AddComMethodTable();

            simpleWrapper.MainWrapper = wrapper1.Address;

            wrapper1.SimpleWrapper = simpleWrapper.Address;
            wrapper1.InterfacePointers[0] = cmt1_0.VTable;
            wrapper1.InterfacePointers[1] = 0;
            wrapper1.InterfacePointers[2] = 0;
            wrapper1.InterfacePointers[3] = 0;
            wrapper1.InterfacePointers[4] = 0;
            wrapper1.Next = wrapper2.Address;

            wrapper2.SimpleWrapper = simpleWrapper.Address;
            wrapper2.InterfacePointers[0] = cmt2_0.VTable;
            wrapper2.InterfacePointers[1] = 0;
            wrapper2.InterfacePointers[2] = cmt2_2.VTable;
            wrapper2.InterfacePointers[3] = 0;
            wrapper2.InterfacePointers[4] = 0;
            wrapper2.Next = LinkedWrapperTerminator;

            cmt1_0.Flags = IsLayoutCompleteFlag;
            cmt1_0.MethodTable = 0;
            cmt2_0.Flags = IsLayoutCompleteFlag;
            cmt2_0.MethodTable = expectedMT_slot0;
            cmt2_2.Flags = IsLayoutCompleteFlag;
            cmt2_2.MethodTable = expectedMT_slot2;

            wrapper1Address = wrapper1.Address;
            wrapper1InterfacePointerAddress = wrapper1.InterfacePointerAddress;
            wrapper2InterfacePointerAddress = wrapper2.InterfacePointerAddress;
        });

        List<COMInterfacePointerData> interfaces =
            contract.GetCCWInterfaces(new TargetPointer(wrapper1Address)).ToList();

        // 3 interfaces: ccw1 slot0 (IUnknown), ccw2 slot0 (IClassX), ccw2 slot2 (interface)
        Assert.Equal(3, interfaces.Count);

        // First wrapper, slot 0: IUnknown => MethodTable = Null
        Assert.Equal(wrapper1InterfacePointerAddress, interfaces[0].InterfacePointerAddress.Value);
        Assert.Equal(TargetPointer.Null.Value, interfaces[0].MethodTable.Value);

        // Second wrapper, slot 0: IClassX - has a MethodTable (not first wrapper)
        Assert.Equal(wrapper2InterfacePointerAddress, interfaces[1].InterfacePointerAddress.Value);
        Assert.Equal(expectedMT_slot0, interfaces[1].MethodTable.Value);

        // Second wrapper, slot 2
        Assert.Equal(wrapper2InterfacePointerAddress + (ulong)(2 * pointerSize), interfaces[2].InterfacePointerAddress.Value);
        Assert.Equal(expectedMT_slot2, interfaces[2].MethodTable.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWInterfaces_LinkedWrapper_WalksFullChainFromAnyWrapper(MockTarget.Architecture arch)
    {
        ulong expectedMT = 0xaaaa_0001;
        int pointerSize = arch.Is64Bit ? sizeof(ulong) : sizeof(uint);
        ulong wrapper1Address = 0;
        ulong wrapper2Address = 0;
        ulong wrapper1InterfacePointerAddress = 0;
        ulong wrapper2InterfacePointerAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper1 = builtInCom.AddComCallWrapper();
            MockComCallWrapper wrapper2 = builtInCom.AddComCallWrapper();
            MockComMethodTable cmt1 = builtInCom.AddComMethodTable();
            MockComMethodTable cmt2 = builtInCom.AddComMethodTable();

            simpleWrapper.MainWrapper = wrapper1.Address;

            wrapper1.SimpleWrapper = simpleWrapper.Address;
            wrapper1.InterfacePointers[0] = cmt1.VTable;
            wrapper1.InterfacePointers[1] = 0;
            wrapper1.InterfacePointers[2] = 0;
            wrapper1.InterfacePointers[3] = 0;
            wrapper1.InterfacePointers[4] = 0;
            wrapper1.Next = wrapper2.Address;

            wrapper2.SimpleWrapper = simpleWrapper.Address;
            wrapper2.InterfacePointers[0] = 0;
            wrapper2.InterfacePointers[1] = cmt2.VTable;
            wrapper2.InterfacePointers[2] = 0;
            wrapper2.InterfacePointers[3] = 0;
            wrapper2.InterfacePointers[4] = 0;
            wrapper2.Next = LinkedWrapperTerminator;

            cmt1.Flags = IsLayoutCompleteFlag;
            cmt1.MethodTable = 0;
            cmt2.Flags = IsLayoutCompleteFlag;
            cmt2.MethodTable = expectedMT;

            wrapper1Address = wrapper1.Address;
            wrapper2Address = wrapper2.Address;
            wrapper1InterfacePointerAddress = wrapper1.InterfacePointerAddress;
            wrapper2InterfacePointerAddress = wrapper2.InterfacePointerAddress;
        });

        // Passing the start CCW enumerates both wrappers' interfaces.
        List<COMInterfacePointerData> interfacesFromStart =
            contract.GetCCWInterfaces(new TargetPointer(wrapper1Address)).ToList();

        Assert.Equal(2, interfacesFromStart.Count);
        // ccw1 slot 0: IUnknown → MethodTable = Null (first wrapper, slot 0)
        Assert.Equal(wrapper1InterfacePointerAddress, interfacesFromStart[0].InterfacePointerAddress.Value);
        Assert.Equal(TargetPointer.Null.Value, interfacesFromStart[0].MethodTable.Value);
        // ccw2 slot 1
        Assert.Equal(wrapper2InterfacePointerAddress + (ulong)pointerSize, interfacesFromStart[1].InterfacePointerAddress.Value);
        Assert.Equal(expectedMT, interfacesFromStart[1].MethodTable.Value);

        // Passing the second (non-start) CCW also navigates to the start and enumerates the full chain.
        List<COMInterfacePointerData> interfacesFromLinked =
            contract.GetCCWInterfaces(new TargetPointer(wrapper2Address)).ToList();

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

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockRCW rcw = builtInCom.AddRCW();
            for (int i = 0; i < expectedEntries.Length; i++)
            {
                rcw.InterfaceEntries[i].MethodTable = expectedEntries[i].MethodTable;
                rcw.InterfaceEntries[i].Unknown = expectedEntries[i].Unknown;
            }

            rcwAddress = rcw.Address;
        });

        Assert.NotNull(contract);

        List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
            contract.GetRCWInterfaces(rcwAddress).ToList();

        Assert.Equal(expectedEntries.Length, results.Count);
        for (int i = 0; i < expectedEntries.Length; i++)
        {
            Assert.Equal(expectedEntries[i].MethodTable, results[i].MethodTable);
            Assert.Equal(expectedEntries[i].Unknown, results[i].Unknown);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWInterfaces_ComIpAddress_ResolvesToCCW(MockTarget.Architecture arch)
    {
        ulong ccwAddress = 0;
        ulong comIPAddr = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();

            simpleWrapper.MainWrapper = wrapper.Address;
            MockComMethodTable cmt = builtInCom.AddComMethodTable(vtableSlots: 2);

            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.InterfacePointers[0] = cmt.VTable;
            wrapper.InterfacePointers[1] = 0;
            wrapper.InterfacePointers[2] = 0;
            wrapper.InterfacePointers[3] = 0;
            wrapper.InterfacePointers[4] = 0;
            wrapper.Next = LinkedWrapperTerminator;

            cmt.Flags = IsLayoutCompleteFlag;
            cmt.MethodTable = 0;
            cmt.SetVTableSlot(0, 0);
            cmt.SetVTableSlot(1, builtInCom.TearOffAddRefAddress);

            ccwAddress = wrapper.Address;
            comIPAddr = wrapper.InterfacePointerAddress;
        }, minAlign: arch.Is64Bit ? 64 : 32);

        TargetPointer startCCWFromIP = contract.GetCCWFromInterfacePointer(new TargetPointer(comIPAddr));
        Assert.Equal(ccwAddress, startCCWFromIP.Value);

        TargetPointer nullResult = contract.GetCCWFromInterfacePointer(new TargetPointer(ccwAddress));
        Assert.Equal(TargetPointer.Null, nullResult);

        List<COMInterfacePointerData> ifacesDirect =
            contract.GetCCWInterfaces(new TargetPointer(ccwAddress)).ToList();
        List<COMInterfacePointerData> ifacesFromIP =
            contract.GetCCWInterfaces(startCCWFromIP).ToList();

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

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockRCW rcw = builtInCom.AddRCW();
            for (int i = 0; i < entries.Length; i++)
            {
                rcw.InterfaceEntries[i].MethodTable = entries[i].MethodTable;
                rcw.InterfaceEntries[i].Unknown = entries[i].Unknown;
            }

            rcwAddress = rcw.Address;
        });

        List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
            contract.GetRCWInterfaces(rcwAddress).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(new TargetPointer(0x1000), results[0].MethodTable);
        Assert.Equal(new TargetPointer(0x2000), results[0].Unknown);
        Assert.Equal(new TargetPointer(0x5000), results[1].MethodTable);
        Assert.Equal(new TargetPointer(0x6000), results[1].Unknown);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWInterfaces_EmptyCache_ReturnsEmpty(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            rcwAddress = builtInCom.AddRCW().Address;
        });

        List<(TargetPointer MethodTable, TargetPointer Unknown)> results =
            contract.GetRCWInterfaces(rcwAddress).ToList();

        Assert.Empty(results);
    }

    // Bit-flag constants mirroring BuiltInCOM_1 internal constants, used to construct Flags for GetRCWData tests.
    private const uint RCWFlagAggregated   = 0x10u;   // URTAggregatedMask
    private const uint RCWFlagContained    = 0x20u;   // URTContainedMask
    private const uint RCWFlagFreeThreaded = 0x100u;  // MarshalingTypeFreeThreadedValue

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

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockRCW rcw = builtInCom.AddRCW();
            rcw.IdentityPointer = expectedIdentity;
            rcw.VTablePtr = expectedVTable;
            rcw.CreatorThread = expectedThread;
            rcw.CtxCookie = expectedCookie;
            rcw.RefCount = expectedRefCount;
            rcwAddress = rcw.Address;
        });

        RCWData result = contract.GetRCWData(rcwAddress);

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
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_FlagsAggregatedAndContained(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockRCW rcw = builtInCom.AddRCW();
            rcw.Flags = RCWFlagAggregated | RCWFlagContained;
            rcwAddress = rcw.Address;
        });

        RCWData result = contract.GetRCWData(rcwAddress);

        Assert.True(result.IsAggregated);
        Assert.True(result.IsContained);
        Assert.False(result.IsFreeThreaded);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_FlagsFreeThreaded(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockRCW rcw = builtInCom.AddRCW();
            rcw.Flags = RCWFlagFreeThreaded;
            rcwAddress = rcw.Address;
        });

        RCWData result = contract.GetRCWData(rcwAddress);

        Assert.True(result.IsFreeThreaded);
        Assert.False(result.IsAggregated);
        Assert.False(result.IsContained);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_IsDisconnected_Sentinel(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        const ulong DisconnectedSentinel = 0xBADF00D;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockRCW rcw = builtInCom.AddRCW();
            rcw.UnknownPointer = new TargetPointer(DisconnectedSentinel);
            rcwAddress = rcw.Address;
        });

        RCWData result = contract.GetRCWData(rcwAddress);

        Assert.True(result.IsDisconnected);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWData_IsDisconnected_CtxCookieMismatch(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            TargetPointer ctxCookieInEntry = new TargetPointer(0xAAAA_0000);
            ulong ctxEntryAddress = builtInCom.AddCtxEntry(ctxCookie: ctxCookieInEntry);

            TargetPointer ctxCookieInRcw = new TargetPointer(0xBBBB_0000);
            MockRCW rcw = builtInCom.AddRCW();
            rcw.CtxCookie = ctxCookieInRcw;
            rcw.CtxEntry = ctxEntryAddress;
            rcwAddress = rcw.Address;
        });

        RCWData result = contract.GetRCWData(rcwAddress);

        Assert.True(result.IsDisconnected);
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

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockRCW rcw = builtInCom.AddRCW();
            rcw.SyncBlockIndex = syncBlockIndex;
            rcwAddress = rcw.Address;
        }, syncBlock: mockSyncBlock.Object);

        RCWData result = contract.GetRCWData(rcwAddress);

        Assert.Equal(expectedManagedObject, result.ManagedObject);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetRCWContext_ReturnsCtxCookie(MockTarget.Architecture arch)
    {
        TargetPointer rcwAddress = default;
        TargetPointer expectedCookie = new TargetPointer(0xC00C_1E00);

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockRCW rcw = builtInCom.AddRCW();
            rcw.CtxCookie = expectedCookie;
            rcwAddress = rcw.Address;
        });

        TargetPointer result = contract.GetRCWContext(rcwAddress);

        Assert.Equal(expectedCookie, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetCCWFromInterfacePointer_SCCWIp_ResolvesToStartCCW(MockTarget.Architecture arch)
    {
        ulong sccwIP = 0;
        ulong ccwAddress = 0;

        IBuiltInCOM contract = CreateBuiltInCOM(arch, builtInCom =>
        {
            MockSimpleComCallWrapper simpleWrapper = builtInCom.AddSimpleComCallWrapper();
            MockComCallWrapper wrapper = builtInCom.AddComCallWrapper();
            const int InterfaceKind = 1;
            int pointerSize = arch.Is64Bit ? sizeof(ulong) : sizeof(uint);
            MockStdInterfaceDesc stdInterfaceDesc = builtInCom.AddStdInterfaceDesc(vtableSlots: 2);

            simpleWrapper.MainWrapper = wrapper.Address;
            simpleWrapper.VTablePointers[0] = 0;
            simpleWrapper.VTablePointers[1] = stdInterfaceDesc.VTable;

            stdInterfaceDesc.StdInterfaceKind = InterfaceKind;
            stdInterfaceDesc.SetVTableSlot(0, 0);
            stdInterfaceDesc.SetVTableSlot(1, builtInCom.TearOffAddRefSimpleAddress);

            wrapper.SimpleWrapper = simpleWrapper.Address;
            wrapper.Next = LinkedWrapperTerminator;

            sccwIP = simpleWrapper.VTablePointerAddress + (ulong)(InterfaceKind * pointerSize);
            ccwAddress = wrapper.Address;
        });

        TargetPointer startCCW = contract.GetCCWFromInterfacePointer(new TargetPointer(sccwIP));
        Assert.Equal(ccwAddress, startCCW.Value);
    }
}
