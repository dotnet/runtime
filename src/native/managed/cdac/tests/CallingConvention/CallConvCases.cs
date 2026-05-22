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

    public static IEnumerable<object[]> AllCases => new[]
    {
        new object[] { AMD64Windows }, new object[] { AMD64Unix },
    };

    public static IEnumerable<object[]> AMD64UnixOnly => new[] { new object[] { AMD64Unix } };
}
