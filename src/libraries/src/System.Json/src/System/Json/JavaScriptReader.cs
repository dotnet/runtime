// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Runtime.Serialization.Json
{
    internal class JavaScriptReader
    {
        private readonly TextReader _r;
        private int _line = 1, _column = 0;
        private int _peek;
        private bool _has_peek;
        private bool _prev_lf;

        public JavaScriptReader(TextReader reader)
        {
            Debug.Assert(reader != null);

            _r = reader;
        }

        public object Read()
        {
            object v = ReadCore();
            SkipSpaces();
            if (ReadChar() >= 0)
            {
                throw JsonError(SR.ArgumentException_ExtraCharacters);
            }
            return v;
        }

        private object ReadCore()
        {
            SkipSpaces();
            int c = PeekChar();
            if (c < 0)
            {
                throw JsonError(SR.ArgumentException_IncompleteInput);
            }

            switch (c)
            {
                case '[':
                    ReadChar();
                    var list = new List<object>();
                    SkipSpaces();
                    if (PeekChar() == ']')
                    {
                        ReadChar();
                        return list;
                    }

                    while (true)
                    {
                        list.Add(ReadCore());
                        SkipSpaces();
                        c = PeekChar();
                        if (c != ',')
                            break;
                        ReadChar();
                        continue;
                    }

                    if (ReadChar() != ']')
                    {
                        throw JsonError(SR.ArgumentException_ArrayMustEndWithBracket);
                    }

                    return list.ToArray();

                case '{':
                    ReadChar();
                    var obj = new Dictionary<string, object>();
                    SkipSpaces();
                    if (PeekChar() == '}')
                    {
                        ReadChar();
                        return obj;
                    }

                    while (true)
                    {
                        SkipSpaces();
                        if (PeekChar() == '}')
                        {
                            ReadChar();
                            break;
                        }
                        string name = ReadStringLiteral();
                        SkipSpaces();
                        Expect(':');
                        SkipSpaces();
                        obj[name] = ReadCore(); // it does not reject duplicate names.
                        SkipSpaces();
                        c = ReadChar();
                        if (c == ',')
                        {
                            continue;
                        }
                        if (c == '}')
                        {
                            break;
                        }
                    }
                    return obj.ToArray();

                case 't':
                    Expect("true");
                    return true;

                case 'f':
                    Expect("false");
                    return false;

                case 'n':
                    Expect("null");
                    return null;

                case '"':
                    return ReadStringLiteral();

                default:
                    if ('0' <= c && c <= '9' || c == '-')
                    {
                        return ReadNumericLiteral();
                    }
                    throw JsonError(SR.Format(SR.ArgumentException_UnexpectedCharacter, (char)c));
            }
        }

        private int PeekChar()
        {
            if (!_has_peek)
            {
                _peek = _r.Read();
                _has_peek = true;
            }
            return _peek;
        }

        private int ReadChar()
        {
            int v = _has_peek ? _peek : _r.Read();

            _has_peek = false;

            if (_prev_lf)
            {
                _line++;
                _column = 0;
                _prev_lf = false;
            }

            if (v == '\n')
            {
                _prev_lf = true;
            }

            _column++;

            return v;
        }

        private void SkipSpaces()
        {
            while (true)
            {
                switch (PeekChar())
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        ReadChar();
                        continue;

                    default:
                        return;
                }
            }
        }

        // It could return either int, long, ulong, decimal or double, depending on the parsed value.
        private object ReadNumericLiteral()
        {
            var sb = new StringBuilder();

            if (PeekChar() == '-')
            {
                sb.Append((char)ReadChar());
            }

            int c;
            int x = 0;
            bool zeroStart = PeekChar() == '0';
            for (; ; x++)
            {
                c = PeekChar();
                if (c < '0' || '9' < c)
                {
                    break;
                }

                sb.Append((char)ReadChar());
                if (zeroStart && x == 1)
                {
                    throw JsonError(SR.ArgumentException_LeadingZeros);
                }
            }

            if (x == 0) // Reached e.g. for "- "
            {
                throw JsonError(SR.ArgumentException_NoDigitFound);
            }

            // fraction
            bool hasFrac = false;
            int fdigits = 0;
            if (PeekChar() == '.')
            {
                hasFrac = true;
                sb.Append((char)ReadChar());
                if (PeekChar() < 0)
                {
                    throw JsonError(SR.ArgumentException_ExtraDot);
                }

                while (true)
                {
                    c = PeekChar();
                    if (c < '0' || '9' < c)
                    {
                        break;
                    }

                    sb.Append((char)ReadChar());
                    fdigits++;
                }
                if (fdigits == 0)
                {
                    throw JsonError(SR.ArgumentException_ExtraDot);
                }
            }

            c = PeekChar();
            if (c != 'e' && c != 'E')
            {
                if (!hasFrac)
                {
                    int valueInt;
                    if (int.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out valueInt))
                    {
                        return valueInt;
                    }

                    long valueLong;
                    if (long.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out valueLong))
                    {
                        return valueLong;
                    }

                    ulong valueUlong;
                    if (ulong.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out valueUlong))
                    {
                        return valueUlong;
                    }
                }

                decimal valueDecimal;
                if (decimal.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out valueDecimal) && valueDecimal != 0)
                {
                    return valueDecimal;
                }
            }
            else
            {
                // exponent
                sb.Append((char)ReadChar());
                if (PeekChar() < 0)
                {
                    throw JsonError(SR.ArgumentException_IncompleteExponent);
                }

                c = PeekChar();
                if (c == '-')
                {
                    sb.Append((char)ReadChar());
                }
                else if (c == '+')
                {
                    sb.Append((char)ReadChar());
                }

                if (PeekChar() < 0)
                {
                    throw JsonError(SR.ArgumentException_IncompleteExponent);
                }

                while (true)
                {
                    c = PeekChar();
                    if (c < '0' || '9' < c)
                    {
                        break;
                    }

                    sb.Append((char)ReadChar());
                }
            }

            return double.Parse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private readonly StringBuilder _vb = new StringBuilder();

        private string ReadStringLiteral()
        {
            if (PeekChar() != '"')
            {
                throw JsonError(SR.ArgumentException_InvalidLiteralFormat);
            }

            ReadChar();
            _vb.Length = 0;
            while (true)
            {
                int c = ReadChar();
                if (c < 0)
                {
                    throw JsonError(SR.ArgumentException_StringNotClosed);
                }

                if (c == '"')
                {
                    return _vb.ToString();
                }
                else if (c != '\\')
                {
                    _vb.Append((char)c);
                    continue;
                }

                // escaped expression
                c = ReadChar();
                if (c < 0)
                {
                    throw JsonError(SR.ArgumentException_IncompleteEscapeSequence);
                }
                switch (c)
                {
                    case '"':
                    case '\\':
                    case '/':
                        _vb.Append((char)c);
                        break;
                    case 'b':
                        _vb.Append('\x8');
                        break;
                    case 'f':
                        _vb.Append('\f');
                        break;
                    case 'n':
                        _vb.Append('\n');
                        break;
                    case 'r':
                        _vb.Append('\r');
                        break;
                    case 't':
                        _vb.Append('\t');
                        break;
                    case 'u':
                        ushort cp = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            cp <<= 4;
                            if ((c = ReadChar()) < 0)
                            {
                                throw JsonError(SR.ArgumentException_IncompleteEscapeLiteral);
                            }

                            if ('0' <= c && c <= '9')
                            {
                                cp += (ushort)(c - '0');
                            }
                            if ('A' <= c && c <= 'F')
                            {
                                cp += (ushort)(c - 'A' + 10);
                            }
                            if ('a' <= c && c <= 'f')
                            {
                                cp += (ushort)(c - 'a' + 10);
                            }
                        }
                        _vb.Append((char)cp);
                        break;
                    default:
                        throw JsonError(SR.ArgumentException_UnexpectedEscapeCharacter);
                }
            }
        }

        private void Expect(char expected)
        {
            int c;
            if ((c = ReadChar()) != expected)
            {
                throw JsonError(SR.Format(SR.ArgumentException_ExpectedXButGotY, expected, (char)c));
            }
        }

        private void Expect(string expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                if (ReadChar() != expected[i])
                {
                    throw JsonError(SR.Format(SR.ArgumentException_ExpectedXDiferedAtY, expected, i));
                }
            }
        }

        private Exception JsonError(string msg)
        {
            return new ArgumentException(SR.Format(SR.ArgumentException_MessageAt, msg, _line, _column));
        }
    }
}
