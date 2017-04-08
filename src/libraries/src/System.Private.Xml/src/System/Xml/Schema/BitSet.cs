// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Text;
    using System.Diagnostics;

    internal sealed class BitSet
    {
        private const int bitSlotShift = 5;
        private const int bitSlotMask = (1 << bitSlotShift) - 1;

        private int _count;
        private uint[] _bits;

        private BitSet()
        {
        }

        public BitSet(int count)
        {
            _count = count;
            _bits = new uint[Subscript(count + bitSlotMask)];
        }

        public int Count
        {
            get { return _count; }
        }

        public bool this[int index]
        {
            get
            {
                return Get(index);
            }
        }

        public void Clear()
        {
            int bitsLength = _bits.Length;
            for (int i = bitsLength; i-- > 0;)
            {
                _bits[i] = 0;
            }
        }

        public void Set(int index)
        {
            int nBitSlot = Subscript(index);
            EnsureLength(nBitSlot + 1);
            _bits[nBitSlot] |= (uint)1 << (index & bitSlotMask);
        }


        public bool Get(int index)
        {
            bool fResult = false;
            if (index < _count)
            {
                int nBitSlot = Subscript(index);
                fResult = ((_bits[nBitSlot] & (1 << (index & bitSlotMask))) != 0);
            }
            return fResult;
        }

        public int NextSet(int startFrom)
        {
            Debug.Assert(startFrom >= -1 && startFrom <= _count);
            int offset = startFrom + 1;
            if (offset == _count)
            {
                return -1;
            }
            int nBitSlot = Subscript(offset);
            offset &= bitSlotMask;
            uint word = _bits[nBitSlot] >> offset;
            // locate non-empty slot
            while (word == 0)
            {
                if ((++nBitSlot) == _bits.Length)
                {
                    return -1;
                }
                offset = 0;
                word = _bits[nBitSlot];
            }
            while ((word & (uint)1) == 0)
            {
                word >>= 1;
                offset++;
            }
            return (nBitSlot << bitSlotShift) + offset;
        }

        public void And(BitSet other)
        {
            /*
             * Need to synchronize  both this and other->
             * This might lead to deadlock if one thread grabs them in one order
             * while another thread grabs them the other order.
             * Use a trick from Doug Lea's book on concurrency,
             * somewhat complicated because BitSet overrides hashCode().
             */
            if (this == other)
            {
                return;
            }
            int bitsLength = _bits.Length;
            int setLength = other._bits.Length;
            int n = (bitsLength > setLength) ? setLength : bitsLength;
            for (int i = n; i-- > 0;)
            {
                _bits[i] &= other._bits[i];
            }
            for (; n < bitsLength; n++)
            {
                _bits[n] = 0;
            }
        }


        public void Or(BitSet other)
        {
            if (this == other)
            {
                return;
            }
            int setLength = other._bits.Length;
            EnsureLength(setLength);
            for (int i = setLength; i-- > 0;)
            {
                _bits[i] |= other._bits[i];
            }
        }

        public override int GetHashCode()
        {
            int h = 1234;
            for (int i = _bits.Length; --i >= 0;)
            {
                h ^= unchecked((int)_bits[i] * (i + 1));
            }
            return (int)((h >> 32) ^ h);
        }


        public override bool Equals(object obj)
        {
            // assume the same type
            if (obj != null)
            {
                if (this == obj)
                {
                    return true;
                }
                BitSet other = (BitSet)obj;

                int bitsLength = _bits.Length;
                int setLength = other._bits.Length;
                int n = (bitsLength > setLength) ? setLength : bitsLength;
                for (int i = n; i-- > 0;)
                {
                    if (_bits[i] != other._bits[i])
                    {
                        return false;
                    }
                }
                if (bitsLength > n)
                {
                    for (int i = bitsLength; i-- > n;)
                    {
                        if (_bits[i] != 0)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    for (int i = setLength; i-- > n;)
                    {
                        if (other._bits[i] != 0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public BitSet Clone()
        {
            BitSet newset = new BitSet();
            newset._count = _count;
            newset._bits = (uint[])_bits.Clone();
            return newset;
        }


        public bool IsEmpty
        {
            get
            {
                uint k = 0;
                for (int i = 0; i < _bits.Length; i++)
                {
                    k |= _bits[i];
                }
                return k == 0;
            }
        }

        public bool Intersects(BitSet other)
        {
            int i = Math.Min(_bits.Length, other._bits.Length);
            while (--i >= 0)
            {
                if ((_bits[i] & other._bits[i]) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        private int Subscript(int bitIndex)
        {
            return bitIndex >> bitSlotShift;
        }

        private void EnsureLength(int nRequiredLength)
        {
            /* Doesn't need to be synchronized because it's an internal method. */
            if (nRequiredLength > _bits.Length)
            {
                /* Ask for larger of doubled size or required size */
                int request = 2 * _bits.Length;
                if (request < nRequiredLength)
                    request = nRequiredLength;
                uint[] newBits = new uint[request];
                Array.Copy(_bits, newBits, _bits.Length);
                _bits = newBits;
            }
        }
    };
}

