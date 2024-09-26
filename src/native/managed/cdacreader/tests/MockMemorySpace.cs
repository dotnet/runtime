// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

/// <summary>
/// Helper for creating a mock memory space for testing.
/// </summary>
/// <remarks>
/// Use MockMemorySpace.CreateContext to create a mostly empty context for reading from the target.
/// Use MockMemorySpace.ContextBuilder to create a context with additional MockMemorySpace.HeapFragment data.
/// </remarks>
internal unsafe static class MockMemorySpace
{
    // These addresses are arbitrary and are used to store the contract descriptor components.
    // They should not overlap with any other heap fragment addresses.
    private const ulong ContractDescriptorAddr = 0xaaaaaaaa;
    // TODO: remove the references to these from TargetTestHelpers.ContractDescriptorFill
    internal const uint JsonDescriptorAddr = 0xdddddddd;
    internal const uint ContractPointerDataAddr = 0xeeeeeeee;

    internal struct HeapFragment
    {
        public ulong Address;
        public byte[] Data;
        public string? Name;
    }

    /// <summary>
    ///  Helper to populate a virtual memory space for reading from a target.
    /// </summary>
    internal class Builder
    {
        private bool _created = false;
        private List<HeapFragment> _heapFragments = new();

        private IReadOnlyCollection<string> _contracts;
        private IDictionary<DataType, Target.TypeInfo> _types;
        private IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? TypeName)> _globals;
        private IReadOnlyCollection<ulong> _indirectValues;

        private TargetTestHelpers _targetTestHelpers;

        public Builder(TargetTestHelpers targetTestHelpers)
        {
            _targetTestHelpers = targetTestHelpers;
        }

