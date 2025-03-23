// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{

    [Experimental(Experimentals.TensorTDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    internal readonly struct TensorShape
    {
        // Used to determine when we need to allocate a metadata array
        public const int MaxInlineArraySize = 5;

        // Used to determine when we can stack alloc for indexing vs when we need to allocate
        public const int MaxInlineRank = 8;

        internal readonly nint[]? _metadata;      // 8 bytes

        internal readonly nint _memoryLength;   // 8 bytes
        internal readonly int _rank;              // 4 bytes

        private readonly NintBuffer _lengths;
        private readonly NintBuffer _strides;

        internal TensorShape(nint memoryLength, ReadOnlySpan<nint> lengths, ReadOnlySpan<nint> strides)
        {
            _memoryLength = memoryLength;
            _rank = lengths.Length;
            if (lengths.Length > MaxInlineArraySize)
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

        [InlineArray(MaxInlineArraySize)] // 5x8 bytes (40)
        private struct NintBuffer
        {
            public nint e0;
        }

        [UnscopedRef]
        public ReadOnlySpan<nint> Lengths => (_metadata is null)
                                           ? ((ReadOnlySpan<nint>)_lengths).Slice(0, _rank)
                                           : MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(_metadata), _rank);

        [UnscopedRef]
        public ReadOnlySpan<nint> Strides => (_metadata is null)
                                           ? ((ReadOnlySpan<nint>)_strides).Slice(0, _rank)
                                           : MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(_metadata), _rank * 2).Slice(_rank);

        public nint FlattenedLength => TensorSpanHelpers.CalculateTotalLength(Lengths);

        public bool IsEmpty => FlattenedLength == 0;
    }
}
