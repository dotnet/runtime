// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Schema;

namespace System.Xml
{
    internal enum DocumentXmlWriterType
    {
        InsertSiblingAfter,
        InsertSiblingBefore,
        PrependChild,
        AppendChild,
        AppendAttribute,
        ReplaceToFollowingSibling,
    }

    // Implements a XmlWriter that augments a XmlDocument.
    internal sealed class DocumentXmlWriter : XmlRawWriter, IXmlNamespaceResolver
    {
        private enum State
        {
            Error,
            Attribute,
            Prolog,
            Fragment,
            Content,

            Last, // always last
        }

        private enum Method
        {
            WriteXmlDeclaration,
            WriteStartDocument,
            WriteEndDocument,
            WriteDocType,
            WriteStartElement,
            WriteEndElement,
            WriteFullEndElement,
            WriteStartAttribute,
            WriteEndAttribute,
            WriteStartNamespaceDeclaration,
            WriteEndNamespaceDeclaration,
            WriteCData,
            WriteComment,
            WriteProcessingInstruction,
            WriteEntityRef,
            WriteWhitespace,
            WriteString,
        }

        private readonly DocumentXmlWriterType _type; // writer type
        private readonly XmlNode _start; // context node
        private readonly XmlDocument _document; // context document
        private XmlNamespaceManager? _namespaceManager; // context namespace manager
        private State _state; // current state
        private XmlNode? _write; // current node
        private readonly List<XmlNode> _fragment; // top level node cache
        private readonly XmlWriterSettings _settings; // wrapping writer settings
        private DocumentXPathNavigator? _navigator; // context for replace
        private XmlNode? _end; // context for replace

        public DocumentXmlWriter(DocumentXmlWriterType type, XmlNode start, XmlDocument document)
        {
            _type = type;
            _start = start;
            _document = document;

            _state = StartState();
            _fragment = new List<XmlNode>();
            _settings = new XmlWriterSettings();
            _settings.ReadOnly = false;
            _settings.CheckCharacters = false;
            _settings.CloseOutput = false;
            _settings.ConformanceLevel = (_state == State.Prolog ? ConformanceLevel.Document : ConformanceLevel.Fragment);
            _settings.ReadOnly = true;
        }

        public XmlNamespaceManager? NamespaceManager
        {
            set
            {
                _namespaceManager = value;
            }
        }

        public override XmlWriterSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        public DocumentXPathNavigator? Navigator
        {
            set
            {
                _navigator = value;
            }
        }

        public XmlNode EndNode
        {
            set
            {
                _end = value;
            }
        }

        internal override void WriteXmlDeclaration(XmlStandalone standalone)
        {
            VerifyState(Method.WriteXmlDeclaration);
            if (standalone != XmlStandalone.Omit)
            {
                XmlNode node = _document.CreateXmlDeclaration("1.0", string.Empty, standalone == XmlStandalone.Yes ? "yes" : "no");
                AddChild(node, _write);
            }
        }

        internal override void WriteXmlDeclaration(string xmldecl)
        {
            VerifyState(Method.WriteXmlDeclaration);
            string? version, encoding, standalone;
            XmlLoader.ParseXmlDeclarationValue(xmldecl, out version, out encoding, out standalone);
            XmlNode node = _document.CreateXmlDeclaration(version!, encoding, standalone);
            AddChild(node, _write);
        }

        public override void WriteStartDocument()
        {
            VerifyState(Method.WriteStartDocument);
        }

        public override void WriteStartDocument(bool standalone)
        {
            VerifyState(Method.WriteStartDocument);
        }

        public override void WriteEndDocument()
        {
            VerifyState(Method.WriteEndDocument);
        }

        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset)
        {
            VerifyState(Method.WriteDocType);
            XmlNode node = _document.CreateDocumentType(name, pubid, sysid, subset);
            AddChild(node, _write);
        }

        public override void WriteStartElement(string? prefix, string localName, string? ns)
        {
            VerifyState(Method.WriteStartElement);
            XmlNode node = _document.CreateElement(prefix, localName, ns);
            AddChild(node, _write);
            _write = node;
        }

