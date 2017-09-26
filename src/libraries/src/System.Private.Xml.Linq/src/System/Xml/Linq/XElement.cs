// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Threading;
using System.Threading.Tasks;

using CultureInfo = System.Globalization.CultureInfo;
using IEnumerable = System.Collections.IEnumerable;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using StringBuilder = System.Text.StringBuilder;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents an XML element.
    /// </summary>
    /// <remarks>
    /// An element has an <see cref="XName"/>, optionally one or more attributes,
    /// and can optionally contain content (see <see cref="XContainer.Nodes"/>.
    /// An <see cref="XElement"/> can contain the following types of content:
    ///   <list>
    ///     <item>string (Text content)</item>
    ///     <item><see cref="XElement"/></item>
    ///     <item><see cref="XComment"/></item>
    ///     <item><see cref="XProcessingInstruction"/></item>
    ///   </list>
    /// </remarks>
    [XmlSchemaProvider(null, IsAny = true)]
    public class XElement : XContainer, IXmlSerializable
    {
        /// <summary>
        /// Gets an empty collection of elements.
        /// </summary>
        public static IEnumerable<XElement> EmptySequence
        {
            get
            {
                return Array.Empty<XElement>();
            }
        }

        internal XName name;
        internal XAttribute lastAttr;

        /// <summary>
        /// Initializes a new instance of the XElement class with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the element.
        /// </param>
        public XElement(XName name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            this.name = name;
        }

        /// <summary>
        /// Initializes a new instance of the XElement class with the specified name and content.
        /// </summary>
        /// <param name="name">
        /// The element name.
        /// </param>
        /// <param name="content">The initial contents of the element.</param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public XElement(XName name, object content)
            : this(name)
        {
            AddContentSkipNotify(content);
        }

        /// <summary>
        /// Initializes a new instance of the XElement class with the specified name and content.
        /// </summary>
        /// <param name="name">
        /// The element name.
        /// </param>
        /// <param name="content">
        /// The initial content of the element.
        /// </param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public XElement(XName name, params object[] content) : this(name, (object)content) { }

        /// <summary>
        /// Initializes a new instance of the XElement class from another XElement object.
        /// </summary>
        /// <param name="other">
        /// Another element that will be copied to this element.
        /// </param>
        /// <remarks>
        /// This constructor makes a deep copy from one element to another.
        /// </remarks>
        public XElement(XElement other)
            : base(other)
        {
            this.name = other.name;
            XAttribute a = other.lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next;
                    AppendAttributeSkipNotify(new XAttribute(a));
                } while (a != other.lastAttr);
            }
        }

        /// <summary>
        /// Initializes an XElement object from an <see cref="XStreamingElement"/> object.
        /// </summary>
        /// <param name="other">
        /// The <see cref="XStreamingElement"/> object whose value will be used
        /// to initialize the new element.
        /// </param>
        public XElement(XStreamingElement other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            name = other.name;
            AddContentSkipNotify(other.content);
        }

#if uap
        // XmlSerializer needs to reflect on the default constructor of XElement.
        // We need to make the ctor public on UWP to keep the metadata for it.
        public XElement()
#else
        internal XElement()
