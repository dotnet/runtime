// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml.Schema;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;


namespace System.Xml.XPath
{
    /// <summary>
    /// Reader that traverses the subtree rooted at the current position of the specified navigator.
    /// </summary>
    internal class XPathNavigatorReader : XmlReader, IXmlNamespaceResolver
    {
        private enum State
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

        private XPathNavigator _nav;
        private XPathNavigator _navToRead;
        private int _depth;
        private State _state;
        private XmlNodeType _nodeType;
        private int _attrCount;
        private bool _readEntireDocument;

        protected IXmlLineInfo lineInfo;
        protected IXmlSchemaInfo schemaInfo;

        private ReadContentAsBinaryHelper _readBinaryHelper;
        private State _savedState;

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

        internal object UnderlyingObject
        {
            get
            {
                return _nav.UnderlyingObject;
            }
        }

        public static XPathNavigatorReader Create(XPathNavigator navToRead)
        {
            XPathNavigator nav = navToRead.Clone();
            IXmlLineInfo xli = nav as IXmlLineInfo;
            IXmlSchemaInfo xsi = nav as IXmlSchemaInfo;
#if NAVREADER_SUPPORTSLINEINFO
            if (null == xsi) {
                if (null == xli) {
                    return new XPathNavigatorReader(nav, xli, xsi);
                }
                else {
                    return new XPathNavigatorReaderWithLI(nav, xli, xsi);
                }
            }
            else {
                if (null == xli) {
                    return new XPathNavigatorReaderWithSI(nav, xli, xsi);
                }
                else {
                    return new XPathNavigatorReaderWithLIAndSI(nav, xli, xsi);
                }
            }
#else
            if (null == xsi)
            {
                return new XPathNavigatorReader(nav, xli, xsi);
            }
            else
            {
                return new XPathNavigatorReaderWithSI(nav, xli, xsi);
            }
#endif
        }

        protected XPathNavigatorReader(XPathNavigator navToRead, IXmlLineInfo xli, IXmlSchemaInfo xsi)
        {
            // Need clone that can be moved independently of original navigator
            _navToRead = navToRead;
            this.lineInfo = xli;
            this.schemaInfo = xsi;
            _nav = XmlEmptyNavigator.Singleton;
            _state = State.Initial;
            _depth = 0;
            _nodeType = XPathNavigatorReader.ToXmlNodeType(_nav.NodeType);
        }

        protected bool IsReading
        {
            get { return _state > State.Initial && _state < State.EOF; }
        }

        internal override XmlNamespaceManager NamespaceManager
        {
            get { return XPathNavigator.GetNamespaces(this); }
        }


        //-----------------------------------------------
        // IXmlNamespaceResolver -- pass through to Navigator
        //-----------------------------------------------
        public override XmlNameTable NameTable
        {
            get
            {
                return _navToRead.NameTable;
            }
        }

        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return _nav.GetNamespacesInScope(scope);
        }

