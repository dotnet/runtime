// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime.CompilerServices;

namespace System
{
    // Represents a Globally Unique Identifier.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [Runtime.Versioning.NonVersionable] // This only applies to field layout
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial struct Guid : IFormattable, IComparable, IComparable<Guid>, IEquatable<Guid>, ISpanFormattable
    {
        public static readonly Guid Empty = new Guid();

        ////////////////////////////////////////////////////////////////////////////////
        //  Member variables
        ////////////////////////////////////////////////////////////////////////////////
        private int _a; // Do not rename (binary serialization)
        private short _b; // Do not rename (binary serialization)
        private short _c; // Do not rename (binary serialization)
        private byte _d; // Do not rename (binary serialization)
        private byte _e; // Do not rename (binary serialization)
        private byte _f; // Do not rename (binary serialization)
        private byte _g; // Do not rename (binary serialization)
        private byte _h; // Do not rename (binary serialization)
        private byte _i; // Do not rename (binary serialization)
        private byte _j; // Do not rename (binary serialization)
        private byte _k; // Do not rename (binary serialization)

        ////////////////////////////////////////////////////////////////////////////////
        //  Constructors
        ////////////////////////////////////////////////////////////////////////////////

        // Creates a new guid from an array of bytes.
        public Guid(byte[] b) :
            this(new ReadOnlySpan<byte>(b ?? throw new ArgumentNullException(nameof(b))))
        {
        }

        // Creates a new guid from a read-only span.
        public Guid(ReadOnlySpan<byte> b)
        {
            if (b.Length != 16)
                throw new ArgumentException(SR.Format(SR.Arg_GuidArrayCtor, "16"), nameof(b));

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
            _k = b[15];
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
        //
        public Guid(int a, short b, short c, byte[] d)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));
            // Check that array is not too big
            if (d.Length != 8)
                throw new ArgumentException(SR.Format(SR.Arg_GuidArrayCtor, "8"), nameof(d));

