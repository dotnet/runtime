// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    internal class XmlUnspecifiedAttribute : XmlAttribute
    {
        private bool _fSpecified = false;


        protected internal XmlUnspecifiedAttribute(string prefix, string localName, string namespaceURI, XmlDocument doc)
        : base(prefix, localName, namespaceURI, doc)
        {
        }

        public override bool Specified
        {
            get { return _fSpecified; }
        }


        public override XmlNode CloneNode(bool deep)
        {
            //CloneNode is deep for attributes irrespective of parameter
            XmlDocument doc = OwnerDocument;
            XmlUnspecifiedAttribute attr = (XmlUnspecifiedAttribute)doc.CreateDefaultAttribute(Prefix, LocalName, NamespaceURI);
            attr.CopyChildren(doc, this, true);
            attr._fSpecified = true; //When clone, should return the specified attribute as default
            return attr;
        }

        public override string InnerText
        {
            set
            {
                base.InnerText = value;
                _fSpecified = true;
            }
        }

        public override XmlNode InsertBefore(XmlNode newChild, XmlNode refChild)
        {
            XmlNode node = base.InsertBefore(newChild, refChild);
            _fSpecified = true;
            return node;
        }

        public override XmlNode InsertAfter(XmlNode newChild, XmlNode refChild)
        {
            XmlNode node = base.InsertAfter(newChild, refChild);
            _fSpecified = true;
            return node;
        }

        public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
        {
            XmlNode node = base.ReplaceChild(newChild, oldChild);
            _fSpecified = true;
            return node;
        }

        public override XmlNode RemoveChild(XmlNode oldChild)
        {
            XmlNode node = base.RemoveChild(oldChild);
            _fSpecified = true;
            return node;
        }

        public override XmlNode AppendChild(XmlNode newChild)
        {
            XmlNode node = base.AppendChild(newChild);
            _fSpecified = true;
            return node;
        }

        public override void WriteTo(XmlWriter w)
        {
            if (_fSpecified)
                base.WriteTo(w);
        }

        internal void SetSpecified(bool f)
        {
            _fSpecified = f;
        }
    }
}
