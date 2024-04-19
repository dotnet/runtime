// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml.Schema;

namespace System.Xml
{
    internal sealed class XmlLoader
    {
        private XmlDocument? _doc;
        private XmlReader? _reader;
        private bool _preserveWhitespace;

        public XmlLoader()
        {
        }

        internal void Load(XmlDocument doc, XmlReader reader, bool preserveWhitespace)
        {
            _doc = doc;
            // perf: unwrap XmlTextReader if no one derived from it
            if (reader.GetType() == typeof(System.Xml.XmlTextReader))
            {
                _reader = ((XmlTextReader)reader).Impl;
            }
            else
            {
                _reader = reader;
            }
            _preserveWhitespace = preserveWhitespace;
            if (doc == null)
                throw new ArgumentException(SR.Xdom_Load_NoDocument);
            if (reader == null)
                throw new ArgumentException(SR.Xdom_Load_NoReader);
            doc.SetBaseURI(reader.BaseURI!);
            if (reader.Settings != null
                && reader.Settings.ValidationType == ValidationType.Schema)
            {
                doc.Schemas = reader.Settings.Schemas;
            }
            if (_reader.ReadState != ReadState.Interactive)
            {
                if (!_reader.Read())
                    return;
            }
            LoadDocSequence(doc);
        }

        //The function will start loading the document from where current XmlReader is pointing at.
        private void LoadDocSequence(XmlDocument parentDoc)
        {
            Debug.Assert(_reader != null);
            Debug.Assert(parentDoc != null);
            XmlNode? node;
            while ((node = LoadNode(true)) != null)
            {
                parentDoc.AppendChildForLoad(node, parentDoc);
                if (!_reader.Read())
                    return;
            }
        }

        internal XmlNode? ReadCurrentNode(XmlDocument doc, XmlReader reader)
        {
            _doc = doc;
            _reader = reader;
            // WS are optional only for loading (see XmlDocument.PreserveWhitespace)
            _preserveWhitespace = true;
            if (doc == null)
                throw new ArgumentException(SR.Xdom_Load_NoDocument);
            if (reader == null)
                throw new ArgumentException(SR.Xdom_Load_NoReader);

            if (reader.ReadState == ReadState.Initial)
            {
                reader.Read();
            }
            if (reader.ReadState == ReadState.Interactive)
            {
                XmlNode n = LoadNode(true)!;

                // Move to the next node
                if (n.NodeType != XmlNodeType.Attribute)
                    reader.Read();

                return n;
            }

            return null;
        }

