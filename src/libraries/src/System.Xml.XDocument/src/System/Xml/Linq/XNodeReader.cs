// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Debug = System.Diagnostics.Debug;

namespace System.Xml.Linq
{
    internal class XNodeReader : XmlReader, IXmlLineInfo
    {
        private static readonly char[] WhitespaceChars = new char[] { ' ', '\t', '\n', '\r' };

        // The reader position is encoded by the tuple (source, parent).
        // Lazy text uses (instance, parent element). Attribute value
        // uses (instance, parent attribute). End element uses (instance, 
        // instance). Common XObject uses (instance, null). 
        object source;
        object parent;
        ReadState state;
        XNode root;
        XmlNameTable nameTable;
        bool omitDuplicateNamespaces;

        internal XNodeReader(XNode node, XmlNameTable nameTable, ReaderOptions options)
        {
            this.source = node;
            this.root = node;
            this.nameTable = nameTable != null ? nameTable : CreateNameTable();
            this.omitDuplicateNamespaces = (options & ReaderOptions.OmitDuplicateNamespaces) != 0 ? true : false;
        }

        internal XNodeReader(XNode node, XmlNameTable nameTable)
            : this(node, nameTable,
                (node.GetSaveOptionsFromAnnotations() & SaveOptions.OmitDuplicateNamespaces) != 0 ?
                    ReaderOptions.OmitDuplicateNamespaces : ReaderOptions.None)
        {
        }

        public override int AttributeCount
        {
            get
            {
                if (!IsInteractive)
                {
                    return 0;
                }
                int count = 0;
                XElement e = GetElementInAttributeScope();
                if (e != null)
                {
                    XAttribute a = e.lastAttr;
                    if (a != null)
                    {
                        do
                        {
                            a = a.next;
                            if (!omitDuplicateNamespaces || !IsDuplicateNamespaceAttribute(a))
                            {
                                count++;
                            }
                        } while (a != e.lastAttr);
                    }
                }
                return count;
            }
        }

        public override string BaseURI
        {
            get
            {
                XObject o = source as XObject;
                if (o != null)
                {
                    return o.BaseUri;
                }
                o = parent as XObject;
                if (o != null)
                {
                    return o.BaseUri;
                }
                return string.Empty;
            }
        }

        public override int Depth
        {
            get
            {
                if (!IsInteractive)
                {
                    return 0;
                }
                XObject o = source as XObject;
                if (o != null)
                {
                    return GetDepth(o);
                }
                o = parent as XObject;
                if (o != null)
                {
                    return GetDepth(o) + 1;
                }
                return 0;
            }
        }

        static int GetDepth(XObject o)
        {
            int depth = 0;
            while (o.parent != null)
            {
                depth++;
                o = o.parent;
            }
            if (o is XDocument)
            {
                depth--;
            }
            return depth;
        }

        public override bool EOF
        {
            get { return state == ReadState.EndOfFile; }
        }

