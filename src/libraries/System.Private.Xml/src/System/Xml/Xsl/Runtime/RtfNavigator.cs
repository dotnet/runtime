// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// RtfNavigators store Xslt result-tree-fragments.  At runtime, the Xslt library tests to see if a Navigator
    /// is an RtfNavigator in order to enforce certain restrictions, such as prohibiting querying into Rtfs.
    /// Furthermore, Rtfs must store extra serialization information required in order to properly implement the
    /// Xslt disable-output-escaping flag.
    /// </summary>
    internal abstract class RtfNavigator : XPathNavigator
    {
        //-----------------------------------------------
        // RtfNavigator
        //-----------------------------------------------

        /// <summary>
        /// Preserve serialization hints when deep copying.
        /// </summary>
        public abstract void CopyToWriter(XmlWriter writer);

        /// <summary>
        /// Discard serialization hints and return a navigator that actually allows navigation.
        /// </summary>
        public abstract XPathNavigator ToNavigator();


        //-----------------------------------------------
        // XPathNavigator
        //-----------------------------------------------

        /// <summary>
        /// Get the XPath node type of the current node.
        /// </summary>
        public override XPathNodeType NodeType
        {
            get { return XPathNodeType.Root; }
        }

        /// <summary>
        /// Get the local name portion of the current node's name.
        /// </summary>
        public override string LocalName
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Get the namespace portion of the current node's name.
        /// </summary>
        public override string NamespaceURI
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Get the name of the current node.
        /// </summary>
        public override string Name
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Get the prefix portion of the current node's name.
        /// </summary>
        public override string Prefix
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Return true if this is an element which used a shortcut tag in its Xml 1.0 serialized form.
        /// </summary>
        public override bool IsEmptyElement
        {
            get { return false; }
        }

        /// <summary>
        /// Return the xml name table which was used to atomize all prefixes, local-names, and
        /// namespace uris in the document.
        /// </summary>
        public override XmlNameTable NameTable
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Position the navigator on the first attribute of the current node and return true.  If no attributes
        /// can be found, return false.
        /// </summary>
        public override bool MoveToFirstAttribute()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// If positioned on an attribute, move to its next sibling attribute.  If no attributes can be found,
        /// return false.
        /// </summary>
        public override bool MoveToNextAttribute()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Position the navigator on the namespace within the specified scope.  If no matching namespace
        /// can be found, return false.
        /// </summary>
        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Position the navigator on the next namespace within the specified scope.  If no matching namespace
        /// can be found, return false.
        /// </summary>
        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// If the current node is an attribute or namespace (not content), return false.  Otherwise,
        /// move to the next content node.  Return false if there are no more content nodes.
        /// </summary>
        public override bool MoveToNext()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// If the current node is an attribute or namespace (not content), return false.  Otherwise,
        /// move to the previous (sibling) content node.  Return false if there are no previous content nodes.
        /// </summary>
        public override bool MoveToPrevious()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Move to the first content-typed child of the current node.  Return false if the current
        /// node has no content children.
        /// </summary>
        public override bool MoveToFirstChild()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Position the navigator on the parent of the current node.  If the current node has no parent,
        /// return false.
        /// </summary>
        public override bool MoveToParent()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Position to the navigator to the element whose id is equal to the specified "id" string.
        /// </summary>
        public override bool MoveToId(string id)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns true if this navigator is positioned to the same node as the "other" navigator.  Returns false
        /// if not, or if the "other" navigator is not the same type as this navigator.
        /// </summary>
        public override bool IsSamePosition(XPathNavigator other)
        {
            throw new NotSupportedException();
        }
    }


    /// <summary>
    /// This navigator is a cursor over a cache that stores Xslt disable-output-escaping flags.
    /// </summary>
    internal sealed class RtfTreeNavigator : RtfNavigator
    {
        private XmlEventCache _events;
        private NavigatorConstructor _constr;
        private XmlNameTable _nameTable;


        //-----------------------------------------------
        // Constructors
        //-----------------------------------------------

        /// <summary>
        /// Create a new navigator over the specified cache of Xml events.
        /// </summary>
        public RtfTreeNavigator(XmlEventCache events, XmlNameTable nameTable)
        {
            _events = events;
            _constr = new NavigatorConstructor();
            _nameTable = nameTable;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public RtfTreeNavigator(RtfTreeNavigator that)
        {
            _events = that._events;
            _constr = that._constr;
            _nameTable = that._nameTable;
        }


        //-----------------------------------------------
        // RtfNavigator
        //-----------------------------------------------

        /// <summary>
        /// Preserve serialization hints when deep copying.
        /// </summary>
        public override void CopyToWriter(XmlWriter writer)
        {
            _events.EventsToWriter(writer);
        }

        /// <summary>
        /// Discard serialization hints and return a navigator that actually allows navigation.
        /// </summary>
        public override XPathNavigator ToNavigator()
        {
            return _constr.GetNavigator(_events, _nameTable);
        }


        //-----------------------------------------------
        // XPathItem
        //-----------------------------------------------

        /// <summary>
        /// Get the string value of the current node, computed using data model dm:string-value rules.
        /// If the node has a typed value, return the string representation of the value.  If the node
        /// is not a parent type (comment, text, pi, etc.), get its simple text value.  Otherwise,
        /// concatenate all text node descendants of the current node.
        /// </summary>
        public override string Value
        {
            get { return _events.EventsToString(); }
        }


        //-----------------------------------------------
        // XPathNavigator
        //-----------------------------------------------

        /// <summary>
        /// Get the base URI of the Rtf.
        /// </summary>
        public override string BaseURI
        {
            get { return _events.BaseUri; }
        }

        /// <summary>
        /// Create a copy of this navigator, positioned to the same node in the tree.
        /// </summary>
        public override XPathNavigator Clone()
        {
            return new RtfTreeNavigator(this);
        }

        /// <summary>
        /// Position this navigator to the same position as the "other" navigator.  If the "other" navigator
        /// is not of the same type as this navigator, then return false.
        /// </summary>
        public override bool MoveTo(XPathNavigator other)
        {
            RtfTreeNavigator? that = other as RtfTreeNavigator;
            if (that != null)
            {
                _events = that._events;
                _constr = that._constr;
                _nameTable = that._nameTable;
                return true;
            }
            return false;
        }
    }


    /// <summary>
    /// This RtfNavigator specializes the case of a root node having a single text node child.  This is a very common
    /// case, such as in &lt;xsl:variable name="foo"&gt;bar&lt;/xsl:variable&gt;.
    /// </summary>
    internal sealed class RtfTextNavigator : RtfNavigator
    {
        private string _text, _baseUri;
        private NavigatorConstructor _constr;


        //-----------------------------------------------
        // Constructors
        //-----------------------------------------------

        /// <summary>
        /// Create a new navigator having a text node with value = "text" string.
        /// </summary>
        public RtfTextNavigator(string text, string baseUri)
        {
            _text = text;
            _baseUri = baseUri;
            _constr = new NavigatorConstructor();
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public RtfTextNavigator(RtfTextNavigator that)
        {
            _text = that._text;
            _baseUri = that._baseUri;
            _constr = that._constr;
        }


        //-----------------------------------------------
        // RtfNavigator
        //-----------------------------------------------

        /// <summary>
        /// Preserve serialization hints when deep copying.
        /// </summary>
        public override void CopyToWriter(XmlWriter writer)
        {
            writer.WriteString(Value);
        }

        /// <summary>
        /// Discard serialization hints and return a navigator that actually allows navigation.
        /// </summary>
        public override XPathNavigator ToNavigator()
        {
            return _constr.GetNavigator(_text, _baseUri, new NameTable());
        }


        //-----------------------------------------------
        // XPathItem
        //-----------------------------------------------

        /// <summary>
        /// Get the string value of the current node, computed using data model dm:string-value rules.
        /// If the node has a typed value, return the string representation of the value.  If the node
        /// is not a parent type (comment, text, pi, etc.), get its simple text value.  Otherwise,
        /// concatenate all text node descendants of the current node.
        /// </summary>
        public override string Value
        {
            get { return _text; }
        }


        //-----------------------------------------------
        // XPathNavigator
        //-----------------------------------------------

        /// <summary>
        /// Get the base URI of the Rtf.
        /// </summary>
        public override string BaseURI
        {
            get { return _baseUri; }
        }

        /// <summary>
        /// Create a copy of this navigator, positioned to the same node in the tree.
        /// </summary>
        public override XPathNavigator Clone()
        {
            return new RtfTextNavigator(this);
        }

        /// <summary>
        /// Position this navigator to the same position as the "other" navigator.  If the "other" navigator
        /// is not of the same type as this navigator, then return false.
        /// </summary>
        public override bool MoveTo(XPathNavigator other)
        {
            RtfTextNavigator? that = other as RtfTextNavigator;
            if (that != null)
            {
                _text = that._text;
                _baseUri = that._baseUri;
                _constr = that._constr;
                return true;
            }
            return false;
        }
    }


    /// <summary>
    /// This class creates a document on the first call to GetNavigator(), and returns a Navigator from it.  On
    /// subsequent calls, Navigators from the same document are returned (no new document is created).
    /// </summary>
    internal sealed class NavigatorConstructor
    {
        private object? _cache;

        /// <summary>
        /// Create a document from the cache of events.  If a document has already been created previously, return it.
        /// This method is thread-safe, and is always guaranteed to return the exact same document, no matter how many
        /// threads have called it concurrently.
        /// </summary>
        public XPathNavigator GetNavigator(XmlEventCache events, XmlNameTable nameTable)
        {
            if (_cache == null)
            {
                // Create XPathDocument from event cache
                XPathDocument doc = new XPathDocument(nameTable);
                XmlRawWriter writer = doc.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames | (events.HasRootNode ? XPathDocument.LoadFlags.None : XPathDocument.LoadFlags.Fragment), events.BaseUri);

                events.EventsToWriter(writer);
                writer.Close();

                _cache = doc;
            }

            return ((XPathDocument)_cache).CreateNavigator();
        }

        /// <summary>
        /// Create a document containing a root node and a single text node child with "text" as its text value.
        /// This method is thread-safe, and is always guaranteed to return the exact same document, no matter how many
        /// threads have called it concurrently.
        /// </summary>
        public XPathNavigator GetNavigator(string text, string baseUri, XmlNameTable nameTable)
        {
            if (_cache == null)
            {
                // Create XPathDocument
                XPathDocument doc = new XPathDocument(nameTable);
                XmlRawWriter writer = doc.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames, baseUri);
                writer.WriteString(text);
                writer.Close();

                _cache = doc;
            }

            return ((XPathDocument)_cache).CreateNavigator();
        }
    }
}
