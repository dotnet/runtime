// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Xml
{
    internal partial class XmlWellFormedWriter : XmlWriter
    {
        //
        // Private types
        //
        private class NamespaceResolverProxy : IXmlNamespaceResolver
        {
            private XmlWellFormedWriter _wfWriter;

            internal NamespaceResolverProxy(XmlWellFormedWriter wfWriter)
            {
                _wfWriter = wfWriter;
            }

            IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
            {
                throw new NotImplementedException();
            }
            string IXmlNamespaceResolver.LookupNamespace(string prefix)
            {
                return _wfWriter.LookupNamespace(prefix);
            }

            string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
            {
                return _wfWriter.LookupPrefix(namespaceName);
            }
        }

        private partial struct ElementScope
        {
            internal int prevNSTop;
            internal string prefix;
            internal string localName;
            internal string namespaceUri;
            internal XmlSpace xmlSpace;
            internal string xmlLang;

            internal void Set(string prefix, string localName, string namespaceUri, int prevNSTop)
            {
                this.prevNSTop = prevNSTop;
                this.prefix = prefix;
                this.namespaceUri = namespaceUri;
                this.localName = localName;
                this.xmlSpace = (System.Xml.XmlSpace)(int)-1;
                this.xmlLang = null;
            }

            internal void WriteEndElement(XmlRawWriter rawWriter)
            {
                rawWriter.WriteEndElement(prefix, localName, namespaceUri);
            }

            internal void WriteFullEndElement(XmlRawWriter rawWriter)
            {
                rawWriter.WriteFullEndElement(prefix, localName, namespaceUri);
            }
        }

        private enum NamespaceKind
        {
            Written,
            NeedToWrite,
            Implied,
            Special,
        }

        private partial struct Namespace
        {
            internal string prefix;
            internal string namespaceUri;
            internal NamespaceKind kind;
            internal int prevNsIndex;

            internal void Set(string prefix, string namespaceUri, NamespaceKind kind)
            {
                this.prefix = prefix;
                this.namespaceUri = namespaceUri;
                this.kind = kind;
                this.prevNsIndex = -1;
            }

            internal void WriteDecl(XmlWriter writer, XmlRawWriter rawWriter)
            {
                Debug.Assert(kind == NamespaceKind.NeedToWrite);
                if (null != rawWriter)
                {
                    rawWriter.WriteNamespaceDeclaration(prefix, namespaceUri);
                }
                else
                {
                    if (prefix.Length == 0)
                    {
                        writer.WriteStartAttribute(string.Empty, "xmlns", XmlReservedNs.NsXmlNs);
                    }
                    else
                    {
                        writer.WriteStartAttribute("xmlns", prefix, XmlReservedNs.NsXmlNs);
                    }
                    writer.WriteString(namespaceUri);
                    writer.WriteEndAttribute();
                }
            }
        }

        private struct AttrName
        {
            internal string prefix;
            internal string namespaceUri;
            internal string localName;
            internal int prev;

            internal void Set(string prefix, string localName, string namespaceUri)
            {
                this.prefix = prefix;
                this.namespaceUri = namespaceUri;
                this.localName = localName;
                this.prev = 0;
            }

            internal bool IsDuplicate(string prefix, string localName, string namespaceUri)
            {
                return ((this.localName == localName)
                    && ((this.prefix == prefix) || (this.namespaceUri == namespaceUri)));
            }
        }

        private enum SpecialAttribute
        {
            No = 0,
            DefaultXmlns,
            PrefixedXmlns,
            XmlSpace,
            XmlLang
        }

        private partial class AttributeValueCache
        {
            private enum ItemType
            {
                EntityRef,
                CharEntity,
                SurrogateCharEntity,
                Whitespace,
                String,
                StringChars,
                Raw,
                RawChars,
                ValueString,
            }

            private class Item
            {
                internal ItemType type;
                internal object data;

                internal Item() { }

                internal void Set(ItemType type, object data)
                {
                    this.type = type;
                    this.data = data;
                }
            }

            private class BufferChunk
            {
                internal char[] buffer;
                internal int index;
                internal int count;

                internal BufferChunk(char[] buffer, int index, int count)
                {
                    this.buffer = buffer;
                    this.index = index;
                    this.count = count;
                }
            }

            private StringBuilder _stringValue = new StringBuilder();
            private string _singleStringValue; // special-case for a single WriteString call
            private Item[] _items;
            private int _firstItem;
            private int _lastItem = -1;

            internal string StringValue
            {
                get
                {
                    if (_singleStringValue != null)
                    {
                        return _singleStringValue;
                    }
                    else
                    {
                        return _stringValue.ToString();
                    }
                }
            }

            internal void WriteEntityRef(string name)
            {
                if (_singleStringValue != null)
                {
                    StartComplexValue();
                }

                switch (name)
                {
                    case "lt":
                        _stringValue.Append('<');
                        break;
                    case "gt":
                        _stringValue.Append('>');
                        break;
                    case "quot":
                        _stringValue.Append('"');
                        break;
                    case "apos":
                        _stringValue.Append('\'');
                        break;
                    case "amp":
                        _stringValue.Append('&');
                        break;
                    default:
                        _stringValue.Append('&');
                        _stringValue.Append(name);
                        _stringValue.Append(';');
                        break;
                }

                AddItem(ItemType.EntityRef, name);
            }

            internal void WriteCharEntity(char ch)
            {
                if (_singleStringValue != null)
                {
                    StartComplexValue();
                }
                _stringValue.Append(ch);
                AddItem(ItemType.CharEntity, ch);
            }

            internal void WriteSurrogateCharEntity(char lowChar, char highChar)
            {
                if (_singleStringValue != null)
                {
                    StartComplexValue();
                }
                _stringValue.Append(highChar);
                _stringValue.Append(lowChar);
                AddItem(ItemType.SurrogateCharEntity, new char[] { lowChar, highChar });
            }

            internal void WriteWhitespace(string ws)
            {
                if (_singleStringValue != null)
                {
                    StartComplexValue();
                }
                _stringValue.Append(ws);
                AddItem(ItemType.Whitespace, ws);
            }

            internal void WriteString(string text)
            {
                if (_singleStringValue != null)
                {
                    StartComplexValue();
                }
                else
                {
                    // special-case for a single WriteString
                    if (_lastItem == -1)
                    {
                        _singleStringValue = text;
                        return;
                    }
                }

                _stringValue.Append(text);
                AddItem(ItemType.String, text);
            }

            internal void WriteChars(char[] buffer, int index, int count)
            {
                if (_singleStringValue != null)
                {
                    StartComplexValue();
                }
                _stringValue.Append(buffer, index, count);
                AddItem(ItemType.StringChars, new BufferChunk(buffer, index, count));
            }

            internal void WriteRaw(char[] buffer, int index, int count)
            {
                if (_singleStringValue != null)
                {
                    StartComplexValue();
                }
                _stringValue.Append(buffer, index, count);
                AddItem(ItemType.RawChars, new BufferChunk(buffer, index, count));
            }

            internal void WriteRaw(string data)
            {
                if (_singleStringValue != null)
                {
                    StartComplexValue();
                }
                _stringValue.Append(data);
                AddItem(ItemType.Raw, data);
            }

            internal void WriteValue(string value)
            {
                if (_singleStringValue != null)
                {
                    StartComplexValue();
                }
                _stringValue.Append(value);
                AddItem(ItemType.ValueString, value);
            }

            internal void Replay(XmlWriter writer)
            {
                if (_singleStringValue != null)
                {
                    writer.WriteString(_singleStringValue);
                    return;
                }

                BufferChunk bufChunk;
                for (int i = _firstItem; i <= _lastItem; i++)
                {
                    Item item = _items[i];
                    switch (item.type)
                    {
                        case ItemType.EntityRef:
                            writer.WriteEntityRef((string)item.data);
                            break;
                        case ItemType.CharEntity:
                            writer.WriteCharEntity((char)item.data);
                            break;
                        case ItemType.SurrogateCharEntity:
                            char[] chars = (char[])item.data;
                            writer.WriteSurrogateCharEntity(chars[0], chars[1]);
                            break;
                        case ItemType.Whitespace:
                            writer.WriteWhitespace((string)item.data);
                            break;
                        case ItemType.String:
                            writer.WriteString((string)item.data);
                            break;
                        case ItemType.StringChars:
                            bufChunk = (BufferChunk)item.data;
                            writer.WriteChars(bufChunk.buffer, bufChunk.index, bufChunk.count);
                            break;
                        case ItemType.Raw:
                            writer.WriteRaw((string)item.data);
                            break;
                        case ItemType.RawChars:
                            bufChunk = (BufferChunk)item.data;
                            writer.WriteChars(bufChunk.buffer, bufChunk.index, bufChunk.count);
                            break;
                        case ItemType.ValueString:
                            writer.WriteValue((string)item.data);
                            break;
                        default:
                            Debug.Assert(false, "Unexpected ItemType value.");
                            break;
                    }
                }
            }

            // This method trims whitespace from the beginning and the end of the string and cached writer events
            internal void Trim()
            {
                // if only one string value -> trim the write spaces directly
                if (_singleStringValue != null)
                {
                    _singleStringValue = XmlConvert.TrimString(_singleStringValue);
                    return;
                }

                // trim the string in StringBuilder
                string valBefore = _stringValue.ToString();
                string valAfter = XmlConvert.TrimString(valBefore);
                if (valBefore != valAfter)
                {
                    _stringValue = new StringBuilder(valAfter);
                }

                // trim the beginning of the recorded writer events
                XmlCharType xmlCharType = XmlCharType.Instance;

                int i = _firstItem;
                while (i == _firstItem && i <= _lastItem)
                {
                    Item item = _items[i];
                    switch (item.type)
                    {
                        case ItemType.Whitespace:
                            _firstItem++;
                            break;
                        case ItemType.String:
                        case ItemType.Raw:
                        case ItemType.ValueString:
                            item.data = XmlConvert.TrimStringStart((string)item.data);
                            if (((string)item.data).Length == 0)
                            {
                                // no characters left -> move the firstItem index to exclude it from the Replay
                                _firstItem++;
                            }
                            break;
                        case ItemType.StringChars:
                        case ItemType.RawChars:
                            BufferChunk bufChunk = (BufferChunk)item.data;
                            int endIndex = bufChunk.index + bufChunk.count;
                            while (bufChunk.index < endIndex && xmlCharType.IsWhiteSpace(bufChunk.buffer[bufChunk.index]))
                            {
                                bufChunk.index++;
                                bufChunk.count--;
                            }
                            if (bufChunk.index == endIndex)
                            {
                                // no characters left -> move the firstItem index to exclude it from the Replay
                                _firstItem++;
                            }
                            break;
                    }
                    i++;
                }

                // trim the end of the recorded writer events
                i = _lastItem;
                while (i == _lastItem && i >= _firstItem)
                {
                    Item item = _items[i];
                    switch (item.type)
                    {
                        case ItemType.Whitespace:
                            _lastItem--;
                            break;
                        case ItemType.String:
                        case ItemType.Raw:
                        case ItemType.ValueString:
                            item.data = XmlConvert.TrimStringEnd((string)item.data);
                            if (((string)item.data).Length == 0)
                            {
                                // no characters left -> move the lastItem index to exclude it from the Replay
                                _lastItem--;
                            }
                            break;
                        case ItemType.StringChars:
                        case ItemType.RawChars:
                            BufferChunk bufChunk = (BufferChunk)item.data;
                            while (bufChunk.count > 0 && xmlCharType.IsWhiteSpace(bufChunk.buffer[bufChunk.index + bufChunk.count - 1]))
                            {
                                bufChunk.count--;
                            }
                            if (bufChunk.count == 0)
                            {
                                // no characters left -> move the lastItem index to exclude it from the Replay
                                _lastItem--;
                            }
                            break;
                    }
                    i--;
                }
            }

            internal void Clear()
            {
                _singleStringValue = null;
                _lastItem = -1;
                _firstItem = 0;
                _stringValue.Length = 0;
            }

            private void StartComplexValue()
            {
                Debug.Assert(_singleStringValue != null);
                Debug.Assert(_lastItem == -1);

                _stringValue.Append(_singleStringValue);
                AddItem(ItemType.String, _singleStringValue);

                _singleStringValue = null;
            }

            private void AddItem(ItemType type, object data)
            {
                int newItemIndex = _lastItem + 1;
                if (_items == null)
                {
                    _items = new Item[4];
                }
                else if (_items.Length == newItemIndex)
                {
                    Item[] newItems = new Item[newItemIndex * 2];
                    Array.Copy(_items, newItems, newItemIndex);
                    _items = newItems;
                }
                if (_items[newItemIndex] == null)
                {
                    _items[newItemIndex] = new Item();
                }
                _items[newItemIndex].Set(type, data);
                _lastItem = newItemIndex;
            }
        }
    }
}
