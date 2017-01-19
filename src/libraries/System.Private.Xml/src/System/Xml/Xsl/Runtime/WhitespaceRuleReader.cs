// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;
using MS.Internal.Xml;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// </summary>
    internal class WhitespaceRuleReader : XmlWrappingReader
    {
        private WhitespaceRuleLookup _wsRules;
        private BitStack _stkStrip;
        private bool _shouldStrip, _preserveAdjacent;
        private string _val;
        private XmlCharType _xmlCharType = XmlCharType.Instance;

        public static XmlReader CreateReader(XmlReader baseReader, WhitespaceRuleLookup wsRules)
        {
            if (wsRules == null)
            {
                return baseReader;    // There is no rules to process
            }
            XmlReaderSettings readerSettings = baseReader.Settings;
            if (readerSettings != null)
            {
                if (readerSettings.IgnoreWhitespace)
                {
                    return baseReader;        // V2 XmlReader that strips all WS
                }
            }
            else
            {
                XmlTextReader txtReader = baseReader as XmlTextReader;
                if (txtReader != null && txtReader.WhitespaceHandling == WhitespaceHandling.None)
                {
                    return baseReader;        // V1 XmlTextReader that strips all WS
                }
                XmlTextReaderImpl txtReaderImpl = baseReader as XmlTextReaderImpl;
                if (txtReaderImpl != null && txtReaderImpl.WhitespaceHandling == WhitespaceHandling.None)
                {
                    return baseReader;        // XmlTextReaderImpl that strips all WS
                }
            }
            return new WhitespaceRuleReader(baseReader, wsRules);
        }

        private WhitespaceRuleReader(XmlReader baseReader, WhitespaceRuleLookup wsRules) : base(baseReader)
        {
            Debug.Assert(wsRules != null);

            _val = null;
            _stkStrip = new BitStack();
            _shouldStrip = false;
            _preserveAdjacent = false;

            _wsRules = wsRules;
            _wsRules.Atomize(baseReader.NameTable);
        }

        /// <summary>
        /// Override Value in order to possibly prepend extra whitespace.
        /// </summary>
        public override string Value
        {
            get { return (_val == null) ? base.Value : _val; }
        }

        /// <summary>
        /// Override Read in order to search for strippable whitespace, to concatenate adjacent text nodes, and to
        /// resolve entities.
        /// </summary>
        public override bool Read()
        {
            XmlCharType xmlCharType = XmlCharType.Instance;
            string ws = null;

            // Clear text value
            _val = null;

            while (base.Read())
            {
                switch (base.NodeType)
                {
                    case XmlNodeType.Element:
                        // Push boolean indicating whether whitespace children of this element should be stripped
                        if (!base.IsEmptyElement)
                        {
                            _stkStrip.PushBit(_shouldStrip);

                            // Strip if rules say we should and we're not within the scope of xml:space="preserve"
                            _shouldStrip = _wsRules.ShouldStripSpace(base.LocalName, base.NamespaceURI) && (base.XmlSpace != XmlSpace.Preserve);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        // Restore parent shouldStrip setting
                        _shouldStrip = _stkStrip.PopBit();
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        // If preserving adjacent text, don't perform any further checks
                        if (_preserveAdjacent)
                            return true;

                        if (_shouldStrip)
                        {
                            // Reader may report whitespace as Text or CDATA
                            if (xmlCharType.IsOnlyWhitespace(base.Value))
                                goto case XmlNodeType.Whitespace;

                            // If whitespace was cached, then prepend it to text or CDATA value
                            if (ws != null)
                                _val = string.Concat(ws, base.Value);

                            // Preserve adjacent whitespace
                            _preserveAdjacent = true;
                            return true;
                        }
                        break;

                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        // If preserving adjacent text, don't perform any further checks
                        if (_preserveAdjacent)
                            return true;

                        if (_shouldStrip)
                        {
                            // Save whitespace until it can be determined whether it will be stripped
                            if (ws == null)
                                ws = base.Value;
                            else
                                ws = string.Concat(ws, base.Value);

                            // Read next event
                            continue;
                        }
                        break;
                    case XmlNodeType.EndEntity:
                        // Read next event
                        continue;
                }

                // No longer preserve adjacent space
                _preserveAdjacent = false;
                return true;
            }

            return false;
        }
    }
}
