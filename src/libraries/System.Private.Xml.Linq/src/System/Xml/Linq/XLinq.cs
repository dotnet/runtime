// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using IEnumerable = System.Collections.IEnumerable;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

namespace System.Xml.Linq
{
    internal struct Inserter
    {
        private readonly XContainer _parent;
        private XNode? _previous;
        private string? _text;

        public Inserter(XContainer parent, XNode? anchor)
        {
            _parent = parent;
            _previous = anchor;
            _text = null;
        }

        public void Add(object? content)
        {
            AddContent(content);
            if (_text != null)
            {
                if (_parent.content == null)
                {
                    if (_parent.SkipNotify())
                    {
                        _parent.content = _text;
                    }
                    else
                    {
                        if (_text.Length > 0)
                        {
                            InsertNode(new XText(_text));
                        }
                        else
                        {
                            if (_parent is XElement)
                            {
                                // Change in the serialization of an empty element:
                                // from empty tag to start/end tag pair
                                _parent.NotifyChanging(_parent, XObjectChangeEventArgs.Value);
                                if (_parent.content != null) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
                                _parent.content = _text;
                                _parent.NotifyChanged(_parent, XObjectChangeEventArgs.Value);
                            }
                            else
                            {
                                _parent.content = _text;
                            }
                        }
                    }
                }
                else if (_text.Length > 0)
                {
                    XText? prevXText = _previous as XText;
                    if (prevXText != null && !(_previous is XCData))
                    {
                        prevXText.Value += _text;
                    }
                    else
                    {
                        _parent.ConvertTextToNode();
                        InsertNode(new XText(_text));
                    }
                }
            }
        }

        private void AddContent(object? content)
        {
            if (content == null) return;
            XNode? n = content as XNode;
            if (n != null)
            {
                AddNode(n);
                return;
            }
            string? s = content as string;
            if (s != null)
            {
                AddString(s);
                return;
            }
            XStreamingElement? x = content as XStreamingElement;
            if (x != null)
            {
                AddNode(new XElement(x));
                return;
            }
            object?[]? o = content as object?[];
            if (o != null)
            {
                foreach (object? obj in o) AddContent(obj);
                return;
            }
            IEnumerable? e = content as IEnumerable;
            if (e != null)
            {
                foreach (object? obj in e) AddContent(obj);
                return;
            }
            if (content is XAttribute) throw new ArgumentException(SR.Argument_AddAttribute);
            AddString(XContainer.GetStringValue(content));
        }

        private void AddNode(XNode n)
        {
            _parent.ValidateNode(n, _previous);
            if (n.parent != null)
            {
                n = n.CloneNode();
            }
            else
            {
                XNode p = _parent;
                while (p.parent != null) p = p.parent;
                if (n == p) n = n.CloneNode();
            }
            _parent.ConvertTextToNode();
            if (_text != null)
            {
                if (_text.Length > 0)
                {
                    XText? prevXText = _previous as XText;
                    if (prevXText != null && !(_previous is XCData))
                    {
                        prevXText.Value += _text;
                    }
                    else
                    {
                        InsertNode(new XText(_text));
                    }
                }
                _text = null;
            }
            InsertNode(n);
        }

        private void AddString(string s)
        {
            _parent.ValidateString(s);
            _text += s;
        }

        // Prepends if previous == null, otherwise inserts after previous
        private void InsertNode(XNode n)
        {
            bool notify = _parent.NotifyChanging(n, XObjectChangeEventArgs.Add);
            if (n.parent != null) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
            n.parent = _parent;
            if (_parent.content == null || _parent.content is string)
            {
                n.next = n;
                _parent.content = n;
            }
            else if (_previous == null)
            {
                XNode last = (XNode)_parent.content;
                n.next = last.next;
                last.next = n;
            }
            else
            {
                n.next = _previous.next;
                _previous.next = n;
                if (_parent.content == _previous) _parent.content = n;
            }
            _previous = n;
            if (notify) _parent.NotifyChanged(n, XObjectChangeEventArgs.Add);
        }
    }

    internal struct NamespaceCache
    {
        private XNamespace _ns;
        private string _namespaceName;

        public XNamespace Get(string namespaceName)
        {
            if ((object)namespaceName == (object)_namespaceName) return _ns;
            _namespaceName = namespaceName;
            _ns = XNamespace.Get(namespaceName);
            return _ns;
        }
    }

    internal struct ElementWriter
    {
        private readonly XmlWriter _writer;
        private NamespaceResolver _resolver;

        public ElementWriter(XmlWriter writer)
        {
            _writer = writer;
            _resolver = default;
        }

