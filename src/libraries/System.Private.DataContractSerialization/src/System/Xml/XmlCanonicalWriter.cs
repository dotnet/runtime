// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime;
using System.Runtime.Serialization;
using System.Text;


namespace System.Xml
{
    internal sealed class XmlCanonicalWriter
    {
        private XmlUTF8NodeWriter _writer = null!; // initialized in SetOutput
        private MemoryStream _elementStream = null!; // initialized in SetOutput
        private byte[]? _elementBuffer;
        private XmlUTF8NodeWriter _elementWriter = null!; // initialized in SetOutput
        private bool _inStartElement;
        private int _depth;
        private Scope[]? _scopes;
        private int _xmlnsAttributeCount;
        private XmlnsAttribute[]? _xmlnsAttributes;
        private int _attributeCount;
        private Attribute[]? _attributes;
        private Attribute _attribute;
        private Element _element;
        private byte[]? _xmlnsBuffer;
        private int _xmlnsOffset;
        private const int maxBytesPerChar = 3;
        private int _xmlnsStartOffset;
        private bool _includeComments;
        private string[]? _inclusivePrefixes;
        private const string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";

        private static readonly bool[] s_isEscapedAttributeChar = new bool[]
        {
            true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, // All
            true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
            false, false, true, false, false, false, true, false, false, false, false, false, false, false, false, false, // '"', '&'
            false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, false  // '<'
        };
        private static readonly bool[] s_isEscapedElementChar = new bool[]
        {
            true, true, true, true, true, true, true, true, true, false, false, true, true, true, true, true, // All but 0x09, 0x0A
            true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
            false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, // '&'
            false, false, false, false, false, false, false, false, false, false, false, false, true, false, true, false  // '<', '>'
        };

        public XmlCanonicalWriter()
        {
        }

        public void SetOutput(Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _writer ??= new XmlUTF8NodeWriter(s_isEscapedAttributeChar, s_isEscapedElementChar);
            _writer.SetOutput(stream, false, null);

            _elementStream ??= new MemoryStream();
            _elementWriter ??= new XmlUTF8NodeWriter(s_isEscapedAttributeChar, s_isEscapedElementChar);
            _elementWriter.SetOutput(_elementStream, false, null);

            if (_xmlnsAttributes == null)
            {
                _xmlnsAttributeCount = 0;
                _xmlnsOffset = 0;
                WriteXmlnsAttribute("xml", "http://www.w3.org/XML/1998/namespace");
                WriteXmlnsAttribute("xmlns", xmlnsNamespace);
                WriteXmlnsAttribute(string.Empty, string.Empty);
                _xmlnsStartOffset = _xmlnsOffset;
                for (int i = 0; i < 3; i++)
                {
                    _xmlnsAttributes[i].referred = true;
                }
            }
            else
            {
                _xmlnsAttributeCount = 3;
                _xmlnsOffset = _xmlnsStartOffset;
            }

            _depth = 0;
            _inStartElement = false;
            _includeComments = includeComments;
            _inclusivePrefixes = null;
            if (inclusivePrefixes != null)
            {
                _inclusivePrefixes = new string[inclusivePrefixes.Length];
                for (int i = 0; i < inclusivePrefixes.Length; ++i)
                {
                    if (inclusivePrefixes[i] == null)
                    {
                        throw new ArgumentException(SR.InvalidInclusivePrefixListCollection);
                    }
                    _inclusivePrefixes[i] = inclusivePrefixes[i];
                }
            }
        }

        public void Flush()
        {
            ThrowIfClosed();
            _writer.Flush();
        }

