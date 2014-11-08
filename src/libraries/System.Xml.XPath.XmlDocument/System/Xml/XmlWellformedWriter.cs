// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;

namespace System.Xml
{
    internal partial class XmlWellFormedWriter : XmlWriter
    {
        //
        // Private types used by the XmlWellFormedWriter are defined in XmlWellFormedWriterHelpers.cs
        //

        //
        // Fields
        //
        // underlying writer
        XmlWriter writer;
        XmlRawWriter rawWriter;  // writer as XmlRawWriter
        IXmlNamespaceResolver predefinedNamespaces; // writer as IXmlNamespaceResolver

        // namespace management
        Namespace[] nsStack;
        int nsTop;
        Dictionary<string, int> nsHashtable;
        bool useNsHashtable;

        // element scoping
        ElementScope[] elemScopeStack;
        int elemTop;

        // attribute tracking
        AttrName[] attrStack;
        int attrCount;
        Dictionary<string, int> attrHashTable;

        // special attribute caching (xmlns, xml:space, xml:lang)
        SpecialAttribute specAttr = SpecialAttribute.No;
        AttributeValueCache attrValueCache;
        string curDeclPrefix;

        // state machine
        State[] stateTable;
        State currentState;

        // settings
        bool checkCharacters;
        bool omitDuplNamespaces;
        bool writeEndDocumentOnClose;

        // actual conformance level
        ConformanceLevel conformanceLevel;

        // flags
        bool dtdWritten;
        bool xmlDeclFollows;

        // char type tables
        XmlCharType xmlCharType = XmlCharType.Instance;

        // hash randomizer
        SecureStringHasher hasher;


        //
        // Constants
        //
        const int ElementStackInitialSize = 8;
        const int NamespaceStackInitialSize = 8;
        const int AttributeArrayInitialSize = 8;
#if DEBUG
        const int MaxAttrDuplWalkCount = 2;
        const int MaxNamespacesWalkCount = 3;
#else
        const int MaxAttrDuplWalkCount = 14;
        const int MaxNamespacesWalkCount = 16;
#endif

        //
        // State tables
        //
        enum State
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

        enum Token
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

        private static WriteState[] state2WriteState = {
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

        private static readonly State[] StateTableDocument = {
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

        private static readonly State[] StateTableAuto = {                                                                                                                                                                                                                                                                                                                                      
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

            this.writer = writer;

            rawWriter = writer as XmlRawWriter;
            predefinedNamespaces = writer as IXmlNamespaceResolver;
            if (rawWriter != null)
            {
                rawWriter.NamespaceResolver = new NamespaceResolverProxy(this);
            }

            checkCharacters = settings.CheckCharacters;
            omitDuplNamespaces = (settings.NamespaceHandling & NamespaceHandling.OmitDuplicates) != 0;
            writeEndDocumentOnClose = settings.WriteEndDocumentOnClose;

            conformanceLevel = settings.ConformanceLevel;
            stateTable = (conformanceLevel == ConformanceLevel.Document) ? StateTableDocument : StateTableAuto;

            currentState = State.Start;

            nsStack = new Namespace[NamespaceStackInitialSize];
            nsStack[0].Set(XmlConst.NsXmlNs, XmlConst.ReservedNsXmlNs, NamespaceKind.Special);
            nsStack[1].Set(XmlConst.NsXml, XmlConst.ReservedNsXml, NamespaceKind.Special);
            if (predefinedNamespaces == null)
            {
                nsStack[2].Set(string.Empty, string.Empty, NamespaceKind.Implied);
            }
            else
            {
                string defaultNs = predefinedNamespaces.LookupNamespace(string.Empty);
                nsStack[2].Set(string.Empty, (defaultNs == null ? string.Empty : defaultNs), NamespaceKind.Implied);
            }
            nsTop = 2;

            elemScopeStack = new ElementScope[ElementStackInitialSize];
            elemScopeStack[0].Set(string.Empty, string.Empty, string.Empty, nsTop);
            elemScopeStack[0].xmlSpace = XmlSpace.None;
            elemScopeStack[0].xmlLang = null;
            elemTop = 0;

            attrStack = new AttrName[AttributeArrayInitialSize];

            hasher = new SecureStringHasher();
        }

        //
        // XmlWriter implementation
        //
        public override WriteState WriteState
        {
            get
            {
                if ((int)currentState <= (int)State.Error)
                {
                    return state2WriteState[(int)currentState];
                }
                else
                {
                    Debug.Assert(false, "Expected currentState <= State.Error ");
                    return WriteState.Error;
                }
            }
        }

        public override XmlWriterSettings Settings
        {
            get
            {
                XmlWriterSettings settings = writer.Settings;
                settings.ConformanceLevel = conformanceLevel;
                if (omitDuplNamespaces)
                {
                    settings.NamespaceHandling |= NamespaceHandling.OmitDuplicates;
                }
                settings.WriteEndDocumentOnClose = writeEndDocumentOnClose;
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
                while (elemTop > 0)
                {
                    WriteEndElement();
                }
                State prevState = currentState;
                AdvanceState(Token.EndDocument);

                if (prevState != State.AfterRootEle)
                {
                    throw new ArgumentException(SR.Xml_NoRoot);
                }
                if (rawWriter == null)
                {
                    writer.WriteEndDocument();
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            try
            {
                if (name == null || name.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyName);
                }
                XmlConvertEx.VerifyQName(name, ExceptionType.XmlException);

                if (conformanceLevel == ConformanceLevel.Fragment)
                {
                    throw new InvalidOperationException(SR.Xml_DtdNotAllowedInFragment);
                }

                AdvanceState(Token.Dtd);
                if (dtdWritten)
                {
                    currentState = State.Error;
                    throw new InvalidOperationException(SR.Xml_DtdAlreadyWritten);
                }

                if (conformanceLevel == ConformanceLevel.Auto)
                {
                    conformanceLevel = ConformanceLevel.Document;
                    stateTable = StateTableDocument;
                }

                int i;

                // check characters
                if (checkCharacters)
                {
                    if (pubid != null)
                    {
                        if ((i = xmlCharType.IsPublicId(pubid)) >= 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidCharacter, XmlExceptionHelper.BuildCharExceptionArgs(pubid, i)), "pubid");
                        }
                    }
                    if (sysid != null)
                    {
                        if ((i = xmlCharType.IsOnlyCharData(sysid)) >= 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidCharacter, XmlExceptionHelper.BuildCharExceptionArgs(sysid, i)), "sysid");
                        }
                    }
                    if (subset != null)
                    {
                        if ((i = xmlCharType.IsOnlyCharData(subset)) >= 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidCharacter, XmlExceptionHelper.BuildCharExceptionArgs(subset, i)), "subset");
                        }
                    }
                }

