// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace System.Numerics.Tensors
{
    internal struct TensorShape
    {
        internal nint[]? _metadata;      // 8 bytes

#pragma warning disable CA1823 // Avoid unused private fields
        internal nint _memoryLength;   // 8 bytes
#pragma warning restore CA1823 // Avoid unused private fields
        internal int _rank;              // 4 bytes

        private LengthsBuffer _lengths;
        private StridesBuffer _strides;

        internal TensorShape(nint memoryLength, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
        {
            _memoryLength = memoryLength;
            _rank = Lengths.Length;
            lengths.CopyTo(_lengths);
            strides.CopyTo(_strides);
        }

        [InlineArray(5)] // 5x8 bytes (40)
        private struct LengthsBuffer
        {
            public nint e0;
        }

        [InlineArray(5)] // 5x8 bytes (40)
        private struct StridesBuffer
        {
            public nint e0;
        }

        [UnscopedRef]
        public ReadOnlySpan<nint> Lengths => (_metadata is null) ? ((ReadOnlySpan<nint>)_lengths).Slice(0, _rank) : MemoryMarshal.CreateReadOnlySpan(in _metadata[0], _rank);

        [UnscopedRef]
        public ReadOnlySpan<nint> Strides => (_metadata is null) ? ((ReadOnlySpan<nint>)_strides).Slice(0, _rank) : MemoryMarshal.CreateReadOnlySpan(in _metadata[_metadata.Length / 2], _rank);

        public nint FlattenedLength => TensorSpanHelpers.CalculateTotalLength(Lengths);
    }
}