        private XmlNode? LoadNode(bool skipOverWhitespace)
        {
            XmlReader r = _reader!;
            XmlNode? parent = null;
            XmlElement? element;
            IXmlSchemaInfo? schemaInfo;
            do
            {
                XmlNode? node;
                switch (r.NodeType)
                {
                    case XmlNodeType.Element:
                        bool fEmptyElement = r.IsEmptyElement;
                        element = _doc!.CreateElement(r.Prefix, r.LocalName, r.NamespaceURI);
                        element.IsEmpty = fEmptyElement;

                        if (r.MoveToFirstAttribute())
                        {
                            XmlAttributeCollection attributes = element.Attributes;
                            do
                            {
                                XmlAttribute attr = LoadAttributeNode();
                                attributes.Append(attr); // special case for load
                            }
                            while (r.MoveToNextAttribute());
                            r.MoveToElement();
                        }

                        // recursively load all children.
                        if (!fEmptyElement)
                        {
                            parent?.AppendChildForLoad(element, _doc);
                            parent = element;
                            continue;
                        }
                        else
                        {
                            schemaInfo = r.SchemaInfo;
                            if (schemaInfo != null)
                            {
                                element.XmlName = _doc.AddXmlName(element.Prefix, element.LocalName, element.NamespaceURI, schemaInfo);
                            }
                            node = element;
                            break;
                        }

                    case XmlNodeType.EndElement:
                        if (parent == null)
                        {
                            return null;
                        }
                        Debug.Assert(parent.NodeType == XmlNodeType.Element);
                        schemaInfo = r.SchemaInfo;
                        if (schemaInfo != null)
                        {
                            element = parent as XmlElement;
                            if (element != null)
                            {
                                element.XmlName = _doc!.AddXmlName(element.Prefix, element.LocalName, element.NamespaceURI, schemaInfo);
                            }
                        }
                        if (parent.ParentNode == null)
                        {
                            return parent;
                        }
                        parent = parent.ParentNode;
                        continue;

                    case XmlNodeType.EntityReference:
                        node = LoadEntityReferenceNode(false);
                        break;

                    case XmlNodeType.EndEntity:
                        Debug.Assert(parent == null);
                        return null;

                    case XmlNodeType.Attribute:
                        node = LoadAttributeNode();
                        break;

                    case XmlNodeType.Text:
                        node = _doc!.CreateTextNode(r.Value);
                        break;

                    case XmlNodeType.SignificantWhitespace:
                        node = _doc!.CreateSignificantWhitespace(r.Value);
                        break;

                    case XmlNodeType.Whitespace:
                        if (_preserveWhitespace)
                        {
                            node = _doc!.CreateWhitespace(r.Value);
                            break;
                        }
                        else if (parent == null && !skipOverWhitespace)
                        {
                            // if called from LoadEntityReferenceNode, just return null
                            return null;
                        }
                        else
                        {
                            continue;
                        }
                    case XmlNodeType.CDATA:
                        node = _doc!.CreateCDataSection(r.Value);
                        break;


                    case XmlNodeType.XmlDeclaration:
                        node = LoadDeclarationNode();
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        node = _doc!.CreateProcessingInstruction(r.Name, r.Value);
                        break;

                    case XmlNodeType.Comment:
                        node = _doc!.CreateComment(r.Value);
                        break;

                    case XmlNodeType.DocumentType:
                        node = LoadDocumentTypeNode();
                        break;

                    default:
                        throw UnexpectedNodeType(r.NodeType);
                }

                Debug.Assert(node != null);
                if (parent != null)
                {
                    parent.AppendChildForLoad(node, _doc!);
                }
                else
                {
                    return node;
                }
            }
            while (r.Read());

            // when the reader ended before full subtree is read, return whatever we have created so far
            if (parent != null)
            {
                while (parent.ParentNode != null)
                {
                    parent = parent.ParentNode;
                }
            }

            return parent;
        }

        private XmlAttribute LoadAttributeNode()
        {
            Debug.Assert(_reader!.NodeType == XmlNodeType.Attribute);

            XmlReader r = _reader;
            if (r.IsDefault)
            {
                return LoadDefaultAttribute();
            }

            XmlAttribute attr = _doc!.CreateAttribute(r.Prefix, r.LocalName, r.NamespaceURI);
            IXmlSchemaInfo? schemaInfo = r.SchemaInfo;
            if (schemaInfo != null)
            {
                attr.XmlName = _doc.AddAttrXmlName(attr.Prefix, attr.LocalName, attr.NamespaceURI, schemaInfo);
            }

            while (r.ReadAttributeValue())
            {
                XmlNode node;
                switch (r.NodeType)
                {
                    case XmlNodeType.Text:
                        node = _doc.CreateTextNode(r.Value);
                        break;
                    case XmlNodeType.EntityReference:
                        node = _doc.CreateEntityReference(r.LocalName);
                        if (r.CanResolveEntity)
                        {
                            r.ResolveEntity();
                            LoadAttributeValue(node, false);
                            // Code internally relies on the fact that an EntRef nodes has at least one child (even an empty text node). Ensure that this holds true,
                            // if the reader does not present any children for the ent-ref
                            if (node.FirstChild == null)
                            {
                                node.AppendChildForLoad(_doc.CreateTextNode(string.Empty), _doc);
                            }
                        }
                        break;
                    default:
                        throw UnexpectedNodeType(r.NodeType);
                }

                Debug.Assert(node != null);
                attr.AppendChildForLoad(node, _doc);
            }

            return attr;
        }