                // write doctype
                writer.WriteDocType(name, pubid, sysid, subset);
                dtdWritten = true;
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            try
            {
                // check local name
                if (localName == null || localName.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyLocalName);
                }
                CheckNCName(localName);

                AdvanceState(Token.StartElement);

                // lookup prefix / namespace  
                if (prefix == null)
                {
                    if (ns != null)
                    {
                        prefix = LookupPrefix(ns);
                    }
                    if (prefix == null)
                    {
                        prefix = string.Empty;
                    }
                }
                else if (prefix.Length > 0)
                {
                    CheckNCName(prefix);
                    if (ns == null)
                    {
                        ns = LookupNamespace(prefix);
                    }
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

                if (elemTop == 0 && rawWriter != null)
                {
                    // notify the underlying raw writer about the root level element
                    rawWriter.OnRootElement(conformanceLevel);
                }

                // write start tag
                writer.WriteStartElement(prefix, localName, ns);

                // push element on stack and add/check namespace
                int top = ++elemTop;
                if (top == elemScopeStack.Length)
                {
                    ElementScope[] newStack = new ElementScope[top * 2];
                    Array.Copy(elemScopeStack, newStack, top);
                    elemScopeStack = newStack;
                }
                elemScopeStack[top].Set(prefix, localName, ns, nsTop);

                PushNamespaceImplicit(prefix, ns);

                if (attrCount >= MaxAttrDuplWalkCount)
                {
                    attrHashTable.Clear();
                }
                attrCount = 0;
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }


