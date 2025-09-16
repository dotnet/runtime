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
    private const uint ContractDescriptorAddr = 0xaaaaaaaa;
    private const uint JsonDescriptorAddr = 0xdddddddd;
    private const uint ContractPointerDataAddr = 0xeeeeeeee;

    bool _created = false;

    public ContractDescriptorBuilder(TargetTestHelpers targetTestHelpers)
        : base(targetTestHelpers)
    { }

    public class DescriptorBuilder(ContractDescriptorBuilder parent)
    {
        private bool _created = false;
        private readonly ContractDescriptorBuilder _parent = parent;

        private IReadOnlyCollection<string> _contracts;
        private IDictionary<DataType, Target.TypeInfo> _types;
        private IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? TypeName)> _globals;
        private IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? TypeName)> _subDescriptors;
        private IReadOnlyCollection<ulong> _indirectValues;

        public DescriptorBuilder SetContracts(IReadOnlyCollection<string> contracts)
        {
            _contracts = contracts;
            return this;
        }

        public DescriptorBuilder SetTypes(IDictionary<DataType, Target.TypeInfo> types)
        {
            _types = types;
            return this;
        }

        public DescriptorBuilder SetGlobals(IReadOnlyCollection<(string Name, ulong Value, string? TypeName)> globals)
        {
            if (_globals != null)
                throw new InvalidOperationException("Globals already set");
            _globals = globals.Select(g => (g.Name, (ulong?)g.Value, (uint?)null, (string?)null, g.TypeName)).ToArray();
            return this;
        }

        public DescriptorBuilder SetGlobals(IReadOnlyCollection<(string Name, ulong? Value, string? StringValue, string? TypeName)> globals)
        {
            if (_globals != null)
                throw new InvalidOperationException("Globals already set");
            _globals = globals.Select(g => (g.Name, (ulong?)g.Value, (uint?)null, g.StringValue, g.TypeName)).ToArray();
            return this;
        }

        public DescriptorBuilder SetGlobals(IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? TypeName)> globals)
        {
            if (_globals != null)
                throw new InvalidOperationException("Globals already set");
            _globals = globals;
            return this;
        }

        public DescriptorBuilder SetGlobals(IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? TypeName)> globals, IReadOnlyCollection<ulong> indirectValues)
        {
            SetGlobals(globals);
            SetIndirectValues(indirectValues);
            return this;
        }

        public DescriptorBuilder SetSubDescriptors(IReadOnlyCollection<(string Name, uint IndirectIndex)> subDescriptors)
        {
            if (_subDescriptors != null)
                throw new InvalidOperationException("Sub descriptors already set");
            _subDescriptors = subDescriptors.Select<(string Name, uint IndirectIndex), (string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? TypeName)>(s => (s.Name, null, s.IndirectIndex, null, null)).ToList();
            return this;
        }

        public DescriptorBuilder SetIndirectValues(IReadOnlyCollection<ulong> indirectValues)
        {
            if (_indirectValues != null)
                throw new InvalidOperationException("Indirect values already set");
            _indirectValues = indirectValues;
            return this;
        }


        public ulong CreateSubDescriptor(uint contractDescriptorAddress, uint jsonAddress, uint pointerDataAddress)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");

            (var json, var pointerData) = CreateDataDescriptor(jsonAddress, pointerDataAddress);
            int pointerDataCount = pointerData.Data is null ? 0 : pointerData.Data.Length / _parent.TargetTestHelpers.PointerSize;
            MockMemorySpace.HeapFragment descriptor = CreateContractDescriptor(
                contractDescriptorAddress,
                jsonAddress,
                pointerDataAddress,
                json.Data.Length,
                pointerDataCount);

            _parent.AddHeapFragment(descriptor);
            _parent.AddHeapFragment(json);
            if (pointerData.Data.Length > 0)
                _parent.AddHeapFragment(pointerData);

            _created = true;
            return descriptor.Address;
        }

        private MockMemorySpace.HeapFragment CreateContractDescriptor(uint contractDescriptorAddress, uint jsonAddress, uint pointerDataAddress, int jsonLength, int pointerDataCount)
        {
            byte[] descriptor = new byte[ContractDescriptorHelpers.Size(_parent.TargetTestHelpers.Arch.Is64Bit)];
            ContractDescriptorHelpers.Fill(descriptor, _parent.TargetTestHelpers.Arch, jsonLength, jsonAddress, pointerDataCount, pointerDataAddress);
            return new MockMemorySpace.HeapFragment
            {
                Address = contractDescriptorAddress,
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

        protected (MockMemorySpace.HeapFragment json, MockMemorySpace.HeapFragment pointerData) CreateDataDescriptor(ulong jsonAddress, ulong pointerDataAddress)
        {
            string metadataTypesJson = _types is not null ? ContractDescriptorHelpers.MakeTypesJson(_types) : string.Empty;
            string metadataGlobalsJson = _globals is not null ? ContractDescriptorHelpers.MakeGlobalsJson(_globals) : string.Empty;
            string metadataSubDescriptorJson = _subDescriptors is not null ? ContractDescriptorHelpers.MakeGlobalsJson(_subDescriptors) : string.Empty;
            string interpolatedContracts = _contracts is not null ? MakeContractsJson() : string.Empty;
            byte[] jsonBytes = Encoding.UTF8.GetBytes($$"""
            {
                "version": 0,
                "baseline": "empty",
                "contracts": { {{interpolatedContracts}} },
                "types": { {{metadataTypesJson}} },
                "globals": { {{metadataGlobalsJson}} },
                "subDescriptors": { {{metadataSubDescriptorJson}} },
            }
            """);
            MockMemorySpace.HeapFragment json = new()
            {
                Address = jsonAddress,
                Data = jsonBytes,
                Name = "JsonDescriptor"
            };

            MockMemorySpace.HeapFragment pointerData;
            if (_indirectValues != null)
            {
                int pointerSize = _parent.TargetTestHelpers.PointerSize;
                byte[] pointerDataBytes = new byte[_indirectValues.Count * pointerSize];
                int offset = 0;
                foreach (var value in _indirectValues)
                {
                    _parent.TargetTestHelpers.WritePointer(pointerDataBytes.AsSpan(offset, pointerSize), value);
                    offset += pointerSize;
                }
                pointerData = new MockMemorySpace.HeapFragment
                {
                    Address = pointerDataAddress,
                    Data = pointerDataBytes,
                    Name = "PointerData"
                };
            }
            else
            {
                pointerData = new MockMemorySpace.HeapFragment
                {
                    Address = pointerDataAddress,
                    Data = Array.Empty<byte>(),
                    Name = "PointerData"
                };
            }
            return (json, pointerData);
        }
    }

    public bool TryCreateTarget(DescriptorBuilder descriptor, [NotNullWhen(true)] out ContractDescriptorTarget? target)
    {
        if (_created)
            throw new InvalidOperationException("Context already created");
        _created = true;
        ulong contractDescriptorAddress = descriptor.CreateSubDescriptor(ContractDescriptorAddr, JsonDescriptorAddr, ContractPointerDataAddr);
        MockMemorySpace.MemoryContext memoryContext = GetMemoryContext();
        return ContractDescriptorTarget.TryCreate(contractDescriptorAddress, memoryContext.ReadFromTarget, memoryContext.WriteToTarget, null, out target);
    }
}