        // TODO: contracts with versions
        public Builder SetContracts(IReadOnlyCollection<string> contracts)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            _contracts = contracts;
            return this;
        }

        public Builder SetTypes(IDictionary<DataType, Target.TypeInfo> types)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            _types = types;
            return this;
        }

        public Builder SetGlobals(IReadOnlyCollection<(string Name, ulong Value, string? TypeName)> globals)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (_globals != null)
                throw new InvalidOperationException("Globals already set");
            _globals = globals.Select(g => (g.Name, (ulong?)g.Value, (uint?)null, g.TypeName)).ToArray();
            _indirectValues = null;
            return this;
        }

        public Builder SetGlobals(IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? TypeName)> globals, IReadOnlyCollection<ulong> indirectValues)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (_globals != null)
                throw new InvalidOperationException("Globals already set");
            _globals = globals;
            _indirectValues = indirectValues;
            return this;
        }

        public Builder AddHeapFragment(HeapFragment fragment)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (fragment.Data is null || fragment.Data.Length == 0)
                throw new InvalidOperationException($"Fragment '{fragment.Name}' data is empty");
            if (!FragmentFits(fragment))
                throw new InvalidOperationException($"Fragment '{fragment.Name}' does not fit in the address space");
            _heapFragments.Add(fragment);
            return this;
        }

        public Builder AddHeapFragments(IEnumerable<HeapFragment> fragments)
        {
            foreach (var f in fragments)
            {
                // add fragments one at a time to check for overlaps
                AddHeapFragment(f);
            }
            return this;
        }

        private HeapFragment CreateContractDescriptor(int jsonLength, int pointerDataCount)
        {
            byte[] descriptor = new byte[_targetTestHelpers.ContractDescriptorSize];
            _targetTestHelpers.ContractDescriptorFill(descriptor, jsonLength, pointerDataCount);
            return new HeapFragment
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
            StringBuilder sb = new ();
            foreach (var c in _contracts)
            {
                sb.Append($"\"{c}\": 1,");
            }
            Debug.Assert(sb.Length > 0);
            sb.Length--; // remove trailing comma
            return sb.ToString();
        }

        private (HeapFragment json, HeapFragment pointerData) CreateDataDescriptor()
        {
            string metadataTypesJson = TargetTestHelpers.MakeTypesJson(_types);
            string metadataGlobalsJson = TargetTestHelpers.MakeGlobalsJson(_globals);
            string interpolatedContracts = MakeContractsJson();
            byte[] jsonBytes = Encoding.UTF8.GetBytes($$"""
            {
                "version": 0,
                "baseline": "empty",
                "contracts": { {{ interpolatedContracts }} },
                "types": { {{metadataTypesJson}} },
                "globals": { {{metadataGlobalsJson}} }
            }
            """);
            HeapFragment json = new () {
                Address = JsonDescriptorAddr,
                Data = jsonBytes,
                Name = "JsonDescriptor"
            };

            HeapFragment pointerData;
            if (_indirectValues != null)
            {
                int pointerSize = _targetTestHelpers.PointerSize;
                byte[] pointerDataBytes = new byte[_indirectValues.Count * pointerSize];
                int offset = 0;
                foreach (var value in _indirectValues)
                {
                    _targetTestHelpers.WritePointer(pointerDataBytes.AsSpan(offset, pointerSize), value);
                    offset += pointerSize;
                }
                pointerData =new HeapFragment {
                    Address = ContractPointerDataAddr,
                    Data = pointerDataBytes,
                    Name = "PointerData"
                };
            } else {
                pointerData = new HeapFragment
                {
                    Address = ContractPointerDataAddr,
                    Data = Array.Empty<byte>(),
                    Name = "PointerData"
                };
            }
            return (json, pointerData);
        }

        private ReadContext CreateContext()
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            (var json, var pointerData) = CreateDataDescriptor();

            int pointerDataCount = pointerData.Data is null ? 0 : pointerData.Data.Length / _targetTestHelpers.PointerSize;

            HeapFragment descriptor = CreateContractDescriptor(json.Data.Length, pointerDataCount);

            ReadContext context = new ReadContext
            {
                ContractDescriptor = descriptor,
                JsonDescriptor = json,
                PointerData = pointerData,
                HeapFragments = _heapFragments,
            };
            _created = true;
            return context;
        }

        private bool FragmentFits(HeapFragment f)
        {
            foreach (var fragment in _heapFragments)
            {
                // f and fragment overlap if either:
                // 1. f starts before fragment starts and ends after fragment starts
                // 2. f starts before fragment ends
                if ((f.Address <= fragment.Address && f.Address + (ulong)f.Data.Length > fragment.Address) ||
                    (f.Address >= fragment.Address && f.Address < fragment.Address + (ulong)fragment.Data.Length))
                {
                    return false;
                }

            }
            return true;
        }

        public bool TryCreateTarget([NotNullWhen(true)] out Target? target)
        {
            ReadContext context = CreateContext();
            return Target.TryCreate(context.ContractDescriptor.Address, context.ReadFromTarget, out target);
        }
    }

    // Used by ReadFromTarget to return the appropriate bytes
    internal class ReadContext
    {
        public HeapFragment ContractDescriptor { get; init;}

        public HeapFragment JsonDescriptor { get; init; }

        public HeapFragment PointerData { get; init;}
        public IReadOnlyList<HeapFragment> HeapFragments { get; init; }

        internal int ReadFromTarget(ulong address, Span<byte> span)
        {
            if (address == 0)
                return -1;
            // Populate the span with the requested portion of the contract descriptor
            if (address >= ContractDescriptor.Address && address + (uint)span.Length <= ContractDescriptor.Address + (ulong)ContractDescriptor.Data.Length)
            {
                int offset = checked ((int)(address - ContractDescriptor.Address));
                ContractDescriptor.Data.AsSpan(offset, span.Length).CopyTo(span);
                return 0;
            }

            // Populate the span with the JSON descriptor - this assumes the product will read it all at once.
            if (address == JsonDescriptor.Address)
            {
                JsonDescriptor.Data.AsSpan().CopyTo(span);
                return 0;
            }

            // Populate the span with the requested portion of the pointer data
            if (address >= PointerData.Address && address + (uint)span.Length <= PointerData.Address + (ulong)PointerData.Data.Length)
            {
                int offset = checked((int)(address - PointerData.Address));
                PointerData.Data.AsSpan(offset, span.Length).CopyTo(span);
                return 0;
            }

            return ReadFragment(address, span);
        }

        private int ReadFragment(ulong address, Span<byte> buffer)
        {
            bool partialReadOcurred = false;
            HeapFragment lastHeapFragment = default;
            int availableLength = 0;
            while (true)
            {
                bool tryAgain = false;
                foreach (var fragment in HeapFragments)
                {
                    if (address >= fragment.Address && address < fragment.Address + (ulong)fragment.Data.Length)
                    {
                        int offset = (int)(address - fragment.Address);
                        availableLength = fragment.Data.Length - offset;
                        if (availableLength >= buffer.Length)
                        {
                            fragment.Data.AsSpan(offset, buffer.Length).CopyTo(buffer);
                            return 0;
                        }
                        else
                        {
                            lastHeapFragment = fragment;
                            partialReadOcurred = true;
                            tryAgain = true;
                            fragment.Data.AsSpan(offset, availableLength).CopyTo(buffer);
                            buffer = buffer.Slice(availableLength);
                            address = fragment.Address + (ulong)fragment.Data.Length;
                            break;
                        }
                    }
                }
                if (!tryAgain)
                    break;
            }

            if (partialReadOcurred)
                throw new InvalidOperationException($"Not enough data in fragment at {lastHeapFragment.Address:X} ('{lastHeapFragment.Name}') to read {buffer.Length} bytes at {address:X} (only {availableLength} bytes available)");
            return -1;
        }
    }
}
