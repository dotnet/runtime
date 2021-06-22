// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace System.Collections.Specialized
{
    /// <devdoc>
    ///    <para>Provides a simple light bit vector with easy integer or Boolean access to
    ///       a 32 bit storage.</para>
    /// </devdoc>
    public struct BitVector32
    {
        private uint _data;

        /// <devdoc>
        /// <para>Initializes a new instance of the BitVector32 structure with the specified internal data.</para>
        /// </devdoc>
        public BitVector32(int data)
        {
            _data = unchecked((uint)data);
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the BitVector32 structure with the information in the specified
        ///    value.</para>
        /// </devdoc>
        public BitVector32(BitVector32 value)
        {
            _data = value._data;
        }

        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether all the specified bits are set.</para>
        /// </devdoc>
        public bool this[int bit]
        {
            get
            {
                return (_data & bit) == unchecked((uint)bit);
            }
            set
            {
                unchecked
                {
                    if (value)
                    {
                        _data |= (uint)bit;
                    }
                    else
                    {
                        _data &= ~(uint)bit;
                    }
                }
            }
        }

        /// <devdoc>
        ///    <para>Gets or sets the value for the specified section.</para>
        /// </devdoc>
        public int this[Section section]
        {
            get
            {
                unchecked
                {
                    return (int)((_data & (uint)(section.Mask << section.Offset)) >> section.Offset);
                }
            }
            set
            {
                // The code should really have originally validated "(value & section.Mask) == value" with
                // an exception (it instead validated it with a Debug.Assert, which does little good in a
                // public method when in a Release build).  We don't include such a check now as it would
                // likely break things and for little benefit.

                value <<= section.Offset;
                int offsetMask = (0xFFFF & (int)section.Mask) << section.Offset;
                _data = unchecked((_data & ~(uint)offsetMask) | ((uint)value & (uint)offsetMask));
            }
        }

        /// <devdoc>
        ///    returns the raw data stored in this bit vector...
        /// </devdoc>
        public int Data
        {
            get
            {
                return unchecked((int)_data);
            }
        }

        /// <devdoc>
        ///    <para> Creates the first mask in a series.</para>
        /// </devdoc>
        public static int CreateMask()
        {
            return CreateMask(0);
        }

        /// <devdoc>
        ///     Creates the next mask in a series.
        /// </devdoc>
        public static int CreateMask(int previous)
        {
            if (previous == 0)
            {
                return 1;
            }

            if (previous == unchecked((int)0x80000000))
            {
                throw new InvalidOperationException(SR.BitVectorFull);
            }

            return previous << 1;
        }

        /// <devdoc>
        ///    <para>Creates the first section in a series, with the specified maximum value.</para>
        /// </devdoc>
        public static Section CreateSection(short maxValue)
        {
            return CreateSectionHelper(maxValue, 0, 0);
        }

        /// <devdoc>
        ///    <para>Creates the next section in a series, with the specified maximum value.</para>
        /// </devdoc>
        public static Section CreateSection(short maxValue, Section previous)
        {
            return CreateSectionHelper(maxValue, previous.Mask, previous.Offset);
        }

        private static Section CreateSectionHelper(short maxValue, short priorMask, short priorOffset)
        {
            if (maxValue < 1)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidValue_TooSmall, nameof(maxValue), 1), nameof(maxValue));
            }

            short offset = (short)(priorOffset + BitOperations.PopCount((uint)(ushort)priorMask));
            if (offset >= 32)
            {
                throw new InvalidOperationException(SR.BitVectorFull);
            }

            short mask = (short)(BitOperations.RoundUpToPowerOf2((uint)(ushort)maxValue + 1) - 1);
            return new Section(mask, offset);
        }

        public override bool Equals([NotNullWhen(true)] object? o) => o is BitVector32 other && _data == other._data;

        public override int GetHashCode() => _data.GetHashCode();

        public static string ToString(BitVector32 value)
        {
            return string.Create(/*"BitVector32{".Length*/12 + /*32 bits*/32 + /*"}".Length"*/1, value, (dst, v) =>
            {
                ReadOnlySpan<char> prefix = "BitVector32{";
                prefix.CopyTo(dst);
                dst[dst.Length - 1] = '}';

                int locdata = unchecked((int)v._data);
                dst = dst.Slice(prefix.Length, 32);
                for (int i = 0; i < dst.Length; i++)
                {
                    dst[i] = (locdata & 0x80000000) != 0 ? '1' : '0';
                    locdata <<= 1;
                }
            });
        }

        public override string ToString()
        {
            return ToString(this);
        }

        /// <devdoc>
        ///    <para>
        ///       Represents an section of the vector that can contain a integer number.</para>
        /// </devdoc>
        public readonly struct Section
        {
            private readonly short _mask;
            private readonly short _offset;

            internal Section(short mask, short offset)
            {
                _mask = mask;
                _offset = offset;
            }

            public short Mask => _mask;

            public short Offset => _offset;

            public override bool Equals([NotNullWhen(true)] object? o) => o is Section other && Equals(other);

            public bool Equals(Section obj)
            {
                return obj._mask == _mask && obj._offset == _offset;
            }

            public static bool operator ==(Section a, Section b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(Section a, Section b)
            {
                return !(a == b);
            }

            public override int GetHashCode() => HashCode.Combine(_mask, _offset);

            public static string ToString(Section value)
            {
                return $"Section{{0x{value.Mask:x}, 0x{value.Offset:x}}}";
            }

            public override string ToString()
            {
                return ToString(this);
            }
        }
    }
}
