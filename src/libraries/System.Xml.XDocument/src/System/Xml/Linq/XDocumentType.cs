// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents an XML Document Type Definition (DTD).
    /// </summary>
    public class XDocumentType : XNode
    {
        string name;
        string publicId;
        string systemId;
        string internalSubset;

        /// <summary>
        /// Initializes an empty instance of the <see cref="XDocumentType"/> class.
        /// </summary>
        public XDocumentType(string name, string publicId, string systemId, string internalSubset)
        {
            this.name = XmlConvert.VerifyName(name);
            this.publicId = publicId;
            this.systemId = systemId;
            this.internalSubset = internalSubset;
        }

        /// <summary>
        /// Initializes an instance of the XDocumentType class
        /// from another XDocumentType object.
        /// </summary>
        /// <param name="other"><see cref="XDocumentType"/> object to copy from.</param>
        public XDocumentType(XDocumentType other)
        {
            if (other == null) throw new ArgumentNullException("other");
            this.name = other.name;
            this.publicId = other.publicId;
            this.systemId = other.systemId;
            this.internalSubset = other.internalSubset;
        }

        internal XDocumentType(XmlReader r)
        {
            name = r.Name;
            publicId = r.GetAttribute("PUBLIC");
            systemId = r.GetAttribute("SYSTEM");
            internalSubset = r.Value;
            r.Read();
        }

        /// <summary>
        /// Gets or sets the internal subset for this Document Type Definition (DTD).
        /// </summary>
        public string InternalSubset
        {
            get
            {
                return internalSubset;
            }
            set
            {
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Value);
                internalSubset = value;
                if (notify) NotifyChanged(this, XObjectChangeEventArgs.Value);
            }
        }

        /// <summary>
        /// Gets or sets the name for this Document Type Definition (DTD).
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                value = XmlConvert.VerifyName(value);
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Name);
                name = value;
                if (notify) NotifyChanged(this, XObjectChangeEventArgs.Name);
            }
        }

        /// <summary>
        /// Gets the node type for this node.
        /// </summary>
        /// <remarks>
        /// This property will always return XmlNodeType.DocumentType.
        /// </remarks>
        public override XmlNodeType NodeType
        {
            get
            {
                return XmlNodeType.DocumentType;
            }
        }

        /// <summary>
        /// Gets or sets the public identifier for this Document Type Definition (DTD).
        /// </summary>
        public string PublicId
        {
            get
            {
                return publicId;
            }
            set
            {
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Value);
                publicId = value;
                if (notify) NotifyChanged(this, XObjectChangeEventArgs.Value);
            }
        }

        /// <summary>
        /// Gets or sets the system identifier for this Document Type Definition (DTD).
        /// </summary>
        public string SystemId
        {
            get
            {
                return systemId;
            }
            set
            {
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Value);
                systemId = value;
                if (notify) NotifyChanged(this, XObjectChangeEventArgs.Value);
            }
        }

        /// <summary>
        /// Write this <see cref="XDocumentType"/> to the passed in <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to write this <see cref="XDocumentType"/> to.
        /// </param>
        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            writer.WriteDocType(name, publicId, systemId, internalSubset);
        }

        internal override XNode CloneNode()
        {
            return new XDocumentType(this);
        }

        internal override bool DeepEquals(XNode node)
        {
            XDocumentType other = node as XDocumentType;
            return other != null && name == other.name && publicId == other.publicId &&
                systemId == other.SystemId && internalSubset == other.internalSubset;
        }

        internal override int GetDeepHashCode()
        {
            return name.GetHashCode() ^
                (publicId != null ? publicId.GetHashCode() : 0) ^
                (systemId != null ? systemId.GetHashCode() : 0) ^
                (internalSubset != null ? internalSubset.GetHashCode() : 0);
        }
    }
}