        public void Close()
        {
            _writer?.Close();
            _elementWriter?.Close();
            if (_elementStream != null && _elementStream.Length > 512)
                _elementStream = null!;
            _elementBuffer = null;
            if (_scopes != null && _scopes.Length > 16)
                _scopes = null;
            if (_attributes != null && _attributes.Length > 16)
                _attributes = null;
            if (_xmlnsBuffer != null && _xmlnsBuffer.Length > 1024)
            {
                _xmlnsAttributes = null;
                _xmlnsBuffer = null;
            }
            _inclusivePrefixes = null;
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is should roughly mirror the XmlNodeWriter API where this is an instance method.")]
        public void WriteDeclaration()
        {
        }

        public void WriteComment(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            ThrowIfClosed();
            if (_includeComments)
            {
                _writer.WriteComment(value);
            }
        }

        [MemberNotNull(nameof(_scopes))]
        private void StartElement()
        {
            if (_scopes == null)
            {
                _scopes = new Scope[4];
            }
            else if (_depth == _scopes.Length)
            {
                Scope[] newScopes = new Scope[_depth * 2];
                Array.Copy(_scopes, newScopes, _depth);
                _scopes = newScopes;
            }
            _scopes[_depth].xmlnsAttributeCount = _xmlnsAttributeCount;
            _scopes[_depth].xmlnsOffset = _xmlnsOffset;
            _depth++;
            _inStartElement = true;
            _attributeCount = 0;
            _elementStream.Position = 0;
        }

        private void EndElement()
        {
            _depth--;
            _xmlnsAttributeCount = _scopes![_depth].xmlnsAttributeCount;
            _xmlnsOffset = _scopes[_depth].xmlnsOffset;
        }

        public void WriteStartElement(string prefix, string localName)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(localName);

            ThrowIfClosed();
            bool isRootElement = (_depth == 0);

            StartElement();
            _element.prefixOffset = _elementWriter.Position + 1;
            _element.prefixLength = Encoding.UTF8.GetByteCount(prefix);
            _element.localNameOffset = _element.prefixOffset + _element.prefixLength + (_element.prefixLength != 0 ? 1 : 0);
            _element.localNameLength = Encoding.UTF8.GetByteCount(localName);
            _elementWriter.WriteStartElement(prefix, localName);

            // If we have a inclusivenamespace prefix list and the namespace declaration is in the
            // outer context, then Add it to the root element.
            if (isRootElement && (_inclusivePrefixes != null))
            {
                // Scan through all the namespace declarations in the outer scope.
                for (int i = 0; i < _scopes[0].xmlnsAttributeCount; ++i)
                {
                    if (IsInclusivePrefix(ref _xmlnsAttributes![i]))
                    {
                        XmlnsAttribute attribute = _xmlnsAttributes[i];
                        AddXmlnsAttribute(ref attribute);
                    }
                }
            }
        }

