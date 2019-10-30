// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    internal class XmlBuffer
    {
        private readonly byte[] _buffer;

        public XmlBuffer(string filePath)
            : this(File.ReadAllBytes(filePath))
        {
        }

        internal XmlBuffer(byte[] buffer)
        {
            _buffer = buffer;
        }

        public Stream CreateStream()
        {
            return new MemoryStream(_buffer);
        }

        public XmlReader CreateTextReader()
        {
            return XmlReader.Create(CreateStream());
        }

        public XmlDictionaryReader CreateDictionaryReader()
        {
            return XmlDictionaryReader.CreateTextReader(_buffer, XmlDictionaryReaderQuotas.Max);
        }

        public XmlReader CreateReader()
        {
            return CreateDictionaryReader();
        }

        public XmlReader CreateReaderAt(string elementToStartAt)
        {
            XmlReader reader = CreateReader();
            for (reader.MoveToContent(); reader.ReadState == ReadState.Interactive; reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == elementToStartAt)
                {
                    return reader;
                }
            }

            throw new ArgumentException("elementToStartAt", string.Format(
                "The element '{0}' was not found in the XML input", elementToStartAt));
        }

        public XmlDocument CreateDocument()
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(CreateTextReader());
            return doc;
        }

        public XmlNodeList CreateSubtreeNodeList(string elementToStartAt)
        {
            return XPathHelper.CreateSubtreeNodeList(CreateDocument(), elementToStartAt);
        }
    }
}
