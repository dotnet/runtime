// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Collections.Generic;
    using System.Xml.Schema;
    using System.Diagnostics;


    /// <summary>
    /// This writer wraps an XmlRawWriter and inserts additional lexical information into the resulting
    /// Xml 1.0 document:
    ///   1. CData sections
    ///   2. DocType declaration
    ///
    /// It also performs well-formed document checks if standalone="yes" and/or a doc-type-decl is output.
    /// </summary>
    internal sealed class QueryOutputWriter : XmlRawWriter
    {
        private readonly XmlRawWriter _wrapped;
        private bool _inCDataSection;
        private readonly Dictionary<XmlQualifiedName, int>? _lookupCDataElems;
        private readonly BitStack? _bitsCData;
        private readonly XmlQualifiedName? _qnameCData;
        private bool _outputDocType;
        private readonly bool _checkWellFormedDoc;
        private bool _hasDocElem;
        private bool _inAttr;
        private readonly string? _systemId, _publicId;
        private int _depth;

        public QueryOutputWriter(XmlRawWriter writer, XmlWriterSettings settings)
        {
            _wrapped = writer;

            _systemId = settings.DocTypeSystem;
            _publicId = settings.DocTypePublic;

            if (settings.OutputMethod == XmlOutputMethod.Xml)
            {
                // Xml output method shouldn't output doc-type-decl if system ID is not defined (even if public ID is)
                // Only check for well-formed document if output method is xml
                if (_systemId != null)
                {
                    _outputDocType = true;
                    _checkWellFormedDoc = true;
                }

                // Check for well-formed document if standalone="yes" in an auto-generated xml declaration
                if (settings.AutoXmlDeclaration && settings.Standalone == XmlStandalone.Yes)
                    _checkWellFormedDoc = true;

                if (settings.CDataSectionElements.Count > 0)
                {
                    _bitsCData = new BitStack();
                    _lookupCDataElems = new Dictionary<XmlQualifiedName, int>();
                    _qnameCData = new XmlQualifiedName();

                    // Add each element name to the lookup table
                    foreach (XmlQualifiedName name in settings.CDataSectionElements)
                    {
                        _lookupCDataElems[name] = 0;
                    }

                    _bitsCData.PushBit(false);
                }
            }
            else if (settings.OutputMethod == XmlOutputMethod.Html)
            {
                // Html output method should output doc-type-decl if system ID or public ID is defined
                if (_systemId != null || _publicId != null)
                    _outputDocType = true;
            }
        }


        //-----------------------------------------------
        // XmlWriter interface
        //-----------------------------------------------

        /// <summary>
        /// Get and set the namespace resolver that's used by this RawWriter to resolve prefixes.
        /// </summary>
        internal override IXmlNamespaceResolver? NamespaceResolver
        {
            get
            {
                return this._resolver;
            }
            set
            {
                this._resolver = value;
                _wrapped.NamespaceResolver = value;
            }
        }

        /// <summary>
        /// Write the xml declaration.  This must be the first call.
        /// </summary>
        internal override void WriteXmlDeclaration(XmlStandalone standalone)
        {
            _wrapped.WriteXmlDeclaration(standalone);
        }

        internal override void WriteXmlDeclaration(string xmldecl)
        {
            _wrapped.WriteXmlDeclaration(xmldecl);
        }

        /// <summary>
        /// Return settings provided to factory.
        /// </summary>
        public override XmlWriterSettings Settings
        {
            get
            {
                XmlWriterSettings settings = _wrapped.Settings!;

                settings.ReadOnly = false;
                settings.DocTypeSystem = _systemId;
                settings.DocTypePublic = _publicId;
                settings.ReadOnly = true;

                return settings;
            }
        }

        /// <summary>
        /// Suppress this explicit call to WriteDocType if information was provided by XmlWriterSettings.
        /// </summary>
        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            if (_publicId == null && _systemId == null)
            {
                Debug.Assert(!_outputDocType);
                _wrapped.WriteDocType(name, pubid, sysid, subset);
            }
        }

        /// <summary>
        /// Check well-formedness, possibly output doc-type-decl, and determine whether this element is a
        /// CData section element.
        /// </summary>
        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            EndCDataSection();

            if (_checkWellFormedDoc)
            {
                // Don't allow multiple document elements
                if (_depth == 0 && _hasDocElem)
                    throw new XmlException(SR.Xml_NoMultipleRoots, string.Empty);

                _depth++;
                _hasDocElem = true;
            }

            // Output doc-type declaration immediately before first element is output
            if (_outputDocType)
            {
                _wrapped.WriteDocType(
                        string.IsNullOrEmpty(prefix) ? localName : prefix + ":" + localName,
                        _publicId,
                        _systemId,
                        null);

                _outputDocType = false;
            }

            _wrapped.WriteStartElement(prefix, localName, ns);

            if (_lookupCDataElems != null)
            {
                // Determine whether this element is a CData section element
                _qnameCData!.Init(localName, ns);
                _bitsCData!.PushBit(_lookupCDataElems.ContainsKey(_qnameCData));
            }
        }

        internal override void WriteEndElement(string prefix, string localName, string ns)
        {
            EndCDataSection();

            _wrapped.WriteEndElement(prefix, localName, ns);

            if (_checkWellFormedDoc)
                _depth--;

            if (_lookupCDataElems != null)
                _bitsCData!.PopBit();
        }

        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
            EndCDataSection();

            _wrapped.WriteFullEndElement(prefix, localName, ns);

            if (_checkWellFormedDoc)
                _depth--;

            if (_lookupCDataElems != null)
                _bitsCData!.PopBit();
        }

        internal override void StartElementContent()
        {
            _wrapped.StartElementContent();
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            _inAttr = true;
            _wrapped.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteEndAttribute()
        {
            _inAttr = false;
            _wrapped.WriteEndAttribute();
        }

        internal override void WriteNamespaceDeclaration(string prefix, string ns)
        {
            _wrapped.WriteNamespaceDeclaration(prefix, ns);
        }

        internal override bool SupportsNamespaceDeclarationInChunks
        {
            get
            {
                return _wrapped.SupportsNamespaceDeclarationInChunks;
            }
        }

        internal override void WriteStartNamespaceDeclaration(string prefix)
        {
            _wrapped.WriteStartNamespaceDeclaration(prefix);
        }

        internal override void WriteEndNamespaceDeclaration()
        {
            _wrapped.WriteEndNamespaceDeclaration();
        }

        public override void WriteCData(string? text)
        {
            _wrapped.WriteCData(text);
        }

        public override void WriteComment(string? text)
        {
            EndCDataSection();
            _wrapped.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            EndCDataSection();
            _wrapped.WriteProcessingInstruction(name, text);
        }

        public override void WriteWhitespace(string? ws)
        {
            if (!_inAttr && (_inCDataSection || StartCDataSection()))
                _wrapped.WriteCData(ws);
            else
                _wrapped.WriteWhitespace(ws);
        }

        public override void WriteString(string? text)
        {
            if (!_inAttr && (_inCDataSection || StartCDataSection()))
                _wrapped.WriteCData(text);
            else
                _wrapped.WriteString(text);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            if (!_inAttr && (_inCDataSection || StartCDataSection()))
                _wrapped.WriteCData(new string(buffer, index, count));
            else
                _wrapped.WriteChars(buffer, index, count);
        }

        public override void WriteEntityRef(string name)
        {
            EndCDataSection();
            _wrapped.WriteEntityRef(name);
        }

        public override void WriteCharEntity(char ch)
        {
            EndCDataSection();
            _wrapped.WriteCharEntity(ch);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            EndCDataSection();
            _wrapped.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            if (!_inAttr && (_inCDataSection || StartCDataSection()))
                _wrapped.WriteCData(new string(buffer, index, count));
            else
                _wrapped.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            if (!_inAttr && (_inCDataSection || StartCDataSection()))
                _wrapped.WriteCData(data);
            else
                _wrapped.WriteRaw(data);
        }

        public override void Close()
        {
            _wrapped.Close();

            if (_checkWellFormedDoc && !_hasDocElem)
            {
                // Need at least one document element
                throw new XmlException(SR.Xml_NoRoot, string.Empty);
            }
        }

        public override void Flush()
        {
            _wrapped.Flush();
        }


        //-----------------------------------------------
        // Helper methods
        //-----------------------------------------------

        /// <summary>
        /// Write CData text if element is a CData element.  Return true if text should be written
        /// within a CData section.
        /// </summary>
        private bool StartCDataSection()
        {
            Debug.Assert(!_inCDataSection);
            if (_lookupCDataElems != null && _bitsCData!.PeekBit())
            {
                _inCDataSection = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// No longer write CData text.
        /// </summary>
        private void EndCDataSection()
        {
            _inCDataSection = false;
        }
    }
}
