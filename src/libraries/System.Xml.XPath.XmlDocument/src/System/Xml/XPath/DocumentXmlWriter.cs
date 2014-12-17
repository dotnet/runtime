// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xml
{
    enum DocumentXmlWriterType
    {
        InsertSiblingAfter,
        InsertSiblingBefore,
        PrependChild,
        AppendChild,
        AppendAttribute,
        ReplaceToFollowingSibling,
    }

    // Implements a XmlWriter that augments a XmlDocument.
    sealed class DocumentXmlWriter : XmlRawWriter, IXmlNamespaceResolver
    {
        enum State
        {
            Error,
            Attribute,
            Prolog,
            Fragment,
            Content,

            Last, // always last
        }

        enum Method
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

        DocumentXmlWriterType type; // writer type
        XmlNode start; // context node
        XmlDocument document; // context document 
        XmlNamespaceManager namespaceManager; // context namespace manager
        State state; // current state
        XmlNode write; // current node 
        List<XmlNode> fragment; // top level node cache
        XmlWriterSettings settings; // wrapping writer settings
        DocumentXPathNavigator navigator; // context for replace 
        XmlNode end; // context for replace 

        public DocumentXmlWriter(DocumentXmlWriterType type, XmlNode start, XmlDocument document)
        {
            this.type = type;
            this.start = start;
            this.document = document;

            state = StartState();
            fragment = new List<XmlNode>();
            settings = new XmlWriterSettings();
            settings.CheckCharacters = false;
            settings.CloseOutput = false;
            settings.ConformanceLevel = (state == State.Prolog ? ConformanceLevel.Document : ConformanceLevel.Fragment);
        }

        public XmlNamespaceManager NamespaceManager
        {
            set
            {
                namespaceManager = value;
            }
        }

        public override XmlWriterSettings Settings
        {
            get
            {
                return settings;
            }
        }

        internal void SetSettings(XmlWriterSettings value)
        {
            settings = value;
        }

        public DocumentXPathNavigator Navigator
        {
            set
            {
                navigator = value;
            }
        }

        public XmlNode EndNode
        {
            set
            {
                end = value;
            }
        }

        internal override void WriteXmlDeclaration(XmlStandalone standalone)
        {
            VerifyState(Method.WriteXmlDeclaration);
            if (standalone != XmlStandalone.Omit)
            {
                XmlNode node = document.CreateXmlDeclaration("1.0", string.Empty, standalone == XmlStandalone.Yes ? "yes" : "no");
                AddChild(node, write);
            }
        }

        internal override void WriteXmlDeclaration(string xmldecl)
        {
            VerifyState(Method.WriteXmlDeclaration);
            string version, encoding, standalone;
            XmlParsingHelper.ParseXmlDeclarationValue(xmldecl, out version, out encoding, out standalone);
            XmlNode node = document.CreateXmlDeclaration(version, encoding, standalone);
            AddChild(node, write);
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

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            throw new NotSupportedException();
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            VerifyState(Method.WriteStartElement);
            XmlNode node = document.CreateElement(prefix, localName, ns);
            AddChild(node, write);
            write = node;
        }

        public override void WriteEndElement()
        {
            VerifyState(Method.WriteEndElement);
            if (write == null)
            {
                throw new InvalidOperationException();
            }
            write = write.ParentNode;
        }

        internal override void WriteEndElement(string prefix, string localName, string ns)
        {
            WriteEndElement();
        }

        public override void WriteFullEndElement()
        {
            VerifyState(Method.WriteFullEndElement);
            XmlElement elem = write as XmlElement;
            if (elem == null)
            {
                throw new InvalidOperationException();
            }
            elem.IsEmpty = false;
            write = elem.ParentNode;
        }

        internal override void WriteFullEndElement(string prefix, string localName, string ns)
        {
            WriteFullEndElement();
        }

        internal override void StartElementContent()
        {
            // nop
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            VerifyState(Method.WriteStartAttribute);
            XmlAttribute attr = document.CreateAttribute(prefix, localName, ns);
            AddAttribute(attr, write);
            write = attr;
        }

        public override void WriteEndAttribute()
        {
            VerifyState(Method.WriteEndAttribute);
            XmlAttribute attr = write as XmlAttribute;
            if (attr == null)
            {
                throw new InvalidOperationException();
            }
            if (!attr.HasChildNodes)
            {
                XmlNode node = document.CreateTextNode(string.Empty);
                AddChild(node, attr);
            }
            write = attr.OwnerElement;
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
                attr = document.CreateAttribute(prefix, XmlConst.NsXmlNs, XmlConst.ReservedNsXmlNs);
            }
            else
            {
                attr = document.CreateAttribute(XmlConst.NsXmlNs, prefix, XmlConst.ReservedNsXmlNs);
            }
            AddAttribute(attr, write);
            write = attr;
        }

        internal override void WriteEndNamespaceDeclaration()
        {
            VerifyState(Method.WriteEndNamespaceDeclaration);
            XmlAttribute attr = write as XmlAttribute;
            if (attr == null)
            {
                throw new InvalidOperationException();
            }
            if (!attr.HasChildNodes)
            {
                XmlNode node = document.CreateTextNode(string.Empty);
                AddChild(node, attr);
            }
            write = attr.OwnerElement;
        }

        public override void WriteCData(string text)
        {
            VerifyState(Method.WriteCData);
            XmlConvertEx.VerifyCharData(text, ExceptionType.ArgumentException);
            XmlNode node = document.CreateCDataSection(text);
            AddChild(node, write);
        }

        public override void WriteComment(string text)
        {
            VerifyState(Method.WriteComment);
            XmlConvertEx.VerifyCharData(text, ExceptionType.ArgumentException);
            XmlNode node = document.CreateComment(text);
            AddChild(node, write);
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            VerifyState(Method.WriteProcessingInstruction);
            XmlConvertEx.VerifyCharData(text, ExceptionType.ArgumentException);
            XmlNode node = document.CreateProcessingInstruction(name, text);
            AddChild(node, write);
        }

        public override void WriteEntityRef(string name)
        {
            throw new NotSupportedException();
        }

        public override void WriteCharEntity(char ch)
        {
            WriteString(new string(ch, 1));
        }

        public override void WriteWhitespace(string text)
        {
            VerifyState(Method.WriteWhitespace);
            XmlConvertEx.VerifyCharData(text, ExceptionType.ArgumentException);
            if (document.PreserveWhitespace)
            {
                XmlNode node = document.CreateWhitespace(text);
                AddChild(node, write);
            }
        }

        public override void WriteString(string text)
        {
            VerifyState(Method.WriteString);
            XmlConvertEx.VerifyCharData(text, ExceptionType.ArgumentException);
            XmlNode node = document.CreateTextNode(text);
            AddChild(node, write);
        }

        public override void WriteSurrogateCharEntity(char lowCh, char highCh)
        {
            WriteString(new string(new char[] { highCh, lowCh }));
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

        internal override void Close(WriteState currentState)
        {
            if (currentState == WriteState.Error)
            {
                return;
            }
            try
            {
                switch (type)
                {
                    case DocumentXmlWriterType.InsertSiblingAfter:
                        XmlNode parent = start.ParentNode;
                        if (parent == null)
                        {
                            throw new InvalidOperationException(SR.Xpn_MissingParent);
                        }
                        for (int i = fragment.Count - 1; i >= 0; i--)
                        {
                            parent.InsertAfter(fragment[i], start);
                        }
                        break;
                    case DocumentXmlWriterType.InsertSiblingBefore:
                        parent = start.ParentNode;
                        if (parent == null)
                        {
                            throw new InvalidOperationException(SR.Xpn_MissingParent);
                        }
                        for (int i = 0; i < fragment.Count; i++)
                        {
                            parent.InsertBefore(fragment[i], start);
                        }
                        break;
                    case DocumentXmlWriterType.PrependChild:
                        for (int i = fragment.Count - 1; i >= 0; i--)
                        {
                            start.PrependChild(fragment[i]);
                        }
                        break;
                    case DocumentXmlWriterType.AppendChild:
                        for (int i = 0; i < fragment.Count; i++)
                        {
                            start.AppendChild(fragment[i]);
                        }
                        break;
                    case DocumentXmlWriterType.AppendAttribute:
                        CloseWithAppendAttribute();
                        break;
                    case DocumentXmlWriterType.ReplaceToFollowingSibling:
                        if (fragment.Count == 0)
                        {
                            throw new InvalidOperationException(SR.Xpn_NoContent);
                        }
                        CloseWithReplaceToFollowingSibling();
                        break;
                }
            }
            finally
            {
                fragment.Clear();
            }
        }

        private void CloseWithAppendAttribute()
        {
            XmlElement elem = start as XmlElement;
            Debug.Assert(elem != null);
            XmlAttributeCollection attrs = elem.Attributes;
            for (int i = 0; i < fragment.Count; i++)
            {
                XmlAttribute attr = fragment[i] as XmlAttribute;
                Debug.Assert(attr != null);
                int offset = attrs.FindNodeOffsetNS(attr);
                if (offset != -1
                    && (attrs[offset]).Specified)
                {
                    throw new XmlException(SR.Format(SR.Xml_DupAttributeName, attr.Prefix.Length == 0 ? attr.LocalName : string.Concat(attr.Prefix, ":", attr.LocalName)));
                }
            }
            for (int i = 0; i < fragment.Count; i++)
            {
                XmlAttribute attr = fragment[i] as XmlAttribute;
                Debug.Assert(attr != null);
                attrs.Append(attr);
            }
        }

        private void CloseWithReplaceToFollowingSibling()
        {
            XmlNode parent = start.ParentNode;
            if (parent == null)
            {
                throw new InvalidOperationException(SR.Xpn_MissingParent);
            }
            if (start != end)
            {
                if (!DocumentXPathNavigator.IsFollowingSibling(start, end))
                {
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
                }
                if (start.IsReadOnly)
                {
                    throw new InvalidOperationException(SR.Xdom_Node_Modify_ReadOnly);
                }
                DocumentXPathNavigator.DeleteToFollowingSibling(start.NextSibling, end);
            }
            XmlNode fragment0 = fragment[0];
            parent.ReplaceChild(fragment0, start);
            for (int i = fragment.Count - 1; i >= 1; i--)
            {
                parent.InsertAfter(fragment[i], fragment0);
            }
            navigator.ResetPosition(fragment0);
        }

        public override void Flush()
        {
            // nop
        }

        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return namespaceManager.GetNamespacesInScope(scope);
        }

        string IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return namespaceManager.LookupNamespace(prefix);
        }

        string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return namespaceManager.LookupPrefix(namespaceName);
        }

        void AddAttribute(XmlAttribute attr, XmlNode parent)
        {
            if (parent == null)
            {
                fragment.Add(attr);
            }
            else
            {
                XmlElement elem = parent as XmlElement;
                if (elem == null)
                {
                    throw new InvalidOperationException();
                }
                elem.Attributes.Append(attr);
            }
        }

        void AddChild(XmlNode node, XmlNode parent)
        {
            if (parent == null)
            {
                fragment.Add(node);
            }
            else
            {
                parent.AppendChild(node);
            }
        }

        State StartState()
        {
            XmlNodeType nodeType = XmlNodeType.None;

            switch (type)
            {
                case DocumentXmlWriterType.InsertSiblingAfter:
                case DocumentXmlWriterType.InsertSiblingBefore:
                    XmlNode parent = start.ParentNode;
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
                    nodeType = start.NodeType;
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

        static State[] changeState = {
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

        void VerifyState(Method method)
        {
            state = changeState[(int)method * (int)State.Last + (int)state];
            if (state == State.Error)
            {
                throw new InvalidOperationException(SR.Xml_ClosedOrError);
            }
        }
    }
}