        public override bool HasAttributes
        {
            get
            {
                if (!IsInteractive)
                {
                    return false;
                }
                XElement e = GetElementInAttributeScope();
                if (e != null && e.lastAttr != null)
                {
                    if (omitDuplicateNamespaces)
                    {
                        return GetFirstNonDuplicateNamespaceAttribute(e.lastAttr.next) != null;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public override bool HasValue
        {
            get
            {
                if (!IsInteractive)
                {
                    return false;
                }
                XObject o = source as XObject;
                if (o != null)
                {
                    switch (o.NodeType)
                    {
                        case XmlNodeType.Attribute:
                        case XmlNodeType.Text:
                        case XmlNodeType.CDATA:
                        case XmlNodeType.Comment:
                        case XmlNodeType.ProcessingInstruction:
                        case XmlNodeType.DocumentType:
                            return true;
                        default:
                            return false;
                    }
                }
                return true;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                if (!IsInteractive)
                {
                    return false;
                }
                XElement e = source as XElement;
                return e != null && e.IsEmpty;
            }
        }

        public override string LocalName
        {
            get { return nameTable.Add(GetLocalName()); }
        }

        string GetLocalName()
        {
            if (!IsInteractive)
            {
                return string.Empty;
            }
            XElement e = source as XElement;
            if (e != null)
            {
                return e.Name.LocalName;
            }
            XAttribute a = source as XAttribute;
            if (a != null)
            {
                return a.Name.LocalName;
            }
            XProcessingInstruction p = source as XProcessingInstruction;
            if (p != null)
            {
                return p.Target;
            }
            XDocumentType n = source as XDocumentType;
            if (n != null)
            {
                return n.Name;
            }
            return string.Empty;
        }

        public override string Name
        {
            get
            {
                string prefix = GetPrefix();
                if (prefix.Length == 0)
                {
                    return nameTable.Add(GetLocalName());
                }
                return nameTable.Add(string.Concat(prefix, ":", GetLocalName()));
            }
        }

        public override string NamespaceURI
        {
            get { return nameTable.Add(GetNamespaceURI()); }
        }

        string GetNamespaceURI()
        {
            if (!IsInteractive)
            {
                return string.Empty;
            }
            XElement e = source as XElement;
            if (e != null)
            {
                return e.Name.NamespaceName;
            }
            XAttribute a = source as XAttribute;
            if (a != null)
            {
                string namespaceName = a.Name.NamespaceName;
                if (namespaceName.Length == 0 && a.Name.LocalName == "xmlns")
                {
                    return XNamespace.xmlnsPrefixNamespace;
                }
                return namespaceName;
            }
            return string.Empty;
        }

        public override XmlNameTable NameTable
        {
            get { return nameTable; }
        }

        public override XmlNodeType NodeType
        {
            get
            {
                if (!IsInteractive)
                {
                    return XmlNodeType.None;
                }
                XObject o = source as XObject;
                if (o != null)
                {
                    if (IsEndElement)
                    {
                        return XmlNodeType.EndElement;
                    }
                    XmlNodeType nt = o.NodeType;
                    if (nt != XmlNodeType.Text)
                    {
                        return nt;
                    }
                    if (o.parent != null && o.parent.parent == null && o.parent is XDocument)
                    {
                        return XmlNodeType.Whitespace;
                    }
                    return XmlNodeType.Text;
                }
                if (parent is XDocument)
                {
                    return XmlNodeType.Whitespace;
                }
                return XmlNodeType.Text;
            }
        }

        public override string Prefix
        {
            get { return nameTable.Add(GetPrefix()); }
        }

        string GetPrefix()
        {
            if (!IsInteractive)
            {
                return string.Empty;
            }
            XElement e = source as XElement;
            if (e != null)
            {
                string prefix = e.GetPrefixOfNamespace(e.Name.Namespace);
                if (prefix != null)
                {
                    return prefix;
                }
                return string.Empty;
            }
            XAttribute a = source as XAttribute;
            if (a != null)
            {
                string prefix = a.GetPrefixOfNamespace(a.Name.Namespace);
                if (prefix != null)
                {
                    return prefix;
                }
            }
            return string.Empty;
        }

        public override ReadState ReadState
        {
            get { return state; }
        }

        public override XmlReaderSettings Settings
        {
            get
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.CheckCharacters = false;
                return settings;
            }
        }

        public override string Value
        {
            get
            {
                if (!IsInteractive)
                {
                    return string.Empty;
                }
                XObject o = source as XObject;
                if (o != null)
                {
                    switch (o.NodeType)
                    {
                        case XmlNodeType.Attribute:
                            return ((XAttribute)o).Value;
                        case XmlNodeType.Text:
                        case XmlNodeType.CDATA:
                            return ((XText)o).Value;
                        case XmlNodeType.Comment:
                            return ((XComment)o).Value;
                        case XmlNodeType.ProcessingInstruction:
                            return ((XProcessingInstruction)o).Data;
                        case XmlNodeType.DocumentType:
                            return ((XDocumentType)o).InternalSubset;
                        default:
                            return string.Empty;
                    }
                }
                return (string)source;
            }
        }

        public override string XmlLang
        {
            get
            {
                if (!IsInteractive)
                {
                    return string.Empty;
                }
                XElement e = GetElementInScope();
                if (e != null)
                {
                    XName name = XNamespace.Xml.GetName("lang");
                    do
                    {
                        XAttribute a = e.Attribute(name);
                        if (a != null)
                        {
                            return a.Value;
                        }
                        e = e.parent as XElement;
                    } while (e != null);
                }
                return string.Empty;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                if (!IsInteractive)
                {
                    return XmlSpace.None;
                }
                XElement e = GetElementInScope();
                if (e != null)
                {
                    XName name = XNamespace.Xml.GetName("space");
                    do
                    {
                        XAttribute a = e.Attribute(name);
                        if (a != null)
                        {
                            switch (a.Value.Trim(WhitespaceChars))
                            {
                                case "preserve":
                                    return XmlSpace.Preserve;
                                case "default":
                                    return XmlSpace.Default;
                                default:
                                    break;
                            }
                        }
                        e = e.parent as XElement;
                    } while (e != null);
                }
                return XmlSpace.None;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && ReadState != ReadState.Closed)
            {
                Close();
            }
        }

        private void Close()
        {
            source = null;
            parent = null;
            root = null;
            state = ReadState.Closed;
        }

        public override string GetAttribute(string name)
        {
            if (!IsInteractive)
            {
                return null;
            }
            XElement e = GetElementInAttributeScope();
            if (e != null)
            {
                string localName, namespaceName;
                GetNameInAttributeScope(name, e, out localName, out namespaceName);
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next;
                        if (a.Name.LocalName == localName && a.Name.NamespaceName == namespaceName)
                        {
                            if (omitDuplicateNamespaces && IsDuplicateNamespaceAttribute(a))
                            {
                                return null;
                            }
                            else
                            {
                                return a.Value;
                            }
                        }
                    } while (a != e.lastAttr);
                }
                return null;
            }
            XDocumentType n = source as XDocumentType;
            if (n != null)
            {
                switch (name)
                {
                    case "PUBLIC":
                        return n.PublicId;
                    case "SYSTEM":
                        return n.SystemId;
                }
            }
            return null;
        }

        public override string GetAttribute(string localName, string namespaceName)
        {
            if (!IsInteractive)
            {
                return null;
            }
            XElement e = GetElementInAttributeScope();
            if (e != null)
            {
                if (localName == "xmlns")
                {
                    if (namespaceName != null && namespaceName.Length == 0)
                    {
                        return null;
                    }
                    if (namespaceName == XNamespace.xmlnsPrefixNamespace)
                    {
                        namespaceName = string.Empty;
                    }
                }
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next;
                        if (a.Name.LocalName == localName && a.Name.NamespaceName == namespaceName)
                        {
                            if (omitDuplicateNamespaces && IsDuplicateNamespaceAttribute(a))
                            {
                                return null;
                            }
                            else
                            {
                                return a.Value;
                            }
                        }
                    } while (a != e.lastAttr);
                }
            }
            return null;
        }

