// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace System.Xml
{
    internal sealed partial class XmlWellFormedWriter : XmlWriter
    {
        private partial struct ElementScope
        {
            internal Task WriteEndElementAsync(XmlRawWriter rawWriter)
            {
                return rawWriter.WriteEndElementAsync(prefix, localName, namespaceUri);
            }

            internal Task WriteFullEndElementAsync(XmlRawWriter rawWriter)
            {
                return rawWriter.WriteFullEndElementAsync(prefix, localName, namespaceUri);
            }
        }

        private partial struct Namespace
        {
            internal async Task WriteDeclAsync(XmlWriter writer, XmlRawWriter? rawWriter)
            {
                Debug.Assert(kind == NamespaceKind.NeedToWrite);
                if (null != rawWriter)
                {
                    await rawWriter.WriteNamespaceDeclarationAsync(prefix, namespaceUri).ConfigureAwait(false);
                }
                else
                {
                    if (prefix.Length == 0)
                    {
                        await writer.WriteStartAttributeAsync(string.Empty, "xmlns", XmlReservedNs.NsXmlNs).ConfigureAwait(false);
                    }
                    else
                    {
                        await writer.WriteStartAttributeAsync("xmlns", prefix, XmlReservedNs.NsXmlNs).ConfigureAwait(false);
                    }
                    await writer.WriteStringAsync(namespaceUri).ConfigureAwait(false);
                    await writer.WriteEndAttributeAsync().ConfigureAwait(false);
                }
            }
        }

        private sealed partial class AttributeValueCache
        {
            internal async Task ReplayAsync(XmlWriter writer)
            {
                if (_singleStringValue != null)
                {
                    await writer.WriteStringAsync(_singleStringValue).ConfigureAwait(false);
                    return;
                }

                BufferChunk bufChunk;
                for (int i = _firstItem; i <= _lastItem; i++)
                {
                    Item item = _items![i];
                    switch (item.type)
                    {
                        case ItemType.EntityRef:
                            await writer.WriteEntityRefAsync((string)item.data).ConfigureAwait(false);
                            break;
                        case ItemType.CharEntity:
                            await writer.WriteCharEntityAsync((char)item.data).ConfigureAwait(false);
                            break;
                        case ItemType.SurrogateCharEntity:
                            char[] chars = (char[])item.data;
                            await writer.WriteSurrogateCharEntityAsync(chars[0], chars[1]).ConfigureAwait(false);
                            break;
                        case ItemType.Whitespace:
                            await writer.WriteWhitespaceAsync((string)item.data).ConfigureAwait(false);
                            break;
                        case ItemType.String:
                            await writer.WriteStringAsync((string)item.data).ConfigureAwait(false);
                            break;
                        case ItemType.StringChars:
                            bufChunk = (BufferChunk)item.data;
                            await writer.WriteCharsAsync(bufChunk.buffer, bufChunk.index, bufChunk.count).ConfigureAwait(false);
                            break;
                        case ItemType.Raw:
                            await writer.WriteRawAsync((string)item.data).ConfigureAwait(false);
                            break;
                        case ItemType.RawChars:
                            bufChunk = (BufferChunk)item.data;
                            await writer.WriteCharsAsync(bufChunk.buffer, bufChunk.index, bufChunk.count).ConfigureAwait(false);
                            break;
                        case ItemType.ValueString:
                            await writer.WriteStringAsync((string)item.data).ConfigureAwait(false);
                            break;
                        default:
                            Debug.Fail("Unexpected ItemType value.");
                            break;
                    }
                }
            }
        }
    }
}
