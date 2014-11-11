// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CultureInfo = System.Globalization.CultureInfo;
using Debug = System.Diagnostics.Debug;
using IEnumerable = System.Collections.IEnumerable;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using Enumerable = System.Linq.Enumerable;
using IComparer = System.Collections.IComparer;
using IEqualityComparer = System.Collections.IEqualityComparer;
using StringBuilder = System.Text.StringBuilder;
using Encoding = System.Text.Encoding;
using Interlocked = System.Threading.Interlocked;
using System.Reflection;

namespace System.Xml.Linq
{
    internal struct Inserter
    {
        XContainer parent;
        XNode previous;
        string text;

        public Inserter(XContainer parent, XNode anchor)
        {
            this.parent = parent;
            this.previous = anchor;
            this.text = null;
        }

        public void Add(object content)
        {
            AddContent(content);
            if (text != null)
            {
                if (parent.content == null)
                {
                    if (parent.SkipNotify())
                    {
                        parent.content = text;
                    }
                    else
                    {
                        if (text.Length > 0)
                        {
                            InsertNode(new XText(text));
                        }
                        else
                        {
                            if (parent is XElement)
                            {
                                // Change in the serialization of an empty element: 
                                // from empty tag to start/end tag pair
                                parent.NotifyChanging(parent, XObjectChangeEventArgs.Value);
                                if (parent.content != null) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
                                parent.content = text;
                                parent.NotifyChanged(parent, XObjectChangeEventArgs.Value);
                            }
                            else
                            {
                                parent.content = text;
                            }
                        }
                    }
                }
                else if (text.Length > 0)
                {
                    if (previous is XText && !(previous is XCData))
                    {
                        ((XText)previous).Value += text;
                    }
                    else
                    {
                        parent.ConvertTextToNode();
                        InsertNode(new XText(text));
                    }
                }
            }
        }

        void AddContent(object content)
        {
            if (content == null) return;
            XNode n = content as XNode;
            if (n != null)
            {
                AddNode(n);
                return;
            }
            string s = content as string;
            if (s != null)
            {
                AddString(s);
                return;
            }
            XStreamingElement x = content as XStreamingElement;
            if (x != null)
            {
                AddNode(new XElement(x));
                return;
            }
            object[] o = content as object[];
            if (o != null)
            {
                foreach (object obj in o) AddContent(obj);
                return;
            }
            IEnumerable e = content as IEnumerable;
            if (e != null)
            {
                foreach (object obj in e) AddContent(obj);
                return;
            }
            if (content is XAttribute) throw new ArgumentException(SR.Argument_AddAttribute);
            AddString(XContainer.GetStringValue(content));
        }

        void AddNode(XNode n)
        {
            parent.ValidateNode(n, previous);
            if (n.parent != null)
            {
                n = n.CloneNode();
            }
            else
            {
                XNode p = parent;
                while (p.parent != null) p = p.parent;
                if (n == p) n = n.CloneNode();
            }
            parent.ConvertTextToNode();
            if (text != null)
            {
                if (text.Length > 0)
                {
                    if (previous is XText && !(previous is XCData))
                    {
                        ((XText)previous).Value += text;
                    }
                    else
                    {
                        InsertNode(new XText(text));
                    }
                }
                text = null;
            }
            InsertNode(n);
        }

        void AddString(string s)
        {
            parent.ValidateString(s);
            text += s;
        }

        // Prepends if previous == null, otherwise inserts after previous
        void InsertNode(XNode n)
        {
            bool notify = parent.NotifyChanging(n, XObjectChangeEventArgs.Add);
            if (n.parent != null) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
            n.parent = parent;
            if (parent.content == null || parent.content is string)
            {
                n.next = n;
                parent.content = n;
            }
            else if (previous == null)
            {
                XNode last = (XNode)parent.content;
                n.next = last.next;
                last.next = n;
            }
            else
            {
                n.next = previous.next;
                previous.next = n;
                if (parent.content == previous) parent.content = n;
            }
            previous = n;
            if (notify) parent.NotifyChanged(n, XObjectChangeEventArgs.Add);
        }
    }

    internal struct NamespaceCache
    {
        XNamespace ns;
        string namespaceName;

        public XNamespace Get(string namespaceName)
        {
            if ((object)namespaceName == (object)this.namespaceName) return this.ns;
            this.namespaceName = namespaceName;
            this.ns = XNamespace.Get(namespaceName);
            return this.ns;
        }
    }

    internal struct ElementWriter
    {
        XmlWriter writer;
        NamespaceResolver resolver;

        public ElementWriter(XmlWriter writer)
        {
            this.writer = writer;
            this.resolver = new NamespaceResolver();
        }