        public void WriteElement(XElement e)
        {
            PushAncestors(e);
            XElement root = e;
            XNode n = e;
            while (true)
            {
                XElement? current = n as XElement;
                if (current != null)
                {
                    WriteStartElement(current);
                    if (current.content == null)
                    {
                        WriteEndElement();
                    }
                    else
                    {
                        string? s = current.content as string;
                        if (s != null)
                        {
                            _writer.WriteString(s);
                            WriteFullEndElement();
                        }
                        else
                        {
                            n = ((XNode)current.content).next!;
                            continue;
                        }
                    }
                }
                else
                {
                    n.WriteTo(_writer);
                }
                while (n != root && n == n.parent!.content)
                {
                    n = n.parent;
                    WriteFullEndElement();
                }
                if (n == root) break;
                n = n.next!;
            }
        }

        public async Task WriteElementAsync(XElement e, CancellationToken cancellationToken)
        {
            PushAncestors(e);
            XElement root = e;
            XNode n = e;
            while (true)
            {
                XElement? current = n as XElement;
                if (current != null)
                {
                    await WriteStartElementAsync(current, cancellationToken).ConfigureAwait(false);
                    if (current.content == null)
                    {
                        await WriteEndElementAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        string? s = current.content as string;
                        if (s != null)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await _writer.WriteStringAsync(s).ConfigureAwait(false);
                            await WriteFullEndElementAsync(cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            n = ((XNode)current.content).next!;
                            continue;
                        }
                    }
                }
                else
                {
                    await n.WriteToAsync(_writer, cancellationToken).ConfigureAwait(false);
                }
                while (n != root && n == n.parent!.content)
                {
                    n = n.parent;
                    await WriteFullEndElementAsync(cancellationToken).ConfigureAwait(false);
                }
                if (n == root) break;
                n = n.next!;
            }
        }

        private string? GetPrefixOfNamespace(XNamespace ns, bool allowDefaultNamespace)
        {
            string namespaceName = ns.NamespaceName;
            if (namespaceName.Length == 0) return string.Empty;
            string? prefix = _resolver.GetPrefixOfNamespace(ns, allowDefaultNamespace);
            if (prefix != null) return prefix;
            if ((object)namespaceName == (object)XNamespace.xmlPrefixNamespace) return "xml";
            if ((object)namespaceName == (object)XNamespace.xmlnsPrefixNamespace) return "xmlns";
            return null;
        }

