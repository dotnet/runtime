// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class SignatureTests
{
    private static TargetTestHelpers.LayoutResult GetVASigCookieLayout(TargetTestHelpers helpers)
    {
        return helpers.LayoutFields(
        [
            new(nameof(Data.VASigCookie.SizeOfArgs), DataType.uint32),
            new(nameof(Data.VASigCookie.SignaturePointer), DataType.pointer),
            new(nameof(Data.VASigCookie.SignatureLength), DataType.uint32),
        ]);
    }

    /// <summary>
    /// Build a target with a single VASigCookie at a known address and a slot containing
    /// a pointer to it (i.e., the "VASigCookieAddr" passed to the contract APIs).
    /// </summary>
    private static TestPlaceholderTarget BuildTarget(
        MockTarget.Architecture arch,
        string targetArchitecture,
        uint sizeOfArgs,
        ulong signaturePointer,
        uint signatureLength,
        out ulong vaSigCookieAddr,
        out ulong vaSigCookiePtr,
        bool nullCookie = false)
    {
        TargetTestHelpers helpers = new(arch);
        var builder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.Builder memBuilder = builder.MemoryBuilder;
        MockMemorySpace.BumpAllocator allocator = memBuilder.CreateAllocator(0x1_0000, 0x2_0000);

        TargetTestHelpers.LayoutResult layout = GetVASigCookieLayout(helpers);
        builder.AddTypes(new()
        {
            [DataType.VASigCookie] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride },
        });

        // Allocate and populate the VASigCookie struct.
        MockMemorySpace.HeapFragment cookieFrag = allocator.Allocate(layout.Stride, "VASigCookie");
        helpers.Write(cookieFrag.Data.AsSpan(layout.Fields[nameof(Data.VASigCookie.SizeOfArgs)].Offset, sizeof(uint)), sizeOfArgs);
        helpers.WritePointer(cookieFrag.Data.AsSpan(layout.Fields[nameof(Data.VASigCookie.SignaturePointer)].Offset, helpers.PointerSize), signaturePointer);
        helpers.Write(cookieFrag.Data.AsSpan(layout.Fields[nameof(Data.VASigCookie.SignatureLength)].Offset, sizeof(uint)), signatureLength);
        vaSigCookiePtr = cookieFrag.Address;

        // Allocate the slot that holds the pointer to the VASigCookie. This is the address
        // passed to GetVarArgArgsBase / GetVarArgSignature.
        MockMemorySpace.HeapFragment slotFrag = allocator.Allocate((ulong)helpers.PointerSize, "VASigCookieSlot");
        helpers.WritePointer(slotFrag.Data, nullCookie ? 0 : cookieFrag.Address);
        vaSigCookieAddr = slotFrag.Address;

        // RuntimeInfo contract reads architecture from this global.
        builder.AddGlobalStrings((Constants.Globals.Architecture, targetArchitecture));
        builder.AddContract<IRuntimeInfo>(version: "c1");
        builder.AddContract<ISignature>(version: "c1");

        return builder.Build();
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetVarArgSignature_ReturnsCookieSignature(MockTarget.Architecture arch)
    {
        const uint expectedSizeOfArgs = 0x40;
        const ulong expectedSigPtr = 0x12_3400;
        const uint expectedSigLen = 12;

        Target target = BuildTarget(arch, "x64", expectedSizeOfArgs, expectedSigPtr, expectedSigLen,
            out ulong vaSigCookieAddr, out _);
        ISignature signature = target.Contracts.Signature;

        signature.GetVarArgSignature(new TargetPointer(vaSigCookieAddr), out TargetPointer sigAddr, out uint sigLen);

        Assert.Equal(expectedSigPtr, sigAddr.Value);
        Assert.Equal(expectedSigLen, sigLen);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetVarArgArgsBase_NonX86_ReturnsCookieAddrPlusPointerSize(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, "x64", sizeOfArgs: 0x80, signaturePointer: 0x1000, signatureLength: 4,
            out ulong vaSigCookieAddr, out _);
        ISignature signature = target.Contracts.Signature;

        TargetPointer argBase = signature.GetVarArgArgsBase(new TargetPointer(vaSigCookieAddr));

        Assert.Equal(vaSigCookieAddr + (ulong)(arch.Is64Bit ? 8 : 4), argBase.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetVarArgArgsBase_X86_ReturnsCookieAddrPlusSizeOfArgs(MockTarget.Architecture arch)
    {
        const uint sizeOfArgs = 0x18;
        Target target = BuildTarget(arch, "x86", sizeOfArgs, signaturePointer: 0x1000, signatureLength: 4,
            out ulong vaSigCookieAddr, out _);
        ISignature signature = target.Contracts.Signature;

        TargetPointer argBase = signature.GetVarArgArgsBase(new TargetPointer(vaSigCookieAddr));

        Assert.Equal(vaSigCookieAddr + sizeOfArgs, argBase.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetVarArgSignature_NullCookieAddr_Throws(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, "x64", sizeOfArgs: 0, signaturePointer: 0, signatureLength: 0,
            out _, out _);
        ISignature signature = target.Contracts.Signature;

        Assert.Throws<ArgumentException>(() => signature.GetVarArgSignature(TargetPointer.Null, out _, out _));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetVarArgArgsBase_NullCookieAddr_Throws(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, "x64", sizeOfArgs: 0, signaturePointer: 0, signatureLength: 0,
            out _, out _);
        ISignature signature = target.Contracts.Signature;

        Assert.Throws<ArgumentException>(() => signature.GetVarArgArgsBase(TargetPointer.Null));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetVarArgSignature_NullCookiePointer_Throws(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, "x64", sizeOfArgs: 0, signaturePointer: 0, signatureLength: 0,
            out ulong vaSigCookieAddr, out _, nullCookie: true);
        ISignature signature = target.Contracts.Signature;

        Assert.Throws<InvalidOperationException>(() => signature.GetVarArgSignature(new TargetPointer(vaSigCookieAddr), out _, out _));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetVarArgArgsBase_NullCookiePointer_Throws(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, "x64", sizeOfArgs: 0, signaturePointer: 0, signatureLength: 0,
            out ulong vaSigCookieAddr, out _, nullCookie: true);
        ISignature signature = target.Contracts.Signature;

        Assert.Throws<InvalidOperationException>(() => signature.GetVarArgArgsBase(new TargetPointer(vaSigCookieAddr)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetVarArgSignature_ZeroLengthSignature_ReturnsZeroes(MockTarget.Architecture arch)
    {
        Target target = BuildTarget(arch, "x64", sizeOfArgs: 0x10, signaturePointer: 0, signatureLength: 0,
            out ulong vaSigCookieAddr, out _);
        ISignature signature = target.Contracts.Signature;

        signature.GetVarArgSignature(new TargetPointer(vaSigCookieAddr), out TargetPointer sigAddr, out uint sigLen);

        Assert.Equal(TargetPointer.Null, sigAddr);
        Assert.Equal(0u, sigLen);
    }
}
