// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    using XmlDsigExcC14NTransform = System.Security.Cryptography.Xml.XmlDsigExcC14NTransform;

    internal class Engine
    {
        private readonly bool _fullDocumentMode;
        private readonly bool _includeComments;
        private readonly string _inclusivePrefixes;
        private readonly string[] _tokenizedInclusivePrefixes;
        private readonly MemoryStream _canonicalWriterStream;
        private readonly CanonicalWriter _canonicalWriter;
        private readonly CanonicalEncoder _encoder;

        public Engine(bool includeComments, string inclusivePrefixes, bool fullDocumentMode)
        {
            _fullDocumentMode = fullDocumentMode;
            _includeComments = includeComments;
            _inclusivePrefixes = inclusivePrefixes;
            _tokenizedInclusivePrefixes = C14nUtil.TokenizeInclusivePrefixList(inclusivePrefixes);
            _canonicalWriterStream = new MemoryStream();
            _encoder = new CanonicalEncoder(_canonicalWriterStream);
            _canonicalWriter = new CanonicalWriter(_encoder, _tokenizedInclusivePrefixes, includeComments, null, 0);
        }

        public byte[] CanonicalizeUsingDictionaryReader(XmlReader reader)
        {
            _canonicalWriterStream.Seek(0, SeekOrigin.Begin);
            _canonicalWriterStream.SetLength(0);

            XmlDictionaryReader dicReader = XmlDictionaryReader.CreateDictionaryReader(reader);
            dicReader.MoveToContent();

            dicReader.StartCanonicalization(_canonicalWriterStream, _includeComments, _tokenizedInclusivePrefixes);
            dicReader.Skip();
            dicReader.EndCanonicalization();

            return _canonicalWriterStream.ToArray();
        }

        public byte[] CanonicalizeUsingWriter(XmlReader reader)
        {
            _canonicalWriter.Reset();
            _canonicalWriterStream.Seek(0, SeekOrigin.Begin);
            _canonicalWriterStream.SetLength(0);
            _canonicalWriter.IncludeComments = _includeComments;
            _canonicalWriter.SetInclusivePrefixes(_tokenizedInclusivePrefixes);
            _canonicalWriter.ContextProvider = (IAncestralNamespaceContextProvider)AncestralNamespaceContextProviderProxy.CreateContextProvider(reader);

            reader.MoveToContent();
            _canonicalWriter.WriteNode(reader, false);

            _canonicalWriter.FlushWriterAndEncoder();
            return _canonicalWriterStream.ToArray();
        }

        public byte[] CanonicalizeUsingDictionaryWriter(XmlReader reader)
        {
            _canonicalWriterStream.Seek(0, SeekOrigin.Begin);
            _canonicalWriterStream.SetLength(0);

            MemoryStream writerStream = new MemoryStream();
            XmlDictionaryWriter dicWriter = XmlDictionaryWriter.CreateTextWriter(writerStream);

            dicWriter.WriteStartElement("Foo");
            if (_tokenizedInclusivePrefixes != null)
            {
                // Populate the Canonicalizer with prefix that are already read in 
                // by the reader.
                foreach (string inclusivePrefix in _tokenizedInclusivePrefixes)
                {
                    string ns = reader.LookupNamespace(inclusivePrefix);
                    if (ns != null)
                    {
                        dicWriter.WriteXmlnsAttribute(inclusivePrefix, ns);
                    }
                }
            }

            dicWriter.StartCanonicalization(_canonicalWriterStream, _includeComments, _tokenizedInclusivePrefixes);
            reader.MoveToContent();
            dicWriter.WriteNode(reader, false);

            dicWriter.EndCanonicalization();
            dicWriter.WriteEndElement();

            return _canonicalWriterStream.ToArray();
        }

        public byte[] CanonicalizeUsingClrLibrary(object input)
        {
            XmlDsigExcC14NTransform t = new XmlDsigExcC14NTransform(_includeComments, _inclusivePrefixes);
            t.LoadInput(input);
            MemoryStream s = (MemoryStream)t.GetOutput(typeof(Stream));
            return s.ToArray();
        }
    }
}