        private XmlAttribute LoadDefaultAttribute()
        {
            Debug.Assert(_reader!.IsDefault);

            XmlReader r = _reader;
            XmlAttribute attr = _doc!.CreateDefaultAttribute(r.Prefix, r.LocalName, r.NamespaceURI);
            IXmlSchemaInfo? schemaInfo = r.SchemaInfo;
            if (schemaInfo != null)
            {
                attr.XmlName = _doc.AddAttrXmlName(attr.Prefix, attr.LocalName, attr.NamespaceURI, schemaInfo);
            }

            LoadAttributeValue(attr, false);

            XmlUnspecifiedAttribute? defAttr = attr as XmlUnspecifiedAttribute;
            // If user overrides CreateDefaultAttribute, then attr will NOT be a XmlUnspecifiedAttribute instance.
            defAttr?.SetSpecified(false);

            return attr;
        }

        private void LoadAttributeValue(XmlNode parent, bool direct)
        {
            XmlReader r = _reader!;
            while (r.ReadAttributeValue())
            {
                XmlNode node;
                switch (r.NodeType)
                {
                    case XmlNodeType.Text:
                        node = direct ? new XmlText(r.Value, _doc!) : _doc!.CreateTextNode(r.Value);
                        break;
                    case XmlNodeType.EndEntity:
                        return;
                    case XmlNodeType.EntityReference:
                        node = direct ? new XmlEntityReference(_reader!.LocalName, _doc!) : _doc!.CreateEntityReference(_reader!.LocalName);
                        if (r.CanResolveEntity)
                        {
                            r.ResolveEntity();
                            LoadAttributeValue(node, direct);
                            // Code internally relies on the fact that an EntRef nodes has at least one child (even an empty text node). Ensure that this holds true,
                            // if the reader does not present any children for the ent-ref
                            if (node.FirstChild == null)
                            {
                                node.AppendChildForLoad(direct ? new XmlText(string.Empty) : _doc!.CreateTextNode(string.Empty), _doc!);
                            }
                        }
                        break;
                    default:
                        throw UnexpectedNodeType(r.NodeType);
                }
                Debug.Assert(node != null);
                parent.AppendChildForLoad(node, _doc!);
            }
            return;
        }

        private XmlEntityReference LoadEntityReferenceNode(bool direct)
        {
            Debug.Assert(_reader!.NodeType == XmlNodeType.EntityReference);
            XmlEntityReference eref = direct ? new XmlEntityReference(_reader.Name, _doc!) : _doc!.CreateEntityReference(_reader.Name);
            if (_reader.CanResolveEntity)
            {
                _reader.ResolveEntity();
                while (_reader.Read() && _reader.NodeType != XmlNodeType.EndEntity)
                {
                    XmlNode? node = direct ? LoadNodeDirect() : LoadNode(false);
                    if (node != null)
                    {
                        eref.AppendChildForLoad(node, _doc!);
                    }
                }
                // Code internally relies on the fact that an EntRef nodes has at least one child (even an empty text node). Ensure that this holds true,
                // if the reader does not present any children for the ent-ref
                if (eref.LastChild == null)
                    eref.AppendChildForLoad(_doc!.CreateTextNode(string.Empty), _doc);
            }
            return eref;
        }

        private XmlDeclaration LoadDeclarationNode()
        {
            Debug.Assert(_reader!.NodeType == XmlNodeType.XmlDeclaration);

            //parse data
            string? version = null;
            string? encoding = null;
            string? standalone = null;

            // Try first to use the reader to get the xml decl "attributes". Since not all readers are required to support this, it is possible to have
            // implementations that do nothing
            while (_reader.MoveToNextAttribute())
            {
                switch (_reader.Name)
                {
                    case "version":
                        version = _reader.Value;
                        break;
                    case "encoding":
                        encoding = _reader.Value;
                        break;
                    case "standalone":
                        standalone = _reader.Value;
                        break;
                    default:
                        Debug.Fail("Unknown reader name");
                        break;
                }
            }

            // For readers that do not break xml decl into attributes, we must parse the xml decl ourselves. We use version attr, b/c xml decl MUST contain
            // at least version attr, so if the reader implements them as attr, then version must be present
            if (version == null)
                ParseXmlDeclarationValue(_reader.Value, out version, out encoding, out standalone);

            return _doc!.CreateXmlDeclaration(version!, encoding, standalone);
        }

