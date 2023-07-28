// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Xml.Serialization;

namespace System.Xml.Schema
{
    public abstract class XmlSchemaObject
    {
        private int _lineNum;
        private int _linePos;
        private string? _sourceUri;
        private XmlSerializerNamespaces? _namespaces;
        private XmlSchemaObject? _parent;

        //internal
        private bool _isProcessing; //Indicates whether this object is currently being processed

        [XmlIgnore]
        public int LineNumber
        {
            get { return _lineNum; }
            set { _lineNum = value; }
        }

        [XmlIgnore]
        public int LinePosition
        {
            get { return _linePos; }
            set { _linePos = value; }
        }

        [XmlIgnore]
        public string? SourceUri
        {
            get { return _sourceUri; }
            set { _sourceUri = value; }
        }

        [XmlIgnore]
        public XmlSchemaObject? Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces
        {
            get => _namespaces ??= new XmlSerializerNamespaces();
            set => _namespaces = value;
        }

        internal virtual void OnAdd(XmlSchemaObjectCollection container, object? item) { }
        internal virtual void OnRemove(XmlSchemaObjectCollection container, object? item) { }
        internal virtual void OnClear(XmlSchemaObjectCollection container) { }

        [XmlIgnore]
        internal virtual string? IdAttribute
        {
            get { Debug.Fail("Should not use base property"); return null; }
            set { Debug.Fail("Should not use base property"); }
        }

        internal virtual void SetUnhandledAttributes(XmlAttribute[] moreAttributes) { }
        internal virtual void AddAnnotation(XmlSchemaAnnotation annotation) { }

        [XmlIgnore]
        internal virtual string? NameAttribute
        {
            get { Debug.Fail("Should not use base property"); return null; }
            set { Debug.Fail("Should not use base property"); }
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
