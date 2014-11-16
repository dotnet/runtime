// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Xml
{
    // Contains information associated with the document type declaration.
    internal class XmlDocumentType : XmlLinkedNode
    {
        string name;
        string publicId;
        string systemId;
        string internalSubset;
        XmlNamedNodeMap entities;
        XmlNamedNodeMap notations;

        protected internal XmlDocumentType(string name, string publicId, string systemId, string internalSubset, XmlDocument doc) : base(doc)
        {
            this.name = name;
            this.publicId = publicId;
            this.systemId = systemId;
            this.internalSubset = internalSubset;
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
            get { return name; }
        }

        // Gets the name of the current node without the namespace prefix.
        public override string LocalName
        {
            get { return name; }
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
            return OwnerDocument.CreateDocumentType(name, publicId, systemId, internalSubset);
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
                if (entities == null)
                    entities = new XmlNamedNodeMap(this);

                return entities;
            }
        }

        // Gets the collection of XmlNotation nodes present in the document type declaration.
        public XmlNamedNodeMap Notations
        {
            get
            {
                if (notations == null)
                    notations = new XmlNamedNodeMap(this);

                return notations;
            }
        }

        //
        // DOM Level 2
        //

        // Gets the value of the public identifier on the DOCTYPE declaration.
        public string PublicId
        {
            get { return publicId; }
        }

        // Gets the value of
        // the system identifier on the DOCTYPE declaration.
        public string SystemId
        {
            get { return systemId; }
        }

        // Gets the entire value of the DTD internal subset
        // on the DOCTYPE declaration.
        public string InternalSubset
        {
            get { return internalSubset; }
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w)
        {
            w.WriteDocType(name, publicId, systemId, internalSubset);
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w)
        {
            // Intentionally do nothing
        }
    }
}
