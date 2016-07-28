// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Security;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Xml.Schema;
using System.Runtime.Versioning;

using System.Threading.Tasks;

namespace System.Xml
{
    // Represents a reader that provides fast, non-cached forward only stream access to XML data. 
    [DebuggerDisplay("{debuggerDisplayProxy}")]
    public abstract partial class XmlReader : IDisposable
    {
        public virtual Task<string> GetValueAsync()
        {
            throw new NotImplementedException();
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and returns the content as the most appropriate type (by default as string). Stops at start tags and end tags.
        public virtual async Task<object> ReadContentAsObjectAsync()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsObject));
            }
            return await InternalReadContentAsStringAsync().ConfigureAwait(false);
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and returns the content as a string. Stops at start tags and end tags.
        public virtual Task<string> ReadContentAsStringAsync()
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAsString));
            }
            return InternalReadContentAsStringAsync();
        }

        // Concatenates values of textual nodes of the current content, ignoring comments and PIs, expanding entity references, 
        // and converts the content to the requested type. Stops at start tags and end tags.
        public virtual async Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            if (!CanReadContentAs())
            {
                throw CreateReadContentAsException(nameof(ReadContentAs));
            }

            string strContentValue = await InternalReadContentAsStringAsync().ConfigureAwait(false);
            if (returnType == typeof(string))
            {
                return strContentValue;
            }
            else
            {
                try
                {
                    return XmlUntypedConverter.Untyped.ChangeType(strContentValue, returnType, (namespaceResolver == null ? this as IXmlNamespaceResolver : namespaceResolver));
                }
                catch (FormatException e)
                {
                    throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
                }
                catch (InvalidCastException e)
                {
                    throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
                }
            }
        }

        // Returns the content of the current element as the most appropriate type. Moves to the node following the element's end tag.
        public virtual async Task<object> ReadElementContentAsObjectAsync()
        {
            if (await SetupReadElementContentAsXxxAsync("ReadElementContentAsObject").ConfigureAwait(false))
            {
                object value = await ReadContentAsObjectAsync().ConfigureAwait(false);
                await FinishReadElementContentAsXxxAsync().ConfigureAwait(false);
                return value;
            }
            return string.Empty;
        }

        // Returns the content of the current element as a string. Moves to the node following the element's end tag.
        public virtual async Task<string> ReadElementContentAsStringAsync()
        {
            if (await SetupReadElementContentAsXxxAsync("ReadElementContentAsString").ConfigureAwait(false))
            {
                string value = await ReadContentAsStringAsync().ConfigureAwait(false);
                await FinishReadElementContentAsXxxAsync().ConfigureAwait(false);
                return value;
            }
            return string.Empty;
        }

        // Returns the content of the current element as the requested type. Moves to the node following the element's end tag.
        public virtual async Task<object> ReadElementContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            if (await SetupReadElementContentAsXxxAsync("ReadElementContentAs").ConfigureAwait(false))
            {
                object value = await ReadContentAsAsync(returnType, namespaceResolver).ConfigureAwait(false);
                await FinishReadElementContentAsXxxAsync().ConfigureAwait(false);
                return value;
            }
            return (returnType == typeof(string)) ? string.Empty : XmlUntypedConverter.Untyped.ChangeType(string.Empty, returnType, namespaceResolver);
        }

        // Moving through the Stream
        // Reads the next node from the stream.

        public virtual Task<bool> ReadAsync()
        {
            throw new NotImplementedException();
        }

        // Skips to the end tag of the current element.
        public virtual Task SkipAsync()
        {
            if (ReadState != ReadState.Interactive)
            {
                return Task.CompletedTask;
            }
            return SkipSubtreeAsync();
        }

        // Returns decoded bytes of the current base64 text content. Call this methods until it returns 0 to get all the data.
        public virtual Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Format(SR.Xml_ReadBinaryContentNotSupported, "ReadContentAsBase64"));
        }

        // Returns decoded bytes of the current base64 element content. Call this methods until it returns 0 to get all the data.
        public virtual Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Format(SR.Xml_ReadBinaryContentNotSupported, "ReadElementContentAsBase64"));
        }

        // Returns decoded bytes of the current binhex text content. Call this methods until it returns 0 to get all the data.
        public virtual Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Format(SR.Xml_ReadBinaryContentNotSupported, "ReadContentAsBinHex"));
        }

        // Returns decoded bytes of the current binhex element content. Call this methods until it returns 0 to get all the data.
        public virtual Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Format(SR.Xml_ReadBinaryContentNotSupported, "ReadElementContentAsBinHex"));
        }

        // Returns a chunk of the value of the current node. Call this method in a loop to get all the data. 
        // Use this method to get a streaming access to the value of the current node.
        public virtual Task<int> ReadValueChunkAsync(char[] buffer, int index, int count)
        {
            throw new NotSupportedException(SR.Xml_ReadValueChunkNotSupported);
        }

        // Checks whether the current node is a content (non-whitespace text, CDATA, Element, EndElement, EntityReference
        // or EndEntity) node. If the node is not a content node, then the method skips ahead to the next content node or 
        // end of file. Skips over nodes of type ProcessingInstruction, DocumentType, Comment, Whitespace and SignificantWhitespace.
        public virtual async Task<XmlNodeType> MoveToContentAsync()
        {
            do
            {
                switch (this.NodeType)
                {
                    case XmlNodeType.Attribute:
                        MoveToElement();
                        goto case XmlNodeType.Element;
                    case XmlNodeType.Element:
                    case XmlNodeType.EndElement:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                    case XmlNodeType.EntityReference:
                    case XmlNodeType.EndEntity:
                        return this.NodeType;
                }
            } while (await ReadAsync().ConfigureAwait(false));
            return this.NodeType;
        }

        // Returns the inner content (including markup) of an element or attribute as a string.
        public virtual async Task<string> ReadInnerXmlAsync()
        {
            if (ReadState != ReadState.Interactive)
            {
                return string.Empty;
            }
            if ((this.NodeType != XmlNodeType.Attribute) && (this.NodeType != XmlNodeType.Element))
            {
                await ReadAsync().ConfigureAwait(false);
                return string.Empty;
            }

            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            XmlWriter xtw = CreateWriterForInnerOuterXml(sw);

            try
            {
                if (this.NodeType == XmlNodeType.Attribute)
                {
                    ((XmlTextWriter)xtw).QuoteChar = this.QuoteChar;
                    WriteAttributeValue(xtw);
                }
                if (this.NodeType == XmlNodeType.Element)
                {
                    await this.WriteNodeAsync(xtw, false).ConfigureAwait(false);
                }
            }
            finally
            {
                xtw.Close();
            }
            return sw.ToString();
        }

        // Writes the content (inner XML) of the current node into the provided XmlWriter.
        private async Task WriteNodeAsync(XmlWriter xtw, bool defattr)
        {
            Debug.Assert(xtw is XmlTextWriter);
            int d = this.NodeType == XmlNodeType.None ? -1 : this.Depth;
            while (await this.ReadAsync().ConfigureAwait(false) && (d < this.Depth))
            {
                switch (this.NodeType)
                {
                    case XmlNodeType.Element:
                        xtw.WriteStartElement(this.Prefix, this.LocalName, this.NamespaceURI);
                        ((XmlTextWriter)xtw).QuoteChar = this.QuoteChar;
                        xtw.WriteAttributes(this, defattr);
                        if (this.IsEmptyElement)
                        {
                            xtw.WriteEndElement();
                        }
                        break;
                    case XmlNodeType.Text:
                        xtw.WriteString(await this.GetValueAsync().ConfigureAwait(false));
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        xtw.WriteWhitespace(await this.GetValueAsync().ConfigureAwait(false));
                        break;
                    case XmlNodeType.CDATA:
                        xtw.WriteCData(this.Value);
                        break;
                    case XmlNodeType.EntityReference:
                        xtw.WriteEntityRef(this.Name);
                        break;
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        xtw.WriteProcessingInstruction(this.Name, this.Value);
                        break;
                    case XmlNodeType.DocumentType:
                        xtw.WriteDocType(this.Name, this.GetAttribute("PUBLIC"), this.GetAttribute("SYSTEM"), this.Value);
                        break;
                    case XmlNodeType.Comment:
                        xtw.WriteComment(this.Value);
                        break;
                    case XmlNodeType.EndElement:
                        xtw.WriteFullEndElement();
                        break;
                }
            }
            if (d == this.Depth && this.NodeType == XmlNodeType.EndElement)
            {
                await ReadAsync().ConfigureAwait(false);
            }
        }

        // Returns the current element and its descendants or an attribute as a string.
        public virtual async Task<string> ReadOuterXmlAsync()
        {
            if (ReadState != ReadState.Interactive)
            {
                return string.Empty;
            }
            if ((this.NodeType != XmlNodeType.Attribute) && (this.NodeType != XmlNodeType.Element))
            {
                await ReadAsync().ConfigureAwait(false);
                return string.Empty;
            }

            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            XmlWriter xtw = CreateWriterForInnerOuterXml(sw);

            try
            {
                if (this.NodeType == XmlNodeType.Attribute)
                {
                    xtw.WriteStartAttribute(this.Prefix, this.LocalName, this.NamespaceURI);
                    WriteAttributeValue(xtw);
                    xtw.WriteEndAttribute();
                }
                else
                {
                    xtw.WriteNode(this, false);
                }
            }
            finally
            {
                xtw.Close();
            }
            return sw.ToString();
        }

        //
        // Private methods
        //
        //SkipSubTree is called whenever validation of the skipped subtree is required on a reader with XsdValidation
        private async Task<bool> SkipSubtreeAsync()
        {
            MoveToElement();
            if (NodeType == XmlNodeType.Element && !IsEmptyElement)
            {
                int depth = Depth;

                while (await ReadAsync().ConfigureAwait(false) && depth < Depth)
                {
                    // Nothing, just read on
                }

                // consume end tag
                if (NodeType == XmlNodeType.EndElement)
                    return await ReadAsync().ConfigureAwait(false);
            }
            else
            {
                return await ReadAsync().ConfigureAwait(false);
            }

            return false;
        }

        internal async Task<string> InternalReadContentAsStringAsync()
        {
            string value = string.Empty;
            StringBuilder sb = null;
            do
            {
                switch (this.NodeType)
                {
                    case XmlNodeType.Attribute:
                        return this.Value;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.CDATA:
                        // merge text content
                        if (value.Length == 0)
                        {
                            value = await this.GetValueAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            if (sb == null)
                            {
                                sb = new StringBuilder();
                                sb.Append(value);
                            }
                            sb.Append(await this.GetValueAsync().ConfigureAwait(false));
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                    case XmlNodeType.EndEntity:
                        // skip comments, pis and end entity nodes
                        break;
                    case XmlNodeType.EntityReference:
                        if (this.CanResolveEntity)
                        {
                            this.ResolveEntity();
                            break;
                        }
                        goto default;
                    case XmlNodeType.EndElement:
                    default:
                        goto ReturnContent;
                }
            } while ((this.AttributeCount != 0) ? this.ReadAttributeValue() : await this.ReadAsync().ConfigureAwait(false));

        ReturnContent:
            return (sb == null) ? value : sb.ToString();
        }

        private async Task<bool> SetupReadElementContentAsXxxAsync(string methodName)
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(methodName);
            }

            bool isEmptyElement = this.IsEmptyElement;

            // move to content or beyond the empty element
            await this.ReadAsync().ConfigureAwait(false);

            if (isEmptyElement)
            {
                return false;
            }

            XmlNodeType nodeType = this.NodeType;
            if (nodeType == XmlNodeType.EndElement)
            {
                await this.ReadAsync().ConfigureAwait(false);
                return false;
            }
            else if (nodeType == XmlNodeType.Element)
            {
                throw new XmlException(SR.Xml_MixedReadElementContentAs, string.Empty, this as IXmlLineInfo);
            }
            return true;
        }

        private Task FinishReadElementContentAsXxxAsync()
        {
            if (this.NodeType != XmlNodeType.EndElement)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, this.NodeType.ToString());
            }
            return this.ReadAsync();
        }
    }
}

