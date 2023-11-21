// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Xml
{
    internal sealed partial class XmlWellFormedWriter : XmlWriter
    {
        //
        // Private types used by the XmlWellFormedWriter are defined in XmlWellFormedWriterHelpers.cs
        //

        //
        // Fields
        //
        // underlying writer
        private readonly XmlWriter _writer;
        private readonly XmlRawWriter? _rawWriter;  // writer as XmlRawWriter
        private readonly IXmlNamespaceResolver? _predefinedNamespaces; // writer as IXmlNamespaceResolver

        // namespace management
        private Namespace[] _nsStack;
        private int _nsTop;
        private Dictionary<string, int>? _nsHashtable;
        private bool _useNsHashtable;

        // element scoping
        private ElementScope[] _elemScopeStack;
        private int _elemTop;

        // attribute tracking
        private AttrName[] _attrStack;
        private int _attrCount;
        private Dictionary<string, int>? _attrHashTable;

        // special attribute caching (xmlns, xml:space, xml:lang)
        private SpecialAttribute _specAttr = SpecialAttribute.No;
        private AttributeValueCache? _attrValueCache;
        private string? _curDeclPrefix;

        // state machine
        private State[] _stateTable;
        private State _currentState;

        // settings
        private readonly bool _checkCharacters;
        private readonly bool _omitDuplNamespaces;
        private readonly bool _writeEndDocumentOnClose;

        // actual conformance level
        private ConformanceLevel _conformanceLevel;

        // flags
        private bool _dtdWritten;
        private bool _xmlDeclFollows;

        //
        // Constants
        //
        private const int ElementStackInitialSize = 8;
        private const int NamespaceStackInitialSize = 8;
        private const int AttributeArrayInitialSize = 8;
#if DEBUG
        private const int MaxAttrDuplWalkCount = 2;
        private const int MaxNamespacesWalkCount = 3;
#else
        private const int MaxAttrDuplWalkCount = 14;
        private const int MaxNamespacesWalkCount = 16;
#endif

        //
        // State tables
        //
        private enum State
        {
            Start = 0,
            TopLevel = 1,
            Document = 2,
            Element = 3,
            Content = 4,
            B64Content = 5,
            B64Attribute = 6,
            AfterRootEle = 7,
            Attribute = 8,
            SpecialAttr = 9,
            EndDocument = 10,
            RootLevelAttr = 11,
            RootLevelSpecAttr = 12,
            RootLevelB64Attr = 13,
            AfterRootLevelAttr = 14,
            Closed = 15,
            Error = 16,

            StartContent = 101,
            StartContentEle = 102,
            StartContentB64 = 103,
            StartDoc = 104,
            StartDocEle = 106,
            EndAttrSEle = 107,
            EndAttrEEle = 108,
            EndAttrSCont = 109,
            EndAttrSAttr = 111,
            PostB64Cont = 112,
            PostB64Attr = 113,
            PostB64RootAttr = 114,
            StartFragEle = 115,
            StartFragCont = 116,
            StartFragB64 = 117,
            StartRootLevelAttr = 118,
        }

        private enum Token
        {
            StartDocument,
            EndDocument,
            PI,
            Comment,
            Dtd,
            StartElement,
            EndElement,
            StartAttribute,
            EndAttribute,
            Text,
            CData,
            AtomicValue,
            Base64,
            RawData,
            Whitespace,
        }

        internal static readonly string[] stateName = {
            "Start",                     // State.Start
            "TopLevel",                  // State.TopLevel
            "Document",                  // State.Document
            "Element Start Tag",         // State.Element
            "Element Content",           // State.Content
            "Element Content",           // State.B64Content
            "Attribute",                 // State.B64Attribute
            "EndRootElement",            // State.AfterRootEle
            "Attribute",                 // State.Attribute
            "Special Attribute",         // State.SpecialAttr
            "End Document",              // State.EndDocument
            "Root Level Attribute Value",           // State.RootLevelAttr
            "Root Level Special Attribute Value",   // State.RootLevelSpecAttr
            "Root Level Base64 Attribute Value",    // State.RootLevelB64Attr
            "After Root Level Attribute",           // State.AfterRootLevelAttr
            "Closed",                    // State.Closed
            "Error",                     // State.Error
        };

        internal static readonly string[] tokenName = {
            "StartDocument",            // Token.StartDocument
            "EndDocument",              // Token.EndDocument
            "PI",                       // Token.PI
            "Comment",                  // Token.Comment
            "DTD",                      // Token.Dtd
            "StartElement",             // Token.StartElement
            "EndElement",               // Token.EndElement
            "StartAttribute",           // Token.StartAttribut
            "EndAttribute",             // Token.EndAttribute
            "Text",                     // Token.Text
            "CDATA",                    // Token.CData
            "Atomic value",             // Token.AtomicValue
            "Base64",                   // Token.Base64
            "RawData",                  // Token.RawData
            "Whitespace",               // Token.Whitespace
        };

        private static readonly WriteState[] s_state2WriteState = {
            WriteState.Start,       // State.Start
            WriteState.Prolog,      // State.TopLevel
            WriteState.Prolog,      // State.Document
            WriteState.Element,     // State.Element
            WriteState.Content,     // State.Content
            WriteState.Content,     // State.B64Content
            WriteState.Attribute,   // State.B64Attribute
            WriteState.Content,     // State.AfterRootEle
            WriteState.Attribute,   // State.Attribute
            WriteState.Attribute,   // State.SpecialAttr
            WriteState.Content,     // State.EndDocument
            WriteState.Attribute,   // State.RootLevelAttr
            WriteState.Attribute,   // State.RootLevelSpecAttr
            WriteState.Attribute,   // State.RootLevelB64Attr
            WriteState.Attribute,   // State.AfterRootLevelAttr
            WriteState.Closed,      // State.Closed
            WriteState.Error,       // State.Error
        };

        private static readonly State[] s_stateTableDocument = {
    //                         State.Start           State.TopLevel   State.Document     State.Element          State.Content     State.B64Content      State.B64Attribute   State.AfterRootEle    State.Attribute,      State.SpecialAttr,   State.EndDocument,  State.RootLevelAttr,      State.RootLevelSpecAttr,  State.RootLevelB64Attr   State.AfterRootLevelAttr, // 16
    /* Token.StartDocument  */ State.Document,       State.Error,     State.Error,       State.Error,           State.Error,      State.PostB64Cont,    State.Error,         State.Error,          State.Error,          State.Error,         State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.EndDocument    */ State.Error,          State.Error,     State.Error,       State.Error,           State.Error,      State.PostB64Cont,    State.Error,         State.EndDocument,    State.Error,          State.Error,         State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.PI             */ State.StartDoc,       State.TopLevel,  State.Document,    State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.AfterRootEle,   State.EndAttrSCont,   State.EndAttrSCont,  State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.Comment        */ State.StartDoc,       State.TopLevel,  State.Document,    State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.AfterRootEle,   State.EndAttrSCont,   State.EndAttrSCont,  State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.Dtd            */ State.StartDoc,       State.TopLevel,  State.Document,    State.Error,           State.Error,      State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.Error,          State.Error,         State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.StartElement   */ State.StartDocEle,    State.Element,   State.Element,     State.StartContentEle, State.Element,    State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.EndAttrSEle,    State.EndAttrSEle,   State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.EndElement     */ State.Error,          State.Error,     State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.EndAttrEEle,    State.EndAttrEEle,   State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.StartAttribute */ State.Error,          State.Error,     State.Error,       State.Attribute,       State.Error,      State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.EndAttrSAttr,   State.EndAttrSAttr,  State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.EndAttribute   */ State.Error,          State.Error,     State.Error,       State.Error,           State.Error,      State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.Element,        State.Element,       State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.Text           */ State.Error,          State.Error,     State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.Attribute,      State.SpecialAttr,   State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.CData          */ State.Error,          State.Error,     State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.EndAttrSCont,   State.EndAttrSCont,  State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.AtomicValue    */ State.Error,          State.Error,     State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.Attribute,      State.Error,         State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.Base64         */ State.Error,          State.Error,     State.Error,       State.StartContentB64, State.B64Content, State.B64Content,     State.B64Attribute,  State.Error,          State.B64Attribute,   State.Error,         State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.RawData        */ State.StartDoc,       State.Error,     State.Document,    State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.AfterRootEle,   State.Attribute,      State.SpecialAttr,   State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error,
    /* Token.Whitespace     */ State.StartDoc,       State.TopLevel,  State.Document,    State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.AfterRootEle,   State.Attribute,      State.SpecialAttr,   State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error
        };

        private static readonly State[] s_stateTableAuto = {
    //                         State.Start           State.TopLevel       State.Document     State.Element          State.Content     State.B64Content      State.B64Attribute   State.AfterRootEle    State.Attribute,      State.SpecialAttr,   State.EndDocument,  State.RootLevelAttr,      State.RootLevelSpecAttr,  State.RootLevelB64Attr,  State.AfterRootLevelAttr  // 16
    /* Token.StartDocument  */ State.Document,       State.Error,         State.Error,       State.Error,           State.Error,      State.PostB64Cont,    State.Error,         State.Error,          State.Error,          State.Error,         State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error, /* Token.StartDocument  */
    /* Token.EndDocument    */ State.Error,          State.Error,         State.Error,       State.Error,           State.Error,      State.PostB64Cont,    State.Error,         State.EndDocument,    State.Error,          State.Error,         State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error, /* Token.EndDocument    */
    /* Token.PI             */ State.TopLevel,       State.TopLevel,      State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.AfterRootEle,   State.EndAttrSCont,   State.EndAttrSCont,  State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error, /* Token.PI             */
    /* Token.Comment        */ State.TopLevel,       State.TopLevel,      State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.AfterRootEle,   State.EndAttrSCont,   State.EndAttrSCont,  State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error, /* Token.Comment        */
    /* Token.Dtd            */ State.StartDoc,       State.TopLevel,      State.Error,       State.Error,           State.Error,      State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.Error,          State.Error,         State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error, /* Token.Dtd            */
    /* Token.StartElement   */ State.StartFragEle,   State.Element,       State.Error,       State.StartContentEle, State.Element,    State.PostB64Cont,    State.PostB64Attr,   State.Element,        State.EndAttrSEle,    State.EndAttrSEle,   State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error, /* Token.StartElement   */
    /* Token.EndElement     */ State.Error,          State.Error,         State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.EndAttrEEle,    State.EndAttrEEle,   State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error, /* Token.EndElement     */
    /* Token.StartAttribute */ State.RootLevelAttr,  State.Error,         State.Error,       State.Attribute,       State.Error,      State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.EndAttrSAttr,   State.EndAttrSAttr,  State.Error,        State.StartRootLevelAttr, State.StartRootLevelAttr, State.PostB64RootAttr,   State.RootLevelAttr,      State.Error, /* Token.StartAttribute */
    /* Token.EndAttribute   */ State.Error,          State.Error,         State.Error,       State.Error,           State.Error,      State.PostB64Cont,    State.PostB64Attr,   State.Error,          State.Element,        State.Element,       State.Error,        State.AfterRootLevelAttr, State.AfterRootLevelAttr, State.PostB64RootAttr,   State.Error,              State.Error, /* Token.EndAttribute   */
    /* Token.Text           */ State.StartFragCont,  State.StartFragCont, State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.Content,        State.Attribute,      State.SpecialAttr,   State.Error,        State.RootLevelAttr,      State.RootLevelSpecAttr,  State.PostB64RootAttr,   State.Error,              State.Error, /* Token.Text           */
    /* Token.CData          */ State.StartFragCont,  State.StartFragCont, State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.Content,        State.EndAttrSCont,   State.EndAttrSCont,  State.Error,        State.Error,              State.Error,              State.Error,             State.Error,              State.Error, /* Token.CData          */
    /* Token.AtomicValue    */ State.StartFragCont,  State.StartFragCont, State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.Content,        State.Attribute,      State.Error,         State.Error,        State.RootLevelAttr,      State.Error,              State.PostB64RootAttr,   State.Error,              State.Error, /* Token.AtomicValue    */
    /* Token.Base64         */ State.StartFragB64,   State.StartFragB64,  State.Error,       State.StartContentB64, State.B64Content, State.B64Content,     State.B64Attribute,  State.B64Content,     State.B64Attribute,   State.Error,         State.Error,        State.RootLevelB64Attr,   State.Error,              State.RootLevelB64Attr,  State.Error,              State.Error, /* Token.Base64         */
    /* Token.RawData        */ State.StartFragCont,  State.TopLevel,      State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.Content,        State.Attribute,      State.SpecialAttr,   State.Error,        State.RootLevelAttr,      State.RootLevelSpecAttr,  State.PostB64RootAttr,   State.AfterRootLevelAttr, State.Error, /* Token.RawData        */
    /* Token.Whitespace     */ State.TopLevel,       State.TopLevel,      State.Error,       State.StartContent,    State.Content,    State.PostB64Cont,    State.PostB64Attr,   State.AfterRootEle,   State.Attribute,      State.SpecialAttr,   State.Error,        State.RootLevelAttr,      State.RootLevelSpecAttr,  State.PostB64RootAttr,   State.AfterRootLevelAttr, State.Error, /* Token.Whitespace     */
        };

        //
        // Constructor & finalizer
        //
        internal XmlWellFormedWriter(XmlWriter writer, XmlWriterSettings settings)
        {
            Debug.Assert(writer != null);
            Debug.Assert(settings != null);
            Debug.Assert(MaxNamespacesWalkCount <= 3);

            _writer = writer;

            _rawWriter = writer as XmlRawWriter;
            _predefinedNamespaces = writer as IXmlNamespaceResolver;
            if (_rawWriter != null)
            {
                _rawWriter.NamespaceResolver = new NamespaceResolverProxy(this);
            }

            _checkCharacters = settings.CheckCharacters;
            _omitDuplNamespaces = (settings.NamespaceHandling & NamespaceHandling.OmitDuplicates) != 0;
            _writeEndDocumentOnClose = settings.WriteEndDocumentOnClose;

            _conformanceLevel = settings.ConformanceLevel;
            _stateTable = (_conformanceLevel == ConformanceLevel.Document) ? s_stateTableDocument : s_stateTableAuto;

            _currentState = State.Start;

            _nsStack = new Namespace[NamespaceStackInitialSize];
            _nsStack[0].Set("xmlns", XmlReservedNs.NsXmlNs, NamespaceKind.Special);
            _nsStack[1].Set("xml", XmlReservedNs.NsXml, NamespaceKind.Special);
            if (_predefinedNamespaces == null)
            {
                _nsStack[2].Set(string.Empty, string.Empty, NamespaceKind.Implied);
            }
            else
            {
                string? defaultNs = _predefinedNamespaces.LookupNamespace(string.Empty);
                _nsStack[2].Set(string.Empty, defaultNs ?? string.Empty, NamespaceKind.Implied);
            }
            _nsTop = 2;

            _elemScopeStack = new ElementScope[ElementStackInitialSize];
            _elemScopeStack[0].Set(string.Empty, string.Empty, string.Empty, _nsTop);
            _elemScopeStack[0].xmlSpace = XmlSpace.None;
            _elemScopeStack[0].xmlLang = null;
            _elemTop = 0;

            _attrStack = new AttrName[AttributeArrayInitialSize];
        }

        //
        // XmlWriter implementation
        //
        public override WriteState WriteState
        {
            get
            {
                if ((int)_currentState <= (int)State.Error)
                {
                    return s_state2WriteState[(int)_currentState];
                }
                else
                {
                    Debug.Fail("Expected currentState <= State.Error ");
                    return WriteState.Error;
                }
            }
        }

        public override XmlWriterSettings Settings
        {
            get
            {
                XmlWriterSettings settings = _writer.Settings!;
                settings.ReadOnly = false;
                settings.ConformanceLevel = _conformanceLevel;
                if (_omitDuplNamespaces)
                {
                    settings.NamespaceHandling |= NamespaceHandling.OmitDuplicates;
                }
                settings.WriteEndDocumentOnClose = _writeEndDocumentOnClose;
                settings.ReadOnly = true;
                return settings;
            }
        }

        public override void WriteStartDocument()
        {
            WriteStartDocumentImpl(XmlStandalone.Omit);
        }

        public override void WriteStartDocument(bool standalone)
        {
            WriteStartDocumentImpl(standalone ? XmlStandalone.Yes : XmlStandalone.No);
        }

        public override void WriteEndDocument()
        {
            try
            {
                // auto-close all elements
                while (_elemTop > 0)
                {
                    WriteEndElement();
                }
                State prevState = _currentState;
                AdvanceState(Token.EndDocument);

                if (prevState != State.AfterRootEle)
                {
                    throw new ArgumentException(SR.Xml_NoRoot);
                }
                if (_rawWriter == null)
                {
                    _writer.WriteEndDocument();
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(name);
                XmlConvert.VerifyQName(name, ExceptionType.XmlException);

                if (_conformanceLevel == ConformanceLevel.Fragment)
                {
                    throw new InvalidOperationException(SR.Xml_DtdNotAllowedInFragment);
                }

                AdvanceState(Token.Dtd);
                if (_dtdWritten)
                {
                    _currentState = State.Error;
                    throw new InvalidOperationException(SR.Xml_DtdAlreadyWritten);
                }

                if (_conformanceLevel == ConformanceLevel.Auto)
                {
                    _conformanceLevel = ConformanceLevel.Document;
                    _stateTable = s_stateTableDocument;
                }

                int i;

                // check characters
                if (_checkCharacters)
                {
                    if (pubid != null)
                    {
                        if ((i = XmlCharType.IsPublicId(pubid)) >= 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(pubid, i)), nameof(pubid));
                        }
                    }
                    if (sysid != null)
                    {
                        if ((i = XmlCharType.IsOnlyCharData(sysid)) >= 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(sysid, i)), nameof(sysid));
                        }
                    }
                    if (subset != null)
                    {
                        if ((i = XmlCharType.IsOnlyCharData(subset)) >= 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(subset, i)), nameof(subset));
                        }
                    }
                }

                // write doctype
                _writer.WriteDocType(name, pubid, sysid, subset);
                _dtdWritten = true;
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            try
            {
                // check local name
                ArgumentException.ThrowIfNullOrEmpty(localName);
                CheckNCName(localName);

                AdvanceState(Token.StartElement);

                // lookup prefix / namespace
                if (prefix == null)
                {
                    if (ns != null)
                    {
                        prefix = LookupPrefix(ns);
                    }
                    prefix ??= string.Empty;
                }
                else if (prefix.Length > 0)
                {
                    CheckNCName(prefix);
                    ns ??= LookupNamespace(prefix);
                    if (ns == null || (ns != null && ns.Length == 0))
                    {
                        throw new ArgumentException(SR.Xml_PrefixForEmptyNs);
                    }
                }
                if (ns == null)
                {
                    ns = LookupNamespace(prefix);
                    if (ns == null)
                    {
                        Debug.Assert(prefix.Length == 0);
                        ns = string.Empty;
                    }
                }

                if (_elemTop == 0 && _rawWriter != null)
                {
                    // notify the underlying raw writer about the root level element
                    _rawWriter.OnRootElement(_conformanceLevel);
                }

                // write start tag
                _writer.WriteStartElement(prefix, localName, ns);

                // push element on stack and add/check namespace
                int top = ++_elemTop;
                if (top == _elemScopeStack.Length)
                {
                    ElementScope[] newStack = new ElementScope[top * 2];
                    Array.Copy(_elemScopeStack, newStack, top);
                    _elemScopeStack = newStack;
                }
                _elemScopeStack[top].Set(prefix, localName, ns, _nsTop);

                PushNamespaceImplicit(prefix, ns);

                if (_attrCount >= MaxAttrDuplWalkCount)
                {
                    Debug.Assert(_attrHashTable != null);
                    _attrHashTable.Clear();
                }
                _attrCount = 0;
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }


        public override void WriteEndElement()
        {
            try
            {
                AdvanceState(Token.EndElement);

                int top = _elemTop;
                if (top == 0)
                {
                    throw new XmlException(SR.Xml_NoStartTag, string.Empty);
                }

                // write end tag
                if (_rawWriter != null)
                {
                    _elemScopeStack[top].WriteEndElement(_rawWriter);
                }
                else
                {
                    _writer.WriteEndElement();
                }

                // pop namespaces
                int prevNsTop = _elemScopeStack[top].prevNSTop;
                if (_useNsHashtable && prevNsTop < _nsTop)
                {
                    PopNamespaces(prevNsTop + 1, _nsTop);
                }
                _nsTop = prevNsTop;
                _elemTop = --top;

                // check "one root element" condition for ConformanceLevel.Document
                if (top == 0)
                {
                    if (_conformanceLevel == ConformanceLevel.Document)
                    {
                        _currentState = State.AfterRootEle;
                    }
                    else
                    {
                        _currentState = State.TopLevel;
                    }
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteFullEndElement()
        {
            try
            {
                AdvanceState(Token.EndElement);

                int top = _elemTop;
                if (top == 0)
                {
                    throw new XmlException(SR.Xml_NoStartTag, string.Empty);
                }

                // write end tag
                if (_rawWriter != null)
                {
                    _elemScopeStack[top].WriteFullEndElement(_rawWriter);
                }
                else
                {
                    _writer.WriteFullEndElement();
                }

                // pop namespaces
                int prevNsTop = _elemScopeStack[top].prevNSTop;
                if (_useNsHashtable && prevNsTop < _nsTop)
                {
                    PopNamespaces(prevNsTop + 1, _nsTop);
                }
                _nsTop = prevNsTop;
                _elemTop = --top;

                // check "one root element" condition for ConformanceLevel.Document
                if (top == 0)
                {
                    if (_conformanceLevel == ConformanceLevel.Document)
                    {
                        _currentState = State.AfterRootEle;
                    }
                    else
                    {
                        _currentState = State.TopLevel;
                    }
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteStartAttribute(string? prefix, string? localName, string? namespaceName)
        {
            try
            {
                // check local name
                if (string.IsNullOrEmpty(localName))
                {
                    if (prefix == "xmlns")
                    {
                        localName = "xmlns";
                        prefix = string.Empty;
                    }
                    else
                    {
                        throw new ArgumentException(SR.Xml_EmptyLocalName);
                    }
                }

                CheckNCName(localName);

                AdvanceState(Token.StartAttribute);

                // lookup prefix / namespace
                if (prefix == null)
                {
                    if (namespaceName != null)
                    {
                        // special case prefix=null/localname=xmlns
                        if (!(localName == "xmlns" && namespaceName == XmlReservedNs.NsXmlNs))
                            prefix = LookupPrefix(namespaceName);
                    }

                    prefix ??= string.Empty;
                }
                if (namespaceName == null)
                {
                    if (prefix.Length > 0)
                    {
                        namespaceName = LookupNamespace(prefix);
                    }
                    namespaceName ??= string.Empty;
                }

                if (prefix.Length == 0)
                {
                    if (localName[0] == 'x' && localName == "xmlns")
                    {
                        if (namespaceName.Length > 0 && namespaceName != XmlReservedNs.NsXmlNs)
                        {
                            throw new ArgumentException(SR.Xml_XmlnsPrefix);
                        }
                        _curDeclPrefix = string.Empty;
                        SetSpecialAttribute(SpecialAttribute.DefaultXmlns);
                        goto SkipPushAndWrite;
                    }
                    else if (namespaceName.Length > 0)
                    {
                        prefix = LookupPrefix(namespaceName);
                        if (string.IsNullOrEmpty(prefix))
                        {
                            prefix = GeneratePrefix();
                        }
                    }
                }
                else
                {
                    if (prefix[0] == 'x')
                    {
                        if (prefix == "xmlns")
                        {
                            if (namespaceName.Length > 0 && namespaceName != XmlReservedNs.NsXmlNs)
                            {
                                throw new ArgumentException(SR.Xml_XmlnsPrefix);
                            }
                            _curDeclPrefix = localName;
                            SetSpecialAttribute(SpecialAttribute.PrefixedXmlns);
                            goto SkipPushAndWrite;
                        }
                        else if (prefix == "xml")
                        {
                            if (namespaceName.Length > 0 && namespaceName != XmlReservedNs.NsXml)
                            {
                                throw new ArgumentException(SR.Xml_XmlPrefix);
                            }
                            switch (localName)
                            {
                                case "space":
                                    SetSpecialAttribute(SpecialAttribute.XmlSpace);
                                    goto SkipPushAndWrite;
                                case "lang":
                                    SetSpecialAttribute(SpecialAttribute.XmlLang);
                                    goto SkipPushAndWrite;
                            }
                        }
                    }

                    CheckNCName(prefix);

                    if (namespaceName.Length == 0)
                    {
                        // attributes cannot have default namespace
                        prefix = string.Empty;
                    }
                    else
                    {
                        string? definedNs = LookupLocalNamespace(prefix);
                        if (definedNs != null && definedNs != namespaceName)
                        {
                            prefix = GeneratePrefix();
                        }
                    }
                }

                if (prefix.Length != 0)
                {
                    PushNamespaceImplicit(prefix, namespaceName);
                }

            SkipPushAndWrite:

                // add attribute to the list and check for duplicates
                AddAttribute(prefix, localName, namespaceName);

                if (_specAttr == SpecialAttribute.No)
                {
                    // write attribute name
                    _writer.WriteStartAttribute(prefix, localName, namespaceName);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteEndAttribute()
        {
            try
            {
                AdvanceState(Token.EndAttribute);

                if (_specAttr != SpecialAttribute.No)
                {
                    Debug.Assert(_attrValueCache != null);
                    string value;

                    switch (_specAttr)
                    {
                        case SpecialAttribute.DefaultXmlns:
                            value = _attrValueCache.StringValue;
                            if (PushNamespaceExplicit(string.Empty, value))
                            { // returns true if the namespace declaration should be written out
                                if (_rawWriter != null)
                                {
                                    if (_rawWriter.SupportsNamespaceDeclarationInChunks)
                                    {
                                        _rawWriter.WriteStartNamespaceDeclaration(string.Empty);
                                        _attrValueCache.Replay(_rawWriter);
                                        _rawWriter.WriteEndNamespaceDeclaration();
                                    }
                                    else
                                    {
                                        _rawWriter.WriteNamespaceDeclaration(string.Empty, value);
                                    }
                                }
                                else
                                {
                                    _writer.WriteStartAttribute(string.Empty, "xmlns", XmlReservedNs.NsXmlNs);
                                    _attrValueCache.Replay(_writer);
                                    _writer.WriteEndAttribute();
                                }
                            }
                            _curDeclPrefix = null;
                            break;
                        case SpecialAttribute.PrefixedXmlns:
                            value = _attrValueCache.StringValue;
                            if (value.Length == 0)
                            {
                                throw new ArgumentException(SR.Xml_PrefixForEmptyNs);
                            }
                            if (value == XmlReservedNs.NsXmlNs || (value == XmlReservedNs.NsXml && _curDeclPrefix != "xml"))
                            {
                                throw new ArgumentException(SR.Xml_CanNotBindToReservedNamespace);
                            }

                            Debug.Assert(_curDeclPrefix != null);

                            if (PushNamespaceExplicit(_curDeclPrefix, value))
                            { // returns true if the namespace declaration should be written out
                                if (_rawWriter != null)
                                {
                                    if (_rawWriter.SupportsNamespaceDeclarationInChunks)
                                    {
                                        _rawWriter.WriteStartNamespaceDeclaration(_curDeclPrefix);
                                        _attrValueCache.Replay(_rawWriter);
                                        _rawWriter.WriteEndNamespaceDeclaration();
                                    }
                                    else
                                    {
                                        _rawWriter.WriteNamespaceDeclaration(_curDeclPrefix, value);
                                    }
                                }
                                else
                                {
                                    _writer.WriteStartAttribute("xmlns", _curDeclPrefix, XmlReservedNs.NsXmlNs);
                                    _attrValueCache.Replay(_writer);
                                    _writer.WriteEndAttribute();
                                }
                            }
                            _curDeclPrefix = null;
                            break;
                        case SpecialAttribute.XmlSpace:
                            _attrValueCache.Trim();
                            value = _attrValueCache.StringValue;

                            if (value == "default")
                            {
                                _elemScopeStack[_elemTop].xmlSpace = XmlSpace.Default;
                            }
                            else if (value == "preserve")
                            {
                                _elemScopeStack[_elemTop].xmlSpace = XmlSpace.Preserve;
                            }
                            else
                            {
                                throw new ArgumentException(SR.Format(SR.Xml_InvalidXmlSpace, value));
                            }
                            _writer.WriteStartAttribute("xml", "space", XmlReservedNs.NsXml);
                            _attrValueCache.Replay(_writer);
                            _writer.WriteEndAttribute();
                            break;
                        case SpecialAttribute.XmlLang:
                            value = _attrValueCache.StringValue;
                            _elemScopeStack[_elemTop].xmlLang = value;
                            _writer.WriteStartAttribute("xml", "lang", XmlReservedNs.NsXml);
                            _attrValueCache.Replay(_writer);
                            _writer.WriteEndAttribute();
                            break;
                    }
                    _specAttr = SpecialAttribute.No;
                    _attrValueCache.Clear();
                }
                else
                {
                    _writer.WriteEndAttribute();
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteCData(string? text)
        {
            try
            {
                text ??= string.Empty;
                AdvanceState(Token.CData);
                _writer.WriteCData(text);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteComment(string? text)
        {
            try
            {
                text ??= string.Empty;
                AdvanceState(Token.Comment);
                _writer.WriteComment(text);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            try
            {
                // check name
                ArgumentException.ThrowIfNullOrEmpty(name);
                CheckNCName(name);

                // check text
                text ??= string.Empty;

                // xml declaration is a special case (not a processing instruction, but we allow WriteProcessingInstruction as a convenience)
                if (name.Length == 3 && string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase))
                {
                    if (_currentState != State.Start)
                    {
                        throw new ArgumentException(_conformanceLevel == ConformanceLevel.Document ? SR.Xml_DupXmlDecl : SR.Xml_CannotWriteXmlDecl);
                    }

                    _xmlDeclFollows = true;
                    AdvanceState(Token.PI);

                    if (_rawWriter != null)
                    {
                        // Translate PI into an xml declaration
                        _rawWriter.WriteXmlDeclaration(text);
                    }
                    else
                    {
                        _writer.WriteProcessingInstruction(name, text);
                    }
                }
                else
                {
                    AdvanceState(Token.PI);
                    _writer.WriteProcessingInstruction(name, text);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteEntityRef(string name)
        {
            try
            {
                // check name
                ArgumentException.ThrowIfNullOrEmpty(name);
                CheckNCName(name);

                AdvanceState(Token.Text);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteEntityRef(name);
                }
                else
                {
                    _writer.WriteEntityRef(name);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteCharEntity(char ch)
        {
            try
            {
                if (char.IsSurrogate(ch))
                {
                    throw new ArgumentException(SR.Xml_InvalidSurrogateMissingLowChar);
                }

                AdvanceState(Token.Text);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteCharEntity(ch);
                }
                else
                {
                    _writer.WriteCharEntity(ch);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            try
            {
                if (!char.IsSurrogatePair(highChar, lowChar))
                {
                    throw XmlConvert.CreateInvalidSurrogatePairException(lowChar, highChar);
                }

                AdvanceState(Token.Text);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteSurrogateCharEntity(lowChar, highChar);
                }
                else
                {
                    _writer.WriteSurrogateCharEntity(lowChar, highChar);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteWhitespace(string? ws)
        {
            try
            {
                ws ??= string.Empty;
                if (!XmlCharType.IsOnlyWhitespace(ws))
                {
                    throw new ArgumentException(SR.Xml_NonWhitespace);
                }

                AdvanceState(Token.Whitespace);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteWhitespace(ws);
                }
                else
                {
                    _writer.WriteWhitespace(ws);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteString(string? text)
        {
            try
            {
                if (text == null)
                {
                    return;
                }

                AdvanceState(Token.Text);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteString(text);
                }
                else
                {
                    _writer.WriteString(text);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(buffer);
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                ArgumentOutOfRangeException.ThrowIfNegative(count);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - index);

                AdvanceState(Token.Text);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteChars(buffer, index, count);
                }
                else
                {
                    _writer.WriteChars(buffer, index, count);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(buffer);
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                ArgumentOutOfRangeException.ThrowIfNegative(count);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - index);

                AdvanceState(Token.RawData);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteRaw(buffer, index, count);
                }
                else
                {
                    _writer.WriteRaw(buffer, index, count);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteRaw(string data)
        {
            try
            {
                if (data == null)
                {
                    return;
                }

                AdvanceState(Token.RawData);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteRaw(data);
                }
                else
                {
                    _writer.WriteRaw(data);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(buffer);
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                ArgumentOutOfRangeException.ThrowIfNegative(count);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - index);

                AdvanceState(Token.Base64);
                _writer.WriteBase64(buffer, index, count);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void Close()
        {
            if (_currentState != State.Closed)
            {
                try
                {
                    if (_writeEndDocumentOnClose)
                    {
                        while (_currentState != State.Error && _elemTop > 0)
                        {
                            WriteEndElement();
                        }
                    }
                    else
                    {
                        if (_currentState != State.Error && _elemTop > 0)
                        {
                            //finish the start element tag '>'
                            try
                            {
                                AdvanceState(Token.EndElement);
                            }
                            catch
                            {
                                _currentState = State.Error;
                                throw;
                            }
                        }
                    }

                    if (InBase64 && _rawWriter != null)
                    {
                        _rawWriter.WriteEndBase64();
                    }

                    _writer.Flush();
                }
                finally
                {
                    try
                    {
                        if (_rawWriter != null)
                        {
                            _rawWriter.Close(WriteState);
                        }
                        else
                        {
                            _writer.Close();
                        }
                    }
                    finally
                    {
                        _currentState = State.Closed;
                    }
                }
            }
        }

        public override void Flush()
        {
            try
            {
                _writer.Flush();
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override string? LookupPrefix(string ns)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(ns);
                for (int i = _nsTop; i >= 0; i--)
                {
                    if (_nsStack[i].namespaceUri == ns)
                    {
                        string prefix = _nsStack[i].prefix;
                        for (i++; i <= _nsTop; i++)
                        {
                            if (_nsStack[i].prefix == prefix)
                            {
                                return null;
                            }
                        }
                        return prefix;
                    }
                }
                return _predefinedNamespaces?.LookupPrefix(ns);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                int i;
                for (i = _elemTop; i >= 0 && _elemScopeStack[i].xmlSpace == (System.Xml.XmlSpace)(int)-1; i--) ;
                Debug.Assert(i >= 0);
                return _elemScopeStack[i].xmlSpace;
            }
        }

        public override string? XmlLang
        {
            get
            {
                int i;
                for (i = _elemTop; i > 0 && _elemScopeStack[i].xmlLang == null; i--) ;
                Debug.Assert(i >= 0);
                return _elemScopeStack[i].xmlLang;
            }
        }

        public override void WriteQualifiedName(string localName, string? ns)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(localName);
                CheckNCName(localName);

                AdvanceState(Token.Text);
                string? prefix = string.Empty;
                if (!string.IsNullOrEmpty(ns))
                {
                    prefix = LookupPrefix(ns);
                    if (prefix == null)
                    {
                        if (_currentState != State.Attribute)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_UndefNamespace, ns));
                        }

                        prefix = GeneratePrefix();
                        PushNamespaceImplicit(prefix, ns);
                    }
                }
                // if this is a special attribute, then just convert this to text
                // otherwise delegate to raw-writer
                if (SaveAttrValue || _rawWriter == null)
                {
                    if (prefix.Length != 0)
                    {
                        WriteString(prefix);
                        WriteString(":");
                    }
                    WriteString(localName);
                }
                else
                {
                    _rawWriter.WriteQualifiedName(prefix, localName, ns);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(bool value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                _writer.WriteValue(value);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(DateTime value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                _writer.WriteValue(value);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(DateTimeOffset value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                _writer.WriteValue(value);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(double value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                _writer.WriteValue(value);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(float value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                _writer.WriteValue(value);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(decimal value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                _writer.WriteValue(value);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(int value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                _writer.WriteValue(value);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(long value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                _writer.WriteValue(value);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(string? value)
        {
            try
            {
                if (value == null)
                {
                    return;
                }
                if (SaveAttrValue)
                {
                    AdvanceState(Token.Text);
                    _attrValueCache!.WriteValue(value);
                }
                else
                {
                    AdvanceState(Token.AtomicValue);
                    _writer.WriteValue(value);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(object value)
        {
            try
            {
                if (SaveAttrValue && value is string)
                {
                    AdvanceState(Token.Text);
                    _attrValueCache!.WriteValue((string)value);
                }
                else
                {
                    AdvanceState(Token.AtomicValue);
                    _writer.WriteValue(value);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            if (IsClosedOrErrorState)
            {
                throw new InvalidOperationException(SR.Xml_ClosedOrError);
            }
            try
            {
                AdvanceState(Token.Text);
                base.WriteBinHex(buffer, index, count);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        internal XmlRawWriter? RawWriter
        {
            get
            {
                return _rawWriter;
            }
        }

        //
        // Private methods
        //
        private bool SaveAttrValue
        {
            get
            {
                return _specAttr != SpecialAttribute.No;
            }
        }

        private bool InBase64
        {
            get
            {
                return (_currentState == State.B64Content || _currentState == State.B64Attribute || _currentState == State.RootLevelB64Attr);
            }
        }

        private void SetSpecialAttribute(SpecialAttribute special)
        {
            _specAttr = special;
            if (State.Attribute == _currentState)
                _currentState = State.SpecialAttr;
            else if (State.RootLevelAttr == _currentState)
                _currentState = State.RootLevelSpecAttr;
            else
                Debug.Fail("State.Attribute == currentState || State.RootLevelAttr == currentState");

            _attrValueCache ??= new AttributeValueCache();
        }

        private void WriteStartDocumentImpl(XmlStandalone standalone)
        {
            try
            {
                AdvanceState(Token.StartDocument);

                if (_conformanceLevel == ConformanceLevel.Auto)
                {
                    _conformanceLevel = ConformanceLevel.Document;
                    _stateTable = s_stateTableDocument;
                }
                else if (_conformanceLevel == ConformanceLevel.Fragment)
                {
                    throw new InvalidOperationException(SR.Xml_CannotStartDocumentOnFragment);
                }

                if (_rawWriter != null)
                {
                    if (!_xmlDeclFollows)
                    {
                        _rawWriter.WriteXmlDeclaration(standalone);
                    }
                }
                else
                {
                    // We do not pass the standalone value here
                    _writer.WriteStartDocument();
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private void StartFragment()
        {
            _conformanceLevel = ConformanceLevel.Fragment;
            Debug.Assert(_stateTable == s_stateTableAuto);
        }

        // PushNamespaceImplicit is called when a prefix/namespace pair is used in an element name, attribute name or some other qualified name.
        private void PushNamespaceImplicit(string prefix, string ns)
        {
            NamespaceKind kind;

            // See if the prefix is already defined
            int existingNsIndex = LookupNamespaceIndex(prefix);

            // Prefix is already defined
            if (existingNsIndex != -1)
            {
                // It is defined in the current scope
                if (existingNsIndex > _elemScopeStack[_elemTop].prevNSTop)
                {
                    // The new namespace Uri needs to be the same as the one that is already declared
                    if (_nsStack[existingNsIndex].namespaceUri != ns)
                    {
                        throw new XmlException(SR.Xml_RedefinePrefix, new string[] { prefix, _nsStack[existingNsIndex].namespaceUri, ns });
                    }
                    // No additional work needed
                    return;
                }
                // The prefix is defined but in a different scope
                else
                {
                    // existing declaration is special one (xml, xmlns) -> validate that the new one is the same and can be declared
                    if (_nsStack[existingNsIndex].kind == NamespaceKind.Special)
                    {
                        if (prefix == "xml")
                        {
                            if (ns != _nsStack[existingNsIndex].namespaceUri)
                            {
                                throw new ArgumentException(SR.Xml_XmlPrefix);
                            }
                            else
                            {
                                kind = NamespaceKind.Implied;
                            }
                        }
                        else
                        {
                            Debug.Assert(prefix == "xmlns");
                            throw new ArgumentException(SR.Xml_XmlnsPrefix);
                        }
                    }
                    // regular namespace declaration -> compare the namespace Uris to decide if the prefix is redefined
                    else
                    {
                        kind = (_nsStack[existingNsIndex].namespaceUri == ns) ? NamespaceKind.Implied : NamespaceKind.NeedToWrite;
                    }
                }
            }
            // No existing declaration found in the namespace stack
            else
            {
                // validate special declaration (xml, xmlns)
                if ((ns == XmlReservedNs.NsXml && prefix != "xml") ||
                     (ns == XmlReservedNs.NsXmlNs && prefix != "xmlns"))
                {
                    throw new ArgumentException(SR.Format(SR.Xml_NamespaceDeclXmlXmlns, prefix));
                }

                // check if it can be found in the predefinedNamespaces (which are provided by the user)
                if (_predefinedNamespaces != null)
                {
                    string? definedNs = _predefinedNamespaces.LookupNamespace(prefix);
                    // compare the namespace Uri to decide if the prefix is redefined
                    kind = (definedNs == ns) ? NamespaceKind.Implied : NamespaceKind.NeedToWrite;
                }
                else
                {
                    // Namespace not declared anywhere yet, we need to write it out
                    kind = NamespaceKind.NeedToWrite;
                }
            }

            AddNamespace(prefix, ns, kind);
        }

        // PushNamespaceExplicit is called when a namespace declaration is written out;
        // It returns true if the namespace declaration should we written out, false if it should be omitted (if OmitDuplicateNamespaceDeclarations is true)
        private bool PushNamespaceExplicit(string prefix, string ns)
        {
            bool writeItOut = true;

            // See if the prefix is already defined
            int existingNsIndex = LookupNamespaceIndex(prefix);

            // Existing declaration in the current scope
            if (existingNsIndex != -1)
            {
                // It is defined in the current scope
                if (existingNsIndex > _elemScopeStack[_elemTop].prevNSTop)
                {
                    // The new namespace Uri needs to be the same as the one that is already declared
                    if (_nsStack[existingNsIndex].namespaceUri != ns)
                    {
                        throw new XmlException(SR.Xml_RedefinePrefix, new string?[] { prefix, _nsStack[existingNsIndex].namespaceUri, ns });
                    }
                    // Check for duplicate declarations
                    NamespaceKind existingNsKind = _nsStack[existingNsIndex].kind;
                    if (existingNsKind == NamespaceKind.Written)
                    {
                        throw DupAttrException((prefix.Length == 0) ? string.Empty : "xmlns", (prefix.Length == 0) ? "xmlns" : prefix);
                    }
                    // Check if it can be omitted
                    if (_omitDuplNamespaces && existingNsKind != NamespaceKind.NeedToWrite)
                    {
                        writeItOut = false;
                    }
                    _nsStack[existingNsIndex].kind = NamespaceKind.Written;
                    // No additional work needed
                    return writeItOut;
                }
                // The prefix is defined but in a different scope
                else
                {
                    // check if is the same and can be omitted
                    if (_nsStack[existingNsIndex].namespaceUri == ns && _omitDuplNamespaces)
                    {
                        writeItOut = false;
                    }
                }
            }
            // No existing declaration found in the namespace stack
            else
            {
                // check if it can be found in the predefinedNamespaces (which are provided by the user)
                if (_predefinedNamespaces != null)
                {
                    string? definedNs = _predefinedNamespaces.LookupNamespace(prefix);
                    // compare the namespace Uri to decide if the prefix is redefined
                    if (definedNs == ns && _omitDuplNamespaces)
                    {
                        writeItOut = false;
                    }
                }
            }

            // validate special declaration (xml, xmlns)
            if ((ns == XmlReservedNs.NsXml && prefix != "xml") ||
                 (ns == XmlReservedNs.NsXmlNs && prefix != "xmlns"))
            {
                throw new ArgumentException(SR.Format(SR.Xml_NamespaceDeclXmlXmlns, prefix));
            }
            if (prefix.StartsWith('x'))
            {
                if (prefix == "xml")
                {
                    if (ns != XmlReservedNs.NsXml)
                    {
                        throw new ArgumentException(SR.Xml_XmlPrefix);
                    }
                }
                else if (prefix == "xmlns")
                {
                    throw new ArgumentException(SR.Xml_XmlnsPrefix);
                }
            }

            AddNamespace(prefix, ns, NamespaceKind.Written);

            return writeItOut;
        }

        private void AddNamespace(string prefix, string ns, NamespaceKind kind)
        {
            int top = ++_nsTop;
            if (top == _nsStack.Length)
            {
                Namespace[] newStack = new Namespace[top * 2];
                Array.Copy(_nsStack, newStack, top);
                _nsStack = newStack;
            }
            _nsStack[top].Set(prefix, ns, kind);

            if (_useNsHashtable)
            {
                // add last
                AddToNamespaceHashtable(_nsTop);
            }
            else if (_nsTop == MaxNamespacesWalkCount)
            {
                // add all
                _nsHashtable = new Dictionary<string, int>();
                for (int i = 0; i <= _nsTop; i++)
                {
                    AddToNamespaceHashtable(i);
                }
                _useNsHashtable = true;
            }
        }

        private void AddToNamespaceHashtable(int namespaceIndex)
        {
            Debug.Assert(_nsHashtable != null);

            string prefix = _nsStack[namespaceIndex].prefix;
            int existingNsIndex;
            if (_nsHashtable.TryGetValue(prefix, out existingNsIndex))
            {
                _nsStack[namespaceIndex].prevNsIndex = existingNsIndex;
            }
            _nsHashtable[prefix] = namespaceIndex;
        }

        private int LookupNamespaceIndex(string prefix)
        {
            int index;
            if (_useNsHashtable)
            {
                Debug.Assert(_nsHashtable != null);

                if (_nsHashtable.TryGetValue(prefix, out index))
                {
                    return index;
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

        private void PopNamespaces(int indexFrom, int indexTo)
        {
            Debug.Assert(_useNsHashtable);
            Debug.Assert(_nsHashtable != null);
            Debug.Assert(indexFrom <= indexTo);
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

        private static XmlException DupAttrException(string prefix, string localName)
        {
            string attr = prefix.Length > 0 ? $"{prefix}:{localName}" : localName;
            return new XmlException(SR.Xml_DupAttributeName, attr);
        }

        // Advance the state machine
        private void AdvanceState(Token token)
        {
            if ((int)_currentState >= (int)State.Closed)
            {
                if (_currentState == State.Closed || _currentState == State.Error)
                {
                    throw new InvalidOperationException(SR.Xml_ClosedOrError);
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.Xml_WrongToken, tokenName[(int)token], GetStateName(_currentState)));
                }
            }

        Advance:
            State newState = _stateTable[((int)token << 4) + (int)_currentState];
            //                         [ (int)token * 16 + (int)currentState ];

            if ((int)newState >= (int)State.Error)
            {
                switch (newState)
                {
                    case State.Error:
                        ThrowInvalidStateTransition(token, _currentState);
                        break;

                    case State.StartContent:
                        StartElementContent();
                        newState = State.Content;
                        break;

                    case State.StartContentEle:
                        StartElementContent();
                        newState = State.Element;
                        break;

                    case State.StartContentB64:
                        StartElementContent();
                        newState = State.B64Content;
                        break;

                    case State.StartDoc:
                        WriteStartDocument();
                        newState = State.Document;
                        break;

                    case State.StartDocEle:
                        WriteStartDocument();
                        newState = State.Element;
                        break;

                    case State.EndAttrSEle:
                        WriteEndAttribute();
                        StartElementContent();
                        newState = State.Element;
                        break;

                    case State.EndAttrEEle:
                        WriteEndAttribute();
                        StartElementContent();
                        newState = State.Content;
                        break;

                    case State.EndAttrSCont:
                        WriteEndAttribute();
                        StartElementContent();
                        newState = State.Content;
                        break;

                    case State.EndAttrSAttr:
                        WriteEndAttribute();
                        newState = State.Attribute;
                        break;

                    case State.PostB64Cont:
                        _rawWriter?.WriteEndBase64();
                        _currentState = State.Content;
                        goto Advance;

                    case State.PostB64Attr:
                        _rawWriter?.WriteEndBase64();
                        _currentState = State.Attribute;
                        goto Advance;

                    case State.PostB64RootAttr:
                        _rawWriter?.WriteEndBase64();
                        _currentState = State.RootLevelAttr;
                        goto Advance;

                    case State.StartFragEle:
                        StartFragment();
                        newState = State.Element;
                        break;

                    case State.StartFragCont:
                        StartFragment();
                        newState = State.Content;
                        break;

                    case State.StartFragB64:
                        StartFragment();
                        newState = State.B64Content;
                        break;

                    case State.StartRootLevelAttr:
                        WriteEndAttribute();
                        newState = State.RootLevelAttr;
                        break;

                    default:
                        Debug.Fail("We should not get to this point.");
                        break;
                }
            }

            _currentState = newState;
        }

        private void StartElementContent()
        {
            // write namespace declarations
            int start = _elemScopeStack[_elemTop].prevNSTop;
            for (int i = _nsTop; i > start; i--)
            {
                if (_nsStack[i].kind == NamespaceKind.NeedToWrite)
                {
                    _nsStack[i].WriteDecl(_writer, _rawWriter);
                }
            }

            _rawWriter?.StartElementContent();
        }

        private static string GetStateName(State state)
        {
            if (state >= State.Error)
            {
                Debug.Fail($"We should never get to this point. State = {state}");
                return "Error";
            }
            else
            {
                return stateName[(int)state];
            }
        }

        internal string? LookupNamespace(string prefix)
        {
            for (int i = _nsTop; i >= 0; i--)
            {
                if (_nsStack[i].prefix == prefix)
                {
                    return _nsStack[i].namespaceUri;
                }
            }
            return _predefinedNamespaces?.LookupNamespace(prefix);
        }

        private string? LookupLocalNamespace(string prefix)
        {
            for (int i = _nsTop; i > _elemScopeStack[_elemTop].prevNSTop; i--)
            {
                if (_nsStack[i].prefix == prefix)
                {
                    return _nsStack[i].namespaceUri;
                }
            }
            return null;
        }

        private string GeneratePrefix()
        {
            string genPrefix = string.Create(CultureInfo.InvariantCulture, $"p{_nsTop - 2:d}");
            if (LookupNamespace(genPrefix) == null)
            {
                return genPrefix;
            }
            int i = 0;

            string s;
            do
            {
                s = string.Create(CultureInfo.InvariantCulture, $"{genPrefix}{i}");
                i++;
            } while (LookupNamespace(s) != null);
            return s;
        }

        private static void CheckNCName(string ncname)
        {
            Debug.Assert(ncname != null && ncname.Length > 0);

            int i;
            int endPos = ncname.Length;

            // Check if first character is StartNCName (inc. surrogates)
            if (XmlCharType.IsStartNCNameSingleChar(ncname[0]))
            {
                i = 1;
            }
            else
            {
                throw InvalidCharsException(ncname, 0);
            }

            // Check if following characters are NCName (inc. surrogates)
            while (i < endPos)
            {
                if (XmlCharType.IsNCNameSingleChar(ncname[i]))
                {
                    i++;
                }
                else
                {
                    throw InvalidCharsException(ncname, i);
                }
            }
        }

        private static ArgumentException InvalidCharsException(string name, int badCharIndex)
        {
            string[] badCharArgs = XmlException.BuildCharExceptionArgs(name, badCharIndex);
            string[] args = new string[3];
            args[0] = name;
            args[1] = badCharArgs[0];
            args[2] = badCharArgs[1];
            return new ArgumentException(SR.Format(SR.Xml_InvalidNameCharsDetail, args));
        }

        // This method translates speficic state transition errors in more friendly error messages
        private void ThrowInvalidStateTransition(Token token, State currentState)
        {
            string wrongTokenMessage = SR.Format(SR.Xml_WrongToken, tokenName[(int)token], GetStateName(currentState));
            switch (currentState)
            {
                case State.AfterRootEle:
                case State.Start:
                    if (_conformanceLevel == ConformanceLevel.Document)
                    {
                        throw new InvalidOperationException($"{wrongTokenMessage} {SR.Xml_ConformanceLevelFragment}");
                    }
                    break;
            }
            throw new InvalidOperationException(wrongTokenMessage);
        }

        private bool IsClosedOrErrorState
        {
            get
            {
                return (int)_currentState >= (int)State.Closed;
            }
        }

        private void AddAttribute(string prefix, string localName, string namespaceName)
        {
            int top = _attrCount++;
            if (top == _attrStack.Length)
            {
                AttrName[] newStack = new AttrName[top * 2];
                Array.Copy(_attrStack, newStack, top);
                _attrStack = newStack;
            }
            _attrStack[top].Set(prefix, localName, namespaceName);

            if (_attrCount < MaxAttrDuplWalkCount)
            {
                // check for duplicates
                for (int i = 0; i < top; i++)
                {
                    if (_attrStack[i].IsDuplicate(prefix, localName, namespaceName))
                    {
                        throw DupAttrException(prefix, localName);
                    }
                }
            }
            else
            {
                // reached the threshold -> add all attributes to hash table
                if (_attrCount == MaxAttrDuplWalkCount)
                {
                    _attrHashTable ??= new Dictionary<string, int>();
                    Debug.Assert(_attrHashTable.Count == 0);
                    for (int i = 0; i < top; i++)
                    {
                        AddToAttrHashTable(i);
                    }
                }

                // add last attribute to hash table and check for duplicates
                AddToAttrHashTable(top);
                int prev = _attrStack[top].prev;
                while (prev > 0)
                {
                    // indexes are stored incremented by 1, 0 means no entry
                    prev--;
                    if (_attrStack[prev].IsDuplicate(prefix, localName, namespaceName))
                    {
                        throw DupAttrException(prefix, localName);
                    }
                    prev = _attrStack[prev].prev;
                }
            }
        }

        private void AddToAttrHashTable(int attributeIndex)
        {
            Debug.Assert(_attrHashTable != null);
            string localName = _attrStack[attributeIndex].localName;
            int count = _attrHashTable.Count;
            _attrHashTable[localName] = 0; // overwrite on collision
            if (count != _attrHashTable.Count)
            {
                return;
            }
            // chain to previous attribute in stack with the same localName
            int prev = attributeIndex - 1;
            while (prev >= 0)
            {
                if (_attrStack[prev].localName == localName)
                {
                    break;
                }
                prev--;
            }
            Debug.Assert(prev >= 0 && _attrStack[prev].localName == localName);
            _attrStack[attributeIndex].prev = prev + 1; // indexes are stored incremented by 1
        }
    }
}
