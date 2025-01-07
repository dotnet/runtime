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
        public override Task WriteStartDocumentAsync()
        {
            return writer.WriteStartDocumentAsync();
        }

        public override Task WriteStartDocumentAsync(bool standalone)
        {
            return writer.WriteStartDocumentAsync(standalone);
        }

        public override Task WriteEndDocumentAsync()
        {
            return writer.WriteEndDocumentAsync();
        }

        public override Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
        {
            return writer.WriteDocTypeAsync(name, pubid, sysid, subset);
        }

        public override Task WriteStartElementAsync(string? prefix, string localName, string? ns)
        {
            return writer.WriteStartElementAsync(prefix, localName, ns);
        }

        public override Task WriteEndElementAsync()
        {
            return writer.WriteEndElementAsync();
        }

        public override Task WriteFullEndElementAsync()
        {
            return writer.WriteFullEndElementAsync();
        }

        protected internal override Task WriteStartAttributeAsync(string? prefix, string localName, string? ns)
        {
            return writer.WriteStartAttributeAsync(prefix, localName, ns);
        }

        protected internal override Task WriteEndAttributeAsync()
        {
            return writer.WriteEndAttributeAsync();
        }

        public override Task WriteCDataAsync(string? text)
        {
            return writer.WriteCDataAsync(text);
        }

        public override Task WriteCommentAsync(string? text)
        {
            return writer.WriteCommentAsync(text);
        }

        public override Task WriteProcessingInstructionAsync(string name, string? text)
        {
            return writer.WriteProcessingInstructionAsync(name, text);
        }

        public override Task WriteEntityRefAsync(string name)
        {
            return writer.WriteEntityRefAsync(name);
        }

        public override Task WriteCharEntityAsync(char ch)
        {
            return writer.WriteCharEntityAsync(ch);
        }

        public override Task WriteWhitespaceAsync(string? ws)
        {
            return writer.WriteWhitespaceAsync(ws);
        }

        public override Task WriteStringAsync(string? text)
        {
            return writer.WriteStringAsync(text);
        }

        public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            return writer.WriteSurrogateCharEntityAsync(lowChar, highChar);
        }

        public override Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            return writer.WriteCharsAsync(buffer, index, count);
        }

        public override Task WriteRawAsync(char[] buffer, int index, int count)
        {
            return writer.WriteRawAsync(buffer, index, count);
        }

        public override Task WriteRawAsync(string data)
        {
            return writer.WriteRawAsync(data);
        }

        public override Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            return writer.WriteBase64Async(buffer, index, count);
        }

        public override Task FlushAsync()
        {
            return writer.FlushAsync();
        }
    }
}