#endif
            : this("default")
        {
        }

        internal XElement(XmlReader r)
            : this(r, LoadOptions.None)
        {
        }

        private XElement(AsyncConstructionSentry s)
        {
            // Dummy ctor used to avoid public default ctor.  This is used
            // by async methods meant to perform the same operations as
            // the XElement constructors that do synchronous processing;
            // the async methods instead construct an XElement using this
            // constructor (which doesn't do any processing) and then themselves
            // do the async processing.  This is because ctors can't be 'async'.
        }
        private struct AsyncConstructionSentry { }

        internal XElement(XmlReader r, LoadOptions o)
        {
            ReadElementFrom(r, o);
        }

        internal static async Task<XElement> CreateAsync(XmlReader r, CancellationToken cancellationToken)
        {
            XElement xe = new XElement(default(AsyncConstructionSentry));
            await xe.ReadElementFromAsync(r, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            return xe;
        }

        ///<overloads>
        /// Outputs this <see cref="XElement"/>'s underlying XML tree.  The output can
        /// be saved to a file, a <see cref="Stream"/>, a <see cref="TextWriter"/>,
        /// or an <see cref="XmlWriter"/>.  Optionally whitespace can be preserved.  
        /// </overloads>
        /// <summary>
        /// Output this <see cref="XElement"/> to a file.
        /// </summary>
        /// <remarks>
        /// The format will be indented by default.  If you want
        /// no indenting then use the SaveOptions version of Save (see
        /// <see cref="XElement.Save(string, SaveOptions)"/>) enabling 
        /// SaveOptions.DisableFormatting. 
        /// There is also an option SaveOptions.OmitDuplicateNamespaces for removing duplicate namespace declarations. 
        /// Or instead use the SaveOptions as an annotation on this node or its ancestors, then this method will use those options.
        /// </remarks>
        /// <param name="fileName">
        /// The name of the file to output the XML to.
        /// </param>
        public void Save(string fileName)
        {
            Save(fileName, GetSaveOptionsFromAnnotations());
        }

        /// <summary>
        /// Output this <see cref="XElement"/> to a file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to output the XML to.  
        /// </param>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the output is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        public void Save(string fileName, SaveOptions options)
        {
            XmlWriterSettings ws = GetXmlWriterSettings(options);
            using (XmlWriter w = XmlWriter.Create(fileName, ws))
            {
                Save(w);
            }
        }

        /// <summary>
        /// Gets the first attribute of an element.
        /// </summary>
        public XAttribute FirstAttribute
        {
            get { return lastAttr != null ? lastAttr.next : null; }
        }

        /// <summary>
        /// Gets a value indicating whether the element has at least one attribute.
        /// </summary>
        public bool HasAttributes
        {
            get { return lastAttr != null; }
        }

        /// <summary>
        /// Gets a value indicating whether the element has at least one child element.
        /// </summary>
        public bool HasElements
        {
            get
            {
                XNode n = content as XNode;
                if (n != null)
                {
                    do
                    {
                        if (n is XElement) return true;
                        n = n.next;
                    } while (n != content);
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the element contains no content.
        /// </summary>
        public bool IsEmpty
        {
            get { return content == null; }
        }

        /// <summary>
        /// Gets the last attribute of an element.
        /// </summary>
        public XAttribute LastAttribute
        {
            get { return lastAttr; }
        }

        /// <summary>
        /// Gets the name of this element.
        /// </summary>
        public XName Name
        {
            get
            {
                return name;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Name);
                name = value;
                if (notify) NotifyChanged(this, XObjectChangeEventArgs.Name);
            }
        }

        /// <summary>
        /// Gets the node type for this node.
        /// </summary>
        /// <remarks>
        /// This property will always return XmlNodeType.Text.
        /// </remarks>
        public override XmlNodeType NodeType
        {
            get
            {
                return XmlNodeType.Element;
            }
        }

        /// <summary>
        /// Gets the text contents of this element.
        /// </summary>
        /// <remarks>
        /// If there is text content interspersed with nodes (mixed content) then the text content
        /// will be concatenated and returned.
        /// </remarks>
        public string Value
        {
            get
            {
                if (content == null) return string.Empty;
                string s = content as string;
                if (s != null) return s;
                StringBuilder sb = StringBuilderCache.Acquire();
                AppendText(sb);
                return StringBuilderCache.GetStringAndRelease(sb);
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                RemoveNodes();
                Add(value);
            }
        }

        /// <overloads>
        /// Returns this <see cref="XElement"/> and all of it's ancestors up
        /// to the root node.  Optionally an <see cref="XName"/> can be passed
        /// in to target a specific ancestor(s).
        /// <seealso cref="XNode.Ancestors()"/>
        /// </overloads>
        /// <summary>
        /// Returns this <see cref="XElement"/> and all of it's ancestors up to 
        /// the root node.
        /// <seealso cref="XNode.Ancestors()"/>
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing all of
        /// this <see cref="XElement"/>'s ancestors up to the root node (including
        /// this <see cref="XElement"/>.
        /// </returns>
        public IEnumerable<XElement> AncestorsAndSelf()
        {
            return GetAncestors(null, true);
        }

        /// <summary>
        /// Returns the ancestor(s) of this <see cref="XElement"/> with the matching
        /// <see cref="XName"/>. If this <see cref="XElement"/>'s <see cref="XName"/>
        /// matches the <see cref="XName"/> passed in then it will be included in the 
        /// resulting <see cref="IEnumerable"/> or <see cref="XElement"/>.
        /// <seealso cref="XNode.Ancestors()"/>
        /// </summary>
        /// <param name="name">
        /// The <see cref="XName"/> of the target ancestor.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing the
        /// ancestors of this <see cref="XElement"/> with a matching <see cref="XName"/>.
        /// </returns>
        public IEnumerable<XElement> AncestorsAndSelf(XName name)
        {
            return name != null ? GetAncestors(name, true) : XElement.EmptySequence;
        }

        /// <summary>
        /// Returns the <see cref="XAttribute"/> associated with this <see cref="XElement"/> that has this 
        /// <see cref="XName"/>.
        /// </summary>
        /// <param name="name">
        /// The <see cref="XName"/> of the <see cref="XAttribute"/> to get.
        /// </param>
        /// <returns>
        /// The <see cref="XAttribute"/> with the <see cref="XName"/> passed in.  If there is no <see cref="XAttribute"/>
        /// with this <see cref="XName"/> then null is returned.
        /// </returns>
        public XAttribute Attribute(XName name)
        {
            XAttribute a = lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next;
                    if (a.name == name) return a;
                } while (a != lastAttr);
            }
            return null;
        }

        /// <overloads>
        /// Returns the <see cref="XAttribute"/> associated with this <see cref="XElement"/>.  Optionally
        /// an <see cref="XName"/> can be given to target a specific <see cref="XAttribute"/>(s).
        /// </overloads>
        /// <summary>
        /// Returns all of the <see cref="XAttribute"/>s associated with this <see cref="XElement"/>.
        /// <seealso cref="XContainer.Elements()"/>
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XAttribute"/> containing all of the <see cref="XAttribute"/>s
        /// associated with this <see cref="XElement"/>.
        /// </returns>
        public IEnumerable<XAttribute> Attributes()
        {
            return GetAttributes(null);
        }

        /// <summary>
        /// Returns the <see cref="XAttribute"/>(s) associated with this <see cref="XElement"/> that has the passed
        /// in <see cref="XName"/>.
        /// <seealso cref="XElement.Attributes()"/>
        /// </summary>
        /// <param name="name">
        /// The <see cref="XName"/> of the targeted <see cref="XAttribute"/>.
        /// </param>
        /// <returns>
        /// The <see cref="XAttribute"/>(s) with the matching 
        /// </returns>
        public IEnumerable<XAttribute> Attributes(XName name)
        {
            return name != null ? GetAttributes(name) : XAttribute.EmptySequence;
        }

        /// <summary>
        /// Get the self and descendant nodes for an <see cref="XElement"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<XNode> DescendantNodesAndSelf()
        {
            return GetDescendantNodes(true);
        }

        /// <overloads>
        /// Returns this <see cref="XElement"/> and all of it's descendants.  Overloads allow
        /// specification of a type of descendant to return, or a specific <see cref="XName"/>
        /// of a descendant <see cref="XElement"/> to match.
        /// </overloads>
        /// <summary>
        /// Returns this <see cref="XElement"/> and all of it's descendant <see cref="XElement"/>s
        /// as an <see cref="IEnumerable"/> of <see cref="XElement"/>.
        /// <seealso cref="XElement.DescendantsAndSelf()"/>
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing this <see cref="XElement"/>
        /// and all of it's descendants.
        /// </returns>
        public IEnumerable<XElement> DescendantsAndSelf()
        {
            return GetDescendants(null, true);
        }

        /// <summary>
        /// Returns the descendants of this <see cref="XElement"/> that have a matching <see cref="XName"/>
        /// to the one passed in, including, potentially, this <see cref="XElement"/>.
        /// <seealso cref="XElement.DescendantsAndSelf(XName)"/>
        /// </summary>
        /// <param name="name">
        /// The <see cref="XName"/> of the descendant <see cref="XElement"/> that is being targeted.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> of <see cref="XElement"/> containing all of the descendant
        /// <see cref="XElement"/>s that have this <see cref="XName"/>.
        /// </returns>
        public IEnumerable<XElement> DescendantsAndSelf(XName name)
        {
            return name != null ? GetDescendants(name, true) : XElement.EmptySequence;
        }

        /// <summary>
        /// Returns the default <see cref="XNamespace"/> of an <see cref="XElement"/> 
        /// </summary>
        public XNamespace GetDefaultNamespace()
        {
            string namespaceName = GetNamespaceOfPrefixInScope("xmlns", null);
            return namespaceName != null ? XNamespace.Get(namespaceName) : XNamespace.None;
        }

        /// <summary>
        /// Get the namespace associated with a particular prefix for this <see cref="XElement"/> 
        /// in its document context. 
        /// </summary>
        /// <param name="prefix">The namespace prefix to look up</param>
        /// <returns>An <see cref="XNamespace"/> for the namespace bound to the prefix</returns>
        public XNamespace GetNamespaceOfPrefix(string prefix)
        {
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));
            if (prefix.Length == 0) throw new ArgumentException(SR.Format(SR.Argument_InvalidPrefix, prefix));
            if (prefix == "xmlns") return XNamespace.Xmlns;
            string namespaceName = GetNamespaceOfPrefixInScope(prefix, null);
            if (namespaceName != null) return XNamespace.Get(namespaceName);
            if (prefix == "xml") return XNamespace.Xml;
            return null;
        }

        /// <summary>
        /// Get the prefix associated with a namespace for an element in its context.
        /// </summary>
        /// <param name="ns">The <see cref="XNamespace"/> for which to get a prefix</param>
        /// <returns>The namespace prefix string</returns>
        public string GetPrefixOfNamespace(XNamespace ns)
        {
            if (ns == null) throw new ArgumentNullException(nameof(ns));
            string namespaceName = ns.NamespaceName;
            bool hasInScopeNamespace = false;
            XElement e = this;
            do
            {
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    bool hasLocalNamespace = false;
                    do
                    {
                        a = a.next;
                        if (a.IsNamespaceDeclaration)
                        {
                            if (a.Value == namespaceName)
                            {
                                if (a.Name.NamespaceName.Length != 0 &&
                                    (!hasInScopeNamespace ||
                                     GetNamespaceOfPrefixInScope(a.Name.LocalName, e) == null))
                                {
                                    return a.Name.LocalName;
                                }
                            }
                            hasLocalNamespace = true;
                        }
                    }
                    while (a != e.lastAttr);
                    hasInScopeNamespace |= hasLocalNamespace;
                }
                e = e.parent as XElement;
            }
            while (e != null);
            if ((object)namespaceName == (object)XNamespace.xmlPrefixNamespace)
            {
                if (!hasInScopeNamespace || GetNamespaceOfPrefixInScope("xml", null) == null) return "xml";
            }
            else if ((object)namespaceName == (object)XNamespace.xmlnsPrefixNamespace)
            {
                return "xmlns";
            }
            return null;
        }

        /// <overloads>
        /// The Load method provides multiple strategies for creating a new 
        /// <see cref="XElement"/> and initializing it from a data source containing
        /// raw XML.  Load from a file (passing in a URI to the file), an
        /// <see cref="Stream"/>, a <see cref="TextReader"/>, or an
        /// <see cref="XmlReader"/>.  Note:  Use <see cref="XDocument.Parse(string)"/>
        /// to create an <see cref="XDocument"/> from a string containing XML.
        /// <seealso cref="XDocument.Load(string)" />
        /// <seealso cref="XElement.Parse(string)"/>
        /// </overloads>
        /// <summary>
        /// Create a new <see cref="XElement"/> based on the contents of the file 
        /// referenced by the URI parameter passed in.  Note: Use 
        /// <see cref="XElement.Parse(string)"/> to create an <see cref="XElement"/> from
        /// a string containing XML.
        /// <seealso cref="XmlReader.Create(string)"/>
        /// <seealso cref="XElement.Parse(string)"/>
        /// <seealso cref="XDocument.Parse(string)"/>
        /// </summary>
        /// <remarks>
        /// This method uses the <see cref="XmlReader.Create(string)"/> method to create
        /// an <see cref="XmlReader"/> to read the raw XML into the underlying
        /// XML tree.
        /// </remarks>
        /// <param name="uri">
        /// A URI string referencing the file to load into a new <see cref="XElement"/>.
        /// </param>
        /// <returns>
        /// An <see cref="XElement"/> initialized with the contents of the file referenced
        /// in the passed in uri parameter.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Back-compat with System.Xml.")]
        public static XElement Load(string uri)
        {
            return Load(uri, LoadOptions.None);
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> based on the contents of the file 
        /// referenced by the URI parameter passed in.  Optionally, whitespace can be preserved.  
        /// <see cref="XmlReader.Create(string)"/>
        /// <seealso cref="XDocument.Load(string, LoadOptions)"/> 
        /// </summary>
        /// <remarks>
        /// This method uses the <see cref="XmlReader.Create(string)"/> method to create
        /// an <see cref="XmlReader"/> to read the raw XML into an underlying
        /// XML tree. If LoadOptions.PreserveWhitespace is enabled then
        /// the <see cref="XmlReaderSettings"/> property <see cref="XmlReaderSettings.IgnoreWhitespace"/>
        /// is set to false.
        /// </remarks>
        /// <param name="uri">
        /// A string representing the URI of the file to be loaded into a new <see cref="XElement"/>.
        /// </param>
        /// <param name="options">
        /// A set of <see cref="LoadOptions"/>.
        /// </param>
        /// <returns>
        /// An <see cref="XElement"/> initialized with the contents of the file referenced
        /// in the passed uri parameter.  If LoadOptions.PreserveWhitespace is enabled then
        /// significant whitespace will be preserved.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Back-compat with System.Xml.")]
        public static XElement Load(string uri, LoadOptions options)
        {
            XmlReaderSettings rs = GetXmlReaderSettings(options);
            using (XmlReader r = XmlReader.Create(uri, rs))
            {
                return Load(r, options);
            }
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> and initialize its underlying XML tree using
        /// the passed <see cref="Stream"/> parameter.  
        /// </summary>
        /// <param name="stream">
        /// A <see cref="Stream"/> containing the raw XML to read into the newly
        /// created <see cref="XElement"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="XElement"/> containing the contents of the passed in
        /// <see cref="Stream"/>.
        /// </returns>
        public static XElement Load(Stream stream)
        {
            return Load(stream, LoadOptions.None);
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> and initialize its underlying XML tree using
        /// the passed <see cref="Stream"/> parameter.  Optionally whitespace handling
        /// can be preserved.
        /// </summary>
        /// <remarks>
        /// If LoadOptions.PreserveWhitespace is enabled then
        /// the <see cref="XmlReaderSettings"/> property <see cref="XmlReaderSettings.IgnoreWhitespace"/>
        /// is set to false.
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> containing the raw XML to read into the newly
        /// created <see cref="XElement"/>.
        /// </param>
        /// <param name="options">
        /// A set of <see cref="LoadOptions"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="XElement"/> containing the contents of the passed in
        /// <see cref="Stream"/>.
        /// </returns>
        public static XElement Load(Stream stream, LoadOptions options)
        {
            XmlReaderSettings rs = GetXmlReaderSettings(options);
            using (XmlReader r = XmlReader.Create(stream, rs))
            {
                return Load(r, options);
            }
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> and initialize its underlying XML tree using
        /// the passed <see cref="Stream"/> parameter.  Optionally whitespace handling
        /// can be preserved.
        /// </summary>
        /// <remarks>
        /// If LoadOptions.PreserveWhitespace is enabled then
        /// the <see cref="XmlReaderSettings"/> property <see cref="XmlReaderSettings.IgnoreWhitespace"/>
        /// is set to false.
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> containing the raw XML to read into the newly
        /// created <see cref="XElement"/>.
        /// </param>
        /// <param name="options">
        /// A set of <see cref="LoadOptions"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.</param>
        /// <returns>
        /// A new <see cref="XElement"/> containing the contents of the passed in
        /// <see cref="Stream"/>.
        /// </returns>
        public static async Task<XElement> LoadAsync(Stream stream, LoadOptions options, CancellationToken cancellationToken)
        {
            XmlReaderSettings rs = GetXmlReaderSettings(options);

            rs.Async = true;

            using (XmlReader r = XmlReader.Create(stream, rs))
            {
                return await LoadAsync(r, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> and initialize its underlying XML tree using
        /// the passed <see cref="TextReader"/> parameter.  
        /// </summary>
        /// <param name="textReader">
        /// A <see cref="TextReader"/> containing the raw XML to read into the newly
        /// created <see cref="XElement"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="XElement"/> containing the contents of the passed in
        /// <see cref="TextReader"/>.
        /// </returns>
        public static XElement Load(TextReader textReader)
        {
            return Load(textReader, LoadOptions.None);
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> and initialize its underlying XML tree using
        /// the passed <see cref="TextReader"/> parameter.  Optionally whitespace handling
        /// can be preserved.
        /// </summary>
        /// <remarks>
        /// If LoadOptions.PreserveWhitespace is enabled then
        /// the <see cref="XmlReaderSettings"/> property <see cref="XmlReaderSettings.IgnoreWhitespace"/>
        /// is set to false.
        /// </remarks>
        /// <param name="textReader">
        /// A <see cref="TextReader"/> containing the raw XML to read into the newly
        /// created <see cref="XElement"/>.
        /// </param>
        /// <param name="options">
        /// A set of <see cref="LoadOptions"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="XElement"/> containing the contents of the passed in
        /// <see cref="TextReader"/>.
        /// </returns>
        public static XElement Load(TextReader textReader, LoadOptions options)
        {
            XmlReaderSettings rs = GetXmlReaderSettings(options);
            using (XmlReader r = XmlReader.Create(textReader, rs))
            {
                return Load(r, options);
            }
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> and initialize its underlying XML tree using
        /// the passed <see cref="TextReader"/> parameter.  Optionally whitespace handling
        /// can be preserved.
        /// </summary>
        /// <remarks>
        /// If LoadOptions.PreserveWhitespace is enabled then
        /// the <see cref="XmlReaderSettings"/> property <see cref="XmlReaderSettings.IgnoreWhitespace"/>
        /// is set to false.
        /// </remarks>
        /// <param name="textReader">
        /// A <see cref="TextReader"/> containing the raw XML to read into the newly
        /// created <see cref="XElement"/>.
        /// </param>
        /// <param name="options">
        /// A set of <see cref="LoadOptions"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.</param>
        /// <returns>
        /// A new <see cref="XElement"/> containing the contents of the passed in
        /// <see cref="TextReader"/>.
        /// </returns>
        public static async Task<XElement> LoadAsync(TextReader textReader, LoadOptions options, CancellationToken cancellationToken)
        {
            XmlReaderSettings rs = GetXmlReaderSettings(options);

            rs.Async = true;

            using (XmlReader r = XmlReader.Create(textReader, rs))
            {
                return await LoadAsync(r, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> containing the contents of the
        /// passed in <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="reader">
        /// An <see cref="XmlReader"/> containing the XML to be read into the new
        /// <see cref="XElement"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="XElement"/> containing the contents of the passed
        /// in <see cref="XmlReader"/>.
        /// </returns>
        public static XElement Load(XmlReader reader)
        {
            return Load(reader, LoadOptions.None);
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> containing the contents of the
        /// passed in <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="reader">
        /// An <see cref="XmlReader"/> containing the XML to be read into the new
        /// <see cref="XElement"/>.
        /// </param>
        /// <param name="options">
        /// A set of <see cref="LoadOptions"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="XElement"/> containing the contents of the passed
        /// in <see cref="XmlReader"/>.
        /// </returns>
        public static XElement Load(XmlReader reader, LoadOptions options)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (reader.MoveToContent() != XmlNodeType.Element) throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ExpectedNodeType, XmlNodeType.Element, reader.NodeType));
            XElement e = new XElement(reader, options);
            reader.MoveToContent();
            if (!reader.EOF) throw new InvalidOperationException(SR.InvalidOperation_ExpectedEndOfFile);
            return e;
        }

        /// <summary>
        /// Create a new <see cref="XElement"/> containing the contents of the
        /// passed in <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="reader">
        /// An <see cref="XmlReader"/> containing the XML to be read into the new
        /// <see cref="XElement"/>.
        /// </param>
        /// <param name="options">
        /// A set of <see cref="LoadOptions"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.</param>
        /// <returns>
        /// A new <see cref="XElement"/> containing the contents of the passed
        /// in <see cref="XmlReader"/>.
        /// </returns>
        public static Task<XElement> LoadAsync(XmlReader reader, LoadOptions options, CancellationToken cancellationToken)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<XElement>(cancellationToken);
            return LoadAsyncInternal(reader, options, cancellationToken);
        }

        private static async Task<XElement> LoadAsyncInternal(XmlReader reader, LoadOptions options, CancellationToken cancellationToken)
        {
            if (await reader.MoveToContentAsync().ConfigureAwait(false) != XmlNodeType.Element) throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ExpectedNodeType, XmlNodeType.Element, reader.NodeType));

            XElement e = new XElement(new AsyncConstructionSentry());
            await e.ReadElementFromAsync(reader, options, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            await reader.MoveToContentAsync().ConfigureAwait(false);

            if (!reader.EOF) throw new InvalidOperationException(SR.InvalidOperation_ExpectedEndOfFile);
            return e;
        }

        /// <overloads>
        /// Parses a string containing XML into an <see cref="XElement"/>.  Optionally
        /// whitespace can be preserved.
        /// </overloads>
        /// <summary>
        /// Parses a string containing XML into an <see cref="XElement"/>.  
        /// </summary>
        /// <remarks>
        /// The XML must contain only one root node.
        /// </remarks>
        /// <param name="text">
        /// A string containing the XML to parse into an <see cref="XElement"/>.
        /// </param>
        /// <returns>
        /// An <see cref="XElement"/> created from the XML string passed in.
        /// </returns>
        public static XElement Parse(string text)
        {
            return Parse(text, LoadOptions.None);
        }

        /// <summary>
        /// Parses a string containing XML into an <see cref="XElement"/> and optionally
        /// preserves the Whitespace. See <see cref="XmlReaderSettings.IgnoreWhitespace"/>.
        /// </summary>
        /// <remarks>
        /// <list>
        /// <item>The XML must contain only one root node.</item>
        /// <item>
        /// If LoadOptions.PreserveWhitespace is enabled the underlying 
        /// <see cref="XmlReaderSettings"/>'
        /// property <see cref="XmlReaderSettings.IgnoreWhitespace"/> will be set to false.
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="text">
        /// A string containing the XML to parse into an <see cref="XElement"/>.
        /// </param>
        /// <param name="options">
        /// A set of <see cref="LoadOptions"/>.
        /// </param>
        /// <returns>
        /// An <see cref="XElement"/> created from the XML string passed in.
        /// </returns>
        public static XElement Parse(string text, LoadOptions options)
        {
            using (StringReader sr = new StringReader(text))
            {
                XmlReaderSettings rs = GetXmlReaderSettings(options);
                using (XmlReader r = XmlReader.Create(sr, rs))
                {
                    return Load(r, options);
                }
            }
        }

        /// <summary>
        /// Removes content and attributes from this <see cref="XElement"/>.
        /// <seealso cref="XElement.RemoveAttributes"/>
        /// <seealso cref="XContainer.RemoveNodes"/>
        /// </summary>
        public void RemoveAll()
        {
            RemoveAttributes();
            RemoveNodes();
        }

        /// <summary>
        /// Removes that attributes of this <see cref="XElement"/>.
        /// <seealso cref="XElement.RemoveAll"/>
        /// <seealso cref="XElement.RemoveAttributes"/>
        /// </summary>
        public void RemoveAttributes()
        {
            if (SkipNotify())
            {
                RemoveAttributesSkipNotify();
                return;
            }
            while (lastAttr != null)
            {
                XAttribute a = lastAttr.next;
                NotifyChanging(a, XObjectChangeEventArgs.Remove);
                if (lastAttr == null || a != lastAttr.next) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
                if (a != lastAttr)
                {
                    lastAttr.next = a.next;
                }
                else
                {
                    lastAttr = null;
                }
                a.parent = null;
                a.next = null;
                NotifyChanged(a, XObjectChangeEventArgs.Remove);
            }
        }

        /// <overloads>
        /// Replaces the child nodes and the attributes of this element with the
        /// specified content. The content can be simple content, a collection of
        /// content objects, a parameter list of content objects, or null.
        /// </overloads>
        /// <summary>
        /// Replaces the children nodes and the attributes of this element with the specified content.
        /// </summary>
        /// <param name="content">
        /// The content that will replace the child nodes and attributes of this element.
        /// </param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void ReplaceAll(object content)
        {
            content = GetContentSnapshot(content);
            RemoveAll();
            Add(content);
        }

        /// <summary>
        /// Replaces the children nodes and the attributes of this element with the specified content.
        /// </summary>
        /// <param name="content">
        /// A parameter list of content objects.
        /// </param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void ReplaceAll(params object[] content)
        {
            ReplaceAll((object)content);
        }

        /// <overloads>
        /// Replaces the attributes of this element with the specified content.
        /// The content can be simple content, a collection of
        /// content objects, a parameter list of content objects, or null.
        /// </overloads>
        /// <summary>
        /// Replaces the attributes of this element with the specified content.
        /// </summary>
        /// <param name="content">
        /// The content that will replace the attributes of this element.
        /// </param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void ReplaceAttributes(object content)
        {
            content = GetContentSnapshot(content);
            RemoveAttributes();
            Add(content);
        }

        /// <summary>
        /// Replaces the attributes of this element with the specified content.
        /// </summary>
        /// <param name="content">
        /// A parameter list of content objects.
        /// </param>
        /// <remarks>
        /// See XContainer.Add(object content) for details about the content that can be added
        /// using this method.
        /// </remarks>
        public void ReplaceAttributes(params object[] content)
        {
            ReplaceAttributes((object)content);
        }


        /// <summary>
        /// Output this <see cref="XElement"/> to the passed in <see cref="Stream"/>.
        /// </summary>
        /// <remarks>
        /// The format will be indented by default.  If you want
        /// no indenting then use the SaveOptions version of Save (see
        /// <see cref="XElement.Save(Stream, SaveOptions)"/>) enabling 
        /// SaveOptions.DisableFormatting.
        /// There is also an option SaveOptions.OmitDuplicateNamespaces for removing duplicate namespace declarations. 
        /// Or instead use the SaveOptions as an annotation on this node or its ancestors, then this method will use those options.
        /// </remarks>
        /// <param name="stream">
        /// The <see cref="Stream"/> to output this <see cref="XElement"/> to.
        /// </param>
        public void Save(Stream stream)
        {
            Save(stream, GetSaveOptionsFromAnnotations());
        }

        /// <summary>
        /// Output this <see cref="XElement"/> to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        /// The <see cref="Stream"/> to output the XML to.  
        /// </param>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the output is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        public void Save(Stream stream, SaveOptions options)
        {
            XmlWriterSettings ws = GetXmlWriterSettings(options);
            using (XmlWriter w = XmlWriter.Create(stream, ws))
            {
                Save(w);
            }
        }

        /// <summary>
        /// Output this <see cref="XElement"/> to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        /// The <see cref="Stream"/> to output the XML to.  
        /// </param>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the output is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public async Task SaveAsync(Stream stream, SaveOptions options, CancellationToken cancellationToken)
        {
            XmlWriterSettings ws = GetXmlWriterSettings(options);

            ws.Async = true;

            using (XmlWriter w = XmlWriter.Create(stream, ws))
            {
                await SaveAsync(w, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Output this <see cref="XElement"/> to the passed in <see cref="TextWriter"/>.
        /// </summary>
        /// <remarks>
        /// The format will be indented by default.  If you want
        /// no indenting then use the SaveOptions version of Save (see
        /// <see cref="XElement.Save(TextWriter, SaveOptions)"/>) enabling 
        /// SaveOptions.DisableFormatting.
        /// There is also an option SaveOptions.OmitDuplicateNamespaces for removing duplicate namespace declarations. 
        /// Or instead use the SaveOptions as an annotation on this node or its ancestors, then this method will use those options.
        /// </remarks>
        /// <param name="textWriter">
        /// The <see cref="TextWriter"/> to output this <see cref="XElement"/> to.
        /// </param>
        public void Save(TextWriter textWriter)
        {
            Save(textWriter, GetSaveOptionsFromAnnotations());
        }

        /// <summary>
        /// Output this <see cref="XElement"/> to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="textWriter">
        /// The <see cref="TextWriter"/> to output the XML to.  
        /// </param>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the output is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        public void Save(TextWriter textWriter, SaveOptions options)
        {
            XmlWriterSettings ws = GetXmlWriterSettings(options);
            using (XmlWriter w = XmlWriter.Create(textWriter, ws))
            {
                Save(w);
            }
        }

        /// <summary>
        /// Output this <see cref="XElement"/> to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="textWriter">
        /// The <see cref="TextWriter"/> to output the XML to.  
        /// </param>
        /// <param name="options">
        /// If SaveOptions.DisableFormatting is enabled the output is not indented.
        /// If SaveOptions.OmitDuplicateNamespaces is enabled duplicate namespace declarations will be removed.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public async Task SaveAsync(TextWriter textWriter, SaveOptions options, CancellationToken cancellationToken)
        {
            XmlWriterSettings ws = GetXmlWriterSettings(options);

            ws.Async = true;

            using (XmlWriter w = XmlWriter.Create(textWriter, ws))
            {
                await SaveAsync(w, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Output this <see cref="XElement"/> to an <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to output the XML to.
        /// </param>
        public void Save(XmlWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.WriteStartDocument();
            WriteTo(writer);
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Output this <see cref="XElement"/> to an <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to output the XML to.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public Task SaveAsync(XmlWriter writer, CancellationToken cancellationToken)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            return SaveAsyncInternal(writer, cancellationToken);
        }

        private async Task SaveAsyncInternal(XmlWriter writer, CancellationToken cancellationToken)
        {
            await writer.WriteStartDocumentAsync().ConfigureAwait(false);

            await WriteToAsync(writer, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteEndDocumentAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the value of an attribute. The value is assigned to the attribute with the given
        /// name. If no attribute with the given name exists, a new attribute is added. If the
        /// value is null, the attribute with the given name, if any, is deleted.
        /// <seealso cref="XAttribute.SetValue"/>
        /// <seealso cref="XElement.SetElementValue"/>
        /// <seealso cref="XElement.SetValue"/>
        /// </summary>
        /// <param name="name">
        /// The name of the attribute whose value to change.
        /// </param>
        /// <param name="value">
        /// The value to assign to the attribute. The attribute is deleted if the value is null.
        /// Otherwise, the value is converted to its string representation and assigned to the
        /// <see cref="Value"/> property of the attribute.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if the value is an instance of <see cref="XObject"/>.
        /// </exception>
        public void SetAttributeValue(XName name, object value)
        {
            XAttribute a = Attribute(name);
            if (value == null)
            {
                if (a != null) RemoveAttribute(a);
            }
            else
            {
                if (a != null)
                {
                    a.Value = GetStringValue(value);
                }
                else
                {
                    AppendAttribute(new XAttribute(name, value));
                }
            }
        }

        /// <summary>
        /// Sets the value of a child element. The value is assigned to the first child element
        /// with the given name. If no child element with the given name exists, a new child
        /// element is added. If the value is null, the first child element with the given name,
        /// if any, is deleted.
        /// <seealso cref="XAttribute.SetValue"/>
        /// <seealso cref="XElement.SetAttributeValue"/>
        /// <seealso cref="XElement.SetValue"/>
        /// </summary>
        /// <param name="name">
        /// The name of the child element whose value to change.
        /// </param>
        /// <param name="value">
        /// The value to assign to the child element. The child element is deleted if the value
        /// is null. Otherwise, the value is converted to its string representation and assigned
        /// to the <see cref="Value"/> property of the child element.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if the value is an instance of <see cref="XObject"/>.
        /// </exception>
        public void SetElementValue(XName name, object value)
        {
            XElement e = Element(name);
            if (value == null)
            {
                if (e != null) RemoveNode(e);
            }
            else
            {
                if (e != null)
                {
                    e.Value = GetStringValue(value);
                }
                else
                {
                    AddNode(new XElement(name, GetStringValue(value)));
                }
            }
        }

        /// <summary>
        /// Sets the value of this element.
        /// <seealso cref="XAttribute.SetValue"/>
        /// <seealso cref="XElement.SetAttributeValue"/>
        /// <seealso cref="XElement.SetElementValue"/>
        /// </summary>
        /// <param name="value">
        /// The value to assign to this element. The value is converted to its string representation
        /// and assigned to the <see cref="Value"/> property.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified value is null.
        /// </exception>
        public void SetValue(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Value = GetStringValue(value);
        }

        /// <summary>
        /// Write this <see cref="XElement"/> to the passed in <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to write this <see cref="XElement"/> to.
        /// </param>
        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            new ElementWriter(writer).WriteElement(this);
        }

        /// <summary>
        /// Write this <see cref="XElement"/> to the passed in <see cref="XmlTextWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlTextWriter"/> to write this <see cref="XElement"/> to.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            return new ElementWriter(writer).WriteElementAsync(this, cancellationToken);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// If the <see cref="XElement"/> is a subtree (an <see cref="XElement"/>
        /// that has <see cref="XElement"/> children.  The concatenated string
        /// value of all of the <see cref="XElement"/>'s text and descendants
        /// text is returned.
        /// </remarks>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to a string.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="string"/>.
        /// </returns>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator string (XElement element)
        {
            if (element == null) return null;
            return element.Value;
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="bool"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="bool"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="bool"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the element does not contain a valid boolean value.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified element is null.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator bool (XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToBoolean(element.Value.ToLowerInvariant());
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="bool"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="bool"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="bool"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the element does not contain a valid boolean value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator bool? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToBoolean(element.Value.ToLowerInvariant());
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="int"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="int"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="int"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the element does not contain a valid integer value.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified element is null.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator int (XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToInt32(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="int"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="int"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="int"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid integer value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator int? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToInt32(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="uint"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="uint"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="uint"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid unsigned integer value.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified element is null.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator uint (XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToUInt32(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="uint"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="uint"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="uint"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid unsigned integer value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator uint? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToUInt32(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="long"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="long"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="long"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the element does not contain a valid long integer value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specified element is null.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator long (XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToInt64(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="long"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="long"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="long"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid long integer value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator long? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToInt64(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="ulong"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="ulong"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="ulong"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid unsigned long integer value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specified element is null.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator ulong (XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToUInt64(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="ulong"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="ulong"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="ulong"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid unsigned long integer value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator ulong? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToUInt64(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="float"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="float"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="float"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid float value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specified element is null.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator float (XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToSingle(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="float"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="float"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="float"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid float value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator float? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToSingle(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="double"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="double"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="double"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid double value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specified element is null.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator double (XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToDouble(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="double"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="double"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="double"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid double value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator double? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToDouble(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="decimal"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="decimal"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="decimal"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid decimal value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specified element is null.
        /// </exception>        
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator decimal (XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToDecimal(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="decimal"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="decimal"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="decimal"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid decimal value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator decimal? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToDecimal(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="DateTime"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="DateTime"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid <see cref="DateTime"/> value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specified element is null.
        /// </exception>        
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator DateTime(XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return DateTime.Parse(element.Value, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="DateTime"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="DateTime"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="DateTime"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid <see cref="DateTime"/> value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator DateTime? (XElement element)
        {
            if (element == null) return null;
            return DateTime.Parse(element.Value, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="DateTimeOffset"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="DateTimeOffset"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid <see cref="DateTimeOffset"/> value.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specified element is null.
        /// </exception>        
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator DateTimeOffset(XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToDateTimeOffset(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="DateTimeOffset"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="DateTimeOffset"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="DateTimeOffset"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid <see cref="DateTimeOffset"/> value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator DateTimeOffset? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToDateTimeOffset(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="TimeSpan"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="TimeSpan"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid <see cref="TimeSpan"/> value.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified element is null.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator TimeSpan(XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToTimeSpan(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="TimeSpan"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="TimeSpan"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="TimeSpan"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid <see cref="TimeSpan"/> value.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator TimeSpan? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToTimeSpan(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to a <see cref="Guid"/>.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="Guid"/>.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="Guid"/>.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid guid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the specified element is null.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator Guid(XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return XmlConvert.ToGuid(element.Value);
        }

        /// <summary>
        /// Cast the value of this <see cref="XElement"/> to an <see cref="Guid"/>?.
        /// </summary>
        /// <param name="element">
        /// The <see cref="XElement"/> to cast to <see cref="Guid"/>?.
        /// </param>
        /// <returns>
        /// The content of this <see cref="XElement"/> as a <see cref="Guid"/>?.
        /// </returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the specified element does not contain a valid guid.
        /// </exception>
        [CLSCompliant(false)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "Operator marked with CLSCompliant(false).")]
        public static explicit operator Guid? (XElement element)
        {
            if (element == null) return null;
            return XmlConvert.ToGuid(element.Value);
        }

        /// <summary>
        /// This method is obsolete for the IXmlSerializable contract.
        /// </summary>
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates a <see cref="XElement"/> from its XML representation.
        /// </summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> stream from which the <see cref="XElement"/>
        /// is deserialized.
        /// </param>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (parent != null || annotations != null || content != null || lastAttr != null) throw new InvalidOperationException(SR.InvalidOperation_DeserializeInstance);
            if (reader.MoveToContent() != XmlNodeType.Element) throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ExpectedNodeType, XmlNodeType.Element, reader.NodeType));
            ReadElementFrom(reader, LoadOptions.None);
        }

        /// <summary>
        /// Converts a <see cref="XElement"/> into its XML representation.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> stream to which the <see cref="XElement"/>
        /// is serialized.
        /// </param>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            WriteTo(writer);
        }

        internal override void AddAttribute(XAttribute a)
        {
            if (Attribute(a.Name) != null) throw new InvalidOperationException(SR.InvalidOperation_DuplicateAttribute);
            if (a.parent != null) a = new XAttribute(a);
            AppendAttribute(a);
        }

        internal override void AddAttributeSkipNotify(XAttribute a)
        {
            if (Attribute(a.Name) != null) throw new InvalidOperationException(SR.InvalidOperation_DuplicateAttribute);
            if (a.parent != null) a = new XAttribute(a);
            AppendAttributeSkipNotify(a);
        }

        internal void AppendAttribute(XAttribute a)
        {
            bool notify = NotifyChanging(a, XObjectChangeEventArgs.Add);
            if (a.parent != null) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
            AppendAttributeSkipNotify(a);
            if (notify) NotifyChanged(a, XObjectChangeEventArgs.Add);
        }

        internal void AppendAttributeSkipNotify(XAttribute a)
        {
            a.parent = this;
            if (lastAttr == null)
            {
                a.next = a;
            }
            else
            {
                a.next = lastAttr.next;
                lastAttr.next = a;
            }
            lastAttr = a;
        }

        private bool AttributesEqual(XElement e)
        {
            XAttribute a1 = lastAttr;
            XAttribute a2 = e.lastAttr;
            if (a1 != null && a2 != null)
            {
                do
                {
                    a1 = a1.next;
                    a2 = a2.next;
                    if (a1.name != a2.name || a1.value != a2.value) return false;
                } while (a1 != lastAttr);
                return a2 == e.lastAttr;
            }
            return a1 == null && a2 == null;
        }

        internal override XNode CloneNode()
        {
            return new XElement(this);
        }

        internal override bool DeepEquals(XNode node)
        {
            XElement e = node as XElement;
            return e != null && name == e.name && ContentsEqual(e) && AttributesEqual(e);
        }

        private IEnumerable<XAttribute> GetAttributes(XName name)
        {
            XAttribute a = lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next;
                    if (name == null || a.name == name) yield return a;
                } while (a.parent == this && a != lastAttr);
            }
        }

        private string GetNamespaceOfPrefixInScope(string prefix, XElement outOfScope)
        {
            XElement e = this;
            while (e != outOfScope)
            {
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next;
                        if (a.IsNamespaceDeclaration && a.Name.LocalName == prefix) return a.Value;
                    }
                    while (a != e.lastAttr);
                }
                e = e.parent as XElement;
            }
            return null;
        }

        internal override int GetDeepHashCode()
        {
            int h = name.GetHashCode();
            h ^= ContentsHashCode();
            XAttribute a = lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next;
                    h ^= a.GetDeepHashCode();
                } while (a != lastAttr);
            }
            return h;
        }

        private void ReadElementFrom(XmlReader r, LoadOptions o)
        {
            ReadElementFromImpl(r, o);

            if (!r.IsEmptyElement)
            {
                r.Read();
                ReadContentFrom(r, o);
            }

            r.Read();
        }

        private async Task ReadElementFromAsync(XmlReader r, LoadOptions o, CancellationToken cancellationTokentoken)
        {
            ReadElementFromImpl(r, o);

            if (!r.IsEmptyElement)
            {
                cancellationTokentoken.ThrowIfCancellationRequested();
                await r.ReadAsync().ConfigureAwait(false);

                await ReadContentFromAsync(r, o, cancellationTokentoken).ConfigureAwait(false);
            }

            cancellationTokentoken.ThrowIfCancellationRequested();
            await r.ReadAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Shared implementation between ReadElementFrom / ReadElementFromAsync.
        /// </summary>
        private void ReadElementFromImpl(XmlReader r, LoadOptions o)
        {
            if(r.ReadState != ReadState.Interactive) throw new InvalidOperationException(SR.InvalidOperation_ExpectedInteractive);
            name = XNamespace.Get(r.NamespaceURI).GetName(r.LocalName);
            if ((o & LoadOptions.SetBaseUri) != 0)
            {
                string baseUri = r.BaseURI;
                if (!string.IsNullOrEmpty(baseUri))
                {
                    SetBaseUri(baseUri);
                }
            }
            IXmlLineInfo li = null;
            if ((o & LoadOptions.SetLineInfo) != 0)
            {
                li = r as IXmlLineInfo;
                if (li != null && li.HasLineInfo())
                {
                    SetLineInfo(li.LineNumber, li.LinePosition);
                }
            }
            if (r.MoveToFirstAttribute())
            {
                do
                {
                    XAttribute a = new XAttribute(XNamespace.Get(r.Prefix.Length == 0 ? string.Empty : r.NamespaceURI).GetName(r.LocalName), r.Value);
                    if (li != null && li.HasLineInfo())
                    {
                        a.SetLineInfo(li.LineNumber, li.LinePosition);
                    }
                    AppendAttributeSkipNotify(a);
                } while (r.MoveToNextAttribute());
                r.MoveToElement();
            }
        }

        internal void RemoveAttribute(XAttribute a)
        {
            bool notify = NotifyChanging(a, XObjectChangeEventArgs.Remove);
            if (a.parent != this) throw new InvalidOperationException(SR.InvalidOperation_ExternalCode);
            XAttribute p = lastAttr, n;
            while ((n = p.next) != a) p = n;
            if (p == a)
            {
                lastAttr = null;
            }
            else
            {
                if (lastAttr == a) lastAttr = p;
                p.next = a.next;
            }
            a.parent = null;
            a.next = null;
            if (notify) NotifyChanged(a, XObjectChangeEventArgs.Remove);
        }

        private void RemoveAttributesSkipNotify()
        {
            if (lastAttr != null)
            {
                XAttribute a = lastAttr;
                do
                {
                    XAttribute next = a.next;
                    a.parent = null;
                    a.next = null;
                    a = next;
                } while (a != lastAttr);
                lastAttr = null;
            }
        }

        internal void SetEndElementLineInfo(int lineNumber, int linePosition)
        {
            AddAnnotation(new LineInfoEndElementAnnotation(lineNumber, linePosition));
        }

        internal override void ValidateNode(XNode node, XNode previous)
        {
            if (node is XDocument) throw new ArgumentException(SR.Format(SR.Argument_AddNode, XmlNodeType.Document));
            if (node is XDocumentType) throw new ArgumentException(SR.Format(SR.Argument_AddNode, XmlNodeType.DocumentType));
        }
    }
}
