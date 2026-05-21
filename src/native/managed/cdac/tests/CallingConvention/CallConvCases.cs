// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public sealed record CallConvTestCase(
    string Name,
    RuntimeInfoArchitecture Architecture,
    RuntimeInfoOperatingSystem OperatingSystem,
    bool Is64Bit,
    int TransitionBlockSize,
    int ArgumentRegistersOffset,
    int FirstGCRefMapSlot,
    int OffsetOfArgs,
    int? OffsetOfFloatArgumentRegisters,
    int NumArgumentRegisters,
    int NumFloatArgumentRegisters,
    int FloatRegisterSize)
{
    public int PointerSize => Is64Bit ? 8 : 4;
    public MockTarget.Architecture MockArch => new() { IsLittleEndian = true, Is64Bit = Is64Bit };
    public override string ToString() => Name;
}

public static class CallConvCases
{
    public static readonly CallConvTestCase X86 = new(
        "x86", RuntimeInfoArchitecture.X86, RuntimeInfoOperatingSystem.Windows, Is64Bit: false,
        TransitionBlockSize: 20, ArgumentRegistersOffset: 0, FirstGCRefMapSlot: 0,
        OffsetOfArgs: 20, OffsetOfFloatArgumentRegisters: null,
        NumArgumentRegisters: 2, NumFloatArgumentRegisters: 0, FloatRegisterSize: 0);

    public static readonly CallConvTestCase AMD64Windows = new(
        "AMD64-Windows", RuntimeInfoArchitecture.X64, RuntimeInfoOperatingSystem.Windows, Is64Bit: true,
        TransitionBlockSize: 40, ArgumentRegistersOffset: 40, FirstGCRefMapSlot: 40,
        OffsetOfArgs: 40, OffsetOfFloatArgumentRegisters: 0,
        NumArgumentRegisters: 4, NumFloatArgumentRegisters: 0, FloatRegisterSize: 16);

    public static readonly CallConvTestCase AMD64Unix = new(
        "AMD64-Unix", RuntimeInfoArchitecture.X64, RuntimeInfoOperatingSystem.Unix, Is64Bit: true,
        TransitionBlockSize: 48, ArgumentRegistersOffset: 0, FirstGCRefMapSlot: 0,
        OffsetOfArgs: 48, OffsetOfFloatArgumentRegisters: -128,
        NumArgumentRegisters: 6, NumFloatArgumentRegisters: 8, FloatRegisterSize: 16);

    public static readonly CallConvTestCase Arm32 = new(
        "ARM32", RuntimeInfoArchitecture.Arm, RuntimeInfoOperatingSystem.Windows, Is64Bit: false,
        TransitionBlockSize: 48, ArgumentRegistersOffset: 32, FirstGCRefMapSlot: 32,
        OffsetOfArgs: 48, OffsetOfFloatArgumentRegisters: -68,
        NumArgumentRegisters: 4, NumFloatArgumentRegisters: 16, FloatRegisterSize: 4);

    public static readonly CallConvTestCase Arm64Windows = new(
        "ARM64-Windows", RuntimeInfoArchitecture.Arm64, RuntimeInfoOperatingSystem.Windows, Is64Bit: true,
        TransitionBlockSize: 160, ArgumentRegistersOffset: 96, FirstGCRefMapSlot: 88,
        OffsetOfArgs: 160, OffsetOfFloatArgumentRegisters: -128,
        NumArgumentRegisters: 8, NumFloatArgumentRegisters: 8, FloatRegisterSize: 16);

    public static readonly CallConvTestCase Arm64Apple = new(
        "ARM64-Apple", RuntimeInfoArchitecture.Arm64, RuntimeInfoOperatingSystem.Apple, Is64Bit: true,
        TransitionBlockSize: 160, ArgumentRegistersOffset: 96, FirstGCRefMapSlot: 88,
        OffsetOfArgs: 160, OffsetOfFloatArgumentRegisters: -128,
        NumArgumentRegisters: 8, NumFloatArgumentRegisters: 8, FloatRegisterSize: 16);

    public static readonly CallConvTestCase LoongArch64 = new(
        "LoongArch64", RuntimeInfoArchitecture.LoongArch64, RuntimeInfoOperatingSystem.Unix, Is64Bit: true,
        TransitionBlockSize: 176, ArgumentRegistersOffset: 112, FirstGCRefMapSlot: 112,
        OffsetOfArgs: 176, OffsetOfFloatArgumentRegisters: -64,
        NumArgumentRegisters: 8, NumFloatArgumentRegisters: 8, FloatRegisterSize: 8);

    public static readonly CallConvTestCase RiscV64 = new(
        "RiscV64", RuntimeInfoArchitecture.RiscV64, RuntimeInfoOperatingSystem.Unix, Is64Bit: true,
        TransitionBlockSize: 192, ArgumentRegistersOffset: 128, FirstGCRefMapSlot: 128,
        OffsetOfArgs: 192, OffsetOfFloatArgumentRegisters: -64,
        NumArgumentRegisters: 8, NumFloatArgumentRegisters: 8, FloatRegisterSize: 8);

    public static IEnumerable<object[]> AllCases => new[]
    {
        new object[] { X86 }, new object[] { AMD64Windows }, new object[] { AMD64Unix }, new object[] { Arm32 },
        new object[] { Arm64Windows }, new object[] { Arm64Apple }, new object[] { LoongArch64 }, new object[] { RiscV64 },
    };

    public static IEnumerable<object[]> AMD64UnixOnly => new[] { new object[] { AMD64Unix } };
}
