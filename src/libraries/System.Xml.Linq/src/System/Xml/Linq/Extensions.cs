// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using IEnumerable = System.Collections.IEnumerable;

namespace System.Xml.Linq
{
    /// <summary>
    /// Defines the LINQ to XML extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns all of the <see cref="XAttribute"/>s for each <see cref="XElement"/> of
        /// this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XAttribute"/> containing the XML
        /// Attributes for every <see cref="XElement"/> in the target <see cref="IEnumerable"/>
        /// of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XAttribute> Attributes(this IEnumerable<XElement> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return GetAttributes(source, null);
        }

        /// <summary>
        /// Returns the <see cref="XAttribute"/>s that have a matching <see cref="XName"/>.  Each
        /// <see cref="XElement"/>'s <see cref="XAttribute"/>s in the target <see cref="IEnumerable"/> 
        /// of <see cref="XElement"/> are scanned for a matching <see cref="XName"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XAttribute"/> containing the XML
        /// Attributes with a matching <see cref="XName"/> for every <see cref="XElement"/> in 
        /// the target <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XAttribute> Attributes(this IEnumerable<XElement> source, XName name)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return name != null ? GetAttributes(source, name) : XAttribute.EmptySequence;
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the ancestors (parent
        /// and it's parent up to the root) of each of the <see cref="XElement"/>s in this 
        /// <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the ancestors (parent
        /// and it's parent up to the root) of each of the <see cref="XElement"/>s in this 
        /// <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> Ancestors<T>(this IEnumerable<T> source) where T : XNode
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return GetAncestors(source, null, false);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the ancestors (parent
        /// and it's parent up to the root) that have a matching <see cref="XName"/>.  This is done for each 
        /// <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the ancestors (parent
        /// and it's parent up to the root) that have a matching <see cref="XName"/>.  This is done for each 
        /// <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> Ancestors<T>(this IEnumerable<T> source, XName name) where T : XNode
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return name != null ? GetAncestors(source, name, false) : XElement.EmptySequence;
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and it's ancestors (parent and it's parent up to the root).
        /// This is done for each <see cref="XElement"/> in this <see cref="IEnumerable"/> of 
        /// <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and it's ancestors (parent and it's parent up to the root).
        /// This is done for each <see cref="XElement"/> in this <see cref="IEnumerable"/> of 
        /// <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> AncestorsAndSelf(this IEnumerable<XElement> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return GetAncestors(source, null, true);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and it's ancestors (parent and it's parent up to the root)
        /// that match the passed in <see cref="XName"/>.  This is done for each 
        /// <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and it's ancestors (parent and it's parent up to the root)
        /// that match the passed in <see cref="XName"/>.  This is done for each 
        /// <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> AncestorsAndSelf(this IEnumerable<XElement> source, XName name)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return name != null ? GetAncestors(source, name, true) : XElement.EmptySequence;
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XNode"/> over the content of a set of nodes
        /// </summary>
        public static IEnumerable<XNode> Nodes<T>(this IEnumerable<T> source) where T : XContainer
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return NodesIterator(source);
        }

        private static IEnumerable<XNode> NodesIterator<T>(IEnumerable<T> source) where T : XContainer
        {
            foreach (XContainer root in source)
            {
                if (root != null)
                {
                    XNode n = root.LastNode;
                    if (n != null)
                    {
                        do
                        {
                            n = n.next;
                            yield return n;
                        } while (n.parent == root && n != root.content);
                    }
                }
            }
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XNode"/> over the descendants of a set of nodes
        /// </summary>     
        public static IEnumerable<XNode> DescendantNodes<T>(this IEnumerable<T> source) where T : XContainer
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return GetDescendantNodes(source, false);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the descendants (children
        /// and their children down to the leaf level).  This is done for each <see cref="XElement"/> in  
        /// this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the descendants (children
        /// and their children down to the leaf level).  This is done for each <see cref="XElement"/> in  
        /// this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> Descendants<T>(this IEnumerable<T> source) where T : XContainer
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return GetDescendants(source, null, false);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the descendants (children
        /// and their children down to the leaf level) that have a matching <see cref="XName"/>.  This is done 
        /// for each <see cref="XElement"/> in the target <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the descendants (children
        /// and their children down to the leaf level) that have a matching <see cref="XName"/>.  This is done 
        /// for each <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> Descendants<T>(this IEnumerable<T> source, XName name) where T : XContainer
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return name != null ? GetDescendants(source, name, false) : XElement.EmptySequence;
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and it's descendants
        /// that match the passed in <see cref="XName"/>.  This is done for each 
        /// <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and descendants.
        /// This is done for each 
        /// <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>      
        public static IEnumerable<XNode> DescendantNodesAndSelf(this IEnumerable<XElement> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return GetDescendantNodes(source, true);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and it's descendants (children and children's children down 
        /// to the leaf nodes).  This is done for each <see cref="XElement"/> in this <see cref="IEnumerable"/> 
        /// of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and it's descendants (children and children's children down 
        /// to the leaf nodes).  This is done for each <see cref="XElement"/> in this <see cref="IEnumerable"/> 
        /// of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> DescendantsAndSelf(this IEnumerable<XElement> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return GetDescendants(source, null, true);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and it's descendants (children and children's children down 
        /// to the leaf nodes) that match the passed in <see cref="XName"/>.  This is done for 
        /// each <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the 
        /// <see cref="XElement"/> and it's descendants (children and children's children down 
        /// to the leaf nodes) that match the passed in <see cref="XName"/>.  This is done for 
        /// each <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> DescendantsAndSelf(this IEnumerable<XElement> source, XName name)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return name != null ? GetDescendants(source, name, true) : XElement.EmptySequence;
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the child elements
        /// for each <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the child elements
        /// for each <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> Elements<T>(this IEnumerable<T> source) where T : XContainer
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return GetElements(source, null);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the child elements
        /// with a matching for each <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the child elements
        /// for each <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </returns>
        public static IEnumerable<XElement> Elements<T>(this IEnumerable<T> source, XName name) where T : XContainer
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return name != null ? GetElements(source, name) : XElement.EmptySequence;
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> of <see cref="XElement"/> containing the child elements
        /// with a matching for each <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the child elements
        /// for each <see cref="XElement"/> in this <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// in document order
        /// </returns>
        public static IEnumerable<T> InDocumentOrder<T>(this IEnumerable<T> source) where T : XNode
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return DocumentOrderIterator<T>(source);
        }

        private static IEnumerable<T> DocumentOrderIterator<T>(IEnumerable<T> source) where T : XNode
        {
            int count;
            T[] items = EnumerableHelpers.ToArray(source, out count);
            if (count > 0)
            {
                Array.Sort(items, 0, count, XNode.DocumentOrderComparer);
                for (int i = 0; i != count; ++i) yield return items[i];
            }
        }

        /// <summary>
        /// Removes each <see cref="XAttribute"/> represented in this <see cref="IEnumerable"/> of
        /// <see cref="XAttribute"/>.  Note that this method uses snapshot semantics (copies the
        /// attributes to an array before deleting each).
        /// </summary>
        public static void Remove(this IEnumerable<XAttribute> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            int count;
            XAttribute[] attributes = EnumerableHelpers.ToArray(source, out count);
            for (int i = 0; i < count; i++)
            {
                XAttribute a = attributes[i];
                if (a != null) a.Remove();
            }
        }

        /// <summary>
        /// Removes each <see cref="XNode"/> represented in this <see cref="IEnumerable"/>
        /// T which must be a derived from <see cref="XNode"/>.  Note that this method uses snapshot semantics
        /// (copies the <see cref="XNode"/>s to an array before deleting each).
        /// </summary>
        public static void Remove<T>(this IEnumerable<T> source) where T : XNode
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            int count;
            T[] nodes = EnumerableHelpers.ToArray(source, out count);
            for (int i = 0; i < count; i++)
            {
                T node = nodes[i];
                if (node != null) node.Remove();
            }
        }

        private static IEnumerable<XAttribute> GetAttributes(IEnumerable<XElement> source, XName name)
        {
            foreach (XElement e in source)
            {
                if (e != null)
                {
                    XAttribute a = e.lastAttr;
                    if (a != null)
                    {
                        do
                        {
                            a = a.next;
                            if (name == null || a.name == name) yield return a;
                        } while (a.parent == e && a != e.lastAttr);
                    }
                }
            }
        }

        private static IEnumerable<XElement> GetAncestors<T>(IEnumerable<T> source, XName name, bool self) where T : XNode
        {
            foreach (XNode node in source)
            {
                if (node != null)
                {
                    XElement e = (self ? node : node.parent) as XElement;
                    while (e != null)
                    {
                        if (name == null || e.name == name) yield return e;
                        e = e.parent as XElement;
                    }
                }
            }
        }

        private static IEnumerable<XNode> GetDescendantNodes<T>(IEnumerable<T> source, bool self) where T : XContainer
        {
            foreach (XContainer root in source)
            {
                if (root != null)
                {
                    if (self) yield return root;
                    XNode n = root;
                    while (true)
                    {
                        XContainer c = n as XContainer;
                        XNode first;
                        if (c != null && (first = c.FirstNode) != null)
                        {
                            n = first;
                        }
                        else
                        {
                            while (n != null && n != root && n == n.parent.content) n = n.parent;
                            if (n == null || n == root) break;
                            n = n.next;
                        }
                        yield return n;
                    }
                }
            }
        }

        private static IEnumerable<XElement> GetDescendants<T>(IEnumerable<T> source, XName name, bool self) where T : XContainer
        {
            foreach (XContainer root in source)
            {
                if (root != null)
                {
                    if (self)
                    {
                        XElement e = (XElement)root;
                        if (name == null || e.name == name) yield return e;
                    }
                    XNode n = root;
                    XContainer c = root;
                    while (true)
                    {
                        if (c != null && c.content is XNode)
                        {
                            n = ((XNode)c.content).next;
                        }
                        else
                        {
                            while (n != null && n != root && n == n.parent.content) n = n.parent;
                            if (n == null || n == root) break;
                            n = n.next;
                        }
                        XElement e = n as XElement;
                        if (e != null && (name == null || e.name == name)) yield return e;
                        c = e;
                    }
                }
            }
        }

        private static IEnumerable<XElement> GetElements<T>(IEnumerable<T> source, XName name) where T : XContainer
        {
            foreach (XContainer root in source)
            {
                if (root != null)
                {
                    XNode n = root.content as XNode;
                    if (n != null)
                    {
                        do
                        {
                            n = n.next;
                            XElement e = n as XElement;
                            if (e != null && (name == null || e.name == name)) yield return e;
                        } while (n.parent == root && n != root.content);
                    }
                }
            }
        }
    }
}
