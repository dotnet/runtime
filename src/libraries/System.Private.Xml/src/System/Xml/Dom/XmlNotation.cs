// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System;
    using System.Diagnostics;

    // Contains a notation declared in the DTD or schema.
    public class XmlNotation : XmlNode
    {
        private string _publicId;
        private string _systemId;
        private string _name;

        internal XmlNotation(string name, string publicId, string systemId, XmlDocument doc) : base(doc)
        {
            _name = doc.NameTable.Add(name);
            _publicId = publicId;
            _systemId = systemId;
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
            get { return XmlNodeType.Notation; }
        }

        // Throws an InvalidOperationException since Notation can not be cloned.
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
                return true;        // Make notations readonly
            }
        }

        // Gets the value of the public identifier on the notation declaration.
        public string PublicId
        {
            get { return _publicId; }
        }

        // Gets the value of
        // the system identifier on the notation declaration.
        public string SystemId
        {
            get { return _systemId; }
        }

        // Without override these two functions, we can't guarantee that WriteTo()/WriteContent() functions will never be called
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
    }
}
