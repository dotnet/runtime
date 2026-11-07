// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class GCInfoTests
{
    private const ulong MethodIP = 0x10000;

    [Theory]
    [InlineData(RuntimeInfoArchitecture.X64, false, new byte[] { 0x0A, 0x80, 0x50, 0x1B, 0xE2, 0x38, 0x21, 0x8C, 0x0B, 0x62, 0x1C, 0x88, 0x38 })]
    [InlineData(RuntimeInfoArchitecture.Arm, false, new byte[] { 0x0A, 0x80, 0x80, 0x31, 0xC4, 0x71, 0x42, 0x18, 0x17, 0xC4, 0x38, 0x10, 0x71 })]
    [InlineData(RuntimeInfoArchitecture.Arm64, false, new byte[] { 0x0A, 0x00, 0xA1, 0x36, 0xC4, 0x71, 0x42, 0x18, 0x17, 0xC4, 0x38, 0x10, 0x71 })]
    [InlineData(RuntimeInfoArchitecture.LoongArch64, false, new byte[] { 0x0A, 0x00, 0xA1, 0x36, 0xC4, 0x71, 0x42, 0x18, 0x17, 0xC4, 0x38, 0x10, 0x71 })]
    [InlineData(RuntimeInfoArchitecture.RiscV64, false, new byte[] { 0x0A, 0x00, 0xA1, 0x36, 0xC4, 0x71, 0x42, 0x18, 0x17, 0xC4, 0x38, 0x10, 0x71 })]
    [InlineData(RuntimeInfoArchitecture.Wasm, false, new byte[] { 0x84, 0x00, 0xC2, 0x78, 0x87, 0x78, 0x46, 0x78, 0x03, 0x5E, 0x00 })]
    [InlineData(RuntimeInfoArchitecture.X64, true, new byte[] { 0x0A, 0x40, 0xA8, 0x0D, 0x71, 0x9C, 0x10, 0xC6, 0x05, 0x31, 0x0E, 0x44, 0x1C })]
    [InlineData(RuntimeInfoArchitecture.Wasm, true, new byte[] { 0x0A, 0x40, 0xA8, 0x0D, 0x71, 0x9C, 0x10, 0xC6, 0x05, 0x31, 0x0E, 0x44, 0x1C })]
    public void StackSlotsUseFlagsThenBaseThenOffset(RuntimeInfoArchitecture architecture, bool interpreter, byte[] encoded)
    {
        IGCInfoDecoder decoder = CreateDecoder(architecture, interpreter, encoded, 6);
        IReadOnlyList<GCSlotLifetime> slots = decoder.GetSlotLifetimes();
        Assert.Equal(8, slots.Count);
        bool wasm = architecture == RuntimeInfoArchitecture.Wasm && !interpreter;
        int slotSize = interpreter ? 1 : architecture is RuntimeInfoArchitecture.Arm or RuntimeInfoArchitecture.Wasm ? 4 : 8;
        for (int i = 0; i < slots.Count; i++)
        {
            Assert.False(slots[i].IsRegister);
            int offset = wasm ? -8 + i % 2 : i % 2 == 0 ? 8 : -8;
            Assert.Equal(offset * slotSize, slots[i].SpOffset);
            Assert.Equal(wasm ? 2u : (uint)(1 + i % 2), slots[i].BaseRegister);
            uint flags = (uint)(3 - i / 2) ^ (wasm ? 2u : 0u);
            Assert.Equal(flags | 4u, slots[i].GcFlags);
            Assert.Equal(0u, slots[i].BeginOffset);
            Assert.Equal(decoder.GetCodeLength(), slots[i].EndOffset);
        }

        SOSGCSlotLifetime[] inspected = InspectGCInfoSlots(architecture, interpreter, decoder, 6);
        Assert.Equal(slots.Count, inspected.Length);
        for (int i = 0; i < inspected.Length; i++)
            Assert.Equal(wasm ? 1u : (uint)(i % 2), inspected[i].BaseRegister);
    }

    [Theory]
    [InlineData(1, new byte[] { 0x84, 0x40, 0x00, 0x78 })]
    [InlineData(2, new byte[] { 0x84, 0x80, 0x00, 0x78, 0x01 })]
    [InlineData(3, new byte[] { 0x84, 0xC0, 0x00, 0x78, 0x11 })]
    public void WasmPinnedObjectRunsOmitRepeatedFlags(int count, byte[] encoded)
    {
        IGCInfoDecoder decoder = CreateDecoder(RuntimeInfoArchitecture.Wasm, false, encoded, 6);
        IReadOnlyList<GCSlotLifetime> slots = decoder.GetSlotLifetimes();
        Assert.Equal(count, slots.Count);
        for (int i = 0; i < slots.Count; i++)
        {
            Assert.False(slots[i].IsRegister);
            Assert.Equal(6u, slots[i].GcFlags);
            Assert.Equal(4 * (-8 + i), slots[i].SpOffset);
            Assert.Equal(2u, slots[i].BaseRegister);
            Assert.Equal(0u, slots[i].BeginOffset);
            Assert.Equal(2u, slots[i].EndOffset);
        }
    }

    [Theory]
    [InlineData(RuntimeInfoArchitecture.X64, 5)]
    [InlineData(RuntimeInfoArchitecture.X64, 6)]
    [InlineData(RuntimeInfoArchitecture.Arm, 5)]
    [InlineData(RuntimeInfoArchitecture.Arm, 6)]
    [InlineData(RuntimeInfoArchitecture.Arm64, 5)]
    [InlineData(RuntimeInfoArchitecture.Arm64, 6)]
    [InlineData(RuntimeInfoArchitecture.LoongArch64, 5)]
    [InlineData(RuntimeInfoArchitecture.LoongArch64, 6)]
    [InlineData(RuntimeInfoArchitecture.RiscV64, 5)]
    [InlineData(RuntimeInfoArchitecture.RiscV64, 6)]
    public void RegisterSlotsUseFlagsFirstAndBoundedIntegers(RuntimeInfoArchitecture architecture, int version)
    {
        bool arm = architecture == RuntimeInfoArchitecture.Arm;
        bool x64 = architecture == RuntimeInfoArchitecture.X64;
        int registerBits = arm ? 4 : 5;
        int absoluteBase = arm ? 2 : 3;
        int deltaBase = arm ? 1 : 2;
        List<(int Register, int Flags)> registers = [];
        for (int flags = 3; flags >= 0; flags--)
        {
            foreach (int register in new[] { 0, 1, 3, 4, 7, 8, 15, 16, 23, 24, 30, 31 })
            {
                if (register >= (1 << registerBits) || flags == 3 && register < 8)
                    continue;
                registers.Add((register, flags));
            }
        }
        List<byte> bytes = [];
        int bitCount = 0;
        Write(0, 1);
        Write(1, 1);
        WriteUnsigned(2, arm ? 7 : 8);
        WriteUnsigned(1, x64 ? 2 : 3);
        Write(0, 1); // Safe point at normalized offset zero.
        Write(1, 1);
        WriteUnsigned(registers.Count, x64 || arm ? 2 : 3);
        Write(0, 1);
        for (int i = 0; i < registers.Count; i++)
        {
            (int register, int flags) = registers[i];
            bool emitFlags = i == 0 || registers[i - 1].Flags != 0;
            bool absolute = version < 6 ? emitFlags : i == 0 || flags != registers[i - 1].Flags;
            int value = absolute ? register : register - registers[i - 1].Register - 1;
            int encodingBase = absolute ? absoluteBase : deltaBase;
            if (version >= 6)
            {
                if (emitFlags)
                    Write(flags, i == 0 || (registers[i - 1].Flags & 2) != 0 ? 2 : 1);
                bool extended = value >= (1 << encodingBase);
                Write((value & ((1 << encodingBase) - 1)) | (extended ? 1 << encodingBase : 0), encodingBase + 1);
                if (extended)
                    Write(value >> encodingBase, registerBits - encodingBase);
            }
            else
            {
                WriteUnsigned(value, encodingBase);
                if (emitFlags)
                    Write(flags, 2);
            }
        }
        Write(0, 1); // Direct live-state bitmap.
        foreach ((int register, int flags) in registers)
            Write(1, 1);

        IGCInfoDecoder decoder = CreateDecoder(architecture, false, bytes.ToArray(), (uint)version);
        IReadOnlyList<GCSlotLifetime> slots = decoder.GetSlotLifetimes();
        Assert.Equal(registers.Count, slots.Count);
        for (int i = 0; i < registers.Count; i++)
        {
            Assert.True(slots[i].IsRegister);
            Assert.Equal((uint)registers[i].Register, slots[i].RegisterNumber);
            Assert.Equal((uint)registers[i].Flags, slots[i].GcFlags);
            Assert.Equal(0u, slots[i].BeginOffset);
            Assert.Equal(1u, slots[i].EndOffset);
        }

        void Write(int value, int count)
        {
            for (int i = 0; i < count; i++, bitCount++)
            {
                if (bitCount % 8 == 0)
                    bytes.Add(0);
                bytes[bitCount / 8] |= (byte)(((value >> i) & 1) << (bitCount % 8));
            }
        }

        void WriteUnsigned(int value, int encodingBase)
        {
            do
            {
                int chunk = value & ((1 << encodingBase) - 1);
                value >>>= encodingBase;
                Write(chunk | (value == 0 ? 0 : 1 << encodingBase), encodingBase + 1);
            } while (value != 0);
        }
    }

    [Theory]
    [InlineData(false, new byte[] { 0x0A, 0x80, 0xB0, 0x10, 0x88, 0x00 })]
    [InlineData(true, new byte[] { 0x0A, 0x80, 0xB0, 0x10, 0x88, 0x00 })]
    public void NonWasmVersion5StackSlotsRemainReadable(bool interpreter, byte[] encoded)
    {
        IGCInfoDecoder decoder = CreateDecoder(RuntimeInfoArchitecture.X64, interpreter, encoded, 5);
        IReadOnlyList<GCSlotLifetime> slots = decoder.GetSlotLifetimes();

        Assert.Equal(2u, decoder.GetCodeLength());
        Assert.Equal(2, slots.Count);
        Assert.Equal(interpreter ? 8 : 64, slots[0].SpOffset);
        Assert.Equal(1u, slots[0].BaseRegister);
        Assert.Equal(interpreter ? 16 : 128, slots[1].SpOffset);
        Assert.Equal(2u, slots[1].BaseRegister);
        Assert.All(slots, slot =>
        {
            Assert.False(slot.IsRegister);
            Assert.Equal(0u, slot.GcFlags);
            Assert.Equal(0u, slot.BeginOffset);
            Assert.Equal(2u, slot.EndOffset);
        });

        SOSGCSlotLifetime[] inspected = InspectGCInfoSlots(RuntimeInfoArchitecture.X64, interpreter, decoder, 5);
        Assert.Equal(0u, inspected[0].BaseRegister);
        Assert.Equal(1u, inspected[1].BaseRegister);
    }

    [Theory]
    [InlineData(new byte[] { 0x04 }, 0, 0u, 0u)]
    [InlineData(new byte[] { 0x21, 0x08, 0x02, 0x00 }, 8, 1u, 0u)]
    [InlineData(new byte[] { 0x41, 0x08, 0x02, 0x00 }, 8, 1u, 0u)]
    [InlineData(new byte[] { 0x01, 0x08, 0x01 }, 0, 0u, 1u)]
    public void WasmHeadersDecodePrologAndFrameRegister(byte[] encoded, int genericContextOffset,
        uint prologSize, uint safePoints)
    {
        IGCInfoDecoder decoder = CreateDecoder(RuntimeInfoArchitecture.Wasm, false, encoded, 6);
        GCInfoHeader header = decoder.GetHeader();

        Assert.Equal(2u, header.CodeSize);
        Assert.Equal(2u, header.StackBaseRegister);
        Assert.Equal(prologSize, header.PrologSize);
        Assert.Equal(genericContextOffset == 0 ? null : new SpecialSlot(genericContextOffset), header.GenericsInstContext);
        Assert.Equal((int)safePoints, decoder.GetSafePoints().Count);
        if (safePoints != 0)
            Assert.Equal(0u, Assert.Single(decoder.GetSafePoints()));
        Assert.Empty(decoder.GetSlotLifetimes());
    }

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
        Assert.Equal((uint)contextKind, header.GenericsInstContextKind);
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

    private static SOSGCSlotLifetime[] InspectGCInfoSlots(
        RuntimeInfoArchitecture architecture, bool interpreter, IGCInfoDecoder decoder, uint version)
    {
        MockTarget.Architecture arch = new()
        {
            IsLittleEndian = true,
            Is64Bit = architecture is not (RuntimeInfoArchitecture.Arm or RuntimeInfoArchitecture.Wasm),
        };
        ISOSDacInterface18 sos = CreateGCInfoInterface(
            arch, interpreter ? CodeKind.Interpreter : CodeKind.ReadyToRun,
            (_, gcInfo, handle) => gcInfo.Setup(g => g.GetSlotLifetimes(handle)).Returns(decoder.GetSlotLifetimes()),
            version);
        uint needed = 0;
        Assert.Equal(HResults.S_FALSE, sos.GetGCInfoSlotLifetimes(MethodIP, 0, null, &needed));
        SOSGCSlotLifetime[] result = new SOSGCSlotLifetime[needed];
        Assert.Equal(HResults.S_OK, sos.GetGCInfoSlotLifetimes(MethodIP, needed, result, &needed));

        return result;
    }

    private static IGCInfoDecoder CreateDecoder(RuntimeInfoArchitecture architecture, bool interpreter, byte[] encoded, uint version)
    {
        MockTarget.Architecture arch = new()
        {
            IsLittleEndian = true,
            Is64Bit = architecture is not (RuntimeInfoArchitecture.Arm or RuntimeInfoArchitecture.Wasm),
        };
        TestPlaceholderTarget.Builder builder = new(arch);
        const ulong GcInfoAddress = 0x10000;
        byte[] data = new byte[encoded.Length + 8];
        encoded.CopyTo(data, 0);
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = GcInfoAddress,
            Data = data,
            Name = "GC slot flag runs",
        });
        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(c => c.GetTargetArchitecture()).Returns(architecture);
        Target target = builder.AddMockContract(runtimeInfo).Build();
        TargetPointer address = new(GcInfoAddress);
        return interpreter
            ? new GcInfoDecoder<InterpreterGCInfoTraits>(target, address, version)
            : architecture switch
            {
                RuntimeInfoArchitecture.X64 => new GcInfoDecoder<AMD64GCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.Arm => new GcInfoDecoder<ARMGCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.Arm64 => new GcInfoDecoder<ARM64GCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.LoongArch64 => new GcInfoDecoder<LoongArch64GCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.RiscV64 => new GcInfoDecoder<RISCV64GCInfoTraits>(target, address, version),
                RuntimeInfoArchitecture.Wasm => new GcInfoDecoder<WasmGCInfoTraits>(target, address, version),
                _ => throw new InvalidOperationException(),
            };
    }
}