        private XmlDocumentType LoadDocumentTypeNode()
        {
            Debug.Assert(_reader!.NodeType == XmlNodeType.DocumentType);

            string? publicId = null;
            string? systemId = null;
            string internalSubset = _reader.Value;
            string localName = _reader.LocalName;
            while (_reader.MoveToNextAttribute())
            {
                switch (_reader.Name)
                {
                    case "PUBLIC":
                        publicId = _reader.Value;
                        break;
                    case "SYSTEM":
                        systemId = _reader.Value;
                        break;
                }
            }

            XmlDocumentType dtNode = _doc!.CreateDocumentType(localName, publicId, systemId, internalSubset);

            IDtdInfo? dtdInfo = _reader.DtdInfo;
            if (dtdInfo != null)
                LoadDocumentType(dtdInfo, dtNode);
            else
            {
                //construct our own XmlValidatingReader to parse the DocumentType node so we could get Entities and notations information
                ParseDocumentType(dtNode);
            }

            return dtNode;
        }

        // LoadNodeDirect does not use creator functions on XmlDocument. It is used loading nodes that are children of entity nodes,
        // because we do not want to let users extend these (if we would allow this, XmlDataDocument would have a problem, because
        // they do not know that those nodes should not be mapped). It can be also used for an optimized load path when if the
        // XmlDocument is not extended if XmlDocumentType and XmlDeclaration handling is added.
        private XmlNode? LoadNodeDirect()
        {
            XmlReader r = _reader!;
            XmlNode? parent = null;
            do
            {
                XmlNode? node;
                switch (r.NodeType)
                {
                    case XmlNodeType.Element:
                        bool fEmptyElement = _reader!.IsEmptyElement;
                        XmlElement element = new XmlElement(_reader.Prefix, _reader.LocalName, _reader.NamespaceURI, _doc!);
                        element.IsEmpty = fEmptyElement;

                        if (_reader.MoveToFirstAttribute())
                        {
                            XmlAttributeCollection attributes = element.Attributes;
                            do
                            {
                                XmlAttribute attr = LoadAttributeNodeDirect();
                                attributes.Append(attr); // special case for load
                            } while (r.MoveToNextAttribute());
                        }

                        // recursively load all children.
                        if (!fEmptyElement)
                        {
                            parent!.AppendChildForLoad(element, _doc!);
                            parent = element;
                            continue;
                        }
                        else
                        {
                            node = element;
                            break;
                        }

                    case XmlNodeType.EndElement:
                        Debug.Assert(parent!.NodeType == XmlNodeType.Element);
                        if (parent.ParentNode == null)
                        {
                            return parent;
                        }

                        parent = parent.ParentNode;
                        continue;

                    case XmlNodeType.EntityReference:
                        node = LoadEntityReferenceNode(true);
                        break;

                    case XmlNodeType.EndEntity:
                        continue;

                    case XmlNodeType.Attribute:
                        node = LoadAttributeNodeDirect();
                        break;

                    case XmlNodeType.SignificantWhitespace:
                        node = new XmlSignificantWhitespace(_reader!.Value, _doc!);
                        break;

                    case XmlNodeType.Whitespace:
                        if (_preserveWhitespace)
                        {
                            node = new XmlWhitespace(_reader!.Value, _doc!);
                        }
                        else
                        {
                            continue;
                        }
                        break;

                    case XmlNodeType.Text:
                        node = new XmlText(_reader!.Value, _doc!);
                        break;

                    case XmlNodeType.CDATA:
                        node = new XmlCDataSection(_reader!.Value, _doc!);
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        node = new XmlProcessingInstruction(_reader!.Name, _reader.Value, _doc!);
                        break;

                    case XmlNodeType.Comment:
                        node = new XmlComment(_reader!.Value, _doc!);
                        break;

                    default:
                        throw UnexpectedNodeType(_reader!.NodeType);
                }

                Debug.Assert(node != null);
                if (parent != null)
                {
                    parent.AppendChildForLoad(node, _doc!);
                }
                else
                {
                    return node;
                }
            }
            while (r.Read());

            return null;
        }

        private XmlAttribute LoadAttributeNodeDirect()
        {
            XmlReader r = _reader!;
            XmlAttribute attr;
            if (r.IsDefault)
            {
                XmlUnspecifiedAttribute defattr = new XmlUnspecifiedAttribute(r.Prefix, r.LocalName, r.NamespaceURI, _doc!);
                LoadAttributeValue(defattr, true);
                defattr.SetSpecified(false);
                return defattr;
            }
            else
            {
                attr = new XmlAttribute(r.Prefix, r.LocalName, r.NamespaceURI, _doc!);
                LoadAttributeValue(attr, true);
                return attr;
            }
        }

