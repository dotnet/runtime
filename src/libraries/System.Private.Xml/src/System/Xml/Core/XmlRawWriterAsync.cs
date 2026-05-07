// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml
{
    /// <summary>
    /// Implementations of XmlRawWriter are intended to be wrapped by the XmlWellFormedWriter.  The
    /// well-formed writer performs many checks in behalf of the raw writer, and keeps state that the
    /// raw writer otherwise would have to keep.  Therefore, the well-formed writer will call the
    /// XmlRawWriter using the following rules, in order to make raw writers easier to implement:
    ///
    ///  1. The well-formed writer keeps a stack of element names, and always calls
    ///     WriteEndElement(string, string, string) instead of WriteEndElement().
    ///  2. The well-formed writer tracks namespaces, and will pass himself in via the
    ///     WellformedWriter property. It is used in the XmlRawWriter's implementation of IXmlNamespaceResolver.
    ///     Thus, LookupPrefix does not have to be implemented.
    ///  3. The well-formed writer tracks write states, so the raw writer doesn't need to.
    ///  4. The well-formed writer will always call StartElementContent.
    ///  5. The well-formed writer will always call WriteNamespaceDeclaration for namespace nodes,
    ///     rather than calling WriteStartAttribute(). If the writer is supporting namespace declarations in chunks
    ///     (SupportsNamespaceDeclarationInChunks is true), the XmlWellFormedWriter will call WriteStartNamespaceDeclaration,
    ///      then any method that can be used to write out a value of an attribute (WriteString, WriteChars, WriteRaw, WriteCharEntity...)
    ///      and then WriteEndNamespaceDeclaration - instead of just a single WriteNamespaceDeclaration call. This feature will be
    ///      supported by raw writers serializing to text that wish to preserve the attribute value escaping etc.
    ///  6. The well-formed writer guarantees a well-formed document, including correct call sequences,
    ///     correct namespaces, and correct document rule enforcement.
    ///  7. All element and attribute names will be fully resolved and validated.  Null will never be
    ///     passed for any of the name parts.
    ///  8. The well-formed writer keeps track of xml:space and xml:lang.
    ///  9. The well-formed writer verifies NmToken, Name, and QName values and calls WriteString().
    /// </summary>
    internal abstract partial class XmlRawWriter : XmlWriter
    {
        //
        // XmlWriter implementation
        //
        // Raw writers do not have to track whether this is a well-formed document.
        public override Task WriteStartDocumentAsync()
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override Task WriteStartDocumentAsync(bool standalone)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override Task WriteEndDocumentAsync()
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
        {
            return Task.CompletedTask;
        }

        // Raw writers do not have to keep a stack of element names.
        public override Task WriteEndElementAsync()
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // Raw writers do not have to keep a stack of element names.
        public override Task WriteFullEndElementAsync()
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // By default, convert base64 value to string and call WriteString.
        public override async Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            _base64Encoder ??= new XmlRawWriterBase64Encoder(this);

            // Encode will call WriteRaw to write out the encoded characters
            await _base64Encoder.EncodeAsync(buffer, index, count).ConfigureAwait(false);
        }

        // Raw writers do not have to verify NmToken values.
        public override Task WriteNmTokenAsync(string name)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // Raw writers do not have to verify Name values.
        public override Task WriteNameAsync(string name)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // Raw writers do not have to verify QName values.
        public override Task WriteQualifiedNameAsync(string localName, string? ns)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        // Forward call to WriteString(string).
        public override async Task WriteCDataAsync(string? text)
        {
            await WriteStringAsync(text).ConfigureAwait(false);
        }

        // Forward call to WriteString(string).
        public override async Task WriteCharEntityAsync(char ch)
        {
            await WriteStringAsync(char.ToString(ch)).ConfigureAwait(false);
        }

        // Forward call to WriteString(string).
        public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            await WriteStringAsync(new string([lowChar, highChar])).ConfigureAwait(false);
        }

        // Forward call to WriteString(string).
        public override async Task WriteWhitespaceAsync(string? ws)
        {
            await WriteStringAsync(ws).ConfigureAwait(false);
        }

        // Forward call to WriteString(string).
        public override async Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            await WriteStringAsync(new string(buffer, index, count)).ConfigureAwait(false);
        }

        // Forward call to WriteString(string).
        public override async Task WriteRawAsync(char[] buffer, int index, int count)
        {
            await WriteStringAsync(new string(buffer, index, count)).ConfigureAwait(false);
        }

        // Forward call to WriteString(string).
        public override async Task WriteRawAsync(string data)
        {
            await WriteStringAsync(data).ConfigureAwait(false);
        }

        // Copying to XmlRawWriter is not currently supported.
        public override Task WriteAttributesAsync(XmlReader reader, bool defattr)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override Task WriteNodeAsync(XmlReader reader, bool defattr)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override Task WriteNodeAsync(System.Xml.XPath.XPathNavigator navigator, bool defattr)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        //
        // XmlRawWriter methods and properties
        //

        // Write the xml declaration.  This must be the first call.
        internal virtual Task WriteXmlDeclarationAsync(XmlStandalone standalone)
        {
            return Task.CompletedTask;
        }
        internal virtual Task WriteXmlDeclarationAsync(string xmldecl)
        {
            return Task.CompletedTask;
        }

        // WriteEndElement() and WriteFullEndElement() overloads, in which caller gives the full name of the
        // element, so that raw writers do not need to keep a stack of element names.  This method should
        // always be called instead of WriteEndElement() or WriteFullEndElement() without parameters.

        internal virtual Task WriteEndElementAsync(string prefix, string localName, string ns)
        {
            throw new NotImplementedException();
        }

        internal virtual async Task WriteFullEndElementAsync(string prefix, string localName, string ns)
        {
            await WriteEndElementAsync(prefix, localName, ns).ConfigureAwait(false);
        }

        internal virtual async Task WriteQualifiedNameAsync(string prefix, string localName, string? ns)
        {
            if (prefix.Length != 0)
            {
                await WriteStringAsync(prefix).ConfigureAwait(false);
                await WriteStringAsync(":").ConfigureAwait(false);
            }

            await WriteStringAsync(localName).ConfigureAwait(false);
        }

        // This method must be called instead of WriteStartAttribute() for namespaces.

        internal virtual Task WriteNamespaceDeclarationAsync(string prefix, string ns)
        {
            throw new NotImplementedException();
        }

        internal virtual Task WriteStartNamespaceDeclarationAsync(string prefix)
        {
            throw new NotSupportedException();
        }

        internal virtual Task WriteEndNamespaceDeclarationAsync()
        {
            throw new NotSupportedException();
        }

        // This is called when the remainder of a base64 value should be output.
        internal virtual async Task WriteEndBase64Async()
        {
            // The Flush will call WriteRaw to write out the rest of the encoded characters
            Debug.Assert(_base64Encoder != null);
            await _base64Encoder.FlushAsync().ConfigureAwait(false);
        }

        internal virtual async ValueTask DisposeAsyncCore(WriteState currentState)
        {
            await DisposeAsyncCore().ConfigureAwait(false);
        }
    }
}
