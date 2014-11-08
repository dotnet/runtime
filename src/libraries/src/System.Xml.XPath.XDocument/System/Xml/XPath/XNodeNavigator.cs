// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace System.Xml.XPath
{
    internal class XNodeNavigator : XPathNavigator, IXmlLineInfo
    {
        internal static readonly string xmlPrefixNamespace = XNamespace.Xml.NamespaceName;
        internal static readonly string xmlnsPrefixNamespace = XNamespace.Xmlns.NamespaceName;
        const int DocumentContentMask =
            (1 << (int)XmlNodeType.Element) |
            (1 << (int)XmlNodeType.ProcessingInstruction) |
            (1 << (int)XmlNodeType.Comment);
        static readonly int[] ElementContentMasks = {
            0,                                              // Root
            (1 << (int)XmlNodeType.Element),                // Element
            0,                                              // Attribute
            0,                                              // Namespace
            (1 << (int)XmlNodeType.CDATA) |
            (1 << (int)XmlNodeType.Text),                   // Text
            0,                                              // SignificantWhitespace
            0,                                              // Whitespace
            (1 << (int)XmlNodeType.ProcessingInstruction),  // ProcessingInstruction
            (1 << (int)XmlNodeType.Comment),                // Comment
            (1 << (int)XmlNodeType.Element) |
            (1 << (int)XmlNodeType.CDATA) |
            (1 << (int)XmlNodeType.Text) |
            (1 << (int)XmlNodeType.ProcessingInstruction) |
            (1 << (int)XmlNodeType.Comment)                 // All
        };
        const int TextMask =
            (1 << (int)XmlNodeType.CDATA) |
            (1 << (int)XmlNodeType.Text);

        static XAttribute XmlNamespaceDeclaration;

        // The navigator position is encoded by the tuple (source, parent). 
        // Lazy text uses (instance, parent element). Namespace declaration uses 
        // (instance, parent element). Common XObjects uses (instance, null).
        object source;
        XElement parent;

        XmlNameTable nameTable;

        public XNodeNavigator(XNode node, XmlNameTable nameTable)
        {
            this.source = node;
            this.nameTable = nameTable != null ? nameTable : CreateNameTable();
        }

        public XNodeNavigator(XNodeNavigator other)
        {
            source = other.source;
            parent = other.parent;
            nameTable = other.nameTable;
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
                if (parent != null)
                {
                    return parent.BaseUri;
                }
                return string.Empty;
            }
        }

        public override bool HasAttributes
        {
            get
            {
                XElement e = source as XElement;
                if (e != null)
                {
                    XAttribute a = e.LastAttribute;
                    if (a != null)
                    {
                        do
                        {
                            a = a.NextAttribute;
                            if (!a.IsNamespaceDeclaration)
                            {
                                return true;
                            }
                        } while (a != e.LastAttribute);
                    }
                }
                return false;
            }
        }

        public override bool HasChildren
        {
            get
            {
                XContainer c = source as XContainer;
                if (c != null)
                {
                    XNode content = c.LastNode;
                    XNode n = content;
                    if (n != null)
                    {
                        do
                        {
                            n = n.NextNode;
                            if (IsContent(c, n))
                            {
                                return true;
                            }
                        } while (n != content);
                        return false;
                    }
                }
                return false;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
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
            XElement e = source as XElement;
            if (e != null)
            {
                return e.Name.LocalName;
            }
            XAttribute a = source as XAttribute;
            if (a != null)
            {
                if (parent != null && a.Name.NamespaceName.Length == 0)
                {
                    return string.Empty; // backcompat
                }
                return a.Name.LocalName;
            }
            XProcessingInstruction p = source as XProcessingInstruction;
            if (p != null)
            {
                return p.Target;
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
            XElement e = source as XElement;
            if (e != null)
            {
                return e.Name.NamespaceName;
            }
            XAttribute a = source as XAttribute;
            if (a != null)
            {
                if (parent != null)
                {
                    return string.Empty; // backcompat
                }
                return a.Name.NamespaceName;
            }
            return string.Empty;
        }

        public override XmlNameTable NameTable
        {
            get { return nameTable; }
        }

        public override XPathNodeType NodeType
        {
            get
            {
                XObject o = source as XObject;
                if (o != null)
                {
                    switch (o.NodeType)
                    {
                        case XmlNodeType.Element:
                            return XPathNodeType.Element;
                        case XmlNodeType.Attribute:
                            if (parent != null)
                            {
                                return XPathNodeType.Namespace;
                            }
                            return XPathNodeType.Attribute;
                        case XmlNodeType.Document:
                            return XPathNodeType.Root;
                        case XmlNodeType.Comment:
                            return XPathNodeType.Comment;
                        case XmlNodeType.ProcessingInstruction:
                            return XPathNodeType.ProcessingInstruction;
                        default:
                            return XPathNodeType.Text;
                    }
                }
                return XPathNodeType.Text;
            }
        }

        public override string Prefix
        {
            get { return nameTable.Add(GetPrefix()); }
        }

        string GetPrefix()
        {
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
                if (parent != null)
                {
                    return string.Empty; // backcompat
                }
                string prefix = a.GetPrefixOfNamespace(a.Name.Namespace);
                if (prefix != null)
                {
                    return prefix;
                }
            }
            return string.Empty;
        }

        public override object UnderlyingObject
        {
            get
            {
                if (source is string)
                {
                    // convert lazy text to eager text
                    source = parent.LastNode;
                    parent = null;
                }
                return source;
            }
        }

        public override string Value
        {
            get
            {
                XObject o = source as XObject;
                if (o != null)
                {
                    switch (o.NodeType)
                    {
                        case XmlNodeType.Element:
                            return ((XElement)o).Value;
                        case XmlNodeType.Attribute:
                            return ((XAttribute)o).Value;
                        case XmlNodeType.Document:
                            XElement root = ((XDocument)o).Root;
                            return root != null ? root.Value : string.Empty;
                        case XmlNodeType.Text:
                        case XmlNodeType.CDATA:
                            return CollectText((XText)o);
                        case XmlNodeType.Comment:
                            return ((XComment)o).Value;
                        case XmlNodeType.ProcessingInstruction:
                            return ((XProcessingInstruction)o).Data;
                        default:
                            return string.Empty;
                    }
                }
                return (string)source;
            }
        }

        public override XPathNavigator Clone()
        {
            return new XNodeNavigator(this);
        }

        public override bool IsSamePosition(XPathNavigator navigator)
        {
            XNodeNavigator other = navigator as XNodeNavigator;
            if (other == null)
            {
                return false;
            }
            return IsSamePosition(this, other);
        }

        public override bool MoveTo(XPathNavigator navigator)
        {
            XNodeNavigator other = navigator as XNodeNavigator;
            if (other != null)
            {
                source = other.source;
                parent = other.parent;
                return true;
            }
            return false;
        }

        public override bool MoveToAttribute(string localName, string namespaceName)
        {
            XElement e = source as XElement;
            if (e != null)
            {
                XAttribute a = e.LastAttribute;
                if (a != null)
                {
                    do
                    {
                        a = a.NextAttribute;
                        if (a.Name.LocalName == localName &&
                            a.Name.NamespaceName == namespaceName &&
                            !a.IsNamespaceDeclaration)
                        {
                            source = a;
                            return true;
                        }
                    } while (a != e.LastAttribute);
                }
            }
            return false;
        }

        public override bool MoveToChild(string localName, string namespaceName)
        {
            XContainer c = source as XContainer;
            if (c != null)
            {
                XNode content = c.LastNode;
                XNode n = content;
                if (n != null)
                {
                    do
                    {
                        n = n.NextNode;
                        XElement e = n as XElement;
                        if (e != null &&
                            e.Name.LocalName == localName &&
                            e.Name.NamespaceName == namespaceName)
                        {
                            source = e;
                            return true;
                        }
                    } while (n != content);
                }
            }
            return false;
        }

        public override bool MoveToChild(XPathNodeType type)
        {
            XContainer c = source as XContainer;
            if (c != null)
            {
                XNode content = c.LastNode;
                XNode n = content;
                if (n != null)
                {
                    int mask = GetElementContentMask(type);
                    if ((TextMask & mask) != 0 && c.GetParent() == null && c is XDocument)
                    {
                        mask &= ~TextMask;
                    }
                    do
                    {
                        n = n.NextNode;
                        if (((1 << (int)n.NodeType) & mask) != 0)
                        {
                            source = n;
                            return true;
                        }
                    } while (n != content);
                    return false;
                }
            }
            return false;
        }

        public override bool MoveToFirstAttribute()
        {
            XElement e = source as XElement;
            if (e != null)
            {
                XAttribute a = e.LastAttribute;
                if (a != null)
                {
                    do
                    {
                        a = a.NextAttribute;
                        if (!a.IsNamespaceDeclaration)
                        {
                            source = a;
                            return true;
                        }
                    } while (a != e.LastAttribute);
                }
            }
            return false;
        }

        public override bool MoveToFirstChild()
        {
            XContainer c = source as XContainer;
            if (c != null)
            {
                XNode content = c.LastNode;
                XNode n = content;
                if (n != null)
                {
                    do
                    {
                        n = n.NextNode;
                        if (IsContent(c, n))
                        {
                            source = n;
                            return true;
                        }
                    } while (n != content);
                    return false;
                }
            }
            return false;
        }

        public override bool MoveToFirstNamespace(XPathNamespaceScope scope)
        {
            XElement e = source as XElement;
            if (e != null)
            {
                XAttribute a = null;
                switch (scope)
                {
                    case XPathNamespaceScope.Local:
                        a = GetFirstNamespaceDeclarationLocal(e);
                        break;
                    case XPathNamespaceScope.ExcludeXml:
                        a = GetFirstNamespaceDeclarationGlobal(e);
                        while (a != null && a.Name.LocalName == "xml")
                        {
                            a = GetNextNamespaceDeclarationGlobal(a);
                        }
                        break;
                    case XPathNamespaceScope.All:
                        a = GetFirstNamespaceDeclarationGlobal(e);
                        if (a == null)
                        {
                            a = GetXmlNamespaceDeclaration();
                        }
                        break;
                }
                if (a != null)
                {
                    source = a;
                    parent = e;
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToId(string id)
        {
            throw new NotSupportedException(SR.NotSupported_MoveToId);
        }

        public override bool MoveToNamespace(string localName)
        {
            XElement e = source as XElement;
            if (e != null)
            {
                if (localName == "xmlns")
                {
                    return false; // backcompat
                }
                if (localName != null && localName.Length == 0)
                {
                    localName = "xmlns"; // backcompat
                }
                XAttribute a = GetFirstNamespaceDeclarationGlobal(e);
                while (a != null)
                {
                    if (a.Name.LocalName == localName)
                    {
                        source = a;
                        parent = e;
                        return true;
                    }
                    a = GetNextNamespaceDeclarationGlobal(a);
                }
                if (localName == "xml")
                {
                    source = GetXmlNamespaceDeclaration();
                    parent = e;
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToNext()
        {
            XNode n = source as XNode;
            if (n != null)
            {
                XContainer c = n.GetParent();
                if (c != null)
                {
                    XNode content = c.LastNode;
                    while (n != content)
                    {
                        XNode next = n.NextNode;
                        if (IsContent(c, next) && !(n is XText && next is XText))
                        {
                            source = next;
                            return true;
                        }
                        n = next;
                    }
                }
            }
            return false;
        }

        public override bool MoveToNext(string localName, string namespaceName)
        {
            XNode n = source as XNode;
            if (n != null)
            {
                XContainer c = n.GetParent();
                if (c != null)
                {
                    XNode content = c.LastNode;
                    while (n != content)
                    {
                        n = n.NextNode;
                        XElement e = n as XElement;
                        if (e != null &&
                            e.Name.LocalName == localName &&
                            e.Name.NamespaceName == namespaceName)
                        {
                            source = e;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool MoveToNext(XPathNodeType type)
        {
            XNode n = source as XNode;
            if (n != null)
            {
                XContainer c = n.GetParent();
                if (c != null)
                {
                    XNode content = c.LastNode;
                    int mask = GetElementContentMask(type);
                    if ((TextMask & mask) != 0 && c.GetParent() == null && c is XDocument)
                    {
                        mask &= ~TextMask;
                    }
                    while (n != content)
                    {
                        XNode next = n.NextNode;
                        if (((1 << (int)next.NodeType) & mask) != 0 && !(n is XText && next is XText))
                        {
                            source = next;
                            return true;
                        }
                        n = next;
                    }
                }
            }
            return false;
        }

        public override bool MoveToNextAttribute()
        {
            XAttribute a = source as XAttribute;
            if (a != null && parent == null)
            {
                XElement e = (XElement)a.GetParent();
                if (e != null)
                {
                    while (a != e.LastAttribute)
                    {
                        a = a.NextAttribute;
                        if (!a.IsNamespaceDeclaration)
                        {
                            source = a;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope scope)
        {
            XAttribute a = source as XAttribute;
            if (a != null && parent != null && !IsXmlNamespaceDeclaration(a))
            {
                switch (scope)
                {
                    case XPathNamespaceScope.Local:
                        if (a.GetParent() != parent)
                        {
                            return false;
                        }
                        a = GetNextNamespaceDeclarationLocal(a);
                        break;
                    case XPathNamespaceScope.ExcludeXml:
                        do
                        {
                            a = GetNextNamespaceDeclarationGlobal(a);
                        } while (a != null &&
                                 (a.Name.LocalName == "xml" ||
                                  HasNamespaceDeclarationInScope(a, parent)));
                        break;
                    case XPathNamespaceScope.All:
                        do
                        {
                            a = GetNextNamespaceDeclarationGlobal(a);
                        } while (a != null &&
                                 HasNamespaceDeclarationInScope(a, parent));
                        if (a == null &&
                            !HasNamespaceDeclarationInScope(GetXmlNamespaceDeclaration(), parent))
                        {
                            a = GetXmlNamespaceDeclaration();
                        }
                        break;
                }
                if (a != null)
                {
                    source = a;
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToParent()
        {
            if (parent != null)
            {
                source = parent;
                parent = null;
                return true;
            }
            XObject o = (XObject)source;
            if (o.GetParent() != null)
            {
                source = o.GetParent();
                return true;
            }
            return false;
        }

        public override bool MoveToPrevious()
        {
            XNode n = source as XNode;
            if (n != null)
            {
                XContainer c = n.GetParent();
                if (c != null)
                {
                    XNode q = c.LastNode;
                    if (q.NextNode != n)
                    {
                        XNode p = null;
                        do
                        {
                            q = q.NextNode;
                            if (IsContent(c, q))
                            {
                                p = p is XText && q is XText ? p : q;
                            }
                        } while (q.NextNode != n);
                        if (p != null)
                        {
                            source = p;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override XmlReader ReadSubtree()
        {
            XContainer c = source as XContainer;
            if (c == null) throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadNodeType, NodeType));
            return c.CreateReader();
        }

        bool IXmlLineInfo.HasLineInfo()
        {
            IXmlLineInfo li = source as IXmlLineInfo;
            if (li != null)
            {
                return li.HasLineInfo();
            }
            return false;
        }

        int IXmlLineInfo.LineNumber
        {
            get
            {
                IXmlLineInfo li = source as IXmlLineInfo;
                if (li != null)
                {
                    return li.LineNumber;
                }
                return 0;
            }
        }

        int IXmlLineInfo.LinePosition
        {
            get
            {
                IXmlLineInfo li = source as IXmlLineInfo;
                if (li != null)
                {
                    return li.LinePosition;
                }
                return 0;
            }
        }

        static string CollectText(XText n)
        {
            string s = n.Value;
            if (n.GetParent() != null)
            {
                while (n != n.GetParent().LastNode)
                {
                    n = n.NextNode as XText;
                    if (n == null) break;
                    s += n.Value;
                }
            }
            return s;
        }

        static XmlNameTable CreateNameTable()
        {
            XmlNameTable nameTable = new NameTable();
            nameTable.Add(string.Empty);
            nameTable.Add(xmlnsPrefixNamespace);
            nameTable.Add(xmlPrefixNamespace);
            return nameTable;
        }

        static bool IsContent(XContainer c, XNode n)
        {
            if (c.GetParent() != null || c is XElement)
            {
                return true;
            }
            return ((1 << (int)n.NodeType) & DocumentContentMask) != 0;
        }

        static bool IsSamePosition(XNodeNavigator n1, XNodeNavigator n2)
        {
            if (n1.source == n2.source && n1.parent == n2.parent)
            {
                return true;
            }
            // compare lazy text with eager text 
            if (n1.parent != null ^ n2.parent != null)
            {
                XText t1 = n1.source as XText;
                if (t1 != null)
                {
                    return (object)t1.Value == (object)n2.source && t1.GetParent() == n2.parent;
                }
                XText t2 = n2.source as XText;
                if (t2 != null)
                {
                    return (object)t2.Value == (object)n1.source && t2.GetParent() == n1.parent;
                }
            }
            return false;
        }

        static bool IsXmlNamespaceDeclaration(XAttribute a)
        {
            return (object)a == (object)GetXmlNamespaceDeclaration();
        }

        static int GetElementContentMask(XPathNodeType type)
        {
            return ElementContentMasks[(int)type];
        }

        static XAttribute GetFirstNamespaceDeclarationGlobal(XElement e)
        {
            do
            {
                XAttribute a = GetFirstNamespaceDeclarationLocal(e);
                if (a != null)
                {
                    return a;
                }
                e = e.Parent;
            } while (e != null);
            return null;
        }

        static XAttribute GetFirstNamespaceDeclarationLocal(XElement e)
        {
            XAttribute a = e.LastAttribute;
            if (a != null)
            {
                do
                {
                    a = a.NextAttribute;
                    if (a.IsNamespaceDeclaration)
                    {
                        return a;
                    }
                } while (a != e.LastAttribute);
            }
            return null;
        }

        static XAttribute GetNextNamespaceDeclarationGlobal(XAttribute a)
        {
            XElement e = (XElement)a.GetParent();
            if (e == null)
            {
                return null;
            }
            XAttribute next = GetNextNamespaceDeclarationLocal(a);
            if (next != null)
            {
                return next;
            }
            e = e.Parent;
            if (e == null)
            {
                return null;
            }
            return GetFirstNamespaceDeclarationGlobal(e);
        }

        static XAttribute GetNextNamespaceDeclarationLocal(XAttribute a)
        {
            XElement e = (XElement)a.GetParent();
            if (e == null)
            {
                return null;
            }
            while (a != e.LastAttribute)
            {
                a = a.NextAttribute;
                if (a.IsNamespaceDeclaration)
                {
                    return a;
                }
            }
            return null;
        }

        static XAttribute GetXmlNamespaceDeclaration()
        {
            if (XmlNamespaceDeclaration == null)
            {
                System.Threading.Interlocked.CompareExchange(ref XmlNamespaceDeclaration, new XAttribute(XNamespace.Xmlns.GetName("xml"), xmlPrefixNamespace), null);
            }
            return XmlNamespaceDeclaration;
        }

        static bool HasNamespaceDeclarationInScope(XAttribute a, XElement e)
        {
            XName name = a.Name;
            while (e != null && e != a.GetParent())
            {
                if (e.Attribute(name) != null)
                {
                    return true;
                }
                e = e.Parent;
            }
            return false;
        }
    }

    struct XPathEvaluator
    {
        public object Evaluate<T>(XNode node, string expression, IXmlNamespaceResolver resolver) where T : class
        {
            XPathNavigator navigator = node.CreateNavigator();
            object result = navigator.Evaluate(expression, resolver);
            if (result is XPathNodeIterator)
            {
                return EvaluateIterator<T>((XPathNodeIterator)result);
            }
            if (!(result is T)) throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedEvaluation, result.GetType()));
            return (T)result;
        }

        IEnumerable<T> EvaluateIterator<T>(XPathNodeIterator result)
        {
            foreach (XPathNavigator navigator in result)
            {
                object r = navigator.UnderlyingObject;
                if (!(r is T)) throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedEvaluation, r.GetType()));
                yield return (T)r;
                XText t = r as XText;
                if (t != null && t.GetParent() != null)
                {
                    while (t != t.GetParent().LastNode)
                    {
                        t = t.NextNode as XText;
                        if (t == null) break;
                        yield return (T)(object)t;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Creates an <see cref="XPathNavigator"/> for a given <see cref="XNode"/>
        /// </summary>
        /// <param name="node">Extension point <see cref="XNode"/></param>
        /// <returns>An <see cref="XPathNavigator"/></returns>
        public static XPathNavigator CreateNavigator(this XNode node)
        {
            return node.CreateNavigator(null);
        }

        /// <summary>
        /// Creates an <see cref="XPathNavigator"/> for a given <see cref="XNode"/>
        /// </summary>
        /// <param name="node">Extension point <see cref="XNode"/></param>
        /// <param name="nameTable">The <see cref="XmlNameTable"/> to be used by
        /// the <see cref="XPathNavigator"/></param>
        /// <returns>An <see cref="XPathNavigator"/></returns>
        public static XPathNavigator CreateNavigator(this XNode node, XmlNameTable nameTable)
        {
            if (node == null) throw new ArgumentNullException("node");
            if (node is XDocumentType) throw new ArgumentException(SR.Format(SR.Argument_CreateNavigator, XmlNodeType.DocumentType));
            XText text = node as XText;
            if (text != null)
            {
                if (text.GetParent() is XDocument) throw new ArgumentException(SR.Format(SR.Argument_CreateNavigator, XmlNodeType.Whitespace));
                node = CalibrateText(text);
            }
            return new XNodeNavigator(node, nameTable);
        }

        /// <summary>
        /// Evaluates an XPath expression
        /// </summary>
        /// <param name="node">Extension point <see cref="XNode"/></param>
        /// <param name="expression">The XPath expression</param>
        /// <returns>The result of evaluating the expression which can be typed as bool, double, string or
        /// IEnumerable</returns>
        public static object XPathEvaluate(this XNode node, string expression)
        {
            return node.XPathEvaluate(expression, null);
        }

        /// <summary>
        /// Evaluates an XPath expression
        /// </summary>
        /// <param name="node">Extension point <see cref="XNode"/></param>
        /// <param name="expression">The XPath expression</param>
        /// <param name="resolver">A <see cref="IXmlNamespaceResolver"> for the namespace
        /// prefixes used in the XPath expression</see></param>
        /// <returns>The result of evaluating the expression which can be typed as bool, double, string or
        /// IEnumerable</returns>
        public static object XPathEvaluate(this XNode node, string expression, IXmlNamespaceResolver resolver)
        {
            if (node == null) throw new ArgumentNullException("node");
            return new XPathEvaluator().Evaluate<object>(node, expression, resolver);
        }

        /// <summary>
        /// Select an <see cref="XElement"/> using a XPath expression
        /// </summary>
        /// <param name="node">Extension point <see cref="XNode"/></param>
        /// <param name="expression">The XPath expression</param>
        /// <returns>An <see cref="XElement"> or null</see></returns>
        public static XElement XPathSelectElement(this XNode node, string expression)
        {
            return node.XPathSelectElement(expression, null);
        }

        /// <summary>
        /// Select an <see cref="XElement"/> using a XPath expression
        /// </summary>
        /// <param name="node">Extension point <see cref="XNode"/></param>
        /// <param name="expression">The XPath expression</param>
        /// <param name="resolver">A <see cref="IXmlNamespaceResolver"/> for the namespace
        /// prefixes used in the XPath expression</param>
        /// <returns>An <see cref="XElement"> or null</see></returns>
        public static XElement XPathSelectElement(this XNode node, string expression, IXmlNamespaceResolver resolver)
        {
            return node.XPathSelectElements(expression, resolver).FirstOrDefault();
        }

        /// <summary>
        /// Select a set of <see cref="XElement"/> using a XPath expression
        /// </summary>
        /// <param name="node">Extension point <see cref="XNode"/></param>
        /// <param name="expression">The XPath expression</param>
        /// <returns>An <see cref="IEnumerable&lt;XElement&gt;"/> corresponding to the resulting set of elements</returns>
        public static IEnumerable<XElement> XPathSelectElements(this XNode node, string expression)
        {
            return node.XPathSelectElements(expression, null);
        }

        /// <summary>
        /// Select a set of <see cref="XElement"/> using a XPath expression
        /// </summary>
        /// <param name="node">Extension point <see cref="XNode"/></param>
        /// <param name="expression">The XPath expression</param>
        /// <param name="resolver">A <see cref="IXmlNamespaceResolver"/> for the namespace
        /// prefixes used in the XPath expression</param>
        /// <returns>An <see cref="IEnumerable&lt;XElement&gt;"/> corresponding to the resulting set of elements</returns>
        public static IEnumerable<XElement> XPathSelectElements(this XNode node, string expression, IXmlNamespaceResolver resolver)
        {
            if (node == null) throw new ArgumentNullException("node");
            return (IEnumerable<XElement>)new XPathEvaluator().Evaluate<XElement>(node, expression, resolver);
        }

        static XText CalibrateText(XText n)
        {
            if (n.GetParent() == null)
            {
                return n;
            }
            XNode p = n.GetParent().LastNode;
            while (true)
            {
                p = p.NextNode;
                XText t = p as XText;
                if (t != null)
                {
                    do
                    {
                        if (p == n)
                        {
                            return t;
                        }
                        p = p.NextNode;
                    } while (p is XText);
                }
            }
        }
    }
}