        internal void ParseDocumentType(XmlDocumentType dtNode)
        {
            XmlDocument doc = dtNode.OwnerDocument!;
            //if xmlresolver is set on doc, use that one, otherwise use the default one being created by xmlvalidatingreader
            if (doc.HasSetResolver)
                ParseDocumentType(dtNode, true, doc.GetResolver());
            else
                ParseDocumentType(dtNode, false, null);
        }

        private void ParseDocumentType(XmlDocumentType dtNode, bool bUseResolver, XmlResolver? resolver)
        {
            _doc = dtNode.OwnerDocument;
            XmlParserContext pc = new XmlParserContext(null, new XmlNamespaceManager(_doc!.NameTable), null, null, null, null, _doc.BaseURI, string.Empty, XmlSpace.None);
            XmlTextReaderImpl tr = new XmlTextReaderImpl("", XmlNodeType.Element, pc);
            tr.Namespaces = dtNode.ParseWithNamespaces;
            if (bUseResolver)
            {
                tr.XmlResolver = resolver;
            }

            IDtdParser dtdParser = DtdParser.Create();
            XmlTextReaderImpl.DtdParserProxy proxy = new XmlTextReaderImpl.DtdParserProxy(tr);

            IDtdInfo dtdInfo = dtdParser.ParseFreeFloatingDtd(_doc.BaseURI, dtNode.Name, dtNode.PublicId, dtNode.SystemId, dtNode.InternalSubset, proxy);
            LoadDocumentType(dtdInfo, dtNode);
        }

        private void LoadDocumentType(IDtdInfo dtdInfo, XmlDocumentType dtNode)
        {
            SchemaInfo? schInfo = dtdInfo as SchemaInfo;
            if (schInfo == null)
            {
                throw new XmlException(SR.Xml_InternalError, string.Empty);
            }

            dtNode.DtdSchemaInfo = schInfo;
            if (schInfo != null)
            {
                //set the schema information into the document
                _doc!.DtdSchemaInfo = schInfo;

                // Notation hashtable
                if (schInfo.Notations != null)
                {
                    foreach (SchemaNotation scNot in schInfo.Notations.Values)
                    {
                        dtNode.Notations.SetNamedItem(new XmlNotation(scNot.Name.Name, scNot.Pubid, scNot.SystemLiteral, _doc));
                    }
                }

                // Entity hashtables
                if (schInfo.GeneralEntities != null)
                {
                    foreach (SchemaEntity scEnt in schInfo.GeneralEntities.Values)
                    {
                        XmlEntity ent = new XmlEntity(scEnt.Name.Name, scEnt.Pubid, scEnt.Url, scEnt.NData.IsEmpty ? null : scEnt.NData.Name, _doc);
                        ent.SetBaseURI(scEnt.DeclaredURI);
                        dtNode.Entities.SetNamedItem(ent);
                    }
                }

                if (schInfo.ParameterEntities != null)
                {
                    foreach (SchemaEntity scEnt in schInfo.ParameterEntities.Values)
                    {
                        XmlEntity ent = new XmlEntity(scEnt.Name.Name, scEnt.Pubid, scEnt.Url, scEnt.NData.IsEmpty ? null : scEnt.NData.Name, _doc);
                        ent.SetBaseURI(scEnt.DeclaredURI);
                        dtNode.Entities.SetNamedItem(ent);
                    }
                }
                _doc.Entities = dtNode.Entities;

                //extract the elements which has attribute defined as ID from the element declarations
                foreach (KeyValuePair<XmlQualifiedName, SchemaElementDecl> elementDecls in schInfo.ElementDecls)
                {
                    SchemaElementDecl elementDecl = elementDecls.Value;
                    if (elementDecl.AttDefs != null)
                    {
                        foreach (KeyValuePair<XmlQualifiedName, SchemaAttDef> attDefs in elementDecl.AttDefs)
                        {
                            SchemaAttDef attdef = attDefs.Value;
                            if (attdef.Datatype.TokenizedType == XmlTokenizedType.ID)
                            {
                                //we only register the XmlElement based on their Prefix/LocalName and skip the namespace
                                _doc.AddIdInfo(
                                    _doc.AddXmlName(elementDecl.Prefix, elementDecl.Name.Name, string.Empty, null),
                                    _doc.AddAttrXmlName(attdef.Prefix, attdef.Name.Name, string.Empty, null));
                                break;
                            }
                        }
                    }
                }
            }
        }
#pragma warning restore 618

