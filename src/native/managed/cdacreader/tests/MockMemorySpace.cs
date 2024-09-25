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
/// <remarks>
/// All the spans should be stackalloc or pinned while the context is being used.
/// </remarks>
internal unsafe static class MockMemorySpace
{
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
        private HeapFragment _descriptor;
        private int _descriptorLength => _descriptor.Data.Length;
        private HeapFragment _json;
        private int _jsonLength => _json.Data.Length;
        private HeapFragment _pointerData;
        private int _pointerDataLength => _pointerData.Data.Length;
        private List<HeapFragment> _heapFragments = new();

        private bool _setDescriptor = false;

        private Mode _mode = Mode.Indeterminate;

        private IReadOnlyCollection<string> _contracts;
        private IDictionary<DataType, Target.TypeInfo> _types;
        private IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? TypeName)> _globals;
        private IReadOnlyCollection<ulong> _indirectValues;

        // builder is either taking low level inputs (json, pointer data) or high level inputs (contracts, types, globals)
        private enum Mode
        {
            Indeterminate,
            ExplicitJson,
            HighLevel,
        }

        private TargetTestHelpers _targetTestHelpers;

        public Builder(TargetTestHelpers targetTestHelpers)
        {
            _targetTestHelpers = targetTestHelpers;
        }

        internal Builder SetDescriptor(scoped ReadOnlySpan<byte> descriptor)
        {
            const ulong ContractDescriptorAddr = 0xaaaaaaaa;

            if (_created)
                throw new InvalidOperationException("Context already created");
            if (_setDescriptor)
                throw new InvalidOperationException("Descriptor already set");
            _descriptor = new HeapFragment
            {
                Address = ContractDescriptorAddr,
                Data = descriptor.ToArray(),
                Name = "ContractDescriptor"
            };
            _setDescriptor = true;
            return this;
        }

        public Builder SetJson(scoped ReadOnlySpan<byte> json)
        {
            if (_mode == Mode.HighLevel)
                throw new InvalidOperationException("HighLevel mode does not support setting JSON");
            _mode = Mode.ExplicitJson;
            return SetJsonInternal(json);
        }

        private Builder SetJsonInternal(scoped ReadOnlySpan<byte> json)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            _json = new HeapFragment
            {
                Address = JsonDescriptorAddr,
                Data = json.ToArray(),
                Name = "JsonDescriptor"
            };
            return this;
        }

        public Builder SetPointerData(scoped ReadOnlySpan<byte> pointerData)
        {

            if (_created)
                throw new InvalidOperationException("Context already created");
            if (_mode == Mode.HighLevel)
                throw new InvalidOperationException("HighLevel mode does not support setting pointer data");
            _mode = Mode.ExplicitJson;
            return SetPointerDataInternal(pointerData);
        }

        private Builder SetPointerDataInternal(scoped ReadOnlySpan<byte> pointerData)
        {
            _pointerData = new HeapFragment
            {
                Address = ContractPointerDataAddr,
                Data = pointerData.Length >= 0 ? pointerData.ToArray() : Array.Empty<byte>(),
                Name = "PointerData"
            };
            return this;
        }

        // TODO: contracts with versions
        public Builder SetContracts(IReadOnlyCollection<string> contracts)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (_mode == Mode.ExplicitJson)
                throw new InvalidOperationException("Explicit JSON mode does not support setting contracts");
            _mode = Mode.HighLevel;
            _contracts = contracts;
            return this;
        }

        public Builder SetTypes(IDictionary<DataType, Target.TypeInfo> types)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (_mode == Mode.ExplicitJson)
                throw new InvalidOperationException("Explicit JSON mode does not support setting types");
            _mode = Mode.HighLevel;
            _types = types;
            return this;
        }

        public Builder SetGlobals(IReadOnlyCollection<(string Name, ulong Value, string? TypeName)> globals)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (_mode == Mode.ExplicitJson)
                throw new InvalidOperationException("Explicit JSON mode does not support setting globals");
            if (_globals != null)
                throw new InvalidOperationException("Globals already set");
            _mode = Mode.HighLevel;
            _globals = globals.Select(g => (g.Name, (ulong?)g.Value, (uint?)null, g.TypeName)).ToArray();
            _indirectValues = null;
            return this;
        }

        public Builder SetGlobals(IReadOnlyCollection<(string Name, ulong? Value, uint? IndirectIndex, string? TypeName)> globals, IReadOnlyCollection<ulong> indirectValues)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (_mode == Mode.ExplicitJson)
                throw new InvalidOperationException("Explicit JSON mode does not support setting globals");
            if (_globals != null)
                throw new InvalidOperationException("Globals already set");
            _mode = Mode.HighLevel;
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

        private Builder FillDescriptor()
        {
            Span<byte> descriptor = stackalloc byte[_targetTestHelpers.ContractDescriptorSize];
            int pointerDataCount = _pointerData.Data is null ? 0 : _pointerDataLength / _targetTestHelpers.PointerSize;
            _targetTestHelpers.ContractDescriptorFill(descriptor, _jsonLength, pointerDataCount);
            SetDescriptor(descriptor);
            return this;
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

        private void FillHighLevel()
        {
            string metadataTypesJson = TargetTestHelpers.MakeTypesJson(_types);
            string metadataGlobalsJson = TargetTestHelpers.MakeGlobalsJson(_globals);
            string interpolatedContracts = MakeContractsJson();
            byte[] json = Encoding.UTF8.GetBytes($$"""
            {
                "version": 0,
                "baseline": "empty",
                "contracts": { {{ interpolatedContracts }} },
                "types": { {{metadataTypesJson}} },
                "globals": { {{metadataGlobalsJson}} }
            }
            """);
            SetJsonInternal(json);

            if (_indirectValues != null)
            {
                int pointerSize = _targetTestHelpers.PointerSize;
                Span<byte> pointerData = stackalloc byte[_indirectValues.Count * pointerSize];
                int offset = 0;
                foreach (var value in _indirectValues)
                {
                    _targetTestHelpers.WritePointer(pointerData.Slice(offset), value);
                    offset += pointerSize;
                }
                SetPointerDataInternal(pointerData);
            }
        }

        private ReadContext CreateContext()
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (_mode == Mode.Indeterminate)
                throw new InvalidOperationException("No input data provided");
            if (_mode == Mode.HighLevel)
            {
                FillHighLevel();
            }

            if (!_setDescriptor)
                FillDescriptor();
            ReadContext context = new ReadContext
            {
                ContractDescriptor = _descriptor,
                JsonDescriptor = _json,
                PointerData = _pointerData,
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
            return ContractDescriptorTarget.TryCreate(context.ContractDescriptor.Address, context.ReadFromTarget, out target);
        }
    }

    public static Target CreateTarget(TargetTestHelpers targetTestHelpers, ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointerData = default)
    {
        Builder builder = new Builder(targetTestHelpers)
        .SetJson(json)
        .SetPointerData(pointerData);
        bool success = builder.TryCreateTarget(out var target);
        Assert.True(success);
        return target;
    }

    public static bool TryCreateTarget(ReadContext context, out Target? target)
    {
        return Target.TryCreate(context.ContractDescriptor.Address, context.ReadFromTarget, out target);
    }

    // Used by ReadFromTarget to return the appropriate bytes
    internal class ReadContext
    {
        public HeapFragment ContractDescriptor { get; init;}
        public int ContractDescriptorLength => ContractDescriptor.Data.Length;

        public HeapFragment JsonDescriptor { get; init; }
        public int JsonDescriptorLength => JsonDescriptor.Data.Length;

        public HeapFragment PointerData { get; init;}
        public int PointerDataLength => PointerData.Data?.Length ?? 0;
        public IReadOnlyList<HeapFragment> HeapFragments { get; init; }

        internal int ReadFromTarget(ulong address, Span<byte> span)
        {
            if (address == 0)
                return -1;
            // Populate the span with the requested portion of the contract descriptor
            if (address >= ContractDescriptor.Address && address + (uint)span.Length <= ContractDescriptor.Address + (ulong)ContractDescriptorLength)
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
            if (address >= PointerData.Address && address + (uint)span.Length <= PointerData.Address + (ulong)PointerDataLength)
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