        public void WriteStartElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            ArgumentNullException.ThrowIfNull(prefixBuffer);
            ArgumentOutOfRangeException.ThrowIfNegative(prefixOffset);
            if (prefixOffset > prefixBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(prefixOffset), SR.Format(SR.OffsetExceedsBufferSize, prefixBuffer.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(prefixLength);
            if (prefixLength > prefixBuffer.Length - prefixOffset)
                throw new ArgumentOutOfRangeException(nameof(prefixLength), SR.Format(SR.SizeExceedsRemainingBufferSpace, prefixBuffer.Length - prefixOffset));

            ArgumentNullException.ThrowIfNull(localNameBuffer);
            ArgumentOutOfRangeException.ThrowIfNegative(localNameOffset);
            if (localNameOffset > localNameBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(localNameOffset), SR.Format(SR.OffsetExceedsBufferSize, localNameBuffer.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(localNameLength);
            if (localNameLength > localNameBuffer.Length - localNameOffset)
                throw new ArgumentOutOfRangeException(nameof(localNameLength), SR.Format(SR.SizeExceedsRemainingBufferSpace, localNameBuffer.Length - localNameOffset));
            ThrowIfClosed();
            bool isRootElement = (_depth == 0);

            StartElement();
            _element.prefixOffset = _elementWriter.Position + 1;
            _element.prefixLength = prefixLength;
            _element.localNameOffset = _element.prefixOffset + prefixLength + (prefixLength != 0 ? 1 : 0);
            _element.localNameLength = localNameLength;
            _elementWriter.WriteStartElement(prefixBuffer, prefixOffset, prefixLength, localNameBuffer, localNameOffset, localNameLength);

            // If we have a inclusivenamespace prefix list and the namespace declaration is in the
            // outer context, then Add it to the root element.
            if (isRootElement && (_inclusivePrefixes != null))
            {
                // Scan through all the namespace declarations in the outer scope.
                for (int i = 0; i < _scopes[0].xmlnsAttributeCount; ++i)
                {
                    if (IsInclusivePrefix(ref _xmlnsAttributes![i]))
                    {
                        XmlnsAttribute attribute = _xmlnsAttributes[i];
                        AddXmlnsAttribute(ref attribute);
                    }
                }
            }
        }

        private bool IsInclusivePrefix(ref XmlnsAttribute xmlnsAttribute)
        {
            for (int i = 0; i < _inclusivePrefixes!.Length; ++i)
            {
                if (_inclusivePrefixes[i].Length == xmlnsAttribute.prefixLength)
                {
                    if (string.Equals(Encoding.UTF8.GetString(_xmlnsBuffer!, xmlnsAttribute.prefixOffset, xmlnsAttribute.prefixLength), _inclusivePrefixes[i], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void WriteEndStartElement(bool isEmpty)
        {
            ThrowIfClosed();
            _elementWriter.Flush();
            _elementBuffer = _elementStream.GetBuffer();
            _inStartElement = false;
            ResolvePrefixes();
            _writer.WriteStartElement(_elementBuffer, _element.prefixOffset, _element.prefixLength, _elementBuffer, _element.localNameOffset, _element.localNameLength);
            for (int i = _scopes![_depth - 1].xmlnsAttributeCount; i < _xmlnsAttributeCount; i++)
            {
                // Check if this prefix with the same namespace has already been rendered.
                int j = i - 1;
                bool alreadyReferred = false;
                while (j >= 0)
                {
                    Debug.Assert(_xmlnsBuffer != null);
                    if (Equals(_xmlnsBuffer, _xmlnsAttributes![i].prefixOffset, _xmlnsAttributes[i].prefixLength, _xmlnsBuffer, _xmlnsAttributes[j].prefixOffset, _xmlnsAttributes[j].prefixLength))
                    {
                        // Check if the namespace is also equal.
                        if (Equals(_xmlnsBuffer, _xmlnsAttributes[i].nsOffset, _xmlnsAttributes[i].nsLength, _xmlnsBuffer, _xmlnsAttributes[j].nsOffset, _xmlnsAttributes[j].nsLength))
                        {
                            // We have found the prefix with the same namespace occur before. See if this has been
                            // referred.
                            if (_xmlnsAttributes[j].referred)
                            {
                                // This has been referred previously. So we don't have
                                // to output the namespace again.
                                alreadyReferred = true;
                                break;
                            }
                        }
                        else
                        {
                            // The prefix is the same, but the namespace value has changed. So we have to
                            // output this namespace.
                            break;
                        }
                    }
                    --j;
                }

                if (!alreadyReferred)
                {
                    WriteXmlnsAttribute(ref _xmlnsAttributes![i]);
                }
            }
            if (_attributeCount > 0)
            {
                if (_attributeCount > 1)
                {
                    SortAttributes();
                }

                for (int i = 0; i < _attributeCount; i++)
                {
                    _writer.WriteText(_elementBuffer, _attributes![i].offset, _attributes[i].length);
                }
            }
            _writer.WriteEndStartElement(false);
            if (isEmpty)
            {
                _writer.WriteEndElement(_elementBuffer, _element.prefixOffset, _element.prefixLength, _elementBuffer, _element.localNameOffset, _element.localNameLength);
                EndElement();
            }
            _elementBuffer = null;
        }

        public void WriteEndElement(string prefix, string localName)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(localName);

            ThrowIfClosed();
            _writer.WriteEndElement(prefix, localName);
            EndElement();
        }

        [MemberNotNull(nameof(_xmlnsBuffer))]
        private void EnsureXmlnsBuffer(int byteCount)
        {
            if (_xmlnsBuffer == null)
            {
                _xmlnsBuffer = new byte[Math.Max(byteCount, 128)];
            }
            else if (_xmlnsOffset + byteCount > _xmlnsBuffer.Length)
            {
                byte[] newBuffer = new byte[Math.Max(_xmlnsOffset + byteCount, _xmlnsBuffer.Length * 2)];
                Buffer.BlockCopy(_xmlnsBuffer, 0, newBuffer, 0, _xmlnsOffset);
                _xmlnsBuffer = newBuffer;
            }
        }

        [MemberNotNull(nameof(_xmlnsAttributes))]
        public void WriteXmlnsAttribute(string prefix, string ns)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(ns);

            ThrowIfClosed();
            if (prefix.Length > int.MaxValue - ns.Length)
                throw new ArgumentOutOfRangeException(nameof(ns), SR.Format(SR.CombinedPrefixNSLength, int.MaxValue / maxBytesPerChar));
            int totalLength = prefix.Length + ns.Length;
            if (totalLength > int.MaxValue / maxBytesPerChar)
                throw new ArgumentOutOfRangeException(nameof(ns), SR.Format(SR.CombinedPrefixNSLength, int.MaxValue / maxBytesPerChar));
            EnsureXmlnsBuffer(totalLength * maxBytesPerChar);
            XmlnsAttribute xmlnsAttribute;
            xmlnsAttribute.prefixOffset = _xmlnsOffset;
            xmlnsAttribute.prefixLength = Encoding.UTF8.GetBytes(prefix, 0, prefix.Length, _xmlnsBuffer, _xmlnsOffset);
            _xmlnsOffset += xmlnsAttribute.prefixLength;
            xmlnsAttribute.nsOffset = _xmlnsOffset;
            xmlnsAttribute.nsLength = Encoding.UTF8.GetBytes(ns, 0, ns.Length, _xmlnsBuffer, _xmlnsOffset);
            _xmlnsOffset += xmlnsAttribute.nsLength;
            xmlnsAttribute.referred = false;
            AddXmlnsAttribute(ref xmlnsAttribute);
        }

        [MemberNotNull(nameof(_xmlnsAttributes))]
        public void WriteXmlnsAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] nsBuffer, int nsOffset, int nsLength)
        {
            ArgumentNullException.ThrowIfNull(prefixBuffer);
            ArgumentOutOfRangeException.ThrowIfNegative(prefixOffset);
            if (prefixOffset > prefixBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(prefixOffset), SR.Format(SR.OffsetExceedsBufferSize, prefixBuffer.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(prefixLength);
            if (prefixLength > prefixBuffer.Length - prefixOffset)
                throw new ArgumentOutOfRangeException(nameof(prefixLength), SR.Format(SR.SizeExceedsRemainingBufferSpace, prefixBuffer.Length - prefixOffset));

            ArgumentNullException.ThrowIfNull(nsBuffer);
            ArgumentOutOfRangeException.ThrowIfNegative(nsOffset);
            if (nsOffset > nsBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(nsOffset), SR.Format(SR.OffsetExceedsBufferSize, nsBuffer.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(nsLength);
            if (nsLength > nsBuffer.Length - nsOffset)
                throw new ArgumentOutOfRangeException(nameof(nsLength), SR.Format(SR.SizeExceedsRemainingBufferSpace, nsBuffer.Length - nsOffset));
            ThrowIfClosed();
            if (prefixLength > int.MaxValue - nsLength)
                throw new ArgumentOutOfRangeException(nameof(nsLength), SR.Format(SR.CombinedPrefixNSLength, int.MaxValue));
            EnsureXmlnsBuffer(prefixLength + nsLength);
            XmlnsAttribute xmlnsAttribute;
            xmlnsAttribute.prefixOffset = _xmlnsOffset;
            xmlnsAttribute.prefixLength = prefixLength;
            Buffer.BlockCopy(prefixBuffer, prefixOffset, _xmlnsBuffer, _xmlnsOffset, prefixLength);
            _xmlnsOffset += prefixLength;
            xmlnsAttribute.nsOffset = _xmlnsOffset;
            xmlnsAttribute.nsLength = nsLength;
            Buffer.BlockCopy(nsBuffer, nsOffset, _xmlnsBuffer, _xmlnsOffset, nsLength);
            _xmlnsOffset += nsLength;
            xmlnsAttribute.referred = false;
            AddXmlnsAttribute(ref xmlnsAttribute);
        }

        public void WriteStartAttribute(string prefix, string localName)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(localName);

            ThrowIfClosed();
            _attribute.offset = _elementWriter.Position;
            _attribute.length = 0;
            _attribute.prefixOffset = _attribute.offset + 1; // WriteStartAttribute emits a space
            _attribute.prefixLength = Encoding.UTF8.GetByteCount(prefix);
            _attribute.localNameOffset = _attribute.prefixOffset + _attribute.prefixLength + (_attribute.prefixLength != 0 ? 1 : 0);
            _attribute.localNameLength = Encoding.UTF8.GetByteCount(localName);
            _attribute.nsOffset = 0;
            _attribute.nsLength = 0;
            _elementWriter.WriteStartAttribute(prefix, localName);
        }

        public void WriteStartAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            ArgumentNullException.ThrowIfNull(prefixBuffer);
            ArgumentOutOfRangeException.ThrowIfNegative(prefixOffset);
            if (prefixOffset > prefixBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(prefixOffset), SR.Format(SR.OffsetExceedsBufferSize, prefixBuffer.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(prefixLength);
            if (prefixLength > prefixBuffer.Length - prefixOffset)
                throw new ArgumentOutOfRangeException(nameof(prefixLength), SR.Format(SR.SizeExceedsRemainingBufferSpace, prefixBuffer.Length - prefixOffset));

            ArgumentNullException.ThrowIfNull(localNameBuffer);
            ArgumentOutOfRangeException.ThrowIfNegative(localNameOffset);
            if (localNameOffset > localNameBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(localNameOffset), SR.Format(SR.OffsetExceedsBufferSize, localNameBuffer.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(localNameLength);
            if (localNameLength > localNameBuffer.Length - localNameOffset)
                throw new ArgumentOutOfRangeException(nameof(localNameLength), SR.Format(SR.SizeExceedsRemainingBufferSpace, localNameBuffer.Length - localNameOffset));
            ThrowIfClosed();
            _attribute.offset = _elementWriter.Position;
            _attribute.length = 0;
            _attribute.prefixOffset = _attribute.offset + 1; // WriteStartAttribute emits a space
            _attribute.prefixLength = prefixLength;
            _attribute.localNameOffset = _attribute.prefixOffset + prefixLength + (prefixLength != 0 ? 1 : 0);
            _attribute.localNameLength = localNameLength;
            _attribute.nsOffset = 0;
            _attribute.nsLength = 0;
            _elementWriter.WriteStartAttribute(prefixBuffer, prefixOffset, prefixLength, localNameBuffer, localNameOffset, localNameLength);
        }

        public void WriteEndAttribute()
        {
            ThrowIfClosed();
            _elementWriter.WriteEndAttribute();
            _attribute.length = _elementWriter.Position - _attribute.offset;
            AddAttribute(ref _attribute);
        }

        public void WriteCharEntity(int ch)
        {
            ThrowIfClosed();
            if (ch <= char.MaxValue)
            {
                char[] chars = new char[1] { (char)ch };
                WriteEscapedText(chars, 0, 1);
            }
            else
            {
                WriteText(ch);
            }
        }

        public void WriteEscapedText(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            ThrowIfClosed();
            // Skip all white spaces before the start of root element.
            if (_depth > 0)
            {
                if (_inStartElement)
                {
                    _elementWriter.WriteEscapedText(value);
                }
                else
                {
                    _writer.WriteEscapedText(value);
                }
            }
        }

        public void WriteEscapedText(byte[] chars, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > chars.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));
            ThrowIfClosed();
            // Skip all white spaces before the start of root element.
            if (_depth > 0)
            {
                if (_inStartElement)
                {
                    _elementWriter.WriteEscapedText(chars, offset, count);
                }
                else
                {
                    _writer.WriteEscapedText(chars, offset, count);
                }
            }
        }

        public void WriteEscapedText(char[] chars, int offset, int count)
        {
            ThrowIfClosed();
            // Skip all white spaces before the start of root element.
            if (_depth > 0)
            {
                if (_inStartElement)
                {
                    _elementWriter.WriteEscapedText(chars, offset, count);
                }
                else
                {
                    _writer.WriteEscapedText(chars, offset, count);
                }
            }
        }

        public void WriteText(int ch)
        {
            ThrowIfClosed();
            if (_inStartElement)
            {
                _elementWriter.WriteText(ch);
            }
            else
            {
                _writer.WriteText(ch);
            }
        }

        public void WriteText(byte[] chars, int offset, int count)
        {
            ThrowIfClosed();
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > chars.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));
            if (_inStartElement)
            {
                _elementWriter.WriteText(chars, offset, count);
            }
            else
            {
                _writer.WriteText(chars, offset, count);
            }
        }

        public void WriteText(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length > 0)
            {
                if (_inStartElement)
                {
                    _elementWriter.WriteText(value);
                }
                else
                {
                    _writer.WriteText(value);
                }
            }
        }

        public void WriteText(char[] chars, int offset, int count)
        {
            ThrowIfClosed();
            ArgumentNullException.ThrowIfNull(chars);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > chars.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));
            if (_inStartElement)
            {
                _elementWriter.WriteText(chars, offset, count);
            }
            else
            {
                _writer.WriteText(chars, offset, count);
            }
        }

        private void ThrowIfClosed()
        {
            ObjectDisposedException.ThrowIf(_writer is null, this);
        }

        private void WriteXmlnsAttribute(ref XmlnsAttribute xmlnsAttribute)
        {
            if (xmlnsAttribute.referred)
            {
                Debug.Assert(_xmlnsBuffer != null);
                _writer.WriteXmlnsAttribute(_xmlnsBuffer, xmlnsAttribute.prefixOffset, xmlnsAttribute.prefixLength, _xmlnsBuffer, xmlnsAttribute.nsOffset, xmlnsAttribute.nsLength);
            }
        }

        private void SortAttributes()
        {
            if (_attributeCount < 16)
            {
                for (int i = 0; i < _attributeCount - 1; i++)
                {
                    int attributeMin = i;
                    for (int j = i + 1; j < _attributeCount; j++)
                    {
                        if (Compare(ref _attributes![j], ref _attributes[attributeMin]) < 0)
                        {
                            attributeMin = j;
                        }
                    }

                    if (attributeMin != i)
                    {
                        Attribute temp = _attributes![i];
                        _attributes[i] = _attributes[attributeMin];
                        _attributes[attributeMin] = temp;
                    }
                }
            }
            else
            {
                new AttributeSorter(this).Sort();
            }
        }

        private void AddAttribute(ref Attribute attribute)
        {
            if (_attributes == null)
            {
                _attributes = new Attribute[4];
            }
            else if (_attributeCount == _attributes.Length)
            {
                Attribute[] newAttributes = new Attribute[_attributeCount * 2];
                Array.Copy(_attributes, newAttributes, _attributeCount);
                _attributes = newAttributes;
            }

            _attributes[_attributeCount] = attribute;
            _attributeCount++;
        }

        [MemberNotNull(nameof(_xmlnsAttributes))]
        private void AddXmlnsAttribute(ref XmlnsAttribute xmlnsAttribute)
        {
            if (_xmlnsAttributes == null)
            {
                _xmlnsAttributes = new XmlnsAttribute[4];
            }
            else if (_xmlnsAttributes.Length == _xmlnsAttributeCount)
            {
                XmlnsAttribute[] newXmlnsAttributes = new XmlnsAttribute[_xmlnsAttributeCount * 2];
                Array.Copy(_xmlnsAttributes, newXmlnsAttributes, _xmlnsAttributeCount);
                _xmlnsAttributes = newXmlnsAttributes;
            }

            // If the prefix is in the inclusive prefix list, then mark it as
            // to be rendered. Depth 0 is outer context and those can be ignored
            // for now.
            if ((_depth > 0) && (_inclusivePrefixes != null))
            {
                if (IsInclusivePrefix(ref xmlnsAttribute))
                {
                    xmlnsAttribute.referred = true;
                }
            }

            if (_depth == 0)
            {
                // XmlnsAttributes at depth 0 are the outer context.  They don't need to be sorted.
                _xmlnsAttributes[_xmlnsAttributeCount++] = xmlnsAttribute;
            }
            else
            {
                // Sort the xmlns xmlnsAttribute
                int xmlnsAttributeIndex = _scopes![_depth - 1].xmlnsAttributeCount;
                bool isNewPrefix = true;
                while (xmlnsAttributeIndex < _xmlnsAttributeCount)
                {
                    int result = Compare(ref xmlnsAttribute, ref _xmlnsAttributes[xmlnsAttributeIndex]);
                    if (result > 0)
                    {
                        xmlnsAttributeIndex++;
                    }
                    else if (result == 0)
                    {
                        // We already have the same prefix at this scope. So let's
                        // just replace the old one with the new.
                        _xmlnsAttributes[xmlnsAttributeIndex] = xmlnsAttribute;
                        isNewPrefix = false;
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                if (isNewPrefix)
                {
                    Array.Copy(_xmlnsAttributes, xmlnsAttributeIndex, _xmlnsAttributes, xmlnsAttributeIndex + 1, _xmlnsAttributeCount - xmlnsAttributeIndex);
                    _xmlnsAttributes[xmlnsAttributeIndex] = xmlnsAttribute;
                    _xmlnsAttributeCount++;
                }
            }
        }

        private void ResolvePrefix(int prefixOffset, int prefixLength, out int nsOffset, out int nsLength)
        {
            int xmlnsAttributeMin = _scopes![_depth - 1].xmlnsAttributeCount;

            // Lookup the attribute; it has to be there.  The decls are in sorted order
            // so we could do a binary search.
            int j = _xmlnsAttributeCount - 1;
            while (!Equals(_elementBuffer!, prefixOffset, prefixLength,
                           _xmlnsBuffer!, _xmlnsAttributes![j].prefixOffset, _xmlnsAttributes[j].prefixLength))
            {
                j--;
            }

            nsOffset = _xmlnsAttributes[j].nsOffset;
            nsLength = _xmlnsAttributes[j].nsLength;

            if (j < xmlnsAttributeMin)
            {
                // If the xmlns decl isn't at this scope, see if we need to copy it down
                if (!_xmlnsAttributes[j].referred)
                {
                    XmlnsAttribute xmlnsAttribute = _xmlnsAttributes[j];
                    xmlnsAttribute.referred = true;

                    // This inserts the xmlns attribute in sorted order, so j is no longer valid
                    AddXmlnsAttribute(ref xmlnsAttribute);
                }
            }
            else
            {
                // Found at this scope, indicate we need to emit it
                _xmlnsAttributes[j].referred = true;
            }
        }

        private void ResolvePrefix(ref Attribute attribute)
        {
            if (attribute.prefixLength != 0)
            {
                ResolvePrefix(attribute.prefixOffset, attribute.prefixLength, out attribute.nsOffset, out attribute.nsLength);
            }
            else
            {
                // These should've been set when we added the prefix
                Debug.Assert(attribute.nsOffset == 0 && attribute.nsLength == 0);
            }
        }

        private void ResolvePrefixes()
        {
            ResolvePrefix(_element.prefixOffset, _element.prefixLength, out _, out _);

            for (int i = 0; i < _attributeCount; i++)
            {
                ResolvePrefix(ref _attributes![i]);
            }
        }

        private int Compare(ref XmlnsAttribute xmlnsAttribute1, ref XmlnsAttribute xmlnsAttribute2)
        {
            return Compare(_xmlnsBuffer!,
                           xmlnsAttribute1.prefixOffset, xmlnsAttribute1.prefixLength,
                           xmlnsAttribute2.prefixOffset, xmlnsAttribute2.prefixLength);
        }

        private int Compare(ref Attribute attribute1, ref Attribute attribute2)
        {
            int s = Compare(_xmlnsBuffer!,
                            attribute1.nsOffset, attribute1.nsLength,
                            attribute2.nsOffset, attribute2.nsLength);

            if (s == 0)
            {
                s = Compare(_elementBuffer!,
                            attribute1.localNameOffset, attribute1.localNameLength,
                            attribute2.localNameOffset, attribute2.localNameLength);
            }

            return s;
        }

        private static int Compare(byte[] buffer, int offset1, int length1, int offset2, int length2)
        {
            if (offset1 == offset2)
            {
                return length1 - length2;
            }

            return Compare(buffer, offset1, length1, buffer, offset2, length2);
        }

        private static int Compare(byte[] buffer1, int offset1, int length1, byte[] buffer2, int offset2, int length2)
        {
            int length = Math.Min(length1, length2);

            int s = 0;
            for (int i = 0; i < length && s == 0; i++)
            {
                s = buffer1[offset1 + i] - buffer2[offset2 + i];
            }

            if (s == 0)
            {
                s = length1 - length2;
            }

            return s;
        }

        private static bool Equals(byte[] buffer1, int offset1, int length1, byte[] buffer2, int offset2, int length2)
        {
            if (length1 != length2)
                return false;

            for (int i = 0; i < length1; i++)
            {
                if (buffer1[offset1 + i] != buffer2[offset2 + i])
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class AttributeSorter : IComparer
        {
            private readonly XmlCanonicalWriter _writer;

            public AttributeSorter(XmlCanonicalWriter writer)
            {
                _writer = writer;
            }

            public void Sort()
            {
                object[] indices = new object[_writer._attributeCount];

                for (int i = 0; i < indices.Length; i++)
                {
                    indices[i] = i;
                }

                Array.Sort(indices, this);

                Attribute[] attributes = new Attribute[_writer._attributes!.Length];
                for (int i = 0; i < indices.Length; i++)
                {
                    attributes[i] = _writer._attributes[(int)indices[i]];
                }

                _writer._attributes = attributes;
            }

            public int Compare(object? obj1, object? obj2)
            {
                int attributeIndex1 = (int)obj1!;
                int attributeIndex2 = (int)obj2!;
                return _writer.Compare(ref _writer._attributes![attributeIndex1], ref _writer._attributes[attributeIndex2]);
            }
        }

        private struct Scope
        {
            public int xmlnsAttributeCount;
            public int xmlnsOffset;
        }

        private struct Element
        {
            public int prefixOffset;
            public int prefixLength;
            public int localNameOffset;
            public int localNameLength;
        }

        private struct Attribute
        {
            public int prefixOffset;
            public int prefixLength;
            public int localNameOffset;
            public int localNameLength;
            public int nsOffset;
            public int nsLength;
            public int offset;
            public int length;
        }

        private struct XmlnsAttribute
        {
            public int prefixOffset;
            public int prefixLength;
            public int nsOffset;
            public int nsLength;
            public bool referred;
        }
    }
}