            _a = a;
            _b = b;
            _c = c;
            _d = d[0];
            _e = d[1];
            _f = d[2];
            _g = d[3];
            _h = d[4];
            _i = d[5];
            _j = d[6];
            _k = d[7];
        }

        // Creates a new GUID initialized to the value represented by the
        // arguments.  The bytes are specified like this to avoid endianness issues.
        //
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

        [Flags]
        private enum GuidStyles
        {
            None = 0x00000000,
            AllowParenthesis = 0x00000001, //Allow the guid to be enclosed in parens
            AllowBraces = 0x00000002, //Allow the guid to be enclosed in braces
            AllowDashes = 0x00000004, //Allow the guid to contain dash group separators
            AllowHexPrefix = 0x00000008, //Allow the guid to contain {0xdd,0xdd}
            RequireParenthesis = 0x00000010, //Require the guid to be enclosed in parens
            RequireBraces = 0x00000020, //Require the guid to be enclosed in braces
            RequireDashes = 0x00000040, //Require the guid to contain dash group separators
            RequireHexPrefix = 0x00000080, //Require the guid to contain {0xdd,0xdd}

            HexFormat = RequireBraces | RequireHexPrefix,                      /* X */
            NumberFormat = None,                                                  /* N */
            DigitFormat = RequireDashes,                                         /* D */
            BraceFormat = RequireBraces | RequireDashes,                         /* B */
            ParenthesisFormat = RequireParenthesis | RequireDashes,                    /* P */

            Any = AllowParenthesis | AllowBraces | AllowDashes | AllowHexPrefix,
        }
        private enum GuidParseThrowStyle
        {
            None = 0,
            All = 1,
            AllButOverflow = 2
        }
        private enum ParseFailureKind
        {
            None = 0,
            ArgumentNull = 1,
            Format = 2,
            FormatWithParameter = 3,
            NativeException = 4,
            FormatWithInnerException = 5
        }

        // This will store the result of the parsing.  And it will eventually be used to construct a Guid instance.
        private struct GuidResult
        {
            internal Guid _parsedGuid;
            internal GuidParseThrowStyle _throwStyle;

            private ParseFailureKind _failure;
            private string _failureMessageID;
            private object _failureMessageFormatArgument;
            private string _failureArgumentName;
            private Exception _innerException;

            internal void Init(GuidParseThrowStyle canThrow)
            {
                _throwStyle = canThrow;
            }

            internal void SetFailure(Exception nativeException)
            {
                _failure = ParseFailureKind.NativeException;
                _innerException = nativeException;
            }

            internal void SetFailure(ParseFailureKind failure, string failureMessageID)
            {
                SetFailure(failure, failureMessageID, null, null, null);
            }

            internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument)
            {
                SetFailure(failure, failureMessageID, failureMessageFormatArgument, null, null);
            }

            internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument,
                                     string failureArgumentName, Exception innerException)
            {
                Debug.Assert(failure != ParseFailureKind.NativeException, "ParseFailureKind.NativeException should not be used with this overload");
                _failure = failure;
                _failureMessageID = failureMessageID;
                _failureMessageFormatArgument = failureMessageFormatArgument;
                _failureArgumentName = failureArgumentName;
                _innerException = innerException;
                if (_throwStyle != GuidParseThrowStyle.None)
                {
                    throw GetGuidParseException();
                }
            }

            internal Exception GetGuidParseException()
            {
                switch (_failure)
                {
                    case ParseFailureKind.ArgumentNull:
                        return new ArgumentNullException(_failureArgumentName, SR.GetResourceString(_failureMessageID));

                    case ParseFailureKind.FormatWithInnerException:
                        return new FormatException(SR.GetResourceString(_failureMessageID), _innerException);

                    case ParseFailureKind.FormatWithParameter:
                        return new FormatException(SR.Format(SR.GetResourceString(_failureMessageID), _failureMessageFormatArgument));

                    case ParseFailureKind.Format:
                        return new FormatException(SR.GetResourceString(_failureMessageID));

                    case ParseFailureKind.NativeException:
                        return _innerException;

                    default:
                        Debug.Fail("Unknown GuidParseFailure: " + _failure);
                        return new FormatException(SR.Format_GuidUnrecognized);
                }
            }
        }

        // Creates a new guid based on the value in the string.  The value is made up
        // of hex digits speared by the dash ("-"). The string may begin and end with
        // brackets ("{", "}").
        //
        // The string must be of the form dddddddd-dddd-dddd-dddd-dddddddddddd. where
        // d is a hex digit. (That is 8 hex digits, followed by 4, then 4, then 4,
        // then 12) such as: "CA761232-ED42-11CE-BACD-00AA0057B223"
        //
        public Guid(string g)
        {
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            GuidResult result = new GuidResult();
            result.Init(GuidParseThrowStyle.All);
            if (TryParseGuid(g, GuidStyles.Any, ref result))
            {
                this = result._parsedGuid;
            }
            else
            {
                throw result.GetGuidParseException();
            }
        }

        public static Guid Parse(string input) =>
            Parse(input != null ? (ReadOnlySpan<char>)input : throw new ArgumentNullException(nameof(input)));

        public static Guid Parse(ReadOnlySpan<char> input)
        {
            GuidResult result = new GuidResult();
            result.Init(GuidParseThrowStyle.AllButOverflow);
            if (TryParseGuid(input, GuidStyles.Any, ref result))
            {
                return result._parsedGuid;
            }
            else
            {
                throw result.GetGuidParseException();
            }
        }

        public static bool TryParse(string input, out Guid result)
        {
            if (input == null)
            {
                result = default(Guid);
                return false;
            }

            return TryParse((ReadOnlySpan<char>)input, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> input, out Guid result)
        {
            GuidResult parseResult = new GuidResult();
            parseResult.Init(GuidParseThrowStyle.None);
            if (TryParseGuid(input, GuidStyles.Any, ref parseResult))
            {
                result = parseResult._parsedGuid;
                return true;
            }
            else
            {
                result = default(Guid);
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

            GuidStyles style;
            switch (format[0])
            {
                case 'D':
                case 'd':
                    style = GuidStyles.DigitFormat;
                    break;
                case 'N':
                case 'n':
                    style = GuidStyles.NumberFormat;
                    break;
                case 'B':
                case 'b':
                    style = GuidStyles.BraceFormat;
                    break;
                case 'P':
                case 'p':
                    style = GuidStyles.ParenthesisFormat;
                    break;
                case 'X':
                case 'x':
                    style = GuidStyles.HexFormat;
                    break;
                default:
                    throw new FormatException(SR.Format_InvalidGuidFormatSpecification);
            }

            GuidResult result = new GuidResult();
            result.Init(GuidParseThrowStyle.AllButOverflow);
            if (TryParseGuid(input, style, ref result))
            {
                return result._parsedGuid;
            }
            else
            {
                throw result.GetGuidParseException();
            }
        }

        public static bool TryParseExact(string input, string format, out Guid result)
        {
            if (input == null)
            {
                result = default(Guid);
                return false;
            }

            return TryParseExact((ReadOnlySpan<char>)input, format, out result);
        }

        public static bool TryParseExact(ReadOnlySpan<char> input, ReadOnlySpan<char> format, out Guid result)
        {
            if (format.Length != 1)
            {
                result = default(Guid);
                return false;
            }

            GuidStyles style;
            switch (format[0])
            {
                case 'D':
                case 'd':
                    style = GuidStyles.DigitFormat;
                    break;
                case 'N':
                case 'n':
                    style = GuidStyles.NumberFormat;
                    break;
                case 'B':
                case 'b':
                    style = GuidStyles.BraceFormat;
                    break;
                case 'P':
                case 'p':
                    style = GuidStyles.ParenthesisFormat;
                    break;
                case 'X':
                case 'x':
                    style = GuidStyles.HexFormat;
                    break;
                default:
                    // invalid guid format specification
                    result = default(Guid);
                    return false;
            }

            GuidResult parseResult = new GuidResult();
            parseResult.Init(GuidParseThrowStyle.None);
            if (TryParseGuid(input, style, ref parseResult))
            {
                result = parseResult._parsedGuid;
                return true;
            }
            else
            {
                result = default(Guid);
                return false;
            }
        }

        private static bool TryParseGuid(ReadOnlySpan<char> guidString, GuidStyles flags, ref GuidResult result)
        {
            guidString = guidString.Trim(); // Remove whitespace from beginning and end

            if (guidString.Length == 0)
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidUnrecognized));
                return false;
            }

            // Check for dashes
            bool dashesExistInString = guidString.IndexOf('-') >= 0;

            if (dashesExistInString)
            {
                if ((flags & (GuidStyles.AllowDashes | GuidStyles.RequireDashes)) == 0)
                {
                    // dashes are not allowed
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidUnrecognized));
                    return false;
                }
            }
            else
            {
                if ((flags & GuidStyles.RequireDashes) != 0)
                {
                    // dashes are required
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidUnrecognized));
                    return false;
                }
            }

            // Check for braces
            bool bracesExistInString = (guidString.IndexOf('{', 0) >= 0);

            if (bracesExistInString)
            {
                if ((flags & (GuidStyles.AllowBraces | GuidStyles.RequireBraces)) == 0)
                {
                    // braces are not allowed
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidUnrecognized));
                    return false;
                }
            }
            else
            {
                if ((flags & GuidStyles.RequireBraces) != 0)
                {
                    // braces are required
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidUnrecognized));
                    return false;
                }
            }

            // Check for parenthesis
            bool parenthesisExistInString = (guidString.IndexOf('(', 0) >= 0);

            if (parenthesisExistInString)
            {
                if ((flags & (GuidStyles.AllowParenthesis | GuidStyles.RequireParenthesis)) == 0)
                {
                    // parenthesis are not allowed
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidUnrecognized));
                    return false;
                }
            }
            else
            {
                if ((flags & GuidStyles.RequireParenthesis) != 0)
                {
                    // parenthesis are required
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidUnrecognized));
                    return false;
                }
            }

            try
            {
                // let's get on with the parsing
                if (dashesExistInString)
                {
                    // Check if it's of the form [{|(]dddddddd-dddd-dddd-dddd-dddddddddddd[}|)]
                    return TryParseGuidWithDashes(guidString, ref result);
                }
                else if (bracesExistInString)
                {
                    // Check if it's of the form {0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}
                    return TryParseGuidWithHexPrefix(guidString, ref result);
                }
                else
                {
                    // Check if it's of the form dddddddddddddddddddddddddddddddd
                    return TryParseGuidWithNoStyle(guidString, ref result);
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                result.SetFailure(ParseFailureKind.FormatWithInnerException, nameof(SR.Format_GuidUnrecognized), null, null, ex);
                return false;
            }
            catch (ArgumentException ex)
            {
                result.SetFailure(ParseFailureKind.FormatWithInnerException, nameof(SR.Format_GuidUnrecognized), null, null, ex);
                return false;
            }
        }

        // Check if it's of the form {0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}
        private static bool TryParseGuidWithHexPrefix(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            int numStart = 0;
            int numLen = 0;

            // Eat all of the whitespace
            guidString = EatAllWhitespace(guidString);

            // Check for leading '{'
            if (guidString.Length == 0 || guidString[0] != '{')
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidBrace));
                return false;
            }

            // Check for '0x'
            if (!IsHexPrefix(guidString, 1))
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidHexPrefix), "{0xdddddddd, etc}");
                return false;
            }

            // Find the end of this hex number (since it is not fixed length)
            numStart = 3;
            numLen = guidString.IndexOf(',', numStart) - numStart;
            if (numLen <= 0)
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidComma));
                return false;
            }

            if (!StringToInt(guidString.Slice(numStart, numLen) /*first DWORD*/, -1, ParseNumbers.IsTight, out result._parsedGuid._a, ref result))
                return false;

            // Check for '0x'
            if (!IsHexPrefix(guidString, numStart + numLen + 1))
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidHexPrefix), "{0xdddddddd, 0xdddd, etc}");
                return false;
            }
            // +3 to get by ',0x'
            numStart = numStart + numLen + 3;
            numLen = guidString.IndexOf(',', numStart) - numStart;
            if (numLen <= 0)
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidComma));
                return false;
            }

            // Read in the number
            if (!StringToShort(guidString.Slice(numStart, numLen) /*first DWORD*/, -1, ParseNumbers.IsTight, out result._parsedGuid._b, ref result))
                return false;
            // Check for '0x'
            if (!IsHexPrefix(guidString, numStart + numLen + 1))
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidHexPrefix), "{0xdddddddd, 0xdddd, 0xdddd, etc}");
                return false;
            }
            // +3 to get by ',0x'
            numStart = numStart + numLen + 3;
            numLen = guidString.IndexOf(',', numStart) - numStart;
            if (numLen <= 0)
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidComma));
                return false;
            }

            // Read in the number
            if (!StringToShort(guidString.Slice(numStart, numLen) /*first DWORD*/, -1, ParseNumbers.IsTight, out result._parsedGuid._c, ref result))
                return false;

            // Check for '{'
            if (guidString.Length <= numStart + numLen + 1 || guidString[numStart + numLen + 1] != '{')
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidBrace));
                return false;
            }

            // Prepare for loop
            numLen++;
            Span<byte> bytes = stackalloc byte[8];

            for (int i = 0; i < bytes.Length; i++)
            {
                // Check for '0x'
                if (!IsHexPrefix(guidString, numStart + numLen + 1))
                {
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidHexPrefix), "{... { ... 0xdd, ...}}");
                    return false;
                }

                // +3 to get by ',0x' or '{0x' for first case
                numStart = numStart + numLen + 3;

                // Calculate number length
                if (i < 7)  // first 7 cases
                {
                    numLen = guidString.IndexOf(',', numStart) - numStart;
                    if (numLen <= 0)
                    {
                        result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidComma));
                        return false;
                    }
                }
                else       // last case ends with '}', not ','
                {
                    numLen = guidString.IndexOf('}', numStart) - numStart;
                    if (numLen <= 0)
                    {
                        result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidBraceAfterLastNumber));
                        return false;
                    }
                }

                // Read in the number
                int signedNumber;
                if (!StringToInt(guidString.Slice(numStart, numLen), -1, ParseNumbers.IsTight, out signedNumber, ref result))
                {
                    return false;
                }
                uint number = (uint)signedNumber;

                // check for overflow
                if (number > 255)
                {
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Overflow_Byte));
                    return false;
                }
                bytes[i] = (byte)number;
            }

            result._parsedGuid._d = bytes[0];
            result._parsedGuid._e = bytes[1];
            result._parsedGuid._f = bytes[2];
            result._parsedGuid._g = bytes[3];
            result._parsedGuid._h = bytes[4];
            result._parsedGuid._i = bytes[5];
            result._parsedGuid._j = bytes[6];
            result._parsedGuid._k = bytes[7];

            // Check for last '}'
            if (numStart + numLen + 1 >= guidString.Length || guidString[numStart + numLen + 1] != '}')
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidEndBrace));
                return false;
            }

            // Check if we have extra characters at the end
            if (numStart + numLen + 1 != guidString.Length - 1)
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_ExtraJunkAtEnd));
                return false;
            }

            return true;
        }

        // Check if it's of the form dddddddddddddddddddddddddddddddd
        private static bool TryParseGuidWithNoStyle(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            int startPos = 0;
            int temp;
            long templ;
            int currentPos = 0;

            if (guidString.Length != 32)
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidInvLen));
                return false;
            }

            for (int i = 0; i < guidString.Length; i++)
            {
                char ch = guidString[i];
                if (ch >= '0' && ch <= '9')
                {
                    continue;
                }
                else
                {
                    char upperCaseCh = char.ToUpperInvariant(ch);
                    if (upperCaseCh >= 'A' && upperCaseCh <= 'F')
                    {
                        continue;
                    }
                }

                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidInvalidChar));
                return false;
            }

            if (!StringToInt(guidString.Slice(startPos, 8) /*first DWORD*/, -1, ParseNumbers.IsTight, out result._parsedGuid._a, ref result))
                return false;

            startPos += 8;
            if (!StringToShort(guidString.Slice(startPos, 4), -1, ParseNumbers.IsTight, out result._parsedGuid._b, ref result))
                return false;

            startPos += 4;
            if (!StringToShort(guidString.Slice(startPos, 4), -1, ParseNumbers.IsTight, out result._parsedGuid._c, ref result))
                return false;

            startPos += 4;
            if (!StringToInt(guidString.Slice(startPos, 4), -1, ParseNumbers.IsTight, out temp, ref result))
                return false;

            startPos += 4;
            currentPos = startPos;

            if (!StringToLong(guidString, ref currentPos, ParseNumbers.NoSpace, out templ, ref result))
                return false;

            if (currentPos - startPos != 12)
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidInvLen));
                return false;
            }

            result._parsedGuid._d = (byte)(temp >> 8);
            result._parsedGuid._e = (byte)(temp);
            temp = (int)(templ >> 32);
            result._parsedGuid._f = (byte)(temp >> 8);
            result._parsedGuid._g = (byte)(temp);
            temp = (int)(templ);
            result._parsedGuid._h = (byte)(temp >> 24);
            result._parsedGuid._i = (byte)(temp >> 16);
            result._parsedGuid._j = (byte)(temp >> 8);
            result._parsedGuid._k = (byte)(temp);

            return true;
        }

        // Check if it's of the form [{|(]dddddddd-dddd-dddd-dddd-dddddddddddd[}|)]
        private static bool TryParseGuidWithDashes(ReadOnlySpan<char> guidString, ref GuidResult result)
        {
            int startPos = 0;
            int temp;
            long templ;
            int currentPos = 0;

            // check to see that it's the proper length
            if (guidString[0] == '{')
            {
                if (guidString.Length != 38 || guidString[37] != '}')
                {
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidInvLen));
                    return false;
                }
                startPos = 1;
            }
            else if (guidString[0] == '(')
            {
                if (guidString.Length != 38 || guidString[37] != ')')
                {
                    result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidInvLen));
                    return false;
                }
                startPos = 1;
            }
            else if (guidString.Length != 36)
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidInvLen));
                return false;
            }

            if (guidString[8 + startPos] != '-' ||
                guidString[13 + startPos] != '-' ||
                guidString[18 + startPos] != '-' ||
                guidString[23 + startPos] != '-')
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidDashes));
                return false;
            }

            currentPos = startPos;
            if (!StringToInt(guidString, ref currentPos, 8, ParseNumbers.NoSpace, out temp, ref result))
                return false;
            result._parsedGuid._a = temp;
            ++currentPos; //Increment past the '-';

            if (!StringToInt(guidString, ref currentPos, 4, ParseNumbers.NoSpace, out temp, ref result))
                return false;
            result._parsedGuid._b = (short)temp;
            ++currentPos; //Increment past the '-';

            if (!StringToInt(guidString, ref currentPos, 4, ParseNumbers.NoSpace, out temp, ref result))
                return false;
            result._parsedGuid._c = (short)temp;
            ++currentPos; //Increment past the '-';

            if (!StringToInt(guidString, ref currentPos, 4, ParseNumbers.NoSpace, out temp, ref result))
                return false;
            ++currentPos; //Increment past the '-';
            startPos = currentPos;

            if (!StringToLong(guidString, ref currentPos, ParseNumbers.NoSpace, out templ, ref result))
                return false;

            if (currentPos - startPos != 12)
            {
                result.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidInvLen));
                return false;
            }
            result._parsedGuid._d = (byte)(temp >> 8);
            result._parsedGuid._e = (byte)(temp);
            temp = (int)(templ >> 32);
            result._parsedGuid._f = (byte)(temp >> 8);
            result._parsedGuid._g = (byte)(temp);
            temp = (int)(templ);
            result._parsedGuid._h = (byte)(temp >> 24);
            result._parsedGuid._i = (byte)(temp >> 16);
            result._parsedGuid._j = (byte)(temp >> 8);
            result._parsedGuid._k = (byte)(temp);

            return true;
        }

        private static bool StringToShort(ReadOnlySpan<char> str, int requiredLength, int flags, out short result, ref GuidResult parseResult)
        {
            int parsePos = 0;
            return StringToShort(str, ref parsePos, requiredLength, flags, out result, ref parseResult);
        }

        private static bool StringToShort(ReadOnlySpan<char> str, ref int parsePos, int requiredLength, int flags, out short result, ref GuidResult parseResult)
        {
            result = 0;
            int x;
            bool retValue = StringToInt(str, ref parsePos, requiredLength, flags, out x, ref parseResult);
            result = (short)x;
            return retValue;
        }

        private static bool StringToInt(ReadOnlySpan<char> str, int requiredLength, int flags, out int result, ref GuidResult parseResult)
        {
            int parsePos = 0;
            return StringToInt(str, ref parsePos, requiredLength, flags, out result, ref parseResult);
        }

        private static bool StringToInt(ReadOnlySpan<char> str, ref int parsePos, int requiredLength, int flags, out int result, ref GuidResult parseResult)
        {
            result = 0;

            int currStart = parsePos;
            try
            {
                result = ParseNumbers.StringToInt(str, 16, flags, ref parsePos);
            }
            catch (OverflowException ex)
            {
                if (parseResult._throwStyle == GuidParseThrowStyle.All)
                {
                    throw;
                }
                else if (parseResult._throwStyle == GuidParseThrowStyle.AllButOverflow)
                {
                    throw new FormatException(SR.Format_GuidUnrecognized, ex);
                }
                else
                {
                    parseResult.SetFailure(ex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (parseResult._throwStyle == GuidParseThrowStyle.None)
                {
                    parseResult.SetFailure(ex);
                    return false;
                }
                else
                {
                    throw;
                }
            }

            //If we didn't parse enough characters, there's clearly an error.
            if (requiredLength != -1 && parsePos - currStart != requiredLength)
            {
                parseResult.SetFailure(ParseFailureKind.Format, nameof(SR.Format_GuidInvalidChar));
                return false;
            }
            return true;
        }

        private static unsafe bool StringToLong(ReadOnlySpan<char> str, ref int parsePos, int flags, out long result, ref GuidResult parseResult)
        {
            result = 0;

            try
            {
                result = ParseNumbers.StringToLong(str, 16, flags, ref parsePos);
            }
            catch (OverflowException ex)
            {
                if (parseResult._throwStyle == GuidParseThrowStyle.All)
                {
                    throw;
                }
                else if (parseResult._throwStyle == GuidParseThrowStyle.AllButOverflow)
                {
                    throw new FormatException(SR.Format_GuidUnrecognized, ex);
                }
                else
                {
                    parseResult.SetFailure(ex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (parseResult._throwStyle == GuidParseThrowStyle.None)
                {
                    parseResult.SetFailure(ex);
                    return false;
                }
                else
                {
                    throw;
                }
            }
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
            (str[i + 1] == 'x' || char.ToLowerInvariant(str[i + 1]) == 'x');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteByteHelper(Span<byte> destination)
        {
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
            destination[15] = _k;
        }

        // Returns an unsigned byte array containing the GUID.
        public byte[] ToByteArray()
        {
            var g = new byte[16];
            WriteByteHelper(g);
            return g;
        }

        // Returns whether bytes are sucessfully written to given span.
        public bool TryWriteBytes(Span<byte> destination)
        {
            if (destination.Length < 16)
                return false;

            WriteByteHelper(destination);
            return true;
        }

        // Returns the guid in "registry" format.
        public override string ToString()
        {
            return ToString("D", null);
        }

        public override int GetHashCode()
        {
            // Simply XOR all the bits of the GUID 32 bits at a time.
            return _a ^ Unsafe.Add(ref _a, 1) ^ Unsafe.Add(ref _a, 2) ^ Unsafe.Add(ref _a, 3);
        }

        // Returns true if and only if the guid represented
        //  by o is the same as this instance.
        public override bool Equals(object o)
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

        private int GetResult(uint me, uint them)
        {
            if (me < them)
            {
                return -1;
            }
            return 1;
        }

        public int CompareTo(object value)
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

        public string ToString(string format)
        {
            return ToString(format, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char HexToChar(int a)
        {
            a = a & 0xf;
            return (char)((a > 9) ? a - 10 + 0x61 : a + 0x30);
        }

        unsafe private static int HexsToChars(char* guidChars, int a, int b)
        {
            guidChars[0] = HexToChar(a >> 4);
            guidChars[1] = HexToChar(a);

            guidChars[2] = HexToChar(b >> 4);
            guidChars[3] = HexToChar(b);

            return 4;
        }

        unsafe private static int HexsToCharsHexOutput(char* guidChars, int a, int b)
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
        public string ToString(string format, IFormatProvider provider)
        {
            if (format == null || format.Length == 0)
                format = "D";

            // all acceptable format strings are of length 1
            if (format.Length != 1)
                throw new FormatException(SR.Format_InvalidGuidFormatSpecification);

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
                format = "D";

            // all acceptable format strings are of length 1
            if (format.Length != 1) 
                throw new FormatException(SR.Format_InvalidGuidFormatSpecification);

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

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
        {
            // Like with the IFormattable implementation, provider is ignored.
            return TryFormat(destination, out charsWritten, format);
        }
    }
}
