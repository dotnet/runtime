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
internal unsafe static partial class MockMemorySpace
{
    internal class BumpAllocator
    {
        private readonly ulong _blockStart;
        private readonly ulong _blockEnd; // exclusive
        ulong _current;
        public BumpAllocator(ulong blockStart, ulong blockEnd)
        {
            _blockStart = blockStart;
            _blockEnd = blockEnd;
            _current = blockStart;
        }

        public ulong RangeStart => _blockStart;
        public ulong RangeEnd => _blockEnd;

        public bool TryAllocate(ulong size, string name, [NotNullWhen(true)] out HeapFragment? fragment)
        {
            // FIXME: alignment
            if (_current + size <= _blockEnd)
            {
                fragment = new HeapFragment {
                    Address = _current,
                    Data = new byte[size],
                    Name = name,
                };
                _current += size;
                return true;
            }
            fragment = null;
            return false;
        }

        public HeapFragment Allocate(ulong size, string name)
        {
            if (!TryAllocate(size, name, out HeapFragment? fragment))
            {
                throw new InvalidOperationException("Failed to allocate");
            }
            return fragment.Value;
        }

        public bool Overlaps(BumpAllocator other)
        {
            if ((other._blockStart <= _blockStart && other._blockEnd > _blockStart) ||
                (other._blockStart >= _blockStart && other._blockStart < _blockEnd))
            {
                return true;
            }
            return false;
        }
    }
}
