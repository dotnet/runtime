// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Versioning;

namespace System
{
    // Represents a Globally Unique Identifier.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NonVersionable] // This only applies to field layout
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly partial struct Guid
        : ISpanFormattable,
          IComparable,
          IComparable<Guid>,
          IEquatable<Guid>,
          ISpanParsable<Guid>,
          IUtf8SpanFormattable
    {
        public static readonly Guid Empty;

        private readonly int _a;   // Do not rename (binary serialization)
        private readonly short _b; // Do not rename (binary serialization)
        private readonly short _c; // Do not rename (binary serialization)
        private readonly byte _d;  // Do not rename (binary serialization)
        private readonly byte _e;  // Do not rename (binary serialization)
        private readonly byte _f;  // Do not rename (binary serialization)
        private readonly byte _g;  // Do not rename (binary serialization)
        private readonly byte _h;  // Do not rename (binary serialization)
        private readonly byte _i;  // Do not rename (binary serialization)
        private readonly byte _j;  // Do not rename (binary serialization)
        private readonly byte _k;  // Do not rename (binary serialization)

        // Creates a new guid from an array of bytes.
        public Guid(byte[] b) :
            this(new ReadOnlySpan<byte>(b ?? throw new ArgumentNullException(nameof(b))))
        {
        }

        // Creates a new guid from a read-only span.
        public Guid(ReadOnlySpan<byte> b)
        {
            if (b.Length != 16)
            {
                ThrowArgumentException();
            }

            if (BitConverter.IsLittleEndian)
            {
                this = MemoryMarshal.Read<Guid>(b);
                return;
            }

            // slower path for BigEndian:
            _k = b[15];  // hoist bounds checks
            _a = BinaryPrimitives.ReadInt32LittleEndian(b);
            _b = BinaryPrimitives.ReadInt16LittleEndian(b.Slice(4));
            _c = BinaryPrimitives.ReadInt16LittleEndian(b.Slice(6));
            _d = b[8];
            _e = b[9];
            _f = b[10];
            _g = b[11];
            _h = b[12];
            _i = b[13];
            _j = b[14];

            [StackTraceHidden]
            static void ThrowArgumentException()
            {
                throw new ArgumentException(SR.Format(SR.Arg_GuidArrayCtor, "16"), nameof(b));
            }
        }

        [CLSCompliant(false)]
        public Guid(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
        {
            _a = (int)a;
            _b = (short)b;
            _c = (short)c;
            _d = d;
            _e = e;
            _f = f;
            _g = g;
            _h = h;
            _i = i;
            _j = j;
            _k = k;
        }

        // Creates a new GUID initialized to the value represented by the arguments.
        public Guid(int a, short b, short c, byte[] d)
        {
            ArgumentNullException.ThrowIfNull(d);

            if (d.Length != 8)
            {
                throw new ArgumentException(SR.Format(SR.Arg_GuidArrayCtor, "8"), nameof(d));
            }

            _a = a;
            _b = b;
            _c = c;
            _k = d[7]; // hoist bounds checks
            _d = d[0];
            _e = d[1];
            _f = d[2];
            _g = d[3];
            _h = d[4];
            _i = d[5];
            _j = d[6];
        }

        // Creates a new GUID initialized to the value represented by the
        // arguments.  The bytes are specified like this to avoid endianness issues.
        public Guid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
            _e = e;
            _f = f;
            _g = g;
            _h = h;
            _i = i;
            _j = j;
            _k = k;
        }

        private enum GuidParseThrowStyle : byte
        {
            None = 0,
            All = 1,
            AllButOverflow = 2
        }

        private enum ParseFailure
        {
            Format_ExtraJunkAtEnd,
            Format_GuidBraceAfterLastNumber,
            Format_GuidBrace,
            Format_GuidComma,
            Format_GuidDashes,
            Format_GuidEndBrace,
            Format_GuidHexPrefix,
            Format_GuidInvalidChar,
            Format_GuidInvLen,
            Format_GuidUnrecognized,
            Overflow_Byte,
            Overflow_UInt32,
        }

        // This will store the result of the parsing. And it will eventually be used to construct a Guid instance.
        // We'll eventually reinterpret_cast<> a GuidResult as a Guid, so we need to give it a sequential
        // layout and ensure that its early fields match the layout of Guid exactly.
        [StructLayout(LayoutKind.Explicit)]
        private struct GuidResult
        {
            [FieldOffset(0)]
            internal uint _a;
            [FieldOffset(4)]
            internal uint _bc;
            [FieldOffset(4)]
            internal ushort _b;
            [FieldOffset(6)]
            internal ushort _c;
            [FieldOffset(8)]
            internal uint _defg;
            [FieldOffset(8)]
            internal ushort _de;
            [FieldOffset(8)]
            internal byte _d;
            [FieldOffset(10)]
            internal ushort _fg;
            [FieldOffset(12)]
            internal uint _hijk;

            [FieldOffset(16)]
            private readonly GuidParseThrowStyle _throwStyle;

            internal GuidResult(GuidParseThrowStyle canThrow) : this()
            {
                _throwStyle = canThrow;
            }

            internal readonly void SetFailure(ParseFailure failureKind)
            {
                if (_throwStyle == GuidParseThrowStyle.None)
                {
                    return;
                }

                if (failureKind == ParseFailure.Overflow_UInt32 && _throwStyle == GuidParseThrowStyle.All)
                {
                    throw new OverflowException(SR.Overflow_UInt32);
                }

                throw new FormatException(failureKind switch
                {
                    ParseFailure.Format_ExtraJunkAtEnd => SR.Format_ExtraJunkAtEnd,
                    ParseFailure.Format_GuidBraceAfterLastNumber => SR.Format_GuidBraceAfterLastNumber,
                    ParseFailure.Format_GuidBrace => SR.Format_GuidBrace,
                    ParseFailure.Format_GuidComma => SR.Format_GuidComma,
                    ParseFailure.Format_GuidDashes => SR.Format_GuidDashes,
                    ParseFailure.Format_GuidEndBrace=> SR.Format_GuidEndBrace,
                    ParseFailure.Format_GuidHexPrefix => SR.Format_GuidHexPrefix,
                    ParseFailure.Format_GuidInvalidChar => SR.Format_GuidInvalidChar,
                    ParseFailure.Format_GuidInvLen => SR.Format_GuidInvLen,
                    _ => SR.Format_GuidUnrecognized
                });
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly Guid ToGuid()
            {
                return Unsafe.As<GuidResult, Guid>(ref Unsafe.AsRef(in this));
            }

            public void ReverseAbcEndianness()
            {
                _a = BinaryPrimitives.ReverseEndianness(_a);
                _b = BinaryPrimitives.ReverseEndianness(_b);
                _c = BinaryPrimitives.ReverseEndianness(_c);
            }
        }

        // Creates a new guid based on the value in the string.  The value is made up
        // of hex digits speared by the dash ("-"). The string may begin and end with
        // brackets ("{", "}").
        //
        // The string must be of the form dddddddd-dddd-dddd-dddd-dddddddddddd. where
        // d is a hex digit. (That is 8 hex digits, followed by 4, then 4, then 4,
        // then 12) such as: "CA761232-ED42-11CE-BACD-00AA0057B223"
        public Guid(string g)
        {
            ArgumentNullException.ThrowIfNull(g);

            var result = new GuidResult(GuidParseThrowStyle.All);
            bool success = TryParseGuid(g, ref result);
            Debug.Assert(success, "GuidParseThrowStyle.All means throw on all failures");

            this = result.ToGuid();
        }

        public static Guid Parse(string input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return Parse((ReadOnlySpan<char>)input);
        }

        public static Guid Parse(ReadOnlySpan<char> input)
        {
            var result = new GuidResult(GuidParseThrowStyle.AllButOverflow);
            bool success = TryParseGuid(input, ref result);
            Debug.Assert(success, "GuidParseThrowStyle.AllButOverflow means throw on all failures");

            return result.ToGuid();
        }

        public static bool TryParse([NotNullWhen(true)] string? input, out Guid result)
        {
            if (input == null)
            {
                result = default;
                return false;
            }

            return TryParse((ReadOnlySpan<char>)input, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> input, out Guid result)
        {
            var parseResult = new GuidResult(GuidParseThrowStyle.None);
            if (TryParseGuid(input, ref parseResult))
            {
                result = parseResult.ToGuid();
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static Guid ParseExact(string input, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string format)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(format);

            return ParseExact((ReadOnlySpan<char>)input, (ReadOnlySpan<char>)format);
        }

        public static Guid ParseExact(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format)
        {
            if (format.Length != 1)
            {
                // all acceptable format strings are of length 1
                ThrowBadGuidFormatSpecification();
            }

            input = input.Trim();

            var result = new GuidResult(GuidParseThrowStyle.AllButOverflow);
            bool success = ((char)(format[0] | 0x20)) switch
            {
                'd' => TryParseExactD(input, ref result),
                'n' => TryParseExactN(input, ref result),
                'b' => TryParseExactB(input, ref result),
                'p' => TryParseExactP(input, ref result),
                'x' => TryParseExactX(input, ref result),
                _ => throw new FormatException(SR.Format_InvalidGuidFormatSpecification),
            };
            Debug.Assert(success, "GuidParseThrowStyle.AllButOverflow means throw on all failures");
            return result.ToGuid();
        }

        public static bool TryParseExact([NotNullWhen(true)] string? input, [NotNullWhen(true), StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format, out Guid result)
        {
            if (input == null)
            {
                result = default;
                return false;
            }

            return TryParseExact((ReadOnlySpan<char>)input, format, out result);
        }

        public static bool TryParseExact(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format, out Guid result)
        {
            if (format.Length != 1 || input.Length < 32) // Minimal length we can parse ('N' format)
            {
                result = default;
                return false;
            }

            input = input.Trim();

            var parseResult = new GuidResult(GuidParseThrowStyle.None);
            bool success = false;
            switch ((char)(format[0] | 0x20))
            {
                case 'd':
                    success = TryParseExactD(input, ref parseResult);
                    break;

                case 'n':
                    success = TryParseExactN(input, ref parseResult);
                    break;

                case 'b':
                    success = TryParseExactB(input, ref parseResult);
                    break;

                case 'p':
                    success = TryParseExactP(input, ref parseResult);
                    break;

                case 'x':
                    success = TryParseExactX(input, ref parseResult);
                    break;
            }

            if (success)
            {
                result = parseResult.ToGuid();
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        private static bool TryParseGuid(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            guidString = guidString.Trim(); // Remove whitespace from beginning and end

            if (guidString.Length < 32) // Minimal length we can parse ('N' format)
            {
                result.SetFailure(ParseFailure.Format_GuidUnrecognized);
                return false;
            }

            return (guidString[0]) switch
            {
                '(' => TryParseExactP(guidString, ref result),
                '{' => guidString[9] == '-' ?
                        TryParseExactB(guidString, ref result) :
                        TryParseExactX(guidString, ref result),
                _ => guidString[8] == '-' ?
                        TryParseExactD(guidString, ref result) :
                        TryParseExactN(guidString, ref result),
            };
        }

        private static bool TryParseExactB(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            // e.g. "{d85b1407-351d-4694-9392-03acc5870eb1}"

            if (guidString.Length != 38 || guidString[0] != '{' || guidString[37] != '}')
            {
                result.SetFailure(ParseFailure.Format_GuidInvLen);
                return false;
            }

            return TryParseExactD(guidString.Slice(1, 36), ref result);
        }

        private static bool TryParseExactD(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            // e.g. "d85b1407-351d-4694-9392-03acc5870eb1"

            if (guidString.Length != 36 || guidString[8] != '-' || guidString[13] != '-' || guidString[18] != '-' || guidString[23] != '-')
            {
                result.SetFailure(guidString.Length != 36 ? ParseFailure.Format_GuidInvLen : ParseFailure.Format_GuidDashes);
                return false;
            }

            Span<byte> bytes = MemoryMarshal.AsBytes(new Span<GuidResult>(ref result));
            int invalidIfNegative = 0;
            bytes[0] = DecodeByte(guidString[6],   guidString[7],  ref invalidIfNegative);
            bytes[1] = DecodeByte(guidString[4],   guidString[5],  ref invalidIfNegative);
            bytes[2] = DecodeByte(guidString[2],   guidString[3],  ref invalidIfNegative);
            bytes[3] = DecodeByte(guidString[0],   guidString[1],  ref invalidIfNegative);
            bytes[4] = DecodeByte(guidString[11],  guidString[12], ref invalidIfNegative);
            bytes[5] = DecodeByte(guidString[9],   guidString[10], ref invalidIfNegative);
            bytes[6] = DecodeByte(guidString[16],  guidString[17], ref invalidIfNegative);
            bytes[7] = DecodeByte(guidString[14],  guidString[15], ref invalidIfNegative);
            bytes[8] = DecodeByte(guidString[19],  guidString[20], ref invalidIfNegative);
            bytes[9] = DecodeByte(guidString[21],  guidString[22], ref invalidIfNegative);
            bytes[10] = DecodeByte(guidString[24], guidString[25], ref invalidIfNegative);
            bytes[11] = DecodeByte(guidString[26], guidString[27], ref invalidIfNegative);
            bytes[12] = DecodeByte(guidString[28], guidString[29], ref invalidIfNegative);
            bytes[13] = DecodeByte(guidString[30], guidString[31], ref invalidIfNegative);
            bytes[14] = DecodeByte(guidString[32], guidString[33], ref invalidIfNegative);
            bytes[15] = DecodeByte(guidString[34], guidString[35], ref invalidIfNegative);

            if (invalidIfNegative >= 0)
            {
                if (!BitConverter.IsLittleEndian)
                {
                    result.ReverseAbcEndianness();
                }

                return true;
            }

            // The 'D' format has some undesirable behavior leftover from its original implementation:
            // - Components may begin with "0x" and/or "+", but the expected length of each component
            //   needs to include those prefixes, e.g. a four digit component could be "1234" or
            //   "0x34" or "+0x4" or "+234", but not "0x1234" nor "+1234" nor "+0x1234".
            // - "0X" is valid instead of "0x"
            // We continue to support these but expect them to be incredibly rare.  As such, we
            // optimize for correctly formed strings where all the digits are valid hex, and only
            // fall back to supporting these other forms if parsing fails.
            if (guidString.IndexOfAny('X', 'x', '+') >= 0 && TryCompatParsing(guidString, ref result))
            {
                return true;
            }

            result.SetFailure(ParseFailure.Format_GuidInvalidChar);
            return false;

            static bool TryCompatParsing(ReadOnlySpan<char> guidString, ref GuidResult result)
            {
                if (TryParseHex(guidString.Slice(0, 8), out result._a) && // _a
                    TryParseHex(guidString.Slice(9, 4), out uint uintTmp)) // _b
                {
                    result._b = (ushort)uintTmp;
                    if (TryParseHex(guidString.Slice(14, 4), out uintTmp)) // _c
                    {
                        result._c = (ushort)uintTmp;
                        if (TryParseHex(guidString.Slice(19, 4), out uintTmp)) // _d, _e
                        {
                            result._de = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness((ushort)uintTmp) : (ushort)uintTmp;
                            if (TryParseHex(guidString.Slice(24, 4), out uintTmp)) // _f, _g
                            {
                                result._fg = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness((ushort)uintTmp) : (ushort)uintTmp;

                                // Unlike the other components, this one never allowed 0x or +, so we can parse it as straight hex.
                                if (Number.TryParseBinaryIntegerHexNumberStyle(guidString.Slice(28, 8), NumberStyles.AllowHexSpecifier, out uintTmp) == Number.ParsingStatus.OK) // _h, _i, _j, _k
                                {
                                    result._hijk = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(uintTmp) : uintTmp;
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }
        }

        private static bool TryParseExactN(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            // e.g. "d85b1407351d4694939203acc5870eb1"

            if (guidString.Length != 32)
            {
                result.SetFailure(ParseFailure.Format_GuidInvLen);
                return false;
            }

            Span<byte> bytes = MemoryMarshal.AsBytes(new Span<GuidResult>(ref result));
            int invalidIfNegative = 0;
            bytes[0] = DecodeByte(guidString[6], guidString[7], ref invalidIfNegative);
            bytes[1] = DecodeByte(guidString[4], guidString[5], ref invalidIfNegative);
            bytes[2] = DecodeByte(guidString[2], guidString[3], ref invalidIfNegative);
            bytes[3] = DecodeByte(guidString[0], guidString[1], ref invalidIfNegative);
            bytes[4] = DecodeByte(guidString[10], guidString[11], ref invalidIfNegative);
            bytes[5] = DecodeByte(guidString[8], guidString[9], ref invalidIfNegative);
            bytes[6] = DecodeByte(guidString[14], guidString[15], ref invalidIfNegative);
            bytes[7] = DecodeByte(guidString[12], guidString[13], ref invalidIfNegative);
            bytes[8] = DecodeByte(guidString[16], guidString[17], ref invalidIfNegative);
            bytes[9] = DecodeByte(guidString[18], guidString[19], ref invalidIfNegative);
            bytes[10] = DecodeByte(guidString[20], guidString[21], ref invalidIfNegative);
            bytes[11] = DecodeByte(guidString[22], guidString[23], ref invalidIfNegative);
            bytes[12] = DecodeByte(guidString[24], guidString[25], ref invalidIfNegative);
            bytes[13] = DecodeByte(guidString[26], guidString[27], ref invalidIfNegative);
            bytes[14] = DecodeByte(guidString[28], guidString[29], ref invalidIfNegative);
            bytes[15] = DecodeByte(guidString[30], guidString[31], ref invalidIfNegative);

            if (invalidIfNegative >= 0)
            {
                if (!BitConverter.IsLittleEndian)
                {
                    result.ReverseAbcEndianness();
                }

                return true;
            }

            result.SetFailure(ParseFailure.Format_GuidInvalidChar);
            return false;
        }

        private static bool TryParseExactP(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            // e.g. "(d85b1407-351d-4694-9392-03acc5870eb1)"

            if (guidString.Length != 38 || guidString[0] != '(' || guidString[37] != ')')
            {
                result.SetFailure(ParseFailure.Format_GuidInvLen);
                return false;
            }

            return TryParseExactD(guidString.Slice(1, 36), ref result);
        }

        private static bool TryParseExactX(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            // e.g. "{0xd85b1407,0x351d,0x4694,{0x93,0x92,0x03,0xac,0xc5,0x87,0x0e,0xb1}}"

            // Compat notes due to the previous implementation's implementation details.
            // - Each component need not be the full expected number of digits.
            // - Each component may contain any number of leading 0s
            // - The "short" components are parsed as 32-bits and only considered to overflow if they'd overflow 32 bits.
            // - The "byte" components are parsed as 32-bits and are considered to overflow if they'd overflow 8 bits,
            //   but for the Guid ctor, whether they overflow 8 bits or 32 bits results in differing exceptions.
            // - Components may begin with "0x", "0x+", even "0x+0x".
            // - "0X" is valid instead of "0x"

            // Eat all of the whitespace.  Unlike the other forms, X allows for any amount of whitespace
            // anywhere, not just at the beginning and end.
            guidString = EatAllWhitespace(guidString);

            // Check for leading '{'
            if (guidString.Length == 0 || guidString[0] != '{')
            {
                result.SetFailure(ParseFailure.Format_GuidBrace);
                return false;
            }

            // Check for '0x'
            if (!IsHexPrefix(guidString, 1))
            {
                result.SetFailure(ParseFailure.Format_GuidHexPrefix);
                return false;
            }

            // Find the end of this hex number (since it is not fixed length)
            int numStart = 3;
            int numLen = guidString.Slice(numStart).IndexOf(',');
            if (numLen <= 0)
            {
                result.SetFailure(ParseFailure.Format_GuidComma);
                return false;
            }

            bool overflow = false;
            if (!TryParseHex(guidString.Slice(numStart, numLen), out result._a, ref overflow) || overflow)
            {
                result.SetFailure(overflow ? ParseFailure.Overflow_UInt32 : ParseFailure.Format_GuidInvalidChar);
                return false;
            }

            // Check for '0x'
            if (!IsHexPrefix(guidString, numStart + numLen + 1))
            {
                result.SetFailure(ParseFailure.Format_GuidHexPrefix);
                return false;
            }
            // +3 to get by ',0x'
            numStart = numStart + numLen + 3;
            numLen = guidString.Slice(numStart).IndexOf(',');
            if (numLen <= 0)
            {
                result.SetFailure(ParseFailure.Format_GuidComma);
                return false;
            }

            // Read in the number
            if (!TryParseHex(guidString.Slice(numStart, numLen), out result._b, ref overflow) || overflow)
            {
                result.SetFailure(overflow ? ParseFailure.Overflow_UInt32: ParseFailure.Format_GuidInvalidChar);
                return false;
            }

            // Check for '0x'
            if (!IsHexPrefix(guidString, numStart + numLen + 1))
            {
                result.SetFailure(ParseFailure.Format_GuidHexPrefix);
                return false;
            }
            // +3 to get by ',0x'
            numStart = numStart + numLen + 3;
            numLen = guidString.Slice(numStart).IndexOf(',');
            if (numLen <= 0)
            {
                result.SetFailure(ParseFailure.Format_GuidComma);
                return false;
            }

            // Read in the number
            if (!TryParseHex(guidString.Slice(numStart, numLen), out result._c, ref overflow) || overflow)
            {
                result.SetFailure(overflow ? ParseFailure.Overflow_UInt32 : ParseFailure.Format_GuidInvalidChar);
                return false;
            }

            // Check for '{'
            if ((uint)guidString.Length <= (uint)(numStart + numLen + 1) || guidString[numStart + numLen + 1] != '{')
            {
                result.SetFailure(ParseFailure.Format_GuidBrace);
                return false;
            }

            // Prepare for loop
            numLen++;
            for (int i = 0; i < 8; i++)
            {
                // Check for '0x'
                if (!IsHexPrefix(guidString, numStart + numLen + 1))
                {
                    result.SetFailure(ParseFailure.Format_GuidHexPrefix);
                    return false;
                }

                // +3 to get by ',0x' or '{0x' for first case
                numStart = numStart + numLen + 3;

                // Calculate number length
                if (i < 7)  // first 7 cases
                {
                    numLen = guidString.Slice(numStart).IndexOf(',');
                    if (numLen <= 0)
                    {
                        result.SetFailure(ParseFailure.Format_GuidComma);
                        return false;
                    }
                }
                else // last case ends with '}', not ','
                {
                    numLen = guidString.Slice(numStart).IndexOf('}');
                    if (numLen <= 0)
                    {
                        result.SetFailure(ParseFailure.Format_GuidBraceAfterLastNumber);
                        return false;
                    }
                }

                // Read in the number
                if (!TryParseHex(guidString.Slice(numStart, numLen), out uint byteVal, ref overflow) || overflow || byteVal > byte.MaxValue)
                {
                    // The previous implementation had some odd inconsistencies, which are carried forward here.
                    // The byte values in the X format are treated as integers with regards to overflow, so
                    // a "byte" value like 0xddd in Guid's ctor results in a FormatException but 0xddddddddd results
                    // in OverflowException.
                    result.SetFailure(
                        overflow ? ParseFailure.Overflow_UInt32 :
                        byteVal > byte.MaxValue ? ParseFailure.Overflow_Byte :
                        ParseFailure.Format_GuidInvalidChar);
                    return false;
                }
                Unsafe.Add(ref result._d, i) = (byte)byteVal;
            }

            // Check for last '}'
            if (numStart + numLen + 1 >= guidString.Length || guidString[numStart + numLen + 1] != '}')
            {
                result.SetFailure(ParseFailure.Format_GuidEndBrace);
                return false;
            }

            // Check if we have extra characters at the end
            if (numStart + numLen + 1 != guidString.Length - 1)
            {
                result.SetFailure(ParseFailure.Format_ExtraJunkAtEnd);
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte DecodeByte(nuint ch1, nuint ch2, ref int invalidIfNegative)
        {
            // TODO https://github.com/dotnet/runtime/issues/13464:
            // Replace the Unsafe.Add with HexConverter.FromChar once the bounds checks are eliminated.

            ReadOnlySpan<byte> lookup = HexConverter.CharToHexLookup;

            int h1 = -1;
            if (ch1 < (nuint)lookup.Length)
            {
                h1 = (sbyte)Unsafe.Add(ref MemoryMarshal.GetReference(lookup), (nint)ch1);
            }
            h1 <<= 4;

            int h2 = -1;
            if (ch2 < (nuint)lookup.Length)
            {
                h2 = (sbyte)Unsafe.Add(ref MemoryMarshal.GetReference(lookup), (nint)ch2);
            }

            int result = h1 | h2;
            invalidIfNegative |= result;
            return (byte)result;
        }

        private static bool TryParseHex(ReadOnlySpan<char> guidString, out ushort result, ref bool overflow)
        {
            bool success = TryParseHex(guidString, out uint tmp, ref overflow);
            result = (ushort)tmp;
            return success;
        }

        private static bool TryParseHex(ReadOnlySpan<char> guidString, out uint result)
        {
            bool overflowIgnored = false;
            return TryParseHex(guidString, out result, ref overflowIgnored);
        }

        private static bool TryParseHex(ReadOnlySpan<char> guidString, out uint result, ref bool overflow)
        {
            if (guidString.Length > 0)
            {
                if (guidString[0] == '+')
                {
                    guidString = guidString.Slice(1);
                }

                if (guidString.Length > 1 && guidString[0] == '0' && (guidString[1] | 0x20) == 'x')
                {
                    guidString = guidString.Slice(2);
                }
            }

            // Skip past leading 0s.
            int i = 0;
            for (; i < guidString.Length && guidString[i] == '0'; i++) ;

            int processedDigits = 0;
            uint tmp = 0;
            for (; i < guidString.Length; i++)
            {
                char c = guidString[i];
                int numValue = HexConverter.FromChar(c);
                if (numValue == 0xFF)
                {
                    if (processedDigits > 8) overflow = true;
                    result = 0;
                    return false;
                }
                tmp = (tmp * 16) + (uint)numValue;
                processedDigits++;
            }

            if (processedDigits > 8) overflow = true;
            result = tmp;
            return true;
        }

        private static ReadOnlySpan<char> EatAllWhitespace(ReadOnlySpan<char> str)
        {
            // Find the first whitespace character.  If there is none, just return the input.
            int i;
            for (i = 0; i < str.Length && !char.IsWhiteSpace(str[i]); i++) ;
            if (i == str.Length)
            {
                return str;
            }

            // There was at least one whitespace.  Copy over everything prior to it to a new array.
            var chArr = new char[str.Length];
            int newLength = 0;
            if (i > 0)
            {
                newLength = i;
                str.Slice(0, i).CopyTo(chArr);
            }

            // Loop through the remaining chars, copying over non-whitespace.
            for (; i < str.Length; i++)
            {
                char c = str[i];
                if (!char.IsWhiteSpace(c))
                {
                    chArr[newLength++] = c;
                }
            }

            // Return the string with the whitespace removed.
            return new ReadOnlySpan<char>(chArr, 0, newLength);
        }

        private static bool IsHexPrefix(ReadOnlySpan<char> str, int i) =>
            i + 1 < str.Length &&
            str[i] == '0' &&
            (str[i + 1] | 0x20) == 'x';

        // Returns an unsigned byte array containing the GUID.
        public byte[] ToByteArray()
        {
            var g = new byte[16];
            if (BitConverter.IsLittleEndian)
            {
                MemoryMarshal.TryWrite<Guid>(g, ref Unsafe.AsRef(in this));
            }
            else
            {
                TryWriteBytes(g);
            }
            return g;
        }

        // Returns whether bytes are successfully written to given span.
        public bool TryWriteBytes(Span<byte> destination)
        {
            if (BitConverter.IsLittleEndian)
            {
                return MemoryMarshal.TryWrite(destination, ref Unsafe.AsRef(in this));
            }

            // slower path for BigEndian
            if (destination.Length < 16)
                return false;

            destination[15] = _k; // hoist bounds checks
            BinaryPrimitives.WriteInt32LittleEndian(destination, _a);
            BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(4), _b);
            BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(6), _c);
            destination[8] = _d;
            destination[9] = _e;
            destination[10] = _f;
            destination[11] = _g;
            destination[12] = _h;
            destination[13] = _i;
            destination[14] = _j;
            return true;
        }

        public override int GetHashCode()
        {
            // Simply XOR all the bits of the GUID 32 bits at a time.
            ref int r = ref Unsafe.AsRef(in _a);
            return r ^ Unsafe.Add(ref r, 1) ^ Unsafe.Add(ref r, 2) ^ Unsafe.Add(ref r, 3);
        }

        // Returns true if and only if the guid represented
        //  by o is the same as this instance.
        public override bool Equals([NotNullWhen(true)] object? o) => o is Guid g && EqualsCore(this, g);

        public bool Equals(Guid g) => EqualsCore(this, g);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EqualsCore(in Guid left, in Guid right)
        {
            if (Vector128.IsHardwareAccelerated)
            {
                return Vector128.LoadUnsafe(ref Unsafe.As<Guid, byte>(ref Unsafe.AsRef(in left))) == Vector128.LoadUnsafe(ref Unsafe.As<Guid, byte>(ref Unsafe.AsRef(in right)));
            }

            ref int rA = ref Unsafe.AsRef(in left._a);
            ref int rB = ref Unsafe.AsRef(in right._a);

            // Compare each element

            return rA == rB
                && Unsafe.Add(ref rA, 1) == Unsafe.Add(ref rB, 1)
                && Unsafe.Add(ref rA, 2) == Unsafe.Add(ref rB, 2)
                && Unsafe.Add(ref rA, 3) == Unsafe.Add(ref rB, 3);
        }

        private static int GetResult(uint me, uint them) => me < them ? -1 : 1;

        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }
            if (!(value is Guid))
            {
                throw new ArgumentException(SR.Arg_MustBeGuid, nameof(value));
            }
            Guid g = (Guid)value;

            if (g._a != _a)
            {
                return GetResult((uint)_a, (uint)g._a);
            }

            if (g._b != _b)
            {
                return GetResult((uint)_b, (uint)g._b);
            }

            if (g._c != _c)
            {
                return GetResult((uint)_c, (uint)g._c);
            }

            if (g._d != _d)
            {
                return GetResult(_d, g._d);
            }

            if (g._e != _e)
            {
                return GetResult(_e, g._e);
            }

            if (g._f != _f)
            {
                return GetResult(_f, g._f);
            }

            if (g._g != _g)
            {
                return GetResult(_g, g._g);
            }

            if (g._h != _h)
            {
                return GetResult(_h, g._h);
            }

            if (g._i != _i)
            {
                return GetResult(_i, g._i);
            }

            if (g._j != _j)
            {
                return GetResult(_j, g._j);
            }

            if (g._k != _k)
            {
                return GetResult(_k, g._k);
            }

            return 0;
        }

        public int CompareTo(Guid value)
        {
            if (value._a != _a)
            {
                return GetResult((uint)_a, (uint)value._a);
            }

            if (value._b != _b)
            {
                return GetResult((uint)_b, (uint)value._b);
            }

            if (value._c != _c)
            {
                return GetResult((uint)_c, (uint)value._c);
            }

            if (value._d != _d)
            {
                return GetResult(_d, value._d);
            }

            if (value._e != _e)
            {
                return GetResult(_e, value._e);
            }

            if (value._f != _f)
            {
                return GetResult(_f, value._f);
            }

            if (value._g != _g)
            {
                return GetResult(_g, value._g);
            }

            if (value._h != _h)
            {
                return GetResult(_h, value._h);
            }

            if (value._i != _i)
            {
                return GetResult(_i, value._i);
            }

            if (value._j != _j)
            {
                return GetResult(_j, value._j);
            }

            if (value._k != _k)
            {
                return GetResult(_k, value._k);
            }

            return 0;
        }

        public static bool operator ==(Guid a, Guid b) => EqualsCore(a, b);

        public static bool operator !=(Guid a, Guid b) => !EqualsCore(a, b);

        private static unsafe int HexsToChars<TChar>(TChar* guidChars, int a, int b) where TChar : unmanaged, IUtfChar<TChar>
        {
            guidChars[0] = TChar.CastFrom(HexConverter.ToCharLower(a >> 4));
            guidChars[1] = TChar.CastFrom(HexConverter.ToCharLower(a));

            guidChars[2] = TChar.CastFrom(HexConverter.ToCharLower(b >> 4));
            guidChars[3] = TChar.CastFrom(HexConverter.ToCharLower(b));

            return 4;
        }

        private static unsafe int HexsToCharsHexOutput<TChar>(TChar* guidChars, int a, int b) where TChar : unmanaged, IUtfChar<TChar>
        {
            guidChars[0] = TChar.CastFrom('0');
            guidChars[1] = TChar.CastFrom('x');

            guidChars[2] = TChar.CastFrom(HexConverter.ToCharLower(a >> 4));
            guidChars[3] = TChar.CastFrom(HexConverter.ToCharLower(a));

            guidChars[4] = TChar.CastFrom(',');
            guidChars[5] = TChar.CastFrom('0');
            guidChars[6] = TChar.CastFrom('x');

            guidChars[7] = TChar.CastFrom(HexConverter.ToCharLower(b >> 4));
            guidChars[8] = TChar.CastFrom(HexConverter.ToCharLower(b));

            return 9;
        }

        // Returns the guid in "registry" format.
        public override string ToString() => ToString("d", null);

        public string ToString([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format)
        {
            return ToString(format, null);
        }

        // IFormattable interface
        // We currently ignore provider
        public string ToString([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format, IFormatProvider? provider)
        {
            int guidSize;
            if (string.IsNullOrEmpty(format))
            {
                guidSize = 36;
            }
            else
            {
                // all acceptable format strings are of length 1
                if (format.Length != 1)
                {
                    ThrowBadGuidFormatSpecification();
                }

                switch (format[0] | 0x20)
                {
                    case 'd':
                        guidSize = 36;
                        break;

                    case 'n':
                        guidSize = 32;
                        break;

                    case 'b' or 'p':
                        guidSize = 38;
                        break;

                    case 'x':
                        guidSize = 68;
                        break;

                    default:
                        guidSize = 0;
                        ThrowBadGuidFormatSpecification();
                        break;
                };
            }

            string guidString = string.FastAllocateString(guidSize);

            bool result = TryFormatCore(new Span<char>(ref guidString.GetRawStringData(), guidString.Length), out int bytesWritten, format);
            Debug.Assert(result && bytesWritten == guidString.Length, "Formatting guid should have succeeded.");

            return guidString;
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format = default) =>
            TryFormatCore(destination, out charsWritten, format);

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) =>
            // Provider is ignored.
            TryFormatCore(destination, out charsWritten, format);

        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format = default) =>
            TryFormatCore(utf8Destination, out bytesWritten, format);

        bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) =>
            // Provider is ignored.
            TryFormatCore(utf8Destination, out bytesWritten, format);

        // TryFormatCore accepts an `int flags` composed of:
        // - Lowest byte: required length
        // - Second byte: opening brace char, or 0 if no braces
        // - Third byte: closing brace char, or 0 if no braces
        // - Highest bit: 1 if use dashes, else 0
        internal const int TryFormatFlags_UseDashes = unchecked((int)0x80000000);
        internal const int TryFormatFlags_CurlyBraces = ('}' << 16) | ('{' << 8);
        internal const int TryFormatFlags_Parens = (')' << 16) | ('(' << 8);

        private bool TryFormatCore<TChar>(Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format) where TChar : unmanaged, IUtfChar<TChar>
        {
            int flags;

            if (format.Length == 0)
            {
                flags = 36 + TryFormatFlags_UseDashes;
            }
            else
            {
                if (format.Length != 1)
                {
                    ThrowBadGuidFormatSpecification();
                }

                switch (format[0] | 0x20)
                {
                    case 'd':
                        flags = 36 + TryFormatFlags_UseDashes;
                        break;

                    case 'p':
                        flags = 38 + TryFormatFlags_UseDashes + TryFormatFlags_Parens;
                        break;

                    case 'b':
                        flags = 38 + TryFormatFlags_UseDashes + TryFormatFlags_CurlyBraces;
                        break;

                    case 'n':
                        flags = 32;
                        break;

                    case 'x':
                        return TryFormatX(destination, out charsWritten);

                    default:
                        flags = 0;
                        ThrowBadGuidFormatSpecification();
                        break;
                }
            }

            return TryFormatCore(destination, out charsWritten, flags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // only used from two callers
        internal unsafe bool TryFormatCore<TChar>(Span<TChar> destination, out int charsWritten, int flags) where TChar : unmanaged, IUtfChar<TChar>
        {
            // The low byte of flags contains the required length.
            if ((byte)flags > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = (byte)flags;
            flags >>= 8;

            fixed (TChar* guidChars = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = guidChars;

                // The low byte of flags now contains the opening brace char (if any)
                if ((byte)flags != 0)
                {
                    *p++ = TChar.CastFrom((byte)flags);
                }
                flags >>= 8;

                if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) && BitConverter.IsLittleEndian)
                {
                    // Vectorized implementation for D, N, P and B formats:
                    // [{|(]dddddddd[-]dddd[-]dddd[-]dddd[-]dddddddddddd[}|)]
                    (Vector128<byte> vecX, Vector128<byte> vecY, Vector128<byte> vecZ) = FormatGuidVector128Utf8(this, flags < 0 /* dash */);

                    if (typeof(TChar) == typeof(byte))
                    {
                        byte* pChar = (byte*)p;
                        if (flags < 0 /* dash */)
                        {
                            // We need to merge these vectors in this order:
                            // xxxxxxxxxxxxxxxx
                            //                     yyyyyyyyyyyyyyyy
                            //         zzzzzzzzzzzzzzzz
                            vecX.Store(pChar);
                            vecY.Store(pChar + 20);
                            vecZ.Store(pChar + 8);
                            p += 36;
                        }
                        else
                        {
                            // xxxxxxxxxxxxxxxxyyyyyyyyyyyyyyyy
                            vecX.Store(pChar);
                            vecY.Store(pChar + 16);
                            p += 32;
                        }
                    }
                    else
                    {
                        // Expand to UTF-16
                        (Vector128<ushort> x0, Vector128<ushort> x1) = Vector128.Widen(vecX);
                        (Vector128<ushort> y0, Vector128<ushort> y1) = Vector128.Widen(vecY);
                        ushort* pChar = (ushort*)p;
                        if (flags < 0 /* dash */)
                        {
                            (Vector128<ushort> z0, Vector128<ushort> z1) = Vector128.Widen(vecZ);

                            // We need to merge these vectors in this order:
                            // xxxxxxxxxxxxxxxx
                            //                     yyyyyyyyyyyyyyyy
                            //         zzzzzzzzzzzzzzzz
                            x0.Store(pChar);
                            y0.Store(pChar + 20);
                            y1.Store(pChar + 28);
                            z0.Store(pChar + 8); // overlaps x1
                            z1.Store(pChar + 16);
                            p += 36;
                        }
                        else
                        {
                            // xxxxxxxxxxxxxxxxyyyyyyyyyyyyyyyy
                            x0.Store(pChar);
                            x1.Store(pChar + 8);
                            y0.Store(pChar + 16);
                            y1.Store(pChar + 24);
                            p += 32;
                        }
                    }
                }
                else
                {
                    // Non-vectorized fallback for D, N, P and B formats:
                    // [{|(]dddddddd[-]dddd[-]dddd[-]dddd[-]dddddddddddd[}|)]
                    p += HexsToChars(p, _a >> 24, _a >> 16);
                    p += HexsToChars(p, _a >> 8, _a);
                    if (flags < 0 /* dash */)
                    {
                        *p++ = TChar.CastFrom('-');
                    }
                    p += HexsToChars(p, _b >> 8, _b);
                    if (flags < 0 /* dash */)
                    {
                        *p++ = TChar.CastFrom('-');
                    }
                    p += HexsToChars(p, _c >> 8, _c);
                    if (flags < 0 /* dash */)
                    {
                        *p++ = TChar.CastFrom('-');
                    }
                    p += HexsToChars(p, _d, _e);
                    if (flags < 0 /* dash */)
                    {
                        *p++ = TChar.CastFrom('-');
                    }
                    p += HexsToChars(p, _f, _g);
                    p += HexsToChars(p, _h, _i);
                    p += HexsToChars(p, _j, _k);
                }

                // The low byte of flags now contains the closing brace char (if any)
                if ((byte)flags != 0)
                {
                    *p = TChar.CastFrom((byte)flags);
                }

                Debug.Assert(p == guidChars + charsWritten - ((byte)flags != 0 ? 1 : 0));
            }

            return true;
        }

        private unsafe bool TryFormatX<TChar>(Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (destination.Length < 68)
            {
                charsWritten = 0;
                return false;
            }
            charsWritten = 68;

            fixed (TChar* guidChars = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = guidChars;

                // {0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}
                *p++ = TChar.CastFrom('{');
                *p++ = TChar.CastFrom('0');
                *p++ = TChar.CastFrom('x');
                p += HexsToChars(p, _a >> 24, _a >> 16);
                p += HexsToChars(p, _a >> 8, _a);
                *p++ = TChar.CastFrom(',');
                *p++ = TChar.CastFrom('0');
                *p++ = TChar.CastFrom('x');
                p += HexsToChars(p, _b >> 8, _b);
                *p++ = TChar.CastFrom(',');
                *p++ = TChar.CastFrom('0');
                *p++ = TChar.CastFrom('x');
                p += HexsToChars(p, _c >> 8, _c);
                *p++ = TChar.CastFrom(',');
                *p++ = TChar.CastFrom('{');
                p += HexsToCharsHexOutput(p, _d, _e);
                *p++ = TChar.CastFrom(',');
                p += HexsToCharsHexOutput(p, _f, _g);
                *p++ = TChar.CastFrom(',');
                p += HexsToCharsHexOutput(p, _h, _i);
                *p++ = TChar.CastFrom(',');
                p += HexsToCharsHexOutput(p, _j, _k);
                *p++ = TChar.CastFrom('}');
                *p = TChar.CastFrom('}');

                Debug.Assert(p == guidChars + charsWritten - 1);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector128<byte>, Vector128<byte>, Vector128<byte>) FormatGuidVector128Utf8(Guid value, bool useDashes)
        {
            Debug.Assert((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) && BitConverter.IsLittleEndian);
            // Vectorized implementation for D, N, P and B formats:
            // [{|(]dddddddd[-]dddd[-]dddd[-]dddd[-]dddddddddddd[}|)]

            Vector128<byte> hexMap = Vector128.Create(
                (byte)'0', (byte)'1', (byte)'2', (byte)'3',
                (byte)'4', (byte)'5', (byte)'6', (byte)'7',
                (byte)'8', (byte)'9', (byte)'a', (byte)'b',
                (byte)'c', (byte)'d', (byte)'e', (byte)'f');

            Vector128<byte> srcVec = Unsafe.As<Guid, Vector128<byte>>(ref value);
            (Vector128<byte> hexLow, Vector128<byte> hexHigh) =
                HexConverter.AsciiToHexVector128(srcVec, hexMap);

            // because of Guid's layout (int _a, short _b, _c, <8 byte fields>)
            // we have to shuffle some bytes for _a, _b and _c
            hexLow = Vector128.Shuffle(hexLow.AsInt16(), Vector128.Create(3, 2, 1, 0, 5, 4, 7, 6)).AsByte();

            if (useDashes)
            {
                // We divide 16 bytes into 3 x Vector128<byte>:
                //
                // ________-____-____-____-____________
                // xxxxxxxxxxxxxxxx
                //                     yyyyyyyyyyyyyyyy
                //         zzzzzzzzzzzzzzzz
                //
                // Vector "x" - just one dash, shift all elements after it.
                Vector128<byte> vecX = Vector128.Shuffle(hexLow,
                    Vector128.Create(0x706050403020100, 0xD0CFF0B0A0908FF).AsByte());

                // Vector "y" - same here.
                Vector128<byte> vecY = Vector128.Shuffle(hexHigh,
                    Vector128.Create(0x7060504FF030201, 0xF0E0D0C0B0A0908).AsByte());

                // Vector "z" - we need to merge some elements of hexLow with hexHigh and add 4 dashes.
                Vector128<byte> mid1 = Vector128.Shuffle(hexLow,
                    Vector128.Create(0x0D0CFF0B0A0908FF, 0xFFFFFFFFFFFF0F0E).AsByte());
                Vector128<byte> mid2 = Vector128.Shuffle(hexHigh,
                    Vector128.Create(0xFFFFFFFFFFFFFFFF, 0xFF03020100FFFFFF).AsByte());
                Vector128<byte> dashesMask = Vector128.Shuffle(Vector128.CreateScalarUnsafe((byte)'-'),
                    Vector128.Create(0xFFFF00FFFFFFFF00, 0x00FFFFFFFF00FFFF).AsByte());

                Vector128<byte> vecZ = (mid1 | mid2 | dashesMask);
                return (vecX, vecY, vecZ);
            }

            // N format - no dashes.
            return (hexLow, hexHigh, default);
        }

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(Guid left, Guid right)
        {
            if (left._a != right._a)
            {
                return (uint)left._a < (uint)right._a;
            }

            if (left._b != right._b)
            {
                return (uint)left._b < (uint)right._b;
            }

            if (left._c != right._c)
            {
                return (uint)left._c < (uint)right._c;
            }

            if (left._d != right._d)
            {
                return left._d < right._d;
            }

            if (left._e != right._e)
            {
                return left._e < right._e;
            }

            if (left._f != right._f)
            {
                return left._f < right._f;
            }

            if (left._g != right._g)
            {
                return left._g < right._g;
            }

            if (left._h != right._h)
            {
                return left._h < right._h;
            }

            if (left._i != right._i)
            {
                return left._i < right._i;
            }

            if (left._j != right._j)
            {
                return left._j < right._j;
            }

            if (left._k != right._k)
            {
                return left._k < right._k;
            }

            return false;
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(Guid left, Guid right)
        {
            if (left._a != right._a)
            {
                return (uint)left._a < (uint)right._a;
            }

            if (left._b != right._b)
            {
                return (uint)left._b < (uint)right._b;
            }

            if (left._c != right._c)
            {
                return (uint)left._c < (uint)right._c;
            }

            if (left._d != right._d)
            {
                return left._d < right._d;
            }

            if (left._e != right._e)
            {
                return left._e < right._e;
            }

            if (left._f != right._f)
            {
                return left._f < right._f;
            }

            if (left._g != right._g)
            {
                return left._g < right._g;
            }

            if (left._h != right._h)
            {
                return left._h < right._h;
            }

            if (left._i != right._i)
            {
                return left._i < right._i;
            }

            if (left._j != right._j)
            {
                return left._j < right._j;
            }

            if (left._k != right._k)
            {
                return left._k < right._k;
            }

            return true;
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(Guid left, Guid right)
        {
            if (left._a != right._a)
            {
                return (uint)left._a > (uint)right._a;
            }

            if (left._b != right._b)
            {
                return (uint)left._b > (uint)right._b;
            }

            if (left._c != right._c)
            {
                return (uint)left._c > (uint)right._c;
            }

            if (left._d != right._d)
            {
                return left._d > right._d;
            }

            if (left._e != right._e)
            {
                return left._e > right._e;
            }

            if (left._f != right._f)
            {
                return left._f > right._f;
            }

            if (left._g != right._g)
            {
                return left._g > right._g;
            }

            if (left._h != right._h)
            {
                return left._h > right._h;
            }

            if (left._i != right._i)
            {
                return left._i > right._i;
            }

            if (left._j != right._j)
            {
                return left._j > right._j;
            }

            if (left._k != right._k)
            {
                return left._k > right._k;
            }

            return false;
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(Guid left, Guid right)
        {
            if (left._a != right._a)
            {
                return (uint)left._a > (uint)right._a;
            }

            if (left._b != right._b)
            {
                return (uint)left._b > (uint)right._b;
            }

            if (left._c != right._c)
            {
                return (uint)left._c > (uint)right._c;
            }

            if (left._d != right._d)
            {
                return left._d > right._d;
            }

            if (left._e != right._e)
            {
                return left._e > right._e;
            }

            if (left._f != right._f)
            {
                return left._f > right._f;
            }

            if (left._g != right._g)
            {
                return left._g > right._g;
            }

            if (left._h != right._h)
            {
                return left._h > right._h;
            }

            if (left._i != right._i)
            {
                return left._i > right._i;
            }

            if (left._j != right._j)
            {
                return left._j > right._j;
            }

            if (left._k != right._k)
            {
                return left._k > right._k;
            }

            return true;
        }

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" />
        public static Guid Parse(string s, IFormatProvider? provider) => Parse(s);

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Guid result) => TryParse(s, out result);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Guid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Guid result) => TryParse(s, out result);

        [DoesNotReturn]
        private static void ThrowBadGuidFormatSpecification() =>
            throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
    }
}
