// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    // Represents a parsed or unparsed entity in the XML document.
    internal class XmlEntity : XmlNode
    {
        private string _publicId;
        private string _systemId;
        private String _notationName;
        private String _name;
        private String _unparsedReplacementStr;
        private String _baseURI;
        private XmlLinkedNode _lastChild;
        private bool _childrenFoliating;

        internal XmlEntity(String name, String strdata, string publicId, string systemId, String notationName, XmlDocument doc) : base(doc)
        {
            _name = doc.NameTable.Add(name);
            _publicId = publicId;
            _systemId = systemId;
            _notationName = notationName;
            _unparsedReplacementStr = strdata;
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

        internal override XmlLinkedNode LastNode
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

        // Gets the value of the system identifier on the entity declaration.
        public String SystemId
        {
            get { return _systemId; }
        }

        //Without override these two functions, we can't guarantee that WriteTo()/WriteContent() functions will never be called
        public override String OuterXml
        {
            get { return String.Empty; }
        }

        public override String InnerXml
        {
            get { return String.Empty; }
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

        public override String BaseURI
        {
            get { return _baseURI; }
        }

        internal void SetBaseURI(String inBaseURI)
        {
            _baseURI = inBaseURI;
        }
    }
}
