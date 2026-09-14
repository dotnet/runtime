// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILCompiler.Reflection.ReadyToRun;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// Tests for decoding and resolving WASM variable debug information.
/// </summary>
/// <remarks>
/// On WASM a debug-info "register" is not a register. RyuJIT packs a
/// <c>(local index, wasm value type)</c> tuple into <c>regNumber</c>
/// (<c>src/coreclr/jit/registeropswasm.cpp</c>), and the resulting wasm local is engine-private
/// frame state that is not in linear memory. <see cref="WasmContext"/> exposes no indexed
/// register file, so a naive resolution reports the value 0 for every variable.
/// </remarks>
public unsafe class WasmDebugInfoTests
{
    private const int WasmRegTypeShift = 29;
    private const byte WasmDebugValueTypeCount = (byte)WasmDebugValueType.Count;
    private static DebugInfoHelpers.WasmDebugInfoEncoding CurrentEncoding
        => new(WasmRegTypeShift, WasmDebugValueTypeCount);

    // The base register RyuJIT emits for every WASM stack location. REG_FPBASE and REG_SPBASE are
    // both REG_NA on WASM (targetwasm.h), REG_NA is REG_COUNT which is 2 because registerwasm.h
    // defines only REG_STK, and ICorDebugInfo::REGNUM_AMBIENT_SP is also 2 (cordebuginfo.h).
    private const uint JitEmittedWasmStackBaseRegister = 2;

    private static uint PackWasmRegister(uint index, uint valueType)
        => index | (valueType << WasmRegTypeShift);

    private static WasmLocalInfo? DecodeWasmRegister(uint packed)
        => DebugInfoHelpers.DecodeWasmRegister(packed, CurrentEncoding);

    private static Target CreateTarget(
        RuntimeInfoArchitecture targetArch,
        bool is64Bit,
        bool includeWasmEncoding = true)
    {
        TestPlaceholderTarget.Builder builder = new(
            new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = is64Bit });
        builder
            .AddGlobalStrings((Constants.Globals.Architecture, targetArch.ToString().ToLowerInvariant()))
            .AddContract<IRuntimeInfo>(version: "c1");

        if (targetArch == RuntimeInfoArchitecture.Wasm && includeWasmEncoding)
        {
            builder.AddGlobals(
                (Constants.Globals.WasmDebugRegisterTypeShift, WasmRegTypeShift),
                (Constants.Globals.WasmDebugValueTypeCount, WasmDebugValueTypeCount));
        }

