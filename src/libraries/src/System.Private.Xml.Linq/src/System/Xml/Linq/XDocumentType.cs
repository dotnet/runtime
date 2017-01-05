// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents an XML Document Type Definition (DTD).
    /// </summary>
    public class XDocumentType : XNode
    {
        private string _name;
        private string _publicId;
        private string _systemId;
        private string _internalSubset;

        /// <summary>
        /// Initializes an empty instance of the <see cref="XDocumentType"/> class.
        /// </summary>
        public XDocumentType(string name, string publicId, string systemId, string internalSubset)
        {
            _name = XmlConvert.VerifyName(name);
            _publicId = publicId;
            _systemId = systemId;
            _internalSubset = internalSubset;
        }

        /// <summary>
        /// Initializes an instance of the XDocumentType class
        /// from another XDocumentType object.
        /// </summary>
        /// <param name="other"><see cref="XDocumentType"/> object to copy from.</param>
        public XDocumentType(XDocumentType other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            _name = other._name;
            _publicId = other._publicId;
            _systemId = other._systemId;
            _internalSubset = other._internalSubset;
        }

        internal XDocumentType(XmlReader r)
        {
            _name = r.Name;
            _publicId = r.GetAttribute("PUBLIC");
            _systemId = r.GetAttribute("SYSTEM");
            _internalSubset = r.Value;
            r.Read();
        }

        /// <summary>
        /// Gets or sets the internal subset for this Document Type Definition (DTD).
        /// </summary>
        public string InternalSubset
        {
            get
            {
                return _internalSubset;
            }
            set
            {
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Value);
                _internalSubset = value;
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
                return _name;
            }
            set
            {
                value = XmlConvert.VerifyName(value);
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Name);
                _name = value;
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
                return _publicId;
            }
            set
            {
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Value);
                _publicId = value;
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
                return _systemId;
            }
            set
            {
                bool notify = NotifyChanging(this, XObjectChangeEventArgs.Value);
                _systemId = value;
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
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.WriteDocType(_name, _publicId, _systemId, _internalSubset);
        }

        /// <summary>
        /// Write this <see cref="XDocumentType"/> to the passed in <see cref="XmlWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to write this <see cref="XDocumentType"/> to.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            return writer.WriteDocTypeAsync(_name, _publicId, _systemId, _internalSubset);
        }

        internal override XNode CloneNode()
        {
            return new XDocumentType(this);
        }

        internal override bool DeepEquals(XNode node)
        {
            XDocumentType other = node as XDocumentType;
            return other != null && _name == other._name && _publicId == other._publicId &&
                _systemId == other.SystemId && _internalSubset == other._internalSubset;
        }

        internal override int GetDeepHashCode()
        {
            return _name.GetHashCode() ^
                (_publicId != null ? _publicId.GetHashCode() : 0) ^
                (_systemId != null ? _systemId.GetHashCode() : 0) ^
                (_internalSubset != null ? _internalSubset.GetHashCode() : 0);
        }
    }
}
