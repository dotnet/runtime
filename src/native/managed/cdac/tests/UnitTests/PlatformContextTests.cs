// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class PlatformContextTests
{
    [Theory]
    [InlineData(0, 0xAABBCCDD11223344UL)]
    [InlineData(4, 0x1234UL)] // Rsp
    [InlineData(15, 0xFFFFUL)] // R15
    public void AMD64_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new AMD64Context();
        Assert.True(ctx.TrySetRegister(regNum, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(regNum, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Theory]
    [InlineData(16)]
    public void AMD64_OutOfRange_ReturnsFalse(int regNum)
    {
        var ctx = new AMD64Context();
        Assert.False(ctx.TrySetRegister(regNum, new TargetNUInt(0)));
        Assert.False(ctx.TryReadRegister(regNum, out _));
    }

    [Theory]
    [InlineData(0, 0x1234UL)]  // X0
    [InlineData(29, 0xABCDUL)] // Fp
    [InlineData(31, 0x5678UL)] // Sp
    [InlineData(32, 0x9000UL)] // Pc
    public void ARM64_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new ARM64Context();
        Assert.True(ctx.TrySetRegister(regNum, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(regNum, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Theory]
    [InlineData(33)]
    public void ARM64_OutOfRange_ReturnsFalse(int regNum)
    {
        var ctx = new ARM64Context();
        Assert.False(ctx.TrySetRegister(regNum, new TargetNUInt(0)));
        Assert.False(ctx.TryReadRegister(regNum, out _));
    }

    [Theory]
    [InlineData(0, 0x12U)]   // R0
    [InlineData(13, 0x100U)] // Sp
    [InlineData(15, 0x40U)]  // Pc
    [InlineData(16, 0x10U)]  // Cpsr
    public void ARM_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new ARMContext();
        Assert.True(ctx.TrySetRegister(regNum, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(regNum, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Theory]
    [InlineData(17)]
    public void ARM_OutOfRange_ReturnsFalse(int regNum)
    {
        var ctx = new ARMContext();
        Assert.False(ctx.TrySetRegister(regNum, new TargetNUInt(0)));
        Assert.False(ctx.TryReadRegister(regNum, out _));
    }

    [Theory]
    [InlineData(0, 0xDEADBEEFUL)] // Eax
    [InlineData(4, 0x1000UL)]     // Esp
    [InlineData(7, 0xABCDUL)]     // Edi
    public void X86_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new X86Context();
        Assert.True(ctx.TrySetRegister(regNum, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(regNum, out TargetNUInt result));
        Assert.Equal(testValue & uint.MaxValue, result.Value); // X86 fields are uint
    }

    [Theory]
    [InlineData(8)]
    public void X86_OutOfRange_ReturnsFalse(int regNum)
    {
        var ctx = new X86Context();
        Assert.False(ctx.TrySetRegister(regNum, new TargetNUInt(0)));
        Assert.False(ctx.TryReadRegister(regNum, out _));
    }

    [Theory]
    [InlineData(0, 0x1234UL)]   // R0
    [InlineData(3, 0x2000UL)]   // Sp
    [InlineData(31, 0xABCDUL)]  // S8
    public void LoongArch64_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new LoongArch64Context();
        Assert.True(ctx.TrySetRegister(regNum, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(regNum, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Theory]
    [InlineData(32)]
    public void LoongArch64_OutOfRange_ReturnsFalse(int regNum)
    {
        var ctx = new LoongArch64Context();
        Assert.False(ctx.TrySetRegister(regNum, new TargetNUInt(0)));
        Assert.False(ctx.TryReadRegister(regNum, out _));
    }

    [Theory]
    [InlineData(1, 0x1234UL)]  // Ra
    [InlineData(2, 0x2000UL)]  // Sp
    [InlineData(31, 0xABCDUL)] // T6
    public void RISCV64_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new RISCV64Context();
        Assert.True(ctx.TrySetRegister(regNum, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(regNum, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Theory]
    [InlineData(32)]
    public void RISCV64_OutOfRange_ReturnsFalse(int regNum)
    {
        var ctx = new RISCV64Context();
        Assert.False(ctx.TrySetRegister(regNum, new TargetNUInt(0)));
        Assert.False(ctx.TryReadRegister(regNum, out _));
    }

    [Fact]
    public void RISCV64_ZeroRegister_ReadReturnsZero_WriteReturnsFalse()
    {
        var ctx = new RISCV64Context();
        Assert.True(ctx.TryReadRegister(0, out TargetNUInt value));
        Assert.Equal(0UL, value.Value);
        Assert.False(ctx.TrySetRegister(0, new TargetNUInt(0xDEAD)));
    }
}