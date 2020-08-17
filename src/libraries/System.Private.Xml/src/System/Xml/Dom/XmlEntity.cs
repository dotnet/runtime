// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Xml
{
    // Represents a parsed or unparsed entity in the XML document.
    public class XmlEntity : XmlNode
    {
        private readonly string? _publicId;
        private readonly string? _systemId;
        private readonly string? _notationName;
        private readonly string _name;
        private string _baseURI = string.Empty;
        private XmlLinkedNode? _lastChild;
        private bool _childrenFoliating;

        internal XmlEntity(string name, string? strdata, string? publicId, string? systemId, string? notationName, XmlDocument doc) : base(doc)
        {
            _name = doc.NameTable.Add(name);
            _publicId = publicId;
            _systemId = systemId;
            _notationName = notationName;
            _childrenFoliating = false;
        }

        // Throws an exception since an entity can not be cloned.
        public override XmlNode CloneNode(bool deep)
        {
            throw new InvalidOperationException(SR.Xdom_Node_Cloning);
        }

        //
        // Microsoft extensions
        //

        // Gets a value indicating whether the node is read-only.
        public override bool IsReadOnly
        {
            get
            {
                return true;        // Make entities readonly
            }
        }


        // Gets the name of the node.
        public override string Name
        {
            get { return _name; }
        }

        // Gets the name of the node without the namespace prefix.
        public override string LocalName
        {
            get { return _name; }
        }

        // Gets the concatenated values of the entity node and all its children.
        // The property is read-only and when tried to be set, exception will be thrown.
        public override string InnerText
        {
            get { return base.InnerText; }
            set
            {
                throw new InvalidOperationException(SR.Xdom_Ent_Innertext);
            }
        }

        internal override bool IsContainer
        {
            get { return true; }
        }

        internal override XmlLinkedNode? LastNode
        {
            get
            {
                if (_lastChild == null && !_childrenFoliating)
                { //expand the unparsedreplacementstring
                    _childrenFoliating = true;
                    //wrap the replacement string with an element
                    XmlLoader loader = new XmlLoader();
                    loader.ExpandEntity(this);
                }
                return _lastChild;
            }
            set { _lastChild = value; }
        }

        internal override bool IsValidChildType(XmlNodeType type)
        {
            return (type == XmlNodeType.Text ||
                   type == XmlNodeType.Element ||
                   type == XmlNodeType.ProcessingInstruction ||
                   type == XmlNodeType.Comment ||
                   type == XmlNodeType.CDATA ||
                   type == XmlNodeType.Whitespace ||
                   type == XmlNodeType.SignificantWhitespace ||
                   type == XmlNodeType.EntityReference);
        }

        // Gets the type of the node.
        public override XmlNodeType NodeType
        {
            get { return XmlNodeType.Entity; }
        }

        // Gets the value of the public identifier on the entity declaration.
        public string? PublicId
        {
            get { return _publicId; }
        }

        // Gets the value of the system identifier on the entity declaration.
        public string? SystemId
        {
            get { return _systemId; }
        }

        // Gets the name of the optional NDATA attribute on the
        // entity declaration.
        public string? NotationName
        {
            get { return _notationName; }
        }

        //Without override these two functions, we can't guarantee that WriteTo()/WriteContent() functions will never be called
        public override string OuterXml
        {
            get { return string.Empty; }
        }

        public override string InnerXml
        {
            get { return string.Empty; }
            set { throw new InvalidOperationException(SR.Xdom_Set_InnerXml); }
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w)
        {
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w)
        {
        }

        public override string BaseURI
        {
            get { return _baseURI; }
        }

        internal void SetBaseURI(string inBaseURI)
        {
            _baseURI = inBaseURI;
        }
    }
}
