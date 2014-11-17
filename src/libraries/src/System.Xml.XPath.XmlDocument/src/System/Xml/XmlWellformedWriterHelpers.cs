// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        class NamespaceResolverProxy : IXmlNamespaceResolver
        {
            XmlWellFormedWriter wfWriter;

            internal NamespaceResolverProxy(XmlWellFormedWriter wfWriter)
            {
                this.wfWriter = wfWriter;
            }

            IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
            {
                throw NotImplemented.ByDesign;
            }
            string IXmlNamespaceResolver.LookupNamespace(string prefix)
            {
                return wfWriter.LookupNamespace(prefix);
            }

            string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
            {
                return wfWriter.LookupPrefix(namespaceName);
            }
        }

        partial struct ElementScope
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

        enum NamespaceKind
        {
            Written,
            NeedToWrite,
            Implied,
            Special,
        }

        partial struct Namespace
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
                        writer.WriteStartAttribute(string.Empty, XmlConst.NsXmlNs, XmlConst.ReservedNsXmlNs);
                    }
                    else
                    {
                        writer.WriteStartAttribute(XmlConst.NsXmlNs, prefix, XmlConst.ReservedNsXmlNs);
                    }
                    writer.WriteString(namespaceUri);
                    writer.WriteEndAttribute();
                }
            }
        }

        struct AttrName
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

        enum SpecialAttribute
        {
            No = 0,
            DefaultXmlns,
            PrefixedXmlns,
            XmlSpace,
            XmlLang
        }

        partial class AttributeValueCache
        {
            enum ItemType
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

            class Item
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

            class BufferChunk
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

            StringBuilder stringValue = new StringBuilder();
            string singleStringValue; // special-case for a single WriteString call
            Item[] items;
            int firstItem;
            int lastItem = -1;

            internal string StringValue
            {
                get
                {
                    if (singleStringValue != null)
                    {
                        return singleStringValue;
                    }
                    else
                    {
                        return stringValue.ToString();
                    }
                }
            }

            internal void WriteEntityRef(string name)
            {
                if (singleStringValue != null)
                {
                    StartComplexValue();
                }

                switch (name)
                {
                    case "lt":
                        stringValue.Append('<');
                        break;
                    case "gt":
                        stringValue.Append('>');
                        break;
                    case "quot":
                        stringValue.Append('"');
                        break;
                    case "apos":
                        stringValue.Append('\'');
                        break;
                    case "amp":
                        stringValue.Append('&');
                        break;
                    default:
                        stringValue.Append('&');
                        stringValue.Append(name);
                        stringValue.Append(';');
                        break;
                }

                AddItem(ItemType.EntityRef, name);
            }

            internal void WriteCharEntity(char ch)
            {
                if (singleStringValue != null)
                {
                    StartComplexValue();
                }
                stringValue.Append(ch);
                AddItem(ItemType.CharEntity, ch);
            }

            internal void WriteSurrogateCharEntity(char lowChar, char highChar)
            {
                if (singleStringValue != null)
                {
                    StartComplexValue();
                }
                stringValue.Append(highChar);
                stringValue.Append(lowChar);
                AddItem(ItemType.SurrogateCharEntity, new char[] { lowChar, highChar });
            }

            internal void WriteWhitespace(string ws)
            {
                if (singleStringValue != null)
                {
                    StartComplexValue();
                }
                stringValue.Append(ws);
                AddItem(ItemType.Whitespace, ws);
            }

            internal void WriteString(string text)
            {
                if (singleStringValue != null)
                {
                    StartComplexValue();
                }
                else
                {
                    // special-case for a single WriteString
                    if (lastItem == -1)
                    {
                        singleStringValue = text;
                        return;
                    }
                }

                stringValue.Append(text);
                AddItem(ItemType.String, text);
            }

            internal void WriteChars(char[] buffer, int index, int count)
            {
                if (singleStringValue != null)
                {
                    StartComplexValue();
                }
                stringValue.Append(buffer, index, count);
                AddItem(ItemType.StringChars, new BufferChunk(buffer, index, count));
            }

            internal void WriteRaw(char[] buffer, int index, int count)
            {
                if (singleStringValue != null)
                {
                    StartComplexValue();
                }
                stringValue.Append(buffer, index, count);
                AddItem(ItemType.RawChars, new BufferChunk(buffer, index, count));
            }

            internal void WriteRaw(string data)
            {
                if (singleStringValue != null)
                {
                    StartComplexValue();
                }
                stringValue.Append(data);
                AddItem(ItemType.Raw, data);
            }

            internal void WriteValue(string value)
            {
                if (singleStringValue != null)
                {
                    StartComplexValue();
                }
                stringValue.Append(value);
                AddItem(ItemType.ValueString, value);
            }

            internal void Replay(XmlWriter writer)
            {
                if (singleStringValue != null)
                {
                    writer.WriteString(singleStringValue);
                    return;
                }

                BufferChunk bufChunk;
                for (int i = firstItem; i <= lastItem; i++)
                {
                    Item item = items[i];
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

            // This method trims whitespaces from the beginnig and the end of the string and cached writer events
            internal void Trim()
            {
                // if only one string value -> trim the write spaces directly
                if (singleStringValue != null)
                {
                    singleStringValue = XmlConvertEx.TrimString(singleStringValue);
                    return;
                }

                // trim the string in StringBuilder
                string valBefore = stringValue.ToString();
                string valAfter = XmlConvertEx.TrimString(valBefore);
                if (valBefore != valAfter)
                {
                    stringValue = new StringBuilder(valAfter);
                }

                // trim the beginning of the recorded writer events
                XmlCharType xmlCharType = XmlCharType.Instance;

                int i = firstItem;
                while (i == firstItem && i <= lastItem)
                {
                    Item item = items[i];
                    switch (item.type)
                    {
                        case ItemType.Whitespace:
                            firstItem++;
                            break;
                        case ItemType.String:
                        case ItemType.Raw:
                        case ItemType.ValueString:
                            item.data = XmlConvertEx.TrimStringStart((string)item.data);
                            if (((string)item.data).Length == 0)
                            {
                                // no characters left -> move the firstItem index to exclude it from the Replay
                                firstItem++;
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
                                firstItem++;
                            }
                            break;
                    }
                    i++;
                }

                // trim the end of the recorded writer events
                i = lastItem;
                while (i == lastItem && i >= firstItem)
                {
                    Item item = items[i];
                    switch (item.type)
                    {
                        case ItemType.Whitespace:
                            lastItem--;
                            break;
                        case ItemType.String:
                        case ItemType.Raw:
                        case ItemType.ValueString:
                            item.data = XmlConvertEx.TrimStringEnd((string)item.data);
                            if (((string)item.data).Length == 0)
                            {
                                // no characters left -> move the lastItem index to exclude it from the Replay
                                lastItem--;
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
                                lastItem--;
                            }
                            break;
                    }
                    i--;
                }
            }

            internal void Clear()
            {
                singleStringValue = null;
                lastItem = -1;
                firstItem = 0;
                stringValue.Length = 0;
            }

            private void StartComplexValue()
            {
                Debug.Assert(singleStringValue != null);
                Debug.Assert(lastItem == -1);

                stringValue.Append(singleStringValue);
                AddItem(ItemType.String, singleStringValue);

                singleStringValue = null;
            }

            void AddItem(ItemType type, object data)
            {
                int newItemIndex = lastItem + 1;
                if (items == null)
                {
                    items = new Item[4];
                }
                else if (items.Length == newItemIndex)
                {
                    Item[] newItems = new Item[newItemIndex * 2];
                    Array.Copy(items, newItems, newItemIndex);
                    items = newItems;
                }
                if (items[newItemIndex] == null)
                {
                    items[newItemIndex] = new Item();
                }
                items[newItemIndex].Set(type, data);
                lastItem = newItemIndex;
            }
        }
    }
}