        private void PushAncestors(XElement? e)
        {
            while (true)
            {
                e = e!.parent as XElement;
                if (e == null) break;
                XAttribute? a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next!;
                        if (a.IsNamespaceDeclaration)
                        {
                            _resolver.AddFirst(a.Name.NamespaceName.Length == 0 ? string.Empty : a.Name.LocalName, XNamespace.Get(a.Value));
                        }
                    } while (a != e.lastAttr);
                }
            }
        }

        private void PushElement(XElement e)
        {
            _resolver.PushScope();
            XAttribute? a = e.lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next!;
                    if (a.IsNamespaceDeclaration)
                    {
                        _resolver.Add(a.Name.NamespaceName.Length == 0 ? string.Empty : a.Name.LocalName, XNamespace.Get(a.Value));
                    }
                } while (a != e.lastAttr);
            }
        }

        private void WriteEndElement()
        {
            _writer.WriteEndElement();
            _resolver.PopScope();
        }

        private async Task WriteEndElementAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _writer.WriteEndElementAsync().ConfigureAwait(false);
            _resolver.PopScope();
        }

        private void WriteFullEndElement()
        {
            _writer.WriteFullEndElement();
            _resolver.PopScope();
        }

        private async Task WriteFullEndElementAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _writer.WriteFullEndElementAsync().ConfigureAwait(false);
            _resolver.PopScope();
        }

        private void WriteStartElement(XElement e)
        {
            PushElement(e);
            XNamespace ns = e.Name.Namespace;
            _writer.WriteStartElement(GetPrefixOfNamespace(ns, true), e.Name.LocalName, ns.NamespaceName);
            XAttribute? a = e.lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next!;
                    ns = a.Name.Namespace;
                    string localName = a.Name.LocalName;
                    string namespaceName = ns.NamespaceName;
                    _writer.WriteAttributeString(GetPrefixOfNamespace(ns, false), localName, namespaceName.Length == 0 && localName == "xmlns" ? XNamespace.xmlnsPrefixNamespace : namespaceName, a.Value);
                } while (a != e.lastAttr);
            }
        }

        private async Task WriteStartElementAsync(XElement e, CancellationToken cancellationToken)
        {
            PushElement(e);
            XNamespace ns = e.Name.Namespace;
            await _writer.WriteStartElementAsync(GetPrefixOfNamespace(ns, true), e.Name.LocalName, ns.NamespaceName).ConfigureAwait(false);
            XAttribute? a = e.lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next!;
                    ns = a.Name.Namespace;
                    string localName = a.Name.LocalName;
                    string namespaceName = ns.NamespaceName;
                    await _writer.WriteAttributeStringAsync(GetPrefixOfNamespace(ns, false), localName, namespaceName.Length == 0 && localName == "xmlns" ? XNamespace.xmlnsPrefixNamespace : namespaceName, a.Value).ConfigureAwait(false);
                } while (a != e.lastAttr);
            }
        }
    }

    internal struct NamespaceResolver
    {
        private sealed class NamespaceDeclaration
        {
            public string prefix = null!;
            public XNamespace ns = null!;
            public int scope;
            public NamespaceDeclaration prev = null!;
        }

        private int _scope;
        private NamespaceDeclaration? _declaration;
        private NamespaceDeclaration? _rover;

        public void PushScope()
        {
            _scope++;
        }

        public void PopScope()
        {
            NamespaceDeclaration? d = _declaration;
            if (d != null)
            {
                do
                {
                    d = d.prev;
                    if (d.scope != _scope) break;
                    if (d == _declaration)
                    {
                        _declaration = null;
                    }
                    else
                    {
                        _declaration!.prev = d.prev;
                    }
                    _rover = null;
                } while (d != _declaration && _declaration != null);
            }
            _scope--;
        }

        public void Add(string prefix, XNamespace ns)
        {
            NamespaceDeclaration d = new NamespaceDeclaration();
            d.prefix = prefix;
            d.ns = ns;
            d.scope = _scope;
            if (_declaration == null)
            {
                _declaration = d;
            }
            else
            {
                d.prev = _declaration.prev;
            }
            _declaration.prev = d;
            _rover = null;
        }

        public void AddFirst(string prefix, XNamespace ns)
        {
            NamespaceDeclaration d = new NamespaceDeclaration();
            d.prefix = prefix;
            d.ns = ns;
            d.scope = _scope;
            if (_declaration == null)
            {
                d.prev = d;
            }
            else
            {
                d.prev = _declaration.prev;
                _declaration.prev = d;
            }
            _declaration = d;
            _rover = null;
        }

        // Only elements allow default namespace declarations. The rover
        // caches the last namespace declaration used by an element.
        public string? GetPrefixOfNamespace(XNamespace ns, bool allowDefaultNamespace)
        {
            if (_rover != null && _rover.ns == ns && (allowDefaultNamespace || _rover.prefix.Length > 0)) return _rover.prefix;
            NamespaceDeclaration? d = _declaration;
            if (d != null)
            {
                do
                {
                    d = d.prev;
                    if (d.ns == ns)
                    {
                        NamespaceDeclaration x = _declaration!.prev;
                        while (x != d && x.prefix != d.prefix)
                        {
                            x = x.prev;
                        }
                        if (x == d)
                        {
                            if (allowDefaultNamespace)
                            {
                                _rover = d;
                                return d.prefix;
                            }
                            else if (d.prefix.Length > 0)
                            {
                                return d.prefix;
                            }
                        }
                    }
                } while (d != _declaration);
            }
            return null;
        }
    }

    internal struct StreamingElementWriter
    {
        private readonly XmlWriter _writer;
        private XStreamingElement? _element;
        private readonly List<XAttribute> _attributes;
        private NamespaceResolver _resolver;

        public StreamingElementWriter(XmlWriter w)
        {
            _writer = w;
            _element = null;
            _attributes = new List<XAttribute>();
            _resolver = default;
        }

        private void FlushElement()
        {
            if (_element != null)
            {
                PushElement();
                XNamespace ns = _element.Name.Namespace;
                _writer.WriteStartElement(GetPrefixOfNamespace(ns, true), _element.Name.LocalName, ns.NamespaceName);
                foreach (XAttribute a in _attributes)
                {
                    ns = a.Name.Namespace;
                    string localName = a.Name.LocalName;
                    string namespaceName = ns.NamespaceName;
                    _writer.WriteAttributeString(GetPrefixOfNamespace(ns, false), localName, namespaceName.Length == 0 && localName == "xmlns" ? XNamespace.xmlnsPrefixNamespace : namespaceName, a.Value);
                }
                _element = null;
                _attributes.Clear();
            }
        }

        private string? GetPrefixOfNamespace(XNamespace ns, bool allowDefaultNamespace)
        {
            string namespaceName = ns.NamespaceName;
            if (namespaceName.Length == 0) return string.Empty;
            string? prefix = _resolver.GetPrefixOfNamespace(ns, allowDefaultNamespace);
            if (prefix != null) return prefix;
            if ((object)namespaceName == (object)XNamespace.xmlPrefixNamespace) return "xml";
            if ((object)namespaceName == (object)XNamespace.xmlnsPrefixNamespace) return "xmlns";
            return null;
        }

        private void PushElement()
        {
            _resolver.PushScope();
            foreach (XAttribute a in _attributes)
            {
                if (a.IsNamespaceDeclaration)
                {
                    _resolver.Add(a.Name.NamespaceName.Length == 0 ? string.Empty : a.Name.LocalName, XNamespace.Get(a.Value));
                }
            }
        }

        private void Write(object? content)
        {
            if (content == null) return;
            XNode? n = content as XNode;
            if (n != null)
            {
                WriteNode(n);
                return;
            }
            string? s = content as string;
            if (s != null)
            {
                WriteString(s);
                return;
            }
            XAttribute? a = content as XAttribute;
            if (a != null)
            {
                WriteAttribute(a);
                return;
            }
            XStreamingElement? x = content as XStreamingElement;
            if (x != null)
            {
                WriteStreamingElement(x);
                return;
            }
            object[]? o = content as object[];
            if (o != null)
            {
                foreach (object obj in o) Write(obj);
                return;
            }
            IEnumerable? e = content as IEnumerable;
            if (e != null)
            {
                foreach (object obj in e) Write(obj);
                return;
            }
            WriteString(XContainer.GetStringValue(content));
        }

        private void WriteAttribute(XAttribute a)
        {
            if (_element == null) throw new InvalidOperationException(SR.InvalidOperation_WriteAttribute);
            _attributes.Add(a);
        }

        private void WriteNode(XNode n)
        {
            FlushElement();
            n.WriteTo(_writer);
        }

        internal void WriteStreamingElement(XStreamingElement e)
        {
            FlushElement();
            _element = e;
            Write(e.content);
            FlushElement();
            _writer.WriteEndElement();
            _resolver.PopScope();
        }

        private void WriteString(string s)
        {
            FlushElement();
            _writer.WriteString(s);
        }
    }

    /// <summary>
    /// Specifies the event type when an event is raised for an <see cref="XObject"/>.
    /// </summary>
    public enum XObjectChange
    {
        /// <summary>
        /// An <see cref="XObject"/> has been or will be added to an <see cref="XContainer"/>.
        /// </summary>
        Add,

        /// <summary>
        /// An <see cref="XObject"/> has been or will be removed from an <see cref="XContainer"/>.
        /// </summary>
        Remove,

        /// <summary>
        /// An <see cref="XObject"/> has been or will be renamed.
        /// </summary>
        Name,

        /// <summary>
        /// The value of an <see cref="XObject"/> has been or will be changed.
        /// There is a special case for elements. Change in the serialization
        /// of an empty element (either from an empty tag to start/end tag
        /// pair or vice versa) raises this event.
        /// </summary>
        Value,
    }

    /// <summary>
    /// Specifies a set of options for Load().
    /// </summary>
    [Flags]
    public enum LoadOptions
    {
        /// <summary>Default options.</summary>
        None = 0x00000000,

        /// <summary>Preserve whitespace.</summary>
        PreserveWhitespace = 0x00000001,

        /// <summary>Set the BaseUri property.</summary>
        SetBaseUri = 0x00000002,

        /// <summary>Set the IXmlLineInfo.</summary>
        SetLineInfo = 0x00000004,
    }

    /// <summary>
    /// Specifies a set of options for Save().
    /// </summary>
    [Flags]
    public enum SaveOptions
    {
        /// <summary>Default options.</summary>
        None = 0x00000000,

        /// <summary>Disable formatting.</summary>
        DisableFormatting = 0x00000001,

        /// <summary>Remove duplicate namespace declarations.</summary>
        OmitDuplicateNamespaces = 0x00000002,
    }

    /// <summary>
    /// Specifies a set of options for CreateReader().
    /// </summary>
    [Flags]
    public enum ReaderOptions
    {
        /// <summary>Default options.</summary>
        None = 0x00000000,

        /// <summary>Remove duplicate namespace declarations.</summary>
        OmitDuplicateNamespaces = 0x00000001,
    }
}
