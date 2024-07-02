// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    internal const ulong ContractDescriptorAddr = 0xaaaaaaaa;
    internal const uint JsonDescriptorAddr = 0xdddddddd;
    internal const uint ContractPointerDataAddr = 0xeeeeeeee;


    internal struct HeapFragment
    {
        public ulong Address;
        public byte[] Data;
        public string? Name;
    }

    /// <summary>
    ///  Helper to build a context (virtual memory space) for reading from a target.
    /// </summary>
    /// <remarks>
    /// All the spans should be stackalloc or pinned while the context is being used.
    /// </remarks>
    internal unsafe ref struct Builder
    {
        private bool _created = false;
        private byte* _descriptor = null;
        private int _descriptorLength = 0;
        private byte* _json = null;
        private int _jsonLength = 0;
        private byte* _pointerData = null;
        private int _pointerDataLength = 0;
        private List<HeapFragment> _heapFragments = new();

        public Builder()
        {

        }

        public Builder SetDescriptor(scoped ReadOnlySpan<byte> descriptor)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            _descriptor = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(descriptor));
            _descriptorLength = descriptor.Length;
            return this;
        }

        public Builder SetJson(scoped ReadOnlySpan<byte> json)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            _json = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(json));
            _jsonLength = json.Length;
            return this;
        }

        public Builder SetPointerData(scoped ReadOnlySpan<byte> pointerData)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (pointerData.Length >= 0)
            {
                _pointerData = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(pointerData));
                _pointerDataLength = pointerData.Length;
            }
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

        public ReadContext Create()
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            GCHandle fragmentReaderHandle = default; ;
            if (_heapFragments.Count > 0)
            {
                fragmentReaderHandle = GCHandle.Alloc(new HeapFragmentReader(_heapFragments));
            }
            ReadContext context = new ReadContext
            {
                ContractDescriptor = _descriptor,
                ContractDescriptorLength = _descriptorLength,
                JsonDescriptor = _json,
                JsonDescriptorLength = _jsonLength,
                PointerData = _pointerData,
                PointerDataLength = _pointerDataLength,
                HeapFragmentReader = GCHandle.ToIntPtr(fragmentReaderHandle)
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
    }

    // Note: all the spans should be stackalloc or pinned.
    public static ReadContext CreateContext(ReadOnlySpan<byte> descriptor, ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointerData = default)
    {
        Builder builder = new Builder()
        .SetJson(json)
        .SetDescriptor(descriptor)
        .SetPointerData(pointerData);
        return builder.Create();
    }

    public static bool TryCreateTarget(ReadContext* context, out Target? target)
    {
        return Target.TryCreate(ContractDescriptorAddr, &ReadFromTarget, context, out target);
    }

    [UnmanagedCallersOnly]
    private static int ReadFromTarget(ulong address, byte* buffer, uint length, void* context)
    {
        ReadContext* readContext = (ReadContext*)context;
        var span = new Span<byte>(buffer, (int)length);

        // Populate the span with the requested portion of the contract descriptor
        if (address >= ContractDescriptorAddr && address <= ContractDescriptorAddr + (ulong)readContext->ContractDescriptorLength - length)
        {
            ulong offset = address - ContractDescriptorAddr;
            new ReadOnlySpan<byte>(readContext->ContractDescriptor + offset, (int)length).CopyTo(span);
            return 0;
        }

        // Populate the span with the JSON descriptor - this assumes the product will read it all at once.
        if (address == JsonDescriptorAddr)
        {
            new ReadOnlySpan<byte>(readContext->JsonDescriptor, readContext->JsonDescriptorLength).CopyTo(span);
            return 0;
        }

        // Populate the span with the requested portion of the pointer data
        if (address >= ContractPointerDataAddr && address <= ContractPointerDataAddr + (ulong)readContext->PointerDataLength - length)
        {
            ulong offset = address - ContractPointerDataAddr;
            new ReadOnlySpan<byte>(readContext->PointerData + offset, (int)length).CopyTo(span);
            return 0;
        }

        HeapFragmentReader? heapFragmentReader = GCHandle.FromIntPtr(readContext->HeapFragmentReader).Target as HeapFragmentReader;
        if (heapFragmentReader is not null)
        {
            return heapFragmentReader.ReadFragment(address, span);
        }

        return -1;
    }

    // Used by ReadFromTarget to return the appropriate bytes
    internal ref struct ReadContext : IDisposable
    {
        public byte* ContractDescriptor;
        public int ContractDescriptorLength;

        public byte* JsonDescriptor;
        public int JsonDescriptorLength;

        public byte* PointerData;
        public int PointerDataLength;

        public IntPtr HeapFragmentReader;

        public void Dispose()
        {
            if (HeapFragmentReader != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(HeapFragmentReader).Free();
                HeapFragmentReader = IntPtr.Zero;
            }
        }
    }

    private class HeapFragmentReader
    {
        private readonly IReadOnlyList<HeapFragment> _fragments;
        public HeapFragmentReader(IReadOnlyList<HeapFragment> fragments)
        {
            _fragments = fragments;
        }

        public int ReadFragment(ulong address, Span<byte> buffer)
        {
            foreach (var fragment in _fragments)
            {
                if (address >= fragment.Address && address < fragment.Address + (ulong)fragment.Data.Length)
                {
                    int offset = (int)(address - fragment.Address);
                    int availableLength = fragment.Data.Length - offset;
                    if (availableLength >= buffer.Length)
                    {
                        fragment.Data.AsSpan(offset, buffer.Length).CopyTo(buffer);
                        return 0;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Not enough data in fragment at {fragment.Address:X} ('{fragment.Name}') to read {buffer.Length} bytes at {address:X} (only {availableLength} bytes available)");
                    }
                }
            }
            return -1;
        }
    }
}
