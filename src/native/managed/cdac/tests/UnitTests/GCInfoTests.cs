// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers.X86;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class GCInfoTests
{
    private const ulong MethodIP = 0x10000;

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void QueryInterfaceFromIXCLRDataProcess_ExposesGCInfo(MockTarget.Architecture arch)
    {
        ISOSDacInterface18 sos = CreateGCInfoInterface(arch, CodeKind.Jitted);
        void* process = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(Assert.IsAssignableFrom<IXCLRDataProcess>(sos));
        try
        {
            Guid iid = typeof(ISOSDacInterface18).GUID;
            Assert.Equal(HResults.S_OK, Marshal.QueryInterface((nint)process, in iid, out nint gcInfo));
            try
            {
                void** vtable = *(void***)gcInfo;
                SOSGCInfoHeader header = new() { SizeOf = (uint)sizeof(SOSGCInfoHeader) };
                int hr = ((delegate* unmanaged[MemberFunction]<nint, ClrDataAddress, SOSGCInfoHeader*, int>)vtable[3])(gcInfo, MethodIP, &header);
                Assert.Equal(HResults.S_OK, hr);
                Assert.Equal(6u, header.GcInfoVersion);
                Assert.Equal(128u, header.CodeSize);

                uint needed = uint.MaxValue;
                SOSCodeRange* ranges = stackalloc SOSCodeRange[2];
                hr = ((delegate* unmanaged[MemberFunction]<nint, ClrDataAddress, uint, SOSCodeRange*, uint*, int>)vtable[4])(gcInfo, MethodIP, 2, ranges, &needed);
                Assert.Equal(HResults.S_OK, hr);
                Assert.Equal(2u, needed);
                Assert.Equal(4u, ranges[0].BeginOffset);
                Assert.Equal(20u, ranges[1].EndOffset);

                uint* offsets = stackalloc uint[2];
                hr = ((delegate* unmanaged[MemberFunction]<nint, ClrDataAddress, uint, uint*, uint*, int>)vtable[5])(gcInfo, MethodIP, 2, offsets, &needed);
                Assert.Equal(HResults.S_OK, hr);
                Assert.Equal(2u, needed);
                Assert.Equal(8u, offsets[0]);
                Assert.Equal(24u, offsets[1]);

                SOSGCSlotLifetime* lifetimes = stackalloc SOSGCSlotLifetime[3];
                hr = ((delegate* unmanaged[MemberFunction]<nint, ClrDataAddress, uint, SOSGCSlotLifetime*, uint*, int>)vtable[6])(gcInfo, MethodIP, 3, lifetimes, &needed);
                Assert.Equal(HResults.S_OK, hr);
                Assert.Equal(3u, needed);
                Assert.Equal(1, lifetimes[0].IsRegister);
                Assert.Equal(-24, lifetimes[1].SpOffset);
                Assert.Equal(1u, lifetimes[1].BaseRegister);
                Assert.Equal(0u, lifetimes[2].BaseRegister);
            }
            finally
            {
                Marshal.Release(gcInfo);
            }
        }
        finally
        {
            ComInterfaceMarshaller<IXCLRDataProcess>.Free(process);
        }
    }

    [Theory]
    [InlineData(CodeKind.Jitted, GenericsContextKind.None)]
    [InlineData(CodeKind.ReadyToRun, GenericsContextKind.This)]
    [InlineData(CodeKind.Interpreter, GenericsContextKind.MethodDesc)]
    [InlineData(CodeKind.Interpreter, GenericsContextKind.MethodHandle)]
    public void GetGCInfoHeader_MapsFieldsAndSelectsDecoder(CodeKind codeKind, GenericsContextKind contextKind)
    {
        bool hasSpecialSlots = contextKind != GenericsContextKind.None;
        MockTarget.Architecture arch = new() { Is64Bit = true, IsLittleEndian = true };
        ISOSDacInterface18 sos = CreateGCInfoInterface(arch, codeKind, (_, gcInfo, handle) =>
        {
            gcInfo.Setup(g => g.GetHeader(handle)).Returns(new GCInfoHeader(
                Version: 6, CodeSize: 128, PrologSize: 3, StackBaseRegister: 5, SizeOfStackParameterArea: 32,
                IsVarArg: hasSpecialSlots, WantsReportOnlyLeaf: !hasSpecialSlots, HasTailCalls: hasSpecialSlots,
                GSCookie: hasSpecialSlots ? new SpecialSlot(-16) : null,
                GSCookieValidRangeStart: hasSpecialSlots ? 3u : 0u,
                GSCookieValidRangeEnd: hasSpecialSlots ? 120u : 0u,
                PSPSym: hasSpecialSlots ? new SpecialSlot(0) : null,
                GenericsInstContext: hasSpecialSlots ? new SpecialSlot(-32) : null,
                GenericsInstContextKind: contextKind));
        });

        SOSGCInfoHeader header = new() { SizeOf = (uint)sizeof(SOSGCInfoHeader) };
        Assert.Equal(HResults.S_OK, sos.GetGCInfoHeader(MethodIP, &header));
        Assert.Equal((uint)sizeof(SOSGCInfoHeader), header.SizeOf);
        Assert.Equal(6u, header.GcInfoVersion);
        Assert.Equal(128u, header.CodeSize);
        Assert.Equal(3u, header.PrologSize);
        Assert.Equal(5u, header.StackBaseRegister);
        Assert.Equal(32u, header.SizeOfStackParameterArea);
        Assert.Equal(hasSpecialSlots ? 1 : 0, header.IsVarArg);
        Assert.Equal(hasSpecialSlots ? 0 : 1, header.WantsReportOnlyLeaf);
        Assert.Equal(hasSpecialSlots ? 1 : 0, header.HasTailCalls);
        Assert.Equal(hasSpecialSlots ? 1 : 0, header.GSCookieIsPresent);
        Assert.Equal(hasSpecialSlots ? -16 : 0, header.GSCookieStackSlot);
        Assert.Equal(hasSpecialSlots ? 3u : 0u, header.GSCookieValidRangeStart);
        Assert.Equal(hasSpecialSlots ? 120u : 0u, header.GSCookieValidRangeEnd);
        Assert.Equal(hasSpecialSlots ? 1 : 0, header.PSPSymIsPresent);
        Assert.Equal(0, header.PSPSymStackSlot);
        Assert.Equal(hasSpecialSlots ? 1 : 0, header.GenericsInstContextIsPresent);
        Assert.Equal(hasSpecialSlots ? -32 : 0, header.GenericsInstContextStackSlot);
        Assert.Equal(contextKind == GenericsContextKind.MethodDesc ? 1u :
            contextKind == GenericsContextKind.MethodHandle ? 2u : 0u, header.GenericsInstContextKind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void GetGCInfoTables_RespectBufferSizes(int count)
    {
        MockTarget.Architecture arch = new() { Is64Bit = false, IsLittleEndian = false };
        ISOSDacInterface18 sos = CreateGCInfoInterface(arch, CodeKind.Interpreter);
        SOSCodeRange[]? ranges = count == 0 ? null : new SOSCodeRange[count];
        uint[]? offsets = count == 0 ? null : new uint[count];
        SOSGCSlotLifetime[]? lifetimes = count == 0 ? null : new SOSGCSlotLifetime[count];
        uint needed = uint.MaxValue;

        Assert.Equal(count < 2 ? HResults.S_FALSE : HResults.S_OK,
            sos.GetGCInfoInterruptibleRanges(MethodIP, (uint)count, ranges, &needed));
        Assert.Equal(2u, needed);
        Assert.Equal(count < 2 ? HResults.S_FALSE : HResults.S_OK,
            sos.GetGCInfoSafePoints(MethodIP, (uint)count, offsets, &needed));
        Assert.Equal(2u, needed);
        Assert.Equal(count < 3 ? HResults.S_FALSE : HResults.S_OK,
            sos.GetGCInfoSlotLifetimes(MethodIP, (uint)count, lifetimes, &needed));
        Assert.Equal(3u, needed);

        if (count >= 2)
        {
            Assert.NotNull(ranges);
            Assert.NotNull(offsets);
            Assert.Equal(new SOSCodeRange { BeginOffset = 4, EndOffset = 12 }, ranges[0]);
            Assert.Equal(new SOSCodeRange { BeginOffset = 16, EndOffset = 20 }, ranges[1]);
            Assert.Equal(8u, offsets[0]);
            Assert.Equal(24u, offsets[1]);
        }
        if (count >= 3)
        {
            Assert.NotNull(lifetimes);
            Assert.Equal(new SOSGCSlotLifetime { BeginOffset = 4, EndOffset = 8, IsRegister = 1, RegisterNumber = 3, GcFlags = 3 }, lifetimes[0]);
            Assert.Equal(new SOSGCSlotLifetime { BeginOffset = 0, EndOffset = 128, SpOffset = -24, BaseRegister = 1, GcFlags = 6 }, lifetimes[1]);
            Assert.Equal(new SOSGCSlotLifetime { BeginOffset = 16, EndOffset = 20, SpOffset = 40, BaseRegister = 0, GcFlags = 1 }, lifetimes[2]);
        }
        if (ranges is not null)
            Assert.All(ranges.AsSpan(count >= 2 ? 2 : 0).ToArray(), value => Assert.Equal(default, value));
        if (offsets is not null)
            Assert.All(offsets.AsSpan(count >= 2 ? 2 : 0).ToArray(), value => Assert.Equal(0u, value));
        if (lifetimes is not null)
            Assert.All(lifetimes.AsSpan(count >= 3 ? 3 : 0).ToArray(), value => Assert.Equal(default, value));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCInfoTables_EmptyResults(MockTarget.Architecture arch)
    {
        ISOSDacInterface18 sos = CreateGCInfoInterface(arch, CodeKind.ReadyToRun, (_, gcInfo, handle) =>
        {
            gcInfo.Setup(g => g.GetInterruptibleRanges(handle)).Returns(Array.Empty<InterruptibleRange>());
            gcInfo.Setup(g => g.GetSafePoints(handle)).Returns(Array.Empty<uint>());
            gcInfo.Setup(g => g.GetSlotLifetimes(handle)).Returns(Array.Empty<GCSlotLifetime>());
        });
        uint needed = uint.MaxValue;
        Assert.Equal(HResults.S_OK, sos.GetGCInfoInterruptibleRanges(MethodIP, 0, null, &needed));
        Assert.Equal(0u, needed);
        needed = uint.MaxValue;
        Assert.Equal(HResults.S_OK, sos.GetGCInfoSafePoints(MethodIP, 0, null, &needed));
        Assert.Equal(0u, needed);
        needed = uint.MaxValue;
        Assert.Equal(HResults.S_OK, sos.GetGCInfoSlotLifetimes(MethodIP, 0, null, &needed));
        Assert.Equal(0u, needed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetGCInfoHeader_RejectsInvalidBuffers(bool almostLargeEnough)
    {
        MockTarget.Architecture arch = new() { Is64Bit = true, IsLittleEndian = true };
        ISOSDacInterface18 sos = CreateGCInfoInterface(arch, CodeKind.Jitted);
        Assert.Equal(HResults.E_POINTER, sos.GetGCInfoHeader(MethodIP, null));
        uint size = almostLargeEnough ? (uint)sizeof(SOSGCInfoHeader) - 1 : 0;
        SOSGCInfoHeader header = new() { SizeOf = size, CodeSize = uint.MaxValue };
        Assert.Equal(HResults.E_INVALIDARG, sos.GetGCInfoHeader(MethodIP, &header));
        Assert.Equal(size, header.SizeOf);
        Assert.Equal(uint.MaxValue, header.CodeSize);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetGCInfoTables_RejectInvalidBuffers(MockTarget.Architecture arch)
    {
        ISOSDacInterface18 sos = CreateGCInfoInterface(arch, CodeKind.Jitted);
        Assert.Equal(HResults.E_POINTER, sos.GetGCInfoInterruptibleRanges(MethodIP, 0, null, null));
        Assert.Equal(HResults.E_POINTER, sos.GetGCInfoSafePoints(MethodIP, 0, null, null));
        Assert.Equal(HResults.E_POINTER, sos.GetGCInfoSlotLifetimes(MethodIP, 0, null, null));

        uint needed = uint.MaxValue;
        Assert.Equal(HResults.E_INVALIDARG, sos.GetGCInfoInterruptibleRanges(MethodIP, 1, null, &needed));
        Assert.Equal(0u, needed);
        needed = uint.MaxValue;
        Assert.Equal(HResults.E_INVALIDARG, sos.GetGCInfoSafePoints(MethodIP, 1, null, &needed));
        Assert.Equal(0u, needed);
        needed = uint.MaxValue;
        Assert.Equal(HResults.E_INVALIDARG, sos.GetGCInfoSlotLifetimes(MethodIP, 1, null, &needed));
        Assert.Equal(0u, needed);

        Assert.Equal(HResults.E_INVALIDARG, sos.GetGCInfoInterruptibleRanges(MethodIP, 2, new SOSCodeRange[1], &needed));
        Assert.Equal(HResults.E_INVALIDARG, sos.GetGCInfoSafePoints(MethodIP, 2, new uint[1], &needed));
        Assert.Equal(HResults.E_INVALIDARG, sos.GetGCInfoSlotLifetimes(MethodIP, 2, new SOSGCSlotLifetime[1], &needed));
    }

    [Theory]
    [InlineData(0ul, HResults.E_INVALIDARG)]
    [InlineData(MethodIP + 1, HResults.E_INVALIDARG)]
    [InlineData(MethodIP, HResults.E_FAIL)]
    public void GetGCInfo_InitializesOutputsOnFailure(ulong ip, int expectedHResult)
    {
        MockTarget.Architecture arch = new() { Is64Bit = true, IsLittleEndian = true };
        ISOSDacInterface18 sos = CreateGCInfoInterface(arch, CodeKind.Jitted, (executionManager, _, _) =>
        {
            executionManager.Setup(e => e.GetCodeBlockHandle(new TargetCodePointer(MethodIP + 1))).Returns((CodeBlockHandle?)null);
            executionManager.Setup(e => e.GetCodeBlockHandle(new TargetCodePointer(MethodIP)))
                .Throws(new COMException("Cannot read the method's code block.", HResults.E_FAIL));
        });
        SOSGCInfoHeader header = new() { SizeOf = (uint)sizeof(SOSGCInfoHeader), CodeSize = uint.MaxValue };
        Assert.Equal(expectedHResult, sos.GetGCInfoHeader(ip, &header));
        Assert.Equal((uint)sizeof(SOSGCInfoHeader), header.SizeOf);
        Assert.Equal(0u, header.CodeSize);
        Assert.Equal(0u, header.GcInfoVersion);

        uint needed = uint.MaxValue;
        Assert.Equal(expectedHResult, sos.GetGCInfoInterruptibleRanges(ip, 0, null, &needed));
        Assert.Equal(0u, needed);
        needed = uint.MaxValue;
        Assert.Equal(expectedHResult, sos.GetGCInfoSafePoints(ip, 0, null, &needed));
        Assert.Equal(0u, needed);
        needed = uint.MaxValue;
        Assert.Equal(expectedHResult, sos.GetGCInfoSlotLifetimes(ip, 0, null, &needed));
        Assert.Equal(0u, needed);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(3u)]
    public void GetGCInfoSlotLifetimes_RejectsUnsupportedStackBases(uint slotBase)
    {
        MockTarget.Architecture arch = new() { Is64Bit = true, IsLittleEndian = true };
        ISOSDacInterface18 sos = CreateGCInfoInterface(arch, CodeKind.Jitted, (_, gcInfo, handle) =>
        {
            gcInfo.Setup(g => g.GetSlotLifetimes(handle)).Returns(new GCSlotLifetime[]
            {
                new(false, 0, 8, slotBase, 0, 0, 128),
            });
        });
        SOSGCSlotLifetime[] result = [new() { SpOffset = 123 }];
        uint needed = 0;
        Assert.Equal(HResults.COR_E_INVALIDOPERATION, sos.GetGCInfoSlotLifetimes(MethodIP, 1, result, &needed));
        Assert.Equal(123, result[0].SpOffset);
    }

    [Theory]
    [InlineData(4u, false)]
    [InlineData(5u, false)]
    [InlineData(4u, true)]
    [InlineData(5u, true)]
    public void X86RegisterLifetimesPreserveLiveInteriorFlags(uint version, bool survives)
    {
        byte[] stream = survives ? [0xBF, 0x49, 0xFF] : [0xBF, 0x49, 0x0B, 0xFF];
        (IGCInfo gcInfo, IGCInfoHandle handle, _) = CreateX86GCInfo([64, 0x81, 0x40, .. stream], version);
        GCSlotLifetime slot = Assert.Single(gcInfo.GetSlotLifetimes(handle));
        Assert.Equal(new GCSlotLifetime(true, 1, 0, 0, 1, 1, survives ? 64u : 4u), slot);
    }

    [Theory]
    [InlineData(4u)]
    [InlineData(5u)]
    public void X86PointerLocationsFollowPushPopAndKill(uint version)
    {
        (IGCInfo gcInfo, IGCInfoHandle handle, _) = CreateX86GCInfo(
            [64, 0x81, 0x40, 0x81, 0xB1, 0xC9, 0xC9, 0xFF], version);
        IReadOnlyList<GCSlotLifetime> slots = gcInfo.GetSlotLifetimes(handle);
        Assert.Equal(new GCSlotLifetime[]
        {
            new(false, 0, 0, 1, 0, 1, 2),
            new(false, 0, 4, 1, 0, 2, 3),
            new(false, 0, 0, 1, 0, 3, 4),
        }, slots);

        (gcInfo, handle, _) = CreateX86GCInfo(
            [64, 0x81, 0x40, 0x81, 0xBF, 0x89, 0xB8, 1, 0xFD, 1, 0xFF], version);
        slots = gcInfo.GetSlotLifetimes(handle);
        Assert.Contains(new GCSlotLifetime(false, 0, 0, 1, 1, 2, 3), slots);
        Assert.Contains(new GCSlotLifetime(false, 0, 4, 1, 0, 2, 64), slots);
        Assert.Equal(4, Assert.Single(gcInfo.EnumerateLiveSlots(handle, 3, new() { IsActiveFrame = true })).SpOffset);
    }

    [Theory]
    [InlineData(4u, 64u)]
    [InlineData(5u, 64u)]
    [InlineData(4u, 65u)]
    [InlineData(5u, 65u)]
    public void X86RegisterDeathAtMethodEndClosesLifetime(uint version, uint endOffset)
    {
        (IGCInfo gcInfo, IGCInfoHandle handle, _) = CreateX86GCInfo(
            [64, 0x81, 0x40, 0xBF, 0x49, 0xB8, (byte)(endOffset - 2), 0x09, 0xFF], version);
        if (endOffset > 64)
        {
            Assert.Throws<BadImageFormatException>(() => gcInfo.GetSlotLifetimes(handle));
        }
        else
        {
            Assert.Equal(new GCSlotLifetime(true, 1, 0, 0, 1, 1, 64),
                Assert.Single(gcInfo.GetSlotLifetimes(handle)));
        }
    }

    [Theory]
    [InlineData(4u)]
    [InlineData(5u)]
    public void X86UntrackedLocationsAreSplitWithoutLosingFlags(uint version)
    {
        // Frame size 2, one untracked local at SP+0, then one non-pointer push/pop.
        (IGCInfo gcInfo, IGCInfoHandle handle, _) = CreateX86GCInfo(
            [64, 0x81, 0x82, 0xB8, 0x40, 0, 0xB1, 0xC9, 0xFF], version);
        Assert.Equal(new GCSlotLifetime[]
        {
            new(false, 0, 0, 1, 4, 0, 1),
            new(false, 0, 4, 1, 4, 1, 2),
            new(false, 0, 0, 1, 4, 2, 64),
        }, gcInfo.GetSlotLifetimes(handle));
    }

    [Theory]
    [InlineData(4u, false)]
    [InlineData(5u, false)]
    [InlineData(4u, true)]
    [InlineData(5u, true)]
    public void X86PoppingNonPointerArgumentPreservesPointerBelowIt(uint version, bool ebpFrame)
    {
        byte[] encoded = ebpFrame
            ? [64, 0x81, 0xBE, 0xBF, 0x40, 0x81, 0xB1, 0xC9, 0xC9, 0xFF]
            : [64, 0x81, 0x40, 0x81, 0xB1, 0xC9, 0xC9, 0xFF];
        (IGCInfo contract, IGCInfoHandle handle, _) = CreateX86GCInfo(encoded, version);
        IReadOnlyList<GCSlotLifetime> slots = contract.GetSlotLifetimes(handle);
        Assert.Equal(ebpFrame
            ? new GCSlotLifetime[] { new(false, 0, -4, 2, 0, 1, 4) }
            : new GCSlotLifetime[]
            {
                new(false, 0, 0, 1, 0, 1, 2),
                new(false, 0, 4, 1, 0, 2, 3),
                new(false, 0, 0, 1, 0, 3, 4),
            }, slots);
        IReadOnlyList<X86GCTransition> transitions = contract.GetX86Transitions(handle);
        Assert.Equal(2u, transitions[0].Flags);
        Assert.Equal(0u, transitions[1].Flags);
        Assert.Empty(contract.EnumerateLiveSlots(handle, 4, new() { IsActiveFrame = true }));
    }

    [Theory]
    [InlineData(4u, false)]
    [InlineData(5u, false)]
    [InlineData(4u, true)]
    [InlineData(5u, true)]
    public void X86BulkNonPointerStackAdjustmentPreservesPointerBelowIt(uint version, bool ebpFrame)
    {
        byte[] encoded = ebpFrame
            ? [64, 0x81, 0xBE, 0xBF, 0x40, 0x81, 0xF9, 3, 0xD9, 0xC9, 0xFF]
            : [64, 0x81, 0x40, 0x81, 0xF9, 3, 0xD9, 0xC9, 0xFF];
        (IGCInfo contract, IGCInfoHandle handle, _) = CreateX86GCInfo(encoded, version);
        Assert.Equal(ebpFrame
            ? new GCSlotLifetime[] { new(false, 0, -4, 2, 0, 1, 3) }
            : new GCSlotLifetime[]
            {
                new(false, 0, 12, 1, 0, 1, 2),
                new(false, 0, 0, 1, 0, 2, 3),
            }, contract.GetSlotLifetimes(handle));
        X86GCTransition adjustment = Assert.Single(contract.GetX86Transitions(handle),
            transition => transition.Kind == X86GCTransitionKind.StackDepth);
        Assert.Equal(1u, adjustment.CodeOffset);
        Assert.Equal(12, adjustment.StackDepthChange);
        Assert.Empty(contract.EnumerateLiveSlots(handle, 3, new() { IsActiveFrame = true }));
    }

    [Theory]
    [InlineData(4u)]
    [InlineData(5u)]
    public void X86CallSitesHaveRegisterAndArgumentRoots(uint version)
    {
        // EBP frame, one byref EDI and byref argument at SP+0 at offset 3.
        (_, _, SOSDacImpl impl) = CreateX86GCInfo(
            [64, 0x81, 0xBE, 0x3F, 0xF9, 3, 0x21, 0x21, 0xFF], version);
        ISOSDacInterface18 sos = impl;
        uint needed;
        Assert.Equal(HResults.S_OK, sos.GetGCInfoInterruptibleRanges(MethodIP, 0, null, &needed));
        Assert.Equal(0u, needed);
        uint[] safePoints = new uint[1];
        Assert.Equal(HResults.S_OK, sos.GetGCInfoSafePoints(MethodIP, 1, safePoints, &needed));
        Assert.Equal(3u, Assert.Single(safePoints));
        SOSGCSlotLifetime[] slots = new SOSGCSlotLifetime[2];
        Assert.Equal(HResults.S_OK, sos.GetGCInfoSlotLifetimes(MethodIP, 2, slots, &needed));
        Assert.Equal(2u, needed);
        Assert.Contains(slots, s => s.IsRegister == 1 && s.RegisterNumber == 7 && s.GcFlags == 1);
        Assert.Contains(slots, s => s.IsRegister == 0 && s.SpOffset == 0 && s.BaseRegister == 0 && s.GcFlags == 1);
        Assert.All(slots, s =>
        {
            Assert.Equal(3u, s.BeginOffset);
            Assert.Equal(4u, s.EndOffset);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void X86InterfaceExposesHeaderEpilogsAndTransitions(int count)
    {
        (_, _, SOSDacImpl impl) = CreateX86GCInfo([64, 0x81, 0x40, 0xB1, 0xC9, 0xFF], 5);
        ISOSDacInterface19 sos = impl;
        SOSGCInfoX86Header header = new() { SizeOf = (uint)sizeof(SOSGCInfoX86Header) };
        Assert.Equal(HResults.S_OK, sos.GetGCInfoX86Header(MethodIP, &header));
        Assert.Equal(1u, header.EpilogSize);
        Assert.Equal(1u, header.EpilogCount);
        Assert.Equal(5u, header.Flags);
        uint needed;
        uint[]? epilogs = count == 0 ? null : new uint[count];
        SOSGCInfoX86Transition[]? transitions = count == 0 ? null : new SOSGCInfoX86Transition[count];
        Assert.Equal(count < 1 ? HResults.S_FALSE : HResults.S_OK,
            sos.GetGCInfoX86Epilogs(MethodIP, (uint)count, epilogs, &needed));
        Assert.Equal(1u, needed);
        Assert.Equal(count < 2 ? HResults.S_FALSE : HResults.S_OK,
            sos.GetGCInfoX86Transitions(MethodIP, (uint)count, transitions, &needed));
        Assert.Equal(2u, needed);
        if (count >= 1)
            Assert.Equal(63u, epilogs.AsSpan()[0]);
        if (count >= 2)
        {
            Assert.Equal(4, transitions.AsSpan()[0].StackDepthChange);
            Assert.Equal(-4, transitions.AsSpan()[1].StackDepthChange);
        }
        Assert.Equal(HResults.E_POINTER, sos.GetGCInfoX86Header(MethodIP, null));
        Assert.Equal(HResults.E_POINTER, sos.GetGCInfoX86Transitions(MethodIP, 0, null, null));
        Assert.Equal(HResults.E_INVALIDARG, sos.GetGCInfoX86Transitions(MethodIP, 1, null, &needed));
        Assert.Equal(0u, needed);

        void* process = ComInterfaceMarshaller<IXCLRDataProcess>.ConvertToUnmanaged(impl);
        try
        {
            Guid iid = typeof(ISOSDacInterface19).GUID;
            Assert.Equal(HResults.S_OK, Marshal.QueryInterface((nint)process, in iid, out nint x86));
            try
            {
                void** vtable = *(void***)x86;
                header.SizeOf = (uint)sizeof(SOSGCInfoX86Header);
                Assert.Equal(HResults.S_OK,
                    ((delegate* unmanaged[MemberFunction]<nint, ClrDataAddress, SOSGCInfoX86Header*, int>)vtable[3])(x86, MethodIP, &header));
                Assert.Equal(5u, header.Flags);
                uint* offsets = stackalloc uint[1];
                Assert.Equal(HResults.S_OK,
                    ((delegate* unmanaged[MemberFunction]<nint, ClrDataAddress, uint, uint*, uint*, int>)vtable[4])
                        (x86, MethodIP, 1, offsets, &needed));
                Assert.Equal(63u, offsets[0]);
                SOSGCInfoX86Transition* raw = stackalloc SOSGCInfoX86Transition[2];
                Assert.Equal(HResults.S_OK,
                    ((delegate* unmanaged[MemberFunction]<nint, ClrDataAddress, uint, SOSGCInfoX86Transition*, uint*, int>)vtable[5])
                        (x86, MethodIP, 2, raw, &needed));
                Assert.Equal(2u, needed);
                Assert.Equal(4, raw[0].StackDepthChange);
                Assert.Equal(-4, raw[1].StackDepthChange);
            }
            finally
            {
                Marshal.Release(x86);
            }
        }
        finally
        {
            ComInterfaceMarshaller<IXCLRDataProcess>.Free(process);
        }
    }

    [Theory]
    [InlineData(4u, true)]
    [InlineData(5u, true)]
    [InlineData(4u, false)]
    [InlineData(5u, false)]
    public void X86HugeCallRootsAndReceiverMetadataRemainAvailable(uint version, bool ebpFrame)
    {
        byte[] encoded = ebpFrame
            ? [64, 0x81, 0xBE, 0x3F, 0x20, 0xFB, 0x11,
                3, 0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 0, 5, 0xFF]
            : [64, 1, 0xF5, 0xF8, 0x11,
                3, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 0, 5, 0xFF];
        (IGCInfo contract, IGCInfoHandle handle, _) = CreateX86GCInfo(encoded, version);
        IReadOnlyList<GCSlotLifetime> slots = contract.GetSlotLifetimes(handle);
        Assert.Equal(3, slots.Count);
        Assert.Contains(new GCSlotLifetime(true, 7, 0, 0, 1, 3, 4), slots);
        Assert.Contains(new GCSlotLifetime(false, 0, 0, 1, 0, 3, 4), slots);
        Assert.Contains(new GCSlotLifetime(false, 0, 4, 1, 1, 3, 4), slots);
        X86GCTransition receiver = Assert.Single(contract.GetX86Transitions(handle),
            t => t.Kind == X86GCTransitionKind.ThisRegister);
        Assert.Equal(6u, receiver.RegisterNumber);
        Assert.Equal(4u, receiver.Flags);
    }

    [Theory]
    [InlineData(4u)]
    [InlineData(5u)]
    public void X86EbpPointerIndicesCanHaveGaps(uint version)
    {
        (IGCInfo contract, IGCInfoHandle handle, _) = CreateX86GCInfo(
            [64, 0x81, 0xBE, 0xBF, 0x40, 0x91, 0xBF, 0xA1, 0xC9, 0xFF], version);
        Assert.Equal(new GCSlotLifetime[]
        {
            new(false, 0, -20, 2, 1, 2, 3),
            new(false, 0, -12, 2, 0, 1, 64),
        }, contract.GetSlotLifetimes(handle));
        Assert.Equal(new LiveSlot(false, 0, -12, 2, 0),
            Assert.Single(contract.EnumerateLiveSlots(handle, 3, new() { IsActiveFrame = true })));
        Assert.All(contract.GetX86Transitions(handle), t => Assert.Equal(0, t.StackDepthChange));
    }

    [Theory]
    [InlineData(4u)]
    [InlineData(5u)]
    public void X86InterruptibilityExcludesExplicitNoGCRegions(uint version)
    {
        byte countEncoding = version >= 5 ? (byte)9 : (byte)5;
        (IGCInfo contract, IGCInfoHandle handle, _) = CreateX86GCInfo(
            [64, 0x81, 0xC0, 0x4F, countEncoding, 2, 8, 4, 0xFF], version);
        Assert.Equal(new InterruptibleRange[] { new(0, 8), new(12, 64) },
            contract.GetInterruptibleRanges(handle));
        Assert.False(contract.IsGcSafe(handle, 8));
        Assert.True(contract.IsGcSafe(handle, 12));
    }

    [Theory]
    [InlineData(4u)]
    [InlineData(5u)]
    public void X86PinnedAndInteriorRootsScenarioProjectsLifetimes(uint version)
    {
        (IGCInfo gcInfo, IGCInfoHandle handle, SOSDacImpl impl) = CreateX86GCInfo(
            [0x82, 0x25, 0xD2, 0x80, 0xDB, 0xC6, 0xC7, 0x93, 0xF7, 0x40, 0x09, 0x09,
             0x04, 0x04, 0x04, 0x06, 0x05, 0x05, 0x04, 0x04, 0x04,
             0xF5, 0x40, 0x4E, 0x06, 0x08, 0x43, 0xF6, 0x86, 0x4B, 0x06, 0x08,
             0xC8, 0x43, 0xF2, 0x00, 0xF0, 0x43, 0xF0, 0x04, 0x40, 0x04, 0xBF,
             0x40, 0x06, 0x47, 0xF0, 0x07, 0x40, 0x04, 0xBF, 0x40, 0xF0, 0x01,
             0xBF, 0x44, 0x02, 0xF1, 0x4C, 0x0E, 0x4C, 0x0E, 0x4C, 0x0E, 0x44,
             0xF0, 0x00, 0xFF], version);
        IReadOnlyList<GCSlotLifetime> slots = gcInfo.GetSlotLifetimes(handle);
        Assert.Contains(new GCSlotLifetime(false, 0, -20, 2, 6, 0, 0x125), slots);
        Assert.Contains(new GCSlotLifetime(false, 0, -24, 2, 5, 0, 0x125), slots);
        Assert.Contains(new GCSlotLifetime(false, 0, -48, 2, 0, 0x7D, 0x86), slots);
        Assert.Contains(new GCSlotLifetime(true, 0, 0, 0, 1, 0xBC, 0xC2), slots);
        uint needed;
        ISOSDacInterface18 sos = impl;
        Assert.Equal(HResults.S_FALSE, sos.GetGCInfoSlotLifetimes(MethodIP, 0, null, &needed));
        SOSGCSlotLifetime[] output = new SOSGCSlotLifetime[needed];
        Assert.Equal(HResults.S_OK, sos.GetGCInfoSlotLifetimes(MethodIP, needed, output, &needed));
    }

    private static (IGCInfo Contract, IGCInfoHandle Handle, SOSDacImpl Impl) CreateX86GCInfo(byte[] encoded, uint version)
    {
        TestPlaceholderTarget.Builder builder = new(new() { IsLittleEndian = true, Is64Bit = false });
        const ulong GCInfoAddress = 0x20000;
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = GCInfoAddress,
            Data = encoded,
            Name = "X86 GCInfo",
        });
        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(RuntimeInfoArchitecture.X86);
        CodeBlockHandle codeBlock = new(new TargetPointer(0x30000));
        TargetPointer address = new(GCInfoAddress);
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(new TargetCodePointer(MethodIP))).Returns(codeBlock);
        executionManager.Setup(e => e.GetCodeKind(new TargetCodePointer(MethodIP))).Returns(CodeKind.Jitted);
        executionManager.Setup(e => e.GetGCInfo(codeBlock, out address, out version));
        Target target = builder.AddMockContract(runtimeInfo).AddMockContract(executionManager)
            .AddContract<IGCInfo>("c1").Build();
        IGCInfo contract = target.Contracts.GCInfo;
        return (contract, contract.DecodePlatformSpecificGCInfo(address, version), new SOSDacImpl(target, null, new()));
    }

    [Fact]
    public void X86InterfaceRejectsUnsupportedDecoderAndInitializesOutputs()
    {
        ISOSDacInterface18 common = CreateGCInfoInterface(
            new() { IsLittleEndian = true, Is64Bit = true }, CodeKind.Interpreter, (_, gcInfo, handle) =>
            {
                gcInfo.Setup(g => g.GetX86Header(handle)).Throws(new PlatformNotSupportedException());
                gcInfo.Setup(g => g.GetX86Epilogs(handle)).Throws(new PlatformNotSupportedException());
                gcInfo.Setup(g => g.GetX86Transitions(handle)).Throws(new PlatformNotSupportedException());
            });
        ISOSDacInterface19 sos = Assert.IsAssignableFrom<ISOSDacInterface19>(common);
        SOSGCInfoX86Header header = new() { SizeOf = (uint)sizeof(SOSGCInfoX86Header), Flags = uint.MaxValue };
        Assert.Equal(HResults.E_NOTIMPL, sos.GetGCInfoX86Header(MethodIP, &header));
        Assert.Equal(0u, header.Flags);
        uint needed = uint.MaxValue;
        Assert.Equal(HResults.E_NOTIMPL, sos.GetGCInfoX86Epilogs(MethodIP, 0, null, &needed));
        Assert.Equal(0u, needed);
        needed = uint.MaxValue;
        Assert.Equal(HResults.E_NOTIMPL, sos.GetGCInfoX86Transitions(MethodIP, 0, null, &needed));
        Assert.Equal(0u, needed);
        header.SizeOf--;
        Assert.Equal(HResults.E_INVALIDARG, sos.GetGCInfoX86Header(MethodIP, &header));
    }

    private static ISOSDacInterface18 CreateGCInfoInterface(
        MockTarget.Architecture arch,
        CodeKind codeKind,
        Action<Mock<IExecutionManager>, Mock<IGCInfo>, IGCInfoHandle>? configure = null,
        uint version = 6)
    {
        CodeBlockHandle codeBlock = new(new TargetPointer(0x30000));
        TargetPointer gcInfoAddress = new(0x20000);
        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        executionManager.Setup(e => e.GetCodeBlockHandle(new TargetCodePointer(MethodIP))).Returns(codeBlock);
        executionManager.Setup(e => e.GetCodeKind(new TargetCodePointer(MethodIP))).Returns(codeKind);
        executionManager.Setup(e => e.GetGCInfo(codeBlock, out gcInfoAddress, out version));
        Mock<IGCInfo> gcInfo = new(MockBehavior.Strict);
        IGCInfoHandle handle = Mock.Of<IGCInfoHandle>();
        if (codeKind == CodeKind.Interpreter)
            gcInfo.Setup(g => g.DecodeInterpreterGCInfo(gcInfoAddress, version)).Returns(handle);
        else
            gcInfo.Setup(g => g.DecodePlatformSpecificGCInfo(gcInfoAddress, version)).Returns(handle);
        gcInfo.Setup(g => g.GetHeader(handle)).Returns(new GCInfoHeader(
            version, 128, 0, 5, 0, false, false, false, null, 0, 0, null, null, GenericsContextKind.None));
        gcInfo.Setup(g => g.GetInterruptibleRanges(handle)).Returns(new InterruptibleRange[] { new(4, 12), new(16, 20) });
        gcInfo.Setup(g => g.GetSafePoints(handle)).Returns(new uint[] { 8, 24 });
        gcInfo.Setup(g => g.GetSlotLifetimes(handle)).Returns(new GCSlotLifetime[]
        {
            new(true, 3, 0, 0, 3, 4, 8),
            new(false, 0, -24, 2, 6, 0, 128),
            new(false, 0, 40, 1, 1, 16, 20),
        });
        configure?.Invoke(executionManager, gcInfo, handle);
        Target target = new TestPlaceholderTarget.Builder(arch)
            .AddMockContract(executionManager)
            .AddMockContract(gcInfo)
            .Build();

        return Assert.IsAssignableFrom<ISOSDacInterface18>(new SOSDacImpl(target, legacyObj: null, new()));
    }

}
