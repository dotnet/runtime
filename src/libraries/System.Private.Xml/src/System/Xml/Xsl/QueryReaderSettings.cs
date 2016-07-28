// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace System.Xml.Xsl
{
    internal class QueryReaderSettings
    {
        private bool _validatingReader;
        private XmlReaderSettings _xmlReaderSettings;
        private XmlNameTable _xmlNameTable;
        private EntityHandling _entityHandling;
        private bool _namespaces;
        private bool _normalization;
        private bool _prohibitDtd;
        private WhitespaceHandling _whitespaceHandling;
        private XmlResolver _xmlResolver;

        public QueryReaderSettings(XmlNameTable xmlNameTable)
        {
            Debug.Assert(xmlNameTable != null);
            _xmlReaderSettings = new XmlReaderSettings();
            _xmlReaderSettings.NameTable = xmlNameTable;
            _xmlReaderSettings.ConformanceLevel = ConformanceLevel.Document;
            _xmlReaderSettings.XmlResolver = null;
            _xmlReaderSettings.DtdProcessing = DtdProcessing.Prohibit;
            _xmlReaderSettings.CloseInput = true;
        }

        public QueryReaderSettings(XmlReader reader)
        {
#pragma warning disable 618
            XmlValidatingReader valReader = reader as XmlValidatingReader;
#pragma warning restore 618
            if (valReader != null)
            {
                // Unwrap validation reader
                _validatingReader = true;
                reader = valReader.Impl.Reader;
            }
            _xmlReaderSettings = reader.Settings;
            if (_xmlReaderSettings != null)
            {
                _xmlReaderSettings = _xmlReaderSettings.Clone();
                _xmlReaderSettings.NameTable = reader.NameTable;
                _xmlReaderSettings.CloseInput = true;
                _xmlReaderSettings.LineNumberOffset = 0;
                _xmlReaderSettings.LinePositionOffset = 0;
                XmlTextReaderImpl impl = reader as XmlTextReaderImpl;
                if (impl != null)
                {
                    _xmlReaderSettings.XmlResolver = impl.GetResolver();
                }
            }
            else
            {
                _xmlNameTable = reader.NameTable;
                XmlTextReader xmlTextReader = reader as XmlTextReader;
                if (xmlTextReader != null)
                {
                    XmlTextReaderImpl impl = xmlTextReader.Impl;
                    _entityHandling = impl.EntityHandling;
                    _namespaces = impl.Namespaces;
                    _normalization = impl.Normalization;
                    _prohibitDtd = (impl.DtdProcessing == DtdProcessing.Prohibit);
                    _whitespaceHandling = impl.WhitespaceHandling;
                    _xmlResolver = impl.GetResolver();
                }
                else
                {
                    _entityHandling = EntityHandling.ExpandEntities;
                    _namespaces = true;
                    _normalization = true;
                    _prohibitDtd = true;
                    _whitespaceHandling = WhitespaceHandling.All;
                    _xmlResolver = null;
                }
            }
        }

        public XmlReader CreateReader(Stream stream, string baseUri)
        {
            XmlReader reader;
            if (_xmlReaderSettings != null)
            {
                reader = XmlTextReader.Create(stream, _xmlReaderSettings, baseUri);
            }
            else
            {
                XmlTextReaderImpl readerImpl = new XmlTextReaderImpl(baseUri, stream, _xmlNameTable);
                readerImpl.EntityHandling = _entityHandling;
                readerImpl.Namespaces = _namespaces;
                readerImpl.Normalization = _normalization;
                readerImpl.DtdProcessing = _prohibitDtd ? DtdProcessing.Prohibit : DtdProcessing.Parse;
                readerImpl.WhitespaceHandling = _whitespaceHandling;
                readerImpl.XmlResolver = _xmlResolver;
                reader = readerImpl;
            }
            if (_validatingReader)
            {
#pragma warning disable 618
                reader = new XmlValidatingReader(reader);
#pragma warning restore 618
            }
            return reader;
        }

        public XmlNameTable NameTable
        {
            get { return _xmlReaderSettings != null ? _xmlReaderSettings.NameTable : _xmlNameTable; }
        }
    }
}
