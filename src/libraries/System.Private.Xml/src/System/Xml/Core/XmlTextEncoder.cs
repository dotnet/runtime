// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Xml
{
    // XmlTextEncoder
    //
    // This class does special handling of text content for XML.  For example
    // it will replace special characters with entities whenever necessary.
    internal sealed class XmlTextEncoder
    {
        //
        // Fields
        //
        // output text writer
        private readonly TextWriter _textWriter;

        // true when writing out the content of attribute value
        private bool _inAttribute;

        // quote char of the attribute (when inAttribute)
        private char _quoteChar;

        // caching of attribute value
        private StringBuilder? _attrValue;
        private bool _cacheAttrValue;

        //
        // Constructor
        //
        internal XmlTextEncoder(TextWriter textWriter)
        {
            _textWriter = textWriter;
            _quoteChar = '"';
        }

        //
        // Internal methods and properties
        //
        internal char QuoteChar
        {
            set
            {
                _quoteChar = value;
            }
        }

        internal void StartAttribute(bool cacheAttrValue)
        {
            _inAttribute = true;
            _cacheAttrValue = cacheAttrValue;
            if (cacheAttrValue)
            {
                if (_attrValue == null)
                {
                    _attrValue = new StringBuilder();
                }
                else
                {
                    _attrValue.Length = 0;
                }
            }
        }

        internal void EndAttribute()
        {
            if (_cacheAttrValue)
            {
                Debug.Assert(_attrValue != null);
                _attrValue.Length = 0;
            }

            _inAttribute = false;
            _cacheAttrValue = false;
        }

        internal string AttributeValue
        {
            get
            {
                if (_cacheAttrValue)
                {
                    Debug.Assert(_attrValue != null);
                    return _attrValue.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        internal void WriteSurrogateChar(char lowChar, char highChar)
        {
            if (!XmlCharType.IsLowSurrogate(lowChar) ||
                 !XmlCharType.IsHighSurrogate(highChar))
            {
                throw XmlConvert.CreateInvalidSurrogatePairException(lowChar, highChar);
            }

            _textWriter.Write(highChar);
            _textWriter.Write(lowChar);
        }

        internal void Write(char[] array, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, array.Length - offset);

            if (_cacheAttrValue)
            {
                Debug.Assert(_attrValue != null);
                _attrValue.Append(array, offset, count);
            }

            int endPos = offset + count;
            int i = offset;
            char ch = (char)0;
            while (true)
            {
                int startPos = i;
                while (i < endPos && XmlCharType.IsAttributeValueChar(ch = array[i]))
                {
                    i++;
                }

                if (startPos < i)
                {
                    _textWriter.Write(array, startPos, i - startPos);
                }
                if (i == endPos)
                {
                    break;
                }

                switch (ch)
                {
                    case (char)0x9:
                        _textWriter.Write(ch);
                        break;
                    case (char)0xA:
                    case (char)0xD:
                        if (_inAttribute)
                        {
                            WriteCharEntityImpl(ch);
                        }
                        else
                        {
                            _textWriter.Write(ch);
                        }
                        break;

                    case '<':
                        WriteEntityRefImpl("lt");
                        break;
                    case '>':
                        WriteEntityRefImpl("gt");
                        break;
                    case '&':
                        WriteEntityRefImpl("amp");
                        break;
                    case '\'':
                        if (_inAttribute && _quoteChar == ch)
                        {
                            WriteEntityRefImpl("apos");
                        }
                        else
                        {
                            _textWriter.Write('\'');
                        }
                        break;
                    case '"':
                        if (_inAttribute && _quoteChar == ch)
                        {
                            WriteEntityRefImpl("quot");
                        }
                        else
                        {
                            _textWriter.Write('"');
                        }
                        break;
                    default:
                        if (XmlCharType.IsHighSurrogate(ch))
                        {
                            if (i + 1 < endPos)
                            {
                                WriteSurrogateChar(array[++i], ch);
                            }
                            else
                            {
                                throw new ArgumentException(SR.Xml_SurrogatePairSplit);
                            }
                        }
                        else if (XmlCharType.IsLowSurrogate(ch))
                        {
                            throw XmlConvert.CreateInvalidHighSurrogateCharException(ch);
                        }
                        else
                        {
                            Debug.Assert((ch < 0x20 && !XmlCharType.IsWhiteSpace(ch)) || (ch > 0xFFFD));
                            WriteCharEntityImpl(ch);
                        }
                        break;
                }
                i++;
            }
        }

        internal void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            if (!XmlCharType.IsLowSurrogate(lowChar) ||
                 !XmlCharType.IsHighSurrogate(highChar))
            {
                throw XmlConvert.CreateInvalidSurrogatePairException(lowChar, highChar);
            }
            int surrogateChar = XmlCharType.CombineSurrogateChar(lowChar, highChar);

            if (_cacheAttrValue)
            {
                Debug.Assert(_attrValue != null);
                _attrValue.Append(highChar);
                _attrValue.Append(lowChar);
            }

            _textWriter.Write("&#x");
            _textWriter.Write(surrogateChar.ToString("X", NumberFormatInfo.InvariantInfo));
            _textWriter.Write(';');
        }

        internal void Write(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                return;
            }

            if (_cacheAttrValue)
            {
                Debug.Assert(_attrValue != null);
                _attrValue.Append(text);
            }

            // scan through the string to see if there are any characters to be escaped
            int len = text.Length;
            int i = 0;
            int startPos = 0;
            char ch = (char)0;
            while (true)
            {
                while (i < len && XmlCharType.IsAttributeValueChar(ch = text[i]))
                {
                    i++;
                }

                if (i == len)
                {
                    // reached the end of the string -> write it whole out
                    _textWriter.Write(text);
                    return;
                }
                if (_inAttribute)
                {
                    if (ch == 0x9)
                    {
                        i++;
                        continue;
                    }
                }
                else
                {
                    if (ch == 0x9 || ch == 0xA || ch == 0xD || ch == '"' || ch == '\'')
                    {
                        i++;
                        continue;
                    }
                }
                // some character that needs to be escaped is found:
                break;
            }

            while (true)
            {
                if (startPos < i)
                {
                    _textWriter.Write(text.Slice(startPos, i - startPos));
                }

                if (i == len)
                {
                    break;
                }

                switch (ch)
                {
                    case (char)0x9:
                        _textWriter.Write(ch);
                        break;
                    case (char)0xA:
                    case (char)0xD:
                        if (_inAttribute)
                        {
                            WriteCharEntityImpl(ch);
                        }
                        else
                        {
                            _textWriter.Write(ch);
                        }
                        break;
                    case '<':
                        WriteEntityRefImpl("lt");
                        break;
                    case '>':
                        WriteEntityRefImpl("gt");
                        break;
                    case '&':
                        WriteEntityRefImpl("amp");
                        break;
                    case '\'':
                        if (_inAttribute && _quoteChar == ch)
                        {
                            WriteEntityRefImpl("apos");
                        }
                        else
                        {
                            _textWriter.Write('\'');
                        }
                        break;
                    case '"':
                        if (_inAttribute && _quoteChar == ch)
                        {
                            WriteEntityRefImpl("quot");
                        }
                        else
                        {
                            _textWriter.Write('"');
                        }
                        break;
                    default:
                        if (XmlCharType.IsHighSurrogate(ch))
                        {
                            if (i + 1 < len)
                            {
                                WriteSurrogateChar(text[++i], ch);
                            }
                            else
                            {
                                throw XmlConvert.CreateInvalidSurrogatePairException(text[i], ch);
                            }
                        }
                        else if (XmlCharType.IsLowSurrogate(ch))
                        {
                            throw XmlConvert.CreateInvalidHighSurrogateCharException(ch);
                        }
                        else
                        {
                            Debug.Assert((ch < 0x20 && !XmlCharType.IsWhiteSpace(ch)) || (ch > 0xFFFD));
                            WriteCharEntityImpl(ch);
                        }
                        break;
                }
                i++;
                startPos = i;
                while (i < len && XmlCharType.IsAttributeValueChar(ch = text[i]))
                {
                    i++;
                }
            }
        }

        internal void WriteRawWithSurrogateChecking(string text)
        {
            if (text == null)
            {
                return;
            }

            if (_cacheAttrValue)
            {
                Debug.Assert(_attrValue != null);
                _attrValue.Append(text);
            }

            int len = text.Length;
            int i = 0;
            char ch = (char)0;

            while (true)
            {
                while (i < len && (XmlCharType.IsCharData((ch = text[i])) || ch < 0x20))
                {
                    i++;
                }
                if (i == len)
                {
                    break;
                }
                if (XmlCharType.IsHighSurrogate(ch))
                {
                    if (i + 1 < len)
                    {
                        char lowChar = text[i + 1];
                        if (XmlCharType.IsLowSurrogate(lowChar))
                        {
                            i += 2;
                            continue;
                        }
                        else
                        {
                            throw XmlConvert.CreateInvalidSurrogatePairException(lowChar, ch);
                        }
                    }
                    throw new ArgumentException(SR.Xml_InvalidSurrogateMissingLowChar);
                }
                else if (XmlCharType.IsLowSurrogate(ch))
                {
                    throw XmlConvert.CreateInvalidHighSurrogateCharException(ch);
                }
                else
                {
                    i++;
                }
            }

            _textWriter.Write(text);
            return;
        }

        internal void WriteRaw(char[] array, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, array.Length - offset);

            if (_cacheAttrValue)
            {
                Debug.Assert(_attrValue != null);
                _attrValue.Append(array, offset, count);
            }

            _textWriter.Write(array, offset, count);
        }



        internal void WriteCharEntity(char ch)
        {
            if (XmlCharType.IsSurrogate(ch))
            {
                throw new ArgumentException(SR.Xml_InvalidSurrogateMissingLowChar);
            }

            string strVal = ((int)ch).ToString("X", NumberFormatInfo.InvariantInfo);
            if (_cacheAttrValue)
            {
                Debug.Assert(_attrValue != null);
                _attrValue.Append("&#x");
                _attrValue.Append(strVal);
                _attrValue.Append(';');
            }

            WriteCharEntityImpl(strVal);
        }

        internal void WriteEntityRef(string name)
        {
            if (_cacheAttrValue)
            {
                Debug.Assert(_attrValue != null);
                _attrValue.Append('&');
                _attrValue.Append(name);
                _attrValue.Append(';');
            }

            WriteEntityRefImpl(name);
        }

        //
        // Private implementation methods
        //

        private void WriteCharEntityImpl(char ch)
        {
            WriteCharEntityImpl(((int)ch).ToString("X", NumberFormatInfo.InvariantInfo));
        }

        private void WriteCharEntityImpl(string strVal)
        {
            _textWriter.Write("&#x");
            _textWriter.Write(strVal);
            _textWriter.Write(';');
        }

        private void WriteEntityRefImpl(string name)
        {
            _textWriter.Write('&');
            _textWriter.Write(name);
            _textWriter.Write(';');
        }
    }
}
