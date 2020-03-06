// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System
{
    // Represents a Globally Unique Identifier.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NonVersionable] // This only applies to field layout
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial struct Guid : IFormattable, IComparable, IComparable<Guid>, IEquatable<Guid>, ISpanFormattable
    {
        public static readonly Guid Empty = default;

        private int _a;   // Do not rename (binary serialization)
        private short _b; // Do not rename (binary serialization)
        private short _c; // Do not rename (binary serialization)
        private byte _d;  // Do not rename (binary serialization)
        private byte _e;  // Do not rename (binary serialization)
        private byte _f;  // Do not rename (binary serialization)
        private byte _g;  // Do not rename (binary serialization)
        private byte _h;  // Do not rename (binary serialization)
        private byte _i;  // Do not rename (binary serialization)
        private byte _j;  // Do not rename (binary serialization)
        private byte _k;  // Do not rename (binary serialization)

        // Creates a new guid from an array of bytes.
        public Guid(byte[] b) :
            this(new ReadOnlySpan<byte>(b ?? throw new ArgumentNullException(nameof(b))))
        {
        }

        // Creates a new guid from a read-only span.
        public Guid(ReadOnlySpan<byte> b)
        {
            if ((uint)b.Length != 16)
            {
                throw new ArgumentException(SR.Format(SR.Arg_GuidArrayCtor, "16"), nameof(b));
            }

            if (BitConverter.IsLittleEndian)
            {
                this = MemoryMarshal.Read<Guid>(b);
                return;
            }

            // slower path for BigEndian:
            _k = b[15];  // hoist bounds checks
            _a = b[3] << 24 | b[2] << 16 | b[1] << 8 | b[0];
            _b = (short)(b[5] << 8 | b[4]);
            _c = (short)(b[7] << 8 | b[6]);
            _d = b[8];
            _e = b[9];
            _f = b[10];
            _g = b[11];
            _h = b[12];
            _i = b[13];
            _j = b[14];
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
            if (d == null)
            {
                throw new ArgumentNullException(nameof(d));
            }
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

        // This will store the result of the parsing. And it will eventually be used to construct a Guid instance.
        private struct GuidResult
        {
            private readonly GuidParseThrowStyle _throwStyle;
            internal Guid _parsedGuid;

            internal GuidResult(GuidParseThrowStyle canThrow) : this()
            {
                _throwStyle = canThrow;
            }

            internal void SetFailure(bool overflow, string failureMessageID)
            {
                if (_throwStyle == GuidParseThrowStyle.None)
                {
                    return;
                }

                if (overflow)
                {
                    if (_throwStyle == GuidParseThrowStyle.All)
                    {
                        throw new OverflowException(SR.GetResourceString(failureMessageID));
                    }

                    throw new FormatException(SR.Format_GuidUnrecognized);
                }

                throw new FormatException(SR.GetResourceString(failureMessageID));
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
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            var result = new GuidResult(GuidParseThrowStyle.All);
            bool success = TryParseGuid(g, ref result);
            Debug.Assert(success, "GuidParseThrowStyle.All means throw on all failures");

            this = result._parsedGuid;
        }

        public static Guid Parse(string input) =>
            Parse(input != null ? (ReadOnlySpan<char>)input : throw new ArgumentNullException(nameof(input)));

        public static Guid Parse(ReadOnlySpan<char> input)
        {
            var result = new GuidResult(GuidParseThrowStyle.AllButOverflow);
            bool success = TryParseGuid(input, ref result);
            Debug.Assert(success, "GuidParseThrowStyle.AllButOverflow means throw on all failures");

            return result._parsedGuid;
        }

        public static bool TryParse(string? input, out Guid result)
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
                result = parseResult._parsedGuid;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public static Guid ParseExact(string input, string format) =>
            ParseExact(
                input != null ? (ReadOnlySpan<char>)input : throw new ArgumentNullException(nameof(input)),
                format != null ? (ReadOnlySpan<char>)format : throw new ArgumentNullException(nameof(format)));

        public static Guid ParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format)
        {
            if (format.Length != 1)
            {
                // all acceptable format strings are of length 1
                throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
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
            return result._parsedGuid;
        }

        public static bool TryParseExact(string? input, string? format, out Guid result)
        {
            if (input == null)
            {
                result = default;
                return false;
            }

            return TryParseExact((ReadOnlySpan<char>)input, format, out result);
        }

        public static bool TryParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format, out Guid result)
        {
            if (format.Length != 1)
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
                result = parseResult._parsedGuid;
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

            if (guidString.Length == 0)
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidUnrecognized));
                return false;
            }

            return (guidString[0]) switch
            {
                '(' => TryParseExactP(guidString, ref result),
                '{' => guidString.Contains('-') ?
                        TryParseExactB(guidString, ref result) :
                        TryParseExactX(guidString, ref result),
                _ => guidString.Contains('-') ?
                        TryParseExactD(guidString, ref result) :
                        TryParseExactN(guidString, ref result),
            };
        }

        // Two helpers used for parsing components:
        // - uint.TryParse(..., NumberStyles.AllowHexSpecifier, ...)
        //       Used when we expect the entire provided span to be filled with and only with hex digits and no overflow is possible
        // - TryParseHex
        //       Used when the component may have an optional '+' and "0x" prefix, when it may overflow, etc.

        private static bool TryParseExactB(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            // e.g. "{d85b1407-351d-4694-9392-03acc5870eb1}"

            if ((uint)guidString.Length != 38 || guidString[0] != '{' || guidString[37] != '}')
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidInvLen));
                return false;
            }

            return TryParseExactD(guidString.Slice(1, 36), ref result);
        }

        private static bool TryParseExactD(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            // e.g. "d85b1407-351d-4694-9392-03acc5870eb1"

            // Compat notes due to the previous implementation's implementation details.
            // - Components may begin with "0x" or "0x+", but the expected length of each component
            //   needs to include those prefixes, e.g. a four digit component could be "1234" or
            //   "0x34" or "+0x4" or "+234", but not "0x1234" nor "+1234" nor "+0x1234".
            // - "0X" is valid instead of "0x"

            if ((uint)guidString.Length != 36)
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidInvLen));
                return false;
            }

            if (guidString[8] != '-' || guidString[13] != '-' || guidString[18] != '-' || guidString[23] != '-')
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidDashes));
                return false;
            }

            ref Guid g = ref result._parsedGuid;

            if (TryParseHex(guidString.Slice(0, 8), out Unsafe.As<int, uint>(ref g._a)) && // _a
                TryParseHex(guidString.Slice(9, 4), out uint uintTmp)) // _b
            {
                g._b = (short)uintTmp;

                if (TryParseHex(guidString.Slice(14, 4), out uintTmp)) // _c
                {
                    g._c = (short)uintTmp;

                    if (TryParseHex(guidString.Slice(19, 4), out uintTmp)) // _d, _e
                    {
                        g._d = (byte)(uintTmp >> 8);
                        g._e = (byte)uintTmp;

                        if (TryParseHex(guidString.Slice(24, 4), out uintTmp)) // _f, _g
                        {
                            g._f = (byte)(uintTmp >> 8);
                            g._g = (byte)uintTmp;

                            if (uint.TryParse(guidString.Slice(28, 8), NumberStyles.AllowHexSpecifier, null, out uintTmp)) // _h, _i, _j, _k
                            {
                                g._h = (byte)(uintTmp >> 24);
                                g._i = (byte)(uintTmp >> 16);
                                g._j = (byte)(uintTmp >> 8);
                                g._k = (byte)uintTmp;

                                return true;
                            }
                        }
                    }
                }
            }

            result.SetFailure(overflow: false, nameof(SR.Format_GuidInvalidChar));
            return false;
        }

        private static bool TryParseExactN(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            // e.g. "d85b1407351d4694939203acc5870eb1"

            if ((uint)guidString.Length != 32)
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidInvLen));
                return false;
            }

            ref Guid g = ref result._parsedGuid;

            if (uint.TryParse(guidString.Slice(0, 8), NumberStyles.AllowHexSpecifier, null, out Unsafe.As<int, uint>(ref g._a)) && // _a
                uint.TryParse(guidString.Slice(8, 8), NumberStyles.AllowHexSpecifier, null, out uint uintTmp)) // _b, _c
            {
                g._b = (short)(uintTmp >> 16);
                g._c = (short)uintTmp;

                if (uint.TryParse(guidString.Slice(16, 8), NumberStyles.AllowHexSpecifier, null, out uintTmp)) // _d, _e, _f, _g
                {
                    g._d = (byte)(uintTmp >> 24);
                    g._e = (byte)(uintTmp >> 16);
                    g._f = (byte)(uintTmp >> 8);
                    g._g = (byte)uintTmp;

                    if (uint.TryParse(guidString.Slice(24, 8), NumberStyles.AllowHexSpecifier, null, out uintTmp)) // _h, _i, _j, _k
                    {
                        g._h = (byte)(uintTmp >> 24);
                        g._i = (byte)(uintTmp >> 16);
                        g._j = (byte)(uintTmp >> 8);
                        g._k = (byte)uintTmp;

                        return true;
                    }
                }
            }

            result.SetFailure(overflow: false, nameof(SR.Format_GuidInvalidChar));
            return false;
        }

        private static bool TryParseExactP(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            // e.g. "(d85b1407-351d-4694-9392-03acc5870eb1)"

            if ((uint)guidString.Length != 38 || guidString[0] != '(' || guidString[37] != ')')
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidInvLen));
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
            if ((uint)guidString.Length == 0 || guidString[0] != '{')
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidBrace));
                return false;
            }

            // Check for '0x'
            if (!IsHexPrefix(guidString, 1))
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidHexPrefix));
                return false;
            }

            // Find the end of this hex number (since it is not fixed length)
            int numStart = 3;
            int numLen = guidString.Slice(numStart).IndexOf(',');
            if (numLen <= 0)
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidComma));
                return false;
            }

            bool overflow = false;
            if (!TryParseHex(guidString.Slice(numStart, numLen), out Unsafe.As<int, uint>(ref result._parsedGuid._a), ref overflow) || overflow)
            {
                result.SetFailure(overflow, overflow ? nameof(SR.Overflow_UInt32) : nameof(SR.Format_GuidInvalidChar));
                return false;
            }

            // Check for '0x'
            if (!IsHexPrefix(guidString, numStart + numLen + 1))
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidHexPrefix));
                return false;
            }
            // +3 to get by ',0x'
            numStart = numStart + numLen + 3;
            numLen = guidString.Slice(numStart).IndexOf(',');
            if (numLen <= 0)
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidComma));
                return false;
            }

            // Read in the number
            if (!TryParseHex(guidString.Slice(numStart, numLen), out result._parsedGuid._b, ref overflow) || overflow)
            {
                result.SetFailure(overflow, overflow ? nameof(SR.Overflow_UInt32) : nameof(SR.Format_GuidInvalidChar));
                return false;
            }

            // Check for '0x'
            if (!IsHexPrefix(guidString, numStart + numLen + 1))
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidHexPrefix));
                return false;
            }
            // +3 to get by ',0x'
            numStart = numStart + numLen + 3;
            numLen = guidString.Slice(numStart).IndexOf(',');
            if (numLen <= 0)
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidComma));
                return false;
            }

            // Read in the number
            if (!TryParseHex(guidString.Slice(numStart, numLen), out result._parsedGuid._c, ref overflow) || overflow)
            {
                result.SetFailure(overflow, overflow ? nameof(SR.Overflow_UInt32) : nameof(SR.Format_GuidInvalidChar));
                return false;
            }

            // Check for '{'
            if ((uint)guidString.Length <= (uint)(numStart + numLen + 1) || guidString[numStart + numLen + 1] != '{')
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidBrace));
                return false;
            }

            // Prepare for loop
            numLen++;
            for (int i = 0; i < 8; i++)
            {
                // Check for '0x'
                if (!IsHexPrefix(guidString, numStart + numLen + 1))
                {
                    result.SetFailure(overflow: false, nameof(SR.Format_GuidHexPrefix));
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
                        result.SetFailure(overflow: false, nameof(SR.Format_GuidComma));
                        return false;
                    }
                }
                else // last case ends with '}', not ','
                {
                    numLen = guidString.Slice(numStart).IndexOf('}');
                    if (numLen <= 0)
                    {
                        result.SetFailure(overflow: false, nameof(SR.Format_GuidBraceAfterLastNumber));
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
                    result.SetFailure(overflow,
                        overflow ? nameof(SR.Overflow_UInt32) :
                        byteVal > byte.MaxValue ? nameof(SR.Overflow_Byte) :
                        nameof(SR.Format_GuidInvalidChar));
                    return false;
                }
                Unsafe.Add(ref result._parsedGuid._d, i) = (byte)byteVal;
            }

            // Check for last '}'
            if (numStart + numLen + 1 >= guidString.Length || guidString[numStart + numLen + 1] != '}')
            {
                result.SetFailure(overflow: false, nameof(SR.Format_GuidEndBrace));
                return false;
            }

            // Check if we have extra characters at the end
            if (numStart + numLen + 1 != guidString.Length - 1)
            {
                result.SetFailure(overflow: false, nameof(SR.Format_ExtraJunkAtEnd));
                return false;
            }

            return true;
        }

        private static bool TryParseHex(ReadOnlySpan<char> guidString, out short result, ref bool overflow)
        {
            bool success = TryParseHex(guidString, out uint tmp, ref overflow);
            result = (short)tmp;
            return success;
        }

        private static bool TryParseHex(ReadOnlySpan<char> guidString, out uint result)
        {
            bool overflowIgnored = false;
            return TryParseHex(guidString, out result, ref overflowIgnored);
        }

        private static bool TryParseHex(ReadOnlySpan<char> guidString, out uint result, ref bool overflow)
        {
            if ((uint)guidString.Length > 0)
            {
                if (guidString[0] == '+')
                {
                    guidString = guidString.Slice(1);
                }

                if ((uint)guidString.Length > 1 && guidString[0] == '0' && (guidString[1] | 0x20) == 'x')
                {
                    guidString = guidString.Slice(2);
                }
            }

            // Skip past leading 0s.
            int i = 0;
            for (; i < guidString.Length && guidString[i] == '0'; i++) ;

            int processedDigits = 0;
            ReadOnlySpan<byte> charToHexLookup = Number.CharToHexLookup;
            uint tmp = 0;
            for (; i < guidString.Length; i++)
            {
                int numValue;
                char c = guidString[i];
                if (c >= (uint)charToHexLookup.Length || (numValue = charToHexLookup[c]) == 0xFF)
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
                MemoryMarshal.TryWrite<Guid>(g, ref this);
            }
            else
            {
                TryWriteBytes(g);
            }
            return g;
        }

        // Returns whether bytes are sucessfully written to given span.
        public bool TryWriteBytes(Span<byte> destination)
        {
            if (BitConverter.IsLittleEndian)
            {
                return MemoryMarshal.TryWrite(destination, ref this);
            }

            // slower path for BigEndian
            if (destination.Length < 16)
                return false;

            destination[15] = _k; // hoist bounds checks
            destination[0] = (byte)(_a);
            destination[1] = (byte)(_a >> 8);
            destination[2] = (byte)(_a >> 16);
            destination[3] = (byte)(_a >> 24);
            destination[4] = (byte)(_b);
            destination[5] = (byte)(_b >> 8);
            destination[6] = (byte)(_c);
            destination[7] = (byte)(_c >> 8);
            destination[8] = _d;
            destination[9] = _e;
            destination[10] = _f;
            destination[11] = _g;
            destination[12] = _h;
            destination[13] = _i;
            destination[14] = _j;
            return true;
        }

        // Returns the guid in "registry" format.
        public override string ToString() => ToString("D", null);

        public override int GetHashCode()
        {
            // Simply XOR all the bits of the GUID 32 bits at a time.
            return _a ^ Unsafe.Add(ref _a, 1) ^ Unsafe.Add(ref _a, 2) ^ Unsafe.Add(ref _a, 3);
        }

        // Returns true if and only if the guid represented
        //  by o is the same as this instance.
        public override bool Equals(object? o)
        {
            Guid g;
            // Check that o is a Guid first
            if (o == null || !(o is Guid))
                return false;
            else g = (Guid)o;

            // Now compare each of the elements
            return g._a == _a &&
                Unsafe.Add(ref g._a, 1) == Unsafe.Add(ref _a, 1) &&
                Unsafe.Add(ref g._a, 2) == Unsafe.Add(ref _a, 2) &&
                Unsafe.Add(ref g._a, 3) == Unsafe.Add(ref _a, 3);
        }

        public bool Equals(Guid g)
        {
            // Now compare each of the elements
            return g._a == _a &&
                Unsafe.Add(ref g._a, 1) == Unsafe.Add(ref _a, 1) &&
                Unsafe.Add(ref g._a, 2) == Unsafe.Add(ref _a, 2) &&
                Unsafe.Add(ref g._a, 3) == Unsafe.Add(ref _a, 3);
        }

        private int GetResult(uint me, uint them) => me < them ? -1 : 1;

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

        public static bool operator ==(Guid a, Guid b) =>
            a._a == b._a &&
                Unsafe.Add(ref a._a, 1) == Unsafe.Add(ref b._a, 1) &&
                Unsafe.Add(ref a._a, 2) == Unsafe.Add(ref b._a, 2) &&
                Unsafe.Add(ref a._a, 3) == Unsafe.Add(ref b._a, 3);

        public static bool operator !=(Guid a, Guid b) =>
            // Now compare each of the elements
            a._a != b._a ||
                Unsafe.Add(ref a._a, 1) != Unsafe.Add(ref b._a, 1) ||
                Unsafe.Add(ref a._a, 2) != Unsafe.Add(ref b._a, 2) ||
                Unsafe.Add(ref a._a, 3) != Unsafe.Add(ref b._a, 3);

        public string ToString(string? format)
        {
            return ToString(format, null);
        }

        // IFormattable interface
        // We currently ignore provider
        public unsafe string ToString(string? format, IFormatProvider? provider)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "D";
            }

            // all acceptable format strings are of length 1
            if (format.Length != 1)
            {
                throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
            }

            if (Avx2.IsSupported)
            {
                switch (format[0])
                {
                    case 'D':
                    case 'd':
                        {
                            string output = string.FastAllocateString(36);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                FormatDAvx((short*)pinnedOutput);
                            }
                            return output;
                        }
                    case 'N':
                    case 'n':
                        {
                            string output = string.FastAllocateString(32);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                FormatNAvx((short*)pinnedOutput);
                            }
                            return output;
                        }
                    case 'B':
                    case 'b':
                        {
                            string output = string.FastAllocateString(38);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                pinnedOutput[0] = '{';
                                FormatDAvx((short*)pinnedOutput + 1);
                                pinnedOutput[37] = '}';
                            }
                            return output;
                        }
                    case 'P':
                    case 'p':
                        {
                            string output = string.FastAllocateString(38);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                pinnedOutput[0] = '(';
                                FormatDAvx((short*)pinnedOutput + 1);
                                pinnedOutput[37] = ')';
                            }
                            return output;
                        }
                    case 'X':
                    case 'x':
                        {
                            string output = string.FastAllocateString(68);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                FormatX(pinnedOutput);
                            }
                            return output;
                        }
                    default:
                        throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
                }
            }
            else
            {
                switch (format[0])
                {
                    case 'D':
                    case 'd':
                        {
                            string output = string.FastAllocateString(36);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                FormatD(pinnedOutput);
                            }
                            return output;
                        }
                    case 'N':
                    case 'n':
                        {
                            string output = string.FastAllocateString(32);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                FormatN(pinnedOutput);
                            }
                            return output;
                        }
                    case 'B':
                    case 'b':
                        {
                            string output = string.FastAllocateString(38);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                pinnedOutput[0] = '{';
                                FormatD(pinnedOutput + 1);
                                pinnedOutput[37] = '}';
                            }
                            return output;
                        }
                    case 'P':
                    case 'p':
                        {
                            string output = string.FastAllocateString(38);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                pinnedOutput[0] = '(';
                                FormatD(pinnedOutput + 1);
                                pinnedOutput[37] = ')';
                            }
                            return output;
                        }
                    case 'X':
                    case 'x':
                        {
                            string output = string.FastAllocateString(68);
                            fixed (char* pinnedOutput = &output.GetPinnableReference())
                            {
                                FormatX(pinnedOutput);
                            }
                            return output;
                        }
                    default:
                        throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
                }
            }
        }

        // Returns whether the guid is successfully formatted as a span.
        public unsafe bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
        {
            if (format.Length == 0)
            {
                format = "D";
            }
            // all acceptable format strings are of length 1
            if (format.Length != 1)
            {
                throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
            }

            if (Avx2.IsSupported)
            {
                switch (format[0])
                {
                    case 'D':
                    case 'd':
                        {
                            if (destination.Length < 36)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                FormatDAvx((short*)pinnedOutput);
                            }
                            charsWritten = 36;
                            return true;
                        }
                    case 'N':
                    case 'n':
                        {
                            if (destination.Length < 32)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                FormatNAvx((short*)pinnedOutput);
                            }
                            charsWritten = 32;
                            return true;
                        }
                    case 'B':
                    case 'b':
                        {
                            if (destination.Length < 38)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                pinnedOutput[0] = '{';
                                FormatDAvx((short*)pinnedOutput + 1);
                                pinnedOutput[37] = '}';
                            }
                            charsWritten = 38;
                            return true;
                        }
                    case 'P':
                    case 'p':
                        {
                            if (destination.Length < 38)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                pinnedOutput[0] = '(';
                                FormatDAvx((short*)pinnedOutput + 1);
                                pinnedOutput[37] = ')';
                            }
                            charsWritten = 38;
                            return true;
                        }
                    case 'X':
                    case 'x':
                        {
                            if (destination.Length < 68)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                FormatX(pinnedOutput);
                            }
                            charsWritten = 68;
                            return true;
                        }
                    default:
                        throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
                }
            }
            else
            {
                switch (format[0])
                {
                    case 'D':
                    case 'd':
                        {
                            if (destination.Length < 36)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                FormatD(pinnedOutput);
                            }
                            charsWritten = 36;
                            return true;
                        }
                    case 'N':
                    case 'n':
                        {
                            if (destination.Length < 32)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                FormatN(pinnedOutput);
                            }
                            charsWritten = 32;
                            return true;
                        }
                    case 'B':
                    case 'b':
                        {
                            if (destination.Length < 38)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                pinnedOutput[0] = '{';
                                FormatD(pinnedOutput + 1);
                                pinnedOutput[37] = '}';
                            }
                            charsWritten = 38;
                            return true;
                        }
                    case 'P':
                    case 'p':
                        {
                            if (destination.Length < 38)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                pinnedOutput[0] = '(';
                                FormatD(pinnedOutput + 1);
                                pinnedOutput[37] = ')';
                            }
                            charsWritten = 38;
                            return true;
                        }
                    case 'X':
                    case 'x':
                        {
                            if (destination.Length < 68)
                            {
                                charsWritten = 0;
                                return false;
                            }

                            fixed (char* pinnedOutput = &destination.GetPinnableReference())
                            {
                                FormatX(pinnedOutput);
                            }
                            charsWritten = 68;
                            return true;
                        }
                    default:
                        throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
                }
            }
        }

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            // Like with the IFormattable implementation, provider is ignored.
            return TryFormat(destination, out charsWritten, format);
        }

        private static readonly Vector128<byte> s_layoutShiftMask = Vector128.Create(
            0x06_07_04_05_00_01_02_03UL, 0x0F_0E_0D_0C_0B_0A_09_08UL).AsByte();
        // 0x03, 0x02, 0x01, 0x00, 0x05, 0x04, 0x07, 0x06, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F

        private static readonly Vector256<byte> s_asciiTable = Vector256.Create(
            0x37_36_35_34_33_32_31_30UL, 0x66_65_64_63_62_61_39_38UL,
            0x37_36_35_34_33_32_31_30UL, 0x66_65_64_63_62_61_39_38UL).AsByte();
        // 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102,
        // 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102

        private static readonly Vector256<byte> s_formatDFirst16DashesShuffleMask = Vector256.Create(
            0x07_06_05_04_03_02_01_00UL, 0x0F_0E_0D_0C_0B_0A_09_08UL,
            0x05_04_03_02_01_00_FF_FFUL, 0x0B_0A_09_08_FF_FF_07_06UL).AsByte();
        // 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        // 0xFF, 0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0xFF, 0xFF, 0x08, 0x09, 0x0A, 0x0B

        private static readonly Vector256<byte> s_formatDFirst16DashesSet = Vector256.Create(
            0x00_00_00_00_00_00_00_00UL, 0x00_00_00_00_00_00_00_00UL,
            0x00_00_00_00_00_00_00_2DUL, 0x00_00_00_00_00_2D_00_00UL).AsByte();
        // 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        // 0x2D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2D, 0x00, 0x00, 0x00, 0x00, 0x00

        private static readonly Vector256<byte> s_formatDFirst16RemainDashesShuffleMask = Vector256.Create(
            0x0B_0A_09_08_07_06_05_04UL, 0x11_10_FF_FF_0F_0E_0D_0CUL,
            0xFF_FF_07_06_05_04_03_02UL, 0x0F_0E_0D_0C_0B_0A_09_08UL).AsByte();
        // 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0xFF, 0xFF, 0x10, 0x11,
        // 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0xFF, 0xFF, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F

        private static readonly Vector256<byte> s_formatDFirst16RemainDashesSet = Vector256.Create(
            0x00_00_00_00_00_00_00_00UL, 0x00_00_00_2D_00_00_00_00UL,
            0x00_2D_00_00_00_00_00_00UL, 0x00_00_00_00_00_00_00_00UL).AsByte();
        // 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2D, 0x00, 0x00, 0x00,
        // 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00

        private static readonly Vector256<byte> s_formatDLast16DashesShuffleMask = Vector256.Create(
            0x05_04_03_02_01_00_FF_FFUL, 0x0B_0A_09_08_FF_FF_07_06UL,
            0xFF_FF_FF_FF_FF_FF_FF_FFUL, 0xFF_FF_FF_FF_FF_FF_FF_FFUL).AsByte();
        // 0xFF, 0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0xFF, 0xFF, 0x08, 0x09, 0x0A, 0x0B,
        // 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);

        private static readonly Vector256<byte> s_formatDLast16DashesSet = Vector256.Create(
            0x00_00_00_00_00_00_00_2DUL, 0x00_00_00_00_00_2D_00_00UL,
            0x00_00_00_00_00_00_00_00UL, 0x00_00_00_00_00_00_00_00UL).AsByte();
        // 0x2D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2D, 0x00, 0x00, 0x00, 0x00, 0x00,
        // 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        private static readonly Vector256<byte> s_formatDLast16RemainDashesShuffleMask = Vector256.Create(
            0xFF_FF_07_06_05_04_03_02UL, 0x0F_0E_0D_0C_0B_0A_09_08UL,
            0x07_06_05_04_03_02_01_00UL, 0x0F_0E_0D_0C_0B_0A_09_08UL).AsByte();
        // 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0xFF, 0xFF, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        // 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F);

        private static Vector256<byte> s_formatDLast16RemainDashesSet = Vector256.Create(
            0x00_2D_00_00_00_00_00_00UL, 0x00_00_00_00_00_00_00_00UL,
            0x00_00_00_00_00_00_00_00UL, 0x00_00_00_00_00_00_00_00UL).AsByte();
        // 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        // 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        private unsafe void FormatDAvx(short* dest)
        {
            // dddddddd-dddd-dddd-dddd-dddddddddddd
            fixed (Guid* thisPtr = &this)
            {
                Vector128<byte> layoutMask = s_layoutShiftMask;
                Vector256<byte> asciiTable = s_asciiTable;

                Vector128<byte> guidStringLayoutBytes = Ssse3.Shuffle(Sse2.LoadVector128((byte*)thisPtr), layoutMask);
                Vector256<short> guidStringLayoutUtf16 = Avx2.ConvertToVector256Int16(guidStringLayoutBytes);
                Vector256<byte> hi = Avx2.ShiftRightLogical(guidStringLayoutUtf16, 4).AsByte();
                Vector256<byte> lo = Avx2.ShiftLeftLogical(guidStringLayoutUtf16, 8).AsByte();
                Vector256<byte> asciiTableIndices = Avx2.And(Avx2.Or(hi, lo), Vector256.Create((byte)0x0F));
                Vector256<byte> asciiBytes = Avx2.Shuffle(asciiTable, asciiTableIndices);

                // Fill dest
                // asciiBytes = 00112233445566778899AABBCCDDEEFF

                // Writing first 16 ASCII result chars with dashes
                Vector256<byte> formatDFirst16RemainDashesShuffleMask = s_formatDFirst16RemainDashesShuffleMask;
                Vector256<byte> formatDFirst16RemainDashesSet = s_formatDFirst16RemainDashesSet;
                Vector256<byte> formatDFirst16DashesShuffleMask = s_formatDFirst16DashesShuffleMask;
                Vector256<byte> formatDFirst16DashesSet = s_formatDFirst16DashesSet;

                Vector256<byte> rawFirst16BytesChars = Avx2.ConvertToVector256Int16(asciiBytes.GetLower()).AsByte();
                Vector256<byte> first16CharsRemainWithDashes = Avx2.Or(
                    Avx2.Shuffle(rawFirst16BytesChars, formatDFirst16RemainDashesShuffleMask),
                    formatDFirst16RemainDashesSet);
                // Will store: xx112233-4455-6677xxxxxxxxxxxxxxxxxx
                Avx.Store((byte*)dest + 4, first16CharsRemainWithDashes);
                // Destination value after store: xx112233-4455-6677xxxxxxxxxxxxxxxxxx

                Vector256<byte> first16CharsWithDashes = Avx2.Or(
                    Avx2.Shuffle(rawFirst16BytesChars, formatDFirst16DashesShuffleMask),
                    formatDFirst16DashesSet);
                // Will store: 00112233-4455-66xxxxxxxxxxxxxxxxxxxx
                Avx.Store((byte*)dest, first16CharsWithDashes);
                // Destination value after store: 00112233-4455-6677xxxxxxxxxxxxxxxxxx

                // Writing last 16 ASCII result chars with dashes
                Vector256<byte> formatDLast16DashesShuffleMask = s_formatDLast16DashesShuffleMask;
                Vector256<byte> formatDLast16DashesSet = s_formatDLast16DashesSet;
                Vector256<byte> formatDLast16RemainDashesShuffleMask = s_formatDLast16RemainDashesShuffleMask;
                Vector256<byte> formatDLast16RemainDashesSet = s_formatDLast16RemainDashesSet;

                Vector256<byte> rawLast16BytesChars = Avx2.ConvertToVector256Int16(asciiBytes.GetUpper()).AsByte();
                Vector256<byte> last16CharsWithDashes = Avx2.Or(
                    Avx2.Shuffle(rawLast16BytesChars, formatDLast16DashesShuffleMask),
                    formatDLast16DashesSet);
                // Will store: xxxxxxxxxxxxxxxxxx-8899-AABB______xx
                Avx.Store((byte*)dest + 36, last16CharsWithDashes);
                // Destination value after store: 00112233-4455-6677-8899-AABBxxxxxxxx

                Vector256<byte> last16CharsRemainWithDashes = Avx2.Or(
                    Avx2.Shuffle(rawLast16BytesChars, formatDLast16RemainDashesShuffleMask),
                    formatDLast16RemainDashesSet);
                // Will store: xxxxxxxxxxxxxxxxxxxx899-AABBCCDDEEFF
                Avx.Store((byte*)dest + 40, last16CharsRemainWithDashes);
                // Destination value after store: 00112233-4455-6677-8899-AABBCCDDEEFF
            }
        }

        private unsafe void FormatNAvx(short* dest)
        {
            // dddddddddddddddddddddddddddddddd
            fixed (Guid* thisPtr = &this)
            {
                Vector128<byte> layoutMask = s_layoutShiftMask;
                Vector256<byte> asciiTable = s_asciiTable;
                Vector128<byte> guidStringLayoutBytes = Ssse3.Shuffle(Sse2.LoadVector128((byte*)thisPtr), layoutMask);
                Vector256<short> guidStringLayoutUtf16 = Avx2.ConvertToVector256Int16(guidStringLayoutBytes);
                Vector256<byte> hi = Avx2.ShiftRightLogical(guidStringLayoutUtf16, 4).AsByte();
                Vector256<byte> lo = Avx2.ShiftLeftLogical(guidStringLayoutUtf16, 8).AsByte();
                Vector256<byte> asciiTableIndices = Avx2.And(Avx2.Or(hi, lo), Vector256.Create((byte)0x0F));
                Vector256<byte> asciiBytes = Avx2.Shuffle(asciiTable, asciiTableIndices);
                Avx.Store(dest, Avx2.ConvertToVector256Int16(asciiBytes.GetLower()));
                Avx.Store(dest + 16, Avx2.ConvertToVector256Int16(asciiBytes.GetUpper()));
            }
        }

        private unsafe void FormatD(char* dest)
        {
            // dddddddd-dddd-dddd-dddd-dddddddddddd
            dest[0] = HexConverter.ToCharLower((byte)(_a >> 28));
            dest[1] = HexConverter.ToCharLower((byte)(_a >> 24));
            dest[2] = HexConverter.ToCharLower((byte)(_a >> 20));
            dest[3] = HexConverter.ToCharLower((byte)(_a >> 16));
            dest[4] = HexConverter.ToCharLower((byte)(_a >> 12));
            dest[5] = HexConverter.ToCharLower((byte)(_a >> 8));
            dest[6] = HexConverter.ToCharLower((byte)(_a >> 4));
            dest[7] = HexConverter.ToCharLower((byte)_a);
            dest[8] = '-';
            dest[9] = HexConverter.ToCharLower((byte)(_b >> 12));
            dest[10] = HexConverter.ToCharLower((byte)(_b >> 8));
            dest[11] = HexConverter.ToCharLower((byte)(_b >> 4));
            dest[12] = HexConverter.ToCharLower((byte)_b);
            dest[13] = '-';
            dest[14] = HexConverter.ToCharLower((byte)(_c >> 12));
            dest[15] = HexConverter.ToCharLower((byte)(_c >> 8));
            dest[16] = HexConverter.ToCharLower((byte)(_c >> 4));
            dest[17] = HexConverter.ToCharLower((byte)_c);
            dest[18] = '-';
            dest[19] = HexConverter.ToCharLower((byte)(_d >> 4));
            dest[20] = HexConverter.ToCharLower(_d);
            dest[21] = HexConverter.ToCharLower((byte)(_e >> 4));
            dest[22] = HexConverter.ToCharLower(_e);
            dest[23] = '-';
            dest[24] = HexConverter.ToCharLower((byte)(_f >> 4));
            dest[25] = HexConverter.ToCharLower(_f);
            dest[26] = HexConverter.ToCharLower((byte)(_g >> 4));
            dest[27] = HexConverter.ToCharLower(_g);
            dest[28] = HexConverter.ToCharLower((byte)(_h >> 4));
            dest[29] = HexConverter.ToCharLower(_h);
            dest[30] = HexConverter.ToCharLower((byte)(_i >> 4));
            dest[31] = HexConverter.ToCharLower(_i);
            dest[32] = HexConverter.ToCharLower((byte)(_j >> 4));
            dest[33] = HexConverter.ToCharLower(_j);
            dest[34] = HexConverter.ToCharLower((byte)(_k >> 4));
            dest[35] = HexConverter.ToCharLower(_k);
        }

        private unsafe void FormatN(char* dest)
        {
            // dddddddddddddddddddddddddddddddd
            dest[0] = HexConverter.ToCharLower((byte)(_a >> 28));
            dest[1] = HexConverter.ToCharLower((byte)(_a >> 24));
            dest[2] = HexConverter.ToCharLower((byte)(_a >> 20));
            dest[3] = HexConverter.ToCharLower((byte)(_a >> 16));
            dest[4] = HexConverter.ToCharLower((byte)(_a >> 12));
            dest[5] = HexConverter.ToCharLower((byte)(_a >> 8));
            dest[6] = HexConverter.ToCharLower((byte)(_a >> 4));
            dest[7] = HexConverter.ToCharLower((byte)_a);
            dest[8] = HexConverter.ToCharLower((byte)(_b >> 12));
            dest[9] = HexConverter.ToCharLower((byte)(_b >> 8));
            dest[10] = HexConverter.ToCharLower((byte)(_b >> 4));
            dest[11] = HexConverter.ToCharLower((byte)_b);
            dest[12] = HexConverter.ToCharLower((byte)(_c >> 12));
            dest[13] = HexConverter.ToCharLower((byte)(_c >> 8));
            dest[14] = HexConverter.ToCharLower((byte)(_c >> 4));
            dest[15] = HexConverter.ToCharLower((byte)_c);
            dest[16] = HexConverter.ToCharLower((byte)(_d >> 4));
            dest[17] = HexConverter.ToCharLower(_d);
            dest[18] = HexConverter.ToCharLower((byte)(_e >> 4));
            dest[19] = HexConverter.ToCharLower(_e);
            dest[20] = HexConverter.ToCharLower((byte)(_f >> 4));
            dest[21] = HexConverter.ToCharLower(_f);
            dest[22] = HexConverter.ToCharLower((byte)(_g >> 4));
            dest[23] = HexConverter.ToCharLower(_g);
            dest[24] = HexConverter.ToCharLower((byte)(_h >> 4));
            dest[25] = HexConverter.ToCharLower(_h);
            dest[26] = HexConverter.ToCharLower((byte)(_i >> 4));
            dest[27] = HexConverter.ToCharLower(_i);
            dest[28] = HexConverter.ToCharLower((byte)(_j >> 4));
            dest[29] = HexConverter.ToCharLower(_j);
            dest[30] = HexConverter.ToCharLower((byte)(_k >> 4));
            dest[31] = HexConverter.ToCharLower(_k);
        }

        private unsafe void FormatX(char* dest)
        {
            // {0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}
            dest[0] = '{';
            dest[1] = '0';
            dest[2] = 'x';
            dest[3] = HexConverter.ToCharLower((byte)(_a >> 28));
            dest[4] = HexConverter.ToCharLower((byte)(_a >> 24));
            dest[5] = HexConverter.ToCharLower((byte)(_a >> 20));
            dest[6] = HexConverter.ToCharLower((byte)(_a >> 16));
            dest[7] = HexConverter.ToCharLower((byte)(_a >> 12));
            dest[8] = HexConverter.ToCharLower((byte)(_a >> 8));
            dest[9] = HexConverter.ToCharLower((byte)(_a >> 4));
            dest[10] = HexConverter.ToCharLower((byte)_a);
            dest[11] = ',';
            dest[12] = '0';
            dest[13] = 'x';
            dest[14] = HexConverter.ToCharLower((byte)(_b >> 12));
            dest[15] = HexConverter.ToCharLower((byte)(_b >> 8));
            dest[16] = HexConverter.ToCharLower((byte)(_b >> 4));
            dest[17] = HexConverter.ToCharLower((byte)_b);
            dest[18] = ',';
            dest[19] = '0';
            dest[20] = 'x';
            dest[21] = HexConverter.ToCharLower((byte)(_c >> 12));
            dest[22] = HexConverter.ToCharLower((byte)(_c >> 8));
            dest[23] = HexConverter.ToCharLower((byte)(_c >> 4));
            dest[24] = HexConverter.ToCharLower((byte)_c);
            dest[25] = ',';
            dest[26] = '{';
            dest[27] = '0';
            dest[28] = 'x';
            dest[29] = HexConverter.ToCharLower((byte)(_d >> 4));
            dest[30] = HexConverter.ToCharLower(_d);
            dest[31] = ',';
            dest[32] = '0';
            dest[33] = 'x';
            dest[34] = HexConverter.ToCharLower((byte)(_e >> 4));
            dest[35] = HexConverter.ToCharLower(_e);
            dest[36] = ',';
            dest[37] = '0';
            dest[38] = 'x';
            dest[39] = HexConverter.ToCharLower((byte)(_f >> 4));
            dest[40] = HexConverter.ToCharLower(_f);
            dest[41] = ',';
            dest[42] = '0';
            dest[43] = 'x';
            dest[44] = HexConverter.ToCharLower((byte)(_g >> 4));
            dest[45] = HexConverter.ToCharLower(_g);
            dest[46] = ',';
            dest[47] = '0';
            dest[48] = 'x';
            dest[49] = HexConverter.ToCharLower((byte)(_h >> 4));
            dest[50] = HexConverter.ToCharLower(_h);
            dest[51] = ',';
            dest[52] = '0';
            dest[53] = 'x';
            dest[54] = HexConverter.ToCharLower((byte)(_i >> 4));
            dest[55] = HexConverter.ToCharLower(_i);
            dest[56] = ',';
            dest[57] = '0';
            dest[58] = 'x';
            dest[59] = HexConverter.ToCharLower((byte)(_j >> 4));
            dest[60] = HexConverter.ToCharLower(_j);
            dest[61] = ',';
            dest[62] = '0';
            dest[63] = 'x';
            dest[64] = HexConverter.ToCharLower((byte)(_k >> 4));
            dest[65] = HexConverter.ToCharLower(_k);
            dest[66] = '}';
            dest[67] = '}';
        }
    }
}
