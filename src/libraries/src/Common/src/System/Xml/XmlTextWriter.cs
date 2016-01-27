// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml
{
    // Specifies formatting options for XmlTextWriter.
    internal enum Formatting
    {
        // No special formatting is done (this is the default).
        None,

        //This option causes child elements to be indented using the Indentation and IndentChar properties.  
        // It only indents Element Content (http://www.w3.org/TR/1998/REC-xml-19980210#sec-element-content)
        // and not Mixed Content (http://www.w3.org/TR/1998/REC-xml-19980210#sec-mixed-content)
        // according to the XML 1.0 definitions of these terms.
        Indented,
    };

    // Represents a writer that provides fast non-cached forward-only way of generating XML streams 
    // containing XML documents that conform to the W3CExtensible Markup Language (XML) 1.0 specification 
    // and the Namespaces in XML specification.

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal class XmlTextWriter : XmlWriter
    {
        //
        // Private types
        //
        enum NamespaceState
        {
            Uninitialized,
            NotDeclaredButInScope,
            DeclaredButNotWrittenOut,
            DeclaredAndWrittenOut
        }

        struct TagInfo
        {
            internal string name;
            internal string prefix;
            internal string defaultNs;
            internal NamespaceState defaultNsState;
            internal XmlSpace xmlSpace;
            internal string xmlLang;
            internal int prevNsTop;
            internal int prefixCount;
            internal bool mixed; // whether to pretty print the contents of this element.

            internal void Init(int nsTop)
            {
                name = null;
                defaultNs = String.Empty;
                defaultNsState = NamespaceState.Uninitialized;
                xmlSpace = XmlSpace.None;
                xmlLang = null;
                prevNsTop = nsTop;
                prefixCount = 0;
                mixed = false;
            }
        }

        struct Namespace
        {
            internal string prefix;
            internal string ns;
            internal bool declared;
            internal int prevNsIndex;

            internal void Set(string prefix, string ns, bool declared)
            {
                this.prefix = prefix;
                this.ns = ns;
                this.declared = declared;
                this.prevNsIndex = -1;
            }
        }

        enum SpecialAttr
        {
            None,
            XmlSpace,
            XmlLang,
            XmlNs
        };

        // State machine is working through autocomplete
        private enum State
        {
            Start,
            Prolog,
            PostDTD,
            Element,
            Attribute,
            Content,
            AttrOnly,
            Epilog,
            Error,
            Closed,
        }

        private enum Token
        {
            PI,
            Doctype,
            Comment,
            CData,
            StartElement,
            EndElement,
            LongEndElement,
            StartAttribute,
            EndAttribute,
            Content,
            Base64,
            RawData,
            Whitespace,
            Empty
        }

        //
        // Fields
        //
        // output
        TextWriter textWriter;
        XmlTextEncoder xmlEncoder;
        Encoding encoding;

        // formatting
        Formatting formatting;
        bool indented; // perf - faster to check a boolean.
        int indentation;
        char indentChar;

        // element stack
        TagInfo[] stack;
        int top;

        // state machine for AutoComplete
        State[] stateTable;
        State currentState;
        Token lastToken;

        // Base64 content
        XmlTextWriterBase64Encoder base64Encoder;

        // misc
        char quoteChar;
        char curQuoteChar;
        bool namespaces;
        SpecialAttr specialAttr;
        string prefixForXmlNs;
        bool flush;

        // namespaces
        Namespace[] nsStack;
        int nsTop;
        Dictionary<string, int> nsHashtable;
        bool useNsHashtable;

        // char types
        XmlCharType xmlCharType = XmlCharType.Instance;

        //
        // Constants and constant tables
        //
        const int NamespaceStackInitialSize = 8;
#if DEBUG
        const int MaxNamespacesWalkCount = 3;
#else
        const int MaxNamespacesWalkCount = 16;
#endif

        static string[] stateName = {
            "Start",
            "Prolog",
            "PostDTD",
            "Element",
            "Attribute",
            "Content",
            "AttrOnly",
            "Epilog",
            "Error",
            "Closed",
        };

        static string[] tokenName = {
            "PI",
            "Doctype",
            "Comment",
            "CData",
            "StartElement",
            "EndElement",
            "LongEndElement",
            "StartAttribute",
            "EndAttribute",
            "Content",
            "Base64",
            "RawData",
            "Whitespace",
            "Empty"
        };

        static readonly State[] stateTableDefault = {
            //                          State.Start      State.Prolog     State.PostDTD    State.Element    State.Attribute  State.Content   State.AttrOnly   State.Epilog
            //
            /* Token.PI             */ State.Prolog,    State.Prolog,    State.PostDTD,   State.Content,   State.Content,   State.Content,  State.Error,     State.Epilog,
            /* Token.Doctype        */ State.PostDTD,   State.PostDTD,   State.Error,     State.Error,     State.Error,     State.Error,    State.Error,     State.Error,
            /* Token.Comment        */ State.Prolog,    State.Prolog,    State.PostDTD,   State.Content,   State.Content,   State.Content,  State.Error,     State.Epilog,
            /* Token.CData          */ State.Content,   State.Content,   State.Error,     State.Content,   State.Content,   State.Content,  State.Error,     State.Epilog,
            /* Token.StartElement   */ State.Element,   State.Element,   State.Element,   State.Element,   State.Element,   State.Element,  State.Error,     State.Element,
            /* Token.EndElement     */ State.Error,     State.Error,     State.Error,     State.Content,   State.Content,   State.Content,  State.Error,     State.Error,
            /* Token.LongEndElement */ State.Error,     State.Error,     State.Error,     State.Content,   State.Content,   State.Content,  State.Error,     State.Error,
            /* Token.StartAttribute */ State.AttrOnly,  State.Error,     State.Error,     State.Attribute, State.Attribute, State.Error,    State.Error,     State.Error,
            /* Token.EndAttribute   */ State.Error,     State.Error,     State.Error,     State.Error,     State.Element,   State.Error,    State.Epilog,     State.Error,
            /* Token.Content        */ State.Content,   State.Content,   State.Error,     State.Content,   State.Attribute, State.Content,  State.Attribute, State.Epilog,
            /* Token.Base64         */ State.Content,   State.Content,   State.Error,     State.Content,   State.Attribute, State.Content,  State.Attribute, State.Epilog,
            /* Token.RawData        */ State.Prolog,    State.Prolog,    State.PostDTD,   State.Content,   State.Attribute, State.Content,  State.Attribute, State.Epilog,
            /* Token.Whitespace     */ State.Prolog,    State.Prolog,    State.PostDTD,   State.Content,   State.Attribute, State.Content,  State.Attribute, State.Epilog,
        };

        static readonly State[] stateTableDocument = {
            //                          State.Start      State.Prolog     State.PostDTD    State.Element    State.Attribute  State.Content   State.AttrOnly   State.Epilog
            //
            /* Token.PI             */ State.Error,     State.Prolog,    State.PostDTD,   State.Content,   State.Content,   State.Content,  State.Error,     State.Epilog,
            /* Token.Doctype        */ State.Error,     State.PostDTD,   State.Error,     State.Error,     State.Error,     State.Error,    State.Error,     State.Error,
            /* Token.Comment        */ State.Error,     State.Prolog,    State.PostDTD,   State.Content,   State.Content,   State.Content,  State.Error,     State.Epilog,
            /* Token.CData          */ State.Error,     State.Error,     State.Error,     State.Content,   State.Content,   State.Content,  State.Error,     State.Error,
            /* Token.StartElement   */ State.Error,     State.Element,   State.Element,   State.Element,   State.Element,   State.Element,  State.Error,     State.Error,
            /* Token.EndElement     */ State.Error,     State.Error,     State.Error,     State.Content,   State.Content,   State.Content,  State.Error,     State.Error,
            /* Token.LongEndElement */ State.Error,     State.Error,     State.Error,     State.Content,   State.Content,   State.Content,  State.Error,     State.Error,
            /* Token.StartAttribute */ State.Error,     State.Error,     State.Error,     State.Attribute, State.Attribute, State.Error,    State.Error,     State.Error,
            /* Token.EndAttribute   */ State.Error,     State.Error,     State.Error,     State.Error,     State.Element,   State.Error,    State.Error,     State.Error,
            /* Token.Content        */ State.Error,     State.Error,     State.Error,     State.Content,   State.Attribute, State.Content,  State.Error,     State.Error,
            /* Token.Base64         */ State.Error,     State.Error,     State.Error,     State.Content,   State.Attribute, State.Content,  State.Error,     State.Error,
            /* Token.RawData        */ State.Error,     State.Prolog,    State.PostDTD,   State.Content,   State.Attribute, State.Content,  State.Error,     State.Epilog,
            /* Token.Whitespace     */ State.Error,     State.Prolog,    State.PostDTD,   State.Content,   State.Attribute, State.Content,  State.Error,     State.Epilog,
        };

        //
        // Constructors
        //
        internal XmlTextWriter()
        {
            namespaces = true;
            formatting = Formatting.None;
            indentation = 2;
            indentChar = ' ';
            // namespaces
            nsStack = new Namespace[NamespaceStackInitialSize];
            nsTop = -1;
            // element stack
            stack = new TagInfo[10];
            top = 0;// 0 is an empty sentential element
            stack[top].Init(-1);
            quoteChar = '"';

            stateTable = stateTableDefault;
            currentState = State.Start;
            lastToken = Token.Empty;
        }

        // Creates an instance of the XmlTextWriter class using the specified stream.
        public XmlTextWriter(Stream w, Encoding encoding) : this()
        {
            this.encoding = encoding;
            if (encoding != null)
                textWriter = new StreamWriter(w, encoding);
            else
                textWriter = new StreamWriter(w);
            xmlEncoder = new XmlTextEncoder(textWriter);
            xmlEncoder.QuoteChar = this.quoteChar;
        }

        // Creates an instance of the XmlTextWriter class using the specified TextWriter.
        public XmlTextWriter(TextWriter w) : this()
        {
            textWriter = w;

            encoding = w.Encoding;
            xmlEncoder = new XmlTextEncoder(w);
            xmlEncoder.QuoteChar = this.quoteChar;
        }

        //
        // XmlTextWriter properties
        //
        // Gets the XmlTextWriter base stream.
        public Stream BaseStream
        {
            get
            {
                StreamWriter streamWriter = textWriter as StreamWriter;
                return (streamWriter == null ? null : streamWriter.BaseStream);
            }
        }

        // Gets or sets a value indicating whether to do namespace support.
        public bool Namespaces
        {
            get { return this.namespaces; }
            set
            {
                if (this.currentState != State.Start)
                    throw new InvalidOperationException(SR.Xml_NotInWriteState);

                this.namespaces = value;
            }
        }

        // Indicates how the output is formatted.
        public Formatting Formatting
        {
            get { return this.formatting; }
            set { this.formatting = value; this.indented = value == Formatting.Indented; }
        }

        // Gets or sets how many IndentChars to write for each level in the hierarchy when Formatting is set to "Indented".
        public int Indentation
        {
            get { return this.indentation; }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.Xml_InvalidIndentation);
                this.indentation = value;
            }
        }

        // Gets or sets which character to use for indenting when Formatting is set to "Indented".
        public char IndentChar
        {
            get { return this.indentChar; }
            set { this.indentChar = value; }
        }

        // Gets or sets which character to use to quote attribute values.
        public char QuoteChar
        {
            get { return this.quoteChar; }
            set
            {
                if (value != '"' && value != '\'')
                {
                    throw new ArgumentException(SR.Xml_InvalidQuote);
                }
                this.quoteChar = value;
                this.xmlEncoder.QuoteChar = value;
            }
        }

        //
        // XmlWriter implementation
        //
        // Writes out the XML declaration with the version "1.0".
        public override void WriteStartDocument()
        {
            StartDocument(-1);
        }

        // Writes out the XML declaration with the version "1.0" and the standalone attribute.
        public override void WriteStartDocument(bool standalone)
        {
            StartDocument(standalone ? 1 : 0);
        }

        // Closes any open elements or attributes and puts the writer back in the Start state.
        public override void WriteEndDocument()
        {
            try
            {
                AutoCompleteAll();
                if (this.currentState != State.Epilog)
                {
                    if (this.currentState == State.Closed)
                    {
                        throw new ArgumentException(SR.Xml_ClosedOrError);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Xml_NoRoot);
                    }
                }
                this.stateTable = stateTableDefault;
                this.currentState = State.Start;
                this.lastToken = Token.Empty;
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out the DOCTYPE declaration with the specified name and optional attributes.
        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            try
            {
                ValidateName(name, false);

                AutoComplete(Token.Doctype);
                textWriter.Write("<!DOCTYPE ");
                textWriter.Write(name);
                if (pubid != null)
                {
                    textWriter.Write(" PUBLIC ");
                    textWriter.Write(quoteChar);
                    textWriter.Write(pubid);
                    textWriter.Write(quoteChar);
                    textWriter.Write(' ');
                    textWriter.Write(quoteChar);
                    textWriter.Write(sysid);
                    textWriter.Write(quoteChar);
                }
                else if (sysid != null)
                {
                    textWriter.Write(" SYSTEM ");
                    textWriter.Write(quoteChar);
                    textWriter.Write(sysid);
                    textWriter.Write(quoteChar);
                }
                if (subset != null)
                {
                    textWriter.Write('[');
                    textWriter.Write(subset);
                    textWriter.Write(']');
                }
                textWriter.Write('>');
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out the specified start tag and associates it with the given namespace and prefix.
        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            try
            {
                AutoComplete(Token.StartElement);
                PushStack();
                textWriter.Write('<');

                if (this.namespaces)
                {
                    // Propagate default namespace and mix model down the stack.
                    stack[top].defaultNs = stack[top - 1].defaultNs;
                    if (stack[top - 1].defaultNsState != NamespaceState.Uninitialized)
                        stack[top].defaultNsState = NamespaceState.NotDeclaredButInScope;
                    stack[top].mixed = stack[top - 1].mixed;
                    if (ns == null)
                    {
                        // use defined prefix
                        if (!string.IsNullOrEmpty(prefix) && (LookupNamespace(prefix) == -1))
                        {
                            throw new ArgumentException(SR.Xml_UndefPrefix);
                        }
                    }
                    else
                    {
                        if (prefix == null)
                        {
                            string definedPrefix = FindPrefix(ns);
                            if (definedPrefix != null)
                            {
                                prefix = definedPrefix;
                            }
                            else
                            {
                                PushNamespace(null, ns, false); // new default
                            }
                        }
                        else if (prefix.Length == 0)
                        {
                            PushNamespace(null, ns, false); // new default
                        }
                        else
                        {
                            if (ns.Length == 0)
                            {
                                prefix = null;
                            }
                            VerifyPrefixXml(prefix, ns);
                            PushNamespace(prefix, ns, false); // define
                        }
                    }
                    stack[top].prefix = null;
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        stack[top].prefix = prefix;
                        textWriter.Write(prefix);
                        textWriter.Write(':');
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(ns) || !string.IsNullOrEmpty(prefix))
                    {
                        throw new ArgumentException(SR.Xml_NoNamespaces);
                    }
                }
                stack[top].name = localName;
                textWriter.Write(localName);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Closes one element and pops the corresponding namespace scope.
        public override void WriteEndElement()
        {
            InternalWriteEndElement(false);
        }

        // Closes one element and pops the corresponding namespace scope.
        public override void WriteFullEndElement()
        {
            InternalWriteEndElement(true);
        }

        // Writes the start of an attribute.
        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            try
            {
                AutoComplete(Token.StartAttribute);

                this.specialAttr = SpecialAttr.None;
                if (this.namespaces)
                {
                    if (prefix != null && prefix.Length == 0)
                    {
                        prefix = null;
                    }

                    if (ns == XmlConst.ReservedNsXmlNs && prefix == null && localName != "xmlns")
                    {
                        prefix = "xmlns";
                    }

                    if (prefix == "xml")
                    {
                        if (localName == "lang")
                        {
                            this.specialAttr = SpecialAttr.XmlLang;
                        }
                        else if (localName == "space")
                        {
                            this.specialAttr = SpecialAttr.XmlSpace;
                        }
                    }
                    else if (prefix == "xmlns")
                    {
                        if (XmlConst.ReservedNsXmlNs != ns && ns != null)
                        {
                            throw new ArgumentException(SR.Xml_XmlnsBelongsToReservedNs);
                        }
                        if (string.IsNullOrEmpty(localName))
                        {
                            localName = prefix;
                            prefix = null;
                            this.prefixForXmlNs = null;
                        }
                        else
                        {
                            this.prefixForXmlNs = localName;
                        }
                        this.specialAttr = SpecialAttr.XmlNs;
                    }
                    else if (prefix == null && localName == "xmlns")
                    {
                        if (XmlConst.ReservedNsXmlNs != ns && ns != null)
                        {
                            // add the below line back in when DOM is fixed
                            throw new ArgumentException(SR.Xml_XmlnsBelongsToReservedNs);
                        }
                        this.specialAttr = SpecialAttr.XmlNs;
                        this.prefixForXmlNs = null;
                    }
                    else
                    {
                        if (ns == null)
                        {
                            // use defined prefix
                            if (prefix != null && (LookupNamespace(prefix) == -1))
                            {
                                throw new ArgumentException(SR.Xml_UndefPrefix);
                            }
                        }
                        else if (ns.Length == 0)
                        {
                            // empty namespace require null prefix
                            prefix = string.Empty;
                        }
                        else
                        { // ns.Length != 0
                            VerifyPrefixXml(prefix, ns);
                            if (prefix != null && LookupNamespaceInCurrentScope(prefix) != -1)
                            {
                                prefix = null;
                            }
                            // Now verify prefix validity
                            string definedPrefix = FindPrefix(ns);
                            if (definedPrefix != null && (prefix == null || prefix == definedPrefix))
                            {
                                prefix = definedPrefix;
                            }
                            else
                            {
                                if (prefix == null)
                                {
                                    prefix = GeneratePrefix(); // need a prefix if
                                }
                                PushNamespace(prefix, ns, false);
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        textWriter.Write(prefix);
                        textWriter.Write(':');
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(ns) || !string.IsNullOrEmpty(prefix))
                    {
                        throw new ArgumentException(SR.Xml_NoNamespaces);
                    }
                    if (localName == "xml:lang")
                    {
                        this.specialAttr = SpecialAttr.XmlLang;
                    }
                    else if (localName == "xml:space")
                    {
                        this.specialAttr = SpecialAttr.XmlSpace;
                    }
                }
                xmlEncoder.StartAttribute(this.specialAttr != SpecialAttr.None);

                textWriter.Write(localName);
                textWriter.Write('=');
                if (this.curQuoteChar != this.quoteChar)
                {
                    this.curQuoteChar = this.quoteChar;
                    xmlEncoder.QuoteChar = this.quoteChar;
                }
                textWriter.Write(this.curQuoteChar);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Closes the attribute opened by WriteStartAttribute.
        public override void WriteEndAttribute()
        {
            try
            {
                AutoComplete(Token.EndAttribute);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out a &lt;![CDATA[...]]&gt; block containing the specified text.
        public override void WriteCData(string text)
        {
            try
            {
                AutoComplete(Token.CData);
                if (null != text && text.IndexOf("]]>", StringComparison.Ordinal) >= 0)
                {
                    throw new ArgumentException(SR.Xml_InvalidCDataChars);
                }
                textWriter.Write("<![CDATA[");

                if (null != text)
                {
                    xmlEncoder.WriteRawWithSurrogateChecking(text);
                }
                textWriter.Write("]]>");
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out a comment <!--...--> containing the specified text.
        public override void WriteComment(string text)
        {
            try
            {
                if (null != text && (text.IndexOf("--", StringComparison.Ordinal) >= 0 || (text.Length != 0 && text[text.Length - 1] == '-')))
                {
                    throw new ArgumentException(SR.Xml_InvalidCommentChars);
                }
                AutoComplete(Token.Comment);
                textWriter.Write("<!--");
                if (null != text)
                {
                    xmlEncoder.WriteRawWithSurrogateChecking(text);
                }
                textWriter.Write("-->");
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out a processing instruction with a space between the name and text as follows: <?name text?>
        public override void WriteProcessingInstruction(string name, string text)
        {
            try
            {
                if (null != text && text.IndexOf("?>", StringComparison.Ordinal) >= 0)
                {
                    throw new ArgumentException(SR.Xml_InvalidPiChars);
                }
                if (String.Equals(name, "xml", StringComparison.OrdinalIgnoreCase) && this.stateTable == stateTableDocument)
                {
                    throw new ArgumentException(SR.Xml_DupXmlDecl);
                }
                AutoComplete(Token.PI);
                InternalWriteProcessingInstruction(name, text);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out an entity reference as follows: "&"+name+";".
        public override void WriteEntityRef(string name)
        {
            try
            {
                ValidateName(name, false);
                AutoComplete(Token.Content);
                xmlEncoder.WriteEntityRef(name);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Forces the generation of a character entity for the specified Unicode character value.
        public override void WriteCharEntity(char ch)
        {
            try
            {
                AutoComplete(Token.Content);
                xmlEncoder.WriteCharEntity(ch);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out the given whitespace. 
        public override void WriteWhitespace(string ws)
        {
            try
            {
                if (null == ws)
                {
                    ws = String.Empty;
                }

                if (!xmlCharType.IsOnlyWhitespace(ws))
                {
                    throw new ArgumentException(SR.Xml_NonWhitespace);
                }
                AutoComplete(Token.Whitespace);
                xmlEncoder.Write(ws);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out the specified text content.
        public override void WriteString(string text)
        {
            try
            {
                if (null != text && text.Length != 0)
                {
                    AutoComplete(Token.Content);
                    xmlEncoder.Write(text);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out the specified surrogate pair as a character entity.
        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            try
            {
                AutoComplete(Token.Content);
                xmlEncoder.WriteSurrogateCharEntity(lowChar, highChar);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }


        // Writes out the specified text content.
        public override void WriteChars(Char[] buffer, int index, int count)
        {
            try
            {
                AutoComplete(Token.Content);
                xmlEncoder.Write(buffer, index, count);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes raw markup from the specified character buffer.
        public override void WriteRaw(Char[] buffer, int index, int count)
        {
            try
            {
                AutoComplete(Token.RawData);
                xmlEncoder.WriteRaw(buffer, index, count);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes raw markup from the specified character string.
        public override void WriteRaw(String data)
        {
            try
            {
                AutoComplete(Token.RawData);
                xmlEncoder.WriteRawWithSurrogateChecking(data);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Encodes the specified binary bytes as base64 and writes out the resulting text.
        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            try
            {
                if (!this.flush)
                {
                    AutoComplete(Token.Base64);
                }

                this.flush = true;
                // No need for us to explicitly validate the args. The StreamWriter will do
                // it for us.
                if (null == this.base64Encoder)
                {
                    this.base64Encoder = new XmlTextWriterBase64Encoder(xmlEncoder);
                }
                // Encode will call WriteRaw to write out the encoded characters
                this.base64Encoder.Encode(buffer, index, count);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }


        // Encodes the specified binary bytes as binhex and writes out the resulting text.
        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            try
            {
                AutoComplete(Token.Content);
                BinHexEncoder.Encode(buffer, index, count, this);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Returns the state of the XmlWriter.
        public override WriteState WriteState
        {
            get
            {
                switch (this.currentState)
                {
                    case State.Start:
                        return WriteState.Start;
                    case State.Prolog:
                    case State.PostDTD:
                        return WriteState.Prolog;
                    case State.Element:
                        return WriteState.Element;
                    case State.Attribute:
                    case State.AttrOnly:
                        return WriteState.Attribute;
                    case State.Content:
                    case State.Epilog:
                        return WriteState.Content;
                    case State.Error:
                        return WriteState.Error;
                    case State.Closed:
                        return WriteState.Closed;
                    default:
                        Debug.Fail("Unmatched state in switch");
                        return WriteState.Error;
                }
            }
        }

        // Disposes the XmlWriter and the underlying stream/TextWriter.
        protected override void Dispose(bool disposing)
        {
            if (disposing && this.currentState != State.Closed)
            {
                try
                {
                    AutoCompleteAll();
                }
                catch
                { // never fail
                }
                finally
                {
                    this.currentState = State.Closed;
                    textWriter.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        // Flushes whatever is in the buffer to the underlying stream/TextWriter and flushes the underlying stream/TextWriter.
        public override void Flush()
        {
            textWriter.Flush();
        }

        // Writes out the specified name, ensuring it is a valid Name according to the XML specification 
        // (http://www.w3.org/TR/1998/REC-xml-19980210#NT-Name
        public override void WriteName(string name)
        {
            try
            {
                AutoComplete(Token.Content);
                InternalWriteName(name, false);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Writes out the specified namespace-qualified name by looking up the prefix that is in scope for the given namespace.
        public override void WriteQualifiedName(string localName, string ns)
        {
            try
            {
                AutoComplete(Token.Content);
                if (this.namespaces)
                {
                    if (!string.IsNullOrEmpty(ns) && ns != stack[top].defaultNs)
                    {
                        string prefix = FindPrefix(ns);
                        if (prefix == null)
                        {
                            if (this.currentState != State.Attribute)
                            {
                                throw new ArgumentException(SR.Format(SR.Xml_UndefNamespace, ns));
                            }
                            prefix = GeneratePrefix(); // need a prefix if
                            PushNamespace(prefix, ns, false);
                        }
                        if (prefix.Length != 0)
                        {
                            InternalWriteName(prefix, true);
                            textWriter.Write(':');
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(ns))
                {
                    throw new ArgumentException(SR.Xml_NoNamespaces);
                }
                InternalWriteName(localName, true);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        // Returns the closest prefix defined in the current namespace scope for the specified namespace URI.
        public override string LookupPrefix(string ns)
        {
            if (string.IsNullOrEmpty(ns))
            {
                throw new ArgumentException(SR.Xml_EmptyName);
            }
            string s = FindPrefix(ns);
            if (s == null && ns == stack[top].defaultNs)
            {
                s = string.Empty;
            }
            return s;
        }

        // Gets an XmlSpace representing the current xml:space scope. 
        public override XmlSpace XmlSpace
        {
            get
            {
                for (int i = top; i > 0; i--)
                {
                    XmlSpace xs = stack[i].xmlSpace;
                    if (xs != XmlSpace.None)
                        return xs;
                }
                return XmlSpace.None;
            }
        }

        // Gets the current xml:lang scope.
        public override string XmlLang
        {
            get
            {
                for (int i = top; i > 0; i--)
                {
                    String xlang = stack[i].xmlLang;
                    if (xlang != null)
                        return xlang;
                }
                return null;
            }
        }

        // Writes out the specified name, ensuring it is a valid NmToken
        // according to the XML specification (http://www.w3.org/TR/1998/REC-xml-19980210#NT-Name).
        public override void WriteNmToken(string name)
        {
            try
            {
                AutoComplete(Token.Content);

                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(SR.Xml_EmptyName);
                }
                if (!ValidateNames.IsNmtokenNoNamespaces(name))
                {
                    throw new ArgumentException(SR.Format(SR.Xml_InvalidNameChars, name));
                }
                textWriter.Write(name);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        //
        // Private implementation methods
        //
        void StartDocument(int standalone)
        {
            try
            {
                if (this.currentState != State.Start)
                {
                    throw new InvalidOperationException(SR.Xml_NotTheFirst);
                }
                this.stateTable = stateTableDocument;
                this.currentState = State.Prolog;

                StringBuilder bufBld = new StringBuilder(128);
                bufBld.Append("version=");
                bufBld.Append(quoteChar);
                bufBld.Append("1.0");
                bufBld.Append(quoteChar);
                if (this.encoding != null)
                {
                    bufBld.Append(" encoding=");
                    bufBld.Append(quoteChar);
                    bufBld.Append(this.encoding.WebName);
                    bufBld.Append(quoteChar);
                }
                if (standalone >= 0)
                {
                    bufBld.Append(" standalone=");
                    bufBld.Append(quoteChar);
                    bufBld.Append(standalone == 0 ? "no" : "yes");
                    bufBld.Append(quoteChar);
                }
                InternalWriteProcessingInstruction("xml", bufBld.ToString());
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        void AutoComplete(Token token)
        {
            if (this.currentState == State.Closed)
            {
                throw new InvalidOperationException(SR.Xml_Closed);
            }
            else if (this.currentState == State.Error)
            {
                throw new InvalidOperationException(SR.Format(SR.Xml_WrongToken, tokenName[(int)token], stateName[(int)State.Error]));
            }

            State newState = this.stateTable[(int)token * 8 + (int)this.currentState];
            if (newState == State.Error)
            {
                throw new InvalidOperationException(SR.Format(SR.Xml_WrongToken, tokenName[(int)token], stateName[(int)this.currentState]));
            }

            switch (token)
            {
                case Token.Doctype:
                    if (this.indented && this.currentState != State.Start)
                    {
                        Indent(false);
                    }
                    break;

                case Token.StartElement:
                case Token.Comment:
                case Token.PI:
                case Token.CData:
                    if (this.currentState == State.Attribute)
                    {
                        WriteEndAttributeQuote();
                        WriteEndStartTag(false);
                    }
                    else if (this.currentState == State.Element)
                    {
                        WriteEndStartTag(false);
                    }
                    if (token == Token.CData)
                    {
                        stack[top].mixed = true;
                    }
                    else if (this.indented && this.currentState != State.Start)
                    {
                        Indent(false);
                    }
                    break;

                case Token.EndElement:
                case Token.LongEndElement:
                    if (this.flush)
                    {
                        FlushEncoders();
                    }
                    if (this.currentState == State.Attribute)
                    {
                        WriteEndAttributeQuote();
                    }
                    if (this.currentState == State.Content)
                    {
                        token = Token.LongEndElement;
                    }
                    else
                    {
                        WriteEndStartTag(token == Token.EndElement);
                    }
                    if (stateTableDocument == this.stateTable && top == 1)
                    {
                        newState = State.Epilog;
                    }
                    break;

                case Token.StartAttribute:
                    if (this.flush)
                    {
                        FlushEncoders();
                    }
                    if (this.currentState == State.Attribute)
                    {
                        WriteEndAttributeQuote();
                        textWriter.Write(' ');
                    }
                    else if (this.currentState == State.Element)
                    {
                        textWriter.Write(' ');
                    }
                    break;

                case Token.EndAttribute:
                    if (this.flush)
                    {
                        FlushEncoders();
                    }
                    WriteEndAttributeQuote();
                    break;

                case Token.Whitespace:
                case Token.Content:
                case Token.RawData:
                case Token.Base64:

                    if (token != Token.Base64 && this.flush)
                    {
                        FlushEncoders();
                    }
                    if (this.currentState == State.Element && this.lastToken != Token.Content)
                    {
                        WriteEndStartTag(false);
                    }
                    if (newState == State.Content)
                    {
                        stack[top].mixed = true;
                    }
                    break;

                default:
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
            }
            this.currentState = newState;
            this.lastToken = token;
        }

        void AutoCompleteAll()
        {
            if (this.flush)
            {
                FlushEncoders();
            }
            while (top > 0)
            {
                WriteEndElement();
            }
        }

        void InternalWriteEndElement(bool longFormat)
        {
            try
            {
                if (top <= 0)
                {
                    throw new InvalidOperationException(SR.Xml_NoStartTag);
                }
                // if we are in the element, we need to close it.
                AutoComplete(longFormat ? Token.LongEndElement : Token.EndElement);
                if (this.lastToken == Token.LongEndElement)
                {
                    if (this.indented)
                    {
                        Indent(true);
                    }
                    textWriter.Write('<');
                    textWriter.Write('/');
                    if (this.namespaces && stack[top].prefix != null)
                    {
                        textWriter.Write(stack[top].prefix);
                        textWriter.Write(':');
                    }
                    textWriter.Write(stack[top].name);
                    textWriter.Write('>');
                }

                // pop namespaces
                int prevNsTop = stack[top].prevNsTop;
                if (useNsHashtable && prevNsTop < nsTop)
                {
                    PopNamespaces(prevNsTop + 1, nsTop);
                }
                nsTop = prevNsTop;
                top--;
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        void WriteEndStartTag(bool empty)
        {
            xmlEncoder.StartAttribute(false);
            for (int i = nsTop; i > stack[top].prevNsTop; i--)
            {
                if (!nsStack[i].declared)
                {
                    textWriter.Write(" xmlns");
                    textWriter.Write(':');
                    textWriter.Write(nsStack[i].prefix);
                    textWriter.Write('=');
                    textWriter.Write(this.quoteChar);
                    xmlEncoder.Write(nsStack[i].ns);
                    textWriter.Write(this.quoteChar);
                }
            }
            // Default
            if ((stack[top].defaultNs != stack[top - 1].defaultNs) &&
                (stack[top].defaultNsState == NamespaceState.DeclaredButNotWrittenOut))
            {
                textWriter.Write(" xmlns");
                textWriter.Write('=');
                textWriter.Write(this.quoteChar);
                xmlEncoder.Write(stack[top].defaultNs);
                textWriter.Write(this.quoteChar);
                stack[top].defaultNsState = NamespaceState.DeclaredAndWrittenOut;
            }
            xmlEncoder.EndAttribute();
            if (empty)
            {
                textWriter.Write(" /");
            }
            textWriter.Write('>');
        }

        void WriteEndAttributeQuote()
        {
            if (this.specialAttr != SpecialAttr.None)
            {
                // Ok, now to handle xmlspace, etc.
                HandleSpecialAttribute();
            }
            xmlEncoder.EndAttribute();
            textWriter.Write(this.curQuoteChar);
        }

        void Indent(bool beforeEndElement)
        {
            // pretty printing.
            if (top == 0)
            {
                textWriter.WriteLine();
            }
            else if (!stack[top].mixed)
            {
                textWriter.WriteLine();
                int i = beforeEndElement ? top - 1 : top;
                for (i *= this.indentation; i > 0; i--)
                {
                    textWriter.Write(this.indentChar);
                }
            }
        }

        // pushes new namespace scope, and returns generated prefix, if one
        // was needed to resolve conflicts.
        void PushNamespace(string prefix, string ns, bool declared)
        {
            if (XmlConst.ReservedNsXmlNs == ns)
            {
                throw new ArgumentException(SR.Xml_CanNotBindToReservedNamespace);
            }

            if (prefix == null)
            {
                switch (stack[top].defaultNsState)
                {
                    case NamespaceState.DeclaredButNotWrittenOut:
                        Debug.Assert(declared == true, "Unexpected situation!!");
                        // the first namespace that the user gave us is what we
                        // like to keep. 
                        break;
                    case NamespaceState.Uninitialized:
                    case NamespaceState.NotDeclaredButInScope:
                        // we now got a brand new namespace that we need to remember
                        stack[top].defaultNs = ns;
                        break;
                    default:
                        Debug.Fail("Should have never come here");
                        return;
                }
                stack[top].defaultNsState = (declared ? NamespaceState.DeclaredAndWrittenOut : NamespaceState.DeclaredButNotWrittenOut);
            }
            else
            {
                if (prefix.Length != 0 && ns.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_PrefixForEmptyNs);
                }

                int existingNsIndex = LookupNamespace(prefix);
                if (existingNsIndex != -1 && nsStack[existingNsIndex].ns == ns)
                {
                    // it is already in scope.
                    if (declared)
                    {
                        nsStack[existingNsIndex].declared = true;
                    }
                }
                else
                {
                    // see if prefix conflicts for the current element
                    if (declared)
                    {
                        if (existingNsIndex != -1 && existingNsIndex > stack[top].prevNsTop)
                        {
                            nsStack[existingNsIndex].declared = true; // old one is silenced now
                        }
                    }
                    AddNamespace(prefix, ns, declared);
                }
            }
        }

        void AddNamespace(string prefix, string ns, bool declared)
        {
            int nsIndex = ++nsTop;
            if (nsIndex == nsStack.Length)
            {
                Namespace[] newStack = new Namespace[nsIndex * 2];
                Array.Copy(nsStack, 0, newStack, 0, nsIndex);
                nsStack = newStack;
            }
            nsStack[nsIndex].Set(prefix, ns, declared);

            if (useNsHashtable)
            {
                AddToNamespaceHashtable(nsIndex);
            }
            else if (nsIndex == MaxNamespacesWalkCount)
            {
                // add all
                nsHashtable = new Dictionary<string, int>(new SecureStringHasher());
                for (int i = 0; i <= nsIndex; i++)
                {
                    AddToNamespaceHashtable(i);
                }
                useNsHashtable = true;
            }
        }

        void AddToNamespaceHashtable(int namespaceIndex)
        {
            string prefix = nsStack[namespaceIndex].prefix;
            int existingNsIndex;
            if (nsHashtable.TryGetValue(prefix, out existingNsIndex))
            {
                nsStack[namespaceIndex].prevNsIndex = existingNsIndex;
            }
            nsHashtable[prefix] = namespaceIndex;
        }

        private void PopNamespaces(int indexFrom, int indexTo)
        {
            Debug.Assert(useNsHashtable);
            for (int i = indexTo; i >= indexFrom; i--)
            {
                Debug.Assert(nsHashtable.ContainsKey(nsStack[i].prefix));
                if (nsStack[i].prevNsIndex == -1)
                {
                    nsHashtable.Remove(nsStack[i].prefix);
                }
                else
                {
                    nsHashtable[nsStack[i].prefix] = nsStack[i].prevNsIndex;
                }
            }
        }

        string GeneratePrefix()
        {
            int temp = stack[top].prefixCount++ + 1;
            return "d" + top.ToString("d", CultureInfo.InvariantCulture)
                + "p" + temp.ToString("d", CultureInfo.InvariantCulture);
        }

        void InternalWriteProcessingInstruction(string name, string text)
        {
            textWriter.Write("<?");
            ValidateName(name, false);
            textWriter.Write(name);
            textWriter.Write(' ');
            if (null != text)
            {
                xmlEncoder.WriteRawWithSurrogateChecking(text);
            }
            textWriter.Write("?>");
        }

        int LookupNamespace(string prefix)
        {
            if (useNsHashtable)
            {
                int nsIndex;
                if (nsHashtable.TryGetValue(prefix, out nsIndex))
                {
                    return nsIndex;
                }
            }
            else
            {
                for (int i = nsTop; i >= 0; i--)
                {
                    if (nsStack[i].prefix == prefix)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        int LookupNamespaceInCurrentScope(string prefix)
        {
            if (useNsHashtable)
            {
                int nsIndex;
                if (nsHashtable.TryGetValue(prefix, out nsIndex))
                {
                    if (nsIndex > stack[top].prevNsTop)
                    {
                        return nsIndex;
                    }
                }
            }
            else
            {
                for (int i = nsTop; i > stack[top].prevNsTop; i--)
                {
                    if (nsStack[i].prefix == prefix)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        string FindPrefix(string ns)
        {
            for (int i = nsTop; i >= 0; i--)
            {
                if (nsStack[i].ns == ns)
                {
                    if (LookupNamespace(nsStack[i].prefix) == i)
                    {
                        return nsStack[i].prefix;
                    }
                }
            }
            return null;
        }

        // There are three kind of strings we write out - Name, LocalName and Prefix.
        // Both LocalName and Prefix can be represented with NCName == false and Name
        // can be represented as NCName == true

        void InternalWriteName(string name, bool isNCName)
        {
            ValidateName(name, isNCName);
            textWriter.Write(name);
        }

        // This method is used for validation of the DOCTYPE, processing instruction and entity names plus names 
        // written out by the user via WriteName and WriteQualifiedName.
        // Unfortunately the names of elements and attributes are not validated by the XmlTextWriter.
        // Also this method does not check whether the character after ':' is a valid start name character. It accepts
        // all valid name characters at that position. This can't be changed because of backwards compatibility.
        private unsafe void ValidateName(string name, bool isNCName)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(SR.Xml_EmptyName);
            }

            int nameLength = name.Length;

            // Namespaces supported
            if (namespaces)
            {
                // We can't use ValidateNames.ParseQName here because of backwards compatibility bug we need to preserve.
                // The bug is that the character after ':' is validated only as a NCName characters instead of NCStartName.
                int colonPosition = -1;

                // Parse NCName (may be prefix, may be local name)
                int position = ValidateNames.ParseNCName(name);

            Continue:
                if (position == nameLength)
                {
                    return;
                }

                // we have prefix:localName
                if (name[position] == ':')
                {
                    if (!isNCName)
                    {
                        // first colon in qname
                        if (colonPosition == -1)
                        {
                            // make sure it is not the first or last characters
                            if (position > 0 && position + 1 < nameLength)
                            {
                                colonPosition = position;
                                // Because of the back-compat bug (described above) parse the rest as Nmtoken
                                position++;
                                position += ValidateNames.ParseNmtoken(name, position);
                                goto Continue;
                            }
                        }
                    }
                }
            }
            // Namespaces not supported
            else
            {
                if (ValidateNames.IsNameNoNamespaces(name))
                {
                    return;
                }
            }
            throw new ArgumentException(SR.Format(SR.Xml_InvalidNameChars, name));
        }

        void HandleSpecialAttribute()
        {
            string value = xmlEncoder.AttributeValue;
            switch (this.specialAttr)
            {
                case SpecialAttr.XmlLang:
                    stack[top].xmlLang = value;
                    break;
                case SpecialAttr.XmlSpace:
                    // validate XmlSpace attribute
                    value = XmlConvertEx.TrimString(value);
                    if (value == "default")
                    {
                        stack[top].xmlSpace = XmlSpace.Default;
                    }
                    else if (value == "preserve")
                    {
                        stack[top].xmlSpace = XmlSpace.Preserve;
                    }
                    else
                    {
                        throw new ArgumentException(SR.Format(SR.Xml_InvalidXmlSpace, value));
                    }
                    break;
                case SpecialAttr.XmlNs:
                    VerifyPrefixXml(this.prefixForXmlNs, value);
                    PushNamespace(this.prefixForXmlNs, value, true);
                    break;
            }
        }


        void VerifyPrefixXml(string prefix, string ns)
        {
            if (prefix != null && prefix.Length == 3)
            {
                if (
                   (prefix[0] == 'x' || prefix[0] == 'X') &&
                   (prefix[1] == 'm' || prefix[1] == 'M') &&
                   (prefix[2] == 'l' || prefix[2] == 'L')
                   )
                {
                    if (XmlConst.ReservedNsXml != ns)
                    {
                        throw new ArgumentException(SR.Xml_InvalidPrefix);
                    }
                }
            }
        }

        void PushStack()
        {
            if (top == stack.Length - 1)
            {
                TagInfo[] na = new TagInfo[stack.Length + 10];
                if (top > 0) Array.Copy(stack, 0, na, 0, top + 1);
                stack = na;
            }

            top++; // Move up stack
            stack[top].Init(nsTop);
        }

        void FlushEncoders()
        {
            if (null != this.base64Encoder)
            {
                // The Flush will call WriteRaw to write out the rest of the encoded characters
                this.base64Encoder.Flush();
            }
            this.flush = false;
        }
    }
}
