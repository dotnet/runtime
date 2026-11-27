// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// Tests for resolving WASM variable debug information.
/// </summary>
/// <remarks>
/// On WASM a debug-info "register" names a WASM local, which is engine-private frame state that is
/// not in linear memory. <see cref="WasmContext"/> exposes no indexed register file, so a naive
/// resolution reports the value 0 for every variable.
/// </remarks>
public unsafe class WasmDebugInfoTests
{
    // ICorDebugInfo::WASM_LOCAL_REGNUM_BASE.
    private const uint WasmLocalRegNumBase = 3;

    // The base register RyuJIT emits for every WASM stack location. REG_FPBASE and REG_SPBASE are
    // both REG_NA on WASM (targetwasm.h), REG_NA is REG_COUNT which is 2 because registerwasm.h
    // defines only REG_STK, and ICorDebugInfo::REGNUM_AMBIENT_SP is also 2 (cordebuginfo.h).
    private const uint JitEmittedWasmStackBaseRegister = 2;

    private static uint EncodeWasmRegister(uint index)
        => index + WasmLocalRegNumBase;

    private static Target CreateTarget(RuntimeInfoArchitecture targetArch, bool is64Bit)
    {
        TestPlaceholderTarget.Builder builder = new(
            new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = is64Bit });
        builder
            .AddGlobalStrings((Constants.Globals.Architecture, targetArch.ToString().ToLowerInvariant()))
            .AddContract<IRuntimeInfo>(version: "c1");

