// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class SOSDacInterfaceAssemblyLocationTests
{
    private static readonly MockTarget.Architecture s_arch = new() { IsLittleEndian = true, Is64Bit = true };
    private static readonly TargetPointer s_assembly = new(0x1000);
    private static readonly ModuleHandle s_module = new(new TargetPointer(0x2000));

    [Theory]
    [InlineData(@"C:\runtime\TestAssembly.dll")]
    [InlineData("")]
    public void GetAssemblyLocation_CopiesPathIncludingEmpty(string path)
    {
        ISOSDacInterface sos = CreateSOSDac(loader =>
        {
            loader.Setup(l => l.GetModuleHandleFromAssemblyPtr(s_assembly)).Returns(s_module);
            loader.Setup(l => l.GetPath(s_module)).Returns(path);
        });

        uint needed;
        Assert.Equal(
            HResults.S_OK,
            sos.GetAssemblyLocation(new ClrDataAddress(s_assembly.Value), 0, null, &needed));
        Assert.Equal((uint)path.Length + 1, needed);
        Assert.Equal(
            HResults.S_OK,
            sos.GetAssemblyLocation(new ClrDataAddress(s_assembly.Value), -1, null, &needed));

        char[] buffer = new char[needed];
        fixed (char* bufferPtr = buffer)
        {
            Assert.Equal(
                HResults.S_OK,
                sos.GetAssemblyLocation(new ClrDataAddress(s_assembly.Value), buffer.Length, bufferPtr, &needed));
        }

        Assert.Equal(path, new string(buffer, 0, path.Length));
        Assert.Equal('\0', buffer[path.Length]);
    }

    [Fact]
    public void GetAssemblyLocation_ValidatesArguments()
    {
        ISOSDacInterface sos = CreateSOSDac(_ => { });
        char value = 'x';
        uint needed;

        Assert.Equal(HResults.E_INVALIDARG, sos.GetAssemblyLocation(default, 1, &value, &needed));
        Assert.Equal(HResults.E_INVALIDARG, sos.GetAssemblyLocation(new ClrDataAddress(s_assembly.Value), 0, null, null));
        Assert.Equal(HResults.E_INVALIDARG, sos.GetAssemblyLocation(new ClrDataAddress(s_assembly.Value), 0, &value, &needed));
    }

    private static ISOSDacInterface CreateSOSDac(Action<Mock<ILoader>> configure)
    {
        var loader = new Mock<ILoader>();
        configure(loader);
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(loader)
            .Build();
        return new SOSDacImpl(target, legacyObj: null);
    }
}
