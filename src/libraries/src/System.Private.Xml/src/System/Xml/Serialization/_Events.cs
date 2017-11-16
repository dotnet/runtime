// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if XMLSERIALIZERGENERATOR
namespace Microsoft.XmlSerializer.Generator
#else
namespace System.Xml.Serialization
#endif
{
    using System.IO;
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Xml;

    /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlAttributeEventHandler"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>

#if XMLSERIALIZERGENERATOR
    internal delegate void XmlAttributeEventHandler(object sender, XmlAttributeEventArgs e);
#else
    public delegate void XmlAttributeEventHandler(object sender, XmlAttributeEventArgs e);
#endif

    /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlAttributeEventArgs"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
#if XMLSERIALIZERGENERATOR
    internal class XmlAttributeEventArgs : EventArgs
#else
    public class XmlAttributeEventArgs : EventArgs
#endif
    {
        private object _o;
        private XmlAttribute _attr;
        private string _qnames;
        private int _lineNumber;
        private int _linePosition;


        internal XmlAttributeEventArgs(XmlAttribute attr, int lineNumber, int linePosition, object o, string qnames)
        {
            _attr = attr;
            _o = o;
            _qnames = qnames;
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }


        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlAttributeEventArgs.ObjectBeingDeserialized"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object ObjectBeingDeserialized
        {
            get { return _o; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlAttributeEventArgs.Attr"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAttribute Attr
        {
            get { return _attr; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlAttributeEventArgs.LineNumber"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the current line number.
        ///    </para>
        /// </devdoc>
        public int LineNumber
        {
            get { return _lineNumber; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlAttributeEventArgs.LinePosition"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the current line position.
        ///    </para>
        /// </devdoc>
        public int LinePosition
        {
            get { return _linePosition; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlAttributeEventArgs.Attributes"]/*' />
        /// <devdoc>
        ///    <para>
        ///       List the qnames of attributes expected in the current context.
        ///    </para>
        /// </devdoc>
        public string ExpectedAttributes
        {
            get { return _qnames == null ? string.Empty : _qnames; }
        }
    }

    /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlElementEventHandler"]/*' />
#if XMLSERIALIZERGENERATOR
    internal delegate void XmlElementEventHandler(object sender, XmlElementEventArgs e);
#else
    public delegate void XmlElementEventHandler(object sender, XmlElementEventArgs e);
#endif

    /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlElementEventArgs"]/*' />
#if XMLSERIALIZERGENERATOR
    internal class XmlElementEventArgs : EventArgs
#else
    public class XmlElementEventArgs : EventArgs
#endif
    {
        private object _o;
        private XmlElement _elem;
        private string _qnames;
        private int _lineNumber;
        private int _linePosition;

        internal XmlElementEventArgs(XmlElement elem, int lineNumber, int linePosition, object o, string qnames)
        {
            _elem = elem;
            _o = o;
            _qnames = qnames;
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlElementEventArgs.ObjectBeingDeserialized"]/*' />
        public object ObjectBeingDeserialized
        {
            get { return _o; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlElementEventArgs.Attr"]/*' />
        public XmlElement Element
        {
            get { return _elem; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlElementEventArgs.LineNumber"]/*' />
        public int LineNumber
        {
            get { return _lineNumber; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlElementEventArgs.LinePosition"]/*' />
        public int LinePosition
        {
            get { return _linePosition; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlAttributeEventArgs.ExpectedElements"]/*' />
        /// <devdoc>
        ///    <para>
        ///       List of qnames of elements expected in the current context.
        ///    </para>
        /// </devdoc>
        public string ExpectedElements
        {
            get { return _qnames == null ? string.Empty : _qnames; }
        }
    }

    /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventHandler"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
#if XMLSERIALIZERGENERATOR
    internal delegate void XmlNodeEventHandler(object sender, XmlNodeEventArgs e);
#else
    public delegate void XmlNodeEventHandler(object sender, XmlNodeEventArgs e);
#endif


    /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventArgs"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
#if XMLSERIALIZERGENERATOR
    internal class XmlNodeEventArgs : EventArgs
#else
    public class XmlNodeEventArgs : EventArgs
#endif
    {
        private object _o;
        private XmlNode _xmlNode;
        private int _lineNumber;
        private int _linePosition;


        internal XmlNodeEventArgs(XmlNode xmlNode, int lineNumber, int linePosition, object o)
        {
            _o = o;
            _xmlNode = xmlNode;
            _lineNumber = lineNumber;
            _linePosition = linePosition;
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventArgs.ObjectBeingDeserialized"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object ObjectBeingDeserialized
        {
            get { return _o; }
        }


        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventArgs.NodeType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlNodeType NodeType
        {
            get { return _xmlNode.NodeType; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventArgs.Name"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Name
        {
            get { return _xmlNode.Name; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventArgs.LocalName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string LocalName
        {
            get { return _xmlNode.LocalName; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventArgs.NamespaceURI"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string NamespaceURI
        {
            get { return _xmlNode.NamespaceURI; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventArgs.Text"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Text
        {
            get { return _xmlNode.Value; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventArgs.LineNumber"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the current line number.
        ///    </para>
        /// </devdoc>
        public int LineNumber
        {
            get { return _lineNumber; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="XmlNodeEventArgs.LinePosition"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the current line position.
        ///    </para>
        /// </devdoc>
        public int LinePosition
        {
            get { return _linePosition; }
        }
    }

    /// <include file='doc\_Events.uex' path='docs/doc[@for="UnreferencedObjectEventHandler"]/*' />
#if XMLSERIALIZERGENERATOR
    internal delegate void UnreferencedObjectEventHandler(object sender, UnreferencedObjectEventArgs e);
#else
    public delegate void UnreferencedObjectEventHandler(object sender, UnreferencedObjectEventArgs e);
#endif

    /// <include file='doc\_Events.uex' path='docs/doc[@for="UnreferencedObjectEventArgs"]/*' />
#if XMLSERIALIZERGENERATOR
    internal class UnreferencedObjectEventArgs : EventArgs
#else
    public class UnreferencedObjectEventArgs : EventArgs
#endif
    {
        private object _o;
        private string _id;

        /// <include file='doc\_Events.uex' path='docs/doc[@for="UnreferencedObjectEventArgs.UnreferencedObjectEventArgs"]/*' />
        public UnreferencedObjectEventArgs(object o, string id)
        {
            _o = o;
            _id = id;
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="UnreferencedObjectEventArgs.UnreferencedObject"]/*' />
        public object UnreferencedObject
        {
            get { return _o; }
        }

        /// <include file='doc\_Events.uex' path='docs/doc[@for="UnreferencedObjectEventArgs.UnreferencedId"]/*' />
        public string UnreferencedId
        {
            get { return _id; }
        }
    }
}