        public override void WriteEndElement()
        {
            try
            {
                AdvanceState(Token.EndElement);

                int top = elemTop;
                if (top == 0)
                {
                    throw new XmlException(SR.Format(SR.Xml_NoStartTag, string.Empty));
                }

                // write end tag
                if (rawWriter != null)
                {
                    elemScopeStack[top].WriteEndElement(rawWriter);
                }
                else
                {
                    writer.WriteEndElement();
                }

                // pop namespaces
                int prevNsTop = elemScopeStack[top].prevNSTop;
                if (useNsHashtable && prevNsTop < nsTop)
                {
                    PopNamespaces(prevNsTop + 1, nsTop);
                }
                nsTop = prevNsTop;
                elemTop = --top;

                // check "one root element" condition for ConformanceLevel.Document
                if (top == 0)
                {
                    if (conformanceLevel == ConformanceLevel.Document)
                    {
                        currentState = State.AfterRootEle;
                    }
                    else
                    {
                        currentState = State.TopLevel;
                    }
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteFullEndElement()
        {
            try
            {
                AdvanceState(Token.EndElement);

                int top = elemTop;
                if (top == 0)
                {
                    throw new XmlException(SR.Format(SR.Xml_NoStartTag, string.Empty));
                }

                // write end tag
                if (rawWriter != null)
                {
                    elemScopeStack[top].WriteFullEndElement(rawWriter);
                }
                else
                {
                    writer.WriteFullEndElement();
                }

                // pop namespaces
                int prevNsTop = elemScopeStack[top].prevNSTop;
                if (useNsHashtable && prevNsTop < nsTop)
                {
                    PopNamespaces(prevNsTop + 1, nsTop);
                }
                nsTop = prevNsTop;
                elemTop = --top;

                // check "one root element" condition for ConformanceLevel.Document
                if (top == 0)
                {
                    if (conformanceLevel == ConformanceLevel.Document)
                    {
                        currentState = State.AfterRootEle;
                    }
                    else
                    {
                        currentState = State.TopLevel;
                    }
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteStartAttribute(string prefix, string localName, string namespaceName)
        {
            try
            {
                // check local name
                if (localName == null || localName.Length == 0)
                {
                    if (prefix == XmlConst.NsXmlNs)
                    {
                        localName = XmlConst.NsXmlNs;
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
                        if (!(localName == XmlConst.NsXmlNs && namespaceName == XmlConst.ReservedNsXmlNs))
                            prefix = LookupPrefix(namespaceName);
                    }
                    if (prefix == null)
                    {
                        prefix = string.Empty;
                    }
                }
                if (namespaceName == null)
                {
                    if (prefix != null && prefix.Length > 0)
                    {
                        namespaceName = LookupNamespace(prefix);
                    }
                    if (namespaceName == null)
                    {
                        namespaceName = string.Empty;
                    }
                }

                if (prefix.Length == 0)
                {
                    if (localName == XmlConst.NsXmlNs)
                    {
                        if (namespaceName.Length > 0 && namespaceName != XmlConst.ReservedNsXmlNs)
                        {
                            throw new ArgumentException(SR.Xml_XmlnsPrefix);
                        }
                        curDeclPrefix = String.Empty;
                        SetSpecialAttribute(SpecialAttribute.DefaultXmlns);
                        goto SkipPushAndWrite;
                    }
                    else if (namespaceName.Length > 0)
                    {
                        prefix = LookupPrefix(namespaceName);
                        if (prefix == null || prefix.Length == 0)
                        {
                            prefix = GeneratePrefix();
                        }
                    }
                }
                else
                {
                    if (prefix == XmlConst.NsXmlNs)
                    {
                        if (namespaceName.Length > 0 && namespaceName != XmlConst.ReservedNsXmlNs)
                        {
                            throw new ArgumentException(SR.Xml_XmlnsPrefix);
                        }
                        curDeclPrefix = localName;
                        SetSpecialAttribute(SpecialAttribute.PrefixedXmlns);
                        goto SkipPushAndWrite;
                    }
                    else if (prefix == XmlConst.NsXml)
                    {
                        if (namespaceName.Length > 0 && namespaceName != XmlConst.ReservedNsXml)
                        {
                            throw new ArgumentException(SR.Xml_XmlPrefix);
                        }
                        switch (localName)
                        {
                            case XmlConst.AttrSpace:
                                SetSpecialAttribute(SpecialAttribute.XmlSpace);
                                goto SkipPushAndWrite;
                            case XmlConst.AttrLang:
                                SetSpecialAttribute(SpecialAttribute.XmlLang);
                                goto SkipPushAndWrite;
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
                        string definedNs = LookupLocalNamespace(prefix);
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

                if (specAttr == SpecialAttribute.No)
                {
                    // write attribute name
                    writer.WriteStartAttribute(prefix, localName, namespaceName);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteEndAttribute()
        {
            try
            {
                AdvanceState(Token.EndAttribute);

                if (specAttr != SpecialAttribute.No)
                {
                    string value;

                    switch (specAttr)
                    {
                        case SpecialAttribute.DefaultXmlns:
                            value = attrValueCache.StringValue;
                            if (PushNamespaceExplicit(string.Empty, value))
                            { // returns true if the namespace declaration should be written out
                                if (rawWriter != null)
                                {
                                    if (rawWriter.SupportsNamespaceDeclarationInChunks)
                                    {
                                        rawWriter.WriteStartNamespaceDeclaration(string.Empty);
                                        attrValueCache.Replay(rawWriter);
                                        rawWriter.WriteEndNamespaceDeclaration();
                                    }
                                    else
                                    {
                                        rawWriter.WriteNamespaceDeclaration(string.Empty, value);
                                    }
                                }
                                else
                                {
                                    writer.WriteStartAttribute(string.Empty, XmlConst.NsXmlNs, XmlConst.ReservedNsXmlNs);
                                    attrValueCache.Replay(writer);
                                    writer.WriteEndAttribute();
                                }
                            }
                            curDeclPrefix = null;
                            break;
                        case SpecialAttribute.PrefixedXmlns:
                            value = attrValueCache.StringValue;
                            if (value.Length == 0)
                            {
                                throw new ArgumentException(SR.Xml_PrefixForEmptyNs);
                            }
                            if (value == XmlConst.ReservedNsXmlNs || (value == XmlConst.ReservedNsXml && curDeclPrefix != XmlConst.NsXml))
                            {
                                throw new ArgumentException(SR.Xml_CanNotBindToReservedNamespace);
                            }
                            if (PushNamespaceExplicit(curDeclPrefix, value))
                            { // returns true if the namespace declaration should be written out
                                if (rawWriter != null)
                                {
                                    if (rawWriter.SupportsNamespaceDeclarationInChunks)
                                    {
                                        rawWriter.WriteStartNamespaceDeclaration(curDeclPrefix);
                                        attrValueCache.Replay(rawWriter);
                                        rawWriter.WriteEndNamespaceDeclaration();
                                    }
                                    else
                                    {
                                        rawWriter.WriteNamespaceDeclaration(curDeclPrefix, value);
                                    }
                                }
                                else
                                {
                                    writer.WriteStartAttribute(XmlConst.NsXmlNs, curDeclPrefix, XmlConst.ReservedNsXmlNs);
                                    attrValueCache.Replay(writer);
                                    writer.WriteEndAttribute();
                                }
                            }
                            curDeclPrefix = null;
                            break;
                        case SpecialAttribute.XmlSpace:
                            attrValueCache.Trim();
                            value = attrValueCache.StringValue;

                            if (value == XmlConst.AttrSpaceValueDefault)
                            {
                                elemScopeStack[elemTop].xmlSpace = XmlSpace.Default;
                            }
                            else if (value == XmlConst.AttrSpaceValuePreserve)
                            {
                                elemScopeStack[elemTop].xmlSpace = XmlSpace.Preserve;
                            }
                            else
                            {
                                throw new ArgumentException(SR.Xml_InvalidXmlSpace, value);
                            }
                            writer.WriteStartAttribute(XmlConst.NsXml, XmlConst.AttrSpace, XmlConst.ReservedNsXml);
                            attrValueCache.Replay(writer);
                            writer.WriteEndAttribute();
                            break;
                        case SpecialAttribute.XmlLang:
                            value = attrValueCache.StringValue;
                            elemScopeStack[elemTop].xmlLang = value;
                            writer.WriteStartAttribute(XmlConst.NsXml, XmlConst.AttrLang, XmlConst.ReservedNsXml);
                            attrValueCache.Replay(writer);
                            writer.WriteEndAttribute();
                            break;
                    }
                    specAttr = SpecialAttribute.No;
                    attrValueCache.Clear();
                }
                else
                {
                    writer.WriteEndAttribute();
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteCData(string text)
        {
            try
            {
                if (text == null)
                {
                    text = string.Empty;
                }
                AdvanceState(Token.CData);
                writer.WriteCData(text);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteComment(string text)
        {
            try
            {
                if (text == null)
                {
                    text = string.Empty;
                }
                AdvanceState(Token.Comment);
                writer.WriteComment(text);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            try
            {
                // check name
                if (name == null || name.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyName);
                }
                CheckNCName(name);

                // check text
                if (text == null)
                {
                    text = string.Empty;
                }

                // xml declaration is a special case (not a processing instruction, but we allow WriteProcessingInstruction as a convenience)
                if (name.Length == 3 && string.Compare(name, XmlConst.XmlDeclarationTag, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (currentState != State.Start)
                    {
                        throw new ArgumentException(conformanceLevel == ConformanceLevel.Document ? SR.Xml_DupXmlDecl : SR.Xml_CannotWriteXmlDecl);
                    }

                    xmlDeclFollows = true;
                    AdvanceState(Token.PI);

                    if (rawWriter != null)
                    {
                        // Translate PI into an xml declaration
                        rawWriter.WriteXmlDeclaration(text);
                    }
                    else
                    {
                        writer.WriteProcessingInstruction(name, text);
                    }
                }
                else
                {
                    AdvanceState(Token.PI);
                    writer.WriteProcessingInstruction(name, text);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteEntityRef(string name)
        {
            try
            {
                // check name
                if (name == null || name.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyName);
                }
                CheckNCName(name);

                AdvanceState(Token.Text);
                if (SaveAttrValue)
                {
                    attrValueCache.WriteEntityRef(name);
                }
                else
                {
                    writer.WriteEntityRef(name);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteCharEntity(char ch)
        {
            try
            {
                if (Char.IsSurrogate(ch))
                {
                    throw new ArgumentException(SR.Xml_InvalidSurrogateMissingLowChar);
                }

                AdvanceState(Token.Text);
                if (SaveAttrValue)
                {
                    attrValueCache.WriteCharEntity(ch);
                }
                else
                {
                    writer.WriteCharEntity(ch);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            try
            {
                if (!Char.IsSurrogatePair(highChar, lowChar))
                {
                    throw XmlConvertEx.CreateInvalidSurrogatePairException(lowChar, highChar);
                }

                AdvanceState(Token.Text);
                if (SaveAttrValue)
                {
                    attrValueCache.WriteSurrogateCharEntity(lowChar, highChar);
                }
                else
                {
                    writer.WriteSurrogateCharEntity(lowChar, highChar);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteWhitespace(string ws)
        {
            try
            {
                if (ws == null)
                {
                    ws = string.Empty;
                }
                if (!XmlCharType.Instance.IsOnlyWhitespace(ws))
                {
                    throw new ArgumentException(SR.Xml_NonWhitespace);
                }

                AdvanceState(Token.Whitespace);
                if (SaveAttrValue)
                {
                    attrValueCache.WriteWhitespace(ws);
                }
                else
                {
                    writer.WriteWhitespace(ws);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteString(string text)
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
                    attrValueCache.WriteString(text);
                }
                else
                {
                    writer.WriteString(text);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            try
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException("buffer");
                }
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException("count");
                }
                if (count > buffer.Length - index)
                {
                    throw new ArgumentOutOfRangeException("count");
                }

                AdvanceState(Token.Text);
                if (SaveAttrValue)
                {
                    attrValueCache.WriteChars(buffer, index, count);
                }
                else
                {
                    writer.WriteChars(buffer, index, count);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            try
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException("buffer");
                }
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException("count");
                }
                if (count > buffer.Length - index)
                {
                    throw new ArgumentOutOfRangeException("count");
                }

                AdvanceState(Token.RawData);
                if (SaveAttrValue)
                {
                    attrValueCache.WriteRaw(buffer, index, count);
                }
                else
                {
                    writer.WriteRaw(buffer, index, count);
                }
            }
            catch
            {
                currentState = State.Error;
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
                    attrValueCache.WriteRaw(data);
                }
                else
                {
                    writer.WriteRaw(data);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            try
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException("buffer");
                }
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException("count");
                }
                if (count > buffer.Length - index)
                {
                    throw new ArgumentOutOfRangeException("count");
                }

                AdvanceState(Token.Base64);
                writer.WriteBase64(buffer, index, count);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && currentState != State.Closed)
            {
                try
                {
                    if (writeEndDocumentOnClose)
                    {
                        while (currentState != State.Error && elemTop > 0)
                        {
                            WriteEndElement();
                        }
                    }
                    else
                    {
                        if (currentState != State.Error && elemTop > 0)
                        {
                            //finish the start element tag '>'
                            try
                            {
                                AdvanceState(Token.EndElement);
                            }
                            catch
                            {
                                currentState = State.Error;
                                throw;
                            }
                        }
                    }

                    if (InBase64 && rawWriter != null)
                    {
                        rawWriter.WriteEndBase64();
                    }

                    writer.Flush();
                }
                finally
                {
                    try
                    {
                        if (rawWriter != null)
                        {
                            rawWriter.Close(WriteState);
                        }
                        else
                        {
                            writer.Dispose();
                        }
                    }
                    finally
                    {
                        currentState = State.Closed;
                    }
                }
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            try
            {
                writer.Flush();
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override string LookupPrefix(string ns)
        {
            try
            {
                if (ns == null)
                {
                    throw new ArgumentNullException("ns");
                }
                for (int i = nsTop; i >= 0; i--)
                {
                    if (nsStack[i].namespaceUri == ns)
                    {
                        string prefix = nsStack[i].prefix;
                        for (i++; i <= nsTop; i++)
                        {
                            if (nsStack[i].prefix == prefix)
                            {
                                return null;
                            }
                        }
                        return prefix;
                    }
                }
                return (predefinedNamespaces != null) ? predefinedNamespaces.LookupPrefix(ns) : null;
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                int i;
                for (i = elemTop; i >= 0 && elemScopeStack[i].xmlSpace == (System.Xml.XmlSpace)(int)-1; i--) ;
                Debug.Assert(i >= 0);
                return elemScopeStack[i].xmlSpace;
            }
        }

        public override string XmlLang
        {
            get
            {
                int i;
                for (i = elemTop; i > 0 && elemScopeStack[i].xmlLang == null; i--) ;
                Debug.Assert(i >= 0);
                return elemScopeStack[i].xmlLang;
            }
        }

        public override void WriteQualifiedName(string localName, string ns)
        {
            try
            {
                if (localName == null || localName.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyLocalName);
                }
                CheckNCName(localName);

                AdvanceState(Token.Text);
                string prefix = String.Empty;
                if (ns != null && ns.Length != 0)
                {
                    prefix = LookupPrefix(ns);
                    if (prefix == null)
                    {
                        if (currentState != State.Attribute)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_UndefNamespace, ns));
                        }
                        prefix = GeneratePrefix();
                        PushNamespaceImplicit(prefix, ns);
                    }
                }
                // if this is a special attribute, then just convert this to text
                // otherwise delegate to raw-writer
                if (SaveAttrValue || rawWriter == null)
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
                    rawWriter.WriteQualifiedName(prefix, localName, ns);
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(bool value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                writer.WriteValue(value);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(DateTimeOffset value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                writer.WriteValue(value);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(double value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                writer.WriteValue(value);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(float value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                writer.WriteValue(value);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(decimal value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                writer.WriteValue(value);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(int value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                writer.WriteValue(value);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(long value)
        {
            try
            {
                AdvanceState(Token.AtomicValue);
                writer.WriteValue(value);
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        public override void WriteValue(string value)
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
                    attrValueCache.WriteValue(value);
                }
                else
                {
                    AdvanceState(Token.AtomicValue);
                    writer.WriteValue(value);
                }
            }
            catch
            {
                currentState = State.Error;
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
                    attrValueCache.WriteValue((string)value);
                }
                else
                {
                    AdvanceState(Token.AtomicValue);
                    writer.WriteValue(value);
                }
            }
            catch
            {
                currentState = State.Error;
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
                currentState = State.Error;
                throw;
            }
        }

        //
        // Internal methods
        //
#if !SILVERLIGHT
        internal XmlWriter InnerWriter
        {
            get
            {
                return this.writer;
            }
        }

        internal XmlRawWriter RawWriter
        {
            get
            {
                return rawWriter;
            }
        }
#endif

        //
        // Private methods
        //
        private bool SaveAttrValue
        {
            get
            {
                return specAttr != SpecialAttribute.No;
            }
        }

        private bool InBase64
        {
            get
            {
                return (currentState == State.B64Content || currentState == State.B64Attribute || currentState == State.RootLevelB64Attr);
            }
        }

        private void SetSpecialAttribute(SpecialAttribute special)
        {
            specAttr = special;
            if (State.Attribute == currentState)
                currentState = State.SpecialAttr;
            else if (State.RootLevelAttr == currentState)
                currentState = State.RootLevelSpecAttr;
            else
                Debug.Assert(false, "State.Attribute == currentState || State.RootLevelAttr == currentState");

            if (attrValueCache == null)
            {
                attrValueCache = new AttributeValueCache();
            }
        }

        private void WriteStartDocumentImpl(XmlStandalone standalone)
        {
            try
            {
                AdvanceState(Token.StartDocument);

                if (conformanceLevel == ConformanceLevel.Auto)
                {
                    conformanceLevel = ConformanceLevel.Document;
                    stateTable = StateTableDocument;
                }
                else if (conformanceLevel == ConformanceLevel.Fragment)
                {
                    throw new InvalidOperationException(SR.Xml_CannotStartDocumentOnFragment);
                }

                if (rawWriter != null)
                {
                    if (!xmlDeclFollows)
                    {
                        rawWriter.WriteXmlDeclaration(standalone);
                    }
                }
                else
                {
                    // We do not pass the standalone value here
                    writer.WriteStartDocument();
                }
            }
            catch
            {
                currentState = State.Error;
                throw;
            }
        }

        private void StartFragment()
        {
            conformanceLevel = ConformanceLevel.Fragment;
            Debug.Assert(stateTable == StateTableAuto);
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
                if (existingNsIndex > elemScopeStack[elemTop].prevNSTop)
                {
                    // The new namespace Uri needs to be the same as the one that is already declared
                    if (nsStack[existingNsIndex].namespaceUri != ns)
                    {
                        throw new XmlException(SR.Format(SR.Xml_RedefinePrefix, new string[] { prefix, nsStack[existingNsIndex].namespaceUri, ns }));
                    }
                    // No additional work needed
                    return;
                }
                // The prefix is defined but in a different scope
                else
                {
                    // existing declaration is special one (xml, xmlns) -> validate that the new one is the same and can be declared 
                    if (nsStack[existingNsIndex].kind == NamespaceKind.Special)
                    {
                        if (prefix == XmlConst.NsXml)
                        {
                            if (ns != nsStack[existingNsIndex].namespaceUri)
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
                            Debug.Assert(prefix == XmlConst.NsXmlNs);
                            throw new ArgumentException(SR.Xml_XmlnsPrefix);
                        }
                    }
                    // regular namespace declaration -> compare the namespace Uris to decide if the prefix is redefined
                    else
                    {
                        kind = (nsStack[existingNsIndex].namespaceUri == ns) ? NamespaceKind.Implied : NamespaceKind.NeedToWrite;
                    }
                }
            }
            // No existing declaration found in the namespace stack
            else
            {
                // validate special declaration (xml, xmlns)
                if ((ns == XmlConst.ReservedNsXml && prefix != XmlConst.NsXml) ||
                     (ns == XmlConst.ReservedNsXmlNs && prefix != XmlConst.NsXmlNs))
                {
                    throw new ArgumentException(SR.Format(SR.Xml_NamespaceDeclXmlXmlns, prefix));
                }

                // check if it can be found in the predefinedNamespaces (which are provided by the user)
                if (predefinedNamespaces != null)
                {
                    string definedNs = predefinedNamespaces.LookupNamespace(prefix);
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
        // It returs true if the namespace declaration should we written out, false if it should be omited (if OmitDuplicateNamespaceDeclarations is true)
        private bool PushNamespaceExplicit(string prefix, string ns)
        {
            bool writeItOut = true;

            // See if the prefix is already defined
            int existingNsIndex = LookupNamespaceIndex(prefix);

            // Existing declaration in the current scope
            if (existingNsIndex != -1)
            {
                // It is defined in the current scope
                if (existingNsIndex > elemScopeStack[elemTop].prevNSTop)
                {
                    // The new namespace Uri needs to be the same as the one that is already declared
                    if (nsStack[existingNsIndex].namespaceUri != ns)
                    {
                        throw new XmlException(SR.Format(SR.Xml_RedefinePrefix, new string[] { prefix, nsStack[existingNsIndex].namespaceUri, ns }));
                    }
                    // Check for duplicate declarations
                    NamespaceKind existingNsKind = nsStack[existingNsIndex].kind;
                    if (existingNsKind == NamespaceKind.Written)
                    {
                        throw DupAttrException((prefix.Length == 0) ? string.Empty : XmlConst.NsXmlNs, (prefix.Length == 0) ? XmlConst.NsXmlNs : prefix);
                    }
                    // Check if it can be omitted
                    if (omitDuplNamespaces && existingNsKind != NamespaceKind.NeedToWrite)
                    {
                        writeItOut = false;
                    }
                    nsStack[existingNsIndex].kind = NamespaceKind.Written;
                    // No additional work needed
                    return writeItOut;
                }
                // The prefix is defined but in a different scope
                else
                {
                    // check if is the same and can be omitted
                    if (nsStack[existingNsIndex].namespaceUri == ns && omitDuplNamespaces)
                    {
                        writeItOut = false;
                    }
                }
            }
            // No existing declaration found in the namespace stack
            else
            {
                // check if it can be found in the predefinedNamespaces (which are provided by the user)
                if (predefinedNamespaces != null)
                {
                    string definedNs = predefinedNamespaces.LookupNamespace(prefix);
                    // compare the namespace Uri to decide if the prefix is redefined
                    if (definedNs == ns && omitDuplNamespaces)
                    {
                        writeItOut = false;
                    }
                }
            }

            // validate special declaration (xml, xmlns)
            if ((ns == XmlConst.ReservedNsXml && prefix != XmlConst.NsXml) ||
                 (ns == XmlConst.ReservedNsXmlNs && prefix != XmlConst.NsXmlNs))
            {
                throw new ArgumentException(SR.Format(SR.Xml_NamespaceDeclXmlXmlns, prefix));
            }

            if (prefix == XmlConst.NsXml)
            {
                if (ns != XmlConst.ReservedNsXml)
                {
                    throw new ArgumentException(SR.Xml_XmlPrefix);
                }
            }
            else if (prefix == XmlConst.NsXmlNs)
            {
                throw new ArgumentException(SR.Xml_XmlnsPrefix);
            }

            AddNamespace(prefix, ns, NamespaceKind.Written);

            return writeItOut;
        }

        private void AddNamespace(string prefix, string ns, NamespaceKind kind)
        {
            int top = ++nsTop;
            if (top == nsStack.Length)
            {
                Namespace[] newStack = new Namespace[top * 2];
                Array.Copy(nsStack, newStack, top);
                nsStack = newStack;
            }
            nsStack[top].Set(prefix, ns, kind);

            if (useNsHashtable)
            {
                // add last
                AddToNamespaceHashtable(nsTop);
            }
            else if (nsTop == MaxNamespacesWalkCount)
            {
                // add all
                nsHashtable = new Dictionary<string, int>(hasher);
                for (int i = 0; i <= nsTop; i++)
                {
                    AddToNamespaceHashtable(i);
                }
                useNsHashtable = true;
            }
        }

        private void AddToNamespaceHashtable(int namespaceIndex)
        {
            string prefix = nsStack[namespaceIndex].prefix;
            int existingNsIndex;
            if (nsHashtable.TryGetValue(prefix, out existingNsIndex))
            {
                nsStack[namespaceIndex].prevNsIndex = existingNsIndex;
            }
            nsHashtable[prefix] = namespaceIndex;
        }

        private int LookupNamespaceIndex(string prefix)
        {
            int index;
            if (useNsHashtable)
            {
                if (nsHashtable.TryGetValue(prefix, out index))
                {
                    return index;
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

        private void PopNamespaces(int indexFrom, int indexTo)
        {
            Debug.Assert(useNsHashtable);
            Debug.Assert(indexFrom <= indexTo);
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

        static private XmlException DupAttrException(string prefix, string localName)
        {
            StringBuilder sb = new StringBuilder();
            if (prefix.Length > 0)
            {
                sb.Append(prefix);
                sb.Append(':');
            }
            sb.Append(localName);
            return new XmlException(SR.Format(SR.Xml_DupAttributeName, sb.ToString()));
        }

        // Advance the state machine
        private void AdvanceState(Token token)
        {
            if ((int)currentState >= (int)State.Closed)
            {
                if (currentState == State.Closed || currentState == State.Error)
                {
                    throw new InvalidOperationException(SR.Xml_ClosedOrError);
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.Xml_WrongToken, tokenName[(int)token], GetStateName(currentState)));
                }
            }

        Advance:
            State newState = stateTable[((int)token << 4) + (int)currentState];
            //                         [ (int)token * 16 + (int)currentState ];

            if ((int)newState >= (int)State.Error)
            {
                switch (newState)
                {
                    case State.Error:
                        ThrowInvalidStateTransition(token, currentState);
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
                        if (rawWriter != null)
                        {
                            rawWriter.WriteEndBase64();
                        }
                        currentState = State.Content;
                        goto Advance;

                    case State.PostB64Attr:
                        if (rawWriter != null)
                        {
                            rawWriter.WriteEndBase64();
                        }
                        currentState = State.Attribute;
                        goto Advance;

                    case State.PostB64RootAttr:
                        if (rawWriter != null)
                        {
                            rawWriter.WriteEndBase64();
                        }
                        currentState = State.RootLevelAttr;
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
                        Debug.Assert(false, "We should not get to this point.");
                        break;
                }
            }

            currentState = newState;
        }

        private void StartElementContent()
        {
            // write namespace declarations
            int start = elemScopeStack[elemTop].prevNSTop;
            for (int i = nsTop; i > start; i--)
            {
                if (nsStack[i].kind == NamespaceKind.NeedToWrite)
                {
                    nsStack[i].WriteDecl(writer, rawWriter);
                }
            }

            if (rawWriter != null)
            {
                rawWriter.StartElementContent();
            }
        }

        private static string GetStateName(State state)
        {
            if (state >= State.Error)
            {
                Debug.Assert(false, "We should never get to this point. State = " + state);
                return "Error";
            }
            else
            {
                return stateName[(int)state];
            }
        }

        internal string LookupNamespace(string prefix)
        {
            for (int i = nsTop; i >= 0; i--)
            {
                if (nsStack[i].prefix == prefix)
                {
                    return nsStack[i].namespaceUri;
                }
            }
            return (predefinedNamespaces != null) ? predefinedNamespaces.LookupNamespace(prefix) : null;
        }

        private string LookupLocalNamespace(string prefix)
        {
            for (int i = nsTop; i > elemScopeStack[elemTop].prevNSTop; i--)
            {
                if (nsStack[i].prefix == prefix)
                {
                    return nsStack[i].namespaceUri;
                }
            }
            return null;
        }

        private string GeneratePrefix()
        {
            string genPrefix = "p" + (nsTop - 2).ToString("d", CultureInfo.InvariantCulture);
            if (LookupNamespace(genPrefix) == null)
            {
                return genPrefix;
            }
            int i = 0;

            string s;
            do
            {
                s = string.Concat(genPrefix, i.ToString(CultureInfo.InvariantCulture));
                i++;
            } while (LookupNamespace(s) != null);
            return s;
        }

#if SILVERLIGHT && !SILVERLIGHT_DISABLE_SECURITY && XMLCHARTYPE_USE_RESOURCE
        [System.Security.SecuritySafeCritical]
#endif
        private unsafe void CheckNCName(string ncname)
        {
            Debug.Assert(ncname != null && ncname.Length > 0);

            int i;
            int endPos = ncname.Length;

            // Check if first character is StartNCName (inc. surrogates)
            if ((xmlCharType.charProperties[ncname[0]] & XmlCharType.fNCStartNameSC) != 0)
            {
                i = 1;
            }
#if XML10_FIFTH_EDITION
            else if (xmlCharType.IsNCNameSurrogateChar(ncname, 0))
            { // surrogate ranges are same for NCName and StartNCName
                i = 2;
            }
#endif
            else
            {
                throw InvalidCharsException(ncname, 0);
            }

            // Check if following characters are NCName (inc. surrogates)
            while (i < endPos)
            {
                if ((xmlCharType.charProperties[ncname[i]] & XmlCharType.fNCNameSC) != 0)
                {
                    i++;
                }
#if XML10_FIFTH_EDITION
                else if (xmlCharType.IsNCNameSurrogateChar(ncname, i))
                {
                    i += 2;
                }
#endif
                else
                {
                    throw InvalidCharsException(ncname, i);
                }
            }
        }

        private static Exception InvalidCharsException(string name, int badCharIndex)
        {
            string[] badCharArgs = XmlExceptionHelper.BuildCharExceptionArgs(name, badCharIndex);
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
                    if (conformanceLevel == ConformanceLevel.Document)
                    {
                        throw new InvalidOperationException(wrongTokenMessage + ' ' + SR.Xml_ConformanceLevelFragment);
                    }
                    break;
            }
            throw new InvalidOperationException(wrongTokenMessage);
        }

        private bool IsClosedOrErrorState
        {
            get
            {
                return (int)currentState >= (int)State.Closed;
            }
        }

        private void AddAttribute(string prefix, string localName, string namespaceName)
        {
            int top = attrCount++;
            if (top == attrStack.Length)
            {
                AttrName[] newStack = new AttrName[top * 2];
                Array.Copy(attrStack, newStack, top);
                attrStack = newStack;
            }
            attrStack[top].Set(prefix, localName, namespaceName);

            if (attrCount < MaxAttrDuplWalkCount)
            {
                // check for duplicates
                for (int i = 0; i < top; i++)
                {
                    if (attrStack[i].IsDuplicate(prefix, localName, namespaceName))
                    {
                        throw DupAttrException(prefix, localName);
                    }
                }
            }
            else
            {
                // reached the threshold -> add all attributes to hash table
                if (attrCount == MaxAttrDuplWalkCount)
                {
                    if (attrHashTable == null)
                    {
                        attrHashTable = new Dictionary<string, int>(hasher);
                    }
                    Debug.Assert(attrHashTable.Count == 0);
                    for (int i = 0; i < top; i++)
                    {
                        AddToAttrHashTable(i);
                    }
                }

                // add last attribute to hash table and check for duplicates
                AddToAttrHashTable(top);
                int prev = attrStack[top].prev;
                while (prev > 0)
                {
                    // indexes are stored incremented by 1, 0 means no entry
                    prev--;
                    if (attrStack[prev].IsDuplicate(prefix, localName, namespaceName))
                    {
                        throw DupAttrException(prefix, localName);
                    }
                    prev = attrStack[prev].prev;
                }
            }
        }

        private void AddToAttrHashTable(int attributeIndex)
        {
            string localName = attrStack[attributeIndex].localName;
            int count = attrHashTable.Count;
            attrHashTable[localName] = 0; // overwrite on collision
            if (count != attrHashTable.Count)
            {
                return;
            }
            // chain to previous attribute in stack with the same localName
            int prev = attributeIndex - 1;
            while (prev >= 0)
            {
                if (attrStack[prev].localName == localName)
                {
                    break;
                }
                prev--;
            }
            Debug.Assert(prev >= 0 && attrStack[prev].localName == localName);
            attrStack[attributeIndex].prev = prev + 1; // indexes are stored incremented by 1 
        }
    }
}
