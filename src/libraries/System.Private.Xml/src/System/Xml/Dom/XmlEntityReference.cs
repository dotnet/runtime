// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Xml
{
    // Represents an entity reference node.

    // <code>EntityReference</code> objects may be inserted into the structure
    // model when an entity reference is in the source document, or when the user
    // wishes to insert an entity reference. Note that  character references and
    // references to predefined entities are considered to be expanded by the
    // HTML or XML processor so that characters are represented by their Unicode
    // equivalent rather than by an entity reference. Moreover, the XML
    // processor may completely expand references to entities while building the
    // structure model, instead of providing <code>EntityReference</code>
    // objects. If it does provide such objects, then for a given
    // <code>EntityReference</code> node, it may be that there is no
    // <code>Entity</code> node representing the referenced entity; but if such
    // an <code>Entity</code> exists, then the child list of the
    // <code>EntityReference</code> node is the same as that of the
    // <code>Entity</code> node. As with the <code>Entity</code> node, all
    // descendants of the <code>EntityReference</code> are readonly.
    // <p>The resolution of the children of the <code>EntityReference</code> (the
    // replacement value of the referenced <code>Entity</code>) may be lazily
    // evaluated; actions by the user (such as calling the
    // <code>childNodes</code> method on the <code>EntityReference</code> node)
    // are assumed to trigger the evaluation.
    public class XmlEntityReference : XmlLinkedNode
    {
        private readonly string _name;
        private XmlLinkedNode? _lastChild;

        protected internal XmlEntityReference(string name, XmlDocument doc) : base(doc)
        {
            if (!doc.IsLoading)
            {
                if (name.StartsWith('#'))
                {
                    throw new ArgumentException(SR.Xdom_InvalidCharacter_EntityReference);
                }
            }

            _name = doc.NameTable.Add(name);
            doc.fEntRefNodesPresent = true;
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

        // Gets or sets the value of the node.
        public override string? Value
        {
            get
            {
                return null;
            }

            set
            {
                throw new InvalidOperationException(SR.Xdom_EntRef_SetVal);
            }
        }

        // Gets the type of the node.
        public override XmlNodeType NodeType
        {
            get { return XmlNodeType.EntityReference; }
        }

        // Creates a duplicate of this node.
        public override XmlNode CloneNode(bool deep)
        {
            Debug.Assert(OwnerDocument != null);
            XmlEntityReference eref = OwnerDocument.CreateEntityReference(_name);
            return eref;
        }

        //
        // Microsoft extensions
        //

        // Gets a value indicating whether the node is read-only.
        public override bool IsReadOnly
        {
            get
            {
                return true;        // Make entity references readonly
            }
        }

        internal override bool IsContainer
        {
            get { return true; }
        }

        internal override void SetParent(XmlNode? node)
        {
            base.SetParent(node);
            if (LastNode == null && node != null && node != OwnerDocument)
            {
                //first time insert the entity reference into the tree, we should expand its children now
                XmlLoader loader = new XmlLoader();
                loader.ExpandEntityReference(this);
            }
        }

        internal override void SetParentForLoad(XmlNode node)
        {
            this.SetParent(node);
        }

        internal override XmlLinkedNode? LastNode
        {
            get
            {
                return _lastChild;
            }
            set { _lastChild = value; }
        }

        internal override bool IsValidChildType(XmlNodeType type)
        {
            switch (type)
            {
                case XmlNodeType.Element:
                case XmlNodeType.Text:
                case XmlNodeType.EntityReference:
                case XmlNodeType.Comment:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.CDATA:
                    return true;

                default:
                    return false;
            }
        }

        // Saves the node to the specified XmlWriter.
        public override void WriteTo(XmlWriter w)
        {
            w.WriteEntityRef(_name);
        }

        // Saves all the children of the node to the specified XmlWriter.
        public override void WriteContentTo(XmlWriter w)
        {
            // -- eventually will the fix. commented out waiting for finalizing on the issue.
            foreach (XmlNode n in this)
            {
                n.WriteTo(w);
            } //still use the old code to generate the output
            /*
            foreach ( XmlNode n in this ) {
                if ( n.NodeType != XmlNodeType.EntityReference )
                n.WriteTo( w );
                else
                    n.WriteContentTo( w );
            }*/
        }

        public override string BaseURI
        {
            get
            {
                return OwnerDocument!.BaseURI;
            }
        }

        private static string ConstructBaseURI(string baseURI, string systemId)
        {
            if (baseURI == null)
                return systemId;
            int nCount = baseURI.LastIndexOf('/') + 1;
            string buf = baseURI;
            if (nCount > 0 && nCount < baseURI.Length)
                buf = baseURI.Substring(0, nCount);
            else if (nCount == 0)
                buf = $"{buf}\\";
            return (buf + systemId.Replace('\\', '/'));
        }

        //childrenBaseURI returns where the entity reference node's children come from
        internal string ChildBaseURI
        {
            get
            {
                //get the associate entity and return its baseUri
                XmlEntity? ent = OwnerDocument!.GetEntityNode(_name);
                if (ent != null)
                {
                    if (!string.IsNullOrEmpty(ent.SystemId))
                        return ConstructBaseURI(ent.BaseURI, ent.SystemId);
                    else
                        return ent.BaseURI;
                }

                return string.Empty;
            }
        }
    }
}