        private XmlParserContext GetContext(XmlNode? node)
        {
            string? lang = null;
            XmlSpace spaceMode = XmlSpace.None;
            XmlDocumentType? docType = _doc!.DocumentType;
            string baseURI = _doc.BaseURI;
            //constructing xmlnamespace
            HashSet<string> prefixes = new HashSet<string>();
            XmlNameTable nt = _doc.NameTable;
            XmlNamespaceManager mgr = new XmlNamespaceManager(nt);
            bool bHasDefXmlnsAttr = false;

            // Process all xmlns, xmlns:prefix, xml:space and xml:lang attributes
            while (node != null && node != _doc)
            {
                XmlElement? element = node as XmlElement;
                if (element != null && element.HasAttributes)
                {
                    mgr.PushScope();
                    foreach (XmlAttribute attr in element.Attributes)
                    {
                        if (attr.Prefix == _doc.strXmlns && !prefixes.Contains(attr.LocalName))
                        {
                            // Make sure the next time we will not add this prefix
                            prefixes.Add(attr.LocalName);
                            mgr.AddNamespace(attr.LocalName, attr.Value);
                        }
                        else if (!bHasDefXmlnsAttr && attr.Prefix.Length == 0 && attr.LocalName == _doc.strXmlns)
                        {
                            // Save the case xmlns="..." where xmlns is the LocalName
                            mgr.AddNamespace(string.Empty, attr.Value);
                            bHasDefXmlnsAttr = true;
                        }
                        else if (spaceMode == XmlSpace.None && attr.Prefix == _doc.strXml && attr.LocalName == _doc.strSpace)
                        {
                            // Save xml:space context
                            if (attr.Value == "default")
                                spaceMode = XmlSpace.Default;
                            else if (attr.Value == "preserve")
                                spaceMode = XmlSpace.Preserve;
                        }
                        else if (lang == null && attr.Prefix == _doc.strXml && attr.LocalName == _doc.strLang)
                        {
                            // Save xml:lag context
                            lang = attr.Value;
                        }
                    }
                }

                node = node.ParentNode;
            }
            return new XmlParserContext(
                nt,
                mgr,
                docType?.Name,
                docType?.PublicId,
                docType?.SystemId,
                docType?.InternalSubset,
                baseURI,
                lang,
                spaceMode
                );
        }



        internal XmlNamespaceManager ParsePartialContent(XmlNode parentNode, string innerxmltext, XmlNodeType nt)
        {
            //the function shouldn't be used to set innerxml for XmlDocument node
            Debug.Assert(parentNode.NodeType != XmlNodeType.Document);
            _doc = parentNode.OwnerDocument;
            Debug.Assert(_doc != null);
            XmlParserContext pc = GetContext(parentNode);
            _reader = CreateInnerXmlReader(innerxmltext, nt, pc, _doc);
            try
            {
                _preserveWhitespace = true;
                bool bOrigLoading = _doc.IsLoading;
                _doc.IsLoading = true;

                if (nt == XmlNodeType.Entity)
                {
                    XmlNode? node;
                    while (_reader.Read() && (node = LoadNodeDirect()) != null)
                    {
                        parentNode.AppendChildForLoad(node, _doc);
                    }
                }
                else
                {
                    XmlNode? node;
                    while (_reader.Read() && (node = LoadNode(true)) != null)
                    {
                        parentNode.AppendChildForLoad(node, _doc);
                    }
                }

                _doc.IsLoading = bOrigLoading;
            }
            finally
            {
                _reader.Close();
            }

            return pc.NamespaceManager!;
        }