        string IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return _nav.LookupNamespace(prefix);
        }

        string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return _nav.LookupPrefix(namespaceName);
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
                rs.ReadOnly = true;
                return rs;
            }
        }

        public override IXmlSchemaInfo SchemaInfo
        {
            get
            {
                // Special case attribute text (this.nav points to attribute even though current state is Text)
                if (_nodeType == XmlNodeType.Text)
                    return null;
                return _nav.SchemaInfo;
            }
        }

        public override System.Type ValueType
        {
            get { return _nav.ValueType; }
        }

        public override XmlNodeType NodeType
        {
            get { return _nodeType; }
        }

        public override string NamespaceURI
        {
            get
            {
                //NamespaceUri for namespace nodes is different in case of XPathNavigator and Reader
                if (_nav.NodeType == XPathNodeType.Namespace)
                    return this.NameTable.Add(XmlReservedNs.NsXmlNs);
                //Special case attribute text node
                if (this.NodeType == XmlNodeType.Text)
                    return string.Empty;
                return _nav.NamespaceURI;
            }
        }

        public override string LocalName
        {
            get
            {
                //Default namespace in case of reader has a local name value of 'xmlns'
                if (_nav.NodeType == XPathNodeType.Namespace && _nav.LocalName.Length == 0)
                    return this.NameTable.Add("xmlns");
                //Special case attribute text node
                if (this.NodeType == XmlNodeType.Text)
                    return string.Empty;
                return _nav.LocalName;
            }
        }

        public override string Prefix
        {
            get
            {
                //Prefix for namespace nodes is different in case of XPathNavigator and Reader
                if (_nav.NodeType == XPathNodeType.Namespace && _nav.LocalName.Length != 0)
                    return this.NameTable.Add("xmlns");
                //Special case attribute text node
                if (this.NodeType == XmlNodeType.Text)
                    return string.Empty;
                return _nav.Prefix;
            }
        }

        public override string BaseURI
        {
            get
            {
                //reader returns BaseUri even before read method is called.
                if (_state == State.Initial)
                    return _navToRead.BaseURI;
                return _nav.BaseURI;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                return _nav.IsEmptyElement;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                XPathNavigator tempNav = _nav.Clone();
                do
                {
                    if (tempNav.MoveToAttribute(XPathNavigatorReader.space, XmlReservedNs.NsXml))
                    {
                        switch (XmlConvert.TrimString(tempNav.Value))
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
                return _nav.XmlLang;
            }
        }

        public override bool HasValue
        {
            get
            {
                if ((_nodeType != XmlNodeType.Element)
                    && (_nodeType != XmlNodeType.Document)
                    && (_nodeType != XmlNodeType.EndElement)
                    && (_nodeType != XmlNodeType.None))
                    return true;
                return false;
            }
        }

        public override string Value
        {
            get
            {
                if ((_nodeType != XmlNodeType.Element)
                    && (_nodeType != XmlNodeType.Document)
                    && (_nodeType != XmlNodeType.EndElement)
                    && (_nodeType != XmlNodeType.None))
                    return _nav.Value;
                return string.Empty;
            }
        }

        private XPathNavigator GetElemNav()
        {
            XPathNavigator tempNav;
            switch (_state)
            {
                case State.Content:
                    return _nav.Clone();
                case State.Attribute:
                case State.AttrVal:
                    tempNav = _nav.Clone();
                    if (tempNav.MoveToParent())
                        return tempNav;
                    break;
                case State.InReadBinary:
                    _state = _savedState;
                    XPathNavigator nav = GetElemNav();
                    _state = State.InReadBinary;
                    return nav;
            }
            return null;
        }

        private XPathNavigator GetElemNav(out int depth)
        {
            XPathNavigator nav = null;
            switch (_state)
            {
                case State.Content:
                    if (_nodeType == XmlNodeType.Element)
                        nav = _nav.Clone();
                    depth = _depth;
                    break;
                case State.Attribute:
                    nav = _nav.Clone();
                    nav.MoveToParent();
                    depth = _depth - 1;
                    break;
                case State.AttrVal:
                    nav = _nav.Clone();
                    nav.MoveToParent();
                    depth = _depth - 2;
                    break;
                case State.InReadBinary:
                    _state = _savedState;
                    nav = GetElemNav(out depth);
                    _state = State.InReadBinary;
                    break;
                default:
                    depth = _depth;
                    break;
            }
            return nav;
        }

        private void MoveToAttr(XPathNavigator nav, int depth)
        {
            _nav.MoveTo(nav);
            _depth = depth;
            _nodeType = XmlNodeType.Attribute;
            _state = State.Attribute;
        }

        public override int AttributeCount
        {
            get
            {
                if (_attrCount < 0)
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
                    _attrCount = count;
                }
                return _attrCount;
            }
        }

        public override string GetAttribute(string name)
        {
            // reader allows calling GetAttribute, even when positioned inside attributes
            XPathNavigator nav = _nav;
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
                if ((object)nav == (object)_nav)
                    nav = nav.Clone();
                if (nav.MoveToAttribute(localname, string.Empty))
                    return nav.Value;
            }
            else
            {
                if (prefix == "xmlns")
                    return nav.GetNamespace(localname);
                if ((object)nav == (object)_nav)
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
                throw new ArgumentNullException(nameof(localName));
            // reader allows calling GetAttribute, even when positioned inside attributes
            XPathNavigator nav = _nav;
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
            if (namespaceURI == XmlReservedNs.NsXmlNs)
            {
                if (localName == "xmlns")
                    localName = string.Empty;
                return nav.GetNamespace(localName);
            }
            if (null == namespaceURI)
                namespaceURI = string.Empty;
            // We need to clone the navigator and move the clone to the attribute to see whether the attribute exists, 
            // because XPathNavigator.GetAttribute return string.Empty for both when the attribute is not there or when 
            // it has an empty value. XmlReader.GetAttribute must return null if the attribute does not exist.
            if ((object)nav == (object)_nav)
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
            throw new ArgumentOutOfRangeException(nameof(index));
        }


        public override bool MoveToAttribute(string localName, string namespaceName)
        {
            if (null == localName)
                throw new ArgumentNullException(nameof(localName));
            int depth = _depth;
            XPathNavigator nav = GetElemNav(out depth);
            if (null != nav)
            {
                if (namespaceName == XmlReservedNs.NsXmlNs)
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
            if (_state == State.InReadBinary)
            {
                _readBinaryHelper.Finish();
                _state = _savedState;
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
            if (_state == State.InReadBinary)
            {
                _readBinaryHelper.Finish();
                _state = _savedState;
            }
            MoveToAttr(nav, depth + 1);
            return true;
        }

        public override bool MoveToNextAttribute()
        {
            switch (_state)
            {
                case State.Content:
                    return MoveToFirstAttribute();

                case State.Attribute:
                    {
                        if (XPathNodeType.Attribute == _nav.NodeType)
                            return _nav.MoveToNextAttribute();

                        // otherwise it is on a namespace... namespace are in reverse order
                        Debug.Assert(XPathNodeType.Namespace == _nav.NodeType);
                        XPathNavigator nav = _nav.Clone();
                        if (!nav.MoveToParent())
                            return false; // shouldn't happen
                        if (!nav.MoveToFirstNamespace(XPathNamespaceScope.Local))
                            return false; // shouldn't happen
                        if (nav.IsSamePosition(_nav))
                        {
                            // this was the last one... start walking attributes
                            nav.MoveToParent();
                            if (!nav.MoveToFirstAttribute())
                                return false;
                            // otherwise we are there
                            _nav.MoveTo(nav);
                            return true;
                        }
                        else
                        {
                            XPathNavigator prev = nav.Clone();
                            for (;;)
                            {
                                if (!nav.MoveToNextNamespace(XPathNamespaceScope.Local))
                                {
                                    Debug.Fail("Couldn't find Namespace Node! Should not happen!");
                                    return false;
                                }
                                if (nav.IsSamePosition(_nav))
                                {
                                    _nav.MoveTo(prev);
                                    return true;
                                }
                                prev.MoveTo(nav);
                            }
                            // found previous namespace position
                        }
                    }
                case State.AttrVal:
                    _depth--;
                    _state = State.Attribute;
                    if (!MoveToNextAttribute())
                    {
                        _depth++;
                        _state = State.AttrVal;
                        return false;
                    }
                    _nodeType = XmlNodeType.Attribute;
                    return true;

                case State.InReadBinary:
                    _state = _savedState;
                    if (!MoveToNextAttribute())
                    {
                        _state = State.InReadBinary;
                        return false;
                    }
                    _readBinaryHelper.Finish();
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
            if (_state == State.InReadBinary)
            {
                _readBinaryHelper.Finish();
                _state = _savedState;
            }
            MoveToAttr(nav, depth + 1);
            return true;
        }

        public override bool MoveToElement()
        {
            switch (_state)
            {
                case State.Attribute:
                case State.AttrVal:
                    if (!_nav.MoveToParent())
                        return false;
                    _depth--;
                    if (_state == State.AttrVal)
                        _depth--;
                    _state = State.Content;
                    _nodeType = XmlNodeType.Element;
                    return true;
                case State.InReadBinary:
                    _state = _savedState;
                    if (!MoveToElement())
                    {
                        _state = State.InReadBinary;
                        return false;
                    }
                    _readBinaryHelper.Finish();
                    break;
            }
            return false;
        }

        public override bool EOF
        {
            get
            {
                return _state == State.EOF;
            }
        }

        public override ReadState ReadState
        {
            get
            {
                switch (_state)
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
            if (_state == State.InReadBinary)
            {
                _readBinaryHelper.Finish();
                _state = _savedState;
            }
            if (_state == State.Attribute)
            {
                _state = State.AttrVal;
                _nodeType = XmlNodeType.Text;
                _depth++;
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
            if (_state != State.InReadBinary)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _state;
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = _savedState;

            // call to the helper
            int readCount = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);

            // turn on InReadBinary state again and return
            _savedState = _state;
            _state = State.InReadBinary;
            return readCount;
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_state != State.InReadBinary)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _state;
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = _savedState;

            // call to the helper
            int readCount = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);

            // turn on InReadBinary state again and return
            _savedState = _state;
            _state = State.InReadBinary;
            return readCount;
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_state != State.InReadBinary)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _state;
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = _savedState;

            // call to the helper
            int readCount = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);

            // turn on InReadBinary state again and return
            _savedState = _state;
            _state = State.InReadBinary;
            return readCount;
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_state != State.InReadBinary)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _state;
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = _savedState;

            // call to the helper
            int readCount = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);

            // turn on InReadBinary state again and return
            _savedState = _state;
            _state = State.InReadBinary;
            return readCount;
        }

        public override string LookupNamespace(string prefix)
        {
            return _nav.LookupNamespace(prefix);
        }

        /// <summary>
        /// Current depth in subtree.
        /// </summary>
        public override int Depth
        {
            get { return _depth; }
        }

        /// <summary>
        /// Move to the next reader state.  Return false if that is ReaderState.Closed.
        /// </summary>
        public override bool Read()
        {
            _attrCount = -1;
            switch (_state)
            {
                case State.Error:
                case State.Closed:
                case State.EOF:
                    return false;

                case State.Initial:
                    // Starting state depends on the navigator's item type
                    _nav = _navToRead;
                    _state = State.Content;
                    if (XPathNodeType.Root == _nav.NodeType)
                    {
                        if (!_nav.MoveToFirstChild())
                        {
                            SetEOF();
                            return false;
                        }
                        _readEntireDocument = true;
                    }
                    else if (XPathNodeType.Attribute == _nav.NodeType)
                    {
                        _state = State.Attribute;
                    }
                    _nodeType = ToXmlNodeType(_nav.NodeType);
                    break;

                case State.Content:
                    if (_nav.MoveToFirstChild())
                    {
                        _nodeType = ToXmlNodeType(_nav.NodeType);
                        _depth++;
                        _state = State.Content;
                    }
                    else if (_nodeType == XmlNodeType.Element
                        && !_nav.IsEmptyElement)
                    {
                        _nodeType = XmlNodeType.EndElement;
                        _state = State.EndElement;
                    }
                    else
                        goto case State.EndElement;
                    break;

                case State.EndElement:
                    if (0 == _depth && !_readEntireDocument)
                    {
                        SetEOF();
                        return false;
                    }
                    else if (_nav.MoveToNext())
                    {
                        _nodeType = ToXmlNodeType(_nav.NodeType);
                        _state = State.Content;
                    }
                    else if (_depth > 0 && _nav.MoveToParent())
                    {
                        Debug.Assert(_nav.NodeType == XPathNodeType.Element, _nav.NodeType.ToString() + " == XPathNodeType.Element");
                        _nodeType = XmlNodeType.EndElement;
                        _state = State.EndElement;
                        _depth--;
                    }
                    else
                    {
                        SetEOF();
                        return false;
                    }
                    break;

                case State.Attribute:
                case State.AttrVal:
                    if (!_nav.MoveToParent())
                    {
                        SetEOF();
                        return false;
                    }
                    _nodeType = ToXmlNodeType(_nav.NodeType);
                    _depth--;
                    if (_state == State.AttrVal)
                        _depth--;
                    goto case State.Content;
                case State.InReadBinary:
                    _state = _savedState;
                    _readBinaryHelper.Finish();
                    return Read();
            }
            return true;
        }


        /// <summary>
        /// End reading by transitioning into the Closed state.
        /// </summary>
        public override void Close()
        {
            _nav = XmlEmptyNavigator.Singleton;
            _nodeType = XmlNodeType.None;
            _state = State.Closed;
            _depth = 0;
        }

        /// <summary>
        /// set reader to EOF state
        /// </summary>
        private void SetEOF()
        {
            _nav = XmlEmptyNavigator.Singleton;
            _nodeType = XmlNodeType.None;
            _state = State.EOF;
            _depth = 0;
        }
    }