        return builder.Build();
    }

    public static TheoryData<uint, uint> WasmPackedRegisters() => new()
    {
        { 1u, 1u },   // $1 (i32)
        { 2u, 2u },   // $2 (i64)
        { 3u, 3u },   // $3 (f32)
        { 4u, 4u },   // $4 (f64)
        { 5u, 5u },   // $5 (v128)
        { 6u, 6u },   // $6 (exnref)
    };

    /// <summary>
    /// A WASM local must not resolve to a fabricated location. Before this guard existed,
    /// <c>ReadRegister</c> fell through to <c>return 0</c> for every packed WASM register,
    /// silently reporting the value 0 as though it had been read successfully.
    /// </summary>
    [Theory]
    [MemberData(nameof(WasmPackedRegisters))]
    public void ResolveVarLocation_WasmRegister_ReportsNoLocation(uint localIndex, uint valueType)
    {
        Target target = CreateTarget(RuntimeInfoArchitecture.Wasm, is64Bit: false);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);

        uint packed = PackWasmRegister(localIndex, valueType);

        // Both the projected kind produced by the contract on WASM and the raw architecture-neutral
        // kind must refuse to resolve.
        foreach (DebugVarLocKind kind in new[] { DebugVarLocKind.WasmLocal, DebugVarLocKind.Register })
        {
            DebugVarInfo varInfo = new()
            {
                StartOffset = 0,
                EndOffset = 0x100,
                VarNumber = 0,
                Kind = kind,
                Register = packed,
                WasmLocal = DecodeWasmRegister(packed),
            };

            Assert.Empty(ClrDataFrame.ResolveVarLocation(varInfo, context, target));
        }
    }

    /// <summary>
    /// A WASM stack location whose base register is neither a decodable WASM local nor the WASM
    /// frame-base sentinel has no frame pointer to resolve against and must not produce an address.
    /// </summary>
    [Theory]
    [InlineData(0u)]  // REGNUM_PC
    [InlineData(1u)]  // REGNUM_COUNT
    public void ResolveVarLocation_WasmStackWithNonLocalBase_ReportsNoLocation(uint baseRegister)
    {
        Target target = CreateTarget(RuntimeInfoArchitecture.Wasm, is64Bit: false);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);

        DebugVarInfo varInfo = new()
        {
            StartOffset = 0,
            EndOffset = 0x100,
            VarNumber = 0,
            Kind = DebugVarLocKind.Stack,
            BaseRegister = baseRegister,
            StackOffset = 0x10,
        };

        Assert.Empty(ClrDataFrame.ResolveVarLocation(varInfo, context, target));
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

    public static TheoryData<uint, uint, WasmDebugValueType> DecodableRegisters() => new()
    {
        { PackWasmRegister(1, 1), 1u, WasmDebugValueType.I32 },
        { PackWasmRegister(2, 2), 2u, WasmDebugValueType.I64 },
        { PackWasmRegister(3, 3), 3u, WasmDebugValueType.F32 },
        { PackWasmRegister(4, 4), 4u, WasmDebugValueType.F64 },
        { PackWasmRegister(5, 5), 5u, WasmDebugValueType.V128 },
        { PackWasmRegister(6, 6), 6u, WasmDebugValueType.ExnRef },
        // Index 0 and the largest representable index must round-trip too.
        { PackWasmRegister(0, 1), 0u, WasmDebugValueType.I32 },
        { PackWasmRegister((1u << WasmRegTypeShift) - 1, 4), (1u << WasmRegTypeShift) - 1, WasmDebugValueType.F64 },
    };

    [Theory]
    [MemberData(nameof(DecodableRegisters))]
    public void DecodeWasmRegister_DecodesIndexAndValueType(uint packed, uint expectedIndex, WasmDebugValueType expectedType)
    {
        WasmLocalInfo? local = DecodeWasmRegister(packed);

        Assert.NotNull(local);
        Assert.Equal(expectedIndex, local.Value.Index);
        Assert.Equal(expectedType, local.Value.ValueType);
    }

    /// <summary>
    /// Value type 0 is reserved by <c>MakeWasmReg</c> so that small raw values remain available
    /// for pseudo-registers. Those must not decode as local 0/1/2.
    /// </summary>
    [Theory]
    [InlineData(0u)]           // PC
    [InlineData(1u)]           // REGNUM_COUNT
    [InlineData(2u)]           // REGNUM_AMBIENT_SP
    [InlineData(0x1FFFFFFFu)]  // largest value with value type 0
    [InlineData(0xE0000000u)]  // value type 7, out of range
    [InlineData(0xFFFFFFFFu)]  // value type 7, out of range
    public void DecodeWasmRegister_NonLocalValues_ReturnNull(uint packed)
    {
        Assert.Null(DecodeWasmRegister(packed));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(31)]
    [InlineData(32)]
    public void WasmDebugInfoEncoding_UnsupportedShift_Throws(byte shift)
    {
        Assert.Throws<NotSupportedException>(
            () => new DebugInfoHelpers.WasmDebugInfoEncoding(shift, WasmDebugValueTypeCount));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    public void WasmDebugInfoEncoding_MismatchedValueTypeCount_Throws(byte valueTypeCount)
    {
        Assert.Throws<NotSupportedException>(
            () => new DebugInfoHelpers.WasmDebugInfoEncoding(WasmRegTypeShift, valueTypeCount));
    }

    [Fact]
    public void GetWasmDebugInfoEncoding_TargetGlobalsPresent_ReturnsAdvertisedEncoding()
    {
        Target target = CreateTarget(RuntimeInfoArchitecture.Wasm, is64Bit: false);

        DebugInfoHelpers.WasmDebugInfoEncoding encoding =
            DebugInfoHelpers.GetWasmDebugInfoEncoding(target);

        Assert.Equal(WasmRegTypeShift, encoding.RegisterTypeShift);
        Assert.Equal(WasmDebugValueTypeCount, encoding.ValueTypeCount);
    }

    [Fact]
    public void GetWasmDebugInfoEncoding_TargetGlobalsMissing_Throws()
    {
        Target target = CreateTarget(
            RuntimeInfoArchitecture.Wasm,
            is64Bit: false,
            includeWasmEncoding: false);

        Assert.Throws<InvalidOperationException>(
            () => DebugInfoHelpers.GetWasmDebugInfoEncoding(target));
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0xE0000000u)]
    public void DoVars_WasmInvalidRegisterEncoding_Throws(uint packedRegister)
    {
        byte[] encoded = EncodeNibbleUInts(
            1,
            unchecked(0u - MaxILNum),
            0,
            1,
            VLT_REG,
            packedRegister);
        NativeReader reader = new(new MemoryStream(encoded));

        Assert.Throws<InvalidOperationException>(
            () => DebugInfoHelpers.DoVars(reader, isX86: false, CurrentEncoding).ToList());
    }

    private const uint MaxILNum = unchecked((uint)-6);

    // VarLocType values from ICorDebugInfo (cordebuginfo.h), in declaration order.
    private const uint VLT_REG = 0;
    private const uint VLT_STK = 3;
    private const uint VLT_REG_REG = 5;

    /// <summary>
    /// A vars blob holding three entries: an f64 in a single WASM local, an i32 pair spanning two
    /// WASM locals, and a stack slot based off the WASM frame-pointer local.
    /// </summary>
    private static byte[] EncodeWasmVarsBlob() => EncodeNibbleUInts(
        3,
        // var 0: [0x00, 0x10) in WASM local $4 (f64)
        unchecked(0u - MaxILNum), 0x00, 0x10, VLT_REG, PackWasmRegister(4, (uint)WasmDebugValueType.F64),
        // var 1: [0x10, 0x30) spanning WASM locals $7 and $8 (i32)
        unchecked(1u - MaxILNum), 0x10, 0x20, VLT_REG_REG, PackWasmRegister(7, (uint)WasmDebugValueType.I32), PackWasmRegister(8, (uint)WasmDebugValueType.I32),
        // var 2: [0x30, 0x40) at [logical FP + 0x18]; WASM stack bases encode as register 2.
        // The signed offset is encoded with the sign in bit 0.
        unchecked(2u - MaxILNum), 0x30, 0x10, VLT_STK, JitEmittedWasmStackBaseRegister, 0x18 << 1);

    [Fact]
    public void DoVars_Wasm_PromotesKindsAndDecodesLocals()
    {
        NativeReader reader = new(new MemoryStream(EncodeWasmVarsBlob()));
        List<DebugVarInfo> result = new(DebugInfoHelpers.DoVars(reader, isX86: false, CurrentEncoding));

        Assert.Collection(
            result,
            varInfo =>
            {
                Assert.Equal(DebugVarLocKind.WasmLocal, varInfo.Kind);
                Assert.NotNull(varInfo.WasmLocal);
                Assert.Equal(4u, varInfo.WasmLocal.Value.Index);
                Assert.Equal(WasmDebugValueType.F64, varInfo.WasmLocal.Value.ValueType);
            },
            varInfo =>
            {
                Assert.Equal(DebugVarLocKind.WasmLocalPair, varInfo.Kind);
                Assert.NotNull(varInfo.WasmLocal);
                Assert.NotNull(varInfo.WasmLocal2);
                Assert.Equal(7u, varInfo.WasmLocal.Value.Index);
                Assert.Equal(8u, varInfo.WasmLocal2.Value.Index);
                Assert.Equal(WasmDebugValueType.I32, varInfo.WasmLocal2.Value.ValueType);
            },
            varInfo =>
            {
                // Stack slots live in linear memory, so the kind and encoded base are unchanged.
                Assert.Equal(DebugVarLocKind.Stack, varInfo.Kind);
                Assert.Null(varInfo.WasmLocal);
                Assert.Equal(JitEmittedWasmStackBaseRegister, varInfo.BaseRegister);
                Assert.Equal(0x18, varInfo.StackOffset);
            });
    }

    /// <summary>
    /// The identical blob decoded as a non-WASM target must keep the architecture-neutral kinds
    /// and decode no locals, so the projection cannot leak into other architectures.
    /// </summary>
    [Fact]
    public void DoVars_NonWasm_LeavesKindsAndLocalsAlone()
    {
        NativeReader reader = new(new MemoryStream(EncodeWasmVarsBlob()));
        List<DebugVarInfo> result = new(DebugInfoHelpers.DoVars(reader, isX86: false));

        Assert.Collection(
            result,
            varInfo =>
            {
                Assert.Equal(DebugVarLocKind.Register, varInfo.Kind);
                Assert.Null(varInfo.WasmLocal);
            },
            varInfo =>
            {
                Assert.Equal(DebugVarLocKind.RegisterRegister, varInfo.Kind);
                Assert.Null(varInfo.WasmLocal);
                Assert.Null(varInfo.WasmLocal2);
            },
            varInfo =>
            {
                Assert.Equal(DebugVarLocKind.Stack, varInfo.Kind);
            });
    }

    /// <summary>
    /// DacDbi's <c>VarLoc</c> mirrors <c>ICorDebugInfo::VarLoc</c>, where a WASM local is encoded
    /// as <c>VLT_REG</c> whose register number is the packed (local index, value type) tuple.
    /// The projected WASM kinds must therefore round-trip to the original ICorDebugInfo form
    /// rather than degrading to <c>VLT_INVALID</c>, which would lose the local index entirely.
    /// </summary>
    [Fact]
    public void ConvertToVarLoc_WasmLocal_RoundTripsToRegisterForms()
    {
        uint packed = PackWasmRegister(4, (uint)WasmDebugValueType.F64);
        uint packed2 = PackWasmRegister(5, (uint)WasmDebugValueType.I32);

        VarLoc single = DacDbiImpl.ConvertToVarLoc(new DebugVarInfo
        {
            Kind = DebugVarLocKind.WasmLocal,
            Register = packed,
            WasmLocal = DecodeWasmRegister(packed),
        });
        Assert.Equal(VarLocType.VLT_REG, single.vlType);
        Assert.Equal(packed, single.vlrReg);

        VarLoc byref = DacDbiImpl.ConvertToVarLoc(new DebugVarInfo
        {
            Kind = DebugVarLocKind.WasmLocal,
            IsByRef = true,
            Register = packed,
            WasmLocal = DecodeWasmRegister(packed),
        });
        Assert.Equal(VarLocType.VLT_REG_BYREF, byref.vlType);
        Assert.Equal(packed, byref.vlrReg);

        VarLoc pair = DacDbiImpl.ConvertToVarLoc(new DebugVarInfo
        {
            Kind = DebugVarLocKind.WasmLocalPair,
            Register = packed,
            Register2 = packed2,
            WasmLocal = DecodeWasmRegister(packed),
            WasmLocal2 = DecodeWasmRegister(packed2),
        });
        Assert.Equal(VarLocType.VLT_REG_REG, pair.vlType);
        Assert.Equal(packed, pair.vlrrReg1);
        Assert.Equal(packed2, pair.vlrrReg2);
    }

    /// <summary>
    /// The success path: a WASM stack slot resolves to <c>logical frame pointer + StackOffset</c>
    /// in linear memory. This exercises <c>ResolveWasmVarLocation</c> end to end through the real
    /// <c>WasmR2RInfo</c> / <c>WasmUnwinder</c>, rather than only asserting the refusal paths.
    /// </summary>
    [Theory]
    [InlineData(0x18, false)]
    [InlineData(0x00, false)]
    [InlineData(0x18, true)]
    public void ResolveVarLocation_WasmStack_ResolvesAgainstLogicalFramePointer(int stackOffset, bool isDoubleStack)
    {
        const ulong FrameAddress = 0x0020_0000;
        const uint FunctionIndex = WasmMockTarget.FunctionTableIndex;

        Target target = WasmMockTarget.Create(FrameAddress, FunctionIndex, isFunclet: false);
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
    /// For a funclet frame the WASM frame-pointer local refers to the parent method's frame, so the
    /// resolved address must be based on the establishing frame pointer recovered by unwinding out
    /// of the funclet — not on the funclet's own frame base.
    /// </summary>
    [Fact]
    public void ResolveVarLocation_WasmStackInFunclet_ResolvesAgainstEstablishingFrame()
    {
        const ulong FuncletFrame = 0x0020_0000;
        const int StackOffset = 0x18;

        Target target = WasmMockTarget.Create(FuncletFrame, WasmMockTarget.FunctionTableIndex, isFunclet: true);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(target);
        context.StackPointer = new TargetPointer(FuncletFrame);
        context.FramePointer = new TargetPointer(WasmMockTarget.EstablishingFramePointer);

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
        Assert.Equal(WasmMockTarget.EstablishingFramePointer + StackOffset, location.AddressOrValue);
        Assert.NotEqual(FuncletFrame + StackOffset, location.AddressOrValue);
    }

    /// <summary>
    /// Nibble encoder matching CoreCLR's <c>NibbleWriter::WriteEncodedU32</c>: three value bits per
    /// nibble, high bit set on every nibble but the last, most significant group first, low nibble
    /// of each byte used before the high nibble.
    /// </summary>
    private static byte[] EncodeNibbleUInts(params uint[] values)
    {
        List<byte> nibbles = new();
        Span<byte> groups = stackalloc byte[11];
        foreach (uint value in values)
        {
            int groupCount = 0;
            uint remaining = value;
            do
            {
                groups[groupCount++] = (byte)(remaining & 7);
                remaining >>= 3;
            }
            while (remaining != 0);

            for (int i = groupCount - 1; i >= 0; i--)
            {
                byte continuation = i == 0 ? (byte)0 : (byte)8;
                nibbles.Add((byte)(groups[i] | continuation));
            }
        }

        byte[] bytes = new byte[(nibbles.Count + 1) / 2];
        for (int i = 0; i < nibbles.Count; i++)
            bytes[i / 2] |= (byte)(nibbles[i] << (4 * (i & 1)));
        return bytes;
    }
}