        public void WriteElement(XElement e)
        {
            PushAncestors(e);
            XElement root = e;
            XNode n = e;
            while (true)
            {
                e = n as XElement;
                if (e != null)
                {
                    WriteStartElement(e);
                    if (e.content == null)
                    {
                        WriteEndElement();
                    }
                    else
                    {
                        string s = e.content as string;
                        if (s != null)
                        {
                            writer.WriteString(s);
                            WriteFullEndElement();
                        }
                        else
                        {
                            n = ((XNode)e.content).next;
                            continue;
                        }
                    }
                }
                else
                {
                    n.WriteTo(writer);
                }
                while (n != root && n == n.parent.content)
                {
                    n = n.parent;
                    WriteFullEndElement();
                }
                if (n == root) break;
                n = n.next;
            }
        }

        string GetPrefixOfNamespace(XNamespace ns, bool allowDefaultNamespace)
        {
            string namespaceName = ns.NamespaceName;
            if (namespaceName.Length == 0) return string.Empty;
            string prefix = resolver.GetPrefixOfNamespace(ns, allowDefaultNamespace);
            if (prefix != null) return prefix;
            if ((object)namespaceName == (object)XNamespace.xmlPrefixNamespace) return "xml";
            if ((object)namespaceName == (object)XNamespace.xmlnsPrefixNamespace) return "xmlns";
            return null;
        }