        return builder.Build();
    }

    public static TheoryData<uint> WasmLocalIndices() => new() { 0u, 1u, 4u, 6u };

    /// <summary>
    /// A WASM local must not resolve to a fabricated location. Before this guard existed,
    /// <c>ReadRegister</c> fell through to <c>return 0</c> for every WASM local register,
    /// silently reporting the value 0 as though it had been read successfully.
    /// </summary>
    [Theory]
    [MemberData(nameof(WasmLocalIndices))]
    public void ResolveVarLocation_WasmRegister_ReportsNoLocation(uint localIndex)
    {
        Target target = CreateTarget(RuntimeInfoArchitecture.Wasm, is64Bit: false);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);

        // A usable frame pointer, so only the kind check can refuse the location.
        context.FramePointer = new TargetPointer(0x0020_0000);
        uint register = EncodeWasmRegister(localIndex);

        foreach (DebugVarLocKind kind in new[] { DebugVarLocKind.Register, DebugVarLocKind.RegisterRegister, DebugVarLocKind.RegisterStack, DebugVarLocKind.StackRegister })
        {
            foreach (bool isByRef in new[] { false, true })
            {
                DebugVarInfo varInfo = new()
                {
                    StartOffset = 0,
                    EndOffset = 0x100,
                    VarNumber = 0,
                    Kind = kind,
                    IsByRef = isByRef,
                    Register = register,
                    Register2 = register + 1,
                    BaseRegister = JitEmittedWasmStackBaseRegister,
                    BaseRegister2 = JitEmittedWasmStackBaseRegister,
                };

                Assert.Empty(ClrDataFrame.ResolveVarLocation(varInfo, context, target));
            }
        }
    }

    /// <summary>
    /// The WASM guard must be scoped to WASM. Architectures with a real register file keep
    /// resolving register locations exactly as before.
    /// </summary>
    [Fact]
    public void ResolveVarLocation_NonWasmRegister_StillResolves()
    {
        Target target = CreateTarget(RuntimeInfoArchitecture.X64, is64Bit: true);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);

        DebugVarInfo varInfo = new()
        {
            StartOffset = 0,
            EndOffset = 0x100,
            VarNumber = 0,
            Kind = DebugVarLocKind.Register,
            Register = 0,
        };

        NativeVarLocation[] locations = ClrDataFrame.ResolveVarLocation(varInfo, context, target);

        NativeVarLocation location = Assert.Single(locations);
        Assert.True(location.IsRegisterValue);
    }

    /// <summary>
    /// The success path: a WASM stack slot resolves to <c>logical frame pointer + StackOffset</c>
    /// in linear memory.
    /// </summary>
    [Theory]
    [InlineData(0x18, false)]
    [InlineData(0x00, false)]
    [InlineData(0x18, true)]
    public void ResolveVarLocation_WasmStack_ResolvesAgainstLogicalFramePointer(int stackOffset, bool isDoubleStack)
    {
        const ulong FrameAddress = 0x0020_0000;

        Target target = CreateTarget(RuntimeInfoArchitecture.Wasm, is64Bit: false);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);
        context.StackPointer = new TargetPointer(FrameAddress);
        context.FramePointer = new TargetPointer(FrameAddress);

        // The base register RyuJIT actually emits for a WASM stack slot: REG_FPBASE / REG_SPBASE
        // are REG_NA (== REG_COUNT == 2), and REGNUM_AMBIENT_SP is also 2.
        DebugVarInfo varInfo = new()
        {
            StartOffset = 0,
            EndOffset = 0x100,
            VarNumber = 0,
            Kind = isDoubleStack ? DebugVarLocKind.DoubleStack : DebugVarLocKind.Stack,
            BaseRegister = JitEmittedWasmStackBaseRegister,
            StackOffset = stackOffset,
        };

        NativeVarLocation location = Assert.Single(ClrDataFrame.ResolveVarLocation(varInfo, context, target));

        Assert.False(location.IsRegisterValue);
        Assert.Equal(FrameAddress + (ulong)stackOffset, location.AddressOrValue);
        Assert.Equal(isDoubleStack ? 8u : 4u, location.Size);
    }

    /// <summary>
    /// The interpreter bases its stack slots on <c>ICorDebugInfo::REGNUM_FP</c>, which is 0 on WASM,
    /// and an interpreted frame's context frame pointer is <c>InterpMethodContextFrame.pStack</c>.
    /// </summary>
    [Fact]
    public void ResolveVarLocation_WasmInterpretedStack_ResolvesAgainstFramePointer()
    {
        const uint InterpreterRegNumFP = 0;
        const ulong InterpStack = 0x0030_0000;
        const int StackOffset = 0x10;

        Target target = CreateTarget(RuntimeInfoArchitecture.Wasm, is64Bit: false);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);
        context.StackPointer = new TargetPointer(0x0020_0000);
        context.FramePointer = new TargetPointer(InterpStack);

        DebugVarInfo varInfo = new()
        {
            StartOffset = 0,
            EndOffset = 0x100,
            VarNumber = 0,
            Kind = DebugVarLocKind.Stack,
            BaseRegister = InterpreterRegNumFP,
            StackOffset = StackOffset,
        };

        NativeVarLocation location = Assert.Single(ClrDataFrame.ResolveVarLocation(varInfo, context, target));
        Assert.False(location.IsRegisterValue);
        Assert.Equal(InterpStack + StackOffset, location.AddressOrValue);
    }

    /// <summary>
    /// For a funclet frame the context's frame pointer is the establishing method's frame, not the
    /// funclet's own frame base, so the resolved address must use the frame pointer rather than SP.
    /// </summary>
    [Fact]
    public void ResolveVarLocation_WasmStackInFunclet_ResolvesAgainstEstablishingFrame()
    {
        const ulong FuncletFrame = 0x0020_0000;
        const ulong EstablishingFramePointer = 0x0020_0100;
        const int StackOffset = 0x18;

        Target target = CreateTarget(RuntimeInfoArchitecture.Wasm, is64Bit: false);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);
        context.StackPointer = new TargetPointer(FuncletFrame);
        context.FramePointer = new TargetPointer(EstablishingFramePointer);

        DebugVarInfo varInfo = new()
        {
            StartOffset = 0,
            EndOffset = 0x100,
            VarNumber = 0,
            Kind = DebugVarLocKind.Stack,
            BaseRegister = JitEmittedWasmStackBaseRegister,
            StackOffset = StackOffset,
        };

        NativeVarLocation location = Assert.Single(ClrDataFrame.ResolveVarLocation(varInfo, context, target));

        Assert.False(location.IsRegisterValue);
        Assert.Equal(EstablishingFramePointer + StackOffset, location.AddressOrValue);
        Assert.NotEqual(FuncletFrame + StackOffset, location.AddressOrValue);
    }

    /// <summary>
    /// Characterizes the pre-existing <see cref="ClrDataValue"/> behavior that the WASM consumer
    /// relies on: a symbolic engine local resolves to no native locations, and reads from that
    /// empty value fail rather than returning a plausible zero.
    /// </summary>
    [Fact]
    public unsafe void ClrDataValue_NoLocations_FailsInsteadOfReturningZero()
    {
        Target target = CreateTarget(RuntimeInfoArchitecture.Wasm, is64Bit: false);

        ClrDataValue value = new(
            target,
            TargetPointer.Null,
            flags: (uint)ClrDataValueFlag.IS_PRIMITIVE,
            typeHandle: null,
            baseAddress: TargetPointer.Null,
            locations: [],
            legacyImpl: null,
            apiLock: new Lock());

        IXCLRDataValue dataValue = value;

        uint numLocs;
        Assert.Equal(HResults.S_OK, dataValue.GetNumLocations(&numLocs));
        Assert.Equal(0u, numLocs);

        // GetBytes must not succeed with a zero-filled buffer.
        byte[] buffer = new byte[8];
        uint dataSize;
        int hr;
        fixed (byte* pBuffer = buffer)
        {
            hr = dataValue.GetBytes((uint)buffer.Length, &dataSize, pBuffer);
        }
        Assert.True(hr < 0, $"GetBytes should fail for a value with no locations, got 0x{hr:X8}");

        // GetAddress must not report address 0 as though it were a real address.
        ClrDataAddress address;
        Assert.True(dataValue.GetAddress(&address) < 0, "GetAddress should fail for a value with no locations");
    }

    /// <summary>
    /// An unreadable or null byref indirection fails at the read rather than becoming address 0,
    /// which is readable linear memory on WASM.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResolveVarLocation_WasmStackByRefUnreadableOrNull_Throws(bool slotIsNull)
    {
        const ulong FrameAddress = 0x0020_0000;
        const int StackOffset = 0x24;
        MockMemorySpace.HeapFragment[] fragments = slotIsNull
            ? [new MockMemorySpace.HeapFragment { Address = FrameAddress + StackOffset, Data = new byte[4], Name = "null byref pointer slot" }]
            : [];
        Target target = CreateWasmTargetWithZeroPage(fragments);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);
        context.FramePointer = new TargetPointer(FrameAddress);

        DebugVarInfo varInfo = new()
        {
            Kind = DebugVarLocKind.Stack,
            IsByRef = true,
            BaseRegister = JitEmittedWasmStackBaseRegister,
            StackOffset = StackOffset,
        };

        VirtualReadException ex = Assert.Throws<VirtualReadException>(() => ClrDataFrame.ResolveVarLocation(varInfo, context, target));
        Assert.Equal(CorDbgHResults.CORDBG_E_READVIRTUAL_FAILURE, ex.HResult);
    }

    [Fact]
    public void ResolveVarLocation_WasmStackByRef_ResolvesThroughPointer()
    {
        const ulong FrameAddress = 0x0020_0000;
        const int StackOffset = 0x24;
        const uint Pointee = 0x0030_0040;
        byte[] slot = new byte[4];
        BitConverter.TryWriteBytes(slot, Pointee);
        Target target = CreateWasmTargetWithZeroPage(
            new MockMemorySpace.HeapFragment { Address = FrameAddress + StackOffset, Data = slot, Name = "byref pointer slot" });
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);
        context.FramePointer = new TargetPointer(FrameAddress);

        DebugVarInfo varInfo = new()
        {
            Kind = DebugVarLocKind.Stack,
            IsByRef = true,
            BaseRegister = JitEmittedWasmStackBaseRegister,
            StackOffset = StackOffset,
        };

        NativeVarLocation location = Assert.Single(ClrDataFrame.ResolveVarLocation(varInfo, context, target));
        Assert.False(location.IsRegisterValue);
        Assert.Equal(Pointee, location.AddressOrValue);
    }

    [Fact]
    public unsafe void ClrDataValue_WasmStackNullReference_RemainsReadable()
    {
        const ulong FrameAddress = 0x0020_0000;
        const int StackOffset = 0x24;
        const ulong SlotAddress = 0x0020_0024;
        Target target = CreateWasmTargetWithZeroPage(
            new MockMemorySpace.HeapFragment
            {
                Address = SlotAddress,
                Data = new byte[4],
                Name = "null reference slot",
            });
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);
        context.FramePointer = new TargetPointer(FrameAddress);
        DebugVarInfo varInfo = new()
        {
            Kind = DebugVarLocKind.Stack,
            BaseRegister = JitEmittedWasmStackBaseRegister,
            StackOffset = StackOffset,
        };
        NativeVarLocation[] locations = ClrDataFrame.ResolveVarLocation(varInfo, context, target);

        IXCLRDataValue dataValue = new ClrDataValue(
            target,
            TargetPointer.Null,
            flags: (uint)ClrDataValueFlag.IS_REFERENCE,
            typeHandle: null,
            baseAddress: new TargetPointer(SlotAddress),
            locations,
            legacyImpl: null,
            apiLock: new Lock());

        uint numLocs;
        Assert.Equal(HResults.S_OK, dataValue.GetNumLocations(&numLocs));
        Assert.Equal(1u, numLocs);

        byte[] buffer = [0xCC, 0xCC, 0xCC, 0xCC];
        uint dataSize;
        fixed (byte* pBuffer = buffer)
        {
            Assert.Equal(HResults.S_OK, dataValue.GetBytes((uint)buffer.Length, &dataSize, pBuffer));
        }
        Assert.Equal(4u, dataSize);
        Assert.Equal(new byte[4], buffer);

        ClrDataAddress address;
        Assert.Equal(HResults.S_OK, dataValue.GetAddress(&address));
        Assert.Equal(SlotAddress, (ulong)address);
    }

    private static Target CreateWasmTargetWithZeroPage(params MockMemorySpace.HeapFragment[] fragments)
    {
        TestPlaceholderTarget.Builder builder = new(
            new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = false });
        builder
            .AddGlobalStrings((Constants.Globals.Architecture, "wasm"))
            .AddContract<IRuntimeInfo>(version: "c1");
        builder.MemoryBuilder.AddHeapFragment(
            new MockMemorySpace.HeapFragment
            {
                Address = 0,
                Data = new byte[64],
                Name = "readable wasm zero page",
            });
        builder.MemoryBuilder.AddHeapFragments(fragments);
        return builder.Build();
    }
}
