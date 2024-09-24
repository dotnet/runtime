// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        public Builder()
        {

        }

        internal Builder SetDescriptor(scoped ReadOnlySpan<byte> descriptor)
        {
            const ulong ContractDescriptorAddr = 0xaaaaaaaa;

            if (_created)
                throw new InvalidOperationException("Context already created");
            _descriptor = new HeapFragment
            {
                Address = ContractDescriptorAddr,
                Data = descriptor.ToArray(),
                Name = "ContractDescriptor"
            };
            return this;
        }

        public Builder SetJson(scoped ReadOnlySpan<byte> json)
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
            _pointerData = new HeapFragment
            {
                Address = ContractPointerDataAddr,
                Data = pointerData.Length >= 0 ? pointerData.ToArray() : Array.Empty<byte>(),
                Name = "PointerData"
            };
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

        public Builder FillDescriptor(TargetTestHelpers targetTestHelpers)
        {
            Span<byte> descriptor = stackalloc byte[targetTestHelpers.ContractDescriptorSize];
            targetTestHelpers.ContractDescriptorFill(descriptor, _jsonLength, _pointerDataLength / targetTestHelpers.PointerSize);
            SetDescriptor(descriptor);
            return this;
        }

        private ReadContext CreateContext()
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
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
            return Target.TryCreate(context.ContractDescriptor.Address, context.ReadFromTarget, out target);
        }
    }

    public static Target CreateTarget(ReadOnlySpan<byte> descriptor, ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointerData = default)
    {
        Builder builder = new Builder()
        .SetJson(json)
        .SetDescriptor(descriptor)
        .SetPointerData(pointerData);
        return builder.Create();
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
        public int PointerDataLength => PointerData.Data.Length;
        public IReadOnlyList<HeapFragment> HeapFragments { get; init; }

        internal int ReadFromTarget(ulong address, Span<byte> span)
        {
            // Populate the span with the requested portion of the contract descriptor
            if (address >= ContractDescriptor.Address && address <= ContractDescriptor.Address + (ulong)ContractDescriptorLength - (uint)span.Length)
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
            if (address >= PointerData.Address && address <= PointerData.Address + (ulong)PointerDataLength - (uint)span.Length)
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
