// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents a bitmap of GC pointers within a memory region divided into
    /// pointer-sized cells.
    /// </summary>
    public partial struct GCPointerMap : IEquatable<GCPointerMap>, IComparable<GCPointerMap>
    {
        // Each bit in this array represents a pointer-sized cell.
        private int[] _gcFlags;

        private int _numCells;

        /// <summary>
        /// Gets a value indicating whether this map is initialized.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                return _gcFlags != null;
            }
        }

        /// <summary>
        /// Gets the size (in cells) of the pointer map.
        /// </summary>
        public int Size
        {
            get
            {
                return _numCells;
            }
        }

        /// <summary>
        /// Gets the number of continuous runs of GC pointers within the map.
        /// </summary>
        public int NumSeries
        {
            get
            {
                int numSeries = 0;
                for (int i = 0; i < _numCells; i++)
                {
                    if (this[i])
                    {
                        numSeries++;
                        while (++i < _numCells && this[i]) ;
                    }
                }
                return numSeries;
            }
        }

        /// <summary>
        /// Returns true if the map is all pointers
        /// </summary>
        public bool IsAllGCPointers
        {
            get
            {
                for (int i = 0; i < _numCells; i++)
                {
                    if (!this[i])
                        return false;
                }
                return true;
            }
        }

        public bool this[int index]
        {
            get
            {
                return (_gcFlags[index >> 5] & (1 << (index & 0x1F))) != 0;
            }
        }

        public GCPointerMap(int[] gcFlags, int numCells)
        {
            Debug.Assert(numCells <= gcFlags.Length << 5);
            _gcFlags = gcFlags;
            _numCells = numCells;
        }

        public BitEnumerator GetEnumerator()
        {
            return new BitEnumerator(_gcFlags, 0, _numCells);
        }

        public override bool Equals(object obj)
        {
            return obj is GCPointerMap && Equals((GCPointerMap)obj);
        }

        public bool Equals(GCPointerMap other)
        {
            if (_numCells != other._numCells)
                return false;

            for (int i = 0; i < _gcFlags.Length; i++)
                if (_gcFlags[i] != other._gcFlags[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            for (int i = 0; i < _gcFlags.Length; i++)
                hashCode ^= _gcFlags[i];
            return hashCode;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(_numCells);
            foreach (var bit in this)
                sb.Append(bit ? '1' : '0');
            return sb.ToString();
        }
        
        public int CompareTo(GCPointerMap other)
        {
            if (_numCells != other._numCells)
                return _numCells - other._numCells;

            for (int i = 0; i < _gcFlags.Length; i++)
            {
                if (_gcFlags[i] != other._gcFlags[i])
                    return _gcFlags[i] - other._gcFlags[i];
            }

            Debug.Assert(Equals(other));
            return 0;
        }
    }

    /// <summary>
    /// Utility class to assist in building <see cref="GCPointerMap"/>.
    /// </summary>
    public struct GCPointerMapBuilder
    {
        // Each bit in this array represents a pointer-sized cell.
        // Bits start at the least significant bit.
        private int[] _gcFlags;

        private int _pointerSize;

        // Both of these are in bytes.
        private int _delta;
        private int _limit;

        public GCPointerMapBuilder(int numBytes, int pointerSize)
        {
            // Align the size up. The size of the pointer map is used to infer the statics storage size that has
            // to include space for non-GC statics smaller than pointer size.
            int numPointerSizedCells = (numBytes + pointerSize - 1) / pointerSize;

            if (numPointerSizedCells > 0)
            {
                // Given the number of cells, how many Int32's do we need to represent them?
                // (It's one bit per cell, but this time we need to round up.)
                _gcFlags = new int[((numPointerSizedCells - 1) >> 5) + 1];
            }
            else
            {
                // Not big enough to fit even a single pointer.
                _gcFlags = Array.Empty<int>();
            }

            _pointerSize = pointerSize;

            _delta = 0;
            _limit = numBytes;
        }

        public void MarkGCPointer(int offset)
        {
            Debug.Assert(offset >= 0);

            int absoluteOffset = _delta + offset;

            Debug.Assert(absoluteOffset % _pointerSize == 0);
            Debug.Assert(absoluteOffset <= (_limit - _pointerSize));

            int cellIndex = absoluteOffset / _pointerSize;

            _gcFlags[cellIndex >> 5] |= 1 << (cellIndex & 0x1F);
        }

        public GCPointerMapBuilder GetInnerBuilder(int offset, int size)
        {
            Debug.Assert(offset >= 0);

            int absoluteOffset = _delta + offset;

            Debug.Assert(absoluteOffset + size <= _limit);

            return new GCPointerMapBuilder
            {
                _gcFlags = this._gcFlags,
                _pointerSize = this._pointerSize,
                _delta = absoluteOffset,
                _limit = absoluteOffset + size
            };
        }

        public GCPointerMap ToGCMap()
        {
            Debug.Assert(_delta == 0);
            return new GCPointerMap(_gcFlags, (_limit + _pointerSize - 1) / _pointerSize);
        }

        public BitEnumerator GetEnumerator()
        {
            int numCells = (_limit - _delta) / _pointerSize;
            int startCell = _delta / _pointerSize;
            return new BitEnumerator(_gcFlags, startCell, numCells);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var bit in this)
                sb.Append(bit ? '1' : '0');
            return sb.ToString();
        }
    }

    public struct BitEnumerator
    {
        private int[] _buffer;
        private int _limitBit;
        private int _currentBit;

        public BitEnumerator(int[] buffer, int startBit, int numBits)
        {
            Debug.Assert(startBit >= 0 && numBits >= 0);
            Debug.Assert(startBit + numBits <= buffer.Length << 5);

            _buffer = buffer;
            _currentBit = startBit - 1;
            _limitBit = startBit + numBits;
        }

        public bool Current
        {
            get
            {
                return (_buffer[_currentBit >> 5] & (1 << (_currentBit & 0x1F))) != 0;
            }
        }

        public bool MoveNext()
        {
            _currentBit++;
            return _currentBit < _limitBit;
        }
    }
}
