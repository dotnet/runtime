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
    public readonly struct TensorShape
    {
        internal readonly nint[]? _metadata;      // 8 bytes

        internal readonly nint _memoryLength;   // 8 bytes
        internal readonly int _rank;              // 4 bytes

        private readonly LengthsBuffer _lengths;
        private readonly StridesBuffer _strides;

        internal TensorShape(nint memoryLength, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
        {
            _memoryLength = memoryLength;
            _rank = Lengths.Length;
            if (lengths.Length > 5)
            {
                _metadata = new nint[lengths.Length + strides.Length];
                lengths.CopyTo(MemoryMarshal.CreateSpan(ref _metadata[0], lengths.Length));
                strides.CopyTo(MemoryMarshal.CreateSpan(ref _metadata[lengths.Length], strides.Length));
            }
            else
            {
                lengths.CopyTo(_lengths);
                strides.CopyTo(_strides);
            }
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
#pragma warning disable CS9192 // Argument should be passed with 'ref' or 'in' keyword
        public ReadOnlySpan<nint> Lengths => (_metadata is null)
                                           ? ((ReadOnlySpan<nint>)_lengths).Slice(0, _rank)
                                           : MemoryMarshal.CreateReadOnlySpan(MemoryMarshal.GetArrayDataReference(_metadata), _rank);

        [UnscopedRef]
        public ReadOnlySpan<nint> Strides => (_metadata is null)
                                           ? ((ReadOnlySpan<nint>)_strides).Slice(0, _rank)
                                           : MemoryMarshal.CreateReadOnlySpan(MemoryMarshal.GetArrayDataReference(_metadata), _rank).Slice(_rank / 2);
#pragma warning restore CS9192 // Argument should be passed with 'ref' or 'in' keyword

        public nint FlattenedLength => TensorSpanHelpers.CalculateTotalLength(Lengths);
    }
}
