// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xml.XPath
{
    /// <summary>
    /// Reader that traverses the subtree rooted at the current position of the specified navigator.
    /// </summary>
    internal class XPathNavigatorReader : XmlReader, IXmlNamespaceResolver
    {
        enum State
        {
            Initial,
            Content,
            EndElement,
            Attribute,
            AttrVal,
            InReadBinary,
            EOF,
            Closed,
            Error,
        }

        private XPathNavigator nav;
        private XPathNavigator navToRead;
        private int depth;
        private State state;
        private XmlNodeType nodeType;
        private int attrCount;
        private bool readEntireDocument;

        private ReadContentAsBinaryHelper readBinaryHelper;
        private State savedState;

        internal const string space = "space";

        internal static XmlNodeType[] convertFromXPathNodeType = {
            XmlNodeType.Document,               // XPathNodeType.Root
            XmlNodeType.Element,                // XPathNodeType.Element
            XmlNodeType.Attribute,              // XPathNodeType.Attribute
            XmlNodeType.Attribute,              // XPathNodeType.Namespace
            XmlNodeType.Text,                   // XPathNodeType.Text
            XmlNodeType.SignificantWhitespace,  // XPathNodeType.SignificantWhitespace
            XmlNodeType.Whitespace,             // XPathNodeType.Whitespace
            XmlNodeType.ProcessingInstruction,  // XPathNodeType.ProcessingInstruction
            XmlNodeType.Comment,                // XPathNodeType.Comment
            XmlNodeType.None                    // XPathNodeType.All
        };

        /// <summary>
        /// Translates an XPathNodeType value into the corresponding XmlNodeType value.
        /// XPathNodeType.Whitespace and XPathNodeType.SignificantWhitespace are mapped into XmlNodeType.Text.
        /// </summary>
        internal static XmlNodeType ToXmlNodeType(XPathNodeType typ)
        {
            return XPathNavigatorReader.convertFromXPathNodeType[(int)typ];
        }

        static public XPathNavigatorReader Create(XPathNavigator navToRead)
        {
            XPathNavigator nav = navToRead.Clone();
            return new XPathNavigatorReader(nav);
        }

        protected XPathNavigatorReader(XPathNavigator navToRead)
        {
            // Need clone that can be moved independently of original navigator
            this.navToRead = navToRead;
            this.nav = XmlEmptyNavigator.Singleton;
            this.state = State.Initial;
            this.depth = 0;
            this.nodeType = XPathNavigatorReader.ToXmlNodeType(this.nav.NodeType);
        }

        //-----------------------------------------------
        // IXmlNamespaceResolver -- pass through to Navigator
        //-----------------------------------------------
        public override XmlNameTable NameTable
        {
            get
            {
                return this.navToRead.NameTable;
            }
        }

        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return this.nav.GetNamespacesInScope(scope);
        }

        string IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return this.nav.LookupNamespace(prefix);
        }

        string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return this.nav.LookupPrefix(namespaceName);
        }

        //-----------------------------------------------
        // XmlReader -- pass through to Navigator
        //-----------------------------------------------

        public override XmlReaderSettings Settings
        {
            get
            {
                XmlReaderSettings rs = new XmlReaderSettings();
                rs.NameTable = this.NameTable;
                rs.ConformanceLevel = ConformanceLevel.Fragment;
                rs.CheckCharacters = false;
                return rs;
            }
        }

        public override System.Type ValueType
        {
            get { return this.nav.ValueType; }
        }

        public override XmlNodeType NodeType
        {
            get { return this.nodeType; }
        }

        public override string NamespaceURI
        {
            get
            {
                //NamespaceUri for namespace nodes is different in case of XPathNavigator and Reader
                if (this.nav.NodeType == XPathNodeType.Namespace)
                    return this.NameTable.Add(XmlConst.ReservedNsXmlNs);
                //Special case attribute text node
                if (this.NodeType == XmlNodeType.Text)
                    return string.Empty;
                return this.nav.NamespaceURI;
            }
        }

        public override string LocalName
        {
            get
            {
                //Default namespace in case of reader has a local name value of 'xmlns'
                if (this.nav.NodeType == XPathNodeType.Namespace && this.nav.LocalName.Length == 0)
                    return this.NameTable.Add("xmlns");
                //Special case attribute text node
                if (this.NodeType == XmlNodeType.Text)
                    return string.Empty;
                return this.nav.LocalName;
            }
        }

        public override string Prefix
        {
            get
            {
                //Prefix for namespace nodes is different in case of XPathNavigator and Reader
                if (this.nav.NodeType == XPathNodeType.Namespace && this.nav.LocalName.Length != 0)
                    return this.NameTable.Add("xmlns");
                //Special case attribute text node
                if (this.NodeType == XmlNodeType.Text)
                    return string.Empty;
                return this.nav.Prefix;
            }
        }

        public override string BaseURI
        {
            get
            {
                //reader returns BaseUri even before read method is called.
                if (this.state == State.Initial)
                    return this.navToRead.BaseURI;
                return this.nav.BaseURI;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                return this.nav.IsEmptyElement;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                XPathNavigator tempNav = this.nav.Clone();
                do
                {
                    if (tempNav.MoveToAttribute(XPathNavigatorReader.space, XmlConst.ReservedNsXml))
                    {
                        switch (XmlConvertEx.TrimString(tempNav.Value))
                        {
                            case "default":
                                return XmlSpace.Default;
                            case "preserve":
                                return XmlSpace.Preserve;
                            default:
                                break;
                        }
                        tempNav.MoveToParent();
                    }
                }
                while (tempNav.MoveToParent());
                return XmlSpace.None;
            }
        }

        public override string XmlLang
        {
            get
            {
                return this.nav.XmlLang;
            }
        }

        public override bool HasValue
        {
            get
            {
                if ((this.nodeType != XmlNodeType.Element)
                    && (this.nodeType != XmlNodeType.Document)
                    && (this.nodeType != XmlNodeType.EndElement)
                    && (this.nodeType != XmlNodeType.None))
                    return true;
                return false;
            }
        }

        public override string Value
        {
            get
            {
                if ((this.nodeType != XmlNodeType.Element)
                    && (this.nodeType != XmlNodeType.Document)
                    && (this.nodeType != XmlNodeType.EndElement)
                    && (this.nodeType != XmlNodeType.None))
                    return this.nav.Value;
                return string.Empty;
            }
        }

        private XPathNavigator GetElemNav()
        {
            XPathNavigator tempNav;
            switch (this.state)
            {
                case State.Content:
                    return this.nav.Clone();
                case State.Attribute:
                case State.AttrVal:
                    tempNav = this.nav.Clone();
                    if (tempNav.MoveToParent())
                        return tempNav;
                    break;
                case State.InReadBinary:
                    state = savedState;
                    XPathNavigator nav = GetElemNav();
                    state = State.InReadBinary;
                    return nav;
            }
            return null;
        }

        private XPathNavigator GetElemNav(out int depth)
        {
            XPathNavigator nav = null;
            switch (this.state)
            {
                case State.Content:
                    if (this.nodeType == XmlNodeType.Element)
                        nav = this.nav.Clone();
                    depth = this.depth;
                    break;
                case State.Attribute:
                    nav = this.nav.Clone();
                    nav.MoveToParent();
                    depth = this.depth - 1;
                    break;
                case State.AttrVal:
                    nav = this.nav.Clone();
                    nav.MoveToParent();
                    depth = this.depth - 2;
                    break;
                case State.InReadBinary:
                    state = savedState;
                    nav = GetElemNav(out depth);
                    state = State.InReadBinary;
                    break;
                default:
                    depth = this.depth;
                    break;
            }
            return nav;
        }

        private void MoveToAttr(XPathNavigator nav, int depth)
        {
            this.nav.MoveTo(nav);
            this.depth = depth;
            this.nodeType = XmlNodeType.Attribute;
            this.state = State.Attribute;
        }

        public override int AttributeCount
        {
            get
            {
                if (this.attrCount < 0)
                {
                    // attribute count works for element, regardless of where you are in start tag
                    XPathNavigator tempNav = GetElemNav();
                    int count = 0;
                    if (null != tempNav)
                    {
                        if (tempNav.MoveToFirstNamespace(XPathNamespaceScope.Local))
                        {
                            do
                            {
                                count++;
                            } while (tempNav.MoveToNextNamespace((XPathNamespaceScope.Local)));
                            tempNav.MoveToParent();
                        }
                        if (tempNav.MoveToFirstAttribute())
                        {
                            do
                            {
                                count++;
                            } while (tempNav.MoveToNextAttribute());
                        }
                    }
                    this.attrCount = count;
                }
                return this.attrCount;
            }
        }

        public override string GetAttribute(string name)
        {
            // reader allows calling GetAttribute, even when positioned inside attributes
            XPathNavigator nav = this.nav;
            switch (nav.NodeType)
            {
                case XPathNodeType.Element:
                    break;
                case XPathNodeType.Attribute:
                    nav = nav.Clone();
                    if (!nav.MoveToParent())
                        return null;
                    break;
                default:
                    return null;
            }
            string prefix, localname;
            ValidateNames.SplitQName(name, out prefix, out localname);
            if (0 == prefix.Length)
            {
                if (localname == "xmlns")
                    return nav.GetNamespace(string.Empty);
                if ((object)nav == (object)this.nav)
                    nav = nav.Clone();
                if (nav.MoveToAttribute(localname, string.Empty))
                    return nav.Value;
            }
            else
            {
                if (prefix == "xmlns")
                    return nav.GetNamespace(localname);
                if ((object)nav == (object)this.nav)
                    nav = nav.Clone();
                if (nav.MoveToFirstAttribute())
                {
                    do
                    {
                        if (nav.LocalName == localname && nav.Prefix == prefix)
                            return nav.Value;
                    } while (nav.MoveToNextAttribute());
                }
            }
            return null;
        }

        public override string GetAttribute(string localName, string namespaceURI)
        {
            if (null == localName)
                throw new ArgumentNullException("localName");
            // reader allows calling GetAttribute, even when positioned inside attributes
            XPathNavigator nav = this.nav;
            switch (nav.NodeType)
            {
                case XPathNodeType.Element:
                    break;
                case XPathNodeType.Attribute:
                    nav = nav.Clone();
                    if (!nav.MoveToParent())
                        return null;
                    break;
                default:
                    return null;
            }
            // are they really looking for a namespace-decl?
            if (namespaceURI == XmlConst.ReservedNsXmlNs)
            {
                if (localName == "xmlns")
                    localName = string.Empty;
                return nav.GetNamespace(localName);
            }
            if (null == namespaceURI)
                namespaceURI = string.Empty;
            // We need to clone the navigator and move the clone to the attribute to see whether the attribute exists, 
            // because XPathNavigator.GetAttribute return string.Empty for both when the the attribute is not there or when 
            // it has an empty value. XmlReader.GetAttribute must return null if the attribute does not exist.
            if ((object)nav == (object)this.nav)
                nav = nav.Clone();
            if (nav.MoveToAttribute(localName, namespaceURI))
            {
                return nav.Value;
            }
            else
            {
                return null;
            }
        }

        private static string GetNamespaceByIndex(XPathNavigator nav, int index, out int count)
        {
            string thisValue = nav.Value;
            string value = null;
            if (nav.MoveToNextNamespace(XPathNamespaceScope.Local))
            {
                value = GetNamespaceByIndex(nav, index, out count);
            }
            else
            {
                count = 0;
            }
            if (count == index)
            {
                Debug.Assert(value == null);
                value = thisValue;
            }
            count++;
            return value;
        }

        public override string GetAttribute(int index)
        {
            if (index < 0)
                goto Error;
            XPathNavigator nav = GetElemNav();
            if (null == nav)
                goto Error;
            if (nav.MoveToFirstNamespace(XPathNamespaceScope.Local))
            {
                // namespaces are returned in reverse order, 
                // but we want to return them in the correct order,
                // so first count the namespaces
                int nsCount;
                string value = GetNamespaceByIndex(nav, index, out nsCount);
                if (null != value)
                {
                    return value;
                }
                index -= nsCount;
                nav.MoveToParent();
            }
            if (nav.MoveToFirstAttribute())
            {
                do
                {
                    if (index == 0)
                        return nav.Value;
                    index--;
                } while (nav.MoveToNextAttribute());
            }
        // can't find it... error
        Error:
            throw new ArgumentOutOfRangeException("index");
        }


        public override bool MoveToAttribute(string localName, string namespaceName)
        {
            if (null == localName)
                throw new ArgumentNullException("localName");
            int depth = this.depth;
            XPathNavigator nav = GetElemNav(out depth);
            if (null != nav)
            {
                if (namespaceName == XmlConst.ReservedNsXmlNs)
                {
                    if (localName == "xmlns")
                        localName = string.Empty;
                    if (nav.MoveToFirstNamespace(XPathNamespaceScope.Local))
                    {
                        do
                        {
                            if (nav.LocalName == localName)
                                goto FoundMatch;
                        } while (nav.MoveToNextNamespace(XPathNamespaceScope.Local));
                    }
                }
                else
                {
                    if (null == namespaceName)
                        namespaceName = string.Empty;
                    if (nav.MoveToAttribute(localName, namespaceName))
                        goto FoundMatch;
                }
            }
            return false;

        FoundMatch:
            if (state == State.InReadBinary)
            {
                readBinaryHelper.Finish();
                state = savedState;
            }
            MoveToAttr(nav, depth + 1);
            return true;
        }

        public override bool MoveToFirstAttribute()
        {
            int depth;
            XPathNavigator nav = GetElemNav(out depth);
            if (null != nav)
            {
                if (nav.MoveToFirstNamespace(XPathNamespaceScope.Local))
                {
                    // attributes are in reverse order
                    while (nav.MoveToNextNamespace(XPathNamespaceScope.Local))
                        ;
                    goto FoundMatch;
                }
                if (nav.MoveToFirstAttribute())
                {
                    goto FoundMatch;
                }
            }
            return false;
        FoundMatch:
            if (state == State.InReadBinary)
            {
                readBinaryHelper.Finish();
                state = savedState;
            }
            MoveToAttr(nav, depth + 1);
            return true;
        }

        public override bool MoveToNextAttribute()
        {
            switch (this.state)
            {
                case State.Content:
                    return MoveToFirstAttribute();

                case State.Attribute:
                    {
                        if (XPathNodeType.Attribute == this.nav.NodeType)
                            return this.nav.MoveToNextAttribute();

                        // otherwise it is on a namespace... namespace are in reverse order
                        Debug.Assert(XPathNodeType.Namespace == this.nav.NodeType);
                        XPathNavigator nav = this.nav.Clone();
                        if (!nav.MoveToParent())
                            return false; // shouldn't happen
                        if (!nav.MoveToFirstNamespace(XPathNamespaceScope.Local))
                            return false; // shouldn't happen
                        if (nav.IsSamePosition(this.nav))
                        {
                            // this was the last one... start walking attributes
                            nav.MoveToParent();
                            if (!nav.MoveToFirstAttribute())
                                return false;
                            // otherwise we are there
                            this.nav.MoveTo(nav);
                            return true;
                        }
                        else
                        {
                            XPathNavigator prev = nav.Clone();
                            for (;;)
                            {
                                if (!nav.MoveToNextNamespace(XPathNamespaceScope.Local))
                                {
                                    Debug.Assert(false, "Couldn't find Namespace Node! Should not happen!");
                                    return false;
                                }
                                if (nav.IsSamePosition(this.nav))
                                {
                                    this.nav.MoveTo(prev);
                                    return true;
                                }
                                prev.MoveTo(nav);
                            }
                            // found previous namespace position
                        }
                    }
                case State.AttrVal:
                    depth--;
                    this.state = State.Attribute;
                    if (!MoveToNextAttribute())
                    {
                        depth++;
                        this.state = State.AttrVal;
                        return false;
                    }
                    this.nodeType = XmlNodeType.Attribute;
                    return true;

                case State.InReadBinary:
                    state = savedState;
                    if (!MoveToNextAttribute())
                    {
                        state = State.InReadBinary;
                        return false;
                    }
                    readBinaryHelper.Finish();
                    return true;

                default:
                    return false;
            }
        }

        public override bool MoveToAttribute(string name)
        {
            int depth;
            XPathNavigator nav = GetElemNav(out depth);
            if (null == nav)
                return false;

            string prefix, localname;
            ValidateNames.SplitQName(name, out prefix, out localname);

            // watch for a namespace name
            bool IsXmlnsNoPrefix = false;
            if ((IsXmlnsNoPrefix = (0 == prefix.Length && localname == "xmlns"))
                || (prefix == "xmlns"))
            {
                if (IsXmlnsNoPrefix)
                    localname = string.Empty;
                if (nav.MoveToFirstNamespace(XPathNamespaceScope.Local))
                {
                    do
                    {
                        if (nav.LocalName == localname)
                            goto FoundMatch;
                    } while (nav.MoveToNextNamespace(XPathNamespaceScope.Local));
                }
            }
            else if (0 == prefix.Length)
            {
                // the empty prefix always means empty namespaceUri for attributes
                if (nav.MoveToAttribute(localname, string.Empty))
                    goto FoundMatch;
            }
            else
            {
                if (nav.MoveToFirstAttribute())
                {
                    do
                    {
                        if (nav.LocalName == localname && nav.Prefix == prefix)
                            goto FoundMatch;
                    } while (nav.MoveToNextAttribute());
                }
            }
            return false;

        FoundMatch:
            if (state == State.InReadBinary)
            {
                readBinaryHelper.Finish();
                state = savedState;
            }
            MoveToAttr(nav, depth + 1);
            return true;
        }

        public override bool MoveToElement()
        {
            switch (this.state)
            {
                case State.Attribute:
                case State.AttrVal:
                    if (!nav.MoveToParent())
                        return false;
                    this.depth--;
                    if (this.state == State.AttrVal)
                        this.depth--;
                    this.state = State.Content;
                    this.nodeType = XmlNodeType.Element;
                    return true;
                case State.InReadBinary:
                    state = savedState;
                    if (!MoveToElement())
                    {
                        state = State.InReadBinary;
                        return false;
                    }
                    readBinaryHelper.Finish();
                    break;
            }
            return false;
        }

        public override bool EOF
        {
            get
            {
                return this.state == State.EOF;
            }
        }

        public override ReadState ReadState
        {
            get
            {
                switch (this.state)
                {
                    case State.Initial:
                        return ReadState.Initial;
                    case State.Content:
                    case State.EndElement:
                    case State.Attribute:
                    case State.AttrVal:
                    case State.InReadBinary:
                        return ReadState.Interactive;
                    case State.EOF:
                        return ReadState.EndOfFile;
                    case State.Closed:
                        return ReadState.Closed;
                    default:
                        return ReadState.Error;
                }
            }
        }

        public override void ResolveEntity()
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation);
        }

        public override bool ReadAttributeValue()
        {
            if (state == State.InReadBinary)
            {
                readBinaryHelper.Finish();
                state = savedState;
            }
            if (this.state == State.Attribute)
            {
                this.state = State.AttrVal;
                this.nodeType = XmlNodeType.Text;
                this.depth++;
                return true;
            }
            return false;
        }

        public override bool CanReadBinaryContent
        {
            get
            {
                return true;
            }
        }

        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (state != State.InReadBinary)
            {
                readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(readBinaryHelper, this);
                savedState = state;
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            state = savedState;

            // call to the helper
            int readCount = readBinaryHelper.ReadContentAsBase64(buffer, index, count);

            // turn on InReadBinary state again and return
            savedState = state;
            state = State.InReadBinary;
            return readCount;
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (state != State.InReadBinary)
            {
                readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(readBinaryHelper, this);
                savedState = state;
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            state = savedState;

            // call to the helper
            int readCount = readBinaryHelper.ReadContentAsBinHex(buffer, index, count);

            // turn on InReadBinary state again and return
            savedState = state;
            state = State.InReadBinary;
            return readCount;
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (state != State.InReadBinary)
            {
                readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(readBinaryHelper, this);
                savedState = state;
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            state = savedState;

            // call to the helper
            int readCount = readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);

            // turn on InReadBinary state again and return
            savedState = state;
            state = State.InReadBinary;
            return readCount;
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (state != State.InReadBinary)
            {
                readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(readBinaryHelper, this);
                savedState = state;
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            state = savedState;

            // call to the helper
            int readCount = readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);

            // turn on InReadBinary state again and return
            savedState = state;
            state = State.InReadBinary;
            return readCount;
        }

        public override string LookupNamespace(string prefix)
        {
            return this.nav.LookupNamespace(prefix);
        }

        /// <summary>
        /// Current depth in subtree.
        /// </summary>
        public override int Depth
        {
            get { return this.depth; }
        }

        /// <summary>
        /// Move to the next reader state.  Return false if that is ReaderState.Closed.
        /// </summary>
        public override bool Read()
        {
            this.attrCount = -1;
            switch (this.state)
            {
                case State.Error:
                case State.Closed:
                case State.EOF:
                    return false;

                case State.Initial:
                    // Starting state depends on the navigator's item type
                    this.nav = this.navToRead;
                    this.state = State.Content;
                    if (XPathNodeType.Root == this.nav.NodeType)
                    {
                        if (!nav.MoveToFirstChild())
                        {
                            SetEOF();
                            return false;
                        }
                        this.readEntireDocument = true;
                    }
                    else if (XPathNodeType.Attribute == this.nav.NodeType)
                    {
                        this.state = State.Attribute;
                    }
                    this.nodeType = ToXmlNodeType(this.nav.NodeType);
                    break;

                case State.Content:
                    if (this.nav.MoveToFirstChild())
                    {
                        this.nodeType = ToXmlNodeType(this.nav.NodeType);
                        this.depth++;
                        this.state = State.Content;
                    }
                    else if (this.nodeType == XmlNodeType.Element
                        && !this.nav.IsEmptyElement)
                    {
                        this.nodeType = XmlNodeType.EndElement;
                        this.state = State.EndElement;
                    }
                    else
                        goto case State.EndElement;
                    break;

                case State.EndElement:
                    if (0 == depth && !this.readEntireDocument)
                    {
                        SetEOF();
                        return false;
                    }
                    else if (this.nav.MoveToNext())
                    {
                        this.nodeType = ToXmlNodeType(this.nav.NodeType);
                        this.state = State.Content;
                    }
                    else if (depth > 0 && this.nav.MoveToParent())
                    {
                        Debug.Assert(this.nav.NodeType == XPathNodeType.Element, this.nav.NodeType.ToString() + " == XPathNodeType.Element");
                        this.nodeType = XmlNodeType.EndElement;
                        this.state = State.EndElement;
                        depth--;
                    }
                    else
                    {
                        SetEOF();
                        return false;
                    }
                    break;

                case State.Attribute:
                case State.AttrVal:
                    if (!this.nav.MoveToParent())
                    {
                        SetEOF();
                        return false;
                    }
                    this.nodeType = ToXmlNodeType(this.nav.NodeType);
                    this.depth--;
                    if (state == State.AttrVal)
                        this.depth--;
                    goto case State.Content;
                case State.InReadBinary:
                    state = savedState;
                    readBinaryHelper.Finish();
                    return Read();
            }
            return true;
        }

        /// <summary>
        /// set reader to EOF state
        /// </summary>
        private void SetEOF()
        {
            this.nav = XmlEmptyNavigator.Singleton;
            this.nodeType = XmlNodeType.None;
            this.state = State.EOF;
            this.depth = 0;
        }
    }

    /// <summary>
    /// The XmlEmptyNavigator exposes a document node with no children.
    /// Only one XmlEmptyNavigator exists per AppDomain (Singleton).  That's why the constructor is private.
    /// Use the Singleton property to get the EmptyNavigator.
    /// </summary>
    internal class XmlEmptyNavigator : XPathNavigator
    {
        private static volatile XmlEmptyNavigator singleton;

        private XmlEmptyNavigator()
        {
        }

        public static XmlEmptyNavigator Singleton
        {
            get
            {
                if (XmlEmptyNavigator.singleton == null)
                    XmlEmptyNavigator.singleton = new XmlEmptyNavigator();
                return XmlEmptyNavigator.singleton;
            }
        }

        //-----------------------------------------------
        // XmlReader
        //-----------------------------------------------

        public override XPathNodeType NodeType
        {
            get { return XPathNodeType.All; }
        }

        public override string NamespaceURI
        {
            get { return string.Empty; }
        }

        public override string LocalName
        {
            get { return string.Empty; }
        }

        public override string Name
        {
            get { return string.Empty; }
        }

        public override string Prefix
        {
            get { return string.Empty; }
        }

        public override string BaseURI
        {
            get { return string.Empty; }
        }

        public override string Value
        {
            get { return string.Empty; }
        }

        public override bool IsEmptyElement
        {
            get { return false; }
        }

        public override string XmlLang
        {
            get { return string.Empty; }
        }

        public override bool HasAttributes
        {
            get { return false; }
        }

        public override bool HasChildren
        {
            get { return false; }
        }


        //-----------------------------------------------
        // IXmlNamespaceResolver
        //-----------------------------------------------

        public override XmlNameTable NameTable
        {
            get { return new NameTable(); }
        }

        public override bool MoveToFirstChild()
        {
            return false;
        }

        public override void MoveToRoot()
        {
            //always on root
            return;
        }

        public override bool MoveToNext()
        {
            return false;
        }

        public override bool MoveToPrevious()
        {
            return false;
        }

        public override bool MoveToFirst()
        {
            return false;
        }

        public override bool MoveToFirstAttribute()
        {
            return false;
        }

        public override bool MoveToNextAttribute()
        {
            return false;
        }

        public override bool MoveToId(string id)
        {
            return false;
        }

        public override string GetAttribute(string localName, string namespaceName)
        {
            return null;
        }

        public override bool MoveToAttribute(string localName, string namespaceName)
        {
            return false;
        }

        public override string GetNamespace(string name)
        {
            return null;
        }

        public override bool MoveToNamespace(string prefix)
        {
            return false;
        }


        public override bool MoveToFirstNamespace(XPathNamespaceScope scope)
        {
            return false;
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope scope)
        {
            return false;
        }

        public override bool MoveToParent()
        {
            return false;
        }

        public override bool MoveTo(XPathNavigator other)
        {
            // Only one instance of XmlEmptyNavigator exists on the system
            return (object)this == (object)other;
        }

        public override XmlNodeOrder ComparePosition(XPathNavigator other)
        {
            // Only one instance of XmlEmptyNavigator exists on the system
            return ((object)this == (object)other) ? XmlNodeOrder.Same : XmlNodeOrder.Unknown;
        }

        public override bool IsSamePosition(XPathNavigator other)
        {
            // Only one instance of XmlEmptyNavigator exists on the system
            return (object)this == (object)other;
        }


        //-----------------------------------------------
        // XPathNavigator2
        //-----------------------------------------------
        public override XPathNavigator Clone()
        {
            // Singleton, so clone just returns this
            return this;
        }
    }
}
