// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// Helper for creating a mock memory space for testing.
/// </summary>
/// <remarks>
/// Use MockMemorySpace.Builder to create a context with MockMemorySpace.HeapFragment data.
/// </remarks>
internal unsafe static partial class MockMemorySpace
{
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
        private readonly List<HeapFragment> _heapFragments = new();
        private readonly List<BumpAllocator> _allocators = new();

        private TargetTestHelpers _targetTestHelpers;

        public Builder(TargetTestHelpers targetTestHelpers)
        {
            _targetTestHelpers = targetTestHelpers;
        }

        internal TargetTestHelpers TargetTestHelpers => _targetTestHelpers;

        internal Span<byte> BorrowAddressRange(ulong address, int length)
        {
            foreach (var fragment in _heapFragments)
            {
                if (address >= fragment.Address && address+(ulong)length <= fragment.Address + (ulong)fragment.Data.Length)
                    return fragment.Data.AsSpan((int)(address - fragment.Address), length);
            }
            throw new InvalidOperationException($"No fragment includes addresses from 0x{address:x} with length {length}");
        }

        public Builder AddHeapFragment(HeapFragment fragment)
        {
            if (fragment.Data is null || fragment.Data.Length == 0)
                throw new InvalidOperationException($"Fragment '{fragment.Name}' data is empty");
            if (!FragmentFits(fragment))
                throw new InvalidOperationException($"Fragment '{fragment.Name}' does not fit in the address space. Overlaps with existing fragments.\n{GetHeapFragmentsDescription()}");
            _heapFragments.Add(fragment);
            return this;
        }

        private string GetHeapFragmentsDescription()
        {
            StringBuilder builder = new();
            foreach (var fragment in _heapFragments)
            {
                builder.AppendLine($"Fragment '{fragment.Name}' at 0x{fragment.Address:x} with length {fragment.Data.Length}");
            }
            return builder.ToString();
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

        internal ReadContext GetReadContext()
        {
            ReadContext context = new ReadContext
            {
                HeapFragments = _heapFragments,
            };
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

        // Get an allocator for a range of addresses to simplify creating heap fragments
        public BumpAllocator CreateAllocator(ulong start, ulong end, int minAlign = 16)
        {
            BumpAllocator allocator = new BumpAllocator(start, end) { MinAlign = minAlign };
            foreach (var a in _allocators)
            {
                if (allocator.Overlaps(a))
                    throw new InvalidOperationException($"Requested range (0x{start:x}, 0x{end:x}) overlaps with existing allocator (0x{a.RangeStart:x}, 0x{a.RangeEnd:x})");
            }
            _allocators.Add(allocator);
            return allocator;
        }
    }

    // Used by ReadFromTarget to return the appropriate bytes
    internal class ReadContext
    {
        public IReadOnlyList<HeapFragment> HeapFragments { get; init; }

        internal int ReadFromTarget(ulong address, Span<byte> buffer)
        {
            if (buffer.Length == 0)
                return 0;

            if (address == 0)
                return -1;

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
