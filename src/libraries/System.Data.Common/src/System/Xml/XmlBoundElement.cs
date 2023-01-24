// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

#pragma warning disable 618 // ignore obsolete warning about XmlDataDocument

namespace System.Xml
{
    internal enum ElementState
    {
        None,
        Defoliated,
        WeakFoliation,
        StrongFoliation,
        Foliating,
        Defoliating,
    }

    internal sealed class XmlBoundElement : XmlElement
    {
        private DataRow? _row;
        private ElementState _state;

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal XmlBoundElement(string prefix, string localName, string namespaceURI, XmlDocument doc) : base(prefix, localName, namespaceURI, doc)
        {
            _state = ElementState.None;
        }

        public override XmlAttributeCollection Attributes
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "This whole class is unsafe. Constructors are marked as such.")]
            get
            {
                AutoFoliate();
                return base.Attributes;
            }
        }

        public override bool HasAttributes => Attributes.Count > 0;

        public override XmlNode? FirstChild
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "This whole class is unsafe. Constructors are marked as such.")]
            get
            {
                AutoFoliate();
                return base.FirstChild;
            }
        }

        internal XmlNode? SafeFirstChild => base.FirstChild;

        public override XmlNode? LastChild
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "This whole class is unsafe. Constructors are marked as such.")]
            get
            {
                AutoFoliate();
                return base.LastChild;
            }
        }

        public override XmlNode? PreviousSibling
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "This whole class is unsafe. Constructors are marked as such.")]
            get
            {
                XmlNode? prev = base.PreviousSibling;
                if (prev == null)
                {
                    XmlBoundElement? parent = ParentNode as XmlBoundElement;
                    if (parent != null)
                    {
                        parent.AutoFoliate();
                        return base.PreviousSibling;
                    }
                }
                return prev;
            }
        }

        internal XmlNode? SafePreviousSibling => base.PreviousSibling;

        public override XmlNode? NextSibling
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "This whole class is unsafe. Constructors are marked as such.")]
            get
            {
                XmlNode? next = base.NextSibling;
                if (next == null)
                {
                    XmlBoundElement? parent = ParentNode as XmlBoundElement;
                    if (parent != null)
                    {
                        parent.AutoFoliate();
                        return base.NextSibling;
                    }
                }
                return next;
            }
        }

        internal XmlNode? SafeNextSibling => base.NextSibling;

        public override bool HasChildNodes
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "This whole class is unsafe. Constructors are marked as such.")]
            get
            {
                AutoFoliate();
                return base.HasChildNodes;
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override XmlNode? InsertBefore(XmlNode newChild, XmlNode? refChild)
        {
            AutoFoliate();
            return base.InsertBefore(newChild, refChild);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override XmlNode? InsertAfter(XmlNode newChild, XmlNode? refChild)
        {
            AutoFoliate();
            return base.InsertAfter(newChild, refChild);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
        {
            AutoFoliate();
            return base.ReplaceChild(newChild, oldChild);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override XmlNode? AppendChild(XmlNode newChild)
        {
            AutoFoliate();
            return base.AppendChild(newChild);
        }

        internal void RemoveAllChildren()
        {
            XmlNode? child = FirstChild;
            XmlNode? sibling;

            while (child != null)
            {
                sibling = child.NextSibling;
                RemoveChild(child);
                child = sibling;
            }
        }

        public override string InnerXml
        {
            get { return base.InnerXml; }
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "This whole class is unsafe. Constructors are marked as such.")]
            set
            {
                RemoveAllChildren();

                XmlDataDocument doc = (XmlDataDocument)OwnerDocument;

                bool bOrigIgnoreXmlEvents = doc.IgnoreXmlEvents;
                bool bOrigIgnoreDataSetEvents = doc.IgnoreDataSetEvents;

                doc.IgnoreXmlEvents = true;
                doc.IgnoreDataSetEvents = true;

                base.InnerXml = value;

                doc.SyncTree(this);

                doc.IgnoreDataSetEvents = bOrigIgnoreDataSetEvents;
                doc.IgnoreXmlEvents = bOrigIgnoreXmlEvents;
            }
        }

        internal DataRow? Row
        {
            get { return _row; }
            set { _row = value; }
        }

        internal bool IsFoliated
        {
            get
            {
                while (_state == ElementState.Foliating || _state == ElementState.Defoliating)
                {
                    Thread.Sleep(0);
                }

                //has to be sure that we are either foliated or defoliated when ask for IsFoliated.
                return _state != ElementState.Defoliated;
            }
        }

        internal ElementState ElementState
        {
            get { return _state; }
            set { _state = value; }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        internal void Foliate(ElementState newState)
        {
            XmlDataDocument doc = (XmlDataDocument)OwnerDocument;
            doc?.Foliate(this, newState);
        }

        // Foliate the node as a side effect of user calling functions on this node (like NextSibling) OR as a side effect of DataDocNav using nodes to do editing
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void AutoFoliate()
        {
            XmlDataDocument doc = (XmlDataDocument)OwnerDocument;
            doc?.Foliate(this, doc.AutoFoliationState);
        }

        public override XmlNode CloneNode(bool deep)
        {
            XmlDataDocument doc = (XmlDataDocument)(OwnerDocument);
            ElementState oldAutoFoliationState = doc.AutoFoliationState;
            doc.AutoFoliationState = ElementState.WeakFoliation;
            XmlElement element;
            try
            {
                Foliate(ElementState.WeakFoliation);
                element = (XmlElement)(base.CloneNode(deep));

                // Clone should create a XmlBoundElement node
                Debug.Assert(element is XmlBoundElement);
            }
            finally
            {
                doc.AutoFoliationState = oldAutoFoliationState;
            }

            return element;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override void WriteContentTo(XmlWriter w)
        {
            DataPointer dp = new DataPointer((XmlDataDocument)OwnerDocument, this);
            try
            {
                dp.AddPointer();
                WriteBoundElementContentTo(dp, w);
            }
            finally
            {
                dp.SetNoLongerUse();
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override void WriteTo(XmlWriter w)
        {
            DataPointer dp = new DataPointer((XmlDataDocument)OwnerDocument, this);
            try
            {
                dp.AddPointer();
                WriteRootBoundElementTo(dp, w);
            }
            finally
            {
                dp.SetNoLongerUse();
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private void WriteRootBoundElementTo(DataPointer dp, XmlWriter w)
        {
            Debug.Assert(dp.NodeType == XmlNodeType.Element);
            XmlDataDocument doc = (XmlDataDocument)OwnerDocument;
            w.WriteStartElement(dp.Prefix, dp.LocalName, dp.NamespaceURI);
            int cAttr = dp.AttributeCount;
            bool bHasXSI = false;
            if (cAttr > 0)
            {
                for (int iAttr = 0; iAttr < cAttr; iAttr++)
                {
                    dp.MoveToAttribute(iAttr);
                    if (dp.Prefix == "xmlns" && dp.LocalName == XmlDataDocument.XSI)
                    {
                        bHasXSI = true;
                    }

                    WriteTo(dp, w);
                    dp.MoveToOwnerElement();
                }
            }

            if (!bHasXSI && doc._bLoadFromDataSet && doc._bHasXSINIL)
            {
                w.WriteAttributeString("xmlns", "xsi", "http://www.w3.org/2000/xmlns/", Keywords.XSINS);
            }

            WriteBoundElementContentTo(dp, w);

            // Force long end tag when the elem is not empty, even if there are no children.
            if (dp.IsEmptyElement)
            {
                w.WriteEndElement();
            }
            else
            {
                w.WriteFullEndElement();
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private static void WriteBoundElementTo(DataPointer dp, XmlWriter w)
        {
            Debug.Assert(dp.NodeType == XmlNodeType.Element);
            w.WriteStartElement(dp.Prefix, dp.LocalName, dp.NamespaceURI);
            int cAttr = dp.AttributeCount;
            if (cAttr > 0)
            {
                for (int iAttr = 0; iAttr < cAttr; iAttr++)
                {
                    dp.MoveToAttribute(iAttr);
                    WriteTo(dp, w);
                    dp.MoveToOwnerElement();
                }
            }

            WriteBoundElementContentTo(dp, w);

            // Force long end tag when the elem is not empty, even if there are no children.
            if (dp.IsEmptyElement)
            {
                w.WriteEndElement();
            }
            else
            {
                w.WriteFullEndElement();
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private static void WriteBoundElementContentTo(DataPointer dp, XmlWriter w)
        {
            if (!dp.IsEmptyElement && dp.MoveToFirstChild())
            {
                do
                {
                    WriteTo(dp, w);
                }
                while (dp.MoveToNextSibling());

                dp.MoveToParent();
            }
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        private static void WriteTo(DataPointer dp, XmlWriter w)
        {
            switch (dp.NodeType)
            {
                case XmlNodeType.Attribute:
                    if (!dp.IsDefault)
                    {
                        w.WriteStartAttribute(dp.Prefix, dp.LocalName, dp.NamespaceURI);

                        if (dp.MoveToFirstChild())
                        {
                            do
                            {
                                WriteTo(dp, w);
                            }
                            while (dp.MoveToNextSibling());

                            dp.MoveToParent();
                        }

                        w.WriteEndAttribute();
                    }
                    break;

                case XmlNodeType.Element:
                    WriteBoundElementTo(dp, w);
                    break;

                case XmlNodeType.Text:
                    w.WriteString(dp.Value);
                    break;

                default:
                    Debug.Assert(((IXmlDataVirtualNode)dp).IsOnColumn(null));
                    dp.GetNode()?.WriteTo(w);
                    break;
            }
        }

        public override XmlNodeList GetElementsByTagName(string name)
        {
            // Retrieving nodes from the returned nodelist may cause foliation which causes new nodes to be created,
            // so the System.Xml iterator will throw if this happens during iteration. To avoid this, foliate everything
            // before iteration, so iteration will not cause foliation (and as a result of this, creation of new nodes).
            XmlNodeList tempNodeList = base.GetElementsByTagName(name);

            _ = tempNodeList.Count;
            return tempNodeList;
        }
    }
}
