// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    public static class JsonReaderWriterFactory
    {
        private const string DefaultIndentChars = "  ";

        public static XmlDictionaryReader CreateJsonReader(Stream stream, XmlDictionaryReaderQuotas quotas)
        {
            return CreateJsonReader(stream, null, quotas, null);
        }

        public static XmlDictionaryReader CreateJsonReader(byte[] buffer, XmlDictionaryReaderQuotas quotas)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            return CreateJsonReader(buffer, 0, buffer.Length, null, quotas, null);
        }

        public static XmlDictionaryReader CreateJsonReader(Stream stream, Encoding? encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose)
        {
            XmlJsonReader reader = new XmlJsonReader();
            reader.SetInput(stream, encoding, quotas, onClose);
            return reader;
        }

        internal static async ValueTask<XmlDictionaryReader> CreateJsonReaderAsync(Stream stream, Encoding? encoding,
            XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose, CancellationToken cancellationToken)
        {
            XmlJsonReader reader = new XmlJsonReader();
            await reader.SetInputAsync(stream, encoding, quotas, onClose, cancellationToken).ConfigureAwait(false);
            return reader;
        }

        public static XmlDictionaryReader CreateJsonReader(byte[] buffer, int offset, int count, XmlDictionaryReaderQuotas quotas)
        {
            return CreateJsonReader(buffer, offset, count, null, quotas, null);
        }

        public static XmlDictionaryReader CreateJsonReader(byte[] buffer, int offset, int count, Encoding? encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose)
        {
            XmlJsonReader reader = new XmlJsonReader();
            reader.SetInput(buffer, offset, count, encoding, quotas, onClose);
            return reader;
        }

        public static XmlDictionaryWriter CreateJsonWriter(Stream stream)
        {
            return CreateJsonWriter(stream, Encoding.UTF8, true);
        }

        public static XmlDictionaryWriter CreateJsonWriter(Stream stream, Encoding encoding)
        {
            return CreateJsonWriter(stream, encoding, true);
        }

        public static XmlDictionaryWriter CreateJsonWriter(Stream stream, Encoding encoding, bool ownsStream)
        {
            return CreateJsonWriter(stream, encoding, ownsStream, false);
        }

        public static XmlDictionaryWriter CreateJsonWriter(Stream stream, Encoding encoding, bool ownsStream, bool indent)
        {
            return CreateJsonWriter(stream, encoding, ownsStream, indent, JsonReaderWriterFactory.DefaultIndentChars);
        }

        public static XmlDictionaryWriter CreateJsonWriter(Stream stream, Encoding encoding, bool ownsStream, bool indent, string? indentChars)
        {
            XmlJsonWriter writer = new XmlJsonWriter(indent, indentChars);
            writer.SetOutput(stream, encoding, ownsStream);
            return writer;
        }

        internal static async ValueTask<XmlDictionaryWriter> CreateJsonWriterAsync(Stream stream, Encoding encoding, bool ownsStream,
            bool indent, string? indentChars, CancellationToken cancellationToken)
        {
            XmlJsonWriter writer = new XmlJsonWriter(indent, indentChars);
            await writer.SetOutputAsync(stream, encoding, ownsStream, cancellationToken).ConfigureAwait(false);
            return new XmlDictionaryAsyncCheckWriter(writer);
        }
    }
}
