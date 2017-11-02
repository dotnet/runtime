// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace System.Xml.XPath
{
    internal class XNodeNavigator : XPathNavigator, IXmlLineInfo
    {
        internal static readonly string xmlPrefixNamespace = XNamespace.Xml.NamespaceName;
        internal static readonly string xmlnsPrefixNamespace = XNamespace.Xmlns.NamespaceName;
        private const int DocumentContentMask =
            (1 << (int)XmlNodeType.Element) |
            (1 << (int)XmlNodeType.ProcessingInstruction) |
            (1 << (int)XmlNodeType.Comment);
        private static readonly int[] s_ElementContentMasks = {
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
        private const int TextMask =
            (1 << (int)XmlNodeType.CDATA) |
            (1 << (int)XmlNodeType.Text);

        private static XAttribute s_XmlNamespaceDeclaration;

        // The navigator position is encoded by the tuple (source, parent).
        // Namespace declaration uses (instance, parent element).
        // Common XObjects uses (instance, null).
        private XObject _source;
        private XElement _parent;

        private XmlNameTable _nameTable;

        public XNodeNavigator(XNode node, XmlNameTable nameTable)
        {
            _source = node;
            _nameTable = nameTable != null ? nameTable : CreateNameTable();
        }

        public XNodeNavigator(XNodeNavigator other)
        {
            _source = other._source;
            _parent = other._parent;
            _nameTable = other._nameTable;
        }

        public override string BaseURI
        {
            get
            {
                if (_source != null)
                {
                    return _source.BaseUri;
                }
                if (_parent != null)
                {
                    return _parent.BaseUri;
                }
                return string.Empty;
            }
        }

        public override bool HasAttributes
        {
            get
            {
                XElement element = _source as XElement;
                if (element != null)
                {
                    foreach (XAttribute attribute in element.Attributes())
                    {
                        if (!attribute.IsNamespaceDeclaration)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public override bool HasChildren
        {
            get
            {
                XContainer container = _source as XContainer;
                if (container != null)
                {
                    foreach (XNode node in container.Nodes())
                    {
                        if (IsContent(container, node))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                XElement e = _source as XElement;
                return e != null && e.IsEmpty;
            }
        }

        public override string LocalName
        {
            get { return _nameTable.Add(GetLocalName()); }
        }

        private string GetLocalName()
        {
            XElement e = _source as XElement;
            if (e != null)
            {
                return e.Name.LocalName;
            }
            XAttribute a = _source as XAttribute;
            if (a != null)
            {
                if (_parent != null && a.Name.NamespaceName.Length == 0)
                {
                    return string.Empty; // backcompat
                }
                return a.Name.LocalName;
            }
            XProcessingInstruction p = _source as XProcessingInstruction;
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
                    return _nameTable.Add(GetLocalName());
                }
                return _nameTable.Add(string.Concat(prefix, ":", GetLocalName()));
            }
        }

        public override string NamespaceURI
        {
            get { return _nameTable.Add(GetNamespaceURI()); }
        }

        private string GetNamespaceURI()
        {
            XElement e = _source as XElement;
            if (e != null)
            {
                return e.Name.NamespaceName;
            }
            XAttribute a = _source as XAttribute;
            if (a != null)
            {
                if (_parent != null)
                {
                    return string.Empty; // backcompat
                }
                return a.Name.NamespaceName;
            }
            return string.Empty;
        }

        public override XmlNameTable NameTable
        {
            get { return _nameTable; }
        }

        public override XPathNodeType NodeType
        {
            get
            {
                if (_source != null)
                {
                    switch (_source.NodeType)
                    {
                        case XmlNodeType.Element:
                            return XPathNodeType.Element;
                        case XmlNodeType.Attribute:
                            XAttribute attribute = (XAttribute)_source;
                            return attribute.IsNamespaceDeclaration ? XPathNodeType.Namespace : XPathNodeType.Attribute;
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
            get { return _nameTable.Add(GetPrefix()); }
        }

        private string GetPrefix()
        {
            XElement e = _source as XElement;
            if (e != null)
            {
                string prefix = e.GetPrefixOfNamespace(e.Name.Namespace);
                if (prefix != null)
                {
                    return prefix;
                }
                return string.Empty;
            }
            XAttribute a = _source as XAttribute;
            if (a != null)
            {
                if (_parent != null)
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
                return _source;
            }
        }

        public override string Value
        {
            get
            {
                if (_source != null)
                {
                    switch (_source.NodeType)
                    {
                        case XmlNodeType.Element:
                            return ((XElement)_source).Value;
                        case XmlNodeType.Attribute:
                            return ((XAttribute)_source).Value;
                        case XmlNodeType.Document:
                            XElement root = ((XDocument)_source).Root;
                            return root != null ? root.Value : string.Empty;
                        case XmlNodeType.Text:
                        case XmlNodeType.CDATA:
                            return CollectText((XText)_source);
                        case XmlNodeType.Comment:
                            return ((XComment)_source).Value;
                        case XmlNodeType.ProcessingInstruction:
                            return ((XProcessingInstruction)_source).Data;
                        default:
                            return string.Empty;
                    }
                }
                return string.Empty;
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
                _source = other._source;
                _parent = other._parent;
                return true;
            }
            return false;
        }

        public override bool MoveToAttribute(string localName, string namespaceName)
        {
            XElement e = _source as XElement;
            if (e != null)
            {
                foreach (XAttribute attribute in e.Attributes())
                {
                    if (attribute.Name.LocalName == localName &&
                        attribute.Name.NamespaceName == namespaceName &&
                        !attribute.IsNamespaceDeclaration)
                    {
                        _source = attribute;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool MoveToChild(string localName, string namespaceName)
        {
            XContainer c = _source as XContainer;
            if (c != null)
            {
                foreach (XElement element in c.Elements())
                {
                    if (element.Name.LocalName == localName &&
                        element.Name.NamespaceName == namespaceName)
                    {
                        _source = element;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool MoveToChild(XPathNodeType type)
        {
            XContainer c = _source as XContainer;
            if (c != null)
            {
                int mask = GetElementContentMask(type);
                if ((TextMask & mask) != 0 && c.GetParent() == null && c is XDocument)
                {
                    mask &= ~TextMask;
                }
                foreach (XNode node in c.Nodes())
                {
                    if (((1 << (int)node.NodeType) & mask) != 0)
                    {
                        _source = node;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool MoveToFirstAttribute()
        {
            XElement e = _source as XElement;
            if (e != null)
            {
                foreach (XAttribute attribute in e.Attributes())
                {
                    if (!attribute.IsNamespaceDeclaration)
                    {
                        _source = attribute;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool MoveToFirstChild()
        {
            XContainer container = _source as XContainer;
            if (container != null)
            {
                foreach (XNode node in container.Nodes())
                {
                    if (IsContent(container, node))
                    {
                        _source = node;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool MoveToFirstNamespace(XPathNamespaceScope scope)
        {
            XElement e = _source as XElement;
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
                    _source = a;
                    _parent = e;
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
            XElement e = _source as XElement;
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
                        _source = a;
                        _parent = e;
                        return true;
                    }
                    a = GetNextNamespaceDeclarationGlobal(a);
                }
                if (localName == "xml")
                {
                    _source = GetXmlNamespaceDeclaration();
                    _parent = e;
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToNext()
        {
            XNode currentNode = _source as XNode;
            if (currentNode != null)
            {
                XContainer container = currentNode.GetParent();
                if (container != null)
                {
                    XNode next = null;
                    for (XNode node = currentNode; node != null; node = next)
                    {
                        next = node.NextNode;
                        if (next == null)
                        {
                            break;
                        }
                        if (IsContent(container, next) && !(node is XText && next is XText))
                        {
                            _source = next;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool MoveToNext(string localName, string namespaceName)
        {
            XNode currentNode = _source as XNode;
            if (currentNode != null)
            {
                foreach (XElement element in currentNode.ElementsAfterSelf())
                {
                    if (element.Name.LocalName == localName &&
                        element.Name.NamespaceName == namespaceName)
                    {
                        _source = element;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool MoveToNext(XPathNodeType type)
        {
            XNode currentNode = _source as XNode;
            if (currentNode != null)
            {
                XContainer container = currentNode.GetParent();
                if (container != null)
                {
                    int mask = GetElementContentMask(type);
                    if ((TextMask & mask) != 0 && container.GetParent() == null && container is XDocument)
                    {
                        mask &= ~TextMask;
                    }
                    XNode next = null;
                    for (XNode node = currentNode; node != null; node = next)
                    {
                        next = node.NextNode;
                        if (((1 << (int)next.NodeType) & mask) != 0 && !(node is XText && next is XText))
                        {
                            _source = next;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool MoveToNextAttribute()
        {
            XAttribute currentAttribute = _source as XAttribute;
            if (currentAttribute != null && _parent == null)
            {
                XElement e = (XElement)currentAttribute.GetParent();
                if (e != null)
                {
                    for (XAttribute attribute = currentAttribute.NextAttribute; attribute != null; attribute = attribute.NextAttribute)
                    {
                        if (!attribute.IsNamespaceDeclaration)
                        {
                            _source = attribute;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope scope)
        {
            XAttribute a = _source as XAttribute;
            if (a != null && _parent != null && !IsXmlNamespaceDeclaration(a))
            {
                switch (scope)
                {
                    case XPathNamespaceScope.Local:
                        if (a.GetParent() != _parent)
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
                                  HasNamespaceDeclarationInScope(a, _parent)));
                        break;
                    case XPathNamespaceScope.All:
                        do
                        {
                            a = GetNextNamespaceDeclarationGlobal(a);
                        } while (a != null &&
                                 HasNamespaceDeclarationInScope(a, _parent));
                        if (a == null &&
                            !HasNamespaceDeclarationInScope(GetXmlNamespaceDeclaration(), _parent))
                        {
                            a = GetXmlNamespaceDeclaration();
                        }
                        break;
                }
                if (a != null)
                {
                    _source = a;
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToParent()
        {
            if (_parent != null)
            {
                _source = _parent;
                _parent = null;
                return true;
            }
            XNode parentNode = _source.GetParent();
            if (parentNode != null)
            {
                _source = parentNode;
                return true;
            }
            return false;
        }

        public override bool MoveToPrevious()
        {
            XNode currentNode = _source as XNode;
            if (currentNode != null)
            {
                XContainer container = currentNode.GetParent();
                if (container != null)
                {
                    XNode previous = null;
                    foreach (XNode node in container.Nodes())
                    {
                        if (node == currentNode)
                        {
                            if (previous != null)
                            {
                                _source = previous;
                                return true;
                            }
                            return false;
                        }

                        if (IsContent(container, node))
                        {
                            previous = node;
                        }
                    }
                }
            }
            return false;
        }

        public override XmlReader ReadSubtree()
        {
            XContainer c = _source as XContainer;
            if (c == null) throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadNodeType, NodeType));
            return c.CreateReader();
        }

        bool IXmlLineInfo.HasLineInfo()
        {
            IXmlLineInfo li = _source as IXmlLineInfo;
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
                IXmlLineInfo li = _source as IXmlLineInfo;
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
                IXmlLineInfo li = _source as IXmlLineInfo;
                if (li != null)
                {
                    return li.LinePosition;
                }
                return 0;
            }
        }

        private static string CollectText(XText n)
        {
            string s = n.Value;
            if (n.GetParent() != null)
            {
                foreach (XNode node in n.NodesAfterSelf())
                {
                    XText t = node as XText;
                    if (t == null) break;
                    s += t.Value;
                }
            }
            return s;
        }

        private static XmlNameTable CreateNameTable()
        {
            XmlNameTable nameTable = new NameTable();
            nameTable.Add(string.Empty);
            nameTable.Add(xmlnsPrefixNamespace);
            nameTable.Add(xmlPrefixNamespace);
            return nameTable;
        }

        private static bool IsContent(XContainer c, XNode n)
        {
            if (c.GetParent() != null || c is XElement)
            {
                return true;
            }
            return ((1 << (int)n.NodeType) & DocumentContentMask) != 0;
        }

        private static bool IsSamePosition(XNodeNavigator n1, XNodeNavigator n2)
        {
            return n1._source == n2._source && n1._source.GetParent() == n2._source.GetParent();
        }

        private static bool IsXmlNamespaceDeclaration(XAttribute a)
        {
            return (object)a == (object)GetXmlNamespaceDeclaration();
        }

        private static int GetElementContentMask(XPathNodeType type)
        {
            return s_ElementContentMasks[(int)type];
        }

        private static XAttribute GetFirstNamespaceDeclarationGlobal(XElement e)
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

        private static XAttribute GetFirstNamespaceDeclarationLocal(XElement e)
        {
            foreach (XAttribute attribute in e.Attributes())
            {
                if (attribute.IsNamespaceDeclaration)
                {
                    return attribute;
                }
            }
            return null;
        }

        private static XAttribute GetNextNamespaceDeclarationGlobal(XAttribute a)
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

        private static XAttribute GetNextNamespaceDeclarationLocal(XAttribute a)
        {
            XElement e = a.Parent;
            if (e == null)
            {
                return null;
            }
            a = a.NextAttribute;
            while (a != null)
            {
                if (a.IsNamespaceDeclaration)
                {
                    return a;
                }
                a = a.NextAttribute;
            }
            return null;
        }

        private static XAttribute GetXmlNamespaceDeclaration()
        {
            if (s_XmlNamespaceDeclaration == null)
            {
                System.Threading.Interlocked.CompareExchange(ref s_XmlNamespaceDeclaration, new XAttribute(XNamespace.Xmlns.GetName("xml"), xmlPrefixNamespace), null);
            }
            return s_XmlNamespaceDeclaration;
        }

        private static bool HasNamespaceDeclarationInScope(XAttribute a, XElement e)
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

    internal readonly struct XPathEvaluator
    {
        public object Evaluate<T>(XNode node, string expression, IXmlNamespaceResolver resolver) where T : class
        {
            XPathNavigator navigator = node.CreateNavigator();
            object result = navigator.Evaluate(expression, resolver);
            XPathNodeIterator iterator = result as XPathNodeIterator;
            if (iterator != null)
            {
                return EvaluateIterator<T>(iterator);
            }
            if (!(result is T)) throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedEvaluation, result.GetType()));
            return (T)result;
        }

        private IEnumerable<T> EvaluateIterator<T>(XPathNodeIterator result)
        {
            foreach (XPathNavigator navigator in result)
            {
                object r = navigator.UnderlyingObject;
                if (!(r is T)) throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedEvaluation, r.GetType()));
                yield return (T)r;
                XText t = r as XText;
                if (t != null && t.GetParent() != null)
                {
                    do
                    {
                        t = t.NextNode as XText;
                        if (t == null) break;
                        yield return (T)(object)t;
                    } while (t != t.GetParent().LastNode);
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
            if (node == null) throw new ArgumentNullException(nameof(node));
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
            if (node == null) throw new ArgumentNullException(nameof(node));
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
            if (node == null) throw new ArgumentNullException(nameof(node));
            return (IEnumerable<XElement>)new XPathEvaluator().Evaluate<XElement>(node, expression, resolver);
        }

        private static XText CalibrateText(XText n)
        {
            XContainer parentNode = n.GetParent();
            if (parentNode == null)
            {
                return n;
            }
            foreach (XNode node in parentNode.Nodes())
            {
                XText t = node as XText;
                bool isTextNode = t != null;
                if (isTextNode && node == n)
                {
                    return t;
                }
            }

            System.Diagnostics.Debug.Fail("Parent node doesn't contain itself.");
            return null;
        }
    }
}
