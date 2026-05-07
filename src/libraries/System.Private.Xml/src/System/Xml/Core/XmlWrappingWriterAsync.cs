// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml
{
    internal partial class XmlWrappingWriter : XmlWriter
    {
        public override async Task WriteStartDocumentAsync()
        {
            await writer.WriteStartDocumentAsync().ConfigureAwait(false);
        }

        public override async Task WriteStartDocumentAsync(bool standalone)
        {
            await writer.WriteStartDocumentAsync(standalone).ConfigureAwait(false);
        }

        public override async Task WriteEndDocumentAsync()
        {
            await writer.WriteEndDocumentAsync().ConfigureAwait(false);
        }

        public override async Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
        {
            await writer.WriteDocTypeAsync(name, pubid, sysid, subset).ConfigureAwait(false);
        }

        public override async Task WriteStartElementAsync(string? prefix, string localName, string? ns)
        {
            await writer.WriteStartElementAsync(prefix, localName, ns).ConfigureAwait(false);
        }

        public override async Task WriteEndElementAsync()
        {
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        public override async Task WriteFullEndElementAsync()
        {
            await writer.WriteFullEndElementAsync().ConfigureAwait(false);
        }

        protected internal override async Task WriteStartAttributeAsync(string? prefix, string localName, string? ns)
        {
            await writer.WriteStartAttributeAsync(prefix, localName, ns).ConfigureAwait(false);
        }

        protected internal override async Task WriteEndAttributeAsync()
        {
            await writer.WriteEndAttributeAsync().ConfigureAwait(false);
        }

        public override async Task WriteCDataAsync(string? text)
        {
            await writer.WriteCDataAsync(text).ConfigureAwait(false);
        }

        public override async Task WriteCommentAsync(string? text)
        {
            await writer.WriteCommentAsync(text).ConfigureAwait(false);
        }

        public override async Task WriteProcessingInstructionAsync(string name, string? text)
        {
            await writer.WriteProcessingInstructionAsync(name, text).ConfigureAwait(false);
        }

        public override async Task WriteEntityRefAsync(string name)
        {
            await writer.WriteEntityRefAsync(name).ConfigureAwait(false);
        }

        public override async Task WriteCharEntityAsync(char ch)
        {
            await writer.WriteCharEntityAsync(ch).ConfigureAwait(false);
        }

        public override async Task WriteWhitespaceAsync(string? ws)
        {
            await writer.WriteWhitespaceAsync(ws).ConfigureAwait(false);
        }

        public override async Task WriteStringAsync(string? text)
        {
            await writer.WriteStringAsync(text).ConfigureAwait(false);
        }

        public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            await writer.WriteSurrogateCharEntityAsync(lowChar, highChar).ConfigureAwait(false);
        }

        public override async Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            await writer.WriteCharsAsync(buffer, index, count).ConfigureAwait(false);
        }

        public override async Task WriteRawAsync(char[] buffer, int index, int count)
        {
            await writer.WriteRawAsync(buffer, index, count).ConfigureAwait(false);
        }

        public override async Task WriteRawAsync(string data)
        {
            await writer.WriteRawAsync(data).ConfigureAwait(false);
        }

        public override async Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            await writer.WriteBase64Async(buffer, index, count).ConfigureAwait(false);
        }

        public override async Task FlushAsync()
        {
            await writer.FlushAsync().ConfigureAwait(false);
        }
    }
}
