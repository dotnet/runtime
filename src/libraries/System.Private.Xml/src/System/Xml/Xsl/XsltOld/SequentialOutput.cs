// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Text;
    using System.Collections;
    using System.Globalization;

    internal abstract class SequentialOutput : RecordOutput
    {
        private const char s_Colon = ':';
        private const char s_GreaterThan = '>';
        private const char s_LessThan = '<';
        private const char s_Space = ' ';
        private const char s_Quote = '\"';
        private const char s_Semicolon = ';';
        private const char s_NewLine = '\n';
        private const char s_Return = '\r';
        private const char s_Ampersand = '&';
        private const string s_LessThanQuestion = "<?";
        private const string s_QuestionGreaterThan = "?>";
        private const string s_LessThanSlash = "</";
        private const string s_SlashGreaterThan = " />";
        private const string s_EqualQuote = "=\"";
        private const string s_DocType = "<!DOCTYPE ";
        private const string s_CommentBegin = "<!--";
        private const string s_CommentEnd = "-->";
        private const string s_CDataBegin = "<![CDATA[";
        private const string s_CDataEnd = "]]>";
        private const string s_VersionAll = " version=\"1.0\"";
        private const string s_Standalone = " standalone=\"";
        private const string s_EncodingStart = " encoding=\"";
        private const string s_Public = "PUBLIC ";
        private const string s_System = "SYSTEM ";
        private const string s_Html = "html";
        private const string s_QuoteSpace = "\" ";
        private const string s_CDataSplit = "]]]]><![CDATA[>";

        private const string s_EnLessThan = "&lt;";
        private const string s_EnGreaterThan = "&gt;";
        private const string s_EnAmpersand = "&amp;";
        private const string s_EnQuote = "&quot;";
        private const string s_EnNewLine = "&#xA;";
        private const string s_EnReturn = "&#xD;";

        private const string s_EndOfLine = "\r\n";

        private static char[] s_TextValueFind = new char[] { s_Ampersand, s_GreaterThan, s_LessThan };
        private static string[] s_TextValueReplace = new string[] { s_EnAmpersand, s_EnGreaterThan, s_EnLessThan };

        private static char[] s_XmlAttributeValueFind = new char[] { s_Ampersand, s_GreaterThan, s_LessThan, s_Quote, s_NewLine, s_Return };
        private static string[] s_XmlAttributeValueReplace = new string[] { s_EnAmpersand, s_EnGreaterThan, s_EnLessThan, s_EnQuote, s_EnNewLine, s_EnReturn };

        // Instance members
        private Processor _processor;
        protected Encoding encoding;
        private ArrayList _outputCache;
        private bool _firstLine = true;
        private bool _secondRoot;

        // Cached Output propertes:
        private XsltOutput _output;
        private bool _isHtmlOutput;
        private bool _isXmlOutput;
        private Hashtable _cdataElements;
        private bool _indentOutput;
        private bool _outputDoctype;
        private bool _outputXmlDecl;
        private bool _omitXmlDeclCalled;

        // Uri Escaping:
        private byte[] _byteBuffer;
        private Encoding _utf8Encoding;

        private XmlCharType _xmlCharType = XmlCharType.Instance;

        private void CacheOuptutProps(XsltOutput output)
        {
            _output = output;
            _isXmlOutput = _output.Method == XsltOutput.OutputMethod.Xml;
            _isHtmlOutput = _output.Method == XsltOutput.OutputMethod.Html;
            _cdataElements = _output.CDataElements;
            _indentOutput = _output.Indent;
            _outputDoctype = _output.DoctypeSystem != null || (_isHtmlOutput && _output.DoctypePublic != null);
            _outputXmlDecl = _isXmlOutput && !_output.OmitXmlDeclaration && !_omitXmlDeclCalled;
        }

        //
        // Constructor
        //
        internal SequentialOutput(Processor processor)
        {
            _processor = processor;
            CacheOuptutProps(processor.Output);
        }

        public void OmitXmlDecl()
        {
            _omitXmlDeclCalled = true;
            _outputXmlDecl = false;
        }

        //
        // Particular outputs
        //
        private void WriteStartElement(RecordBuilder record)
        {
            Debug.Assert(record.MainNode.NodeType == XmlNodeType.Element);
            BuilderInfo mainNode = record.MainNode;
            HtmlElementProps htmlProps = null;
            if (_isHtmlOutput)
            {
                if (mainNode.Prefix.Length == 0)
                {
                    htmlProps = mainNode.htmlProps;
                    if (htmlProps == null && mainNode.search)
                    {
                        htmlProps = HtmlElementProps.GetProps(mainNode.LocalName);
                    }
                    record.Manager.CurrentElementScope.HtmlElementProps = htmlProps;
                    mainNode.IsEmptyTag = false;
                }
            }
            else if (_isXmlOutput)
            {
                if (mainNode.Depth == 0)
                {
                    if (
                        _secondRoot && (
                            _output.DoctypeSystem != null ||
                            _output.Standalone
                        )
                    )
                    {
                        throw XsltException.Create(SR.Xslt_MultipleRoots);
                    }
                    _secondRoot = true;
                }
            }

            if (_outputDoctype)
            {
                WriteDoctype(mainNode);
                _outputDoctype = false;
            }

            if (_cdataElements != null && _cdataElements.Contains(new XmlQualifiedName(mainNode.LocalName, mainNode.NamespaceURI)) && _isXmlOutput)
            {
                record.Manager.CurrentElementScope.ToCData = true;
            }

            Indent(record);
            Write(s_LessThan);
            WriteName(mainNode.Prefix, mainNode.LocalName);

            WriteAttributes(record.AttributeList, record.AttributeCount, htmlProps);


            if (mainNode.IsEmptyTag)
            {
                Debug.Assert(!_isHtmlOutput || mainNode.Prefix != null, "Html can't have abbreviated elements");
                Write(s_SlashGreaterThan);
            }
            else
            {
                Write(s_GreaterThan);
            }

            if (htmlProps != null && htmlProps.Head)
            {
                mainNode.Depth++;
                Indent(record);
                mainNode.Depth--;
                Write("<META http-equiv=\"Content-Type\" content=\"");
                Write(_output.MediaType);
                Write("; charset=");
                Write(this.encoding.WebName);
                Write("\">");
            }
        }

        private void WriteTextNode(RecordBuilder record)
        {
            BuilderInfo mainNode = record.MainNode;
            OutputScope scope = record.Manager.CurrentElementScope;

            scope.Mixed = true;

            if (scope.HtmlElementProps != null && scope.HtmlElementProps.NoEntities)
            {
                // script or stile
                Write(mainNode.Value);
            }
            else if (scope.ToCData)
            {
                WriteCDataSection(mainNode.Value);
            }
            else
            {
                WriteTextNode(mainNode);
            }
        }

        private void WriteTextNode(BuilderInfo node)
        {
            for (int i = 0; i < node.TextInfoCount; i++)
            {
                string text = node.TextInfo[i];
                if (text == null)
                { // disableEscaping marker
                    i++;
                    Debug.Assert(i < node.TextInfoCount, "disableEscaping marker can't be last TextInfo record");
                    Write(node.TextInfo[i]);
                }
                else
                {
                    WriteWithReplace(text, s_TextValueFind, s_TextValueReplace);
                }
            }
        }

        private void WriteCDataSection(string value)
        {
            Write(s_CDataBegin);
            WriteCData(value);
            Write(s_CDataEnd);
        }

        private void WriteDoctype(BuilderInfo mainNode)
        {
            Debug.Assert(_outputDoctype == true, "It supposed to check this condition before actual call");
            Debug.Assert(_output.DoctypeSystem != null || (_isHtmlOutput && _output.DoctypePublic != null), "We set outputDoctype == true only if");
            Indent(0);
            Write(s_DocType);
            if (_isXmlOutput)
            {
                WriteName(mainNode.Prefix, mainNode.LocalName);
            }
            else
            {
                WriteName(string.Empty, "html");
            }
            Write(s_Space);
            if (_output.DoctypePublic != null)
            {
                Write(s_Public);
                Write(s_Quote);
                Write(_output.DoctypePublic);
                Write(s_QuoteSpace);
            }
            else
            {
                Write(s_System);
            }
            if (_output.DoctypeSystem != null)
            {
                Write(s_Quote);
                Write(_output.DoctypeSystem);
                Write(s_Quote);
            }
            Write(s_GreaterThan);
        }

        private void WriteXmlDeclaration()
        {
            Debug.Assert(_outputXmlDecl == true, "It supposed to check this condition before actual call");
            Debug.Assert(_isXmlOutput && !_output.OmitXmlDeclaration, "We set outputXmlDecl == true only if");
            _outputXmlDecl = false;

            Indent(0);
            Write(s_LessThanQuestion);
            WriteName(string.Empty, "xml");
            Write(s_VersionAll);
            if (this.encoding != null)
            {
                Write(s_EncodingStart);
                Write(this.encoding.WebName);
                Write(s_Quote);
            }
            if (_output.HasStandalone)
            {
                Write(s_Standalone);
                Write(_output.Standalone ? "yes" : "no");
                Write(s_Quote);
            }
            Write(s_QuestionGreaterThan);
        }

        private void WriteProcessingInstruction(RecordBuilder record)
        {
            Indent(record);
            WriteProcessingInstruction(record.MainNode);
        }

        private void WriteProcessingInstruction(BuilderInfo node)
        {
            Write(s_LessThanQuestion);
            WriteName(node.Prefix, node.LocalName);
            Write(s_Space);
            Write(node.Value);

            if (_isHtmlOutput)
            {
                Write(s_GreaterThan);
            }
            else
            {
                Write(s_QuestionGreaterThan);
            }
        }

        private void WriteEndElement(RecordBuilder record)
        {
            BuilderInfo node = record.MainNode;
            HtmlElementProps htmlProps = record.Manager.CurrentElementScope.HtmlElementProps;

            if (htmlProps != null && htmlProps.Empty)
            {
                return;
            }

            Indent(record);
            Write(s_LessThanSlash);
            WriteName(record.MainNode.Prefix, record.MainNode.LocalName);
            Write(s_GreaterThan);
        }

        //
        // RecordOutput interface method implementation
        //

        public Processor.OutputResult RecordDone(RecordBuilder record)
        {
            if (_output.Method == XsltOutput.OutputMethod.Unknown)
            {
                if (!DecideDefaultOutput(record.MainNode))
                {
                    CacheRecord(record);
                }
                else
                {
                    OutputCachedRecords();
                    OutputRecord(record);
                }
            }
            else
            {
                OutputRecord(record);
            }

            record.Reset();
            return Processor.OutputResult.Continue;
        }

        public void TheEnd()
        {
            OutputCachedRecords();
            Close();
        }

        private bool DecideDefaultOutput(BuilderInfo node)
        {
            XsltOutput.OutputMethod method = XsltOutput.OutputMethod.Xml;
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    if (node.NamespaceURI.Length == 0 && String.Compare("html", node.LocalName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        method = XsltOutput.OutputMethod.Html;
                    }
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    if (_xmlCharType.IsOnlyWhitespace(node.Value))
                    {
                        return false;
                    }
                    method = XsltOutput.OutputMethod.Xml;
                    break;
                default:
                    return false;
            }
            if (_processor.SetDefaultOutput(method))
            {
                CacheOuptutProps(_processor.Output);
            }
            return true;
        }

        private void CacheRecord(RecordBuilder record)
        {
            if (_outputCache == null)
            {
                _outputCache = new ArrayList();
            }

            _outputCache.Add(record.MainNode.Clone());
        }

        private void OutputCachedRecords()
        {
            if (_outputCache == null)
            {
                return;
            }

            for (int record = 0; record < _outputCache.Count; record++)
            {
                Debug.Assert(_outputCache[record] is BuilderInfo);
                BuilderInfo info = (BuilderInfo)_outputCache[record];

                OutputRecord(info);
            }

            _outputCache = null;
        }

        private void OutputRecord(RecordBuilder record)
        {
            BuilderInfo mainNode = record.MainNode;

            if (_outputXmlDecl)
            {
                WriteXmlDeclaration();
            }

            switch (mainNode.NodeType)
            {
                case XmlNodeType.Element:
                    WriteStartElement(record);
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    WriteTextNode(record);
                    break;
                case XmlNodeType.CDATA:
                    Debug.Fail("Should never get here");
                    break;
                case XmlNodeType.EntityReference:
                    Write(s_Ampersand);
                    WriteName(mainNode.Prefix, mainNode.LocalName);
                    Write(s_Semicolon);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    WriteProcessingInstruction(record);
                    break;
                case XmlNodeType.Comment:
                    Indent(record);
                    Write(s_CommentBegin);
                    Write(mainNode.Value);
                    Write(s_CommentEnd);
                    break;
                case XmlNodeType.Document:
                    break;
                case XmlNodeType.DocumentType:
                    Write(mainNode.Value);
                    break;
                case XmlNodeType.EndElement:
                    WriteEndElement(record);
                    break;
                default:
                    break;
            }
        }

        private void OutputRecord(BuilderInfo node)
        {
            if (_outputXmlDecl)
            {
                WriteXmlDeclaration();
            }

            Indent(0); // we can have only top level stuff here

            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    Debug.Fail("Should never get here");
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    WriteTextNode(node);
                    break;
                case XmlNodeType.CDATA:
                    Debug.Fail("Should never get here");
                    break;
                case XmlNodeType.EntityReference:
                    Write(s_Ampersand);
                    WriteName(node.Prefix, node.LocalName);
                    Write(s_Semicolon);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    WriteProcessingInstruction(node);
                    break;
                case XmlNodeType.Comment:
                    Write(s_CommentBegin);
                    Write(node.Value);
                    Write(s_CommentEnd);
                    break;
                case XmlNodeType.Document:
                    break;
                case XmlNodeType.DocumentType:
                    Write(node.Value);
                    break;
                case XmlNodeType.EndElement:
                    Debug.Fail("Should never get here");
                    break;
                default:
                    break;
            }
        }

        //
        // Internal helpers
        //

        private void WriteName(string prefix, string name)
        {
            if (prefix != null && prefix.Length > 0)
            {
                Write(prefix);
                if (name != null && name.Length > 0)
                {
                    Write(s_Colon);
                }
                else
                {
                    return;
                }
            }
            Write(name);
        }

        private void WriteXmlAttributeValue(string value)
        {
            Debug.Assert(value != null);
            WriteWithReplace(value, s_XmlAttributeValueFind, s_XmlAttributeValueReplace);
        }

        private void WriteHtmlAttributeValue(string value)
        {
            Debug.Assert(value != null);

            int length = value.Length;
            int i = 0;
            while (i < length)
            {
                char ch = value[i];
                i++;
                switch (ch)
                {
                    case '&':
                        if (i != length && value[i] == '{')
                        { // &{ hasn't to be encoded in HTML output.
                            Write(ch);
                        }
                        else
                        {
                            Write(s_EnAmpersand);
                        }
                        break;
                    case '"':
                        Write(s_EnQuote);
                        break;
                    default:
                        Write(ch);
                        break;
                }
            }
        }

        private void WriteHtmlUri(string value)
        {
            Debug.Assert(value != null);
            Debug.Assert(_isHtmlOutput);

            int length = value.Length;
            int i = 0;
            while (i < length)
            {
                char ch = value[i];
                i++;
                switch (ch)
                {
                    case '&':
                        if (i != length && value[i] == '{')
                        { // &{ hasn't to be encoded in HTML output.
                            Write(ch);
                        }
                        else
                        {
                            Write(s_EnAmpersand);
                        }
                        break;
                    case '"':
                        Write(s_EnQuote);
                        break;
                    case '\n':
                        Write(s_EnNewLine);
                        break;
                    case '\r':
                        Write(s_EnReturn);
                        break;
                    default:
                        if (127 < ch)
                        {
                            if (_utf8Encoding == null)
                            {
                                _utf8Encoding = Encoding.UTF8;
                                _byteBuffer = new byte[_utf8Encoding.GetMaxByteCount(1)];
                            }
                            int bytes = _utf8Encoding.GetBytes(value, i - 1, 1, _byteBuffer, 0);
                            for (int j = 0; j < bytes; j++)
                            {
                                Write("%");
                                Write(((uint)_byteBuffer[j]).ToString("X2", CultureInfo.InvariantCulture));
                            }
                        }
                        else
                        {
                            Write(ch);
                        }
                        break;
                }
            }
        }

        private void WriteWithReplace(string value, char[] find, string[] replace)
        {
            Debug.Assert(value != null);
            Debug.Assert(find.Length == replace.Length);

            int length = value.Length;
            int pos = 0;

            while (pos < length)
            {
                int newPos = value.IndexOfAny(find, pos);
                if (newPos == -1)
                {
                    break; // not found;
                }
                // output clean leading part of the string
                while (pos < newPos)
                {
                    Write(value[pos]);
                    pos++;
                }
                // output replacement
                char badChar = value[pos];
                int i;
                for (i = find.Length - 1; 0 <= i; i--)
                {
                    if (find[i] == badChar)
                    {
                        Write(replace[i]);
                        break;
                    }
                }
                Debug.Assert(0 <= i, "find char wasn't realy find");
                pos++;
            }

            // output rest of the string
            if (pos == 0)
            {
                Write(value);
            }
            else
            {
                while (pos < length)
                {
                    Write(value[pos]);
                    pos++;
                }
            }
        }

        private void WriteCData(string value)
        {
            Debug.Assert(value != null);
            Write(value.Replace(s_CDataEnd, s_CDataSplit));
        }

        private void WriteAttributes(ArrayList list, int count, HtmlElementProps htmlElementsProps)
        {
            Debug.Assert(count <= list.Count);
            for (int attrib = 0; attrib < count; attrib++)
            {
                Debug.Assert(list[attrib] is BuilderInfo);
                BuilderInfo attribute = (BuilderInfo)list[attrib];
                string attrValue = attribute.Value;
                bool abr = false, uri = false;
                {
                    if (htmlElementsProps != null && attribute.Prefix.Length == 0)
                    {
                        HtmlAttributeProps htmlAttrProps = attribute.htmlAttrProps;
                        if (htmlAttrProps == null && attribute.search)
                        {
                            htmlAttrProps = HtmlAttributeProps.GetProps(attribute.LocalName);
                        }
                        if (htmlAttrProps != null)
                        {
                            abr = htmlElementsProps.AbrParent && htmlAttrProps.Abr;
                            uri = htmlElementsProps.UriParent && (htmlAttrProps.Uri ||
                                  htmlElementsProps.NameParent && htmlAttrProps.Name
                            );
                        }
                    }
                }
                Write(s_Space);
                WriteName(attribute.Prefix, attribute.LocalName);
                if (abr && 0 == string.Compare(attribute.LocalName, attrValue, StringComparison.OrdinalIgnoreCase))
                {
                    // Since the name of the attribute = the value of the attribute, 
                    // this is a boolean attribute whose value should be suppressed
                    continue;
                }
                Write(s_EqualQuote);
                if (uri)
                {
                    WriteHtmlUri(attrValue);
                }
                else if (_isHtmlOutput)
                {
                    WriteHtmlAttributeValue(attrValue);
                }
                else
                {
                    WriteXmlAttributeValue(attrValue);
                }
                Write(s_Quote);
            }
        }

        private void Indent(RecordBuilder record)
        {
            if (!record.Manager.CurrentElementScope.Mixed)
            {
                Indent(record.MainNode.Depth);
            }
        }

        private void Indent(int depth)
        {
            if (_firstLine)
            {
                if (_indentOutput)
                {
                    _firstLine = false;
                }
                return;    // preven leading CRLF
            }
            Write(s_EndOfLine);
            for (int i = 2 * depth; 0 < i; i--)
            {
                Write(" ");
            }
        }

        //
        // Abstract methods
        internal abstract void Write(char outputChar);
        internal abstract void Write(string outputText);
        internal abstract void Close();
    }
}
