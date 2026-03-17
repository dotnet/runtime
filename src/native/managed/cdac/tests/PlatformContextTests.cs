// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class PlatformContextTests
{
    [Theory]
    // AMD64: GP registers 0-15
    [InlineData(0, "Rax")] [InlineData(1, "Rcx")] [InlineData(7, "Rdi")]
    [InlineData(8, "R8")] [InlineData(15, "R15")]
    [InlineData(16, null)]
    public void AMD64_TryGetRegisterName(int number, string? expected)
    {
        var ctx = new AMD64Context();
        Assert.Equal(expected != null, ctx.TryGetRegisterName(number, out string? name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(0, 0xAABBCCDD11223344UL)]
    [InlineData(4, 0x1234UL)] // Rsp
    [InlineData(15, 0xFFFFUL)] // R15
    public void AMD64_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new AMD64Context();
        Assert.True(ctx.TryGetRegisterName(regNum, out string? name));
        Assert.True(ctx.TrySetRegister(name, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(name, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Theory]
    // ARM64: 0-28=X0..X28, 29=Fp, 30=Lr, 31=Sp, 32=Pc
    [InlineData(0, "X0")] [InlineData(28, "X28")] [InlineData(29, "Fp")]
    [InlineData(30, "Lr")] [InlineData(31, "Sp")] [InlineData(32, "Pc")]
    [InlineData(33, null)]
    public void ARM64_TryGetRegisterName(int number, string? expected)
    {
        var ctx = new ARM64Context();
        Assert.Equal(expected != null, ctx.TryGetRegisterName(number, out string? name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(0, 0x1234UL)]  // X0
    [InlineData(29, 0xABCDUL)] // Fp
    [InlineData(31, 0x5678UL)] // Sp
    [InlineData(32, 0x9000UL)] // Pc
    public void ARM64_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new ARM64Context();
        Assert.True(ctx.TryGetRegisterName(regNum, out string? name));
        Assert.True(ctx.TrySetRegister(name, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(name, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Theory]
    // ARM: 0-12=R0..R12, 13=Sp, 14=Lr, 15=Pc, 16=Cpsr
    [InlineData(0, "R0")] [InlineData(12, "R12")] [InlineData(13, "Sp")]
    [InlineData(14, "Lr")] [InlineData(15, "Pc")] [InlineData(16, "Cpsr")]
    [InlineData(17, null)]
    public void ARM_TryGetRegisterName(int number, string? expected)
    {
        var ctx = new ARMContext();
        Assert.Equal(expected != null, ctx.TryGetRegisterName(number, out string? name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(0, 0x12U)]   // R0
    [InlineData(13, 0x100U)] // Sp
    [InlineData(15, 0x40U)]  // Pc
    [InlineData(16, 0x10U)]  // Cpsr
    public void ARM_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new ARMContext();
        Assert.True(ctx.TryGetRegisterName(regNum, out string? name));
        Assert.True(ctx.TrySetRegister(name, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(name, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Theory]
    // X86: 0=Eax, 1=Ecx, 2=Edx, 3=Ebx, 4=Esp, 5=Ebp, 6=Esi, 7=Edi
    [InlineData(0, "Eax")] [InlineData(3, "Ebx")] [InlineData(7, "Edi")]
    [InlineData(8, null)]
    public void X86_TryGetRegisterName(int number, string? expected)
    {
        var ctx = new X86Context();
        Assert.Equal(expected != null, ctx.TryGetRegisterName(number, out string? name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(0, 0xDEADBEEFUL)] // Eax
    [InlineData(4, 0x1000UL)]     // Esp
    [InlineData(7, 0xABCDUL)]     // Edi
    public void X86_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new X86Context();
        Assert.True(ctx.TryGetRegisterName(regNum, out string? name));
        Assert.True(ctx.TrySetRegister(name, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(name, out TargetNUInt result));
        Assert.Equal(testValue & uint.MaxValue, result.Value); // X86 fields are uint
    }

    [Theory]
    // LoongArch64: 0=R0 .. 31=S8
    [InlineData(0, "R0")] [InlineData(3, "Sp")] [InlineData(21, "X0")]
    [InlineData(31, "S8")]
    [InlineData(32, null)]
    public void LoongArch64_TryGetRegisterName(int number, string? expected)
    {
        var ctx = new LoongArch64Context();
        Assert.Equal(expected != null, ctx.TryGetRegisterName(number, out string? name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(0, 0x1234UL)]   // R0
    [InlineData(3, 0x2000UL)]   // Sp
    [InlineData(31, 0xABCDUL)]  // S8
    public void LoongArch64_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new LoongArch64Context();
        Assert.True(ctx.TryGetRegisterName(regNum, out string? name));
        Assert.True(ctx.TrySetRegister(name, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(name, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Theory]
    // RISCV64: 0=zero (read-only), 1=Ra, 2=Sp .. 31=T6
    [InlineData(0, "zero")] [InlineData(1, "Ra")] [InlineData(2, "Sp")]
    [InlineData(31, "T6")]
    [InlineData(32, null)]
    public void RISCV64_TryGetRegisterName(int number, string? expected)
    {
        var ctx = new RISCV64Context();
        Assert.Equal(expected != null, ctx.TryGetRegisterName(number, out string? name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(1, 0x1234UL)]  // Ra
    [InlineData(2, 0x2000UL)]  // Sp
    [InlineData(31, 0xABCDUL)] // T6
    public void RISCV64_TrySetAndRead_ByNumber_RoundTrips(int regNum, ulong testValue)
    {
        var ctx = new RISCV64Context();
        Assert.True(ctx.TryGetRegisterName(regNum, out string? name));
        Assert.True(ctx.TrySetRegister(name, new TargetNUInt(testValue)));
        Assert.True(ctx.TryReadRegister(name, out TargetNUInt result));
        Assert.Equal(testValue, result.Value);
    }

    [Fact]
    public void RISCV64_ZeroRegister_ReadReturnsZero_WriteReturnsFalse()
    {
        var ctx = new RISCV64Context();
        Assert.True(ctx.TryReadRegister("zero", out TargetNUInt value));
        Assert.Equal(0UL, value.Value);
        Assert.False(ctx.TrySetRegister("zero", new TargetNUInt(0xDEAD)));
    }
}