        internal void LoadInnerXmlElement(XmlElement node, string innerxmltext)
        {
            //construct a tree underneath the node
            XmlNamespaceManager mgr = ParsePartialContent(node, innerxmltext, XmlNodeType.Element);
            //remove the duplicate namespace
            if (node.ChildNodes.Count > 0)
                RemoveDuplicateNamespace((XmlElement)node, mgr, false);
        }

        internal void LoadInnerXmlAttribute(XmlAttribute node, string innerxmltext)
        {
            ParsePartialContent(node, innerxmltext, XmlNodeType.Attribute);
        }


        private void RemoveDuplicateNamespace(XmlElement elem, XmlNamespaceManager mgr, bool fCheckElemAttrs)
        {
            //remove the duplicate attributes on current node first
            mgr.PushScope();
            XmlAttributeCollection attrs = elem.Attributes;
            int cAttrs = attrs.Count;
            if (fCheckElemAttrs && cAttrs > 0)
            {
                for (int i = cAttrs - 1; i >= 0; --i)
                {
                    XmlAttribute attr = attrs[i];
                    if (attr.Prefix == _doc!.strXmlns)
                    {
                        string? nsUri = mgr.LookupNamespace(attr.LocalName);
                        if (nsUri != null)
                        {
                            if (attr.Value == nsUri)
                                elem.Attributes.RemoveNodeAt(i);
                        }
                        else
                        {
                            // Add this namespace, so it we will behave correctly when setting "<bar xmlns:p="BAR"><foo2 xmlns:p="FOO"/></bar>" as
                            // InnerXml on this foo elem where foo is like this "<foo xmlns:p="FOO"></foo>"
                            // If do not do this, then we will remove the inner p prefix definition and will let the 1st p to be in scope for
                            // the subsequent InnerXml_set or setting an EntRef inside.
                            mgr.AddNamespace(attr.LocalName, attr.Value);
                        }
                    }
                    else if (attr.Prefix.Length == 0 && attr.LocalName == _doc.strXmlns)
                    {
                        string nsUri = mgr.DefaultNamespace;
                        if (nsUri != null)
                        {
                            if (attr.Value == nsUri)
                                elem.Attributes.RemoveNodeAt(i);
                        }
                        else
                        {
                            // Add this namespace, so it we will behave correctly when setting "<bar xmlns:p="BAR"><foo2 xmlns:p="FOO"/></bar>" as
                            // InnerXml on this foo elem where foo is like this "<foo xmlns:p="FOO"></foo>"
                            // If do not do this, then we will remove the inner p prefix definition and will let the 1st p to be in scope for
                            // the subsequent InnerXml_set or setting an EntRef inside.
                            mgr.AddNamespace(attr.LocalName, attr.Value);
                        }
                    }
                }
            }
            //now recursively remove the duplicate attributes on the children
            XmlNode? child = elem.FirstChild;
            while (child != null)
            {
                XmlElement? childElem = child as XmlElement;
                if (childElem != null)
                    RemoveDuplicateNamespace(childElem, mgr, true);
                child = child.NextSibling;
            }

            mgr.PopScope();
        }

        private static string EntitizeName(string name)
        {
            return $"&{name};";
        }

        //The function is called when expanding the entity when its children being asked
        internal void ExpandEntity(XmlEntity ent)
        {
            ParsePartialContent(ent, EntitizeName(ent.Name), XmlNodeType.Entity);
        }

