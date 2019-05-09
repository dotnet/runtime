// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        public static readonly Guid Empty = new Guid();

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

        public static bool TryParse(string input, out Guid result)
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
            bool success;
            switch ((char)(format[0] | 0x20))
            {
                case 'd':
                    success = TryParseExactD(input, ref result);
                    break;

                case 'n':
                    success = TryParseExactN(input, ref result);
                    break;

                case 'b':
                    success = TryParseExactB(input, ref result);
                    break;

                case 'p':
                    success = TryParseExactP(input, ref result);
                    break;

                case 'x':
                    success = TryParseExactX(input, ref result);
                    break;

                default:
                    throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
            }

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

            switch (guidString[0])
            {
                case '(':
                    return TryParseExactP(guidString, ref result);

                case '{':
                    return guidString.Contains('-') ?
                        TryParseExactB(guidString, ref result) :
                        TryParseExactX(guidString, ref result);

                default:
                    return guidString.Contains('-') ?
                        TryParseExactD(guidString, ref result) :
                        TryParseExactN(guidString, ref result);
            }
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

            uint uintTmp;
            if (TryParseHex(guidString.Slice(0, 8), out Unsafe.As<int, uint>(ref g._a)) && // _a
                TryParseHex(guidString.Slice(9, 4), out uintTmp)) // _b
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

            uint uintTmp;
            if (uint.TryParse(guidString.Slice(0, 8), NumberStyles.AllowHexSpecifier, null, out Unsafe.As<int, uint>(ref g._a)) && // _a
                uint.TryParse(guidString.Slice(8, 8), NumberStyles.AllowHexSpecifier, null, out uintTmp)) // _b, _c
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
                uint byteVal;
                if (!TryParseHex(guidString.Slice(numStart, numLen), out byteVal, ref overflow) || overflow || byteVal > byte.MaxValue)
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
            uint tmp;
            bool success = TryParseHex(guidString, out tmp, ref overflow);
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
            for (; i < guidString.Length && guidString[i] == '0'; i++);

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

        public static bool operator ==(Guid a, Guid b)
        {
            // Now compare each of the elements
            return a._a == b._a &&
                Unsafe.Add(ref a._a, 1) == Unsafe.Add(ref b._a, 1) &&
                Unsafe.Add(ref a._a, 2) == Unsafe.Add(ref b._a, 2) &&
                Unsafe.Add(ref a._a, 3) == Unsafe.Add(ref b._a, 3);
        }

        public static bool operator !=(Guid a, Guid b)
        {
            // Now compare each of the elements
            return a._a != b._a ||
                Unsafe.Add(ref a._a, 1) != Unsafe.Add(ref b._a, 1) ||
                Unsafe.Add(ref a._a, 2) != Unsafe.Add(ref b._a, 2) ||
                Unsafe.Add(ref a._a, 3) != Unsafe.Add(ref b._a, 3);
        }

        public string ToString(string? format)
        {
            return ToString(format, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char HexToChar(int a)
        {
            a = a & 0xf;
            return (char)((a > 9) ? a - 10 + 0x61 : a + 0x30);
        }

        private static unsafe int HexsToChars(char* guidChars, int a, int b)
        {
            guidChars[0] = HexToChar(a >> 4);
            guidChars[1] = HexToChar(a);

            guidChars[2] = HexToChar(b >> 4);
            guidChars[3] = HexToChar(b);

            return 4;
        }

        private static unsafe int HexsToCharsHexOutput(char* guidChars, int a, int b)
        {
            guidChars[0] = '0';
            guidChars[1] = 'x';

            guidChars[2] = HexToChar(a >> 4);
            guidChars[3] = HexToChar(a);

            guidChars[4] = ',';
            guidChars[5] = '0';
            guidChars[6] = 'x';

            guidChars[7] = HexToChar(b >> 4);
            guidChars[8] = HexToChar(b);

            return 9;
        }

        // IFormattable interface
        // We currently ignore provider
        public string ToString(string? format, IFormatProvider? provider)
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

            int guidSize;
            switch (format[0])
            {
                case 'D':
                case 'd':
                    guidSize = 36;
                    break;
                case 'N':
                case 'n':
                    guidSize = 32;
                    break;
                case 'B':
                case 'b':
                case 'P':
                case 'p':
                    guidSize = 38;
                    break;
                case 'X':
                case 'x':
                    guidSize = 68;
                    break;
                default:
                    throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
            }

            string guidString = string.FastAllocateString(guidSize);

            int bytesWritten;
            bool result = TryFormat(new Span<char>(ref guidString.GetRawStringData(), guidString.Length), out bytesWritten, format);
            Debug.Assert(result && bytesWritten == guidString.Length, "Formatting guid should have succeeded.");

            return guidString;
        }

        // Returns whether the guid is successfully formatted as a span. 
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
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

            bool dash = true;
            bool hex = false;
            int braces = 0;

            int guidSize;

            switch (format[0])
            {
                case 'D':
                case 'd':
                    guidSize = 36;
                    break;
                case 'N':
                case 'n':
                    dash = false;
                    guidSize = 32;
                    break;
                case 'B':
                case 'b':
                    braces = '{' + ('}' << 16);
                    guidSize = 38;
                    break;
                case 'P':
                case 'p':
                    braces = '(' + (')' << 16);
                    guidSize = 38;
                    break;
                case 'X':
                case 'x':
                    braces = '{' + ('}' << 16);
                    dash = false;
                    hex = true;
                    guidSize = 68;
                    break;
                default:
                    throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
            }

            if (destination.Length < guidSize)
            {
                charsWritten = 0;
                return false;
            }

            unsafe
            {
                fixed (char* guidChars = &MemoryMarshal.GetReference(destination))
                {
                    char * p = guidChars;

                    if (braces != 0)
                        *p++ = (char)braces;

                    if (hex)
                    {
                        // {0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}
                        *p++ = '0';
                        *p++ = 'x';
                        p += HexsToChars(p, _a >> 24, _a >> 16);
                        p += HexsToChars(p, _a >> 8, _a);
                        *p++ = ',';
                        *p++ = '0';
                        *p++ = 'x';
                        p += HexsToChars(p, _b >> 8, _b);
                        *p++ = ',';
                        *p++ = '0';
                        *p++ = 'x';
                        p += HexsToChars(p, _c >> 8, _c);
                        *p++ = ',';
                        *p++ = '{';
                        p += HexsToCharsHexOutput(p, _d, _e);
                        *p++ = ',';
                        p += HexsToCharsHexOutput(p, _f, _g);
                        *p++ = ',';
                        p += HexsToCharsHexOutput(p, _h, _i);
                        *p++ = ',';
                        p += HexsToCharsHexOutput(p, _j, _k);
                        *p++ = '}';
                    }
                    else
                    {
                        // [{|(]dddddddd[-]dddd[-]dddd[-]dddd[-]dddddddddddd[}|)]
                        p += HexsToChars(p, _a >> 24, _a >> 16);
                        p += HexsToChars(p, _a >> 8, _a);
                        if (dash)
                            *p++ = '-';
                        p += HexsToChars(p, _b >> 8, _b);
                        if (dash)
                            *p++ = '-';
                        p += HexsToChars(p, _c >> 8, _c);
                        if (dash)
                            *p++ = '-';
                        p += HexsToChars(p, _d, _e);
                        if (dash)
                            *p++ = '-';
                        p += HexsToChars(p, _f, _g);
                        p += HexsToChars(p, _h, _i);
                        p += HexsToChars(p, _j, _k);
                    }

                    if (braces != 0)
                        *p++ = (char)(braces >> 16);

                    Debug.Assert(p - guidChars == guidSize);
                }
            }

            charsWritten = guidSize;
            return true;
        }

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            // Like with the IFormattable implementation, provider is ignored.
            return TryFormat(destination, out charsWritten, format);
        }
    }
}
