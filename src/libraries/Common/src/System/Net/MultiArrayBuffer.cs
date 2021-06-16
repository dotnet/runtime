// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Net
{
    // Warning: Mutable struct!
    // The purpose of this struct is to simplify buffer management in cases where the size of the buffer may grow large (e.g. >64K),
    // thus making it worthwhile to add the overhead involved in managing multiple individual array allocations.
    // Like ArrayBuffer, this manages a sliding buffer where bytes can be added at the end and removed at the beginning.
    // Unlike ArrayBuffer, the buffer itself is managed using 16K blocks which are added/removed to the block list as necessary.

    // 'ActiveBuffer' contains the current buffer contents; these bytes will be preserved on any call to TryEnsureAvailableBytesUpToLimit.
    // 'AvailableBuffer' contains the available bytes past the end of the current content,
    // and can be written to in order to add data to the end of the buffer.
    // Commit(byteCount) will extend the ActiveBuffer by 'byteCount' bytes into the AvailableBuffer.
    // Discard(byteCount) will discard 'byteCount' bytes as the beginning of the ActiveBuffer.
    // TryEnsureAvailableBytesUpToLimit will grow the buffer if necessary; *however*, this may invalidate
    // old values of 'ActiveBuffer' and 'AvailableBuffer', so they must be retrieved again.

    internal struct MultiArrayBuffer : IDisposable
    {
        private byte[]?[]? _blocks;
        private uint _allocatedEnd;
        private uint _activeStart;
        private uint _availableStart;

        // Invariants:
        // 0 <= _activeStart <= _availableStart <= total buffer size (i.e. _blockCount * BlockSize)

        private const int BlockSize = 16 * 1024;

        public MultiArrayBuffer(int initialBufferSize) : this()
        {
            // 'initialBufferSize' is ignored for now. Some callers are passing useful info here that we might want to act on in the future.
            Debug.Assert(initialBufferSize >= 0);
        }

        public void Dispose()
        {
            _activeStart = 0;
            _availableStart = 0;

            if (_blocks is not null)
            {
                for (int i = 0; i < _blocks.Length; i++)
                {
                    if (_blocks[i] is byte[] toReturn)
                    {
                        _blocks[i] = null;
                        ArrayPool<byte>.Shared.Return(toReturn);
                    }
                }

                _blocks = null;
                _allocatedEnd = 0;
            }
        }

        public bool IsEmpty => _activeStart == _availableStart;

        public MultiMemory ActiveMemory => new MultiMemory(_blocks, _activeStart, _availableStart - _activeStart);

        public MultiMemory AvailableMemory => new MultiMemory(_blocks, _availableStart, _allocatedEnd - _availableStart);

        public void Discard(int byteCount)
        {
            Debug.Assert(byteCount >= 0);
            Debug.Assert(byteCount <= ActiveMemory.Length, $"MultiArrayBuffer.Discard: Expected byteCount={byteCount} <= {ActiveMemory.Length}");

            if (byteCount == ActiveMemory.Length)
            {
                DiscardAll();
                return;
            }

            CheckState();

            uint ubyteCount = (uint)byteCount;

            uint oldStartBlock = _activeStart / BlockSize;
            _activeStart += ubyteCount;
            uint newStartBlock = _activeStart / BlockSize;

            FreeBlocks(oldStartBlock, newStartBlock);

            CheckState();
        }

        public void DiscardAll()
        {
            CheckState();

            uint firstAllocatedBlock = _activeStart / BlockSize;
            uint firstUnallocatedBlock = _allocatedEnd / BlockSize;
            FreeBlocks(firstAllocatedBlock, firstUnallocatedBlock);

            _activeStart = _availableStart = _allocatedEnd = 0;

            CheckState();

        }

        private void FreeBlocks(uint startBlock, uint endBlock)
        {
            byte[]?[] blocks = _blocks!;
            for (uint i = startBlock; i < endBlock; i++)
            {
                byte[]? toReturn = blocks[i];
                Debug.Assert(toReturn is not null);
                blocks[i] = null;
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }

        public void Commit(int byteCount)
        {
            Debug.Assert(byteCount >= 0);
            Debug.Assert(byteCount <= AvailableMemory.Length, $"MultiArrayBuffer.Commit: Expected byteCount={byteCount} <= {AvailableMemory.Length}");

            uint ubyteCount = (uint)byteCount;

            _availableStart += ubyteCount;
        }

        public void EnsureAvailableSpaceUpToLimit(int byteCount, int limit)
        {
            Debug.Assert(byteCount >= 0);
            Debug.Assert(limit >= 0);

            if (ActiveMemory.Length >= limit)
            {
                // Already past limit. Do nothing.
                return;
            }

            // Enforce the limit.
            byteCount = Math.Min(byteCount, limit - ActiveMemory.Length);

            EnsureAvailableSpace(byteCount);
        }

        public void EnsureAvailableSpace(int byteCount)
        {
            Debug.Assert(byteCount >= 0);

            if (byteCount > AvailableMemory.Length)
            {
                GrowAvailableSpace(byteCount);
            }
        }

        public void GrowAvailableSpace(int byteCount)
        {
            Debug.Assert(byteCount > AvailableMemory.Length);

            CheckState();

            uint ubyteCount = (uint)byteCount;

            uint newBytesNeeded = ubyteCount - (uint)AvailableMemory.Length;
            uint newBlocksNeeded = (newBytesNeeded + BlockSize - 1) / BlockSize;

            // Ensure we have enough space in the block array for the new blocks needed.
            if (_blocks is null)
            {
                Debug.Assert(_allocatedEnd == 0);
                Debug.Assert(_activeStart == 0);
                Debug.Assert(_availableStart == 0);

                int blockArraySize = 4;
                while (blockArraySize < newBlocksNeeded)
                {
                    blockArraySize *= 2;
                }

                _blocks = new byte[]?[blockArraySize];
            }
            else
            {
                Debug.Assert(_allocatedEnd % BlockSize == 0);
                Debug.Assert(_allocatedEnd <= _blocks.Length * BlockSize);

                uint allocatedBlocks = _allocatedEnd / BlockSize;
                uint blockArraySize = (uint)_blocks.Length;
                if (allocatedBlocks + newBlocksNeeded > blockArraySize)
                {
                    // Not enough room in current block array.
                    uint unusedInitialBlocks = _activeStart / BlockSize;
                    uint usedBlocks = (allocatedBlocks - unusedInitialBlocks);
                    uint blocksNeeded = usedBlocks + newBlocksNeeded;
                    if (blocksNeeded > blockArraySize)
                    {
                        // Need to allocate a new array and copy.
                        while (blockArraySize < blocksNeeded)
                        {
                            blockArraySize *= 2;
                        }

                        byte[]?[] newBlockArray = new byte[]?[blockArraySize];
                        _blocks.AsSpan().Slice((int)unusedInitialBlocks, (int)usedBlocks).CopyTo(newBlockArray);
                        _blocks = newBlockArray;
                    }
                    else
                    {
                        // We can shift the array down to make enough space
                        _blocks.AsSpan().Slice((int)unusedInitialBlocks, (int)usedBlocks).CopyTo(_blocks);

                        // Null out the part of the array left over from the shift, so that we aren't holding references to those blocks.
                        _blocks.AsSpan().Slice((int)usedBlocks, (int)unusedInitialBlocks).Clear();
                    }

                    uint shift = unusedInitialBlocks * BlockSize;
                    _allocatedEnd -= shift;
                    _activeStart -= shift;
                    _availableStart -= shift;

                    Debug.Assert(_activeStart / BlockSize == 0, $"Start is not in first block after move or resize?? _activeStart={_activeStart}");
                }
            }

            // Allocate new blocks
            Debug.Assert(_allocatedEnd % BlockSize == 0);
            uint allocatedBlockCount = _allocatedEnd / BlockSize;
            Debug.Assert(allocatedBlockCount == 0 || _blocks[allocatedBlockCount - 1] is not null);
            for (uint i = 0; i < newBlocksNeeded; i++)
            {
                Debug.Assert(_blocks[allocatedBlockCount] is null);
                _blocks[allocatedBlockCount++] = ArrayPool<byte>.Shared.Rent(BlockSize);
            }

            _allocatedEnd = allocatedBlockCount * BlockSize;

            // After all of that, we should have enough available memory now
            Debug.Assert(byteCount <= AvailableMemory.Length);

            CheckState();
        }

        [Conditional("DEBUG")]
        private void CheckState()
        {
            if (_blocks == null)
            {
                Debug.Assert(_activeStart == 0);
                Debug.Assert(_availableStart == 0);
                Debug.Assert(_allocatedEnd == 0);
            }
            else
            {
                Debug.Assert(_activeStart <= _availableStart);
                Debug.Assert(_availableStart <= _allocatedEnd);
                Debug.Assert(_allocatedEnd <= _blocks.Length * BlockSize);

                Debug.Assert(_allocatedEnd % BlockSize == 0, $"_allocatedEnd={_allocatedEnd} not at block boundary?");

                uint firstAllocatedBlock = _activeStart / BlockSize;
                uint firstUnallocatedBlock = _allocatedEnd / BlockSize;

                for (uint i = 0; i < firstAllocatedBlock; i++)
                {
                    Debug.Assert(_blocks[i] is null);
                }

                for (uint i = firstAllocatedBlock; i < firstUnallocatedBlock; i++)
                {
                    Debug.Assert(_blocks[i] is not null);
                }

                for (uint i = firstUnallocatedBlock; i < _blocks.Length; i++)
                {
                    Debug.Assert(_blocks[i] is null);
                }

                if (_activeStart == _availableStart)
                {
                    Debug.Assert(_activeStart == 0, $"No active bytes but _activeStart={_activeStart}");
                }
            }
        }
    }

    // This is a Memory-like struct for handling multi-array segments from MultiArrayBuffer above.
    // It supports standard Span/Memory operations like indexing, Slice, Length, etc
    // It also supports CopyTo/CopyFrom Span<byte>

    internal readonly struct MultiMemory
    {
        private readonly byte[]?[]? _blocks;
        private readonly uint _start;
        private readonly uint _length;

        private const int BlockSize = 16 * 1024;

        internal MultiMemory(byte[]?[]? blocks, uint start, uint length)
        {
            if (length == 0)
            {
                _blocks = null;
                _start = 0;
                _length = 0;
            }
            else
            {
                Debug.Assert(blocks is not null);
                Debug.Assert(start <= int.MaxValue);
                Debug.Assert(length <= int.MaxValue);
                Debug.Assert(start + length <= blocks.Length * BlockSize);

                _blocks = blocks;
                _start = start;
                _length = length;
            }
        }

        private static uint GetBlockIndex(uint offset) => offset / BlockSize;
        private static uint GetOffsetInBlock(uint offset) => offset % BlockSize;

        public bool IsEmpty => _length == 0;

        public int Length => (int)_length;

        public ref byte this[int index]
        {
            get
            {
                uint uindex = (uint)index;
                if (uindex >= _length)
                {
                    throw new IndexOutOfRangeException();
                }

                uint offset = _start + uindex;
                return ref _blocks![GetBlockIndex(offset)]![GetOffsetInBlock(offset)];
            }
        }

        public int BlockCount => (int)(GetBlockIndex(_start + _length + (BlockSize - 1)) - GetBlockIndex(_start));

        public Memory<byte> GetBlock(int blockIndex)
        {
            if ((uint)blockIndex >= BlockCount)
            {
                throw new IndexOutOfRangeException();
            }

            Debug.Assert(_length > 0, "Length should never be 0 here because BlockCount would be 0");
            Debug.Assert(_blocks is not null);

            uint startInBlock = (blockIndex == 0 ? GetOffsetInBlock(_start) : 0);
            uint endInBlock = (blockIndex == BlockCount - 1 ? GetOffsetInBlock(_start + _length - 1) + 1 : BlockSize);

            Debug.Assert(0 <= startInBlock, $"Invalid startInBlock={startInBlock}. blockIndex={blockIndex}, _blocks.Length={_blocks.Length}, _start={_start}, _length={_length}");
            Debug.Assert(startInBlock < endInBlock, $"Invalid startInBlock={startInBlock}, endInBlock={endInBlock}. blockIndex={blockIndex}, _blocks.Length={_blocks.Length}, _start={_start}, _length={_length}");
            Debug.Assert(endInBlock <= BlockSize, $"Invalid endInBlock={endInBlock}. blockIndex={blockIndex}, _blocks.Length={_blocks.Length}, _start={_start}, _length={_length}");

            return new Memory<byte>(_blocks[GetBlockIndex(_start) + blockIndex], (int)startInBlock, (int)(endInBlock - startInBlock));
        }

        public MultiMemory Slice(int start)
        {
            uint ustart = (uint)start;
            if (ustart > _length)
            {
                throw new IndexOutOfRangeException();
            }

            return new MultiMemory(_blocks, _start + ustart, _length - ustart);
        }

        public MultiMemory Slice(int start, int length)
        {
            uint ustart = (uint)start;
            uint ulength = (uint)length;
            if (ustart > _length || ulength > _length - ustart)
            {
                throw new IndexOutOfRangeException();
            }

            return new MultiMemory(_blocks, _start + ustart, ulength);
        }

        public void CopyTo(Span<byte> destination)
        {
            if (destination.Length < _length)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            int blockCount = BlockCount;
            for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                Memory<byte> block = GetBlock(blockIndex);
                block.Span.CopyTo(destination);
                destination = destination.Slice(block.Length);
            }
        }

        public void CopyFrom(ReadOnlySpan<byte> source)
        {
            if (_length < source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            int blockCount = BlockCount;
            for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                Memory<byte> block = GetBlock(blockIndex);

                if (source.Length <= block.Length)
                {
                    source.CopyTo(block.Span);
                    break;
                }

                source.Slice(0, block.Length).CopyTo(block.Span);
                source = source.Slice(block.Length);
            }
        }

        public static MultiMemory Empty => default;
    }
}
