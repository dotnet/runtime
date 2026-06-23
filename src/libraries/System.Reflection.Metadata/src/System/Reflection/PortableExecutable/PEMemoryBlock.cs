// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Reflection.PortableExecutable
{
    public readonly struct PEMemoryBlock
    {
        private readonly AbstractMemoryBlock _block;
        private readonly int _offset;

        internal PEMemoryBlock(AbstractMemoryBlock block, int offset = 0)
        {
            Debug.Assert(block != null);
            Debug.Assert(offset >= 0 && offset <= block.Size);

            _block = block;
            _offset = offset;
        }

        /// <summary>
        /// Pointer to the first byte of the block.
        /// </summary>
        public unsafe byte* Pointer => (_block != null) ? _block.Pointer + _offset : null;

        /// <summary>
        /// Length of the block.
        /// </summary>
        public int Length => _block?.Size - _offset ?? 0;

        /// <summary>
        /// Gets the content of the block as a <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        /// <remarks>
        /// This provides safe access to the underlying memory without requiring pointer manipulation.
        /// The returned memory is valid as long as the owning <see cref="PEReader"/> or
        /// <see cref="MetadataReaderProvider"/> is not disposed.
        /// </remarks>
        public ReadOnlyMemory<byte> GetMemory()
        {
            if (_block is null)
            {
                return default;
            }

            ReadOnlyMemory<byte> memory = _block.GetMemory();
            if (memory.IsEmpty && Length == 0)
            {
                return default;
            }

            if (!memory.IsEmpty)
            {
                return memory.Slice(_offset, Length);
            }

            // Fallback: copy from pointer for blocks that don't support managed memory (e.g., memory-mapped)
            return _block.GetContentUnchecked(_offset, Length).AsMemory();
        }

        /// <summary>
        /// Creates <see cref="BlobReader"/> for a blob spanning the entire block.
        /// </summary>
        public unsafe BlobReader GetReader()
        {
            if (_block is null)
            {
                return default;
            }

            return new BlobReader(_block.GetMemoryBlock(_offset, Length));
        }

        /// <summary>
        /// Creates <see cref="BlobReader"/> for a blob spanning a part of the block.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Specified range is not contained within the block.</exception>
        public unsafe BlobReader GetReader(int start, int length)
        {
            BlobUtilities.ValidateRange(Length, start, length, nameof(length));

            if (_block is null)
            {
                return default;
            }

            return new BlobReader(_block.GetMemoryBlock(_offset + start, length));
        }

        /// <summary>
        /// Reads the content of the entire block into an array.
        /// </summary>
        public ImmutableArray<byte> GetContent()
        {
            return _block?.GetContentUnchecked(_offset, Length) ?? ImmutableArray<byte>.Empty;
        }

        /// <summary>
        /// Reads the content of a part of the block into an array.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Specified range is not contained within the block.</exception>
        public ImmutableArray<byte> GetContent(int start, int length)
        {
            BlobUtilities.ValidateRange(Length, start, length, nameof(length));
            return _block?.GetContentUnchecked(_offset + start, length) ?? ImmutableArray<byte>.Empty;
        }
    }
}
