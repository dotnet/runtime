// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{ 
    using System.Diagnostics;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaObject.uex' path='docs/doc[@for="XmlSchemaObject"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public abstract class XmlSchemaObject
    {
        private int _lineNum = 0;
        private int _linePos = 0;
        private string _sourceUri;
        private XmlSerializerNamespaces _namespaces;
        private XmlSchemaObject _parent;

        //internal
        private bool _isProcessing; //Indicates whether this object is currently being processed

        /// <include file='doc\XmlSchemaObject.uex' path='docs/doc[@for="XmlSchemaObject.LineNum"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public int LineNumber
        {
            get { return _lineNum; }
            set { _lineNum = value; }
        }

        /// <include file='doc\XmlSchemaObject.uex' path='docs/doc[@for="XmlSchemaObject.LinePos"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public int LinePosition
        {
            get { return _linePos; }
            set { _linePos = value; }
        }

        /// <include file='doc\XmlSchemaObject.uex' path='docs/doc[@for="XmlSchemaObject.SourceUri"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlIgnore]
        public string SourceUri
        {
            get { return _sourceUri; }
            set { _sourceUri = value; }
        }

        /// <include file='doc\XmlSchemaObject.uex' path='docs/doc[@for="XmlSchemaObject.Parent"]/*' />
        [XmlIgnore]
        public XmlSchemaObject Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        /// <include file='doc\XmlSchemaObject.uex' path='docs/doc[@for="XmlSchemaObject.Namespaces"]/*' />
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces
        {
            get
            {
                if (_namespaces == null)
                    _namespaces = new XmlSerializerNamespaces();
                return _namespaces;
            }
            set { _namespaces = value; }
        }

        internal virtual void OnAdd(XmlSchemaObjectCollection container, object item) { }
        internal virtual void OnRemove(XmlSchemaObjectCollection container, object item) { }
        internal virtual void OnClear(XmlSchemaObjectCollection container) { }

        [XmlIgnore]
        internal virtual string IdAttribute
        {
            get { Debug.Assert(false); return null; }
            set { Debug.Assert(false); }
        }

        internal virtual void SetUnhandledAttributes(XmlAttribute[] moreAttributes) { }
        internal virtual void AddAnnotation(XmlSchemaAnnotation annotation) { }

        [XmlIgnore]
        internal virtual string NameAttribute
        {
            get { Debug.Assert(false); return null; }
            set { Debug.Assert(false); }
        }

        [XmlIgnore]
        internal bool IsProcessing
        {
            get
            {
                return _isProcessing;
            }
            set
            {
                _isProcessing = value;
            }
        }

        internal virtual XmlSchemaObject Clone()
        {
            return (XmlSchemaObject)MemberwiseClone();
        }
    }
}