#if NAVREADER_SUPPORTSLINEINFO
    internal class XPathNavigatorReaderWithLI : XPathNavigatorReader, System.Xml.IXmlLineInfo {
        internal XPathNavigatorReaderWithLI( XPathNavigator navToRead, IXmlLineInfo xli, IXmlSchemaInfo xsi ) 
            : base( navToRead, xli, xsi ) {
        }

        //-----------------------------------------------
        // IXmlLineInfo
        //-----------------------------------------------

        public virtual bool HasLineInfo() { return IsReading ? this.lineInfo.HasLineInfo() : false; }
        public virtual int LineNumber { get { return IsReading ? this.lineInfo.LineNumber : 0; } }
        public virtual int LinePosition { get { return IsReading ? this.lineInfo.LinePosition : 0; } }
    }

    internal class XPathNavigatorReaderWithLIAndSI : XPathNavigatorReaderWithLI, System.Xml.IXmlLineInfo, System.Xml.Schema.IXmlSchemaInfo {
        internal XPathNavigatorReaderWithLIAndSI( XPathNavigator navToRead, IXmlLineInfo xli, IXmlSchemaInfo xsi ) 
            : base( navToRead, xli, xsi ) {
        }

        //-----------------------------------------------
        // IXmlSchemaInfo
        //-----------------------------------------------

        public virtual XmlSchemaValidity Validity { get { return IsReading ? this.schemaInfo.Validity : XmlSchemaValidity.NotKnown; } }
        public override bool IsDefault { get { return IsReading ? this.schemaInfo.IsDefault : false; } }
        public virtual bool IsNil { get { return IsReading ? this.schemaInfo.IsNil : false; } }
        public virtual XmlSchemaSimpleType MemberType { get { return IsReading ? this.schemaInfo.MemberType : null; } }
        public virtual XmlSchemaType SchemaType { get { return IsReading ? this.schemaInfo.SchemaType : null; } }
        public virtual XmlSchemaElement SchemaElement { get { return IsReading ? this.schemaInfo.SchemaElement : null; } }
        public virtual XmlSchemaAttribute SchemaAttribute { get { return IsReading ? this.schemaInfo.SchemaAttribute : null; } }
    }
