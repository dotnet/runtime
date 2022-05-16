// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Xml
{
    // Specifies formatting options for XmlTextWriter.
    public enum Formatting
    {
        // No special formatting is done (this is the default).
        None,

        //This option causes child elements to be indented using the Indentation and IndentChar properties.
        // It only indents Element Content (http://www.w3.org/TR/1998/REC-xml-19980210#sec-element-content)
        // and not Mixed Content (http://www.w3.org/TR/1998/REC-xml-19980210#sec-mixed-content)
        // according to the XML 1.0 definitions of these terms.
        Indented,
    }

    // Represents a writer that provides fast non-cached forward-only way of generating XML streams
    // containing XML documents that conform to the W3CExtensible Markup Language (XML) 1.0 specification
    // and the Namespaces in XML specification.

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class XmlTextWriter : XmlWriter
    {
        //
        // Private types
        //
        private enum NamespaceState
        {
            Uninitialized,
            NotDeclaredButInScope,
            DeclaredButNotWrittenOut,
            DeclaredAndWrittenOut
        }

        private struct TagInfo
        {
            internal string? name;
            internal string? prefix;
            internal string defaultNs;
            internal NamespaceState defaultNsState;
            internal XmlSpace xmlSpace;
            internal string? xmlLang;
            internal int prevNsTop;
            internal int prefixCount;
            internal bool mixed; // whether to pretty print the contents of this element.

            internal void Init(int nsTop)
            {
                name = null;
                defaultNs = string.Empty;
                defaultNsState = NamespaceState.Uninitialized;
                xmlSpace = XmlSpace.None;
                xmlLang = null;
                prevNsTop = nsTop;
                prefixCount = 0;
                mixed = false;
            }
        }

        private struct Namespace
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

        private enum SpecialAttr
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
        private readonly TextWriter _textWriter = null!;
        private readonly XmlTextEncoder _xmlEncoder = null!;
        private readonly Encoding? _encoding;

        // formatting
        private Formatting _formatting;
        private bool _indented; // perf - faster to check a boolean.
        private int _indentation;
        private char[] _indentChars;
        private static readonly char[] s_defaultIndentChars = CreateDefaultIndentChars();

        private static char[] CreateDefaultIndentChars()
        {
            var result = new char[IndentArrayLength];
            Array.Fill(result, DefaultIndentChar);
            return result;
        }

        // element stack
        private TagInfo[] _stack;
        private int _top;

        // state machine for AutoComplete
        private State[] _stateTable;
        private State _currentState;
        private Token _lastToken;

        // Base64 content
        private XmlTextWriterBase64Encoder? _base64Encoder;

        // misc
        private char _quoteChar;
        private char _curQuoteChar;
        private bool _namespaces;
        private SpecialAttr _specialAttr;
        private string? _prefixForXmlNs;
        private bool _flush;

        // namespaces
        private Namespace[] _nsStack;
        private int _nsTop;
        private Dictionary<string, int>? _nsHashtable;
        private bool _useNsHashtable;

        //
        // Constants and constant tables
        //
        private const int IndentArrayLength = 64;
        private const char DefaultIndentChar = ' ';
        private const int NamespaceStackInitialSize = 8;
#if DEBUG
        private const int MaxNamespacesWalkCount = 3;
#else
        private const int MaxNamespacesWalkCount = 16;
#endif

        private static readonly string[] s_stateName = {
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

        private static readonly string[] s_tokenName = {
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

        private static readonly State[] s_stateTableDefault = {
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

        private static readonly State[] s_stateTableDocument = {
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
        private XmlTextWriter()
        {
            _namespaces = true;
            _formatting = Formatting.None;
            _indentation = 2;
            _indentChars = s_defaultIndentChars;

            // namespaces
            _nsStack = new Namespace[NamespaceStackInitialSize];
            _nsTop = -1;
            // element stack
            _stack = new TagInfo[10];
            _top = 0; // 0 is an empty sentanial element
            _stack[_top].Init(-1);
            _quoteChar = '"';

            _stateTable = s_stateTableDefault;
            _currentState = State.Start;
            _lastToken = Token.Empty;
        }

        // Creates an instance of the XmlTextWriter class using the specified stream.
        public XmlTextWriter(Stream w, Encoding? encoding) : this()
        {
            _encoding = encoding;
            if (encoding != null)
                _textWriter = new StreamWriter(w, encoding);
            else
                _textWriter = new StreamWriter(w);
            _xmlEncoder = new XmlTextEncoder(_textWriter);
            _xmlEncoder.QuoteChar = _quoteChar;
        }

        // Creates an instance of the XmlTextWriter class using the specified file.
        public XmlTextWriter(string filename, Encoding? encoding)
        : this(new FileStream(filename, FileMode.Create,
                              FileAccess.Write, FileShare.Read), encoding)
        {
        }

        // Creates an instance of the XmlTextWriter class using the specified TextWriter.
        public XmlTextWriter(TextWriter w) : this()
        {
            _textWriter = w;

            _encoding = w.Encoding;
            _xmlEncoder = new XmlTextEncoder(w);
            _xmlEncoder.QuoteChar = _quoteChar;
        }

        //
        // XmlTextWriter properties
        //
        // Gets the XmlTextWriter base stream.
        public Stream? BaseStream
        {
            get
            {
                if (_textWriter is StreamWriter streamWriter)
                {
                    return streamWriter.BaseStream;
                }
                else
                {
                    return null;
                }
            }
        }

        // Gets or sets a value indicating whether to do namespace support.
        public bool Namespaces
        {
            get { return _namespaces; }
            set
            {
                if (_currentState != State.Start)
                    throw new InvalidOperationException(SR.Xml_NotInWriteState);

                _namespaces = value;
            }
        }

        // Indicates how the output is formatted.
        public Formatting Formatting
        {
            get { return _formatting; }
            set { _formatting = value; _indented = value == Formatting.Indented; }
        }

        // Gets or sets how many IndentChars to write for each level in the hierarchy when Formatting is set to "Indented".
        public int Indentation
        {
            get { return _indentation; }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.Xml_InvalidIndentation);
                _indentation = value;
            }
        }

        // Gets or sets which character to use for indenting when Formatting is set to "Indented".
        public char IndentChar
        {
            get { return _indentChars[0]; }
            set
            {
                if (value == DefaultIndentChar)
                {
                    _indentChars = s_defaultIndentChars;
                    return;
                }

                if (ReferenceEquals(_indentChars, s_defaultIndentChars))
                {
                    _indentChars = new char[IndentArrayLength];
                }

                for (int i = 0; i < IndentArrayLength; i++)
                {
                    _indentChars[i] = value;
                }
            }
        }

        // Gets or sets which character to use to quote attribute values.
        public char QuoteChar
        {
            get { return _quoteChar; }
            set
            {
                if (value != '"' && value != '\'')
                {
                    throw new ArgumentException(SR.Xml_InvalidQuote);
                }
                _quoteChar = value;
                _xmlEncoder.QuoteChar = value;
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
                if (_currentState != State.Epilog)
                {
                    if (_currentState == State.Closed)
                    {
                        throw new ArgumentException(SR.Xml_ClosedOrError);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Xml_NoRoot);
                    }
                }
                _stateTable = s_stateTableDefault;
                _currentState = State.Start;
                _lastToken = Token.Empty;
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Writes out the DOCTYPE declaration with the specified name and optional attributes.
        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            try
            {
                ValidateName(name, false);

                AutoComplete(Token.Doctype);
                _textWriter.Write("<!DOCTYPE ");
                _textWriter.Write(name);
                if (pubid != null)
                {
                    _textWriter.Write($" PUBLIC {_quoteChar}");
                    _textWriter.Write(pubid);
                    _textWriter.Write($"{_quoteChar} {_quoteChar}");
                    _textWriter.Write(sysid);
                    _textWriter.Write(_quoteChar);
                }
                else if (sysid != null)
                {
                    _textWriter.Write($" SYSTEM {_quoteChar}");
                    _textWriter.Write(sysid);
                    _textWriter.Write(_quoteChar);
                }
                if (subset != null)
                {
                    _textWriter.Write("[");
                    _textWriter.Write(subset);
                    _textWriter.Write("]");
                }
                _textWriter.Write('>');
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Writes out the specified start tag and associates it with the given namespace and prefix.
        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            try
            {
                AutoComplete(Token.StartElement);
                PushStack();
                _textWriter.Write('<');

                if (_namespaces)
                {
                    // Propagate default namespace and mix model down the stack.
                    _stack[_top].defaultNs = _stack[_top - 1].defaultNs;
                    if (_stack[_top - 1].defaultNsState != NamespaceState.Uninitialized)
                        _stack[_top].defaultNsState = NamespaceState.NotDeclaredButInScope;
                    _stack[_top].mixed = _stack[_top - 1].mixed;
                    if (ns == null)
                    {
                        // use defined prefix
                        if (prefix != null && prefix.Length != 0 && (LookupNamespace(prefix) == -1))
                        {
                            throw new ArgumentException(SR.Xml_UndefPrefix);
                        }
                    }
                    else
                    {
                        if (prefix == null)
                        {
                            string? definedPrefix = FindPrefix(ns);
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
                    _stack[_top].prefix = null;
                    if (prefix != null && prefix.Length != 0)
                    {
                        _stack[_top].prefix = prefix;
                        _textWriter.Write(prefix);
                        _textWriter.Write(':');
                    }
                }
                else
                {
                    if ((ns != null && ns.Length != 0) || (prefix != null && prefix.Length != 0))
                    {
                        throw new ArgumentException(SR.Xml_NoNamespaces);
                    }
                }
                _stack[_top].name = localName;
                _textWriter.Write(localName);
            }
            catch
            {
                _currentState = State.Error;
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
        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            try
            {
                AutoComplete(Token.StartAttribute);

                _specialAttr = SpecialAttr.None;
                if (_namespaces)
                {
                    if (prefix != null && prefix.Length == 0)
                    {
                        prefix = null;
                    }

                    if (ns == XmlReservedNs.NsXmlNs && prefix == null && localName != "xmlns")
                    {
                        prefix = "xmlns";
                    }

                    if (prefix == "xml")
                    {
                        if (localName == "lang")
                        {
                            _specialAttr = SpecialAttr.XmlLang;
                        }
                        else if (localName == "space")
                        {
                            _specialAttr = SpecialAttr.XmlSpace;
                        }
                        /* bug54408. to be fwd compatible we need to treat xml prefix as reserved
                        and not really insist on a specific value. Who knows in the future it
                        might be OK to say xml:blabla
                        else {
                            throw new ArgumentException(SR.Xml_InvalidPrefix);
                        }*/
                    }
                    else if (prefix == "xmlns")
                    {
                        if (XmlReservedNs.NsXmlNs != ns && ns != null)
                        {
                            throw new ArgumentException(SR.Xml_XmlnsBelongsToReservedNs);
                        }

                        if (localName == null || localName.Length == 0)
                        {
                            localName = prefix;
                            prefix = null;
                            _prefixForXmlNs = null;
                        }
                        else
                        {
                            _prefixForXmlNs = localName;
                        }

                        _specialAttr = SpecialAttr.XmlNs;
                    }
                    else if (prefix == null && localName == "xmlns")
                    {
                        if (XmlReservedNs.NsXmlNs != ns && ns != null)
                        {
                            // add the below line back in when DOM is fixed
                            throw new ArgumentException(SR.Xml_XmlnsBelongsToReservedNs);
                        }

                        _specialAttr = SpecialAttr.XmlNs;
                        _prefixForXmlNs = null;
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
                            string? definedPrefix = FindPrefix(ns);
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

                    if (prefix != null && prefix.Length != 0)
                    {
                        _textWriter.Write(prefix);
                        _textWriter.Write(':');
                    }
                }
                else
                {
                    if ((ns != null && ns.Length != 0) || (prefix != null && prefix.Length != 0))
                    {
                        throw new ArgumentException(SR.Xml_NoNamespaces);
                    }

                    if (localName == "xml:lang")
                    {
                        _specialAttr = SpecialAttr.XmlLang;
                    }

                    else if (localName == "xml:space")
                    {
                        _specialAttr = SpecialAttr.XmlSpace;
                    }
                }

                _xmlEncoder.StartAttribute(_specialAttr != SpecialAttr.None);

                _textWriter.Write(localName);
                _textWriter.Write('=');
                if (_curQuoteChar != _quoteChar)
                {
                    _curQuoteChar = _quoteChar;
                    _xmlEncoder.QuoteChar = _quoteChar;
                }
                _textWriter.Write(_curQuoteChar);
            }
            catch
            {
                _currentState = State.Error;
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
                _currentState = State.Error;
                throw;
            }
        }

        // Writes out a &lt;![CDATA[...]]&gt; block containing the specified text.
        public override void WriteCData(string? text)
        {
            try
            {
                AutoComplete(Token.CData);
                if (null != text && text.Contains("]]>"))
                {
                    throw new ArgumentException(SR.Xml_InvalidCDataChars);
                }

                _textWriter.Write("<![CDATA[");

                if (null != text)
                {
                    _xmlEncoder.WriteRawWithSurrogateChecking(text);
                }

                _textWriter.Write("]]>");
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Writes out a comment <!--...--> containing the specified text.
        public override void WriteComment(string? text)
        {
            try
            {
                if (null != text && (text.Contains("--") || (text.Length != 0 && text[text.Length - 1] == '-')))
                {
                    throw new ArgumentException(SR.Xml_InvalidCommentChars);
                }
                AutoComplete(Token.Comment);
                _textWriter.Write("<!--");
                if (null != text)
                {
                    _xmlEncoder.WriteRawWithSurrogateChecking(text);
                }
                _textWriter.Write("-->");
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Writes out a processing instruction with a space between the name and text as follows: <?name text?>
        public override void WriteProcessingInstruction(string name, string? text)
        {
            try
            {
                if (null != text && text.Contains("?>"))
                {
                    throw new ArgumentException(SR.Xml_InvalidPiChars);
                }

                if (string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase) && _stateTable == s_stateTableDocument)
                {
                    throw new ArgumentException(SR.Xml_DupXmlDecl);
                }

                AutoComplete(Token.PI);
                InternalWriteProcessingInstruction(name, text);
            }
            catch
            {
                _currentState = State.Error;
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
                _xmlEncoder.WriteEntityRef(name);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Forces the generation of a character entity for the specified Unicode character value.
        public override void WriteCharEntity(char ch)
        {
            try
            {
                AutoComplete(Token.Content);
                _xmlEncoder.WriteCharEntity(ch);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Writes out the given whitespace.
        public override void WriteWhitespace(string? ws)
        {
            try
            {
                if (null == ws)
                {
                    ws = string.Empty;
                }

                if (!XmlCharType.IsOnlyWhitespace(ws))
                {
                    throw new ArgumentException(SR.Xml_NonWhitespace);
                }
                AutoComplete(Token.Whitespace);
                _xmlEncoder.Write(ws);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Writes out the specified text content.
        public override void WriteString(string? text)
        {
            try
            {
                if (null != text && text.Length != 0)
                {
                    AutoComplete(Token.Content);
                    _xmlEncoder.Write(text);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Writes out the specified surrogate pair as a character entity.
        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            try
            {
                AutoComplete(Token.Content);
                _xmlEncoder.WriteSurrogateCharEntity(lowChar, highChar);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }


        // Writes out the specified text content.
        public override void WriteChars(char[] buffer, int index, int count)
        {
            try
            {
                AutoComplete(Token.Content);
                _xmlEncoder.Write(buffer, index, count);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Writes raw markup from the specified character buffer.
        public override void WriteRaw(char[] buffer, int index, int count)
        {
            try
            {
                AutoComplete(Token.RawData);
                _xmlEncoder.WriteRaw(buffer, index, count);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Writes raw markup from the specified character string.
        public override void WriteRaw(string data)
        {
            try
            {
                AutoComplete(Token.RawData);
                _xmlEncoder.WriteRawWithSurrogateChecking(data);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Encodes the specified binary bytes as base64 and writes out the resulting text.
        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            try
            {
                if (!_flush)
                {
                    AutoComplete(Token.Base64);
                }

                _flush = true;
                // No need for us to explicitly validate the args. The StreamWriter will do
                // it for us.
                if (null == _base64Encoder)
                {
                    _base64Encoder = new XmlTextWriterBase64Encoder(_xmlEncoder);
                }
                // Encode will call WriteRaw to write out the encoded characters
                _base64Encoder.Encode(buffer, index, count);
            }
            catch
            {
                _currentState = State.Error;
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
                _currentState = State.Error;
                throw;
            }
        }

        // Returns the state of the XmlWriter.
        public override WriteState WriteState
        {
            get
            {
                switch (_currentState)
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
                        Debug.Fail($"Unexpected state {_currentState}");
                        return WriteState.Error;
                }
            }
        }

        // Closes the XmlWriter and the underlying stream/TextWriter.
        public override void Close()
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
                _currentState = State.Closed;
                _textWriter.Dispose();
            }
        }

        // Flushes whatever is in the buffer to the underlying stream/TextWriter and flushes the underlying stream/TextWriter.
        public override void Flush()
        {
            _textWriter.Flush();
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
                _currentState = State.Error;
                throw;
            }
        }

        // Writes out the specified namespace-qualified name by looking up the prefix that is in scope for the given namespace.
        public override void WriteQualifiedName(string localName, string? ns)
        {
            try
            {
                AutoComplete(Token.Content);
                if (_namespaces)
                {
                    if (ns != null && ns.Length != 0 && ns != _stack[_top].defaultNs)
                    {
                        string? prefix = FindPrefix(ns);
                        if (prefix == null)
                        {
                            if (_currentState != State.Attribute)
                            {
                                throw new ArgumentException(SR.Format(SR.Xml_UndefNamespace, ns));
                            }

                            prefix = GeneratePrefix();
                            PushNamespace(prefix, ns, false);
                        }

                        if (prefix.Length != 0)
                        {
                            InternalWriteName(prefix, true);
                            _textWriter.Write(':');
                        }
                    }
                }
                else if (ns != null && ns.Length != 0)
                {
                    throw new ArgumentException(SR.Xml_NoNamespaces);
                }

                InternalWriteName(localName, true);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        // Returns the closest prefix defined in the current namespace scope for the specified namespace URI.
        public override string? LookupPrefix(string ns)
        {
            if (ns == null || ns.Length == 0)
            {
                throw new ArgumentException(SR.Xml_EmptyName);
            }

            string? s = FindPrefix(ns);
            if (s == null && ns == _stack[_top].defaultNs)
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
                for (int i = _top; i > 0; i--)
                {
                    XmlSpace xs = _stack[i].xmlSpace;
                    if (xs != XmlSpace.None)
                        return xs;
                }
                return XmlSpace.None;
            }
        }

        // Gets the current xml:lang scope.
        public override string? XmlLang
        {
            get
            {
                for (int i = _top; i > 0; i--)
                {
                    string? xlang = _stack[i].xmlLang;

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

                if (name == null || name.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyName);
                }
                if (!ValidateNames.IsNmtokenNoNamespaces(name))
                {
                    throw new ArgumentException(SR.Format(SR.Xml_InvalidNameChars, name));
                }
                _textWriter.Write(name);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        //
        // Private implementation methods
        //
        private void StartDocument(int standalone)
        {
            try
            {
                if (_currentState != State.Start)
                {
                    throw new InvalidOperationException(SR.Xml_NotTheFirst);
                }
                _stateTable = s_stateTableDocument;
                _currentState = State.Prolog;

                StringBuilder bufBld = new StringBuilder(128);
                bufBld.Append($"version={_quoteChar}1.0{_quoteChar}");
                if (_encoding != null)
                {
                    bufBld.Append(" encoding=");
                    bufBld.Append(_quoteChar);
                    bufBld.Append(_encoding.WebName);
                    bufBld.Append(_quoteChar);
                }
                if (standalone >= 0)
                {
                    bufBld.Append(" standalone=");
                    bufBld.Append(_quoteChar);
                    bufBld.Append(standalone == 0 ? "no" : "yes");
                    bufBld.Append(_quoteChar);
                }
                InternalWriteProcessingInstruction("xml", bufBld.ToString());
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private void AutoComplete(Token token)
        {
            if (_currentState == State.Closed)
            {
                throw new InvalidOperationException(SR.Xml_Closed);
            }
            else if (_currentState == State.Error)
            {
                throw new InvalidOperationException(SR.Format(SR.Xml_WrongToken, s_tokenName[(int)token], s_stateName[(int)State.Error]));
            }

            State newState = _stateTable[(int)token * 8 + (int)_currentState];
            if (newState == State.Error)
            {
                throw new InvalidOperationException(SR.Format(SR.Xml_WrongToken, s_tokenName[(int)token], s_stateName[(int)_currentState]));
            }

            switch (token)
            {
                case Token.Doctype:
                    if (_indented && _currentState != State.Start)
                    {
                        Indent(false);
                    }
                    break;

                case Token.StartElement:
                case Token.Comment:
                case Token.PI:
                case Token.CData:
                    if (_currentState == State.Attribute)
                    {
                        WriteEndAttributeQuote();
                        WriteEndStartTag(false);
                    }
                    else if (_currentState == State.Element)
                    {
                        WriteEndStartTag(false);
                    }
                    if (token == Token.CData)
                    {
                        _stack[_top].mixed = true;
                    }
                    else if (_indented && _currentState != State.Start)
                    {
                        Indent(false);
                    }
                    break;

                case Token.EndElement:
                case Token.LongEndElement:
                    if (_flush)
                    {
                        FlushEncoders();
                    }
                    if (_currentState == State.Attribute)
                    {
                        WriteEndAttributeQuote();
                    }
                    if (_currentState == State.Content)
                    {
                        token = Token.LongEndElement;
                    }
                    else
                    {
                        WriteEndStartTag(token == Token.EndElement);
                    }
                    if (s_stateTableDocument == _stateTable && _top == 1)
                    {
                        newState = State.Epilog;
                    }
                    break;

                case Token.StartAttribute:
                    if (_flush)
                    {
                        FlushEncoders();
                    }
                    if (_currentState == State.Attribute)
                    {
                        WriteEndAttributeQuote();
                        _textWriter.Write(' ');
                    }
                    else if (_currentState == State.Element)
                    {
                        _textWriter.Write(' ');
                    }
                    break;

                case Token.EndAttribute:
                    if (_flush)
                    {
                        FlushEncoders();
                    }
                    WriteEndAttributeQuote();
                    break;

                case Token.Whitespace:
                case Token.Content:
                case Token.RawData:
                case Token.Base64:

                    if (token != Token.Base64 && _flush)
                    {
                        FlushEncoders();
                    }
                    if (_currentState == State.Element && _lastToken != Token.Content)
                    {
                        WriteEndStartTag(false);
                    }
                    if (newState == State.Content)
                    {
                        _stack[_top].mixed = true;
                    }
                    break;

                default:
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
            }
            _currentState = newState;
            _lastToken = token;
        }

        private void AutoCompleteAll()
        {
            if (_flush)
            {
                FlushEncoders();
            }
            while (_top > 0)
            {
                WriteEndElement();
            }
        }

        private static readonly char[] s_selfClosingTagOpen = new char[] { '<', '/' };

        private void InternalWriteEndElement(bool longFormat)
        {
            try
            {
                if (_top <= 0)
                {
                    throw new InvalidOperationException(SR.Xml_NoStartTag);
                }
                // if we are in the element, we need to close it.
                AutoComplete(longFormat ? Token.LongEndElement : Token.EndElement);
                if (_lastToken == Token.LongEndElement)
                {
                    if (_indented)
                    {
                        Indent(true);
                    }
                    _textWriter.Write(s_selfClosingTagOpen);
                    if (_namespaces && _stack[_top].prefix != null)
                    {
                        _textWriter.Write(_stack[_top].prefix);
                        _textWriter.Write(':');
                    }
                    _textWriter.Write(_stack[_top].name);
                    _textWriter.Write('>');
                }

                // pop namespaces
                int prevNsTop = _stack[_top].prevNsTop;
                if (_useNsHashtable && prevNsTop < _nsTop)
                {
                    PopNamespaces(prevNsTop + 1, _nsTop);
                }
                _nsTop = prevNsTop;
                _top--;
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private static readonly char[] s_closeTagEnd = new char[] { ' ', '/', '>' };

        private void WriteEndStartTag(bool empty)
        {
            _xmlEncoder.StartAttribute(false);
            for (int i = _nsTop; i > _stack[_top].prevNsTop; i--)
            {
                if (!_nsStack[i].declared)
                {
                    _textWriter.Write(" xmlns:");
                    _textWriter.Write(_nsStack[i].prefix);
                    _textWriter.Write('=');
                    _textWriter.Write(_quoteChar);
                    _xmlEncoder.Write(_nsStack[i].ns);
                    _textWriter.Write(_quoteChar);
                }
            }
            // Default
            if ((_stack[_top].defaultNs != _stack[_top - 1].defaultNs) &&
                (_stack[_top].defaultNsState == NamespaceState.DeclaredButNotWrittenOut))
            {
                _textWriter.Write(" xmlns=");
                _textWriter.Write(_quoteChar);
                _xmlEncoder.Write(_stack[_top].defaultNs);
                _textWriter.Write(_quoteChar);
                _stack[_top].defaultNsState = NamespaceState.DeclaredAndWrittenOut;
            }
            _xmlEncoder.EndAttribute();
            if (empty)
            {
                _textWriter.Write(s_closeTagEnd);
            }
            else
            {
                _textWriter.Write('>');
            }
        }

        private void WriteEndAttributeQuote()
        {
            if (_specialAttr != SpecialAttr.None)
            {
                // Ok, now to handle xmlspace, etc.
                HandleSpecialAttribute();
            }
            _xmlEncoder.EndAttribute();
            _textWriter.Write(_curQuoteChar);
        }

        private void Indent(bool beforeEndElement)
        {
            // pretty printing.
            if (_top == 0)
            {
                _textWriter.WriteLine();
            }
            else if (!_stack[_top].mixed)
            {
                _textWriter.WriteLine();
                int i = (beforeEndElement ? _top - 1 : _top) * _indentation;
                if (i <= _indentChars.Length)
                {
                    _textWriter.Write(_indentChars, 0, i);
                }
                else
                {
                    while (i > 0)
                    {
                        _textWriter.Write(_indentChars, 0, Math.Min(i, _indentChars.Length));
                        i -= _indentChars.Length;
                    }
                }
            }
        }

        // pushes new namespace scope, and returns generated prefix, if one
        // was needed to resolve conflicts.
        private void PushNamespace(string? prefix, string ns, bool declared)
        {
            if (XmlReservedNs.NsXmlNs == ns)
            {
                throw new ArgumentException(SR.Xml_CanNotBindToReservedNamespace);
            }

            if (prefix == null)
            {
                switch (_stack[_top].defaultNsState)
                {
                    case NamespaceState.DeclaredButNotWrittenOut:
                        Debug.Assert(declared == true, "Unexpected situation!!");
                        // the first namespace that the user gave us is what we
                        // like to keep.
                        break;
                    case NamespaceState.Uninitialized:
                    case NamespaceState.NotDeclaredButInScope:
                        // we now got a brand new namespace that we need to remember
                        _stack[_top].defaultNs = ns;
                        break;
                    default:
                        Debug.Fail("Should have never come here");
                        return;
                }

                _stack[_top].defaultNsState = (declared ? NamespaceState.DeclaredAndWrittenOut : NamespaceState.DeclaredButNotWrittenOut);
            }
            else
            {
                if (prefix.Length != 0 && ns.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_PrefixForEmptyNs);
                }

                int existingNsIndex = LookupNamespace(prefix);
                if (existingNsIndex != -1 && _nsStack[existingNsIndex].ns == ns)
                {
                    // it is already in scope.
                    if (declared)
                    {
                        _nsStack[existingNsIndex].declared = true;
                    }
                }
                else
                {
                    // see if prefix conflicts for the current element
                    if (declared)
                    {
                        if (existingNsIndex != -1 && existingNsIndex > _stack[_top].prevNsTop)
                        {
                            _nsStack[existingNsIndex].declared = true; // old one is silenced now
                        }
                    }

                    AddNamespace(prefix, ns, declared);
                }
            }
        }

        private void AddNamespace(string prefix, string ns, bool declared)
        {
            int nsIndex = ++_nsTop;
            if (nsIndex == _nsStack.Length)
            {
                Namespace[] newStack = new Namespace[nsIndex * 2];
                Array.Copy(_nsStack, newStack, nsIndex);
                _nsStack = newStack;
            }
            _nsStack[nsIndex].Set(prefix, ns, declared);

            if (_useNsHashtable)
            {
                AddToNamespaceHashtable(nsIndex);
            }
            else if (nsIndex == MaxNamespacesWalkCount)
            {
                // add all
                _nsHashtable = new Dictionary<string, int>();
                _useNsHashtable = true;

                for (int i = 0; i <= nsIndex; i++)
                {
                    AddToNamespaceHashtable(i);
                }
            }
        }

        private void AddToNamespaceHashtable(int namespaceIndex)
        {
            Debug.Assert(_useNsHashtable);
            Debug.Assert(_nsHashtable != null);

            string prefix = _nsStack[namespaceIndex].prefix;
            int existingNsIndex;

            if (_nsHashtable.TryGetValue(prefix, out existingNsIndex))
            {
                _nsStack[namespaceIndex].prevNsIndex = existingNsIndex;
            }

            _nsHashtable[prefix] = namespaceIndex;
        }

        private void PopNamespaces(int indexFrom, int indexTo)
        {
            Debug.Assert(_useNsHashtable);
            Debug.Assert(_nsHashtable != null);

            for (int i = indexTo; i >= indexFrom; i--)
            {
                Debug.Assert(_nsHashtable.ContainsKey(_nsStack[i].prefix));
                if (_nsStack[i].prevNsIndex == -1)
                {
                    _nsHashtable.Remove(_nsStack[i].prefix);
                }
                else
                {
                    _nsHashtable[_nsStack[i].prefix] = _nsStack[i].prevNsIndex;
                }
            }
        }

        private string GeneratePrefix()
        {
            int temp = _stack[_top].prefixCount++ + 1;
            return string.Create(CultureInfo.InvariantCulture, $"d{_top:d}p{temp:d}");
        }

        private void InternalWriteProcessingInstruction(string name, string? text)
        {
            _textWriter.Write("<?");
            ValidateName(name, false);
            _textWriter.Write(name);
            _textWriter.Write(' ');

            if (null != text)
            {
                _xmlEncoder.WriteRawWithSurrogateChecking(text);
            }

            _textWriter.Write("?>");
        }

        private int LookupNamespace(string prefix)
        {
            if (_useNsHashtable)
            {
                Debug.Assert(_nsHashtable != null);
                int nsIndex;
                if (_nsHashtable.TryGetValue(prefix, out nsIndex))
                {
                    return nsIndex;
                }
            }
            else
            {
                for (int i = _nsTop; i >= 0; i--)
                {
                    if (_nsStack[i].prefix == prefix)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private int LookupNamespaceInCurrentScope(string prefix)
        {
            if (_useNsHashtable)
            {
                Debug.Assert(_nsHashtable != null);
                int nsIndex;

                if (_nsHashtable.TryGetValue(prefix, out nsIndex))
                {
                    if (nsIndex > _stack[_top].prevNsTop)
                    {
                        return nsIndex;
                    }
                }
            }
            else
            {
                for (int i = _nsTop; i > _stack[_top].prevNsTop; i--)
                {
                    if (_nsStack[i].prefix == prefix)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private string? FindPrefix(string ns)
        {
            for (int i = _nsTop; i >= 0; i--)
            {
                if (_nsStack[i].ns == ns)
                {
                    if (LookupNamespace(_nsStack[i].prefix) == i)
                    {
                        return _nsStack[i].prefix;
                    }
                }
            }

            return null;
        }

        // There are three kind of strings we write out - Name, LocalName and Prefix.
        // Both LocalName and Prefix can be represented with NCName == false and Name
        // can be represented as NCName == true

        private void InternalWriteName(string name, bool isNCName)
        {
            ValidateName(name, isNCName);
            _textWriter.Write(name);
        }

        // This method is used for validation of the DOCTYPE, processing instruction and entity names plus names
        // written out by the user via WriteName and WriteQualifiedName.
        // Unfortunatelly the names of elements and attributes are not validated by the XmlTextWriter.
        // Also this method does not check wheather the character after ':' is a valid start name character. It accepts
        // all valid name characters at that position. This can't be changed because of backwards compatibility.
        private void ValidateName(string name, bool isNCName)
        {
            if (name == null || name.Length == 0)
            {
                throw new ArgumentException(SR.Xml_EmptyName);
            }

            int nameLength = name.Length;

            // Namespaces supported
            if (_namespaces)
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

        private void HandleSpecialAttribute()
        {
            string value = _xmlEncoder.AttributeValue;
            switch (_specialAttr)
            {
                case SpecialAttr.XmlLang:
                    _stack[_top].xmlLang = value;
                    break;
                case SpecialAttr.XmlSpace:
                    // validate XmlSpace attribute
                    value = XmlConvert.TrimString(value);
                    if (value == "default")
                    {
                        _stack[_top].xmlSpace = XmlSpace.Default;
                    }
                    else if (value == "preserve")
                    {
                        _stack[_top].xmlSpace = XmlSpace.Preserve;
                    }
                    else
                    {
                        throw new ArgumentException(SR.Format(SR.Xml_InvalidXmlSpace, value));
                    }
                    break;
                case SpecialAttr.XmlNs:
                    VerifyPrefixXml(_prefixForXmlNs, value);
                    PushNamespace(_prefixForXmlNs, value, true);
                    break;
            }
        }


        private static void VerifyPrefixXml(string? prefix, string ns)
        {
            if (prefix != null && prefix.Length == 3)
            {
                if (
                   (prefix[0] == 'x' || prefix[0] == 'X') &&
                   (prefix[1] == 'm' || prefix[1] == 'M') &&
                   (prefix[2] == 'l' || prefix[2] == 'L')
                   )
                {
                    if (XmlReservedNs.NsXml != ns)
                    {
                        throw new ArgumentException(SR.Xml_InvalidPrefix);
                    }
                }
            }
        }

        private void PushStack()
        {
            if (_top == _stack.Length - 1)
            {
                TagInfo[] na = new TagInfo[_stack.Length + 10];
                if (_top > 0) Array.Copy(_stack, na, _top + 1);
                _stack = na;
            }

            _top++; // Move up stack
            _stack[_top].Init(_nsTop);
        }

        private void FlushEncoders()
        {
            if (null != _base64Encoder)
            {
                // The Flush will call WriteRaw to write out the rest of the encoded characters
                _base64Encoder.Flush();
            }
            _flush = false;
        }
    }
}
