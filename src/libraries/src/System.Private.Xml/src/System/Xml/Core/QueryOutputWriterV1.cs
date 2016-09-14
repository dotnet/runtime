// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Xml
{
    /// <summary>
    /// This writer wraps an XmlWriter that was not build using the XmlRawWriter architecture (such as XmlTextWriter or a custom XmlWriter) 
    /// for use in the XslCompilerTransform. Depending on the Xsl stylesheet output settings (which gets transfered to this writer via the 
    /// internal properties of XmlWriterSettings) this writer will inserts additional lexical information into the resulting Xml 1.0 document:
    /// 
    ///   1. CData sections
    ///   2. DocType declaration
    ///   3. Standalone attribute
    ///
    /// It also calls WriteStateDocument if standalone="yes" and/or a DocType declaration is written out in order to enforce document conformance
    /// checking.
    /// </summary>
    internal class QueryOutputWriterV1 : XmlWriter
    {
        private XmlWriter _wrapped;
        private bool _inCDataSection;
        private Dictionary<XmlQualifiedName, XmlQualifiedName> _lookupCDataElems;
        private BitStack _bitsCData;
        private XmlQualifiedName _qnameCData;
        private bool _outputDocType, _inAttr;
        private string _systemId, _publicId;
        private XmlStandalone _standalone;

        public QueryOutputWriterV1(XmlWriter writer, XmlWriterSettings settings)
        {
            _wrapped = writer;

            _systemId = settings.DocTypeSystem;
            _publicId = settings.DocTypePublic;

            if (settings.OutputMethod == XmlOutputMethod.Xml)
            {
                bool documentConformance = false;

                // Xml output method shouldn't output doc-type-decl if system ID is not defined (even if public ID is)
                // Only check for well-formed document if output method is xml
                if (_systemId != null)
                {
                    documentConformance = true;
                    _outputDocType = true;
                }

                // Check for well-formed document if standalone="yes" in an auto-generated xml declaration
                if (settings.Standalone == XmlStandalone.Yes)
                {
                    documentConformance = true;
                    _standalone = settings.Standalone;
                }

                if (documentConformance)
                {
                    if (settings.Standalone == XmlStandalone.Yes)
                    {
                        _wrapped.WriteStartDocument(true);
                    }
                    else
                    {
                        _wrapped.WriteStartDocument();
                    }
                }

                if (settings.CDataSectionElements != null && settings.CDataSectionElements.Count > 0)
                {
                    _bitsCData = new BitStack();
                    _lookupCDataElems = new Dictionary<XmlQualifiedName, XmlQualifiedName>();
                    _qnameCData = new XmlQualifiedName();

                    // Add each element name to the lookup table
                    foreach (XmlQualifiedName name in settings.CDataSectionElements)
                    {
                        _lookupCDataElems[name] = null;
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

        public override WriteState WriteState
        {
            get
            {
                return _wrapped.WriteState;
            }
        }

        public override void WriteStartDocument()
        {
            _wrapped.WriteStartDocument();
        }

        public override void WriteStartDocument(bool standalone)
        {
            _wrapped.WriteStartDocument(standalone);
        }

        public override void WriteEndDocument()
        {
            _wrapped.WriteEndDocument();
        }

        /// <summary>
        /// Suppress this explicit call to WriteDocType if information was provided by XmlWriterSettings.
        /// </summary>
        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            if (_publicId == null && _systemId == null)
            {
                Debug.Assert(!_outputDocType);
                _wrapped.WriteDocType(name, pubid, sysid, subset);
            }
        }

        /// <summary>
        /// Output doc-type-decl on the first element, and determine whether this element is a
        /// CData section element.
        /// </summary>
        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            EndCDataSection();

            // Output doc-type declaration immediately before first element is output
            if (_outputDocType)
            {
                WriteState ws = _wrapped.WriteState;
                if (ws == WriteState.Start || ws == WriteState.Prolog)
                {
                    _wrapped.WriteDocType(
                            prefix.Length != 0 ? prefix + ":" + localName : localName,
                            _publicId,
                            _systemId,
                            null);
                }
                _outputDocType = false;
            }

            _wrapped.WriteStartElement(prefix, localName, ns);

            if (_lookupCDataElems != null)
            {
                // Determine whether this element is a CData section element
                _qnameCData.Init(localName, ns);
                _bitsCData.PushBit(_lookupCDataElems.ContainsKey(_qnameCData));
            }
        }

        public override void WriteEndElement()
        {
            EndCDataSection();

            _wrapped.WriteEndElement();

            if (_lookupCDataElems != null)
                _bitsCData.PopBit();
        }

        public override void WriteFullEndElement()
        {
            EndCDataSection();

            _wrapped.WriteFullEndElement();

            if (_lookupCDataElems != null)
                _bitsCData.PopBit();
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            _inAttr = true;
            _wrapped.WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteEndAttribute()
        {
            _inAttr = false;
            _wrapped.WriteEndAttribute();
        }

        public override void WriteCData(string text)
        {
            _wrapped.WriteCData(text);
        }

        public override void WriteComment(string text)
        {
            EndCDataSection();
            _wrapped.WriteComment(text);
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            EndCDataSection();
            _wrapped.WriteProcessingInstruction(name, text);
        }

        public override void WriteWhitespace(string ws)
        {
            if (!_inAttr && (_inCDataSection || StartCDataSection()))
                _wrapped.WriteCData(ws);
            else
                _wrapped.WriteWhitespace(ws);
        }

        public override void WriteString(string text)
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

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            if (!_inAttr && (_inCDataSection || StartCDataSection()))
                _wrapped.WriteBase64(buffer, index, count);
            else
                _wrapped.WriteBase64(buffer, index, count);
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
        }

        public override void Flush()
        {
            _wrapped.Flush();
        }

        public override string LookupPrefix(string ns)
        {
            return _wrapped.LookupPrefix(ns);
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
            if (_lookupCDataElems != null && _bitsCData.PeekBit())
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