        public override void WriteEndElement()
        {
            VerifyState(Method.WriteEndElement);
            if (_write == null)
            {
                throw new InvalidOperationException();
            }

            _write = _write.ParentNode;
        }

        internal override void WriteEndElement(string prefix, string localName, string ns)
        {
            WriteEndElement();
        }

        public override void WriteFullEndElement()
        {
            VerifyState(Method.WriteFullEndElement);
            XmlElement? elem = _write as XmlElement;
            if (elem == null)
            {
                throw new InvalidOperationException();
            }

            elem.IsEmpty = false;
            _write = elem.ParentNode;
        }

        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
            WriteFullEndElement();
        }

        internal override void StartElementContent()
        {
            // nop
        }

        public override void WriteStartAttribute(string? prefix, string localName, string? ns)
        {
            VerifyState(Method.WriteStartAttribute);
            XmlAttribute attr = _document.CreateAttribute(prefix, localName, ns);
            AddAttribute(attr, _write);
            _write = attr;
        }

        public override void WriteEndAttribute()
        {
            VerifyState(Method.WriteEndAttribute);
            XmlAttribute? attr = _write as XmlAttribute;
            if (attr == null)
            {
                throw new InvalidOperationException();
            }

            if (!attr.HasChildNodes)
            {
                XmlNode node = _document.CreateTextNode(string.Empty);
                AddChild(node, attr);
            }

            _write = attr.OwnerElement;
        }

        internal override void WriteNamespaceDeclaration(string prefix, string ns)
        {
            this.WriteStartNamespaceDeclaration(prefix);
            this.WriteString(ns);
            this.WriteEndNamespaceDeclaration();
        }

        internal override bool SupportsNamespaceDeclarationInChunks
        {
            get
            {
                return true;
            }
        }

        internal override void WriteStartNamespaceDeclaration(string prefix)
        {
            VerifyState(Method.WriteStartNamespaceDeclaration);
            XmlAttribute attr;
            if (prefix.Length == 0)
            {
                attr = _document.CreateAttribute(prefix, _document.strXmlns, _document.strReservedXmlns);
            }
            else
            {
                attr = _document.CreateAttribute(_document.strXmlns, prefix, _document.strReservedXmlns);
            }

            AddAttribute(attr, _write);
            _write = attr;
        }

        internal override void WriteEndNamespaceDeclaration()
        {
            VerifyState(Method.WriteEndNamespaceDeclaration);
            XmlAttribute? attr = _write as XmlAttribute;
            if (attr == null)
            {
                throw new InvalidOperationException();
            }

            if (!attr.HasChildNodes)
            {
                XmlNode node = _document.CreateTextNode(string.Empty);
                AddChild(node, attr);
            }

            _write = attr.OwnerElement;
        }

        public override void WriteCData(string? text)
        {
            VerifyState(Method.WriteCData);
            XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
            XmlNode node = _document.CreateCDataSection(text);
            AddChild(node, _write);
        }

        public override void WriteComment(string? text)
        {
            VerifyState(Method.WriteComment);
            XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
            XmlNode node = _document.CreateComment(text);
            AddChild(node, _write);
        }

        public override void WriteProcessingInstruction(string name, string? text)
        {
            VerifyState(Method.WriteProcessingInstruction);
            XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
            XmlNode node = _document.CreateProcessingInstruction(name, text!);
            AddChild(node, _write);
        }

        public override void WriteEntityRef(string name)
        {
            VerifyState(Method.WriteEntityRef);
            XmlNode node = _document.CreateEntityReference(name);
            AddChild(node, _write);
            // REVIEW: the namespace scope is incorrect(?) unless write == null
        }

        public override void WriteCharEntity(char ch)
        {
            WriteString(char.ToString(ch));
        }

        public override void WriteWhitespace(string? text)
        {
            VerifyState(Method.WriteWhitespace);
            XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
            if (_document.PreserveWhitespace)
            {
                XmlNode node = _document.CreateWhitespace(text);
                AddChild(node, _write);
            }
        }

        public override void WriteString(string? text)
        {
            VerifyState(Method.WriteString);
            XmlConvert.VerifyCharData(text, ExceptionType.ArgumentException);
            XmlNode node = _document.CreateTextNode(text);
            AddChild(node, _write);
        }

        public override void WriteSurrogateCharEntity(char lowCh, char highCh)
        {
            ReadOnlySpan<char> entity = stackalloc char[] { highCh, lowCh };
            WriteString(new string(entity));
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            WriteString(new string(buffer, index, count));
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            WriteString(new string(buffer, index, count));
        }

        public override void WriteRaw(string data)
        {
            WriteString(data);
        }

        public override void Close()
        {
            // nop
        }

        internal override void Close(WriteState currentState)
        {
            if (currentState == WriteState.Error)
            {
                return;
            }
            try
            {
                switch (_type)
                {
                    case DocumentXmlWriterType.InsertSiblingAfter:
                        XmlNode? parent = _start.ParentNode;
                        if (parent == null)
                        {
                            throw new InvalidOperationException(SR.Xpn_MissingParent);
                        }

                        for (int i = _fragment.Count - 1; i >= 0; i--)
                        {
                            parent.InsertAfter(_fragment[i], _start);
                        }

                        break;
                    case DocumentXmlWriterType.InsertSiblingBefore:
                        parent = _start.ParentNode;
                        if (parent == null)
                        {
                            throw new InvalidOperationException(SR.Xpn_MissingParent);
                        }

                        for (int i = 0; i < _fragment.Count; i++)
                        {
                            parent.InsertBefore(_fragment[i], _start);
                        }

                        break;
                    case DocumentXmlWriterType.PrependChild:
                        for (int i = _fragment.Count - 1; i >= 0; i--)
                        {
                            _start.PrependChild(_fragment[i]);
                        }

                        break;
                    case DocumentXmlWriterType.AppendChild:
                        for (int i = 0; i < _fragment.Count; i++)
                        {
                            _start.AppendChild(_fragment[i]);
                        }

                        break;
                    case DocumentXmlWriterType.AppendAttribute:
                        CloseWithAppendAttribute();
                        break;
                    case DocumentXmlWriterType.ReplaceToFollowingSibling:
                        if (_fragment.Count == 0)
                        {
                            throw new InvalidOperationException(SR.Xpn_NoContent);
                        }

                        CloseWithReplaceToFollowingSibling();
                        break;
                }
            }
            finally
            {
                _fragment.Clear();
            }
        }

        private void CloseWithAppendAttribute()
        {
            XmlElement? elem = _start as XmlElement;
            Debug.Assert(elem != null);
            XmlAttributeCollection attrs = elem.Attributes;
            for (int i = 0; i < _fragment.Count; i++)
            {
                XmlAttribute? attr = _fragment[i] as XmlAttribute;
                Debug.Assert(attr != null);
                int offset = attrs.FindNodeOffsetNS(attr);
                if (offset != -1
                    && ((XmlAttribute)attrs.nodes[offset]).Specified)
                {
                    throw new XmlException(SR.Xml_DupAttributeName, attr.Prefix.Length == 0 ? attr.LocalName : string.Concat(attr.Prefix, ":", attr.LocalName));
                }
            }
            for (int i = 0; i < _fragment.Count; i++)
            {
                XmlAttribute? attr = _fragment[i] as XmlAttribute;
                Debug.Assert(attr != null);
                attrs.Append(attr);
            }
        }

        private void CloseWithReplaceToFollowingSibling()
        {
            XmlNode? parent = _start.ParentNode;
            if (parent == null)
            {
                throw new InvalidOperationException(SR.Xpn_MissingParent);
            }

            if (_start != _end)
            {
                if (!DocumentXPathNavigator.IsFollowingSibling(_start, _end))
                {
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
                }
                if (_start.IsReadOnly)
                {
                    throw new InvalidOperationException(SR.Xdom_Node_Modify_ReadOnly);
                }

                DocumentXPathNavigator.DeleteToFollowingSibling(_start.NextSibling!, _end);
            }

            XmlNode fragment0 = _fragment[0];
            parent.ReplaceChild(fragment0, _start);
            for (int i = _fragment.Count - 1; i >= 1; i--)
            {
                parent.InsertAfter(_fragment[i], fragment0);
            }

            _navigator!.ResetPosition(fragment0);
        }

        public override void Flush()
        {
            // nop
        }

        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return _namespaceManager!.GetNamespacesInScope(scope);
        }

        string? IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return _namespaceManager!.LookupNamespace(prefix);
        }

        string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return _namespaceManager!.LookupPrefix(namespaceName);
        }

        private void AddAttribute(XmlAttribute attr, XmlNode? parent)
        {
            if (parent == null)
            {
                _fragment.Add(attr);
            }
            else
            {
                XmlElement? elem = parent as XmlElement;
                if (elem == null)
                {
                    throw new InvalidOperationException();
                }

                elem.Attributes.Append(attr);
            }
        }

        private void AddChild(XmlNode node, XmlNode? parent)
        {
            if (parent == null)
            {
                _fragment.Add(node);
            }
            else
            {
                parent.AppendChild(node);
            }
        }

        private State StartState()
        {
            XmlNodeType nodeType = XmlNodeType.None;

            switch (_type)
            {
                case DocumentXmlWriterType.InsertSiblingAfter:
                case DocumentXmlWriterType.InsertSiblingBefore:
                    XmlNode? parent = _start.ParentNode;
                    if (parent != null)
                    {
                        nodeType = parent.NodeType;
                    }

                    if (nodeType == XmlNodeType.Document)
                    {
                        return State.Prolog;
                    }
                    else if (nodeType == XmlNodeType.DocumentFragment)
                    {
                        return State.Fragment;
                    }

                    break;
                case DocumentXmlWriterType.PrependChild:
                case DocumentXmlWriterType.AppendChild:
                    nodeType = _start.NodeType;
                    if (nodeType == XmlNodeType.Document)
                    {
                        return State.Prolog;
                    }
                    else if (nodeType == XmlNodeType.DocumentFragment)
                    {
                        return State.Fragment;
                    }

                    break;
                case DocumentXmlWriterType.AppendAttribute:
                    return State.Attribute;
                case DocumentXmlWriterType.ReplaceToFollowingSibling:
                    break;
            }
            return State.Content;
        }

        private static readonly State[] s_changeState = {
//          State.Error,    State.Attribute,State.Prolog,   State.Fragment, State.Content,

// Method.XmlDeclaration:
            State.Error,    State.Error,    State.Prolog,   State.Content,  State.Error,
// Method.StartDocument:
            State.Error,    State.Error,    State.Error,    State.Error,    State.Error,
// Method.EndDocument:
            State.Error,    State.Error,    State.Error,    State.Error,    State.Error,
// Method.DocType:
            State.Error,    State.Error,    State.Prolog,   State.Error,    State.Error,
// Method.StartElement:
            State.Error,    State.Error,    State.Content,  State.Content,  State.Content,
// Method.EndElement:
            State.Error,    State.Error,    State.Error,    State.Error,    State.Content,
// Method.FullEndElement:
            State.Error,    State.Error,    State.Error,    State.Error,    State.Content,
// Method.StartAttribute:
            State.Error,    State.Content,  State.Error,    State.Error,    State.Content,
// Method.EndAttribute:
            State.Error,    State.Error,    State.Error,    State.Error,    State.Content,
// Method.StartNamespaceDeclaration:
            State.Error,    State.Content,  State.Error,    State.Error,    State.Content,
// Method.EndNamespaceDeclaration:
            State.Error,    State.Error,    State.Error,    State.Error,    State.Content,
// Method.CData:
            State.Error,    State.Error,    State.Error,    State.Content,  State.Content,
// Method.Comment:
            State.Error,    State.Error,    State.Prolog,   State.Content,  State.Content,
// Method.ProcessingInstruction:
            State.Error,    State.Error,    State.Prolog,   State.Content,  State.Content,
// Method.EntityRef:
            State.Error,    State.Error,    State.Error,    State.Content,  State.Content,
// Method.Whitespace:
            State.Error,    State.Error,    State.Prolog,   State.Content,  State.Content,
// Method.String:
            State.Error,    State.Error,    State.Error,    State.Content,  State.Content,
        };

        private void VerifyState(Method method)
        {
            _state = s_changeState[(int)method * (int)State.Last + (int)_state];
            if (_state == State.Error)
            {
                throw new InvalidOperationException(SR.Xml_ClosedOrError);
            }
        }
    }
}