        public override string GetAttribute(int index)
        {
            if (!IsInteractive)
            {
                return null;
            }
            if (index < 0)
            {
                return null;
            }
            XElement e = GetElementInAttributeScope();
            if (e != null)
            {
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next;
                        if (!omitDuplicateNamespaces || !IsDuplicateNamespaceAttribute(a))
                        {
                            if (index-- == 0)
                            {
                                return a.Value;
                            }
                        }
                    } while (a != e.lastAttr);
                }
            }
            return null;
        }

        public override string LookupNamespace(string prefix)
        {
            if (!IsInteractive)
            {
                return null;
            }
            if (prefix == null)
            {
                return null;
            }
            XElement e = GetElementInScope();
            if (e != null)
            {
                XNamespace ns = prefix.Length == 0 ? e.GetDefaultNamespace() : e.GetNamespaceOfPrefix(prefix);
                if (ns != null)
                {
                    return nameTable.Add(ns.NamespaceName);
                }
            }
            return null;
        }

        public override bool MoveToAttribute(string name)
        {
            if (!IsInteractive)
            {
                return false;
            }
            XElement e = GetElementInAttributeScope();
            if (e != null)
            {
                string localName, namespaceName;
                GetNameInAttributeScope(name, e, out localName, out namespaceName);
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next;
                        if (a.Name.LocalName == localName &&
                            a.Name.NamespaceName == namespaceName)
                        {
                            if (omitDuplicateNamespaces && IsDuplicateNamespaceAttribute(a))
                            {
                                // If it's a duplicate namespace attribute just act as if it doesn't exist
                                return false;
                            }
                            else
                            {
                                source = a;
                                parent = null;
                                return true;
                            }
                        }
                    } while (a != e.lastAttr);
                }
            }
            return false;
        }

        public override bool MoveToAttribute(string localName, string namespaceName)
        {
            if (!IsInteractive)
            {
                return false;
            }
            XElement e = GetElementInAttributeScope();
            if (e != null)
            {
                if (localName == "xmlns")
                {
                    if (namespaceName != null && namespaceName.Length == 0)
                    {
                        return false;
                    }
                    if (namespaceName == XNamespace.xmlnsPrefixNamespace)
                    {
                        namespaceName = string.Empty;
                    }
                }
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next;
                        if (a.Name.LocalName == localName &&
                            a.Name.NamespaceName == namespaceName)
                        {
                            if (omitDuplicateNamespaces && IsDuplicateNamespaceAttribute(a))
                            {
                                // If it's a duplicate namespace attribute just act as if it doesn't exist
                                return false;
                            }
                            else
                            {
                                source = a;
                                parent = null;
                                return true;
                            }
                        }
                    } while (a != e.lastAttr);
                }
            }
            return false;
        }

        public override void MoveToAttribute(int index)
        {
            if (!IsInteractive)
            {
                return;
            }
            if (index < 0) throw new ArgumentOutOfRangeException("index");
            XElement e = GetElementInAttributeScope();
            if (e != null)
            {
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next;
                        if (!omitDuplicateNamespaces || !IsDuplicateNamespaceAttribute(a))
                        {
                            // Only count those which are non-duplicates if we're asked to
                            if (index-- == 0)
                            {
                                source = a;
                                parent = null;
                                return;
                            }
                        }
                    } while (a != e.lastAttr);
                }
            }
            throw new ArgumentOutOfRangeException("index");
        }

        public override bool MoveToElement()
        {
            if (!IsInteractive)
            {
                return false;
            }
            XAttribute a = source as XAttribute;
            if (a == null)
            {
                a = parent as XAttribute;
            }
            if (a != null)
            {
                if (a.parent != null)
                {
                    source = a.parent;
                    parent = null;
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToFirstAttribute()
        {
            if (!IsInteractive)
            {
                return false;
            }
            XElement e = GetElementInAttributeScope();
            if (e != null)
            {
                if (e.lastAttr != null)
                {
                    if (omitDuplicateNamespaces)
                    {
                        object na = GetFirstNonDuplicateNamespaceAttribute(e.lastAttr.next);
                        if (na == null)
                        {
                            return false;
                        }
                        source = na;
                    }
                    else
                    {
                        source = e.lastAttr.next;
                    }
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToNextAttribute()
        {
            if (!IsInteractive)
            {
                return false;
            }
            XElement e = source as XElement;
            if (e != null)
            {
                if (IsEndElement)
                {
                    return false;
                }
                if (e.lastAttr != null)
                {
                    if (omitDuplicateNamespaces)
                    {
                        // Skip duplicate namespace attributes
                        // We must NOT modify the this.source until we find the one we're looking for
                        //   because if we don't find anything, we need to stay positioned where we're now
                        object na = GetFirstNonDuplicateNamespaceAttribute(e.lastAttr.next);
                        if (na == null)
                        {
                            return false;
                        }
                        source = na;
                    }
                    else
                    {
                        source = e.lastAttr.next;
                    }
                    return true;
                }
                return false;
            }
            XAttribute a = source as XAttribute;
            if (a == null)
            {
                a = parent as XAttribute;
            }
            if (a != null)
            {
                if (a.parent != null && ((XElement)a.parent).lastAttr != a)
                {
                    if (omitDuplicateNamespaces)
                    {
                        // Skip duplicate namespace attributes
                        // We must NOT modify the this.source until we find the one we're looking for
                        //   because if we don't find anything, we need to stay positioned where we're now
                        object na = GetFirstNonDuplicateNamespaceAttribute(a.next);
                        if (na == null)
                        {
                            return false;
                        }
                        source = na;
                    }
                    else
                    {
                        source = a.next;
                    }
                    parent = null;
                    return true;
                }
            }
            return false;
        }

        public override bool Read()
        {
            switch (state)
            {
                case ReadState.Initial:
                    state = ReadState.Interactive;
                    XDocument d = source as XDocument;
                    if (d != null)
                    {
                        return ReadIntoDocument(d);
                    }
                    return true;
                case ReadState.Interactive:
                    return Read(false);
                default:
                    return false;
            }
        }

        public override bool ReadAttributeValue()
        {
            if (!IsInteractive)
            {
                return false;
            }
            XAttribute a = source as XAttribute;
            if (a != null)
            {
                return ReadIntoAttribute(a);
            }
            return false;
        }

        public override bool ReadToDescendant(string localName, string namespaceName)
        {
            if (!IsInteractive)
            {
                return false;
            }
            MoveToElement();
            XElement c = source as XElement;
            if (c != null && !c.IsEmpty)
            {
                if (IsEndElement)
                {
                    return false;
                }
                foreach (XElement e in c.Descendants())
                {
                    if (e.Name.LocalName == localName &&
                        e.Name.NamespaceName == namespaceName)
                    {
                        source = e;
                        return true;
                    }
                }
                IsEndElement = true;
            }
            return false;
        }

        public override bool ReadToFollowing(string localName, string namespaceName)
        {
            while (Read())
            {
                XElement e = source as XElement;
                if (e != null)
                {
                    if (IsEndElement) continue;
                    if (e.Name.LocalName == localName && e.Name.NamespaceName == namespaceName)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool ReadToNextSibling(string localName, string namespaceName)
        {
            if (!IsInteractive)
            {
                return false;
            }
            MoveToElement();
            if (source != root)
            {
                XNode n = source as XNode;
                if (n != null)
                {
                    foreach (XElement e in n.ElementsAfterSelf())
                    {
                        if (e.Name.LocalName == localName &&
                            e.Name.NamespaceName == namespaceName)
                        {
                            source = e;
                            IsEndElement = false;
                            return true;
                        }
                    }
                    if (n.parent is XElement)
                    {
                        source = n.parent;
                        IsEndElement = true;
                        return false;
                    }
                }
                else
                {
                    if (parent is XElement)
                    {
                        source = parent;
                        parent = null;
                        IsEndElement = true;
                        return false;
                    }
                }
            }
            return ReadToEnd();
        }

        public override void ResolveEntity()
        {
        }

        public override void Skip()
        {
            if (!IsInteractive)
            {
                return;
            }
            Read(true);
        }

        bool IXmlLineInfo.HasLineInfo()
        {
            if (IsEndElement)
            {
                // Special case for EndElement - we store the line info differently in this case
                //   we also know that the current node (source) is XElement
                XElement e = source as XElement;
                if (e != null)
                {
                    return e.Annotation<LineInfoEndElementAnnotation>() != null;
                }
            }
            else
            {
                IXmlLineInfo li = source as IXmlLineInfo;
                if (li != null)
                {
                    return li.HasLineInfo();
                }
            }
            return false;
        }

        int IXmlLineInfo.LineNumber
        {
            get
            {
                if (IsEndElement)
                {
                    // Special case for EndElement - we store the line info differently in this case
                    //   we also know that the current node (source) is XElement
                    XElement e = source as XElement;
                    if (e != null)
                    {
                        LineInfoEndElementAnnotation a = e.Annotation<LineInfoEndElementAnnotation>();
                        if (a != null)
                        {
                            return a.lineNumber;
                        }
                    }
                }
                else
                {
                    IXmlLineInfo li = source as IXmlLineInfo;
                    if (li != null)
                    {
                        return li.LineNumber;
                    }
                }
                return 0;
            }
        }

        int IXmlLineInfo.LinePosition
        {
            get
            {
                if (IsEndElement)
                {
                    // Special case for EndElement - we store the line info differently in this case
                    //   we also know that the current node (source) is XElement
                    XElement e = source as XElement;
                    if (e != null)
                    {
                        LineInfoEndElementAnnotation a = e.Annotation<LineInfoEndElementAnnotation>();
                        if (a != null)
                        {
                            return a.linePosition;
                        }
                    }
                }
                else
                {
                    IXmlLineInfo li = source as IXmlLineInfo;
                    if (li != null)
                    {
                        return li.LinePosition;
                    }
                }
                return 0;
            }
        }

        bool IsEndElement
        {
            get { return parent == source; }
            set { parent = value ? source : null; }
        }

        bool IsInteractive
        {
            get { return state == ReadState.Interactive; }
        }

        static XmlNameTable CreateNameTable()
        {
            XmlNameTable nameTable = new NameTable();
            nameTable.Add(string.Empty);
            nameTable.Add(XNamespace.xmlnsPrefixNamespace);
            nameTable.Add(XNamespace.xmlPrefixNamespace);
            return nameTable;
        }

        XElement GetElementInAttributeScope()
        {
            XElement e = source as XElement;
            if (e != null)
            {
                if (IsEndElement)
                {
                    return null;
                }
                return e;
            }
            XAttribute a = source as XAttribute;
            if (a != null)
            {
                return (XElement)a.parent;
            }
            a = parent as XAttribute;
            if (a != null)
            {
                return (XElement)a.parent;
            }
            return null;
        }

        XElement GetElementInScope()
        {
            XElement e = source as XElement;
            if (e != null)
            {
                return e;
            }
            XNode n = source as XNode;
            if (n != null)
            {
                return n.parent as XElement;
            }
            XAttribute a = source as XAttribute;
            if (a != null)
            {
                return (XElement)a.parent;
            }
            e = parent as XElement;
            if (e != null)
            {
                return e;
            }
            a = parent as XAttribute;
            if (a != null)
            {
                return (XElement)a.parent;
            }
            return null;
        }

        static void GetNameInAttributeScope(string qualifiedName, XElement e, out string localName, out string namespaceName)
        {
            if (qualifiedName != null && qualifiedName.Length != 0)
            {
                int i = qualifiedName.IndexOf(':');
                if (i != 0 && i != qualifiedName.Length - 1)
                {
                    if (i == -1)
                    {
                        localName = qualifiedName;
                        namespaceName = string.Empty;
                        return;
                    }
                    XNamespace ns = e.GetNamespaceOfPrefix(qualifiedName.Substring(0, i));
                    if (ns != null)
                    {
                        localName = qualifiedName.Substring(i + 1, qualifiedName.Length - i - 1);
                        namespaceName = ns.NamespaceName;
                        return;
                    }
                }
            }
            localName = null;
            namespaceName = null;
        }

        bool Read(bool skipContent)
        {
            XElement e = source as XElement;
            if (e != null)
            {
                if (e.IsEmpty || IsEndElement || skipContent)
                {
                    return ReadOverNode(e);
                }
                return ReadIntoElement(e);
            }
            XNode n = source as XNode;
            if (n != null)
            {
                return ReadOverNode(n);
            }
            XAttribute a = source as XAttribute;
            if (a != null)
            {
                return ReadOverAttribute(a, skipContent);
            }
            return ReadOverText(skipContent);
        }

        bool ReadIntoDocument(XDocument d)
        {
            XNode n = d.content as XNode;
            if (n != null)
            {
                source = n.next;
                return true;
            }
            string s = d.content as string;
            if (s != null)
            {
                if (s.Length > 0)
                {
                    source = s;
                    parent = d;
                    return true;
                }
            }
            return ReadToEnd();
        }

        bool ReadIntoElement(XElement e)
        {
            XNode n = e.content as XNode;
            if (n != null)
            {
                source = n.next;
                return true;
            }
            string s = e.content as string;
            if (s != null)
            {
                if (s.Length > 0)
                {
                    source = s;
                    parent = e;
                }
                else
                {
                    source = e;
                    IsEndElement = true;
                }
                return true;
            }
            return ReadToEnd();
        }

        bool ReadIntoAttribute(XAttribute a)
        {
            source = a.value;
            parent = a;
            return true;
        }

        bool ReadOverAttribute(XAttribute a, bool skipContent)
        {
            XElement e = (XElement)a.parent;
            if (e != null)
            {
                if (e.IsEmpty || skipContent)
                {
                    return ReadOverNode(e);
                }
                return ReadIntoElement(e);
            }
            return ReadToEnd();
        }

        bool ReadOverNode(XNode n)
        {
            if (n == root)
            {
                return ReadToEnd();
            }
            XNode next = n.next;
            if (null == next || next == n || n == n.parent.content)
            {
                if (n.parent == null || (n.parent.parent == null && n.parent is XDocument))
                {
                    return ReadToEnd();
                }
                source = n.parent;
                IsEndElement = true;
            }
            else
            {
                source = next;
                IsEndElement = false;
            }
            return true;
        }

        bool ReadOverText(bool skipContent)
        {
            if (parent is XElement)
            {
                source = parent;
                parent = null;
                IsEndElement = true;
                return true;
            }
            if (parent is XAttribute)
            {
                XAttribute a = (XAttribute)parent;
                parent = null;
                return ReadOverAttribute(a, skipContent);
            }
            return ReadToEnd();
        }

        bool ReadToEnd()
        {
            state = ReadState.EndOfFile;
            return false;
        }

        /// <summary>
        /// Determines if the specified attribute would be a duplicate namespace declaration
        ///  - one which we already reported on some ancestor, so it's not necessary to report it here
        /// </summary>
        /// <param name="candidateAttribute">The attribute to test.</param>
        /// <returns>true if the attribute is a duplicate namespace declaration attribute</returns>
        bool IsDuplicateNamespaceAttribute(XAttribute candidateAttribute)
        {
            if (!candidateAttribute.IsNamespaceDeclaration)
            {
                return false;
            }
            else
            {
                // Split the method in two to enable inlining of this piece (Which will work for 95% of cases)
                return IsDuplicateNamespaceAttributeInner(candidateAttribute);
            }
        }

        bool IsDuplicateNamespaceAttributeInner(XAttribute candidateAttribute)
        {
            // First of all - if this is an xmlns:xml declaration then it's a duplicate
            //   since xml prefix can't be redeclared and it's declared by default always.
            if (candidateAttribute.Name.LocalName == "xml")
            {
                return true;
            }
            // The algorithm we use is:
            //    Go up the tree (but don't go higher than the root of this reader)
            //    and find the closest namespace declaration attribute which declares the same prefix
            //    If it declares that prefix to the exact same URI as ours does then ours is a duplicate
            //    Note that if we find a namespace declaration for the same prefix but with a different URI, then we don't have a dupe!
            XElement element = candidateAttribute.parent as XElement;
            if (element == root || element == null)
            {
                // If there's only the parent element of our attribute, there can be no duplicates
                return false;
            }
            element = element.parent as XElement;
            while (element != null)
            {
                // Search all attributes of this element for the same prefix declaration
                // Trick - a declaration for the same prefix will have the exact same XName - so we can do a quick ref comparison of names
                // (The default ns decl is represented by an XName "xmlns{}", even if you try to create
                //  an attribute with XName "xmlns{http://www.w3.org/2000/xmlns/}" it will fail,
                //  because it's treated as a declaration of prefix "xmlns" which is invalid)
                XAttribute a = element.lastAttr;
                if (a != null)
                {
                    do
                    {
                        if (a.name == candidateAttribute.name)
                        {
                            // Found the same prefix decl
                            if (a.Value == candidateAttribute.Value)
                            {
                                // And it's for the same namespace URI as well - so ours is a duplicate
                                return true;
                            }
                            else
                            {
                                // It's not for the same namespace URI - which means we have to keep ours
                                //   (no need to continue the search as this one overrides anything above it)
                                return false;
                            }
                        }
                        a = a.next;
                    } while (a != element.lastAttr);
                }
                if (element == root)
                {
                    return false;
                }
                element = element.parent as XElement;
            }
            return false;
        }

        /// <summary>
        /// Finds a first attribute (starting with the parameter) which is not a duplicate namespace attribute
        /// </summary>
        /// <param name="candidate">The attribute to start with</param>
        /// <returns>The first attribute which is not a namespace attribute or null if the end of attributes has bean reached</returns>
        XAttribute GetFirstNonDuplicateNamespaceAttribute(XAttribute candidate)
        {
            Debug.Assert(omitDuplicateNamespaces, "This method should only be called if we're omitting duplicate namespace attribute." +
                                                  "For perf reason it's better to test this flag in the caller method.");
            if (!IsDuplicateNamespaceAttribute(candidate))
            {
                return candidate;
            }

            XElement e = candidate.parent as XElement;
            if (e != null && candidate != e.lastAttr)
            {
                do
                {
                    candidate = candidate.next;
                    if (!IsDuplicateNamespaceAttribute(candidate))
                    {
                        return candidate;
                    }
                } while (candidate != e.lastAttr);
            }
            return null;
        }
    }
}