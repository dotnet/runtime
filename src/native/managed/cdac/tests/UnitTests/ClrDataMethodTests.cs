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

public unsafe class ClrDataMethodTests
{
    private const uint MethodToken = 0x06000001;
    private static readonly MockTarget.Architecture s_arch = new() { IsLittleEndian = true, Is64Bit = true };
    private static readonly TargetPointer s_module = new(0x1000);
    private static readonly TargetPointer s_methodDesc = new(0x2000);
    private static readonly ModuleHandle s_moduleHandle = new(new TargetPointer(0x3000));

    [Theory]
    [InlineData(false, 0u)]
    [InlineData(true, 1u)]
    public void GetFlags_UsesSignatureHasThis(bool hasThis, uint expectedFlags)
    {
        byte[] signature = [hasThis ? (byte)0x20 : (byte)0x00, 0x00, 0x01];
        TestPlaceholderTarget target = CreateTarget(signature);

        IXCLRDataMethodDefinition definition = new ClrDataMethodDefinition(
            target,
            s_module,
            MethodToken,
            legacyImpl: null);
        uint definitionFlags;
        Assert.Equal(HResults.S_OK, definition.GetFlags(&definitionFlags));
        Assert.Equal(expectedFlags, definitionFlags);

        IXCLRDataMethodInstance instance = new ClrDataMethodInstance(
            target,
            new MethodDescHandle(s_methodDesc),
            appDomain: TargetPointer.Null,
            legacyImpl: null);
        uint instanceFlags;
        Assert.Equal(HResults.S_OK, instance.GetFlags(&instanceFlags));
        Assert.Equal(expectedFlags, instanceFlags);
    }

    private static TestPlaceholderTarget CreateTarget(byte[] signature)
    {
        TargetPointer methodDefToDesc = new(0x4000);
        ModuleLookupTables lookupTables = new(
            FieldDefToDesc: TargetPointer.Null,
            ManifestModuleReferences: TargetPointer.Null,
            MemberRefToDesc: TargetPointer.Null,
            MethodDefToDesc: methodDefToDesc,
            TypeDefToMethodTable: TargetPointer.Null,
            TypeRefToMethodTable: TargetPointer.Null,
            MethodDefToILCodeVersioningState: TargetPointer.Null,
            TableDataOffset: 0);
        TargetNUInt lookupFlags = default;

        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetModuleHandleFromModulePtr(s_module)).Returns(s_moduleHandle);
        loader.Setup(l => l.GetLookupTables(s_moduleHandle)).Returns(lookupTables);
        loader.Setup(l => l.GetModuleLookupMapElement(methodDefToDesc, MethodToken, out lookupFlags))
            .Returns(s_methodDesc);

        return new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(loader)
            .AddMockContract<IRuntimeTypeSystem>(new SignatureRuntimeTypeSystem(signature))
            .Build();
    }

    private sealed class SignatureRuntimeTypeSystem(byte[] signature) : IRuntimeTypeSystem
    {
        private readonly byte[] _signature = signature;

        public MethodDescHandle GetMethodDescHandle(TargetPointer address) => new(address);

        public bool TryGetMethodSignature(MethodDescHandle methodDesc, out ReadOnlySpan<byte> signature)
        {
            signature = _signature;
            return true;
        }
    }
}