#endif

    internal class XPathNavigatorReaderWithSI : XPathNavigatorReader, System.Xml.Schema.IXmlSchemaInfo
    {
        internal XPathNavigatorReaderWithSI(XPathNavigator navToRead, IXmlLineInfo xli, IXmlSchemaInfo xsi)
            : base(navToRead, xli, xsi)
        {
        }

        //-----------------------------------------------
        // IXmlSchemaInfo
        //-----------------------------------------------

        public virtual XmlSchemaValidity Validity { get { return IsReading ? this.schemaInfo.Validity : XmlSchemaValidity.NotKnown; } }
        public override bool IsDefault { get { return IsReading ? this.schemaInfo.IsDefault : false; } }
        public virtual bool IsNil { get { return IsReading ? this.schemaInfo.IsNil : false; } }
        public virtual XmlSchemaSimpleType MemberType { get { return IsReading ? this.schemaInfo.MemberType : null; } }
        public virtual XmlSchemaType SchemaType { get { return IsReading ? this.schemaInfo.SchemaType : null; } }
        public virtual XmlSchemaElement SchemaElement { get { return IsReading ? this.schemaInfo.SchemaElement : null; } }
        public virtual XmlSchemaAttribute SchemaAttribute { get { return IsReading ? this.schemaInfo.SchemaAttribute : null; } }
    }

    /// <summary>
    /// The XmlEmptyNavigator exposes a document node with no children.
    /// Only one XmlEmptyNavigator exists per AppDomain (Singleton).  That's why the constructor is private.
    /// Use the Singleton property to get the EmptyNavigator.
    /// </summary>
    internal class XmlEmptyNavigator : XPathNavigator
    {
        private static volatile XmlEmptyNavigator s_singleton;

        private XmlEmptyNavigator()
        {
        }

        public static XmlEmptyNavigator Singleton
        {
            get
            {
                if (XmlEmptyNavigator.s_singleton == null)
                    XmlEmptyNavigator.s_singleton = new XmlEmptyNavigator();
                return XmlEmptyNavigator.s_singleton;
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
