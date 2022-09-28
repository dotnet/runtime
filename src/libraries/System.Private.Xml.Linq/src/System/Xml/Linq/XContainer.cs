// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Debug = System.Diagnostics.Debug;
using IEnumerable = System.Collections.IEnumerable;
using StringBuilder = System.Text.StringBuilder;
using Interlocked = System.Threading.Interlocked;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents a node that can contain other nodes.
    /// </summary>
    /// <remarks>
    /// The two classes that derive from <see cref="XContainer"/> are
    /// <see cref="XDocument"/> and <see cref="XElement"/>.
    /// </remarks>
    public abstract class XContainer : XNode
    {
        internal object? content;

        internal XContainer() { }

        internal XContainer(XContainer other)
        {
            ArgumentNullException.ThrowIfNull(other);

            if (other.content is string)
            {
                this.content = other.content;
            }
            else
            {
                XNode? n = (XNode?)other.content;
                if (n != null)
                {
                    do
                    {
                        n = n.next!;
                        AppendNodeSkipNotify(n.CloneNode());
                    } while (n != other.content);
                }
            }
        }

        /// <summary>
        /// Get the first child node of this node.
        /// </summary>
        public XNode? FirstNode
        {
            get
            {
                return LastNode?.next;
            }
        }

        /// <summary>
        /// Get the last child node of this node.
        /// </summary>
        public XNode? LastNode
        {
            get
            {
                if (content == null) return null;
                XNode? n = content as XNode;
                if (n != null) return n;
                string? s = content as string;
                if (s != null)
                {
                    if (s.Length == 0) return null;
                    XText t = new XText(s);
                    t.parent = this;
                    t.next = t;
                    Interlocked.CompareExchange<object>(ref content, t, s);
                }
                return (XNode)content;
            }
        }

        /// <overloads>
        /// Adds the specified content as a child (or as children) to this <see cref="XContainer"/>. The
        /// content can be simple content, a collection of content objects, a parameter list
        /// of content objects, or null.
        /// </overloads>
        /// <summary>
        /// Adds the specified content as a child (or children) of this <see cref="XContainer"/>.
        /// </summary>
        /// <param name="content">
        /// A content object containing simple content or a collection of content objects
        /// to be added.
        /// </param>
        /// <remarks>
        /// When adding simple content, a number of types may be passed to this method.
        /// Valid types include:
        /// <list>
        /// <item>string</item>
        /// <item>double</item>
        /// <item>float</item>
        /// <item>decimal</item>
        /// <item>bool</item>
        /// <item>DateTime</item>
        /// <item>DateTimeOffset</item>
        /// <item>TimeSpan</item>
        /// <item>Any type implementing ToString()</item>
        /// <item>Any type implementing IEnumerable</item>
        ///
        /// </list>
        /// When adding complex content, a number of types may be passed to this method.
        /// <list>
        /// <item>XObject</item>
        /// <item>XNode</item>
        /// <item>XAttribute</item>
        /// <item>Any type implementing IEnumerable</item>
        /// </list>
        ///
        /// If an object implements IEnumerable, then the collection in the object is enumerated,
        /// and all items in the collection are added. If the collection contains simple content,
        /// then the simple content in the collection is concatenated and added as a single
        /// string of simple content. If the collection contains complex content, then each item
        /// in the collection is added separately.
        ///
        /// If content is null, nothing is added. This allows the results of a query to be passed
        /// as content. If the query returns null, no contents are added, and this method does not
        /// throw a NullReferenceException.
        ///
        /// Attributes and simple content can't be added to a document.
        ///
        /// An added attribute must have a unique name within the element to
        /// which it is being added.
        /// </remarks>
        public void Add(object? content)
        {
            if (SkipNotify())
            {
                AddContentSkipNotify(content);
                return;
            }
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
            XAttribute? a = content as XAttribute;
            if (a != null)
            {
                AddAttribute(a);
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
                foreach (object? obj in o) Add(obj);
                return;
            }
            IEnumerable? e = content as IEnumerable;
            if (e != null)
            {
                foreach (object? obj in e) Add(obj);
                return;
            }
            AddString(GetStringValue(content));
        }

        /// <summary>
        /// Adds the specified content as a child (or children) of this <see cref="XContainer"/>.
        /// </summary>
        /// <param name="content">
        /// A parameter list of content objects.
        /// </param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void Add(params object?[] content)
        {
            Add((object)content);
        }

        /// <overloads>
        /// Adds the specified content as the first child (or children) of this document or element. The
        /// content can be simple content, a collection of content objects, a parameter
        /// list of content objects, or null.
        /// </overloads>
        /// <summary>
        /// Adds the specified content as the first child (or children) of this document or element.
        /// </summary>
        /// <param name="content">
        /// A content object containing simple content or a collection of content objects
        /// to be added.
        /// </param>
        /// <remarks>
        /// See <see cref="XContainer.Add(object)"/> for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void AddFirst(object? content)
        {
            new Inserter(this, null).Add(content);
        }

        /// <summary>
        /// Adds the specified content as the first children of this document or element.
        /// </summary>
        /// <param name="content">
        /// A parameter list of content objects.
        /// </param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the parent is null.
        /// </exception>
        public void AddFirst(params object?[] content)
        {
            AddFirst((object)content);
        }

        /// <summary>
        /// Creates an <see cref="XmlWriter"/> used to add either nodes
        /// or attributes to the <see cref="XContainer"/>. The later option
        /// applies only for <see cref="XElement"/>.
        /// </summary>
        /// <returns>An <see cref="XmlWriter"/></returns>
        public XmlWriter CreateWriter()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.ConformanceLevel = this is XDocument ? ConformanceLevel.Document : ConformanceLevel.Fragment;
            return XmlWriter.Create(new XNodeBuilder(this), settings);
        }

        /// <summary>
        /// Get descendant elements plus leaf nodes contained in an <see cref="XContainer"/>
        /// </summary>
        /// <returns><see cref="IEnumerable{XNode}"/> over all descendants</returns>
        public IEnumerable<XNode> DescendantNodes()
        {
            return GetDescendantNodes(false);
        }

        /// <summary>
        /// Returns the descendant <see cref="XElement"/>s of this <see cref="XContainer"/>.  Note this method will
        /// not return itself in the resulting IEnumerable.  See <see cref="XElement.DescendantsAndSelf()"/> if you
        /// need to include the current <see cref="XElement"/> in the results.
        /// <seealso cref="XElement.DescendantsAndSelf()"/>
        /// </summary>
        /// <returns>
        /// An IEnumerable of <see cref="XElement"/> with all of the descendants below this <see cref="XContainer"/> in the XML tree.
        /// </returns>
        public IEnumerable<XElement> Descendants()
        {
            return GetDescendants(null, false);
        }

        /// <summary>
        /// Returns the Descendant <see cref="XElement"/>s with the passed in <see cref="XName"/> as an IEnumerable
        /// of XElement.
        /// </summary>
        /// <param name="name">The <see cref="XName"/> to match against descendant <see cref="XElement"/>s.</param>
        /// <returns>An <see cref="IEnumerable"/> of <see cref="XElement"/></returns>
        public IEnumerable<XElement> Descendants(XName? name)
        {
            return name != null ? GetDescendants(name, false) : XElement.EmptySequence;
        }

        /// <summary>
        /// Returns the child element with this <see cref="XName"/> or null if there is no child element
        /// with a matching <see cref="XName"/>.
        /// <seealso cref="XContainer.Elements()"/>
        /// </summary>
        /// <param name="name">
        /// The <see cref="XName"/> to match against this <see cref="XContainer"/>s child elements.
        /// </param>
        /// <returns>
        /// An <see cref="XElement"/> child that matches the <see cref="XName"/> passed in, or null.
        /// </returns>
        public XElement? Element(XName name)
        {
            XNode? n = content as XNode;
            if (n != null)
            {
                do
                {
                    n = n.next!;
                    XElement? e = n as XElement;
                    if (e != null && e.name == name) return e;
                } while (n != content);
            }
            return null;
        }

        ///<overloads>
        /// Returns the child <see cref="XElement"/>s of this <see cref="XContainer"/>.
        /// </overloads>
        /// <summary>
        /// Returns all of the child elements of this <see cref="XContainer"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> over all of this <see cref="XContainer"/>'s child <see cref="XElement"/>s.
        /// </returns>
        public IEnumerable<XElement> Elements()
        {
            return GetElements(null);
        }

        /// <summary>
        /// Returns the child elements of this <see cref="XContainer"/> that match the <see cref="XName"/> passed in.
        /// </summary>
        /// <param name="name">
        /// The <see cref="XName"/> to match against the <see cref="XElement"/> children of this <see cref="XContainer"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> children of this <see cref="XContainer"/> that have
        /// a matching <see cref="XName"/>.
        /// </returns>
        public IEnumerable<XElement> Elements(XName? name)
        {
            return name != null ? GetElements(name) : XElement.EmptySequence;
        }

        ///<overloads>
        /// Returns the content of this <see cref="XContainer"/>.  Note that the content does not
        /// include <see cref="XAttribute"/>s.
        /// <seealso cref="XElement.Attributes()"/>
        /// </overloads>
        /// <summary>
        /// Returns the content of this <see cref="XContainer"/> as an <see cref="IEnumerable"/> of <see cref="object"/>.  Note
        /// that the content does not include <see cref="XAttribute"/>s.
        /// <seealso cref="XElement.Attributes()"/>
        /// </summary>
        /// <returns>The contents of this <see cref="XContainer"/></returns>
        public IEnumerable<XNode> Nodes()
        {
            XNode? n = LastNode;
            if (n != null)
            {
                do
                {
                    n = n.next!;
                    yield return n;
                } while (n.parent == this && n != content);
            }
        }

        /// <summary>
        /// Removes the nodes from this <see cref="XContainer"/>.  Note this
        /// methods does not remove attributes.  See <see cref="XElement.RemoveAttributes()"/>.
        /// <seealso cref="XElement.RemoveAttributes()"/>
        /// </summary>
        public void RemoveNodes()
        {
            if (SkipNotify())
            {
                RemoveNodesSkipNotify();
                return;
            }
            while (content != null)
            {
                string? s = content as string;
                if (s != null)
                {
                    if (s.Length > 0)
                    {
                        ConvertTextToNode();
                    }
                    else
                    {
                        if (this is XElement)
                        {
                            // Change in the serialization of an empty element:
                            // from start/end tag pair to empty tag
                            NotifyChanging(this, XObjectChangeEventArgs.Value);
                            if ((object)s != (object)content) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
                            content = null;
                            NotifyChanged(this, XObjectChangeEventArgs.Value);
                        }
                        else
                        {
                            content = null;
                        }
                    }
                }
                XNode? last = content as XNode;
                if (last != null)
                {
                    XNode n = last.next!;
                    NotifyChanging(n, XObjectChangeEventArgs.Remove);
                    if (last != content || n != last.next) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
                    if (n != last)
                    {
                        last.next = n.next;
                    }
                    else
                    {
                        content = null;
                    }
                    n.parent = null;
                    n.next = null;
                    NotifyChanged(n, XObjectChangeEventArgs.Remove);
                }
            }
        }

        /// <overloads>
        /// Replaces the children nodes of this document or element with the specified content. The
        /// content can be simple content, a collection of content objects, a parameter
        /// list of content objects, or null.
        /// </overloads>
        /// <summary>
        /// Replaces the children nodes of this document or element with the specified content.
        /// </summary>
        /// <param name="content">
        /// A content object containing simple content or a collection of content objects
        /// that replace the children nodes.
        /// </param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void ReplaceNodes(object? content)
        {
            content = GetContentSnapshot(content);
            RemoveNodes();
            Add(content);
        }

        /// <summary>
        /// Replaces the children nodes of this document or element with the specified content.
        /// </summary>
        /// <param name="content">
        /// A parameter list of content objects.
        /// </param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void ReplaceNodes(params object?[] content)
        {
            ReplaceNodes((object)content);
        }

        internal virtual void AddAttribute(XAttribute a)
        {
        }

        internal virtual void AddAttributeSkipNotify(XAttribute a)
        {
        }

        internal void AddContentSkipNotify(object? content)
        {
            if (content == null) return;
            XNode? n = content as XNode;
            if (n != null)
            {
                AddNodeSkipNotify(n);
                return;
            }
            string? s = content as string;
            if (s != null)
            {
                AddStringSkipNotify(s);
                return;
            }
            XAttribute? a = content as XAttribute;
            if (a != null)
            {
                AddAttributeSkipNotify(a);
                return;
            }
            XStreamingElement? x = content as XStreamingElement;
            if (x != null)
            {
                AddNodeSkipNotify(new XElement(x));
                return;
            }
            object?[]? o = content as object?[];
            if (o != null)
            {
                foreach (object? obj in o) AddContentSkipNotify(obj);
                return;
            }
            IEnumerable? e = content as IEnumerable;
            if (e != null)
            {
                foreach (object? obj in e) AddContentSkipNotify(obj);
                return;
            }
            AddStringSkipNotify(GetStringValue(content));
        }

        internal void AddNode(XNode n)
        {
            ValidateNode(n, this);
            if (n.parent != null)
            {
                n = n.CloneNode();
            }
            else
            {
                XNode p = this;
                while (p.parent != null) p = p.parent;
                if (n == p) n = n.CloneNode();
            }
            ConvertTextToNode();
            AppendNode(n);
        }

        internal void AddNodeSkipNotify(XNode n)
        {
            ValidateNode(n, this);
            if (n.parent != null)
            {
                n = n.CloneNode();
            }
            else
            {
                XNode p = this;
                while (p.parent != null) p = p.parent;
                if (n == p) n = n.CloneNode();
            }
            ConvertTextToNode();
            AppendNodeSkipNotify(n);
        }

        internal void AddString(string s)
        {
            ValidateString(s);
            if (content == null)
            {
                if (s.Length > 0)
                {
                    AppendNode(new XText(s));
                }
                else
                {
                    if (this is XElement)
                    {
                        // Change in the serialization of an empty element:
                        // from empty tag to start/end tag pair
                        NotifyChanging(this, XObjectChangeEventArgs.Value);
                        if (content != null) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
                        content = s;
                        NotifyChanged(this, XObjectChangeEventArgs.Value);
                    }
                    else
                    {
                        content = s;
                    }
                }
            }
            else if (s.Length > 0)
            {
                ConvertTextToNode();
                XText? tn = content as XText;
                if (tn != null && !(tn is XCData))
                {
                    tn.Value += s;
                }
                else
                {
                    AppendNode(new XText(s));
                }
            }
        }

        internal void AddStringSkipNotify(string s)
        {
            ValidateString(s);
            if (content == null)
            {
                content = s;
            }
            else if (s.Length > 0)
            {
                string? stringContent = content as string;
                if (stringContent != null)
                {
                    content = stringContent + s;
                }
                else
                {
                    XText? tn = content as XText;
                    if (tn != null && !(tn is XCData))
                    {
                        tn.text += s;
                    }
                    else
                    {
                        AppendNodeSkipNotify(new XText(s));
                    }
                }
            }
        }

        internal void AppendNode(XNode n)
        {
            bool notify = NotifyChanging(n, XObjectChangeEventArgs.Add);
            if (n.parent != null) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
            AppendNodeSkipNotify(n);
            if (notify) NotifyChanged(n, XObjectChangeEventArgs.Add);
        }

        internal void AppendNodeSkipNotify(XNode n)
        {
            n.parent = this;
            if (content == null || content is string)
            {
                n.next = n;
            }
            else
            {
                XNode x = (XNode)content;
                n.next = x.next;
                x.next = n;
            }
            content = n;
        }

        internal override void AppendText(StringBuilder sb)
        {
            string? s = content as string;
            if (s != null)
            {
                sb.Append(s);
            }
            else
            {
                XNode? n = (XNode?)content;
                if (n != null)
                {
                    do
                    {
                        n = n.next!;
                        n.AppendText(sb);
                    } while (n != content);
                }
            }
        }

        private string? GetTextOnly()
        {
            if (content == null) return null;
            string? s = content as string;
            if (s == null)
            {
                XNode n = (XNode)content;
                do
                {
                    n = n.next!;
                    if (n.NodeType != XmlNodeType.Text) return null;
                    s += ((XText)n).Value;
                } while (n != content);
            }
            return s;
        }

        private string CollectText(ref XNode? n)
        {
            string s = "";
            while (n != null && n.NodeType == XmlNodeType.Text)
            {
                s += ((XText)n).Value;
                n = n != content ? n.next : null;
            }
            return s;
        }

        internal bool ContentsEqual(XContainer e)
        {
            if (content == e.content) return true;
            string? s = GetTextOnly();
            if (s != null) return s == e.GetTextOnly();
            XNode? n1 = content as XNode;
            XNode? n2 = e.content as XNode;
            if (n1 != null && n2 != null)
            {
                n1 = n1.next;
                n2 = n2.next;
                while (true)
                {
                    if (CollectText(ref n1) != e.CollectText(ref n2)) break;
                    if (n1 == null && n2 == null) return true;
                    if (n1 == null || n2 == null || !n1.DeepEquals(n2)) break;
                    n1 = n1 != content ? n1.next : null;
                    n2 = n2 != e.content ? n2.next : null;
                }
            }
            return false;
        }

        internal int ContentsHashCode()
        {
            string? s = GetTextOnly();
            if (s != null) return s.GetHashCode();
            int h = 0;
            XNode? n = content as XNode;
            if (n != null)
            {
                do
                {
                    n = n.next;
                    string text = CollectText(ref n);
                    if (text.Length > 0)
                    {
                        h ^= text.GetHashCode();
                    }
                    if (n == null) break;
                    h ^= n.GetDeepHashCode();
                } while (n != content);
            }
            return h;
        }

        internal void ConvertTextToNode()
        {
            string? s = content as string;
            if (!string.IsNullOrEmpty(s))
            {
                XText t = new XText(s);
                t.parent = this;
                t.next = t;
                content = t;
            }
        }

        internal IEnumerable<XNode> GetDescendantNodes(bool self)
        {
            if (self) yield return this;
            XNode n = this;
            while (true)
            {
                XContainer? c = n as XContainer;
                XNode? first;
                if (c != null && (first = c.FirstNode) != null)
                {
                    n = first;
                }
                else
                {
                    while (n != null && n != this && n == n.parent!.content) n = n.parent;
                    if (n == null || n == this) break;
                    n = n.next!;
                }
                yield return n;
            }
        }

        internal IEnumerable<XElement> GetDescendants(XName? name, bool self)
        {
            if (self)
            {
                XElement e = (XElement)this;
                if (name == null || e.name == name) yield return e;
            }
            XNode n = this;
            XContainer? c = this;
            while (true)
            {
                if (c != null && c.content is XNode)
                {
                    n = ((XNode)c.content).next!;
                }
                else
                {
                    while (n != this && n == n.parent!.content) n = n.parent;
                    if (n == this) break;
                    n = n.next!;
                }
                XElement? e = n as XElement;
                if (e != null && (name == null || e.name == name)) yield return e;
                c = e;
            }
        }

        private IEnumerable<XElement> GetElements(XName? name)
        {
            XNode? n = content as XNode;
            if (n != null)
            {
                do
                {
                    n = n.next!;
                    XElement? e = n as XElement;
                    if (e != null && (name == null || e.name == name)) yield return e;
                } while (n.parent == this && n != content);
            }
        }

        internal static string GetStringValue(object value)
        {
            string? s = value switch
            {
                string stringValue => stringValue,
                int intValue => XmlConvert.ToString(intValue),
                double doubleValue => XmlConvert.ToString(doubleValue),
                long longValue => XmlConvert.ToString(longValue),
                float floatValue => XmlConvert.ToString(floatValue),
                decimal decimalValue => XmlConvert.ToString(decimalValue),
                short shortValue => XmlConvert.ToString(shortValue),
                sbyte sbyteValue => XmlConvert.ToString(sbyteValue),
                bool boolValue => XmlConvert.ToString(boolValue),
                DateTime dtValue => XmlConvert.ToString(dtValue, XmlDateTimeSerializationMode.RoundtripKind),
                DateTimeOffset dtoValue => XmlConvert.ToString(dtoValue),
                TimeSpan tsValue => XmlConvert.ToString(tsValue),
                XObject => throw new ArgumentException(SR.Argument_XObjectValue),
                _ => value.ToString()
            };

            if (s == null) throw new ArgumentException(SR.Argument_ConvertToString);
            return s;
        }

        internal void ReadContentFrom(XmlReader r)
        {
            if (r.ReadState != ReadState.Interactive) throw new InvalidOperationException(SR.InvalidOperation_ExpectedInteractive);

            ContentReader cr = new ContentReader(this);
            while (cr.ReadContentFrom(this, r) && r.Read()) ;
        }

        internal void ReadContentFrom(XmlReader r, LoadOptions o)
        {
            if ((o & (LoadOptions.SetBaseUri | LoadOptions.SetLineInfo)) == 0)
            {
                ReadContentFrom(r);
                return;
            }
            if (r.ReadState != ReadState.Interactive) throw new InvalidOperationException(SR.InvalidOperation_ExpectedInteractive);

            ContentReader cr = new ContentReader(this, r, o);
            while (cr.ReadContentFrom(this, r, o) && r.Read()) ;
        }

        internal async Task ReadContentFromAsync(XmlReader r, CancellationToken cancellationToken)
        {
            if (r.ReadState != ReadState.Interactive) throw new InvalidOperationException(SR.InvalidOperation_ExpectedInteractive);

            ContentReader cr = new ContentReader(this);
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            while (await cr.ReadContentFromAsync(this, r).ConfigureAwait(false) && await r.ReadAsync().ConfigureAwait(false));
        }

        internal async Task ReadContentFromAsync(XmlReader r, LoadOptions o, CancellationToken cancellationToken)
        {
            if ((o & (LoadOptions.SetBaseUri | LoadOptions.SetLineInfo)) == 0)
            {
                await ReadContentFromAsync(r, cancellationToken).ConfigureAwait(false);
                return;
            }
            if (r.ReadState != ReadState.Interactive) throw new InvalidOperationException(SR.InvalidOperation_ExpectedInteractive);

            ContentReader cr = new ContentReader(this, r, o);
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            while (await cr.ReadContentFromAsync(this, r, o).ConfigureAwait(false) && await r.ReadAsync().ConfigureAwait(false));
        }

        private sealed class ContentReader
        {
            private readonly NamespaceCache _eCache;
            private readonly NamespaceCache _aCache;
            private readonly IXmlLineInfo? _lineInfo;
            private XContainer _currentContainer;
            private string? _baseUri;

            public ContentReader(XContainer rootContainer)
            {
                _currentContainer = rootContainer;
            }

            public ContentReader(XContainer rootContainer, XmlReader r, LoadOptions o)
            {
                _currentContainer = rootContainer;
                _baseUri = (o & LoadOptions.SetBaseUri) != 0 ? r.BaseURI : null;
                _lineInfo = (o & LoadOptions.SetLineInfo) != 0 ? r as IXmlLineInfo : null;
            }

            public bool ReadContentFrom(XContainer rootContainer, XmlReader r)
            {
                switch (r.NodeType)
                {
                    case XmlNodeType.Element:
                        XElement e = new XElement(_eCache.Get(r.NamespaceURI).GetName(r.LocalName));
                        if (r.MoveToFirstAttribute())
                        {
                            do
                            {
                                e.AppendAttributeSkipNotify(new XAttribute(_aCache.Get(r.Prefix.Length == 0 ? string.Empty : r.NamespaceURI).GetName(r.LocalName), r.Value));
                            } while (r.MoveToNextAttribute());
                            r.MoveToElement();
                        }
                        _currentContainer.AddNodeSkipNotify(e);
                        if (!r.IsEmptyElement)
                        {
                            _currentContainer = e;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        _currentContainer.content ??= string.Empty;
                        if (_currentContainer == rootContainer) return false;
                        _currentContainer = _currentContainer.parent!;
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        _currentContainer.AddStringSkipNotify(r.Value);
                        break;
                    case XmlNodeType.CDATA:
                        _currentContainer.AddNodeSkipNotify(new XCData(r.Value));
                        break;
                    case XmlNodeType.Comment:
                        _currentContainer.AddNodeSkipNotify(new XComment(r.Value));
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        _currentContainer.AddNodeSkipNotify(new XProcessingInstruction(r.Name, r.Value));
                        break;
                    case XmlNodeType.DocumentType:
                        _currentContainer.AddNodeSkipNotify(new XDocumentType(r.LocalName, r.GetAttribute("PUBLIC"), r.GetAttribute("SYSTEM"), r.Value));
                        break;
                    case XmlNodeType.EntityReference:
                        if (!r.CanResolveEntity) throw new InvalidOperationException(SR.InvalidOperation_UnresolvedEntityReference);
                        r.ResolveEntity();
                        break;
                    case XmlNodeType.EndEntity:
                        break;
                    default:
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedNodeType, r.NodeType));
                }
                return true;
            }

            public async ValueTask<bool> ReadContentFromAsync(XContainer rootContainer, XmlReader r)
            {
                switch (r.NodeType)
                {
                    case XmlNodeType.Element:
                        XElement e = new XElement(_eCache.Get(r.NamespaceURI).GetName(r.LocalName));
                        if (r.MoveToFirstAttribute())
                        {
                            do
                            {
                                e.AppendAttributeSkipNotify(new XAttribute(
                                    _aCache.Get(r.Prefix.Length == 0 ? string.Empty : r.NamespaceURI).GetName(r.LocalName),
                                    await r.GetValueAsync().ConfigureAwait(false)));
                            } while (r.MoveToNextAttribute());
                            r.MoveToElement();
                        }
                        _currentContainer.AddNodeSkipNotify(e);
                        if (!r.IsEmptyElement)
                        {
                            _currentContainer = e;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        _currentContainer.content ??= string.Empty;
                        if (_currentContainer == rootContainer) return false;
                        _currentContainer = _currentContainer.parent!;
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        _currentContainer.AddStringSkipNotify(await r.GetValueAsync().ConfigureAwait(false));
                        break;
                    case XmlNodeType.CDATA:
                        _currentContainer.AddNodeSkipNotify(new XCData(await r.GetValueAsync().ConfigureAwait(false)));
                        break;
                    case XmlNodeType.Comment:
                        _currentContainer.AddNodeSkipNotify(new XComment(await r.GetValueAsync().ConfigureAwait(false)));
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        _currentContainer.AddNodeSkipNotify(new XProcessingInstruction(r.Name, await r.GetValueAsync().ConfigureAwait(false)));
                        break;
                    case XmlNodeType.DocumentType:
                        _currentContainer.AddNodeSkipNotify(new XDocumentType(r.LocalName, r.GetAttribute("PUBLIC"), r.GetAttribute("SYSTEM"), await r.GetValueAsync().ConfigureAwait(false)));
                        break;
                    case XmlNodeType.EntityReference:
                        if (!r.CanResolveEntity) throw new InvalidOperationException(SR.InvalidOperation_UnresolvedEntityReference);
                        r.ResolveEntity();
                        break;
                    case XmlNodeType.EndEntity:
                        break;
                    default:
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedNodeType, r.NodeType));
                }
                return true;
            }

            public bool ReadContentFrom(XContainer rootContainer, XmlReader r, LoadOptions o)
            {
                XNode? newNode = null;
                string baseUri = r.BaseURI;

                switch (r.NodeType)
                {
                    case XmlNodeType.Element:
                    {
                        XElement e = new XElement(_eCache.Get(r.NamespaceURI).GetName(r.LocalName));
                        if (_baseUri != null && _baseUri != baseUri)
                        {
                            e.SetBaseUri(baseUri);
                        }
                        if (_lineInfo != null && _lineInfo.HasLineInfo())
                        {
                            e.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
                        }
                        if (r.MoveToFirstAttribute())
                        {
                            do
                            {
                                XAttribute a = new XAttribute(_aCache.Get(r.Prefix.Length == 0 ? string.Empty : r.NamespaceURI).GetName(r.LocalName), r.Value);
                                if (_lineInfo != null && _lineInfo.HasLineInfo())
                                {
                                    a.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
                                }
                                e.AppendAttributeSkipNotify(a);
                            } while (r.MoveToNextAttribute());
                            r.MoveToElement();
                        }
                        _currentContainer.AddNodeSkipNotify(e);
                        if (!r.IsEmptyElement)
                        {
                            _currentContainer = e;
                            if (_baseUri != null)
                            {
                                _baseUri = baseUri;
                            }
                        }
                        break;
                    }
                    case XmlNodeType.EndElement:
                    {
                        _currentContainer.content ??= string.Empty;
                        // Store the line info of the end element tag.
                        // Note that since we've got EndElement the current container must be an XElement
                        XElement? e = _currentContainer as XElement;
                        Debug.Assert(e != null, "EndElement received but the current container is not an element.");
                        if (e != null && _lineInfo != null && _lineInfo.HasLineInfo())
                        {
                                e.SetEndElementLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
                        }
                        if (_currentContainer == rootContainer) return false;
                        if (_baseUri != null && _currentContainer.HasBaseUri)
                        {
                                _baseUri = _currentContainer.parent!.BaseUri;
                        }
                        _currentContainer = _currentContainer.parent!;
                        break;
                    }
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        if ((_baseUri != null && _baseUri != baseUri) ||
                            (_lineInfo != null && _lineInfo.HasLineInfo()))
                        {
                            newNode = new XText(r.Value);
                        }
                        else
                        {
                            _currentContainer.AddStringSkipNotify(r.Value);
                        }
                        break;
                    case XmlNodeType.CDATA:
                        newNode = new XCData(r.Value);
                        break;
                    case XmlNodeType.Comment:
                        newNode = new XComment(r.Value);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        newNode = new XProcessingInstruction(r.Name, r.Value);
                        break;
                    case XmlNodeType.DocumentType:
                        newNode = new XDocumentType(r.LocalName, r.GetAttribute("PUBLIC"), r.GetAttribute("SYSTEM"), r.Value);
                        break;
                    case XmlNodeType.EntityReference:
                        if (!r.CanResolveEntity) throw new InvalidOperationException(SR.InvalidOperation_UnresolvedEntityReference);
                        r.ResolveEntity();
                        break;
                    case XmlNodeType.EndEntity:
                        break;
                    default:
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedNodeType, r.NodeType));
                }

                if (newNode != null)
                {
                    if (_baseUri != null && _baseUri != baseUri)
                    {
                        newNode.SetBaseUri(baseUri);
                    }

                    if (_lineInfo != null && _lineInfo.HasLineInfo())
                    {
                        newNode.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
                    }

                    _currentContainer.AddNodeSkipNotify(newNode);
                }

                return true;
            }

            public async ValueTask<bool> ReadContentFromAsync(XContainer rootContainer, XmlReader r, LoadOptions o)
            {
                XNode? newNode = null;
                string baseUri = r.BaseURI!;

                switch (r.NodeType)
                {
                    case XmlNodeType.Element:
                        {
                            XElement e = new XElement(_eCache.Get(r.NamespaceURI).GetName(r.LocalName));
                            if (_baseUri != null && _baseUri != baseUri)
                            {
                                e.SetBaseUri(baseUri);
                            }
                            if (_lineInfo != null && _lineInfo.HasLineInfo())
                            {
                                e.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
                            }
                            if (r.MoveToFirstAttribute())
                            {
                                do
                                {
                                    XAttribute a = new XAttribute(
                                        _aCache.Get(r.Prefix.Length == 0 ? string.Empty : r.NamespaceURI).GetName(r.LocalName),
                                        await r.GetValueAsync().ConfigureAwait(false));
                                    if (_lineInfo != null && _lineInfo.HasLineInfo())
                                    {
                                        a.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
                                    }
                                    e.AppendAttributeSkipNotify(a);
                                } while (r.MoveToNextAttribute());
                                r.MoveToElement();
                            }
                            _currentContainer.AddNodeSkipNotify(e);
                            if (!r.IsEmptyElement)
                            {
                                _currentContainer = e;
                                if (_baseUri != null)
                                {
                                    _baseUri = baseUri;
                                }
                            }
                            break;
                        }
                    case XmlNodeType.EndElement:
                        {
                            _currentContainer.content ??= string.Empty;
                            // Store the line info of the end element tag.
                            // Note that since we've got EndElement the current container must be an XElement
                            XElement? e = _currentContainer as XElement;
                            Debug.Assert(e != null, "EndElement received but the current container is not an element.");
                            if (e != null && _lineInfo != null && _lineInfo.HasLineInfo())
                            {
                                e.SetEndElementLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
                            }
                            if (_currentContainer == rootContainer) return false;
                            if (_baseUri != null && _currentContainer.HasBaseUri)
                            {
                                _baseUri = _currentContainer.parent!.BaseUri;
                            }
                            _currentContainer = _currentContainer.parent!;
                            break;
                        }
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        if ((_baseUri != null && _baseUri != baseUri) ||
                            (_lineInfo != null && _lineInfo.HasLineInfo()))
                        {
                            newNode = new XText(await r.GetValueAsync().ConfigureAwait(false));
                        }
                        else
                        {
                            _currentContainer.AddStringSkipNotify(await r.GetValueAsync().ConfigureAwait(false));
                        }
                        break;
                    case XmlNodeType.CDATA:
                        newNode = new XCData(await r.GetValueAsync().ConfigureAwait(false));
                        break;
                    case XmlNodeType.Comment:
                        newNode = new XComment(await r.GetValueAsync().ConfigureAwait(false));
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        newNode = new XProcessingInstruction(r.Name, await r.GetValueAsync().ConfigureAwait(false));
                        break;
                    case XmlNodeType.DocumentType:
                        newNode = new XDocumentType(r.LocalName, r.GetAttribute("PUBLIC"), r.GetAttribute("SYSTEM"), await r.GetValueAsync().ConfigureAwait(false));
                        break;
                    case XmlNodeType.EntityReference:
                        if (!r.CanResolveEntity) throw new InvalidOperationException(SR.InvalidOperation_UnresolvedEntityReference);
                        r.ResolveEntity();
                        break;
                    case XmlNodeType.EndEntity:
                        break;
                    default:
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedNodeType, r.NodeType));
                }

                if (newNode != null)
                {
                    if (_baseUri != null && _baseUri != baseUri)
                    {
                        newNode.SetBaseUri(baseUri);
                    }

                    if (_lineInfo != null && _lineInfo.HasLineInfo())
                    {
                        newNode.SetLineInfo(_lineInfo.LineNumber, _lineInfo.LinePosition);
                    }

                    _currentContainer.AddNodeSkipNotify(newNode);
                }

                return true;
            }
        }

        internal void RemoveNode(XNode n)
        {
            bool notify = NotifyChanging(n, XObjectChangeEventArgs.Remove);
            if (n.parent != this) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);

            Debug.Assert(content != null);
            XNode p = (XNode)content;
            while (p.next != n) p = p.next!;
            if (p == n)
            {
                content = null;
            }
            else
            {
                if (content == n) content = p;
                p.next = n.next;
            }
            n.parent = null;
            n.next = null;
            if (notify) NotifyChanged(n, XObjectChangeEventArgs.Remove);
        }

        private void RemoveNodesSkipNotify()
        {
            XNode? n = content as XNode;
            if (n != null)
            {
                do
                {
                    XNode next = n.next!;
                    n.parent = null;
                    n.next = null;
                    n = next;
                } while (n != content);
            }
            content = null;
        }

        // Validate insertion of the given node. previous is the node after which insertion
        // will occur. previous == null means at beginning, previous == this means at end.
        internal virtual void ValidateNode(XNode node, XNode? previous)
        {
        }

        internal virtual void ValidateString(string s)
        {
        }

        internal void WriteContentTo(XmlWriter writer)
        {
            if (content != null)
            {
                string? stringContent = content as string;
                if (stringContent != null)
                {
                    if (this is XDocument)
                    {
                        writer.WriteWhitespace(stringContent);
                    }
                    else
                    {
                        writer.WriteString(stringContent);
                    }
                }
                else
                {
                    XNode n = (XNode)content;
                    do
                    {
                        n = n.next!;
                        n.WriteTo(writer);
                    } while (n != content);
                }
            }
        }

        internal async Task WriteContentToAsync(XmlWriter writer, CancellationToken cancellationToken)
        {
            if (content != null)
            {
                string? stringContent = content as string;

                if (stringContent != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Task tWrite;

                    if (this is XDocument)
                    {
                        tWrite = writer.WriteWhitespaceAsync(stringContent);
                    }
                    else
                    {
                        tWrite = writer.WriteStringAsync(stringContent);
                    }

                    await tWrite.ConfigureAwait(false);
                }
                else
                {
                    XNode n = (XNode)content;
                    do
                    {
                        n = n.next!;
                        await n.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
                    } while (n != content);
                }
            }
        }

        private static void AddContentToList(List<object?> list, object? content)
        {
            IEnumerable? e = content is string ? null : content as IEnumerable;
            if (e == null)
            {
                list.Add(content);
            }
            else
            {
                foreach (object? obj in e)
                {
                    if (obj != null) AddContentToList(list, obj);
                }
            }
        }

        [return: NotNullIfNotNull(nameof(content))]
        internal static object? GetContentSnapshot(object? content)
        {
            if (content is string || !(content is IEnumerable)) return content;
            List<object?> list = new List<object?>();
            AddContentToList(list, content);
            return list;
        }
    }
}
