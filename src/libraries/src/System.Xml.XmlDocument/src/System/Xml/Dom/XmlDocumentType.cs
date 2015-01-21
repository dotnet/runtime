// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Xml
{
    // Contains information associated with the document type declaration.
    internal class XmlDocumentType : XmlLinkedNode
    {
        private readonly string _name;
        private readonly string _publicId;
        private readonly string _systemId;
        private readonly string _internalSubset;
        private XmlNamedNodeMap _entities;
        private XmlNamedNodeMap _notations;

        protected internal XmlDocumentType(string name, string publicId, string systemId, string internalSubset, XmlDocument doc) : base(doc)
        {
            _name = name;
            _publicId = publicId;
            _systemId = systemId;
            _internalSubset = internalSubset;
            Debug.Assert(doc != null);
            if (!doc.IsLoading)
            {
                doc.IsLoading = true;
                XmlLoader loader = new XmlLoader();
                doc.IsLoading = false;
            }
        }

        // Gets the name of the node.
        public override string Name
        {
            get { return _name; }
        }

        // Gets the name of the current node without the namespace prefix.
        public override string LocalName
        {
            get { return _name; }
        }

        // Gets the type of the current node.
        public override XmlNodeType NodeType
        {
            get { return XmlNodeType.DocumentType; }
        }

        // Creates a duplicate of this node.
        public override XmlNode CloneNode(bool deep)
        {
            Debug.Assert(OwnerDocument != null);
            return OwnerDocument.CreateDocumentType(_name, _publicId, _systemId, _internalSubset);
        }

        // 
        // Microsoft extensions
        //

        //  Gets a value indicating whether the node is read-only.
        public override bool IsReadOnly
        {
            get
            {
                return true;        // Make entities and notations readonly
            }
        }

        // Gets the collection of XmlEntity nodes declared in the document type declaration.
        public XmlNamedNodeMap Entities
        {
            get
            {
                if (_entities == null)
                    _entities = new XmlNamedNodeMap(this);

                return _entities;
            }
        }

        // Gets the collection of XmlNotation nodes present in the document type declaration.
        public XmlNamedNodeMap Notations
        {
            get
            {
                if (_notations == null)
                    _notations = new XmlNamedNodeMap(this);

                return _notations;
            }
        }

        //
        // DOM Level 2
        //

        // Gets the value of the public identifier on the DOCTYPE declaration.
        public string PublicId
        {
            get { return _publicId; }
        }

        // Gets the value of
        // the system identifier on the DOCTYPE declaration.
        public string SystemId
        {
            get { return _systemId; }
        }

        // Gets the entire value of the DTD internal subset
        // on the DOCTYPE declaration.
        public string InternalSubset
        {
            get { return _internalSubset; }
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w)
        {
            w.WriteDocType(_name, _publicId, _systemId, _internalSubset);
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w)
        {
            // Intentionally do nothing
        }
    }
}