        //The function is called when expanding the entity ref. ( inside XmlEntityReference.SetParent )
        internal void ExpandEntityReference(XmlEntityReference eref)
        {
            //when the ent ref is not associated w/ an entity, append an empty string text node as child
            _doc = eref.OwnerDocument;
            bool bOrigLoadingState = _doc!.IsLoading;
            _doc.IsLoading = true;
            switch (eref.Name)
            {
                case "lt":
                    eref.AppendChildForLoad(_doc.CreateTextNode("<"), _doc);
                    _doc.IsLoading = bOrigLoadingState;
                    return;
                case "gt":
                    eref.AppendChildForLoad(_doc.CreateTextNode(">"), _doc);
                    _doc.IsLoading = bOrigLoadingState;
                    return;
                case "amp":
                    eref.AppendChildForLoad(_doc.CreateTextNode("&"), _doc);
                    _doc.IsLoading = bOrigLoadingState;
                    return;
                case "apos":
                    eref.AppendChildForLoad(_doc.CreateTextNode("'"), _doc);
                    _doc.IsLoading = bOrigLoadingState;
                    return;
                case "quot":
                    eref.AppendChildForLoad(_doc.CreateTextNode("\""), _doc);
                    _doc.IsLoading = bOrigLoadingState;
                    return;
            }

            XmlNamedNodeMap entities = _doc.Entities;
            foreach (XmlEntity ent in entities)
            {
                if (Ref.Equal(ent.Name, eref.Name))
                {
                    ParsePartialContent(eref, EntitizeName(eref.Name), XmlNodeType.EntityReference);
                    return;
                }
            }
            //no fit so far
            if (!(_doc.ActualLoadingStatus))
            {
                eref.AppendChildForLoad(_doc.CreateTextNode(""), _doc);
                _doc.IsLoading = bOrigLoadingState;
            }
            else
            {
                _doc.IsLoading = bOrigLoadingState;
                throw new XmlException(SR.Xml_UndeclaredParEntity, eref.Name);
            }
        }

#pragma warning disable 618
        // Creates a XmlValidatingReader suitable for parsing InnerXml strings
        private static XmlTextReaderImpl CreateInnerXmlReader(string xmlFragment, XmlNodeType nt, XmlParserContext context, XmlDocument doc)
        {
            XmlNodeType contentNT = nt;
            if (contentNT == XmlNodeType.Entity || contentNT == XmlNodeType.EntityReference)
                contentNT = XmlNodeType.Element;

            XmlTextReaderImpl tr = new XmlTextReaderImpl(xmlFragment, contentNT, context);
            tr.XmlValidatingReaderCompatibilityMode = true;
            if (doc.HasSetResolver)
            {
                tr.XmlResolver = doc.GetResolver();
            }
            if (!(doc.ActualLoadingStatus))
            {
                tr.DisableUndeclaredEntityCheck = true;
            }
            Debug.Assert(tr.EntityHandling == EntityHandling.ExpandCharEntities);

            XmlDocumentType? dtdNode = doc.DocumentType;
            if (dtdNode != null)
            {
                tr.Namespaces = dtdNode.ParseWithNamespaces;
                if (dtdNode.DtdSchemaInfo != null)
                {
                    tr.SetDtdInfo(dtdNode.DtdSchemaInfo);
                }
                else
                {
                    IDtdParser dtdParser = DtdParser.Create();
                    XmlTextReaderImpl.DtdParserProxy proxy = new XmlTextReaderImpl.DtdParserProxy(tr);

                    IDtdInfo dtdInfo = dtdParser.ParseFreeFloatingDtd(context.BaseURI, context.DocTypeName, context.PublicId, context.SystemId, context.InternalSubset, proxy);

                    dtdNode.DtdSchemaInfo = dtdInfo as SchemaInfo;
                    tr.SetDtdInfo(dtdInfo);
                }
            }

            if (nt == XmlNodeType.Entity || nt == XmlNodeType.EntityReference)
            {
                tr.Read(); //this will skip the first element "wrapper"
                tr.ResolveEntity();
            }
            return tr;
        }
#pragma warning restore 618

        internal static void ParseXmlDeclarationValue(string strValue, out string? version, out string? encoding, out string? standalone)
        {
            version = null;
            encoding = null;
            standalone = null;
            XmlTextReaderImpl tempreader = new XmlTextReaderImpl(strValue, (XmlParserContext?)null);
            try
            {
                tempreader.Read();
                //get version info.
                if (tempreader.MoveToAttribute(nameof(version)))
                    version = tempreader.Value;
                //get encoding info
                if (tempreader.MoveToAttribute(nameof(encoding)))
                    encoding = tempreader.Value;
                //get standalone info
                if (tempreader.MoveToAttribute(nameof(standalone)))
                    standalone = tempreader.Value;
            }
            finally
            {
                tempreader.Close();
            }
        }

        internal static Exception UnexpectedNodeType(XmlNodeType nodetype)
        {
            return new InvalidOperationException(SR.Format(CultureInfo.InvariantCulture, SR.Xml_UnexpectedNodeType, nodetype.ToString()));
        }
    }
}
