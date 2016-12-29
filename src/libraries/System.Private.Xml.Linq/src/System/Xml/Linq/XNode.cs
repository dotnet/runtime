// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CultureInfo = System.Globalization.CultureInfo;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using StringBuilder = System.Text.StringBuilder;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents nodes (elements, comments, document type, processing instruction,
    /// and text nodes) in the XML tree.
    /// </summary>
    /// <remarks>
    /// Nodes in the XML tree consist of objects of the following classes:
    /// <see cref="XElement"/>,
    /// <see cref="XComment"/>,
    /// <see cref="XDocument"/>,
    /// <see cref="XProcessingInstruction"/>,
    /// <see cref="XText"/>,
    /// <see cref="XDocumentType"/>
    /// Note that an <see cref="XAttribute"/> is not an <see cref="XNode"/>.
    /// </remarks>
    public abstract class XNode : XObject
    {
        private static XNodeDocumentOrderComparer s_documentOrderComparer;
        private static XNodeEqualityComparer s_equalityComparer;

        internal XNode next;

        internal XNode() { }

        /// <summary>
        /// Gets the next sibling node of this node.
        /// </summary>
        /// <remarks>
        /// If this property does not have a parent, or if there is no next node,
        /// then this property returns null.
        /// </remarks>
        public XNode NextNode
        {
            get
            {
                return parent == null || this == parent.content ? null : next;
            }
        }

        /// <summary>
        /// Gets the previous sibling node of this node.
        /// </summary>
        /// <remarks>
        /// If this property does not have a parent, or if there is no previous node,
        /// then this property returns null.
        /// </remarks>
        public XNode PreviousNode
        {
            get
            {
                if (parent == null) return null;
                XNode n = ((XNode)parent.content).next;
                XNode p = null;
                while (n != this)
                {
                    p = n;
                    n = n.next;
                }
                return p;
            }
        }

        /// <summary>
        /// Gets a comparer that can compare the relative position of two nodes.
        /// </summary>
        public static XNodeDocumentOrderComparer DocumentOrderComparer
        {
            get
            {
                if (s_documentOrderComparer == null) s_documentOrderComparer = new XNodeDocumentOrderComparer();
                return s_documentOrderComparer;
            }
        }

        /// <summary>
        /// Gets a comparer that can compare two nodes for value equality.
        /// </summary>
        public static XNodeEqualityComparer EqualityComparer
        {
            get
            {
                if (s_equalityComparer == null) s_equalityComparer = new XNodeEqualityComparer();
                return s_equalityComparer;
            }
        }

        /// <overloads>
        /// Adds the specified content immediately after this node. The
        /// content can be simple content, a collection of
        /// content objects, a parameter list of content objects,
        /// or null.
        /// </overloads>
        /// <summary>
        /// Adds the specified content immediately after this node.
        /// </summary>
        /// <param name="content">
        /// A content object containing simple content or a collection of content objects
        /// to be added after this node.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the parent is null.
        /// </exception>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void AddAfterSelf(object content)
        {
            if (parent == null) throw new InvalidOperationException(SR.InvalidOperation_MissingParent);
            new Inserter(parent, this).Add(content);
        }

        /// <summary>
        /// Adds the specified content immediately after this node.
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
        public void AddAfterSelf(params object[] content)
        {
            AddAfterSelf((object)content);
        }

        /// <overloads>
        /// Adds the specified content immediately before this node. The
        /// content can be simple content, a collection of
        /// content objects, a parameter list of content objects,
        /// or null.
        /// </overloads>
        /// <summary>
        /// Adds the specified content immediately before this node.
        /// </summary>
        /// <param name="content">
        /// A content object containing simple content or a collection of content objects
        /// to be added after this node.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the parent is null.
        /// </exception>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void AddBeforeSelf(object content)
        {
            if (parent == null) throw new InvalidOperationException(SR.InvalidOperation_MissingParent);
            XNode p = (XNode)parent.content;
            while (p.next != this) p = p.next;
            if (p == parent.content) p = null;
            new Inserter(parent, p).Add(content);
        }

        /// <summary>
        /// Adds the specified content immediately before this node.
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
        public void AddBeforeSelf(params object[] content)
        {
            AddBeforeSelf((object)content);
        }

        /// <overloads>
        /// Returns an collection of the ancestor elements for this node.
        /// Optionally an node name can be specified to filter for a specific ancestor element.
        /// </overloads>
        /// <summary>
        /// Returns a collection of the ancestor elements of this node.
        /// </summary>
        /// <returns>
        /// The ancestor elements of this node.
        /// </returns>
        /// <remarks>
        /// This method will not return itself in the results.
        /// </remarks>
        public IEnumerable<XElement> Ancestors()
        {
            return GetAncestors(null, false);
        }

        /// <summary>
        /// Returns a collection of the ancestor elements of this node with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the ancestor elements to find.
        /// </param>
        /// <returns>
        /// A collection of the ancestor elements of this node with the specified name.
        /// </returns>
        /// <remarks>
        /// This method will not return itself in the results.
        /// </remarks>
        public IEnumerable<XElement> Ancestors(XName name)
        {
            return name != null ? GetAncestors(name, false) : XElement.EmptySequence;
        }

        /// <summary>
        /// Compares two nodes to determine their relative XML document order.
        /// </summary>
        /// <param name="n1">First node to compare.</param>
        /// <param name="n2">Second node to compare.</param>
        /// <returns>
        /// 0 if the nodes are equal; -1 if n1 is before n2; 1 if n1 is after n2.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the two nodes do not share a common ancestor.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Reviewed.")]
        public static int CompareDocumentOrder(XNode n1, XNode n2)
        {
            if (n1 == n2) return 0;
            if (n1 == null) return -1;
            if (n2 == null) return 1;
            if (n1.parent != n2.parent)
            {
                int height = 0;
                XNode p1 = n1;
                while (p1.parent != null)
                {
                    p1 = p1.parent;
                    height++;
                }
                XNode p2 = n2;
                while (p2.parent != null)
                {
                    p2 = p2.parent;
                    height--;
                }
                if (p1 != p2) throw new InvalidOperationException(SR.InvalidOperation_MissingAncestor);
                if (height < 0)
                {
                    do
                    {
                        n2 = n2.parent;
                        height++;
                    } while (height != 0);
                    if (n1 == n2) return -1;
                }
                else if (height > 0)
                {
                    do
                    {
                        n1 = n1.parent;
                        height--;
                    } while (height != 0);
                    if (n1 == n2) return 1;
                }
                while (n1.parent != n2.parent)
                {
                    n1 = n1.parent;
                    n2 = n2.parent;
                }
            }
            else if (n1.parent == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_MissingAncestor);
            }
            XNode n = (XNode)n1.parent.content;
            while (true)
            {
                n = n.next;
                if (n == n1) return -1;
                if (n == n2) return 1;
            }
        }

        /// <summary>
        /// Creates an <see cref="XmlReader"/> for the node.
        /// </summary>
        /// <returns>An <see cref="XmlReader"/> that can be used to read the node and its descendants.</returns>
        public XmlReader CreateReader()
        {
            return new XNodeReader(this, null);
        }

        /// <summary>
        /// Creates an <see cref="XmlReader"/> for the node.
        /// </summary>
        /// <param name="readerOptions">
        /// Options to be used for the returned reader. These override the default usage of annotations from the tree.
        /// </param>
        /// <returns>An <see cref="XmlReader"/> that can be used to read the node and its descendants.</returns>
        public XmlReader CreateReader(ReaderOptions readerOptions)
        {
            return new XNodeReader(this, null, readerOptions);
        }

        /// <summary>
        /// Returns a collection of the sibling nodes after this node, in document order.
        /// </summary>
        /// <remarks>
        /// This method only includes sibling nodes in the returned collection.
        /// </remarks>
        /// <returns>The nodes after this node.</returns>
        public IEnumerable<XNode> NodesAfterSelf()
        {
            XNode n = this;
            while (n.parent != null && n != n.parent.content)
            {
                n = n.next;
                yield return n;
            }
        }

        /// <summary>
        /// Returns a collection of the sibling nodes before this node, in document order.
        /// </summary>
        /// <remarks>
        /// This method only includes sibling nodes in the returned collection.
        /// </remarks>
        /// <returns>The nodes after this node.</returns>
        public IEnumerable<XNode> NodesBeforeSelf()
        {
            if (parent != null)
            {
                XNode n = (XNode)parent.content;
                do
                {
                    n = n.next;
                    if (n == this) break;
                    yield return n;
                } while (parent != null && parent == n.parent);
            }
        }

        /// <summary>
        /// Returns a collection of the sibling element nodes after this node, in document order.
        /// </summary>
        /// <remarks>
        /// This method only includes sibling element nodes in the returned collection.
        /// </remarks>
        /// <returns>The element nodes after this node.</returns>
        public IEnumerable<XElement> ElementsAfterSelf()
        {
            return GetElementsAfterSelf(null);
        }

        /// <summary>
        /// Returns a collection of the sibling element nodes with the specified name
        /// after this node, in document order.
        /// </summary>
        /// <remarks>
        /// This method only includes sibling element nodes in the returned collection.
        /// </remarks>
        /// <returns>The element nodes after this node with the specified name.</returns>
        /// <param name="name">The name of elements to enumerate.</param>
        public IEnumerable<XElement> ElementsAfterSelf(XName name)
        {
            return name != null ? GetElementsAfterSelf(name) : XElement.EmptySequence;
        }

        /// <summary>
        /// Returns a collection of the sibling element nodes before this node, in document order.
        /// </summary>
        /// <remarks>
        /// This method only includes sibling element nodes in the returned collection.
        /// </remarks>
        /// <returns>The element nodes before this node.</returns>
        public IEnumerable<XElement> ElementsBeforeSelf()
        {
            return GetElementsBeforeSelf(null);
        }

        /// <summary>
        /// Returns a collection of the sibling element nodes with the specified name
        /// before this node, in document order.
        /// </summary>
        /// <remarks>
        /// This method only includes sibling element nodes in the returned collection.
        /// </remarks>
        /// <returns>The element nodes before this node with the specified name.</returns>
        /// <param name="name">The name of elements to enumerate.</param>
        public IEnumerable<XElement> ElementsBeforeSelf(XName name)
        {
            return name != null ? GetElementsBeforeSelf(name) : XElement.EmptySequence;
        }

        /// <summary>
        /// Determines if the current node appears after a specified node 
        /// in terms of document order.
        /// </summary>
        /// <param name="node">The node to compare for document order.</param>
        /// <returns>True if this node appears after the specified node; false if not.</returns>
        public bool IsAfter(XNode node)
        {
            return CompareDocumentOrder(this, node) > 0;
        }

        /// <summary>
        /// Determines if the current node appears before a specified node 
        /// in terms of document order.
        /// </summary>
        /// <param name="node">The node to compare for document order.</param>
        /// <returns>True if this node appears before the specified node; false if not.</returns>
        public bool IsBefore(XNode node)
        {
            return CompareDocumentOrder(this, node) < 0;
        }

        /// <summary>
        /// Creates an <see cref="XNode"/> from an <see cref="XmlReader"/>.
        /// The runtime type of the node is determined by the node type
        /// (<see cref="XObject.NodeType"/>) of the first node encountered
        /// in the reader.
        /// </summary>
        /// <param name="reader">An <see cref="XmlReader"/> positioned at the node to read into this <see cref="XNode"/>.</param>
        /// <returns>An <see cref="XNode"/> that contains the nodes read from the reader.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="XmlReader"/> is not positioned on a recognized node type.
        /// </exception>
        public static XNode ReadFrom(XmlReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (reader.ReadState != ReadState.Interactive) throw new InvalidOperationException(SR.InvalidOperation_ExpectedInteractive);
            switch (reader.NodeType)
            {
                case XmlNodeType.Text:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    return new XText(reader);
                case XmlNodeType.CDATA:
                    return new XCData(reader);
                case XmlNodeType.Comment:
                    return new XComment(reader);
                case XmlNodeType.DocumentType:
                    return new XDocumentType(reader);
                case XmlNodeType.Element:
                    return new XElement(reader);
                case XmlNodeType.ProcessingInstruction:
                    return new XProcessingInstruction(reader);
                default:
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedNodeType, reader.NodeType));
            }
        }

        /// <summary>
        /// Creates an <see cref="XNode"/> from an <see cref="XmlReader"/>.
        /// The runtime type of the node is determined by the node type
        /// (<see cref="XObject.NodeType"/>) of the first node encountered
        /// in the reader.
        /// </summary>
        /// <param name="reader">An <see cref="XmlReader"/> positioned at the node to read into this <see cref="XNode"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>An <see cref="XNode"/> that contains the nodes read from the reader.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="XmlReader"/> is not positioned on a recognized node type.
        /// </exception>
        public static Task<XNode> ReadFromAsync(XmlReader reader, CancellationToken cancellationToken)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<XNode>(cancellationToken);
            return ReadFromAsyncInternal(reader, cancellationToken);
        }

        private static async Task<XNode> ReadFromAsyncInternal(XmlReader reader, CancellationToken cancellationToken)
        {
            if (reader.ReadState != ReadState.Interactive) throw new InvalidOperationException(SR.InvalidOperation_ExpectedInteractive);

            XNode ret;

            switch (reader.NodeType)
            {
                case XmlNodeType.Text:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    ret = new XText(reader.Value);
                    break;
                case XmlNodeType.CDATA:
                    ret = new XCData(reader.Value);
                    break;
                case XmlNodeType.Comment:
                    ret = new XComment(reader.Value);
                    break;
                case XmlNodeType.DocumentType:
                    var name = reader.Name;
                    var publicId = reader.GetAttribute("PUBLIC");
                    var systemId = reader.GetAttribute("SYSTEM");
                    var internalSubset = reader.Value;

                    ret = new XDocumentType(name, publicId, systemId, internalSubset);
                    break;
                case XmlNodeType.Element:
                    return await XElement.CreateAsync(reader, cancellationToken).ConfigureAwait(false);
                case XmlNodeType.ProcessingInstruction:
                    var target = reader.Name;
                    var data = reader.Value;

                    ret = new XProcessingInstruction(target, data);
                    break;
                default:
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnexpectedNodeType, reader.NodeType));
            }

            cancellationToken.ThrowIfCancellationRequested();
            await reader.ReadAsync().ConfigureAwait(false);

            return ret;
        }

        /// <summary>
        /// Removes this XNode from the underlying XML tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the parent is null.
        /// </exception>
        public void Remove()
        {
            if (parent == null) throw new InvalidOperationException(SR.InvalidOperation_MissingParent);
            parent.RemoveNode(this);
        }

        /// <overloads>
        /// Replaces this node with the specified content. The
        /// content can be simple content, a collection of
        /// content objects, a parameter list of content objects,
        /// or null.
        /// </overloads>
        /// <summary>
        /// Replaces the content of this <see cref="XNode"/>.
        /// </summary>
        /// <param name="content">Content that replaces this node.</param>
        public void ReplaceWith(object content)
        {
            if (parent == null) throw new InvalidOperationException(SR.InvalidOperation_MissingParent);
            XContainer c = parent;
            XNode p = (XNode)parent.content;
            while (p.next != this) p = p.next;
            if (p == parent.content) p = null;
            parent.RemoveNode(this);
            if (p != null && p.parent != c) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
            new Inserter(c, p).Add(content);
        }

        /// <summary>
        /// Replaces this node with the specified content.
        /// </summary>
        /// <param name="content">Content that replaces this node.</param>
        public void ReplaceWith(params object[] content)
        {
            ReplaceWith((object)content);
        }

        /// <summary>
        /// Provides the formatted XML text representation.
        /// You can use the SaveOptions as an annotation on this node or its ancestors, then this method will use those options.
        /// </summary>
        /// <returns>A formatted XML string.</returns>
        public override string ToString()
        {
            return GetXmlString(GetSaveOptionsFromAnnotations());
        }

        /// <summary>
        /// Provides the XML text representation.
        /// </summary>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the output is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        /// <returns>An XML string.</returns>
        public string ToString(SaveOptions options)
        {
            return GetXmlString(options);
        }

        /// <summary>
        /// Compares the values of two nodes, including the values of all descendant nodes.
        /// </summary>
        /// <param name="n1">The first node to compare.</param>
        /// <param name="n2">The second node to compare.</param>
        /// <returns>true if the nodes are equal, false otherwise.</returns>
        /// <remarks>
        /// A null node is equal to another null node but unequal to a non-null
        /// node. Two <see cref="XNode"/> objects of different types are never equal. Two
        /// <see cref="XText"/> nodes are equal if they contain the same text. Two
        /// <see cref="XElement"/> nodes are equal if they have the same tag name, the same
        /// set of attributes with the same values, and, ignoring comments and processing
        /// instructions, contain two equal length sequences of equal content nodes.
        /// Two <see cref="XDocument"/>s are equal if their root nodes are equal. Two
        /// <see cref="XComment"/> nodes are equal if they contain the same comment text.
        /// Two <see cref="XProcessingInstruction"/> nodes are equal if they have the same
        /// target and data. Two <see cref="XDocumentType"/> nodes are equal if the have the
        /// same name, public id, system id, and internal subset.</remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Reviewed.")]
        public static bool DeepEquals(XNode n1, XNode n2)
        {
            if (n1 == n2) return true;
            if (n1 == null || n2 == null) return false;
            return n1.DeepEquals(n2);
        }

        /// <summary>
        /// Write the current node to an <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> to write the current node into.</param>
        public abstract void WriteTo(XmlWriter writer);

        /// <summary>
        /// Write the current node to an <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="XmlWriter"/> to write the current node into.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public abstract Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken);

        internal virtual void AppendText(StringBuilder sb)
        {
        }

        internal abstract XNode CloneNode();

        internal abstract bool DeepEquals(XNode node);

        internal IEnumerable<XElement> GetAncestors(XName name, bool self)
        {
            XElement e = (self ? this : parent) as XElement;
            while (e != null)
            {
                if (name == null || e.name == name) yield return e;
                e = e.parent as XElement;
            }
        }

        private IEnumerable<XElement> GetElementsAfterSelf(XName name)
        {
            XNode n = this;
            while (n.parent != null && n != n.parent.content)
            {
                n = n.next;
                XElement e = n as XElement;
                if (e != null && (name == null || e.name == name)) yield return e;
            }
        }

        private IEnumerable<XElement> GetElementsBeforeSelf(XName name)
        {
            if (parent != null)
            {
                XNode n = (XNode)parent.content;
                do
                {
                    n = n.next;
                    if (n == this) break;
                    XElement e = n as XElement;
                    if (e != null && (name == null || e.name == name)) yield return e;
                } while (parent != null && parent == n.parent);
            }
        }

        internal abstract int GetDeepHashCode();

        // The settings simulate a non-validating processor with the external
        // entity resolution disabled. The processing of the internal subset is 
        // enabled by default. In order to prevent DoS attacks, the expanded 
        // size of the internal subset is limited to 10 million characters.
        internal static XmlReaderSettings GetXmlReaderSettings(LoadOptions o)
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            if ((o & LoadOptions.PreserveWhitespace) == 0) rs.IgnoreWhitespace = true;

            // DtdProcessing.Parse; Parse is not defined in the public contract 
            rs.DtdProcessing = (DtdProcessing)2;
            rs.MaxCharactersFromEntities = (long)1e7;
            // rs.XmlResolver = null;
            return rs;
        }

        internal static XmlWriterSettings GetXmlWriterSettings(SaveOptions o)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            if ((o & SaveOptions.DisableFormatting) == 0) ws.Indent = true;
            if ((o & SaveOptions.OmitDuplicateNamespaces) != 0) ws.NamespaceHandling |= NamespaceHandling.OmitDuplicates;
            return ws;
        }

        private string GetXmlString(SaveOptions o)
        {
            using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
            {
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.OmitXmlDeclaration = true;
                if ((o & SaveOptions.DisableFormatting) == 0) ws.Indent = true;
                if ((o & SaveOptions.OmitDuplicateNamespaces) != 0) ws.NamespaceHandling |= NamespaceHandling.OmitDuplicates;
                if (this is XText) ws.ConformanceLevel = ConformanceLevel.Fragment;
                using (XmlWriter w = XmlWriter.Create(sw, ws))
                {
                    XDocument n = this as XDocument;
                    if (n != null)
                    {
                        n.WriteContentTo(w);
                    }
                    else
                    {
                        WriteTo(w);
                    }
                }
                return sw.ToString();
            }
        }
    }
}
