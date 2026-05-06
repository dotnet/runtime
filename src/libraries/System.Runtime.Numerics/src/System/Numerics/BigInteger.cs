// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Numerics
{
    [Serializable]
    [TypeForwardedFrom("System.Numerics, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089")]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly partial struct BigInteger
        : ISpanFormattable,
          IComparable,
          IComparable<BigInteger>,
          IEquatable<BigInteger>,
          IBinaryInteger<BigInteger>,
          ISignedNumber<BigInteger>,
          ISerializable // introduced in .NET 11 for compat with existing serialized assets, not exposed in the ref assembly
    {
        internal const uint UInt32HighBit = 0x80000000;
        internal const int BitsPerUInt32 = 32;
        internal const int BitsPerUInt64 = 64;
        internal const int DecimalScaleFactorMask = 0x00FF0000;

        /// <summary>Splits a shift by int.MinValue into two shifts to avoid negation overflow (-int.MinValue overflows int).</summary>
        private const int MinIntSplitShift = int.MaxValue - BitsPerUInt32 + 1;

        /// <summary>
        /// Maximum number of limbs in a <see cref="BigInteger"/>. Restricts allocations to ~256MB,
        /// supporting almost 646,456,974 digits.
        /// </summary>
        internal static int MaxLength => Array.MaxLength / BigIntegerCalculator.BitsPerLimb;

        /// <summary>
        /// For values <c>int.MinValue &lt; n &lt;= int.MaxValue</c>, the value is stored in
        /// <see cref="_sign"/> and <see cref="_bits"/> is <see langword="null"/>.
        /// For all other values, <see cref="_sign"/> is +1 or -1 and the magnitude is in <see cref="_bits"/>.
        /// </summary>
        /// <remarks>
        /// This field is <see langword="int"/> rather than <see langword="nint"/> by design.
        /// Using <see langword="nint"/> would allow values up to <see cref="long.MaxValue"/> to be stored
        /// inline on 64-bit, avoiding an array allocation. However, that would regress the common
        /// case of values in the <see cref="int"/> range by requiring wider comparisons and branches
        /// everywhere <see cref="_sign"/> is used.
        /// </remarks>
        internal readonly int _sign; // Do not rename (binary serialization)
        internal readonly nuint[]? _bits; // Do not rename (binary serialization)

        /// <summary>
        /// Cached representation of <see cref="int.MinValue"/> as a BigInteger. Uses the large
        /// representation (sign=-1, bits=[0x80000000]) so that negation is symmetric.
        /// </summary>
        private static readonly BigInteger s_int32MinValue = new(-1, [UInt32HighBit]);
        private static readonly BigInteger s_one = new(1);
        private static readonly BigInteger s_zero = new(0);
        private static readonly BigInteger s_minusOne = new(-1);

        public BigInteger(int value)
        {
            if (value == int.MinValue)
            {
                this = s_int32MinValue;
            }
            else
            {
                _sign = value;
                _bits = null;
            }

            AssertValid();
        }

        [CLSCompliant(false)]
        public BigInteger(uint value)
        {
            if (value <= int.MaxValue)
            {
                _sign = (int)value;
                _bits = null;
            }
            else
            {
                _sign = +1;
                _bits = [value];
            }

            AssertValid();
        }

        public BigInteger(long value)
        {
            if (value is > int.MinValue and <= int.MaxValue)
            {
                _sign = (int)value;
                _bits = null;
            }
            else if (value == int.MinValue)
            {
                this = s_int32MinValue;
            }
            else
            {
                ulong x;
                if (value < 0)
                {
                    x = (ulong)-value;
                    _sign = -1;
                }
                else
                {
                    x = (ulong)value;
                    _sign = +1;
                }

                if (nint.Size == 8)
                {
                    _bits = [(nuint)x];
                }
                else
                {
                    _bits = x <= uint.MaxValue ? [((uint)x)] : [(uint)x, (uint)(x >> BitsPerUInt32)];
                }
            }

            AssertValid();
        }

        [CLSCompliant(false)]
        public BigInteger(ulong value)
        {
            if (value <= int.MaxValue)
            {
                _sign = (int)value;
                _bits = null;
            }
            else
            {
                _sign = +1;
                if (nint.Size == 8)
                {
                    _bits = [(nuint)value];
                }
                else
                {
                    _bits = value <= uint.MaxValue ? [((uint)value)] : [(uint)value, (uint)(value >> BitsPerUInt32)];
                }
            }

            AssertValid();
        }

        public BigInteger(float value) : this((double)value)
        {
        }

        public BigInteger(double value)
        {
            if (!double.IsFinite(value))
            {
                throw new OverflowException(double.IsInfinity(value) ? SR.Overflow_BigIntInfinity : SR.Overflow_NotANumber);
            }

            _sign = 0;
            _bits = null;

            NumericsHelpers.GetDoubleParts(value, out int sign, out int exp, out ulong man, out _);
            Debug.Assert(sign is +1 or -1);

            if (man == 0)
            {
                this = Zero;
                return;
            }

            Debug.Assert(man < (1UL << 53));
            Debug.Assert(exp <= 0 || man >= (1UL << 52));

            if (exp <= 0)
            {
                if (exp <= -BitsPerUInt64)
                {
                    this = Zero;
                    return;
                }

                this = man >> -exp;
                if (sign < 0)
                {
                    _sign = -_sign;
                }
            }
            else if (exp <= 11)
            {
                // 53-bit mantissa shifted left by at most 11 fits in 64 bits (53 + 11 = 64),
                // so the result fits in a single inline value without needing _bits.
                this = man << exp;
                if (sign < 0)
                {
                    _sign = -_sign;
                }
            }
            else
            {
                // Overflow into multiple limbs.
                // Move the leading 1 to the high bit.
                man <<= 11;
                exp -= 11;

                int bitsPerLimb = BigIntegerCalculator.BitsPerLimb;

                // Compute cu and cbit so that exp == bitsPerLimb * cu - cbit and 0 <= cbit < bitsPerLimb.
                int cu = (exp - 1) / bitsPerLimb + 1;
                int cbit = cu * bitsPerLimb - exp;
                Debug.Assert(0 <= cbit && cbit < bitsPerLimb);
                Debug.Assert(cu >= 1);

                // Populate the limbs.
                if (nint.Size == 8)
                {
                    // 64-bit: mantissa (64 bits) fits in 1-2 nuint limbs
                    _bits = new nuint[cu + 1];
                    _bits[cu] = (nuint)(man >> cbit);
                    if (cbit > 0)
                    {
                        _bits[cu - 1] = (nuint)(man << (64 - cbit));
                    }
                }
                else
                {
                    // 32-bit: mantissa (64 bits) spans 2-3 nuint limbs
                    _bits = new nuint[cu + 2];
                    _bits[cu + 1] = (uint)(man >> (cbit + BitsPerUInt32));
                    _bits[cu] = (uint)(man >> cbit);
                    if (cbit > 0)
                    {
                        _bits[cu - 1] = (nuint)(uint)man << (BitsPerUInt32 - cbit);
                    }
                }

                _sign = sign;
            }

            AssertValid();
        }

        public BigInteger(decimal value)
        {
            // First truncate to get scale to 0 and extract bits
            Span<int> bits = stackalloc int[4];
            decimal.GetBits(decimal.Truncate(value), bits);

            Debug.Assert(bits.Length == 4 && (bits[3] & DecimalScaleFactorMask) == 0);

            const int SignMask = int.MinValue;
            int size =
                bits[2] != 0 ? 3 :
                bits[1] != 0 ? 2 :
                bits[0] != 0 ? 1 :
                0;

            if (size == 0)
            {
                this = s_zero;
            }
            else if (size == 1 && bits[0] > 0)
            {
                // bits[0] is the absolute value of this decimal
                // if bits[0] < 0 then it is too large to be packed into _sign
                _sign = bits[0];
                _sign *= ((bits[3] & SignMask) != 0) ? -1 : +1;
                _bits = null;
            }
            else
            {
                if (nint.Size == 8)
                {
                    // 64-bit: pack up to 3 uint-sized values into 1-2 nuint limbs
                    int nuintSize = (size + 1) / 2;
                    _bits = new nuint[nuintSize];
                    _bits[0] = (uint)bits[0];
                    if (size > 1)
                    {
                        _bits[0] |= (nuint)(uint)bits[1] << 32;
                        if (size > 2)
                        {
                            _bits[1] = (uint)bits[2];
                        }
                    }
                }
                else
                {
                    _bits = new nuint[size];
                    _bits[0] = (uint)bits[0];
                    if (size > 1)
                    {
                        _bits[1] = (uint)bits[1];
                        if (size > 2)
                        {
                            _bits[2] = (uint)bits[2];
                        }
                    }
                }

                _sign = ((bits[3] & SignMask) != 0) ? -1 : +1;

                // Canonicalize: single-limb values that fit in int should be stored inline
                if (_bits.Length is 1 && _bits[0] <= int.MaxValue)
                {
                    _sign = _sign < 0 ? -(int)_bits[0] : (int)_bits[0];
                    _bits = null;
                }
            }

            AssertValid();
        }

        /// <summary>
        /// Creates a BigInteger from a little-endian twos-complement byte array.
        /// </summary>
        /// <param name="value"></param>
        [CLSCompliant(false)]
        public BigInteger(byte[] value) :
            this(new ReadOnlySpan<byte>(value ?? throw new ArgumentNullException(nameof(value))))
        {
        }

        public BigInteger(ReadOnlySpan<byte> value, bool isUnsigned = false, bool isBigEndian = false)
        {
            int byteCount = value.Length;

            bool isNegative;
            if (byteCount > 0)
            {
                byte mostSignificantByte = isBigEndian ? value[0] : value[byteCount - 1];
                isNegative = (mostSignificantByte & 0x80) != 0 && !isUnsigned;

                if (mostSignificantByte == 0)
                {
                    // Try to conserve space as much as possible by checking for wasted leading byte[] entries
                    if (isBigEndian)
                    {
                        int offset = value.Slice(1).IndexOfAnyExcept((byte)0);
                        value = value.Slice(offset < 0 ? byteCount : offset + 1);
                        byteCount = value.Length;
                    }
                    else
                    {
                        byteCount = value[..^1].LastIndexOfAnyExcept((byte)0) + 1;
                    }
                }
            }
            else
            {
                isNegative = false;
            }

            if (byteCount == 0)
            {
                // BigInteger.Zero
                _sign = 0;
                _bits = null;
                AssertValid();
                return;
            }

            if (byteCount <= 4)
            {
                _sign = isNegative ? -1 : 0;

                if (isBigEndian)
                {
                    for (int i = 0; i < byteCount; i++)
                    {
                        _sign = (_sign << 8) | value[i];
                    }
                }
                else
                {
                    for (int i = byteCount - 1; i >= 0; i--)
                    {
                        _sign = (_sign << 8) | value[i];
                    }
                }

                _bits = null;
                if (_sign < 0 && !isNegative)
                {
                    // int overflow: unsigned value overflows into the int sign bit
                    _bits = [(uint)_sign];
                    _sign = +1;
                }

                if (_sign == int.MinValue)
                {
                    this = s_int32MinValue;
                }
            }
            else
            {
                int wholeLimbCount = Math.DivRem(byteCount, nint.Size, out int unalignedBytes);
                nuint[] val = new nuint[wholeLimbCount + (unalignedBytes == 0 ? 0 : 1)];

                // Copy the bytes to the nuint array, apart from those which represent the
                // most significant limb if it's not a full limb.
                // The limbs are stored in 'least significant first' order.
                if (isBigEndian)
                {
                    // The bytes parameter is in big-endian byte order.
                    // We need to read the limbs out in reverse.

                    Span<byte> limbBytes = MemoryMarshal.AsBytes(val.AsSpan(0, wholeLimbCount));

                    // We need to slice off the remainder from the beginning.
                    value.Slice(unalignedBytes).CopyTo(limbBytes);

                    limbBytes.Reverse();
                }
                else
                {
                    // The bytes parameter is in little-endian byte order.
                    // We can just copy the bytes directly into the nuint array.
                    value.Slice(0, wholeLimbCount * nint.Size).CopyTo(MemoryMarshal.AsBytes(val.AsSpan()));
                }

                // In both of the above cases on big-endian architecture, we need to perform
                // an endianness swap on the resulting limbs.
                if (!BitConverter.IsLittleEndian)
                {
                    Span<nuint> limbSpan = val.AsSpan(0, wholeLimbCount);
                    BinaryPrimitives.ReverseEndianness(limbSpan, limbSpan);
                }

                // Copy the last limb specially if it's not aligned
                if (unalignedBytes != 0)
                {
                    if (isNegative)
                    {
                        val[wholeLimbCount] = nuint.MaxValue;
                    }

                    if (isBigEndian)
                    {
                        for (int curByte = 0; curByte < unalignedBytes; curByte++)
                        {
                            byte curByteValue = value[curByte];
                            val[wholeLimbCount] = (val[wholeLimbCount] << 8) | curByteValue;
                        }
                    }
                    else
                    {
                        for (int curByte = byteCount - 1; curByte >= byteCount - unalignedBytes; curByte--)
                        {
                            byte curByteValue = value[curByte];
                            val[wholeLimbCount] = (val[wholeLimbCount] << 8) | curByteValue;
                        }
                    }
                }

                if (isNegative)
                {
                    NumericsHelpers.DangerousMakeTwosComplement(val); // Mutates val

                    // Pack _bits to remove any wasted space after the twos complement
                    int len = val.AsSpan().LastIndexOfAnyExcept(0u) + 1;

                    if (len == 1)
                    {
                        if (val[0] == 1) // abs(-1)
                        {
                            this = s_minusOne;
                            return;
                        }
                        else if (val[0] == UInt32HighBit) // abs(int.MinValue)
                        {
                            this = s_int32MinValue;
                            return;
                        }
                        else if (val[0] < UInt32HighBit) // fits in int as negative
                        {
                            _sign = -(int)val[0];
                            _bits = null;
                            AssertValid();
                            return;
                        }
                    }

                    if (len != val.Length)
                    {
                        _sign = -1;
                        _bits = val.AsSpan(0, len).ToArray();
                    }
                    else
                    {
                        _sign = -1;
                        _bits = val;
                    }
                }
                else
                {
                    _sign = +1;
                    _bits = val;
                }
            }

            AssertValid();
        }

        /// <summary>
        /// Create a BigInteger directly from inner components (sign and bits).
        /// The caller must ensure the parameters are valid.
        /// </summary>
        /// <param name="sign">the sign field</param>
        /// <param name="bits">the bits field</param>
        internal BigInteger(int sign, nuint[]? bits)
        {
            // Runtime check is converted to assertions because only one call from TryParseBigIntegerHexOrBinaryNumberStyle may fail the length check.
            // Validation in TryParseBigIntegerHexOrBinaryNumberStyle is also added in the accompanying PR.

            _sign = sign;
            _bits = bits;

            AssertValid();
        }

        /// <summary>
        /// Constructor used during bit manipulation and arithmetic.
        /// When possible the value will be packed into  _sign to conserve space.
        /// </summary>
        /// <param name="value">The absolute value of the number</param>
        /// <param name="negative">The bool indicating the sign of the value.</param>
        internal BigInteger(ReadOnlySpan<nuint> value, bool negative)
        {
            // Try to conserve space as much as possible by checking for wasted leading span entries
            // sometimes the span has leading zeros from bit manipulation operations & and ^

            int length = value.LastIndexOfAnyExcept(0u) + 1;
            value = value[..length];

            if (value.Length > MaxLength)
            {
                ThrowHelper.ThrowOverflowException();
            }

            if (value.Length == 0)
            {
                this = default;
            }
            else if (value.Length == 1 && value[0] < UInt32HighBit)
            {
                _sign = negative ? -(int)value[0] : (int)value[0];
                _bits = null;
            }
            else if (value.Length == 1 && negative && value[0] == UInt32HighBit)
            {
                // Although int.MinValue fits in _sign, we represent this case differently for negate
                this = s_int32MinValue;
            }
            else
            {
                _sign = negative ? -1 : +1;
                _bits = value.ToArray();
            }

            AssertValid();
        }

        /// <summary>
        /// Create a BigInteger from a little-endian twos-complement nuint span.
        /// </summary>
        /// <param name="value"></param>
        private BigInteger(Span<nuint> value)
        {
            bool isNegative;
            int length;

            if ((value.Length > 0) && ((nint)value[^1] < 0))
            {
                isNegative = true;
                length = value.LastIndexOfAnyExcept(nuint.MaxValue) + 1;

                if ((length == 0) || ((nint)value[length - 1] >= 0))
                {
                    // We need to preserve the sign bit
                    length++;
                }

                Debug.Assert((nint)value[length - 1] < 0);
            }
            else
            {
                isNegative = false;
                length = value.LastIndexOfAnyExcept(0u) + 1;
            }

            value = value[..length];

            if (value.Length > MaxLength)
            {
                ThrowHelper.ThrowOverflowException();
            }

            if (value.Length == 0)
            {
                // 0
                this = s_zero;
            }
            else if (value.Length == 1)
            {
                if (isNegative)
                {
                    if (value[0] == nuint.MaxValue)
                    {
                        // -1
                        this = s_minusOne;
                    }
                    else if (nint.Size == 4 && value[0] == UInt32HighBit)
                    {
                        // int.MinValue
                        this = s_int32MinValue;
                    }
                    else
                    {
                        // Single-limb negative twos-complement: convert to magnitude and
                        // check if it fits in int _sign.
                        NumericsHelpers.DangerousMakeTwosComplement(value);
                        nuint magnitude = value[0];

                        if (magnitude < UInt32HighBit)
                        {
                            _sign = -(int)magnitude;
                            _bits = null;
                        }
                        else if (nint.Size == 8)
                        {
                            // On 64-bit, check if multi-uint magnitude fits in one nuint
                            _sign = -1;
                            int trimLen = value.LastIndexOfAnyExcept(0u) + 1;
                            _bits = trimLen == 1 ? [magnitude] : value[..trimLen].ToArray();
                        }
                        else
                        {
                            // On 32-bit, magnitude > int.MaxValue always needs _bits
                            _sign = -1;
                            _bits = [magnitude];
                        }
                    }
                }
                else if (value[0] >= UInt32HighBit)
                {
                    _sign = +1;
                    _bits = [value[0]];
                }
                else
                {
                    _sign = (int)value[0];
                    _bits = null;
                }
            }
            else
            {
                if (isNegative)
                {
                    NumericsHelpers.DangerousMakeTwosComplement(value);

                    // Retrim any leading zeros carried from the sign
                    length = value.LastIndexOfAnyExcept(0u) + 1;
                    value = value[..length];

                    _sign = -1;
                }
                else
                {
                    _sign = +1;
                }

                _bits = value.ToArray();
            }

            AssertValid();
        }

        /// <summary>
        /// Initializes a new <see cref="BigInteger"/> from serialized data.
        /// Reads <see cref="_bits"/> as <see cref="uint"/>[] for backward compatibility with previous
        /// runtimes where the field was <see cref="uint"/>[].
        /// </summary>
        private BigInteger(SerializationInfo info, StreamingContext _)
        {
            ArgumentNullException.ThrowIfNull(info);

            _sign = info.GetInt32("_sign");
            uint[]? bits32 = (uint[]?)info.GetValue("_bits", typeof(uint[]));

            if (bits32 is null)
            {
                _bits = null;
            }
            else if (nint.Size == 4)
            {
                _bits = new nuint[bits32.Length];
                Buffer.BlockCopy(bits32, 0, _bits, 0, bits32.Length * sizeof(uint));
            }
            else
            {
                int nuintLen = (bits32.Length + 1) / 2;
                _bits = new nuint[nuintLen];
                for (int i = 0; i < bits32.Length; i += 2)
                {
                    ulong lo = bits32[i];
                    ulong hi = (i + 1 < bits32.Length) ? bits32[i + 1] : 0;
                    _bits[i / 2] = (nuint)(lo | (hi << 32));
                }
            }

            AssertValid();
        }

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the <see cref="BigInteger"/>.
        /// Serializes <see cref="_bits"/> as <see cref="uint"/>[] for backward compatibility with previous
        /// runtimes where the field was <see cref="uint"/>[].
        /// </summary>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);
            info.AddValue("_sign", _sign);

            uint[]? bits32 = null;
            if (_bits is not null)
            {
                if (nint.Size == 4)
                {
                    bits32 = new uint[_bits.Length];
                    Buffer.BlockCopy(_bits, 0, bits32, 0, _bits.Length * sizeof(uint));
                }
                else
                {
                    int len = _bits.Length * 2;
                    if ((uint)(_bits[^1] >> 32) == 0)
                    {
                        len--;
                    }

                    bits32 = new uint[len];
                    for (int i = 0; i < _bits.Length; i++)
                    {
                        bits32[i * 2] = (uint)_bits[i];
                        if (i * 2 + 1 < len)
                        {
                            bits32[i * 2 + 1] = (uint)(_bits[i] >> 32);
                        }
                    }
                }
            }

            info.AddValue("_bits", bits32, typeof(uint[]));
        }

        public static BigInteger Zero => s_zero;

        public static BigInteger One => s_one;

        public static BigInteger MinusOne => s_minusOne;

        public bool IsPowerOfTwo
        {
            get
            {
                if (_bits is null)
                {
                    return BitOperations.IsPow2(_sign);
                }

                if (_sign != 1)
                {
                    return false;
                }

                int iu = _bits.Length - 1;
                return BitOperations.IsPow2(_bits[iu]) && !_bits.AsSpan(0, iu).ContainsAnyExcept(0u);
            }
        }

        public bool IsZero => _sign == 0;

        public bool IsOne => _sign == 1 && _bits is null;

        public bool IsEven => _bits is null ? (_sign & 1) == 0 : (_bits[0] & 1) == 0;

        public int Sign => (_sign >> 31) - (-_sign >> 31);

        public static BigInteger Parse(string value)
        {
            return Parse(value, NumberStyles.Integer);
        }

        public static BigInteger Parse(string value, NumberStyles style)
        {
            return Parse(value, style, NumberFormatInfo.CurrentInfo);
        }

        public static BigInteger Parse(string value, IFormatProvider? provider)
        {
            return Parse(value, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static BigInteger Parse(string value, NumberStyles style, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(value);
            return Parse(value.AsSpan(), style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? value, out BigInteger result)
        {
            return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? value, NumberStyles style, IFormatProvider? provider, out BigInteger result)
        {
            return TryParse(value.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static BigInteger Parse(ReadOnlySpan<char> value, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            return Number.ParseBigInteger(MemoryMarshal.Cast<char, Utf16Char>(value), style, NumberFormatInfo.GetInstance(provider));
        }

        public static BigInteger Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            return Number.ParseBigInteger(MemoryMarshal.Cast<byte, Utf8Char>(utf8Text), style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse(ReadOnlySpan<char> value, out BigInteger result)
        {
            return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> value, NumberStyles style, IFormatProvider? provider, out BigInteger result)
        {
            return Number.TryParseBigInteger(MemoryMarshal.Cast<char, Utf16Char>(value), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out BigInteger result)
        {
            return TryParse(utf8Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out BigInteger result)
        {
            return Number.TryParseBigInteger(MemoryMarshal.Cast<byte, Utf8Char>(utf8Text), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
        }

        public static int Compare(BigInteger left, BigInteger right)
        {
            return left.CompareTo(right);
        }

        public static BigInteger Abs(BigInteger value)
        {
            return new BigInteger((int)NumericsHelpers.Abs(value._sign), value._bits);
        }

        public static BigInteger Add(BigInteger left, BigInteger right)
        {
            return left + right;
        }

        public static BigInteger Subtract(BigInteger left, BigInteger right)
        {
            return left - right;
        }

        public static BigInteger Multiply(BigInteger left, BigInteger right)
        {
            return left * right;
        }

        public static BigInteger Divide(BigInteger dividend, BigInteger divisor)
        {
            return dividend / divisor;
        }

        public static BigInteger Remainder(BigInteger dividend, BigInteger divisor)
        {
            return dividend % divisor;
        }

        public static BigInteger DivRem(BigInteger dividend, BigInteger divisor, out BigInteger remainder)
        {
            bool trivialDividend = dividend._bits is null;
            bool trivialDivisor = divisor._bits is null;

            if (trivialDividend && trivialDivisor)
            {
                BigInteger quotient;
                (int q, int r) = Math.DivRem(dividend._sign, divisor._sign);
                quotient = q;
                remainder = r;
                return quotient;
            }

            if (trivialDividend)
            {
                // The divisor is non-trivial and therefore the bigger one.
                remainder = dividend;
                return s_zero;
            }

            Debug.Assert(dividend._bits is not null);

            if (trivialDivisor)
            {
                int size = dividend._bits.Length;
                Span<nuint> quotient = RentedBuffer.Create(size, out RentedBuffer quotientBuffer);
                using var _ = quotientBuffer;

                // may throw DivideByZeroException
                BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign), quotient, out nuint rest);

                remainder = dividend._sign < 0 ? -(long)rest : (long)rest;
                return new BigInteger(quotient, (dividend._sign < 0) ^ (divisor._sign < 0));
            }

            Debug.Assert(divisor._bits is not null);

            if (dividend._bits.Length < divisor._bits.Length)
            {
                remainder = dividend;
                return s_zero;
            }
            else
            {
                int size = dividend._bits.Length;
                Span<nuint> rest = RentedBuffer.Create(size, out RentedBuffer restBuffer);

                size = dividend._bits.Length - divisor._bits.Length + 1;
                Span<nuint> quotient = RentedBuffer.Create(size, out RentedBuffer quotientBuffer);

                BigIntegerCalculator.Divide(dividend._bits, divisor._bits, quotient, rest);

                remainder = new(rest, dividend._sign < 0);
                BigInteger result = new(quotient, (dividend._sign < 0) ^ (divisor._sign < 0));

                restBuffer.Dispose();
                quotientBuffer.Dispose();

                return result;
            }
        }

        public static BigInteger Negate(BigInteger value)
        {
            return -value;
        }

        public static double Log(BigInteger value)
        {
            return Log(value, Math.E);
        }

        public static double Log(BigInteger value, double baseValue)
        {
            if (value._sign < 0 || baseValue == 1.0D)
            {
                return double.NaN;
            }

            if (baseValue == double.PositiveInfinity)
            {
                return value.IsOne ? 0.0D : double.NaN;
            }

            if (baseValue == 0.0D && !value.IsOne)
            {
                return double.NaN;
            }

            if (value._bits is null)
            {
                return Math.Log(value._sign, baseValue);
            }

            ulong h, m, l;
            int c;
            long b;
            ulong x;

            if (nint.Size == 8)
            {
                h = value._bits[^1];
                m = value._bits.Length > 1 ? value._bits[^2] : 0;

                c = BitOperations.LeadingZeroCount(h);
                b = (long)value._bits.Length * 64 - c;

                // Extract most significant 64 bits
                x = c == 0 ? h : (h << c) | (m >> (64 - c));
            }
            else
            {
                h = (uint)value._bits[^1];
                m = value._bits.Length > 1 ? (uint)value._bits[^2] : 0;
                l = value._bits.Length > 2 ? (uint)value._bits[^3] : 0;

                // Measure the exact bit count
                c = BitOperations.LeadingZeroCount((uint)h);
                b = (long)value._bits.Length * 32 - c;

                // Extract most significant bits
                x = (h << 32 + c) | (m << c) | (l >> 32 - c);
            }

            // Let v = value, b = bit count, x = v/2^b-64
            // log ( v/2^b-64 * 2^b-64 ) = log ( x ) + log ( 2^b-64 )
            return Math.Log(x, baseValue) + (b - 64) / Math.Log(baseValue, 2);
        }

        public static double Log10(BigInteger value)
        {
            return Log(value, 10);
        }

        public static BigInteger GreatestCommonDivisor(BigInteger left, BigInteger right)
        {
            bool trivialLeft = left._bits is null;
            bool trivialRight = right._bits is null;

            if (trivialLeft && trivialRight)
            {
                return BigIntegerCalculator.Gcd(NumericsHelpers.Abs(left._sign), NumericsHelpers.Abs(right._sign));
            }

            if (trivialLeft)
            {
                Debug.Assert(right._bits is not null);
                return left._sign != 0
                    ? BigIntegerCalculator.Gcd(right._bits, NumericsHelpers.Abs(left._sign))
                    : new BigInteger(+1, right._bits);
            }

            if (trivialRight)
            {
                Debug.Assert(left._bits is not null);
                return right._sign != 0
                    ? BigIntegerCalculator.Gcd(left._bits, NumericsHelpers.Abs(right._sign))
                    : new BigInteger(+1, left._bits);
            }

            Debug.Assert(left._bits is not null && right._bits is not null);

            return BigIntegerCalculator.Compare(left._bits, right._bits) < 0
                ? GreatestCommonDivisor(right._bits, left._bits)
                : GreatestCommonDivisor(left._bits, right._bits);
        }

        private static BigInteger GreatestCommonDivisor(ReadOnlySpan<nuint> leftBits, ReadOnlySpan<nuint> rightBits)
        {
            Debug.Assert(BigIntegerCalculator.Compare(leftBits, rightBits) >= 0);

            BigInteger result;

            // Short circuits to spare some allocations...
            if (rightBits.Length == 1)
            {
                nuint temp = BigIntegerCalculator.Remainder(leftBits, rightBits[0]);
                result = BigIntegerCalculator.Gcd(rightBits[0], temp);
            }
            else if (nint.Size == 4 && rightBits.Length == 2)
            {
                Span<nuint> bits = RentedBuffer.Create(leftBits.Length, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Remainder(leftBits, rightBits, bits);

                ulong left = ((ulong)rightBits[1] << 32) | (uint)rightBits[0];
                ulong right = ((ulong)bits[1] << 32) | (uint)bits[0];

                result = BigIntegerCalculator.Gcd(left, right);
                bitsBuffer.Dispose();
            }
            else
            {
                Span<nuint> bits = RentedBuffer.Create(leftBits.Length, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Gcd(leftBits, rightBits, bits);
                result = new BigInteger(bits, negative: false);
                bitsBuffer.Dispose();
            }

            return result;
        }

        public static BigInteger Max(BigInteger left, BigInteger right)
        {
            return left.CompareTo(right) < 0 ? right : left;
        }

        public static BigInteger Min(BigInteger left, BigInteger right)
        {
            return left.CompareTo(right) <= 0 ? left : right;
        }

        public static BigInteger ModPow(BigInteger value, BigInteger exponent, BigInteger modulus)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(exponent.Sign, nameof(exponent));

            bool trivialValue = value._bits is null;
            bool trivialExponent = exponent._bits is null;
            bool trivialModulus = modulus._bits is null;

            BigInteger result;

            if (trivialModulus)
            {
                nuint bitsResult = trivialValue && trivialExponent ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) :
                    trivialValue ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits!, NumericsHelpers.Abs(modulus._sign)) :
                    trivialExponent ? BigIntegerCalculator.Pow(value._bits!, NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) :
                    BigIntegerCalculator.Pow(value._bits!, exponent._bits!, NumericsHelpers.Abs(modulus._sign));

                result = value._sign < 0 && !exponent.IsEven ? -(long)bitsResult : (long)bitsResult;
            }
            else
            {
                int size = (modulus._bits?.Length ?? 1) << 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);
                if (trivialValue)
                {
                    if (trivialExponent)
                    {
                        BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), modulus._bits!, bits);
                    }
                    else
                    {
                        BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits!, modulus._bits!, bits);
                    }
                }
                else if (trivialExponent)
                {
                    BigIntegerCalculator.Pow(value._bits!, NumericsHelpers.Abs(exponent._sign), modulus._bits!, bits);
                }
                else
                {
                    BigIntegerCalculator.Pow(value._bits!, exponent._bits!, modulus._bits!, bits);
                }

                result = new BigInteger(bits, value._sign < 0 && !exponent.IsEven);

                bitsBuffer.Dispose();
            }

            return result;
        }

        public static BigInteger Pow(BigInteger value, int exponent)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(exponent);

            if (exponent == 0)
            {
                return s_one;
            }

            if (exponent == 1)
            {
                return value;
            }

            bool trivialValue = value._bits is null;

            nuint power = NumericsHelpers.Abs(exponent);
            BigInteger result;

            if (trivialValue)
            {
                if (value._sign == 1)
                {
                    return value;
                }

                if (value._sign == -1)
                {
                    return (exponent & 1) != 0 ? value : s_one;
                }

                if (value._sign == 0)
                {
                    return value;
                }

                int size = BigIntegerCalculator.PowBound(power, 1);
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), power, bits);
                result = new BigInteger(bits, value._sign < 0 && (exponent & 1) != 0);
                bitsBuffer.Dispose();
            }
            else
            {
                int size = BigIntegerCalculator.PowBound(power, value._bits!.Length);
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Pow(value._bits, power, bits);
                result = new BigInteger(bits, value._sign < 0 && (exponent & 1) != 0);
                bitsBuffer.Dispose();
            }

            return result;
        }

        public override int GetHashCode()
        {
            if (_bits is null)
            {
                return _sign;
            }

            HashCode hash = default;
            hash.AddBytes(MemoryMarshal.AsBytes(_bits.AsSpan()));
            hash.Add(_sign);
            return hash.ToHashCode();
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is BigInteger other && Equals(other);
        }

        public bool Equals(long other)
        {
            if (_bits is null)
            {
                return _sign == other;
            }

            int cu;
            int maxLimbs = sizeof(long) / nint.Size;
            if ((_sign ^ other) < 0 || (cu = _bits.Length) > maxLimbs)
            {
                return false;
            }

            ulong uu = other < 0 ? (ulong)-other : (ulong)other;

            if (nint.Size == 8)
            {
                return _bits[0] == uu;
            }
            else
            {
                return cu == 1
                    ? (uint)_bits[0] == uu
                    : ((ulong)(uint)_bits[1] << 32 | (uint)_bits[0]) == uu;
            }
        }

        [CLSCompliant(false)]
        public bool Equals(ulong other)
        {
            if (_sign < 0)
            {
                return false;
            }

            if (_bits is null)
            {
                return (ulong)_sign == other;
            }

            int cu = _bits.Length;
            int maxLimbs = sizeof(long) / nint.Size;
            if (cu > maxLimbs)
            {
                return false;
            }

            if (nint.Size == 8)
            {
                return _bits[0] == other;
            }
            else
            {
                return cu == 1
                    ? (uint)_bits[0] == other
                    : ((ulong)(uint)_bits[1] << 32 | (uint)_bits[0]) == other;
            }
        }

        public bool Equals(BigInteger other)
        {
            return _sign == other._sign && _bits.AsSpan().SequenceEqual(other._bits);
        }

        public int CompareTo(long other)
        {
            if (_bits is null)
            {
                return ((long)_sign).CompareTo(other);
            }

            int cu;
            int maxLimbs = sizeof(long) / nint.Size;
            if ((_sign ^ other) < 0 || (cu = _bits.Length) > maxLimbs)
            {
                return _sign;
            }

            ulong uu = other < 0 ? (ulong)-other : (ulong)other;
            ulong uuTmp;

            if (nint.Size == 8)
            {
                uuTmp = _bits[0];
            }
            else
            {
                uuTmp = cu == 2
                    ? ((ulong)(uint)_bits[1] << 32 | (uint)_bits[0])
                    : (uint)_bits[0];
            }

            return _sign * uuTmp.CompareTo(uu);
        }

        [CLSCompliant(false)]
        public int CompareTo(ulong other)
        {
            if (_sign < 0)
            {
                return -1;
            }

            if (_bits is null)
            {
                return ((ulong)(uint)_sign).CompareTo(other);
            }

            int cu = _bits.Length;
            int maxLimbs = sizeof(long) / nint.Size;
            if (cu > maxLimbs)
            {
                return +1;
            }

            ulong uuTmp;

            if (nint.Size == 8)
            {
                uuTmp = _bits[0];
            }
            else
            {
                uuTmp = cu == 2
                    ? ((ulong)(uint)_bits[1] << 32 | (uint)_bits[0])
                    : (uint)_bits[0];
            }

            return uuTmp.CompareTo(other);
        }

        public int CompareTo(BigInteger other)
        {
            if ((_sign ^ other._sign) < 0)
            {
                // Different signs, so the comparison is easy.
                return _sign < 0 ? -1 : +1;
            }

            // Same signs
            if (_bits is null)
            {
                return
                    other._bits is not null ? -other._sign :
                    _sign < other._sign ? -1 :
                    _sign > other._sign ? +1 :
                    0;
            }

            if (other._bits is null)
            {
                return _sign;
            }

            int bitsResult = BigIntegerCalculator.Compare(_bits, other._bits);
            return _sign < 0 ? -bitsResult : bitsResult;
        }

        public int CompareTo(object? obj)
        {
            return
                obj is null ? 1 :
                obj is BigInteger bigInt ? CompareTo(bigInt) :
                throw new ArgumentException(SR.Argument_MustBeBigInt, nameof(obj));
        }

        /// <summary>
        /// Returns the value of this BigInteger as a little-endian twos-complement
        /// byte array, using the fewest number of bytes possible. If the value is zero,
        /// return an array of one byte whose element is 0x00.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray() => ToByteArray(isUnsigned: false, isBigEndian: false);

        /// <summary>
        /// Returns the value of this BigInteger as a byte array using the fewest number of bytes possible.
        /// If the value is zero, returns an array of one byte whose element is 0x00.
        /// </summary>
        /// <param name="isUnsigned">Whether or not an unsigned encoding is to be used</param>
        /// <param name="isBigEndian">Whether or not to write the bytes in a big-endian byte order</param>
        /// <returns></returns>
        /// <exception cref="OverflowException">
        ///   If <paramref name="isUnsigned"/> is <c>true</c> and <see cref="Sign"/> is negative.
        /// </exception>
        /// <remarks>
        /// The integer value <c>33022</c> can be exported as four different arrays.
        ///
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       <c>(isUnsigned: false, isBigEndian: false)</c> => <c>new byte[] { 0xFE, 0x80, 0x00 }</c>
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <c>(isUnsigned: false, isBigEndian: true)</c> => <c>new byte[] { 0x00, 0x80, 0xFE }</c>
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <c>(isUnsigned: true, isBigEndian: false)</c> => <c>new byte[] { 0xFE, 0x80 }</c>
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <c>(isUnsigned: true, isBigEndian: true)</c> => <c>new byte[] { 0x80, 0xFE }</c>
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        public byte[] ToByteArray(bool isUnsigned = false, bool isBigEndian = false)
        {
            int ignored = 0;
            return TryGetBytes(GetBytesMode.AllocateArray, default, isUnsigned, isBigEndian, ref ignored)!;
        }

        /// <summary>
        /// Copies the value of this BigInteger as little-endian twos-complement
        /// bytes, using the fewest number of bytes possible. If the value is zero,
        /// outputs one byte whose element is 0x00.
        /// </summary>
        /// <param name="destination">The destination span to which the resulting bytes should be written.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="isUnsigned">Whether or not an unsigned encoding is to be used</param>
        /// <param name="isBigEndian">Whether or not to write the bytes in a big-endian byte order</param>
        /// <returns>true if the bytes fit in <paramref name="destination"/>; false if not all bytes could be written due to lack of space.</returns>
        /// <exception cref="OverflowException">If <paramref name="isUnsigned"/> is <c>true</c> and <see cref="Sign"/> is negative.</exception>
        public bool TryWriteBytes(Span<byte> destination, out int bytesWritten, bool isUnsigned = false, bool isBigEndian = false)
        {
            bytesWritten = 0;
            if (TryGetBytes(GetBytesMode.Span, destination, isUnsigned, isBigEndian, ref bytesWritten) is null)
            {
                bytesWritten = 0;
                return false;
            }

            return true;
        }

        internal bool TryWriteOrCountBytes(Span<byte> destination, out int bytesWritten, bool isUnsigned = false, bool isBigEndian = false)
        {
            bytesWritten = 0;
            return TryGetBytes(GetBytesMode.Span, destination, isUnsigned, isBigEndian, ref bytesWritten) is not null;
        }

        /// <summary>Gets the number of bytes that will be output by <see cref="ToByteArray(bool, bool)"/> and <see cref="TryWriteBytes(Span{byte}, out int, bool, bool)"/>.</summary>
        /// <returns>The number of bytes.</returns>
        public int GetByteCount(bool isUnsigned = false)
        {
            // Big or Little Endian doesn't matter for the byte count.
            int count = 0;
            const bool IsBigEndian = false;
            TryGetBytes(GetBytesMode.Count, default, isUnsigned, IsBigEndian, ref count);
            return count;
        }

        /// <summary>Mode used to enable sharing <see cref="TryGetBytes(GetBytesMode, Span{byte}, bool, bool, ref int)"/> for multiple purposes.</summary>
        private enum GetBytesMode
        {
            AllocateArray,
            Count,
            Span
        }

        /// <summary>Shared logic for <see cref="ToByteArray(bool, bool)"/>, <see cref="TryWriteBytes(Span{byte}, out int, bool, bool)"/>, and <see cref="GetByteCount"/>.</summary>
        /// <param name="mode">Which entry point is being used.</param>
        /// <param name="destination">The destination span, if mode is <see cref="GetBytesMode.Span"/>.</param>
        /// <param name="isUnsigned">True to never write a padding byte, false to write it if the high bit is set.</param>
        /// <param name="isBigEndian">True for big endian byte ordering, false for little endian byte ordering.</param>
        /// <param name="bytesWritten">
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.AllocateArray"/>, ignored.
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.Count"/>, the number of bytes that would be written.
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.Span"/>, the number of bytes written to the span or that would be written if it were long enough.
        /// </param>
        /// <returns>
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.AllocateArray"/>, the result array.
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.Count"/>, null.
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.Span"/>, non-null if the span was long enough, null if there wasn't enough room.
        /// </returns>
        /// <exception cref="OverflowException">If <paramref name="isUnsigned"/> is <c>true</c> and <see cref="Sign"/> is negative.</exception>
        private byte[]? TryGetBytes(GetBytesMode mode, Span<byte> destination, bool isUnsigned, bool isBigEndian, ref int bytesWritten)
        {
            Debug.Assert(mode is GetBytesMode.AllocateArray or GetBytesMode.Count or GetBytesMode.Span, $"Unexpected mode {mode}.");
            Debug.Assert(mode == GetBytesMode.Span || destination.IsEmpty, $"If we're not in span mode, we shouldn't have been passed a destination.");

            int sign = _sign;
            if (sign == 0)
            {
                switch (mode)
                {
                    case GetBytesMode.AllocateArray:
                        return [0];

                    case GetBytesMode.Count:
                        bytesWritten = 1;
                        return null;

                    default: // case GetBytesMode.Span:
                        bytesWritten = 1;
                        if (destination.Length != 0)
                        {
                            destination[0] = 0;
                            return Array.Empty<byte>();
                        }

                        return null;
                }
            }

            if (isUnsigned && sign < 0)
            {
                throw new OverflowException(SR.Overflow_Negative_Unsigned);
            }

            int bytesPerLimb = nint.Size;
            byte highByte;
            int nonZeroLimbIndex = 0;
            nuint highLimb;
            nuint[]? bits = _bits;
            if (bits is null)
            {
                highByte = (byte)((sign < 0) ? 0xff : 0x00);
                highLimb = (nuint)sign;
            }
            else if (sign == -1)
            {
                highByte = 0xff;

                // If sign is -1, we will need to two's complement bits.
                // Previously this was accomplished via NumericsHelpers.DangerousMakeTwosComplement(),
                // however, we can do the two's complement on the stack so as to avoid
                // creating a temporary copy of bits just to hold the two's complement.
                // One special case in DangerousMakeTwosComplement() is that if the array
                // is all zeros, then it would allocate a new array with the high-order
                // limb set to 1 (for the carry). In our usage, we will not hit this case
                // because a bits array of all zeros would represent 0, and this case
                // would be encoded as _bits = null and _sign = 0.
                Debug.Assert(bits.Length > 0);
                Debug.Assert(bits[^1] != 0);
                nonZeroLimbIndex = ((ReadOnlySpan<nuint>)bits).IndexOfAnyExcept(0u);

                highLimb = ~bits[^1];
                if (bits.Length - 1 == nonZeroLimbIndex)
                {
                    // This will not overflow because highLimb is less than or equal to nuint.MaxValue - 1.
                    Debug.Assert(highLimb <= nuint.MaxValue - 1);
                    highLimb += 1;
                }
            }
            else
            {
                Debug.Assert(sign == 1);
                highByte = 0x00;
                highLimb = bits[^1];
            }

            // Find the most significant byte index within the high limb.
            // Use LeadingZeroCount for O(1) instead of byte-scanning loop.
            byte msb;
            int msbIndex;
            if (highByte == 0x00)
            {
                // Positive: find highest non-zero byte
                int lzc = BitOperations.LeadingZeroCount(highLimb);
                msbIndex = Math.Max(0, bytesPerLimb - 1 - (lzc / 8));
            }
            else
            {
                // Negative: find highest non-0xFF byte
                int lzc = BitOperations.LeadingZeroCount(~highLimb);
                msbIndex = Math.Max(0, bytesPerLimb - 1 - (lzc / 8));
            }

            msb = (byte)(highLimb >> (msbIndex * 8));

            // Ensure high bit is 0 if positive, 1 if negative
            bool needExtraByte = (msb & 0x80) != (highByte & 0x80) && !isUnsigned;
            int length = msbIndex + 1 + (needExtraByte ? 1 : 0);
            if (bits is not null)
            {
                length = checked(bytesPerLimb * (bits.Length - 1) + length);
            }

            byte[] array;
            switch (mode)
            {
                case GetBytesMode.AllocateArray:
                    destination = array = new byte[length];
                    break;

                case GetBytesMode.Count:
                    bytesWritten = length;
                    return null;

                default: // case GetBytesMode.Span:
                    bytesWritten = length;
                    if (destination.Length < length)
                    {
                        return null;
                    }

                    array = Array.Empty<byte>();
                    break;
            }

            int curByte = isBigEndian ? length : 0;
            int increment = isBigEndian ? -1 : 1;

            if (bits is not null)
            {
                if (BitConverter.IsLittleEndian && sign > 0)
                {
                    ReadOnlySpan<byte> srcBytes = MemoryMarshal.AsBytes(bits.AsSpan(..^1));

                    if (isBigEndian)
                    {
                        curByte = length - srcBytes.Length;
                        Span<byte> destBytes = destination.Slice(curByte, srcBytes.Length);
                        srcBytes.CopyTo(destBytes);
                        destBytes.Reverse();
                    }
                    else
                    {
                        srcBytes.CopyTo(destination);
                        curByte = srcBytes.Length;
                    }
                }
                else
                {
                    for (int i = 0; i < bits.Length - 1; i++)
                    {
                        nuint limb = bits[i];

                        if (sign == -1)
                        {
                            limb = ~limb;
                            if (i <= nonZeroLimbIndex)
                            {
                                limb++;
                            }
                        }

                        if (isBigEndian)
                        {
                            curByte -= bytesPerLimb;
                            BinaryPrimitives.WriteUIntPtrBigEndian(destination.Slice(curByte), limb);
                        }
                        else
                        {
                            BinaryPrimitives.WriteUIntPtrLittleEndian(destination.Slice(curByte), limb);
                            curByte += bytesPerLimb;
                        }
                    }
                }
            }

            if (isBigEndian)
            {
                curByte--;
            }

            // Write significant bytes of the high limb
            Debug.Assert(msbIndex >= 0 && msbIndex < bytesPerLimb);
            for (int byteIdx = 0; byteIdx <= msbIndex; byteIdx++)
            {
                destination[curByte] = (byte)(highLimb >> (byteIdx * 8));
                if (byteIdx < msbIndex)
                {
                    curByte += increment;
                }
            }

            // Assert we're big endian, or little endian consistency holds.
            // Assert we're little endian, or big endian consistency holds.
            Debug.Assert(isBigEndian || (!needExtraByte && curByte == length - 1) || (needExtraByte && curByte == length - 2));
            Debug.Assert(!isBigEndian || (!needExtraByte && curByte == 0) || (needExtraByte && curByte == 1));

            if (needExtraByte)
            {
                curByte += increment;
                destination[curByte] = highByte;
            }

            return array;
        }

        /// <summary>
        /// Converts the value of this BigInteger to a little-endian twos-complement
        /// nuint span allocated by the caller using the fewest number of nuints possible.
        /// </summary>
        /// <param name="buffer">Pre-allocated buffer by the caller.</param>
        /// <returns>The actual number of copied elements.</returns>
        private int WriteTo(Span<nuint> buffer)
        {
            Debug.Assert(_bits is null || _sign == 0 ? buffer.Length == 2 : buffer.Length >= _bits.Length + 1);

            nuint highLimb;

            if (_bits is null)
            {
                buffer[0] = (nuint)_sign;
                highLimb = (_sign < 0) ? nuint.MaxValue : 0;
            }
            else
            {
                _bits.CopyTo(buffer);
                buffer = buffer.Slice(0, _bits.Length + 1);
                if (_sign == -1)
                {
                    NumericsHelpers.DangerousMakeTwosComplement(buffer.Slice(0, buffer.Length - 1));  // Mutates limbs
                    highLimb = nuint.MaxValue;
                }
                else
                {
                    highLimb = 0;
                }
            }

            // Find highest significant limb and ensure high bit is 0 if positive, 1 if negative
            int msb = Math.Max(0, buffer[..^1].LastIndexOfAnyExcept(highLimb));

            // Ensure high bit is 0 if positive, 1 if negative
            nuint highBitMask = (nuint)1 << (BigIntegerCalculator.BitsPerLimb - 1);
            bool needExtraLimb = (buffer[msb] & highBitMask) != (highLimb & highBitMask);
            int count;

            if (needExtraLimb)
            {
                count = msb + 2;
                buffer = buffer.Slice(0, count);
                buffer[^1] = highLimb;
            }
            else
            {
                count = msb + 1;
            }

            return count;
        }

        public override string ToString()
        {
            return Number.FormatBigInteger(this, null, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatBigInteger(this, null, NumberFormatInfo.GetInstance(provider));
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatBigInteger(this, format, NumberFormatInfo.CurrentInfo);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider));
        }

        private string DebuggerDisplay
        {
            get
            {
                // For very big numbers, ToString can be too long or even timeout for Visual Studio to display
                // Display a fast estimated value instead

                // Use ToString for small values

                if ((_bits is null) || (_bits.Length <= 4))
                {
                    return ToString();
                }

                // Estimate the value x as `L * 2^n`, while L is the value of high bits, and n is the length of low bits
                // Represent L as `k * 10^i`, then `x = L * 2^n = k * 10^(i + (n * log10(2)))`
                // Let `m = n * log10(2)`, the final result would be `x = (k * 10^(m - [m])) * 10^(i+[m])`

                const double Log10Of2 = 0.3010299956639812; // Log10(2)
                int bitsPerLimb = BigIntegerCalculator.BitsPerLimb;
                ulong highBits;
                double lowBitsCount;
                if (nint.Size == 8)
                {
                    highBits = _bits[^1];
                    lowBitsCount = _bits.Length - 1;
                }
                else
                {
                    highBits = ((ulong)_bits[^1] << BitsPerUInt32) + (uint)_bits[^2];
                    lowBitsCount = _bits.Length - 2;
                }

                double exponentLow = lowBitsCount * bitsPerLimb * Log10Of2;

                // Max possible length of _bits is int.MaxValue of bytes,
                // thus max possible value of BigInteger is 2^(8*Array.MaxLength)-1 which is larger than 10^(2^33)
                // Use long to avoid potential overflow
                long exponent = (long)exponentLow;
                double significand = highBits * Math.Pow(10, exponentLow - exponent);

                // scale significand to [1, 10)
                double log10 = Math.Log10(significand);
                if (log10 >= 1)
                {
                    exponent += (long)log10;
                    significand /= Math.Pow(10, Math.Floor(log10));
                }

                // The digits can be incorrect because of floating point errors and estimation in Log and Exp
                // Keep some digits in the significand. 8 is arbitrarily chosen, about half of the precision of double
                significand = Math.Round(significand, 8);

                if (significand >= 10.0)
                {
                    // 9.9999999999999 can be rounded to 10, make the display to be more natural
                    significand /= 10.0;
                    exponent++;
                }

                string signStr = _sign < 0 ? NumberFormatInfo.CurrentInfo.NegativeSign : "";

                // Use about a half of the precision of double
                return $"{signStr}{significand:F8}e+{exponent}";
            }
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider), MemoryMarshal.Cast<char, Utf16Char>(destination), out charsWritten);
        }

        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider), MemoryMarshal.Cast<byte, Utf8Char>(utf8Destination), out bytesWritten);
        }

        private static BigInteger Add(ReadOnlySpan<nuint> leftBits, int leftSign, ReadOnlySpan<nuint> rightBits, int rightSign)
        {
            bool trivialLeft = leftBits.IsEmpty;
            bool trivialRight = rightBits.IsEmpty;

            Debug.Assert(!(trivialLeft && trivialRight), "Trivial cases should be handled on the caller operator");

            BigInteger result;

            if (trivialLeft)
            {
                Debug.Assert(!rightBits.IsEmpty);

                int size = rightBits.Length + 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Add(rightBits, NumericsHelpers.Abs(leftSign), bits);
                result = new BigInteger(bits, leftSign < 0);
                bitsBuffer.Dispose();
            }
            else if (trivialRight)
            {
                Debug.Assert(!leftBits.IsEmpty);

                int size = leftBits.Length + 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Add(leftBits, NumericsHelpers.Abs(rightSign), bits);
                result = new BigInteger(bits, leftSign < 0);
                bitsBuffer.Dispose();
            }
            else if (leftBits.Length < rightBits.Length)
            {
                Debug.Assert(!leftBits.IsEmpty && !rightBits.IsEmpty);

                int size = rightBits.Length + 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Add(rightBits, leftBits, bits);
                result = new BigInteger(bits, leftSign < 0);
                bitsBuffer.Dispose();
            }
            else
            {
                Debug.Assert(!leftBits.IsEmpty && !rightBits.IsEmpty);

                int size = leftBits.Length + 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Add(leftBits, rightBits, bits);
                result = new BigInteger(bits, leftSign < 0);
                bitsBuffer.Dispose();
            }

            return result;
        }

        public static BigInteger operator -(BigInteger left, BigInteger right)
        {
            if (left._bits is null && right._bits is null)
            {
                return (long)left._sign - right._sign;
            }

            return left._sign < 0 != right._sign < 0
                ? Add(left._bits, left._sign, right._bits, -right._sign)
                : Subtract(left._bits, left._sign, right._bits, right._sign);
        }

        private static BigInteger Subtract(ReadOnlySpan<nuint> leftBits, int leftSign, ReadOnlySpan<nuint> rightBits, int rightSign)
        {
            bool trivialLeft = leftBits.IsEmpty;
            bool trivialRight = rightBits.IsEmpty;

            Debug.Assert(!(trivialLeft && trivialRight), "Trivial cases should be handled on the caller operator");

            BigInteger result;

            if (trivialLeft)
            {
                Debug.Assert(!rightBits.IsEmpty);

                int size = rightBits.Length;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Subtract(rightBits, NumericsHelpers.Abs(leftSign), bits);
                result = new BigInteger(bits, leftSign >= 0);
                bitsBuffer.Dispose();
            }
            else if (trivialRight)
            {
                Debug.Assert(!leftBits.IsEmpty);

                int size = leftBits.Length;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Subtract(leftBits, NumericsHelpers.Abs(rightSign), bits);
                result = new BigInteger(bits, leftSign < 0);
                bitsBuffer.Dispose();
            }
            else if (BigIntegerCalculator.Compare(leftBits, rightBits) < 0)
            {
                int size = rightBits.Length;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Subtract(rightBits, leftBits, bits);
                result = new BigInteger(bits, leftSign >= 0);
                bitsBuffer.Dispose();
            }
            else
            {
                Debug.Assert(!leftBits.IsEmpty && !rightBits.IsEmpty);

                int size = leftBits.Length;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Subtract(leftBits, rightBits, bits);
                result = new BigInteger(bits, leftSign < 0);
                bitsBuffer.Dispose();
            }

            return result;
        }

        //
        // Explicit Conversions From BigInteger
        //

        public static explicit operator byte(BigInteger value) => checked((byte)((int)value));

        /// <summary>Explicitly converts a big integer to a <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="char" /> value.</returns>
        public static explicit operator char(BigInteger value) => checked((char)((int)value));

        public static explicit operator decimal(BigInteger value)
        {
            if (value._bits is null)
            {
                return value._sign;
            }

            return checked((decimal)(Int128)value);
        }

        public static explicit operator double(BigInteger value)
        {
            int sign = value._sign;
            nuint[]? bits = value._bits;

            if (bits is null)
            {
                return sign;
            }

            int length = bits.Length;
            int bitsPerLimb = BigIntegerCalculator.BitsPerLimb;

            // The maximum exponent for doubles is 1023, which corresponds to a limb bit length of 1024.
            // All BigIntegers with bits[] longer than this evaluate to Double.Infinity (or NegativeInfinity).
            int infinityLength = 1024 / bitsPerLimb;

            if (length > infinityLength)
            {
                return sign == 1 ? double.PositiveInfinity : double.NegativeInfinity;
            }

            ulong h, m, l;
            int z, exp;
            ulong man;

            if (nint.Size == 8)
            {
                h = bits[length - 1];
                m = length > 1 ? bits[length - 2] : 0;

                z = BitOperations.LeadingZeroCount(h);
                exp = (length - 1) * 64 - z;
                man = z == 0 ? h : (h << z) | (m >> (64 - z));
            }
            else
            {
                h = (uint)bits[length - 1];
                m = length > 1 ? (uint)bits[length - 2] : 0;
                l = length > 2 ? (uint)bits[length - 3] : 0;

                z = BitOperations.LeadingZeroCount((uint)h);
                exp = (length - 2) * 32 - z;
                man = (h << 32 + z) | (m << z) | (l >> 32 - z);
            }

            return NumericsHelpers.GetDoubleFromParts(sign, exp, man);
        }

        /// <summary>Explicitly converts a big integer to a <see cref="Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="Half" /> value.</returns>
        public static explicit operator Half(BigInteger value) => (Half)(double)value;

        /// <summary>Explicitly converts a big integer to a <see cref="BFloat16" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="BFloat16" /> value.</returns>
        public static explicit operator BFloat16(BigInteger value) => (BFloat16)(double)value;

        public static explicit operator short(BigInteger value) => checked((short)((int)value));

        public static explicit operator int(BigInteger value)
        {
            if (value._bits is null)
            {
                return value._sign;
            }

            if (value._bits.Length > 1)
            {
                // More than one limb
                throw new OverflowException(SR.Overflow_Int32);
            }

            if (value._sign > 0)
            {
                return checked((int)value._bits[0]);
            }

            if (value._bits[0] > UInt32HighBit)
            {
                // Value > Int32.MinValue
                throw new OverflowException(SR.Overflow_Int32);
            }

            return -(int)value._bits[0];
        }

        public static explicit operator long(BigInteger value)
        {
            if (value._bits is null)
            {
                return value._sign;
            }

            int len = value._bits.Length;
            int maxLimbs = sizeof(long) / nint.Size;
            if (len > maxLimbs)
            {
                throw new OverflowException(SR.Overflow_Int64);
            }

            ulong uu;

            if (nint.Size == 8)
            {
                uu = value._bits[0];
            }
            else
            {
                uu = len > 1
                    ? ((ulong)(uint)value._bits[1] << 32 | (uint)value._bits[0])
                    : (uint)value._bits[0];
            }

            long ll = value._sign > 0 ? (long)uu : -(long)uu;
            if ((ll > 0 && value._sign > 0) || (ll < 0 && value._sign < 0))
            {
                // Signs match, no overflow
                return ll;
            }

            throw new OverflowException(SR.Overflow_Int64);
        }

        /// <summary>Explicitly converts a big integer to a <see cref="Int128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="Int128" /> value.</returns>
        public static explicit operator Int128(BigInteger value)
        {
            if (value._bits is null)
            {
                return value._sign;
            }

            int len = value._bits.Length;
            int maxLimbs = 16 / nint.Size;

            if (len > maxLimbs)
            {
                throw new OverflowException(SR.Overflow_Int128);
            }

            UInt128 uu;

            if (nint.Size == 8)
            {
                uu = len > 1 ? new UInt128(value._bits[1], value._bits[0]) : (UInt128)(ulong)value._bits[0];
            }
            else if (len > 2)
            {
                uu = new UInt128(
                    ((ulong)((len > 3) ? (uint)value._bits[3] : 0) << 32 | (uint)value._bits[2]),
                    ((ulong)(uint)value._bits[1] << 32 | (uint)value._bits[0])
                );
            }
            else
            {
                uu = len > 1
                    ? (UInt128)((ulong)(uint)value._bits[1] << 32 | (uint)value._bits[0])
                    : (UInt128)(uint)value._bits[0];
            }

            Int128 ll = (value._sign > 0) ? (Int128)uu : -(Int128)uu;

            if (((ll > 0) && (value._sign > 0)) || ((ll < 0) && (value._sign < 0)))
            {
                // Signs match, no overflow
                return ll;
            }

            throw new OverflowException(SR.Overflow_Int128);
        }

        /// <summary>Explicitly converts a big integer to a <see cref="IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="IntPtr" /> value.</returns>
        public static explicit operator nint(BigInteger value) => Environment.Is64BitProcess ? (nint)(long)value : (int)value;

        [CLSCompliant(false)]
        public static explicit operator sbyte(BigInteger value) => checked((sbyte)((int)value));

        public static explicit operator float(BigInteger value) => (float)((double)value);

        [CLSCompliant(false)]
        public static explicit operator ushort(BigInteger value) => checked((ushort)((int)value));

        [CLSCompliant(false)]
        public static explicit operator uint(BigInteger value)
        {
            if (value._bits is null)
            {
                return checked((uint)value._sign);
            }
            else
            {
                return value._bits.Length <= 1 && value._sign >= 0
                    ? checked((uint)value._bits[0])
                    : throw new OverflowException(SR.Overflow_UInt32);
            }
        }

        [CLSCompliant(false)]
        public static explicit operator ulong(BigInteger value)
        {
            if (value._bits is null)
            {
                return checked((ulong)value._sign);
            }

            int len = value._bits.Length;
            int maxLimbs = sizeof(long) / nint.Size;
            if (len > maxLimbs || value._sign < 0)
            {
                throw new OverflowException(SR.Overflow_UInt64);
            }

            if (nint.Size == 8)
            {
                return value._bits[0];
            }

            return len > 1
                ? ((ulong)(uint)value._bits[1] << 32 | (uint)value._bits[0])
                : (uint)value._bits[0];
        }

        /// <summary>Explicitly converts a big integer to a <see cref="UInt128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="UInt128" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator UInt128(BigInteger value)
        {
            if (value._bits is null)
            {
                return checked((UInt128)value._sign);
            }

            int len = value._bits.Length;
            int maxLimbs = 16 / nint.Size;

            if ((len > maxLimbs) || (value._sign < 0))
            {
                throw new OverflowException(SR.Overflow_UInt128);
            }

            if (nint.Size == 8)
            {
                return len > 1
                    ? new UInt128(value._bits[1], value._bits[0])
                    : (UInt128)(ulong)value._bits[0];
            }
            else if (len > 2)
            {
                return new UInt128(
                    ((ulong)((len > 3) ? (uint)value._bits[3] : 0) << 32 | (uint)value._bits[2]),
                    ((ulong)(uint)value._bits[1] << 32 | (uint)value._bits[0])
                );
            }
            else if (len > 1)
            {
                return ((ulong)(uint)value._bits[1] << 32 | (uint)value._bits[0]);
            }

            return (uint)value._bits[0];
        }

        /// <summary>Explicitly converts a big integer to a <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="UIntPtr" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(BigInteger value) => Environment.Is64BitProcess ? (nuint)(ulong)value : (uint)value;

        //
        // Explicit Conversions To BigInteger
        //

        public static explicit operator BigInteger(decimal value) => new BigInteger(value);

        public static explicit operator BigInteger(double value) => new BigInteger(value);

        /// <summary>Explicitly converts a <see cref="Half" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static explicit operator BigInteger(Half value) => new BigInteger((float)value);

        /// <summary>Explicitly converts a <see cref="BFloat16" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static explicit operator BigInteger(BFloat16 value) => new BigInteger((float)value);

        /// <summary>Explicitly converts a <see cref="Complex" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static explicit operator BigInteger(Complex value)
        {
            if (value.Imaginary != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }

            return (BigInteger)value.Real;
        }

        public static explicit operator BigInteger(float value) => new BigInteger(value);

        //
        // Implicit Conversions To BigInteger
        //

        public static implicit operator BigInteger(byte value) => new BigInteger(value);

        /// <summary>Implicitly converts a <see cref="char" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static implicit operator BigInteger(char value) => new BigInteger(value);

        public static implicit operator BigInteger(short value) => new BigInteger(value);

        public static implicit operator BigInteger(int value) => new BigInteger(value);

        public static implicit operator BigInteger(long value) => new BigInteger(value);

        /// <summary>Implicitly converts a <see cref="Int128" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static implicit operator BigInteger(Int128 value)
        {
            int sign;
            nuint[]? bits;

            if ((int.MinValue < value) && (value <= int.MaxValue))
            {
                if (value == int.MinValue)
                {
                    return s_int32MinValue;
                }

                sign = (int)(long)value;
                bits = null;
            }
            else
            {
                UInt128 x;
                if (value < 0)
                {
                    x = (UInt128)(-value);
                    sign = -1;
                }
                else
                {
                    x = (UInt128)value;
                    sign = +1;
                }

                if (nint.Size == 8)
                {
                    bits = x <= ulong.MaxValue
                        ? [(nuint)(ulong)x]
                        : [(nuint)(ulong)x, (nuint)(ulong)(x >> 64)];
                }
                else
                {
                    if (x <= uint.MaxValue)
                    {
                        bits = [(uint)(x >> (BitsPerUInt32 * 0))];
                    }
                    else if (x <= ulong.MaxValue)
                    {
                        bits = [(uint)(x >> (BitsPerUInt32 * 0)),
                                (uint)(x >> (BitsPerUInt32 * 1))];
                    }
                    else if (x <= new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF))
                    {
                        bits = [(uint)(x >> (BitsPerUInt32 * 0)),
                                (uint)(x >> (BitsPerUInt32 * 1)),
                                (uint)(x >> (BitsPerUInt32 * 2))];
                    }
                    else
                    {
                        bits = [(uint)(x >> (BitsPerUInt32 * 0)),
                                (uint)(x >> (BitsPerUInt32 * 1)),
                                (uint)(x >> (BitsPerUInt32 * 2)),
                                (uint)(x >> (BitsPerUInt32 * 3))];
                    }
                }
            }

            return new BigInteger(sign, bits);
        }

        /// <summary>Implicitly converts a <see cref="IntPtr" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static implicit operator BigInteger(nint value) => new BigInteger(value);

        [CLSCompliant(false)]
        public static implicit operator BigInteger(sbyte value) => new BigInteger(value);

        [CLSCompliant(false)]
        public static implicit operator BigInteger(ushort value) => new BigInteger(value);

        [CLSCompliant(false)]
        public static implicit operator BigInteger(uint value) => new BigInteger(value);

        [CLSCompliant(false)]
        public static implicit operator BigInteger(ulong value) => new BigInteger(value);

        /// <summary>Implicitly converts a <see cref="UInt128" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator BigInteger(UInt128 value)
        {
            int sign = +1;
            nuint[]? bits;

            if (value <= (ulong)int.MaxValue)
            {
                sign = (int)(ulong)value;
                bits = null;
            }
            else if (nint.Size == 8)
            {
                if (value <= ulong.MaxValue)
                {
                    bits = [(nuint)(ulong)value];
                }
                else
                {
                    bits = [(nuint)(ulong)value,
                            (nuint)(ulong)(value >> 64)];
                }
            }
            else if (value <= uint.MaxValue)
            {
                bits = [(uint)(value >> (BitsPerUInt32 * 0))];
            }
            else if (value <= ulong.MaxValue)
            {
                bits = [(uint)(value >> (BitsPerUInt32 * 0)),
                        (uint)(value >> (BitsPerUInt32 * 1))];
            }
            else if (value <= new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF))
            {
                bits = [(uint)(value >> (BitsPerUInt32 * 0)),
                        (uint)(value >> (BitsPerUInt32 * 1)),
                        (uint)(value >> (BitsPerUInt32 * 2))];
            }
            else
            {
                bits = [(uint)(value >> (BitsPerUInt32 * 0)),
                        (uint)(value >> (BitsPerUInt32 * 1)),
                        (uint)(value >> (BitsPerUInt32 * 2)),
                        (uint)(value >> (BitsPerUInt32 * 3))];
            }

            return new BigInteger(sign, bits);
        }

        /// <summary>Implicitly converts a <see cref="UIntPtr" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator BigInteger(nuint value) => value <= int.MaxValue ? new BigInteger((int)value, null) : new BigInteger(+1, [value]);

        public static BigInteger operator &(BigInteger left, BigInteger right) =>
            left.IsZero || right.IsZero ? Zero :
                left._bits is null && right._bits is null ? (BigInteger)(left._sign & right._sign) :
                BitwiseAnd(ref left, ref right);

        public static BigInteger operator |(BigInteger left, BigInteger right)
        {
            if (left.IsZero)
            {
                return right;
            }

            if (right.IsZero)
            {
                return left;
            }

            return left._bits is null && right._bits is null
                ? (BigInteger)(left._sign | right._sign)
                : BitwiseOr(ref left, ref right);
        }

        public static BigInteger operator ^(BigInteger left, BigInteger right) =>
            left._bits is null && right._bits is null
                ? (BigInteger)(left._sign ^ right._sign)
                : BitwiseXor(ref left, ref right);

        /// <summary>
        /// Computes two's complement AND directly from magnitude representation,
        /// eliminating temporary buffers for the operands.
        /// </summary>
        private static BigInteger BitwiseAnd(ref readonly BigInteger left, ref readonly BigInteger right)
        {
            int xLen = left._bits?.Length ?? 1;
            int yLen = right._bits?.Length ?? 1;

            // AND result length: for positive operands, min length suffices (AND with 0 = 0),
            // plus 1 for sign extension so the two's complement constructor doesn't
            // misinterpret a high bit in the top limb as a negative sign.
            // For negative operands (sign-extended with 1s), we need max length + 1 for sign.
            int zLen = (left._sign < 0 || right._sign < 0)
                ? Math.Max(xLen, yLen) + 1
                : Math.Min(xLen, yLen) + 1;

            return BitwiseOp<BigIntegerCalculator.BitwiseAndOp>(in left, in right, zLen);
        }

        /// <summary>
        /// Computes two's complement OR directly from magnitude representation.
        /// </summary>
        private static BigInteger BitwiseOr(ref readonly BigInteger left, ref readonly BigInteger right)
        {
            int xLen = left._bits?.Length ?? 1;
            int yLen = right._bits?.Length ?? 1;
            return BitwiseOp<BigIntegerCalculator.BitwiseOrOp>(in left, in right, Math.Max(xLen, yLen) + 1);
        }

        /// <summary>
        /// Computes two's complement XOR directly from magnitude representation.
        /// </summary>
        private static BigInteger BitwiseXor(ref readonly BigInteger left, ref readonly BigInteger right)
        {
            int xLen = left._bits?.Length ?? 1;
            int yLen = right._bits?.Length ?? 1;
            return BitwiseOp<BigIntegerCalculator.BitwiseXorOp>(in left, in right, Math.Max(xLen, yLen) + 1);
        }

        private static BigInteger BitwiseOp<TOp>(ref readonly BigInteger left, ref readonly BigInteger right, int zLen)
            where TOp : struct, BigIntegerCalculator.IBitwiseOp
        {
            Span<nuint> z = RentedBuffer.Create(zLen, out RentedBuffer zBuffer);

            BigIntegerCalculator.BitwiseOp<TOp>(
                left._bits, left._sign,
                right._bits, right._sign,
                z);

            BigInteger result = new(z);

            zBuffer.Dispose();

            return result;
        }

        public static BigInteger operator <<(BigInteger value, int shift)
        {
            if (shift == 0)
            {
                return value;
            }

            if (shift == int.MinValue)
            {
                return value >> MinIntSplitShift >> BitsPerUInt32;
            }

            if (shift < 0)
            {
                return value >> -shift;
            }

            (int digitShift, int smallShift) = Math.DivRem(shift, BigIntegerCalculator.BitsPerLimb);

            if (value._bits is null)
            {
                return LeftShift(value._sign, digitShift, smallShift);
            }


            ReadOnlySpan<nuint> bits = value._bits;

            Debug.Assert(bits.Length > 0);


            nuint over = smallShift == 0
                ? 0
                : bits[^1] >> (BigIntegerCalculator.BitsPerLimb - smallShift);

            nuint[] z;
            int zLength = bits.Length + digitShift;
            if (over != 0)
            {
                z = new nuint[++zLength];
                z[^1] = over;
            }
            else
            {
                z = new nuint[zLength];
            }

            Span<nuint> zd = z.AsSpan(digitShift, bits.Length);

            bits.CopyTo(zd);

            BigIntegerCalculator.LeftShiftSelf(zd, smallShift, out nuint carry);

            Debug.Assert(carry == over);
            Debug.Assert(z[^1] != 0);

            return new BigInteger(value._sign, z);
        }

        private static BigInteger LeftShift(int value, int digitShift, int smallShift)
        {
            if (value == 0)
            {
                return s_zero;
            }

            nuint m = NumericsHelpers.Abs(value);

            nuint r = m << smallShift;
            nuint over = smallShift == 0
                ? 0
                : m >> (BigIntegerCalculator.BitsPerLimb - smallShift);

            nuint[] rgu;

            if (over == 0)
            {
                if (digitShift == 0 && r <= int.MaxValue)
                {
                    return new BigInteger(value >= 0 ? (int)r : -(int)r, null);
                }

                rgu = new nuint[digitShift + 1];
            }
            else
            {
                rgu = new nuint[digitShift + 2];
                rgu[^1] = over;
            }

            rgu[digitShift] = r;

            return new BigInteger(value > 0 ? 1 : -1, rgu);
        }

        public static BigInteger operator >>(BigInteger value, int shift)
        {
            if (shift == 0)
            {
                return value;
            }

            if (shift == int.MinValue)
            {
                return value << BitsPerUInt32 << MinIntSplitShift;
            }

            if (shift < 0)
            {
                return value << -shift;
            }

            (int digitShift, int smallShift) = Math.DivRem(shift, BigIntegerCalculator.BitsPerLimb);

            if (value._bits is null)
            {
                if (digitShift != 0 || smallShift >= 32)
                {
                    // If the shift length exceeds the int bit width, non-negative values result
                    // in 0, and negative values result in -1. This behavior can be implemented
                    // using a 31-bit right shift on an int type.
                    smallShift = 31;
                }

                return new BigInteger(value._sign >> smallShift, null);
            }

            ReadOnlySpan<nuint> bits = value._bits;

            Debug.Assert(bits.Length > 0);

            int zLength = bits.Length - digitShift + 1;

            if (zLength <= 1)
            {
                return new BigInteger(value._sign >> 31, null);
            }

            Span<nuint> zd = RentedBuffer.Create(zLength, out RentedBuffer zdBuffer);

            zd[^1] = 0;
            bits.Slice(digitShift).CopyTo(zd);

            BigIntegerCalculator.RightShiftSelf(zd, smallShift, out nuint carry);

            bool neg = value._sign < 0;
            if (neg && (carry != 0 || bits.Slice(0, digitShift).ContainsAnyExcept(0u)))
            {
                // Since right shift rounds towards zero, rounding up is performed
                // if the number is negative and the shifted-out bits are not all zeros.
                int leastSignificant = zd.IndexOfAnyExcept(nuint.MaxValue);
                Debug.Assert((uint)leastSignificant < (uint)zd.Length);
                ++zd[leastSignificant];
                zd.Slice(0, leastSignificant).Clear();
            }

            BigInteger result = new(zd, neg);

            zdBuffer.Dispose();

            return result;
        }

        public static BigInteger operator ~(BigInteger value)
        {
            value.AssertValid();

            if (value._bits is null)
            {
                return ~value._sign; // implicit int -> BigInteger handles int.MinValue
            }

            BigInteger result;

            if (value._sign >= 0)
            {
                // ~positive = -(positive + 1): add 1 to magnitude, negate
                int size = value._bits.Length + 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Add(value._bits, 1, bits);
                result = new BigInteger(bits, negative: true);
                bitsBuffer.Dispose();
            }
            else
            {
                // ~negative = |negative| - 1: subtract 1 from magnitude
                Span<nuint> bits = RentedBuffer.Create(value._bits.Length, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Subtract(value._bits, 1, bits);
                result = new BigInteger(bits, negative: false);
                bitsBuffer.Dispose();
            }

            return result;
        }

        public static BigInteger operator -(BigInteger value) => new BigInteger(-value._sign, value._bits);

        public static BigInteger operator +(BigInteger value) => value;

        public static BigInteger operator ++(BigInteger value)
        {
            if (value._bits is null)
            {
                return (long)value._sign + 1;
            }

            BigInteger result;

            if (value._sign >= 0)
            {
                int size = value._bits.Length + 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Add(value._bits, 1, bits);
                result = new BigInteger(bits, negative: false);
                bitsBuffer.Dispose();
            }
            else
            {
                Span<nuint> bits = RentedBuffer.Create(value._bits.Length, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Subtract(value._bits, 1, bits);
                result = new BigInteger(bits, negative: true);
                bitsBuffer.Dispose();
            }

            return result;
        }

        public static BigInteger operator --(BigInteger value)
        {
            if (value._bits is null)
            {
                return (long)value._sign - 1;
            }

            BigInteger result;

            if (value._sign >= 0)
            {
                Span<nuint> bits = RentedBuffer.Create(value._bits.Length, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Subtract(value._bits, 1, bits);
                result = new BigInteger(bits, negative: false);
                bitsBuffer.Dispose();
            }
            else
            {
                int size = value._bits.Length + 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Add(value._bits, 1, bits);
                result = new BigInteger(bits, negative: true);
                bitsBuffer.Dispose();
            }

            return result;
        }

        public static BigInteger operator +(BigInteger left, BigInteger right)
        {
            if (left._bits is null && right._bits is null)
            {
                return (long)left._sign + right._sign;
            }

            return left._sign < 0 != right._sign < 0
                ? Subtract(left._bits, left._sign, right._bits, -right._sign)
                : Add(left._bits, left._sign, right._bits, right._sign);
        }

        public static BigInteger operator *(BigInteger left, BigInteger right) =>
            left._bits is null && right._bits is null
                ? (BigInteger)((long)left._sign * right._sign)
                : Multiply(left._bits, left._sign, right._bits, right._sign);

        private static BigInteger Multiply(ReadOnlySpan<nuint> left, int leftSign, ReadOnlySpan<nuint> right, int rightSign)
        {
            bool trivialLeft = left.IsEmpty;
            bool trivialRight = right.IsEmpty;

            Debug.Assert(!(trivialLeft && trivialRight), "Trivial cases should be handled on the caller operator");

            BigInteger result;

            if (trivialLeft)
            {
                Debug.Assert(!right.IsEmpty);

                int size = right.Length + 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Multiply(right, NumericsHelpers.Abs(leftSign), bits);
                result = new BigInteger(bits, (leftSign < 0) ^ (rightSign < 0));
                bitsBuffer.Dispose();
            }
            else if (trivialRight)
            {
                Debug.Assert(!left.IsEmpty);

                int size = left.Length + 1;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Multiply(left, NumericsHelpers.Abs(rightSign), bits);
                result = new BigInteger(bits, (leftSign < 0) ^ (rightSign < 0));
                bitsBuffer.Dispose();
            }
            else if (left == right)
            {
                int size = left.Length + right.Length;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Square(left, bits);
                result = new BigInteger(bits, (leftSign < 0) ^ (rightSign < 0));
                bitsBuffer.Dispose();
            }
            else
            {
                Debug.Assert(!left.IsEmpty && !right.IsEmpty);

                int size = left.Length + right.Length;
                Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

                BigIntegerCalculator.Multiply(left, right, bits);
                result = new BigInteger(bits, (leftSign < 0) ^ (rightSign < 0));
                bitsBuffer.Dispose();
            }

            return result;
        }

        public static BigInteger operator /(BigInteger dividend, BigInteger divisor)
        {
            bool trivialDividend = dividend._bits is null;
            bool trivialDivisor = divisor._bits is null;

            if (trivialDividend && trivialDivisor)
            {
                return dividend._sign / divisor._sign;
            }

            if (trivialDividend)
            {
                // The divisor is non-trivial
                // and therefore the bigger one
                return s_zero;
            }

            if (trivialDivisor)
            {
                Debug.Assert(dividend._bits is not null);

                int size = dividend._bits.Length;
                Span<nuint> quotient = RentedBuffer.Create(size, out RentedBuffer quotientBuffer);

                //may throw DivideByZeroException
                BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign), quotient);

                BigInteger result = new BigInteger(quotient, (dividend._sign < 0) ^ (divisor._sign < 0));

                quotientBuffer.Dispose();

                return result;
            }

            Debug.Assert(dividend._bits is not null && divisor._bits is not null);

            if (dividend._bits.Length < divisor._bits.Length)
            {
                return s_zero;
            }
            else
            {
                int size = dividend._bits.Length - divisor._bits.Length + 1;
                Span<nuint> quotient = RentedBuffer.Create(size, out RentedBuffer quotientBuffer);

                BigIntegerCalculator.Divide(dividend._bits, divisor._bits, quotient);
                BigInteger result = new(quotient, (dividend._sign < 0) ^ (divisor._sign < 0));

                quotientBuffer.Dispose();

                return result;
            }
        }

        public static BigInteger operator %(BigInteger dividend, BigInteger divisor)
        {
            bool trivialDividend = dividend._bits is null;
            bool trivialDivisor = divisor._bits is null;

            if (trivialDividend && trivialDivisor)
            {
                return dividend._sign % divisor._sign;
            }

            if (trivialDividend)
            {
                // The divisor is non-trivial
                // and therefore the bigger one
                return dividend;
            }

            if (trivialDivisor)
            {
                Debug.Assert(dividend._bits is not null);
                nuint remainder = BigIntegerCalculator.Remainder(dividend._bits, NumericsHelpers.Abs(divisor._sign));
                return dividend._sign < 0 ? -(long)remainder : (long)remainder;
            }

            Debug.Assert(dividend._bits is not null && divisor._bits is not null);

            if (dividend._bits.Length < divisor._bits.Length)
            {
                return dividend;
            }

            int size = dividend._bits.Length;
            Span<nuint> bits = RentedBuffer.Create(size, out RentedBuffer bitsBuffer);

            BigIntegerCalculator.Remainder(dividend._bits, divisor._bits, bits);
            BigInteger result = new(bits, dividend._sign < 0);

            bitsBuffer.Dispose();

            return result;
        }

        public static bool operator <(BigInteger left, BigInteger right) => left.CompareTo(right) < 0;

        public static bool operator <=(BigInteger left, BigInteger right) => left.CompareTo(right) <= 0;

        public static bool operator >(BigInteger left, BigInteger right) => left.CompareTo(right) > 0;

        public static bool operator >=(BigInteger left, BigInteger right) => left.CompareTo(right) >= 0;

        public static bool operator ==(BigInteger left, BigInteger right) => left.Equals(right);

        public static bool operator !=(BigInteger left, BigInteger right) => !left.Equals(right);

        public static bool operator <(BigInteger left, long right) => left.CompareTo(right) < 0;

        public static bool operator <=(BigInteger left, long right) => left.CompareTo(right) <= 0;

        public static bool operator >(BigInteger left, long right) => left.CompareTo(right) > 0;

        public static bool operator >=(BigInteger left, long right) => left.CompareTo(right) >= 0;

        public static bool operator ==(BigInteger left, long right) => left.Equals(right);

        public static bool operator !=(BigInteger left, long right) => !left.Equals(right);

        public static bool operator <(long left, BigInteger right) => right.CompareTo(left) > 0;

        public static bool operator <=(long left, BigInteger right) => right.CompareTo(left) >= 0;

        public static bool operator >(long left, BigInteger right) => right.CompareTo(left) < 0;

        public static bool operator >=(long left, BigInteger right) => right.CompareTo(left) <= 0;

        public static bool operator ==(long left, BigInteger right) => right.Equals(left);

        public static bool operator !=(long left, BigInteger right) => !right.Equals(left);

        [CLSCompliant(false)]
        public static bool operator <(BigInteger left, ulong right) => left.CompareTo(right) < 0;

        [CLSCompliant(false)]
        public static bool operator <=(BigInteger left, ulong right) => left.CompareTo(right) <= 0;

        [CLSCompliant(false)]
        public static bool operator >(BigInteger left, ulong right) => left.CompareTo(right) > 0;

        [CLSCompliant(false)]
        public static bool operator >=(BigInteger left, ulong right) => left.CompareTo(right) >= 0;

        [CLSCompliant(false)]
        public static bool operator ==(BigInteger left, ulong right) => left.Equals(right);

        [CLSCompliant(false)]
        public static bool operator !=(BigInteger left, ulong right) => !left.Equals(right);

        [CLSCompliant(false)]
        public static bool operator <(ulong left, BigInteger right) => right.CompareTo(left) > 0;

        [CLSCompliant(false)]
        public static bool operator <=(ulong left, BigInteger right) => right.CompareTo(left) >= 0;

        [CLSCompliant(false)]
        public static bool operator >(ulong left, BigInteger right) => right.CompareTo(left) < 0;

        [CLSCompliant(false)]
        public static bool operator >=(ulong left, BigInteger right) => right.CompareTo(left) <= 0;

        [CLSCompliant(false)]
        public static bool operator ==(ulong left, BigInteger right) => right.Equals(left);

        [CLSCompliant(false)]
        public static bool operator !=(ulong left, BigInteger right) => !right.Equals(left);

        /// <summary>
        /// Gets the number of bits required for shortest two's complement representation of the current instance without the sign bit.
        /// </summary>
        /// <returns>The minimum non-negative number of bits in two's complement notation without the sign bit.</returns>
        /// <remarks>This method returns 0 iff the value of current object is equal to <see cref="Zero"/> or <see cref="MinusOne"/>. For positive integers the return value is equal to the ordinary binary representation string length.</remarks>
        public long GetBitLength()
        {
            nuint highValue;
            int bitsArrayLength;
            int sign = _sign;
            nuint[]? bits = _bits;

            if (bits is null)
            {
                bitsArrayLength = 1;
                highValue = (nuint)(sign < 0 ? -sign : sign);
            }
            else
            {
                bitsArrayLength = bits.Length;
                highValue = bits[bitsArrayLength - 1];
            }

            long bitLength = (long)bitsArrayLength * BigIntegerCalculator.BitsPerLimb -
                BitOperations.LeadingZeroCount(highValue);

            if (sign >= 0)
            {
                return bitLength;
            }

            // When negative and IsPowerOfTwo, the answer is (bitLength - 1)

            // Check highValue
            if ((highValue & (highValue - 1)) != 0)
            {
                return bitLength;
            }

            // Check the rest of the bits (if present)
            return bits.AsSpan(0, bitsArrayLength - 1).ContainsAnyExcept(0u) ? bitLength : bitLength - 1;
        }

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            if (_bits is not null)
            {
                // _sign must be +1 or -1 when _bits is non-null
                Debug.Assert(_sign is 1 or -1);
                // _bits must contain at least 1 element or be null
                Debug.Assert(_bits.Length > 0);
                // Wasted space: _bits[0] could have been packed into _sign
                Debug.Assert(_bits.Length > 1 || _bits[0] > int.MaxValue);
                // Wasted space: leading zeros could have been truncated
                Debug.Assert(_bits[^1] != 0);
                // Arrays larger than this can't fit into a Span<byte>
                Debug.Assert(_bits.Length <= MaxLength);
            }
            else
            {
                // int.MinValue should not be stored in the _sign field
                Debug.Assert(_sign > int.MinValue);
            }
        }

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static BigInteger IAdditiveIdentity<BigInteger, BigInteger>.AdditiveIdentity => Zero;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.Log10(TSelf)" />
        static BigInteger IBinaryInteger<BigInteger>.Log10(BigInteger value)
        {
            value.AssertValid();

            if (IsNegative(value))
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            // For small values stored in _sign, use the fast path
            if (value._bits is null)
            {
                return uint.Log10((uint)value._sign);
            }

            // For large values, use Log2-based estimation with single correction.
            // log10(x) = log2(x) * log10(2); we approximate log10(2) as N/2^S.
            // The smaller fixed-width types (uint, ulong, UInt128) use 1233/4096
            // (~4.6e-6 error per bit), which is sufficient for up to ~217K bits.
            // For BigInteger, which has no upper bound on bit count, we use
            // 1292913986/2^32 (~1.1e-10 error per bit), safe up to ~8.7B bits,
            // which covers the full BigInteger range.
            BigInteger log2Value = Log2(value);
            BigInteger approx = ((log2Value + 1) * 1292913986L) >> 32;
            BigInteger power = Pow(10, (int)approx);

            return value < power ? approx - 1 : approx;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (BigInteger Quotient, BigInteger Remainder) DivRem(BigInteger left, BigInteger right)
        {
            BigInteger quotient = DivRem(left, right, out BigInteger remainder);
            return (quotient, remainder);
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static BigInteger LeadingZeroCount(BigInteger value)
        {
            if (value._bits is null)
            {
                // For small values stored in _sign, use 32-bit counting to match the
                // behavior when _bits was uint[] (where each limb was always 32-bit).
                return uint.LeadingZeroCount((uint)value._sign);
            }

            // When negative, two's complement has infinite sign-extension of 1-bits, so LZC is always 0.
            if (value._sign < 0)
            {
                return 0;
            }

            // When positive, count leading zeros in the most significant 32-bit word.
            // The & 31 maps the result to 32-bit word semantics: on 64-bit, when the
            // upper half is zero, LZC is 32 + uint_lzc, and (32 + x) & 31 == x.
            return BitOperations.LeadingZeroCount(value._bits[^1]) & 31;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static BigInteger PopCount(BigInteger value)
        {
            if (value._bits is null)
            {
                return int.PopCount(value._sign);
            }

            ulong result = 0;

            if (value._sign >= 0)
            {
                // When the value is positive, we simply need to do a popcount for all bits

                for (int i = 0; i < value._bits.Length; i++)
                {
                    nuint part = value._bits[i];
                    result += (ulong)BitOperations.PopCount(part);
                }
            }
            else
            {
                // When the value is negative, we need to PopCount the two's complement
                // representation. We'll do this "inline" to avoid needing to unnecessarily allocate.

                int firstNonZero = value._bits.AsSpan().IndexOfAnyExcept((nuint)0);

                int i = firstNonZero;
                nuint part;

                // Negate the first non-zero limb (two's complement start).
                part = ~value._bits[i] + 1;
                result += (ulong)BitOperations.PopCount(part);
                i++;

                while (i < value._bits.Length)
                {
                    // Then process the remaining limbs using ones' complement.
                    part = ~value._bits[i];
                    result += (ulong)BitOperations.PopCount(part);
                    i++;
                }

                // On 64-bit, when the MSL's upper 32 bits are zero, complementing
                // produces 0xFFFFFFFF in those bits, adding 32 phantom 1-bits.
                // Subtract them to maintain 32-bit word semantics.
                if (Environment.Is64BitProcess && (uint)(value._bits[^1] >> BitsPerUInt32) == 0)
                {
                    result -= BitsPerUInt32;
                }
            }

            return result;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static BigInteger RotateLeft(BigInteger value, int rotateAmount)
        {
            if (rotateAmount == 0)
            {
                return value;
            }

            bool neg = value._sign < 0;

            if (value._bits is null)
            {
                uint rs = uint.RotateLeft((uint)value._sign, rotateAmount);
                return neg
                    ? new BigInteger((int)rs)
                    : new BigInteger(rs);
            }

            return Rotate(value._bits, neg, rotateAmount);
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static BigInteger RotateRight(BigInteger value, int rotateAmount)
        {
            if (rotateAmount == 0)
            {
                return value;
            }

            bool neg = value._sign < 0;

            if (value._bits is null)
            {
                uint rs = uint.RotateRight((uint)value._sign, rotateAmount);
                return neg
                    ? new BigInteger((int)rs)
                    : new BigInteger(rs);
            }

            return Rotate(value._bits, neg, -(long)rotateAmount);
        }

        private static BigInteger Rotate(ReadOnlySpan<nuint> bits, bool negative, long rotateLeftAmount)
        {
            Debug.Assert(bits.Length > 0);
            Debug.Assert(Math.Abs(rotateLeftAmount) <= 0x80000000);

            if (!Environment.Is64BitProcess)
            {
                // On 32-bit, nuint and uint are the same width so the standard nuint
                // rotation algorithm (with BitsPerLimb = 32) is directly correct.
                return RotateNuint(bits, negative, rotateLeftAmount);
            }

            // On 64-bit, each nuint limb is 64 bits, but the rotation ring width must
            // be a multiple of 32 bits for platform-independent results. The last limb
            // may hold only one significant 32-bit word (upper 32 bits zero).

            // Count effective 32-bit words.
            int wordCount = bits.Length * 2;
            bool halfLimb = (uint)(bits[^1] >> BitsPerUInt32) == 0;
            if (halfLimb) wordCount--;

            // Determine if sign extension adds a 32-bit word.
            int zWordCount = wordCount;
            int firstNonZeroLimb = negative ? bits.IndexOfAnyExcept((nuint)0) : 0;

            if (negative)
            {
                // The MSW's sign bit indicates whether two's complement needs an extra word.
                bool mswSignBitSet = halfLimb
                    ? (int)(uint)bits[^1] < 0               // bit 31 of lower half
                    : (nint)bits[^1] < 0;                    // bit 63 (= MSW bit 31)

                if (mswSignBitSet)
                {
                    // Sign extension needed unless value is exactly -2^(wordCount*32-1).
                    bool isMinValue = halfLimb
                        ? ((uint)bits[^1] == UInt32HighBit && firstNonZeroLimb == bits.Length - 1)
                        : (bits[^1] == ((nuint)UInt32HighBit << BitsPerUInt32) && firstNonZeroLimb == bits.Length - 1);

                    if (!isMinValue)
                        ++zWordCount;
                }
            }

            // Allocate result buffer sized for zWordCount 32-bit words.
            int zLimbCount = (zWordCount + 1) / 2;
            bool resultHalfLimb = (zWordCount & 1) != 0;

            Span<nuint> zd = RentedBuffer.Create(zLimbCount, out RentedBuffer zdBuffer);
            zd.Slice(bits.Length).Clear();
            bits.CopyTo(zd);

            // Two's complement conversion at nuint level.
            if (negative)
            {
                NumericsHelpers.DangerousMakeTwosComplement(zd);

                if (resultHalfLimb)
                {
                    // Complementing the zero padding in the upper 32 of the last limb
                    // produces phantom 0xFFFFFFFF; clear it.
                    zd[^1] = (nuint)(uint)zd[^1];
                }
            }

            // Decompose the rotation amount at 32-bit word granularity.
            int digitShift32 = (int)(0x80000000 / BitsPerUInt32);
            int smallShift32 = 0;
            bool rotateRight;

            if (rotateLeftAmount < 0)
            {
                rotateRight = true;
                if (rotateLeftAmount != -0x80000000)
                    (digitShift32, smallShift32) = Math.DivRem(-(int)rotateLeftAmount, BitsPerUInt32);
            }
            else
            {
                rotateRight = false;
                if (rotateLeftAmount != 0x80000000)
                    (digitShift32, smallShift32) = Math.DivRem((int)rotateLeftAmount, BitsPerUInt32);
            }

            // Perform the rotation.
            if (!resultHalfLimb)
            {
                // Even word count: the ring fills all nuint limbs completely.
                // An odd 32-bit digit shift is absorbed into the nuint small shift (0..63).
                int nuintSmallShift = (digitShift32 & 1) * BitsPerUInt32 + smallShift32;
                int nuintDigitShift = digitShift32 >> 1;

                if (rotateRight)
                {
                    BigIntegerCalculator.RightShiftSelf(zd, nuintSmallShift, out nuint carry);
                    zd[^1] |= carry;

                    nuintDigitShift %= zd.Length;
                    if (nuintDigitShift != 0)
                        BigIntegerCalculator.SwapUpperAndLower(zd, nuintDigitShift);
                }
                else
                {
                    BigIntegerCalculator.LeftShiftSelf(zd, nuintSmallShift, out nuint carry);
                    zd[0] |= carry;

                    nuintDigitShift %= zd.Length;
                    if (nuintDigitShift != 0)
                        BigIntegerCalculator.SwapUpperAndLower(zd, zd.Length - nuintDigitShift);
                }
            }
            else
            {
                // Odd word count: the last limb's upper 32 bits are not part of the ring.
                // The SIMD-accelerated nuint shift handles the bit-level rotation; a carry
                // fixup accounts for the half-used last limb. The digit swap operates at
                // uint granularity via MemoryMarshal.Cast because the swap boundary may
                // fall mid-nuint.
                if (rotateRight)
                {
                    if (smallShift32 != 0)
                    {
                        BigIntegerCalculator.RightShiftSelf(zd, smallShift32, out nuint carry);
                        // The nuint carry is at bit positions (64-shift)..63.
                        // For the half-limb ring, it wraps to bit (32-shift)..31 of the last word.
                        zd[^1] |= carry >> BitsPerUInt32;
                    }

                    int effectiveDigitShift = digitShift32 % zWordCount;
                    if (effectiveDigitShift != 0)
                    {
                        if (!BitConverter.IsLittleEndian)
                            SwapHalvesWithinLimbs(zd);

                        Span<uint> words = MemoryMarshal.Cast<nuint, uint>(zd).Slice(0, zWordCount);
                        BigIntegerCalculator.SwapUpperAndLower(words, effectiveDigitShift);

                        if (!BitConverter.IsLittleEndian)
                            SwapHalvesWithinLimbs(zd);
                    }
                }
                else
                {
                    if (smallShift32 != 0)
                    {
                        BigIntegerCalculator.LeftShiftSelf(zd, smallShift32, out _);
                        // Bits that overflowed into the upper 32 of the last limb should wrap.
                        // The nuint carry is 0 since the upper 32 were zero and shift < 32.
                        nuint overflow = zd[^1] >> BitsPerUInt32;
                        zd[^1] = (nuint)(uint)zd[^1];
                        zd[0] |= overflow;
                    }

                    int effectiveDigitShift = digitShift32 % zWordCount;
                    if (effectiveDigitShift != 0)
                    {
                        if (!BitConverter.IsLittleEndian)
                            SwapHalvesWithinLimbs(zd);

                        Span<uint> words = MemoryMarshal.Cast<nuint, uint>(zd).Slice(0, zWordCount);
                        BigIntegerCalculator.SwapUpperAndLower(words, zWordCount - effectiveDigitShift);

                        if (!BitConverter.IsLittleEndian)
                            SwapHalvesWithinLimbs(zd);
                    }
                }
            }

            // Check sign bit and convert back from two's complement if needed.
            bool resultNeg = resultHalfLimb
                ? negative && (int)(uint)zd[^1] < 0
                : negative && (nint)zd[^1] < 0;

            if (resultNeg)
            {
                NumericsHelpers.DangerousMakeTwosComplement(zd);

                if (resultHalfLimb)
                {
                    // Clear phantom bits from complementing the zero padding.
                    zd[^1] = (nuint)(uint)zd[^1];
                }
            }
            else
            {
                negative = false;
            }

            BigInteger result = new(zd, negative);

            zdBuffer.Dispose();

            return result;
        }

        /// <summary>
        /// Swaps the upper and lower 32-bit halves within each nuint limb.
        /// Used on big-endian 64-bit before/after MemoryMarshal.Cast&lt;nuint, uint&gt;
        /// to ensure correct 32-bit word ordering.
        /// </summary>
        private static void SwapHalvesWithinLimbs(Span<nuint> limbs)
        {
            for (int i = 0; i < limbs.Length; i++)
            {
                nuint v = limbs[i];
                limbs[i] = ((v & 0xFFFFFFFF) << BitsPerUInt32) | (v >> BitsPerUInt32);
            }
        }

        /// <summary>
        /// Rotation using the standard nuint algorithm. Only correct on 32-bit where
        /// nuint and uint have the same width (BitsPerLimb = 32).
        /// </summary>
        private static BigInteger RotateNuint(ReadOnlySpan<nuint> bits, bool negative, long rotateLeftAmount)
        {
            Debug.Assert(!Environment.Is64BitProcess);

            int zLength = bits.Length;
            int leadingZeroCount = negative ? bits.IndexOfAnyExcept((nuint)0) : 0;

            if (negative && (nint)bits[^1] < 0
                && (leadingZeroCount != bits.Length - 1 || bits[^1] != ((nuint)1 << (BigIntegerCalculator.BitsPerLimb - 1))))
            {
                ++zLength;
            }

            Span<nuint> zd = RentedBuffer.Create(zLength, out RentedBuffer zdBuffer);

            zd[^1] = 0;
            bits.CopyTo(zd);

            if (negative)
            {
                Debug.Assert((uint)leadingZeroCount < (uint)zd.Length);

                zd[leadingZeroCount] = (nuint)(-(nint)zd[leadingZeroCount]);
                NumericsHelpers.DangerousMakeOnesComplement(zd.Slice(leadingZeroCount + 1));
            }

            BigIntegerCalculator.RotateLeft(zd, rotateLeftAmount);

            if (negative && (nint)zd[^1] < 0)
            {
                NumericsHelpers.DangerousMakeTwosComplement(zd);
            }
            else
            {
                negative = false;
            }

            BigInteger result = new(zd, negative);

            zdBuffer.Dispose();

            return result;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static BigInteger TrailingZeroCount(BigInteger value)
        {
            if (value._bits is null)
            {
                return int.TrailingZeroCount(value._sign);
            }

            ulong result = 0;

            // Both positive values and their two's-complement negative representation will share the same TrailingZeroCount,
            // so the sign of value does not matter and both cases can be handled in the same way

            nuint part = value._bits[0];

            for (int i = 1; (part == 0) && (i < value._bits.Length); i++)
            {
                part = value._bits[i];
                result += (uint)BigIntegerCalculator.BitsPerLimb;
            }

            result += (ulong)BitOperations.TrailingZeroCount(part);

            return result;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadBigEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<BigInteger>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BigInteger value)
        {
            value = new BigInteger(source, isUnsigned, isBigEndian: true);
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadLittleEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<BigInteger>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BigInteger value)
        {
            value = new BigInteger(source, isUnsigned, isBigEndian: false);
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        int IBinaryInteger<BigInteger>.GetShortestBitLength()
        {
            nuint[]? bits = _bits;

            if (bits is null)
            {
                int value = _sign;

                return value >= 0 ? 32 - BitOperations.LeadingZeroCount((uint)value) : 33 - BitOperations.LeadingZeroCount(~(uint)value);
            }

            int result = (bits.Length - 1) * BigIntegerCalculator.BitsPerLimb;

            if (_sign >= 0)
            {
                result += BigIntegerCalculator.BitsPerLimb - BitOperations.LeadingZeroCount(bits[^1]);
            }
            else
            {
                nuint part = ~bits[^1] + 1;

                // We need to remove the "carry" (the +1) if any of the initial
                // bytes are not zero. This ensures we get the correct two's complement
                // part for the computation.

                if (bits.AsSpan(0, bits.Length - 1).ContainsAnyExcept(0u))
                {
                    part -= 1;
                }

                result += BigIntegerCalculator.BitsPerLimb + 1 - BitOperations.LeadingZeroCount(~part);
            }

            return result;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<BigInteger>.GetByteCount() => GetGenericMathByteCount();

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteBigEndian(Span{byte}, out int)" />
        bool IBinaryInteger<BigInteger>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
        {
            nuint[]? bits = _bits;

            int byteCount = GetGenericMathByteCount();

            if (destination.Length >= byteCount)
            {
                if (bits is null)
                {
                    BinaryPrimitives.WriteIntPtrBigEndian(destination, _sign);
                }
                else if (_sign >= 0)
                {
                    // When the value is positive, we simply need to copy all bits as big endian

                    Span<byte> dest = destination;

                    for (int i = bits.Length - 1; i >= 0; i--)
                    {
                        BinaryPrimitives.WriteUIntPtrBigEndian(dest, bits[i]);
                        dest = dest.Slice(nint.Size);
                    }
                }
                else
                {
                    // When the value is negative, we need to copy the two's complement representation
                    // We'll do this "inline" to avoid needing to unnecessarily allocate.

                    bool needsSignExtension = byteCount > bits.Length * nint.Size;
                    Span<byte> dest = destination;

                    if (needsSignExtension)
                    {
                        // We need one extra part to represent the sign as the most
                        // significant bit of the two's complement value was 0.
                        BinaryPrimitives.WriteUIntPtrBigEndian(dest, nuint.MaxValue);
                        dest = dest.Slice(nint.Size);
                    }

                    // Find first non-zero limb to determine carry boundary
                    int firstNonZero = ((ReadOnlySpan<nuint>)bits).IndexOfAnyExcept(0u);
                    Debug.Assert(firstNonZero >= 0);

                    // Write from highest limb to lowest (forward in big-endian dest)
                    for (int i = bits.Length - 1; i >= 0; i--)
                    {
                        nuint part;

                        if (i > firstNonZero)
                        {
                            part = ~bits[i];
                        }
                        else if (i == firstNonZero)
                        {
                            part = ~bits[i] + 1;
                        }
                        else
                        {
                            part = 0;
                        }

                        BinaryPrimitives.WriteUIntPtrBigEndian(dest, part);
                        dest = dest.Slice(nint.Size);
                    }
                }

                bytesWritten = byteCount;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<BigInteger>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            nuint[]? bits = _bits;

            int byteCount = GetGenericMathByteCount();

            if (destination.Length >= byteCount)
            {
                if (bits is null)
                {
                    BinaryPrimitives.WriteIntPtrLittleEndian(destination, _sign);
                }
                else if (_sign >= 0)
                {
                    // When the value is positive, we simply need to copy all bits as little endian

                    Span<byte> dest = destination;

                    for (int i = 0; i < bits.Length; i++)
                    {
                        BinaryPrimitives.WriteUIntPtrLittleEndian(dest, bits[i]);
                        dest = dest.Slice(nint.Size);
                    }
                }
                else
                {
                    // When the value is negative, we need to copy the two's complement representation
                    // We'll do this "inline" to avoid needing to unnecessarily allocate.

                    bool needsSignExtension = byteCount > bits.Length * nint.Size;
                    Span<byte> dest = destination;

                    // Find first non-zero limb to determine carry boundary
                    int firstNonZero = ((ReadOnlySpan<nuint>)bits).IndexOfAnyExcept(0u);
                    Debug.Assert(firstNonZero >= 0);

                    for (int i = 0; i < bits.Length; i++)
                    {
                        nuint part;

                        if (i < firstNonZero)
                        {
                            part = 0;
                        }
                        else if (i == firstNonZero)
                        {
                            part = ~bits[i] + 1;
                        }
                        else
                        {
                            part = ~bits[i];
                        }

                        BinaryPrimitives.WriteUIntPtrLittleEndian(dest, part);
                        dest = dest.Slice(nint.Size);
                    }

                    if (needsSignExtension)
                    {
                        // We need one extra part to represent the sign as the most
                        // significant bit of the two's complement value was 0.
                        BinaryPrimitives.WriteUIntPtrLittleEndian(dest, nuint.MaxValue);
                    }
                }

                bytesWritten = byteCount;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        private int GetGenericMathByteCount()
        {
            nuint[]? bits = _bits;

            if (bits is null)
            {
                return nint.Size;
            }

            int result = bits.Length * nint.Size;

            if (_sign < 0)
            {
                nuint part = ~bits[^1] + 1;

                // We need to remove the "carry" (the +1) if any of the initial
                // bytes are not zero. This ensures we get the correct two's complement
                // part for the computation.

                if (bits.AsSpan(0, bits.Length - 1).ContainsAnyExcept(0u))
                {
                    part -= 1;
                }

                if ((nint)part >= 0)
                {
                    // When the most significant bit of the part is zero
                    // we need another part to represent the value.
                    result += nint.Size;
                }
            }

            return result;
        }

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.AllBitsSet" />
        static BigInteger IBinaryNumber<BigInteger>.AllBitsSet => MinusOne;

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(BigInteger value) => value.IsPowerOfTwo;

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static BigInteger Log2(BigInteger value)
        {
            if (IsNegative(value))
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            return value._bits is null
                ? (BigInteger)((BigIntegerCalculator.BitsPerLimb - 1) ^ BitOperations.LeadingZeroCount((nuint)value._sign | 1))
                : (BigInteger)(((long)value._bits.Length * BigIntegerCalculator.BitsPerLimb - 1) ^ BitOperations.LeadingZeroCount(value._bits[^1]));
        }

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static BigInteger IMultiplicativeIdentity<BigInteger, BigInteger>.MultiplicativeIdentity => One;

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static BigInteger Clamp(BigInteger value, BigInteger min, BigInteger max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;

            [DoesNotReturn]
            static void ThrowMinMaxException<T>(T min, T max)
            {
                throw new ArgumentException(SR.Format(SR.Argument_MinMaxValue, min, max));
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static BigInteger CopySign(BigInteger value, BigInteger sign)
        {
            nint currentSign = value._sign;

            if (value._bits is null)
            {
                currentSign = (currentSign >= 0) ? 1 : -1;
            }

            nint targetSign = sign._sign;

            if (sign._bits is null)
            {
                targetSign = (targetSign >= 0) ? 1 : -1;
            }

            return (currentSign == targetSign) ? value : -value;
        }

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        static BigInteger INumber<BigInteger>.MaxNumber(BigInteger x, BigInteger y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        static BigInteger INumber<BigInteger>.MinNumber(BigInteger x, BigInteger y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        static int INumber<BigInteger>.Sign(BigInteger value)
        {
            return value._bits is null
                ? value._sign > 0 ? 1
                : (value._sign < 0 ? -1 : 0) : value._sign;
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<BigInteger>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BigInteger CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            BigInteger result;

            if (typeof(TOther) == typeof(BigInteger))
            {
                result = (BigInteger)(object)value;
            }
            else if (!TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BigInteger CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            BigInteger result;

            if (typeof(TOther) == typeof(BigInteger))
            {
                result = (BigInteger)(object)value;
            }
            else if (!TryConvertFromSaturating(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BigInteger CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            BigInteger result;

            if (typeof(TOther) == typeof(BigInteger))
            {
                result = (BigInteger)(object)value;
            }
            else if (!TryConvertFromTruncating(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<BigInteger>.IsCanonical(BigInteger value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<BigInteger>.IsComplexNumber(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(BigInteger value)
        {
            return value._bits is null
                ? (value._sign & 1) == 0
                : (value._bits[0] & 1) == 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsFinite(TSelf)" />
        static bool INumberBase<BigInteger>.IsFinite(BigInteger value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<BigInteger>.IsImaginaryNumber(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInfinity(TSelf)" />
        static bool INumberBase<BigInteger>.IsInfinity(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        static bool INumberBase<BigInteger>.IsInteger(BigInteger value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNaN(TSelf)" />
        static bool INumberBase<BigInteger>.IsNaN(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(BigInteger value)
        {
            return value._sign < 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        static bool INumberBase<BigInteger>.IsNegativeInfinity(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        static bool INumberBase<BigInteger>.IsNormal(BigInteger value) => (value != 0);

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(BigInteger value)
        {
            return value._bits is null
                ? (value._sign & 1) != 0
                : (value._bits[0] & 1) != 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(BigInteger value)
        {
            return value._sign >= 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        static bool INumberBase<BigInteger>.IsPositiveInfinity(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        static bool INumberBase<BigInteger>.IsRealNumber(BigInteger value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        static bool INumberBase<BigInteger>.IsSubnormal(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<BigInteger>.IsZero(BigInteger value)
        {
            return value._sign == 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static BigInteger MaxMagnitude(BigInteger x, BigInteger y)
        {
            int compareResult = Abs(x).CompareTo(Abs(y));
            return compareResult > 0 || (compareResult == 0 && IsPositive(x)) ? x : y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static BigInteger INumberBase<BigInteger>.MaxMagnitudeNumber(BigInteger x, BigInteger y) => MaxMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static BigInteger MinMagnitude(BigInteger x, BigInteger y)
        {
            int compareResult = Abs(x).CompareTo(Abs(y));
            return compareResult < 0 || (compareResult == 0 && IsNegative(x)) ? x : y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        static BigInteger INumberBase<BigInteger>.MinMagnitudeNumber(BigInteger x, BigInteger y) => MinMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MultiplyAddEstimate(TSelf, TSelf, TSelf)" />
        static BigInteger INumberBase<BigInteger>.MultiplyAddEstimate(BigInteger left, BigInteger right, BigInteger addend) => (left * right) + addend;

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertFromChecked<TOther>(TOther value, out BigInteger result) => TryConvertFromChecked(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromChecked<TOther>(TOther value, out BigInteger result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = checked((BigInteger)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = checked((BigInteger)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualValue = (BFloat16)(object)value;
                result = checked((BigInteger)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = checked((BigInteger)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertFromSaturating<TOther>(TOther value, out BigInteger result) => TryConvertFromSaturating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromSaturating<TOther>(TOther value, out BigInteger result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = double.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = Half.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualValue = (BFloat16)(object)value;
                result = BFloat16.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = float.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertFromTruncating<TOther>(TOther value, out BigInteger result) => TryConvertFromTruncating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromTruncating<TOther>(TOther value, out BigInteger result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = double.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = Half.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualValue = (BFloat16)(object)value;
                result = BFloat16.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = float.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertToChecked<TOther>(BigInteger value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = checked((byte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = checked((char)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = checked((decimal)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = (double)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualResult = (BFloat16)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = checked((short)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = checked((int)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = checked((long)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = checked((Int128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = checked((nint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Complex))
            {
                Complex actualResult = (Complex)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = checked((sbyte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = checked((float)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = checked((ushort)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = checked((uint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = checked((ulong)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = checked((UInt128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = checked((nuint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertToSaturating<TOther>(BigInteger value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? byte.MinValue : byte.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= byte.MaxValue) ? byte.MaxValue :
                                   (value._sign <= byte.MinValue) ? byte.MinValue : (byte)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? char.MinValue : char.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= char.MaxValue) ? char.MaxValue :
                                   (value._sign <= char.MinValue) ? char.MinValue : (char)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value >= new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)) ? decimal.MaxValue :
                                       (value <= new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001)) ? decimal.MinValue : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = (double)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualResult = (BFloat16)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? short.MinValue : short.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= short.MaxValue) ? short.MaxValue :
                                   (value._sign <= short.MinValue) ? short.MinValue : (short)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? int.MinValue : int.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= int.MaxValue) ? int.MaxValue :
                                   (value._sign <= int.MinValue) ? int.MinValue : value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = (value >= long.MaxValue) ? long.MaxValue :
                                    (value <= long.MinValue) ? long.MinValue : (long)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = (value >= Int128.MaxValue) ? Int128.MaxValue :
                                      (value <= Int128.MinValue) ? Int128.MinValue : (Int128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = (value >= nint.MaxValue) ? nint.MaxValue :
                                    (value <= nint.MinValue) ? nint.MinValue : (nint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Complex))
            {
                Complex actualResult = (Complex)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? sbyte.MinValue : sbyte.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= sbyte.MaxValue) ? sbyte.MaxValue :
                                   (value._sign <= sbyte.MinValue) ? sbyte.MinValue : (sbyte)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? ushort.MinValue : ushort.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= ushort.MaxValue) ? ushort.MaxValue :
                                   (value._sign <= ushort.MinValue) ? ushort.MinValue : (ushort)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value >= uint.MaxValue) ? uint.MaxValue :
                                    IsNegative(value) ? uint.MinValue : (uint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (value >= ulong.MaxValue) ? ulong.MaxValue :
                                     IsNegative(value) ? ulong.MinValue : (ulong)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value >= UInt128.MaxValue) ? UInt128.MaxValue :
                                       IsNegative(value) ? UInt128.MinValue : (UInt128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = (value >= nuint.MaxValue) ? nuint.MaxValue :
                                     IsNegative(value) ? nuint.MinValue : (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertToTruncating<TOther>(BigInteger value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult;

                if (value._bits is not null)
                {
                    nuint bits = value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (byte)bits;
                }
                else
                {
                    actualResult = (byte)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult;

                if (value._bits is not null)
                {
                    nuint bits = value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (char)bits;
                }
                else
                {
                    actualResult = (char)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value >= new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)) ? decimal.MaxValue :
                                       (value <= new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001)) ? decimal.MinValue : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = (double)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualResult = (BFloat16)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? (short)(~value._bits[0] + 1) : (short)value._bits[0];
                }
                else
                {
                    actualResult = (short)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? (int)(~value._bits[0] + 1) : (int)value._bits[0];
                }
                else
                {
                    actualResult = value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult;

                if (value._bits is not null)
                {
                    ulong bits;

                    if (nint.Size == 8)
                    {
                        bits = value._bits[0];
                    }
                    else
                    {
                        bits = 0;

                        if (value._bits.Length >= 2)
                        {
                            bits = value._bits[1];
                            bits <<= 32;
                        }

                        bits |= value._bits[0];
                    }

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (long)bits;
                }
                else
                {
                    actualResult = value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult;

                if (value._bits is not null)
                {
                    ulong lowerBits = 0;
                    ulong upperBits = 0;

                    if (nint.Size == 8)
                    {
                        lowerBits = value._bits[0];

                        if (value._bits.Length >= 2)
                        {
                            upperBits = value._bits[1];
                        }
                    }
                    else
                    {
                        if (value._bits.Length >= 4)
                        {
                            upperBits = value._bits[3];
                            upperBits <<= 32;
                        }

                        if (value._bits.Length >= 3)
                        {
                            upperBits |= value._bits[2];
                        }

                        if (value._bits.Length >= 2)
                        {
                            lowerBits = value._bits[1];
                            lowerBits <<= 32;
                        }

                        lowerBits |= value._bits[0];
                    }

                    UInt128 bits = new(upperBits, lowerBits);

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (Int128)bits;
                }
                else
                {
                    actualResult = (Int128)(long)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult;

                if (value._bits is not null)
                {
                    nuint bits = value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (nint)bits;
                }
                else
                {
                    actualResult = value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Complex))
            {
                Complex actualResult = (Complex)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? (sbyte)(~value._bits[0] + 1) : (sbyte)value._bits[0];
                }
                else
                {
                    actualResult = (sbyte)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult;

                if (value._bits is not null)
                {
                    nuint bits = value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (ushort)bits;
                }
                else
                {
                    actualResult = (ushort)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult;

                if (value._bits is not null)
                {
                    uint bits = (uint)value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = bits;
                }
                else
                {
                    actualResult = (uint)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult;

                if (value._bits is not null)
                {
                    ulong bits;

                    if (nint.Size == 8)
                    {
                        bits = value._bits[0];
                    }
                    else
                    {
                        bits = 0;

                        if (value._bits.Length >= 2)
                        {
                            bits = value._bits[1];
                            bits <<= 32;
                        }

                        bits |= value._bits[0];
                    }

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = bits;
                }
                else
                {
                    actualResult = (ulong)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult;

                if (value._bits is not null)
                {
                    ulong lowerBits = 0;
                    ulong upperBits = 0;

                    if (nint.Size == 8)
                    {
                        lowerBits = value._bits[0];

                        if (value._bits.Length >= 2)
                        {
                            upperBits = value._bits[1];
                        }
                    }
                    else
                    {
                        if (value._bits.Length >= 4)
                        {
                            upperBits = value._bits[3];
                            upperBits <<= 32;
                        }

                        if (value._bits.Length >= 3)
                        {
                            upperBits |= value._bits[2];
                        }

                        if (value._bits.Length >= 2)
                        {
                            lowerBits = value._bits[1];
                            lowerBits <<= 32;
                        }

                        lowerBits |= value._bits[0];
                    }

                    UInt128 bits = new(upperBits, lowerBits);

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = bits;
                }
                else
                {
                    actualResult = (UInt128)(Int128)(long)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult;

                if (value._bits is not null)
                {
                    nuint bits = value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = bits;
                }
                else
                {
                    actualResult = (nuint)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out BigInteger result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_UnsignedRightShift(TSelf, TOther)" />
        public static BigInteger operator >>>(BigInteger value, int shiftAmount)
        {
            if (shiftAmount == 0)
            {
                return value;
            }

            if (shiftAmount == int.MinValue)
            {
                return value << BitsPerUInt32 << MinIntSplitShift;
            }

            if (shiftAmount < 0)
            {
                return value << -shiftAmount;
            }

            (int digitShift, int smallShift) = Math.DivRem(shiftAmount, BigIntegerCalculator.BitsPerLimb);

            if (value._bits is null)
            {
                if (digitShift != 0)
                {
                    goto Excess;
                }

                // Sign-extend _sign from int to nint before unsigned right shift
                // to preserve nint-width semantics consistent with nuint-limb storage.
                return new BigInteger((nint)value._sign >>> smallShift);
            }

            ReadOnlySpan<nuint> bits = value._bits;

            Debug.Assert(bits.Length > 0);

            int zLength = bits.Length - digitShift;

            if (zLength < 0)
            {
                goto Excess;
            }

            bool neg = value._sign < 0;
            int negLeadingZeroCount = neg ? bits.IndexOfAnyExcept(0u) : 0;
            Debug.Assert(negLeadingZeroCount >= 0);

            if (neg && (nint)bits[^1] < 0)
            {
                // For a shift of N x BitsPerLimb bit,
                // We check for a special case where its sign bit could be outside the nuint array after 2's complement conversion.
                // For example given [nuint.MaxValue, nuint.MaxValue, nuint.MaxValue], its 2's complement is [0x01, 0x00, 0x00]
                // After a BitsPerLimb bit right shift, it becomes [0x00, 0x00] which is [0x00, 0x00] when converted back.
                // The expected result is [0x00, 0x00, nuint.MaxValue] (2's complement) or [0x00, 0x00, 0x01] when converted back
                // If the 2's component's last element is a 0, we will track the sign externally
                ++zLength;

                nuint signBit = (nuint)1 << (BigIntegerCalculator.BitsPerLimb - 1);
                if (bits[^1] == signBit && negLeadingZeroCount == bits.Length - 1)
                {
                    // When bits are [0, ..., 0, signBit], special handling is required.
                    // Since the bit length remains unchanged in two's complement, the result must be computed directly.
                    --zLength;
                    if (zLength <= 0)
                    {
                        return s_minusOne;
                    }

                    if (zLength == 1)
                    {
                        return new BigInteger(nint.MinValue >>> smallShift);
                    }

                    nuint[] rgu = new nuint[zLength];
                    rgu[^1] = signBit >>> smallShift;
                    return new BigInteger(smallShift == 0 ? -1 : +1, rgu);
                }
            }
            else if (zLength <= 0)
            {
                goto Excess;
            }

            Span<nuint> zd = RentedBuffer.Create(zLength, out RentedBuffer zdBuffer);

            zd[^1] = 0;
            bits.Slice(digitShift).CopyTo(zd);

            if (neg)
            {
                // Calculate the two's complement. The least significant nonzero bit has already been computed.
                negLeadingZeroCount -= digitShift;

                if ((uint)negLeadingZeroCount < (uint)zd.Length) // is equivalent to negLeadingZeroCount >= 0 && negLeadingZeroCount < zd.Length
                {
                    // negLeadingZeroCount >= zd.Length should never be true, so this can be rewritten
                    // as the case where the least significant nonzero bit is included in zd.
                    zd[negLeadingZeroCount] = (nuint)(-(nint)zd[negLeadingZeroCount]);
                    NumericsHelpers.DangerousMakeOnesComplement(zd.Slice(negLeadingZeroCount + 1));
                }
                else
                {
                    // When the least significant nonzero bit is located below zd.
                    NumericsHelpers.DangerousMakeOnesComplement(zd);
                }
            }

            BigIntegerCalculator.RightShiftSelf(zd, smallShift, out _);
            zd = zd.TrimEnd((nuint)0);

            BigInteger result;
            if (zd.IsEmpty)
            {
                result = neg ? s_minusOne : default;
            }
            else if (neg && (nint)zd[^1] < 0)
            {
                NumericsHelpers.DangerousMakeTwosComplement(zd);
                result = new BigInteger(zd, true);
            }
            else
            {
                result = new BigInteger(zd, false);
            }

            zdBuffer.Dispose();

            return result;
        Excess:
            // Return -1 if the value is negative; otherwise, return 0.
            return new BigInteger(value._sign >> 31, null);
        }

        //
        // ISignedNumber

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static BigInteger ISignedNumber<BigInteger>.NegativeOne => MinusOne;

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static BigInteger Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BigInteger result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IUtf8SpanParsable
        //

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static BigInteger Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Integer, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out BigInteger result) => TryParse(utf8Text, NumberStyles.Integer, provider, out result);
    }
}
