// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml
{
    // XmlTextEncoder
    //
    // This class does special handling of text content for XML.  For example
    // it will replace special characters with entities whenever necessary.
    internal class XmlTextEncoder
    {
        //
        // Fields
        //
        // output text writer
        private TextWriter _textWriter;

        // true when writing out the content of attribute value
        private bool _inAttribute;

        // quote char of the attribute (when inAttribute) 
        private char _quoteChar;

        // caching of attribute value
        private StringBuilder _attrValue;
        private bool _cacheAttrValue;

        // XmlCharType
        private XmlCharType _xmlCharType;

        //
        // Constructor
        //
        internal XmlTextEncoder(TextWriter textWriter)
        {
            _textWriter = textWriter;
            _quoteChar = '"';
            _xmlCharType = XmlCharType.Instance;
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
            if (null == array)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (0 > offset)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (0 > count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count > array.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (_cacheAttrValue)
            {
                _attrValue.Append(array, offset, count);
            }

            int endPos = offset + count;
            int i = offset;
            char ch = (char)0;
            for (;;)
            {
                int startPos = i;
                unsafe
                {
                    while (i < endPos && _xmlCharType.IsAttributeValueChar(ch = array[i]))
                    {
                        i++;
                    }
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
                            Debug.Assert((ch < 0x20 && !_xmlCharType.IsWhiteSpace(ch)) || (ch > 0xFFFD));
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
                _attrValue.Append(highChar);
                _attrValue.Append(lowChar);
            }

            _textWriter.Write("&#x");
            _textWriter.Write(surrogateChar.ToString("X", NumberFormatInfo.InvariantInfo));
            _textWriter.Write(';');
        }

        internal void Write(string text)
        {
            if (text == null)
            {
                return;
            }

            if (_cacheAttrValue)
            {
                _attrValue.Append(text);
            }

            // scan through the string to see if there are any characters to be escaped
            int len = text.Length;
            int i = 0;
            int startPos = 0;
            char ch = (char)0;
            for (;;)
            {
                unsafe
                {
                    while (i < len && _xmlCharType.IsAttributeValueChar(ch = text[i]))
                    {
                        i++;
                    }
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

            char[] helperBuffer = new char[256];
            for (;;)
            {
                if (startPos < i)
                {
                    WriteStringFragment(text, startPos, i - startPos, helperBuffer);
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
                            Debug.Assert((ch < 0x20 && !_xmlCharType.IsWhiteSpace(ch)) || (ch > 0xFFFD));
                            WriteCharEntityImpl(ch);
                        }
                        break;
                }
                i++;
                startPos = i;
                unsafe
                {
                    while (i < len && _xmlCharType.IsAttributeValueChar(ch = text[i]))
                    {
                        i++;
                    }
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
                _attrValue.Append(text);
            }

            int len = text.Length;
            int i = 0;
            char ch = (char)0;

            for (;;)
            {
                unsafe
                {
                    while (i < len && (_xmlCharType.IsCharData((ch = text[i])) || ch < 0x20))
                    {
                        i++;
                    }
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
            if (null == array)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (0 > count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (0 > offset)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count > array.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (_cacheAttrValue)
            {
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
                _attrValue.Append('&');
                _attrValue.Append(name);
                _attrValue.Append(';');
            }
            WriteEntityRefImpl(name);
        }

        //
        // Private implementation methods
        //
        // This is a helper method to workaround the fact that TextWriter does not have a Write method 
        // for fragment of a string such as Write( string, offset, count). 
        // The string fragment will be written out by copying into a small helper buffer and then 
        // calling textWriter to write out the buffer.
        private void WriteStringFragment(string str, int offset, int count, char[] helperBuffer)
        {
            int bufferSize = helperBuffer.Length;
            while (count > 0)
            {
                int copyCount = count;
                if (copyCount > bufferSize)
                {
                    copyCount = bufferSize;
                }

                str.CopyTo(offset, helperBuffer, 0, copyCount);
                _textWriter.Write(helperBuffer, 0, copyCount);
                offset += copyCount;
                count -= copyCount;
            }
        }

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
