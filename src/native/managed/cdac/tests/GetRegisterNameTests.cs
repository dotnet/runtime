// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class GetRegisterNameTests
{
    private static SOSDacImpl CreateSosDacImpl(
        MockTarget.Architecture arch,
        RuntimeInfoArchitecture targetArch)
    {
        MockMemorySpace.Builder builder = new MockMemorySpace.Builder(new TargetTestHelpers(arch));
        TestPlaceholderTarget target = new TestPlaceholderTarget(
            arch,
            builder.GetMemoryContext().ReadFromTarget,
            [],
            [],
            [(Constants.Globals.Architecture, targetArch.ToString().ToLowerInvariant())]);

        IContractFactory<IRuntimeInfo> runtimeInfoFactory = new RuntimeInfoFactory();
        ContractRegistry reg = Mock.Of<ContractRegistry>(
            c => c.RuntimeInfo == runtimeInfoFactory.CreateContract(target, 1));
        target.SetContracts(reg);

        return new SOSDacImpl(target, legacyObj: null);
    }

    public static IEnumerable<object[]> BasicRegisterNameData()
    {
        // AMD64 registers
        yield return [RuntimeInfoArchitecture.X64, 0, "rax"];
        yield return [RuntimeInfoArchitecture.X64, 1, "rcx"];
        yield return [RuntimeInfoArchitecture.X64, 5, "rbp"];
        yield return [RuntimeInfoArchitecture.X64, 15, "r15"];

        // x86 registers
        yield return [RuntimeInfoArchitecture.X86, 0, "eax"];
        yield return [RuntimeInfoArchitecture.X86, 7, "edi"];

        // ARM registers
        yield return [RuntimeInfoArchitecture.Arm, 0, "r0"];
        yield return [RuntimeInfoArchitecture.Arm, 13, "sp"];
        yield return [RuntimeInfoArchitecture.Arm, 14, "lr"];

        // ARM64 registers
        yield return [RuntimeInfoArchitecture.Arm64, 0, "X0"];
        yield return [RuntimeInfoArchitecture.Arm64, 29, "Fp"];
        yield return [RuntimeInfoArchitecture.Arm64, 30, "Lr"];
        yield return [RuntimeInfoArchitecture.Arm64, 31, "Sp"];

        // LoongArch64 registers
        yield return [RuntimeInfoArchitecture.LoongArch64, 0, "R0"];
        yield return [RuntimeInfoArchitecture.LoongArch64, 1, "RA"];
        yield return [RuntimeInfoArchitecture.LoongArch64, 3, "SP"];

        // RiscV64 registers
        yield return [RuntimeInfoArchitecture.RiscV64, 0, "R0"];
        yield return [RuntimeInfoArchitecture.RiscV64, 1, "RA"];
        yield return [RuntimeInfoArchitecture.RiscV64, 2, "SP"];
    }

    [Theory]
    [MemberData(nameof(BasicRegisterNameData))]
    public void GetRegisterName_ReturnsCorrectName(
        RuntimeInfoArchitecture targetArch,
        int regNum,
        string expectedName)
    {
        SOSDacImpl impl = CreateSosDacImpl(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true }, targetArch);
        ISOSDacInterface sos = impl;

        char[] buffer = new char[256];
        uint needed;
        int hr;
        fixed (char* pBuffer = buffer)
        {
            hr = sos.GetRegisterName(regNum, (uint)buffer.Length, pBuffer, &needed);
        }

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)(expectedName.Length + 1), needed);
        Assert.Equal(expectedName, new string(buffer, 0, (int)needed - 1));
    }

    [Theory]
    [InlineData(RuntimeInfoArchitecture.X64, 0, "rax")]
    [InlineData(RuntimeInfoArchitecture.X86, 3, "ebx")]
    [InlineData(RuntimeInfoArchitecture.Arm64, 0, "X0")]
    public void GetRegisterName_CallerFrame_PrependsCaller(
        RuntimeInfoArchitecture targetArch,
        int regNum,
        string expectedRegName)
    {
        SOSDacImpl impl = CreateSosDacImpl(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true }, targetArch);
        ISOSDacInterface sos = impl;

        // Caller frame registers are encoded as "-(reg+1)"
        int callerRegNum = -(regNum + 1);
        string expectedName = $"caller.{expectedRegName}";

        char[] buffer = new char[256];
        uint needed;
        int hr;
        fixed (char* pBuffer = buffer)
        {
            hr = sos.GetRegisterName(callerRegNum, (uint)buffer.Length, pBuffer, &needed);
        }

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)(expectedName.Length + 1), needed);
        Assert.Equal(expectedName, new string(buffer, 0, (int)needed - 1));
    }

    [Fact]
    public void GetRegisterName_NullBufferAndNullNeeded_ReturnsEPointer()
    {
        SOSDacImpl impl = CreateSosDacImpl(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true }, RuntimeInfoArchitecture.X64);
        ISOSDacInterface sos = impl;

        int hr = sos.GetRegisterName(0, 0, null, null);

        Assert.Equal(HResults.E_POINTER, hr);
    }

    [Fact]
    public void GetRegisterName_NullBuffer_SetsNeeded()
    {
        SOSDacImpl impl = CreateSosDacImpl(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true }, RuntimeInfoArchitecture.X64);
        ISOSDacInterface sos = impl;

        uint needed;
        int hr = sos.GetRegisterName(0, 0, null, &needed);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)("rax".Length + 1), needed);
    }

    [Theory]
    [InlineData(RuntimeInfoArchitecture.X64, 16)]
    [InlineData(RuntimeInfoArchitecture.X86, 8)]
    [InlineData(RuntimeInfoArchitecture.Arm, 15)]
    [InlineData(RuntimeInfoArchitecture.Arm64, 32)]
    [InlineData(RuntimeInfoArchitecture.LoongArch64, 32)]
    [InlineData(RuntimeInfoArchitecture.RiscV64, 32)]
    public void GetRegisterName_OutOfRange_ReturnsEUnexpected(
        RuntimeInfoArchitecture targetArch,
        int regNum)
    {
        SOSDacImpl impl = CreateSosDacImpl(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true }, targetArch);
        ISOSDacInterface sos = impl;

        uint needed;
        int hr;
        char[] buffer = new char[256];
        fixed (char* pBuffer = buffer)
        {
            hr = sos.GetRegisterName(regNum, (uint)buffer.Length, pBuffer, &needed);
        }

        Assert.Equal(unchecked((int)0x8000FFFF), hr); // E_UNEXPECTED
    }

    [Fact]
    public void GetRegisterName_SmallBuffer_ReturnsSFalse()
    {
        SOSDacImpl impl = CreateSosDacImpl(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true }, RuntimeInfoArchitecture.X64);
        ISOSDacInterface sos = impl;

        // "rax" needs 4 chars (3 + null), provide only 2
        char[] buffer = new char[2];
        uint needed;
        int hr;
        fixed (char* pBuffer = buffer)
        {
            hr = sos.GetRegisterName(0, (uint)buffer.Length, pBuffer, &needed);
        }

        Assert.Equal(HResults.S_FALSE, hr);
        Assert.Equal((uint)("rax".Length + 1), needed);
    }

    [Fact]
    public void GetRegisterName_UnsupportedArchitecture_ReturnsError()
    {
        SOSDacImpl impl = CreateSosDacImpl(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true }, RuntimeInfoArchitecture.Wasm);
        ISOSDacInterface sos = impl;

        uint needed;
        char[] buffer = new char[256];
        int hr;
        fixed (char* pBuffer = buffer)
        {
            hr = sos.GetRegisterName(0, (uint)buffer.Length, pBuffer, &needed);
        }

        Assert.NotEqual(HResults.S_OK, hr);
    }
}