        void PushAncestors(XElement e)
        {
            while (true)
            {
                e = e.parent as XElement;
                if (e == null) break;
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next;
                        if (a.IsNamespaceDeclaration)
                        {
                            resolver.AddFirst(a.Name.NamespaceName.Length == 0 ? string.Empty : a.Name.LocalName, XNamespace.Get(a.Value));
                        }
                    } while (a != e.lastAttr);
                }
            }
        }

        void PushElement(XElement e)
        {
            resolver.PushScope();
            XAttribute a = e.lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next;
                    if (a.IsNamespaceDeclaration)
                    {
                        resolver.Add(a.Name.NamespaceName.Length == 0 ? string.Empty : a.Name.LocalName, XNamespace.Get(a.Value));
                    }
                } while (a != e.lastAttr);
            }
        }

        void WriteEndElement()
        {
            writer.WriteEndElement();
            resolver.PopScope();
        }

        void WriteFullEndElement()
        {
            writer.WriteFullEndElement();
            resolver.PopScope();
        }

        void WriteStartElement(XElement e)
        {
            PushElement(e);
            XNamespace ns = e.Name.Namespace;
            writer.WriteStartElement(GetPrefixOfNamespace(ns, true), e.Name.LocalName, ns.NamespaceName);
            XAttribute a = e.lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next;
                    ns = a.Name.Namespace;
                    string localName = a.Name.LocalName;
                    string namespaceName = ns.NamespaceName;
                    writer.WriteAttributeString(GetPrefixOfNamespace(ns, false), localName, namespaceName.Length == 0 && localName == "xmlns" ? XNamespace.xmlnsPrefixNamespace : namespaceName, a.Value);
                } while (a != e.lastAttr);
            }
        }
    }

    internal struct NamespaceResolver
    {
        class NamespaceDeclaration
        {
            public string prefix;
            public XNamespace ns;
            public int scope;
            public NamespaceDeclaration prev;
        }

        int scope;
        NamespaceDeclaration declaration;
        NamespaceDeclaration rover;

        public void PushScope()
        {
            scope++;
        }

        public void PopScope()
        {
            NamespaceDeclaration d = declaration;
            if (d != null)
            {
                do
                {
                    d = d.prev;
                    if (d.scope != scope) break;
                    if (d == declaration)
                    {
                        declaration = null;
                    }
                    else
                    {
                        declaration.prev = d.prev;
                    }
                    rover = null;
                } while (d != declaration && declaration != null);
            }
            scope--;
        }

        public void Add(string prefix, XNamespace ns)
        {
            NamespaceDeclaration d = new NamespaceDeclaration();
            d.prefix = prefix;
            d.ns = ns;
            d.scope = scope;
            if (declaration == null)
            {
                declaration = d;
            }
            else
            {
                d.prev = declaration.prev;
            }
            declaration.prev = d;
            rover = null;
        }

        public void AddFirst(string prefix, XNamespace ns)
        {
            NamespaceDeclaration d = new NamespaceDeclaration();
            d.prefix = prefix;
            d.ns = ns;
            d.scope = scope;
            if (declaration == null)
            {
                d.prev = d;
            }
            else
            {
                d.prev = declaration.prev;
                declaration.prev = d;
            }
            declaration = d;
            rover = null;
        }

        // Only elements allow default namespace declarations. The rover 
        // caches the last namespace declaration used by an element.
        public string GetPrefixOfNamespace(XNamespace ns, bool allowDefaultNamespace)
        {
            if (rover != null && rover.ns == ns && (allowDefaultNamespace || rover.prefix.Length > 0)) return rover.prefix;
            NamespaceDeclaration d = declaration;
            if (d != null)
            {
                do
                {
                    d = d.prev;
                    if (d.ns == ns)
                    {
                        NamespaceDeclaration x = declaration.prev;
                        while (x != d && x.prefix != d.prefix)
                        {
                            x = x.prev;
                        }
                        if (x == d)
                        {
                            if (allowDefaultNamespace)
                            {
                                rover = d;
                                return d.prefix;
                            }
                            else if (d.prefix.Length > 0)
                            {
                                return d.prefix;
                            }
                        }
                    }
                } while (d != declaration);
            }
            return null;
        }
    }

    internal struct StreamingElementWriter
    {
        XmlWriter writer;
        XStreamingElement element;
        List<XAttribute> attributes;
        NamespaceResolver resolver;

        public StreamingElementWriter(XmlWriter w)
        {
            writer = w;
            element = null;
            attributes = new List<XAttribute>();
            resolver = new NamespaceResolver();
        }

        void FlushElement()
        {
            if (element != null)
            {
                PushElement();
                XNamespace ns = element.Name.Namespace;
                writer.WriteStartElement(GetPrefixOfNamespace(ns, true), element.Name.LocalName, ns.NamespaceName);
                foreach (XAttribute a in attributes)
                {
                    ns = a.Name.Namespace;
                    string localName = a.Name.LocalName;
                    string namespaceName = ns.NamespaceName;
                    writer.WriteAttributeString(GetPrefixOfNamespace(ns, false), localName, namespaceName.Length == 0 && localName == "xmlns" ? XNamespace.xmlnsPrefixNamespace : namespaceName, a.Value);
                }
                element = null;
                attributes.Clear();
            }
        }

        string GetPrefixOfNamespace(XNamespace ns, bool allowDefaultNamespace)
        {
            string namespaceName = ns.NamespaceName;
            if (namespaceName.Length == 0) return string.Empty;
            string prefix = resolver.GetPrefixOfNamespace(ns, allowDefaultNamespace);
            if (prefix != null) return prefix;
            if ((object)namespaceName == (object)XNamespace.xmlPrefixNamespace) return "xml";
            if ((object)namespaceName == (object)XNamespace.xmlnsPrefixNamespace) return "xmlns";
            return null;
        }

        void PushElement()
        {
            resolver.PushScope();
            foreach (XAttribute a in attributes)
            {
                if (a.IsNamespaceDeclaration)
                {
                    resolver.Add(a.Name.NamespaceName.Length == 0 ? string.Empty : a.Name.LocalName, XNamespace.Get(a.Value));
                }
            }
        }

        void Write(object content)
        {
            if (content == null) return;
            XNode n = content as XNode;
            if (n != null)
            {
                WriteNode(n);
                return;
            }
            string s = content as string;
            if (s != null)
            {
                WriteString(s);
                return;
            }
            XAttribute a = content as XAttribute;
            if (a != null)
            {
                WriteAttribute(a);
                return;
            }
            XStreamingElement x = content as XStreamingElement;
            if (x != null)
            {
                WriteStreamingElement(x);
                return;
            }
            object[] o = content as object[];
            if (o != null)
            {
                foreach (object obj in o) Write(obj);
                return;
            }
            IEnumerable e = content as IEnumerable;
            if (e != null)
            {
                foreach (object obj in e) Write(obj);
                return;
            }
            WriteString(XContainer.GetStringValue(content));
        }

        void WriteAttribute(XAttribute a)
        {
            if (element == null) throw new InvalidOperationException(SR.InvalidOperation_WriteAttribute);
            attributes.Add(a);
        }

        void WriteNode(XNode n)
        {
            FlushElement();
            n.WriteTo(writer);
        }

        internal void WriteStreamingElement(XStreamingElement e)
        {
            FlushElement();
            element = e;
            Write(e.content);
            bool contentWritten = element == null;
            FlushElement();
            if (contentWritten)
            {
                writer.WriteFullEndElement();
            }
            else
            {
                writer.WriteEndElement();
            }
            resolver.PopScope();
        }

        void WriteString(string s)
        {
            FlushElement();
            writer.WriteString(s);
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
    [Flags()]
    public enum LoadOptions
    {
        /// <summary>Default options.</summary>
        None = 0x00000000,

        /// <summary>Preserve whitespace.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "Back-compat with System.Xml.")]
        PreserveWhitespace = 0x00000001,

        /// <summary>Set the BaseUri property.</summary>
        SetBaseUri = 0x00000002,

        /// <summary>Set the IXmlLineInfo.</summary>
        SetLineInfo = 0x00000004,
    }

    /// <summary>
    /// Specifies a set of options for Save().
    /// </summary>
    [Flags()]
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
    [Flags()]
    public enum ReaderOptions
    {
        /// <summary>Default options.</summary>
        None = 0x00000000,

        /// <summary>Remove duplicate namespace declarations.</summary>
        OmitDuplicateNamespaces = 0x00000001,
    }
}
