// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

internal class ContractDescriptorBuilder : MockMemorySpace.Builder
{
    // These addresses are arbitrary and are used to store the contract descriptor components.
    // They should not overlap with any other heap fragment addresses.
    private const ulong ContractDescriptorAddr = 0xaaaaaaaa;
    private const uint JsonDescriptorAddr = 0xdddddddd;
    private const uint ContractPointerDataAddr = 0xeeeeeeee;

    private bool _created = false;

    private IReadOnlyCollection<string> _contracts;
    private IDictionary<DataType, Target.TypeInfo> _types;
    private IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? TypeName)> _globals;
    private IReadOnlyCollection<ulong> _indirectValues;

    public ContractDescriptorBuilder(TargetTestHelpers targetTestHelpers)
        : base(targetTestHelpers)
    { }

    public ContractDescriptorBuilder SetContracts(IReadOnlyCollection<string> contracts)
    {
        if (_created)
            throw new InvalidOperationException("Context already created");
        _contracts = contracts;
        return this;
    }

    public ContractDescriptorBuilder SetTypes(IDictionary<DataType, Target.TypeInfo> types)
    {
        if (_created)
            throw new InvalidOperationException("Context already created");
        _types = types;
        return this;
    }

    public ContractDescriptorBuilder SetGlobals(IReadOnlyCollection<(string Name, ulong Value, string? TypeName)> globals)
    {
        if (_created)
            throw new InvalidOperationException("Context already created");
        if (_globals != null)
            throw new InvalidOperationException("Globals already set");
        _globals = globals.Select(g => (g.Name, (ulong?)g.Value, (uint?)null, g.TypeName)).ToArray();
        _indirectValues = null;
        return this;
    }

    public ContractDescriptorBuilder SetGlobals(IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? TypeName)> globals, IReadOnlyCollection<ulong> indirectValues)
    {
        if (_created)
            throw new InvalidOperationException("Context already created");
        if (_globals != null)
            throw new InvalidOperationException("Globals already set");
        _globals = globals;
        _indirectValues = indirectValues;
        return this;
    }

    private MockMemorySpace.HeapFragment CreateContractDescriptor(int jsonLength, int pointerDataCount)
    {
        byte[] descriptor = new byte[ContractDescriptorHelpers.Size(TargetTestHelpers.Arch.Is64Bit)];
        ContractDescriptorHelpers.Fill(descriptor, TargetTestHelpers.Arch, jsonLength, JsonDescriptorAddr, pointerDataCount, ContractPointerDataAddr);
        return new MockMemorySpace.HeapFragment
        {
            Address = ContractDescriptorAddr,
            Data = descriptor,
            Name = "ContractDescriptor"
        };
    }

    private string MakeContractsJson()
    {
        if (_contracts.Count == 0)
            return string.Empty;
        StringBuilder sb = new();
        foreach (var c in _contracts)
        {
            sb.Append($"\"{c}\": 1,");
        }
        Debug.Assert(sb.Length > 0);
        sb.Length--; // remove trailing comma
        return sb.ToString();
    }

    private (MockMemorySpace.HeapFragment json, MockMemorySpace.HeapFragment pointerData) CreateDataDescriptor()
    {
        string metadataTypesJson = _types is not null ? ContractDescriptorHelpers.MakeTypesJson(_types) : string.Empty;
        string metadataGlobalsJson = _globals is not null ? ContractDescriptorHelpers.MakeGlobalsJson(_globals) : string.Empty;
        string interpolatedContracts = _contracts is not null ? MakeContractsJson() : string.Empty;
        byte[] jsonBytes = Encoding.UTF8.GetBytes($$"""
        {
            "version": 0,
            "baseline": "empty",
            "contracts": { {{interpolatedContracts}} },
            "types": { {{metadataTypesJson}} },
            "globals": { {{metadataGlobalsJson}} }
        }
        """);
        MockMemorySpace.HeapFragment json = new()
        {
            Address = JsonDescriptorAddr,
            Data = jsonBytes,
            Name = "JsonDescriptor"
        };

        MockMemorySpace.HeapFragment pointerData;
        if (_indirectValues != null)
        {
            int pointerSize = TargetTestHelpers.PointerSize;
            byte[] pointerDataBytes = new byte[_indirectValues.Count * pointerSize];
            int offset = 0;
            foreach (var value in _indirectValues)
            {
                TargetTestHelpers.WritePointer(pointerDataBytes.AsSpan(offset, pointerSize), value);
                offset += pointerSize;
            }
            pointerData = new MockMemorySpace.HeapFragment
            {
                Address = ContractPointerDataAddr,
                Data = pointerDataBytes,
                Name = "PointerData"
            };
        }
        else
        {
            pointerData = new MockMemorySpace.HeapFragment
            {
                Address = ContractPointerDataAddr,
                Data = Array.Empty<byte>(),
                Name = "PointerData"
            };
        }
        return (json, pointerData);
    }

    private ulong CreateDescriptorFragments()
    {
        if (_created)
            throw new InvalidOperationException("Context already created");

        (var json, var pointerData) = CreateDataDescriptor();
        int pointerDataCount = pointerData.Data is null ? 0 : pointerData.Data.Length / TargetTestHelpers.PointerSize;
        MockMemorySpace.HeapFragment descriptor = CreateContractDescriptor(json.Data.Length, pointerDataCount);

        AddHeapFragment(descriptor);
        AddHeapFragment(json);
        if (pointerData.Data.Length > 0)
            AddHeapFragment(pointerData);

        _created = true;
        return descriptor.Address;
    }

    public bool TryCreateTarget([NotNullWhen(true)] out ContractDescriptorTarget? target)
    {
        if (_created)
            throw new InvalidOperationException("Context already created");
        ulong contractDescriptorAddress = CreateDescriptorFragments();
        MockMemorySpace.ReadContext context = GetReadContext();
        ContractDescriptorTarget.GetTargetPlatformDelegate getTargetPlatform = (out int platform) =>
        {
            platform = TargetTestHelpers.Arch.Is64Bit ?
                (int)Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_AMD64 :
                (int)Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_X86;
            return 0;
        };
        return ContractDescriptorTarget.TryCreate(contractDescriptorAddress, context.ReadFromTarget, null, getTargetPlatform, out target);
    }
}
