// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml
{
    internal sealed class DocumentSchemaValidator : IXmlNamespaceResolver
    {
        private XmlSchemaValidator? _validator;
        private readonly XmlSchemaSet _schemas;

        private readonly XmlNamespaceManager _nsManager;
        private readonly XmlNameTable _nameTable;

        //Attributes
        private ArrayList? _defaultAttributes;
        private readonly XmlValueGetter _nodeValueGetter;
        private XmlSchemaInfo? _attributeSchemaInfo;

        //Element PSVI
        private XmlSchemaInfo? _schemaInfo;

        //Event Handler
        private readonly ValidationEventHandler? _eventHandler;
        private readonly ValidationEventHandler _internalEventHandler;

        //Store nodes
        private XmlNode? _startNode;
        private XmlNode? _currentNode;
        private readonly XmlDocument _document;

        //List of nodes for partial validation tree walk
        private XmlNode?[]? _nodeSequenceToValidate;
        private bool _isPartialTreeValid;

        private bool _psviAugmentation;
        private bool _isValid;

        //To avoid SchemaNames creation
        private readonly string _nsXmlNs;
        private readonly string _nsXsi;
        private readonly string _xsiType;
        private readonly string _xsiNil;

        public DocumentSchemaValidator(XmlDocument ownerDocument, XmlSchemaSet schemas, ValidationEventHandler? eventHandler)
        {
            _schemas = schemas;
            _eventHandler = eventHandler;
            _document = ownerDocument;
            _internalEventHandler = new ValidationEventHandler(InternalValidationCallBack);

            _nameTable = _document.NameTable;
            _nsManager = new XmlNamespaceManager(_nameTable);

            Debug.Assert(schemas != null && schemas.Count > 0);

            _nodeValueGetter = new XmlValueGetter(GetNodeValue);
            _psviAugmentation = true;

            //Add common strings to be compared to NameTable
            _nsXmlNs = _nameTable.Add(XmlReservedNs.NsXmlNs);
            _nsXsi = _nameTable.Add(XmlReservedNs.NsXsi);
            _xsiType = _nameTable.Add("type");
            _xsiNil = _nameTable.Add("nil");
        }

        public bool PsviAugmentation
        {
            get { return _psviAugmentation; }
            set { _psviAugmentation = value; }
        }

        public bool Validate(XmlNode nodeToValidate)
        {
            XmlSchemaObject? partialValidationType = null;
            XmlSchemaValidationFlags validationFlags = XmlSchemaValidationFlags.AllowXmlAttributes;
            Debug.Assert(nodeToValidate.SchemaInfo != null);

            _startNode = nodeToValidate;
            switch (nodeToValidate.NodeType)
            {
                case XmlNodeType.Document:
                    validationFlags |= XmlSchemaValidationFlags.ProcessIdentityConstraints;
                    break;

                case XmlNodeType.DocumentFragment:
                    break;

                case XmlNodeType.Element: //Validate children of this element
                    IXmlSchemaInfo schemaInfo = nodeToValidate.SchemaInfo;
                    XmlSchemaElement? schemaElement = schemaInfo.SchemaElement;
                    if (schemaElement != null)
                    {
                        if (!schemaElement.RefName.IsEmpty)
                        { //If it is element ref,
                            partialValidationType = _schemas.GlobalElements[schemaElement.QualifiedName]; //Get Global element with correct Nillable, Default etc
                        }
                        else
                        { //local element
                            partialValidationType = schemaElement;
                        }
                        //Verify that if there was xsi:type, the schemaElement returned has the correct type set
                        Debug.Assert(schemaElement.ElementSchemaType == schemaInfo.SchemaType);
                    }
                    else
                    { //Can be an element that matched xs:any and had xsi:type
                        partialValidationType = schemaInfo.SchemaType;

                        if (partialValidationType == null)
                        { //Validated against xs:any with pc= lax or skip or undeclared / not validated element
                            if (nodeToValidate.ParentNode!.NodeType == XmlNodeType.Document)
                            {
                                //If this is the documentElement and it has not been validated at all
                                nodeToValidate = nodeToValidate.ParentNode;
                            }
                            else
                            {
                                partialValidationType = FindSchemaInfo((nodeToValidate as XmlElement)!);
                                if (partialValidationType == null)
                                {
                                    throw new XmlSchemaValidationException(SR.XmlDocument_NoNodeSchemaInfo, null, nodeToValidate);
                                }
                            }
                        }
                    }
                    break;

                case XmlNodeType.Attribute:
                    if (nodeToValidate.XPNodeType == XPathNodeType.Namespace) goto default;
                    partialValidationType = nodeToValidate.SchemaInfo.SchemaAttribute;
                    if (partialValidationType == null)
                    { //Validated against xs:anyAttribute with pc = lax or skip / undeclared attribute
                        partialValidationType = FindSchemaInfo((nodeToValidate as XmlAttribute)!);
                        if (partialValidationType == null)
                        {
                            throw new XmlSchemaValidationException(SR.XmlDocument_NoNodeSchemaInfo, null, nodeToValidate);
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException(SR.Format(SR.XmlDocument_ValidateInvalidNodeType, null));
            }
            _isValid = true;
            CreateValidator(partialValidationType, validationFlags);
            if (_psviAugmentation)
            {
                //Might have created it during FindSchemaInfo
                _schemaInfo ??= new XmlSchemaInfo();
                _attributeSchemaInfo = new XmlSchemaInfo();
            }
            ValidateNode(nodeToValidate);
            _validator!.EndValidation();
            return _isValid;
        }

        public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            IDictionary<string, string> dictionary = _nsManager.GetNamespacesInScope(scope);
            if (scope != XmlNamespaceScope.Local)
            {
                XmlNode? node = _startNode;
                while (node != null)
                {
                    switch (node.NodeType)
                    {
                        case XmlNodeType.Element:
                            XmlElement elem = (XmlElement)node;
                            if (elem.HasAttributes)
                            {
                                XmlAttributeCollection attrs = elem.Attributes;
                                for (int i = 0; i < attrs.Count; i++)
                                {
                                    XmlAttribute attr = attrs[i];
                                    if (Ref.Equal(attr.NamespaceURI, _document.strReservedXmlns))
                                    {
                                        if (attr.Prefix.Length == 0)
                                        {
                                            // xmlns='' declaration
                                            if (!dictionary.ContainsKey(string.Empty))
                                            {
                                                dictionary.Add(string.Empty, attr.Value);
                                            }
                                        }
                                        else
                                        {
                                            // xmlns:prefix='' declaration
                                            if (!dictionary.ContainsKey(attr.LocalName))
                                            {
                                                dictionary.Add(attr.LocalName, attr.Value);
                                            }
                                        }
                                    }
                                }
                            }
                            node = node.ParentNode;
                            break;
                        case XmlNodeType.Attribute:
                            node = ((XmlAttribute)node).OwnerElement;
                            break;
                        default:
                            node = node.ParentNode;
                            break;
                    }
                }
            }
            return dictionary;
        }

        public string? LookupNamespace(string prefix) =>
            _nsManager.LookupNamespace(prefix) ??
            _startNode!.GetNamespaceOfPrefixStrict(prefix);

        public string? LookupPrefix(string namespaceName) =>
            _nsManager.LookupPrefix(namespaceName) ??
            _startNode!.GetPrefixOfNamespaceStrict(namespaceName);

        private IXmlNamespaceResolver NamespaceResolver
        {
            get
            {
                if ((object?)_startNode == (object?)_document)
                {
                    return _nsManager;
                }

                return this;
            }
        }

        private void CreateValidator(XmlSchemaObject? partialValidationType, XmlSchemaValidationFlags validationFlags)
        {
            _validator = new XmlSchemaValidator(_nameTable, _schemas, NamespaceResolver, validationFlags);
            _validator.SourceUri = XmlConvert.ToUri(_document.BaseURI);
            _validator.XmlResolver = null;
            _validator.ValidationEventHandler += _internalEventHandler;
            _validator.ValidationEventSender = this;

            if (partialValidationType != null)
            {
                _validator.Initialize(partialValidationType);
            }
            else
            {
                _validator.Initialize();
            }
        }

        private void ValidateNode(XmlNode node)
        {
            _currentNode = node;
            switch (_currentNode.NodeType)
            {
                case XmlNodeType.Document:
                    XmlElement? docElem = ((XmlDocument)node).DocumentElement;
                    if (docElem == null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Xml_InvalidXmlDocument, SR.Xdom_NoRootEle));
                    }

                    ValidateNode(docElem);
                    break;

                case XmlNodeType.DocumentFragment:
                case XmlNodeType.EntityReference:
                    for (XmlNode? child = node.FirstChild; child != null; child = child.NextSibling)
                    {
                        ValidateNode(child);
                    }

                    break;

                case XmlNodeType.Element:
                    ValidateElement();
                    break;

                case XmlNodeType.Attribute: //Top-level attribute
                    XmlAttribute attr = (_currentNode as XmlAttribute)!;
                    _validator!.ValidateAttribute(attr.LocalName, attr.NamespaceURI, _nodeValueGetter, _attributeSchemaInfo);
                    if (_psviAugmentation)
                    {
                        attr.XmlName = _document.AddAttrXmlName(attr.Prefix, attr.LocalName, attr.NamespaceURI, _attributeSchemaInfo);
                    }
                    break;

                case XmlNodeType.Text:
                    _validator!.ValidateText(_nodeValueGetter);
                    break;

                case XmlNodeType.CDATA:
                    _validator!.ValidateText(_nodeValueGetter);
                    break;

                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    _validator!.ValidateWhitespace(_nodeValueGetter);
                    break;

                case XmlNodeType.Comment:
                case XmlNodeType.ProcessingInstruction:
                    break;

                default:
                    throw new InvalidOperationException(SR.Format(SR.Xml_UnexpectedNodeType, _currentNode.NodeType));
            }
        }

        private void ValidateElement()
        {
            _nsManager.PushScope();
            XmlElement? elementNode = _currentNode as XmlElement;
            Debug.Assert(elementNode != null);

            XmlAttributeCollection attributes = elementNode.Attributes;
            XmlAttribute? attr;

            //Find Xsi attributes that need to be processed before validating the element
            string? xsiNil = null;
            string? xsiType = null;

            for (int i = 0; i < attributes.Count; i++)
            {
                attr = attributes[i];
                string objectNs = attr.NamespaceURI;
                string objectName = attr.LocalName;
                Debug.Assert(_nameTable.Get(attr.NamespaceURI) != null);
                Debug.Assert(_nameTable.Get(attr.LocalName) != null);

                if (Ref.Equal(objectNs, _nsXsi))
                {
                    if (Ref.Equal(objectName, _xsiType))
                    {
                        xsiType = attr.Value;
                    }
                    else if (Ref.Equal(objectName, _xsiNil))
                    {
                        xsiNil = attr.Value;
                    }
                }
                else if (Ref.Equal(objectNs, _nsXmlNs))
                {
                    _nsManager.AddNamespace(attr.Prefix.Length == 0 ? string.Empty : attr.LocalName, attr.Value);
                }
            }

            _validator!.ValidateElement(elementNode.LocalName, elementNode.NamespaceURI, _schemaInfo, xsiType, xsiNil, null, null);
            ValidateAttributes(elementNode);
            _validator!.ValidateEndOfAttributes(_schemaInfo);

            //If element has children, drill down
            for (XmlNode? child = elementNode.FirstChild; child != null; child = child.NextSibling)
            {
                ValidateNode(child);
            }

            //Validate end of element
            _currentNode = elementNode; //Reset current Node for validation call back
            _validator.ValidateEndElement(_schemaInfo);
            //Get XmlName, as memberType / validity might be set now
            if (_psviAugmentation)
            {
                elementNode.XmlName = _document.AddXmlName(elementNode.Prefix, elementNode.LocalName, elementNode.NamespaceURI, _schemaInfo);
                if (_schemaInfo!.IsDefault)
                { //the element has a default value
                    XmlText textNode = _document.CreateTextNode(_schemaInfo.SchemaElement!.ElementDecl!.DefaultValueRaw);
                    elementNode.AppendChild(textNode);
                }
            }

            _nsManager.PopScope(); //Pop current namespace scope
        }

        private void ValidateAttributes(XmlElement elementNode)
        {
            XmlAttributeCollection attributes = elementNode.Attributes;
            XmlAttribute? attr;

            for (int i = 0; i < attributes.Count; i++)
            {
                attr = attributes[i];
                _currentNode = attr; //For nodeValueGetter to pick up the right attribute value
                if (Ref.Equal(attr.NamespaceURI, _nsXmlNs))
                { //Do not validate namespace decls
                    continue;
                }
                _validator!.ValidateAttribute(attr.LocalName, attr.NamespaceURI, _nodeValueGetter, _attributeSchemaInfo);
                if (_psviAugmentation)
                {
                    attr.XmlName = _document.AddAttrXmlName(attr.Prefix, attr.LocalName, attr.NamespaceURI, _attributeSchemaInfo);
                }
            }

            if (_psviAugmentation)
            {
                //Add default attributes to the attributes collection
                if (_defaultAttributes == null)
                {
                    _defaultAttributes = new ArrayList();
                }
                else
                {
                    _defaultAttributes.Clear();
                }
                _validator!.GetUnspecifiedDefaultAttributes(_defaultAttributes);
                XmlQualifiedName attrQName;
                for (int i = 0; i < _defaultAttributes.Count; i++)
                {
                    XmlSchemaAttribute schemaAttribute = (XmlSchemaAttribute)_defaultAttributes[i]!;
                    attrQName = schemaAttribute.QualifiedName;
                    attr = _document.CreateDefaultAttribute(GetDefaultPrefix(attrQName.Namespace), attrQName.Name, attrQName.Namespace);
                    SetDefaultAttributeSchemaInfo(schemaAttribute);
                    attr.XmlName = _document.AddAttrXmlName(attr.Prefix, attr.LocalName, attr.NamespaceURI, _attributeSchemaInfo);
                    attr.AppendChild(_document.CreateTextNode(schemaAttribute.AttDef!.DefaultValueRaw));
                    attributes.Append(attr);
                    XmlUnspecifiedAttribute? defAttr = attr as XmlUnspecifiedAttribute;
                    defAttr?.SetSpecified(false);
                }
            }
        }

        private void SetDefaultAttributeSchemaInfo(XmlSchemaAttribute schemaAttribute)
        {
            Debug.Assert(_attributeSchemaInfo != null);
            _attributeSchemaInfo.Clear();
            _attributeSchemaInfo.IsDefault = true;
            _attributeSchemaInfo.IsNil = false;
            _attributeSchemaInfo.SchemaType = schemaAttribute.AttributeSchemaType;
            _attributeSchemaInfo.SchemaAttribute = schemaAttribute;

            //Get memberType for default attribute
            SchemaAttDef attributeDef = schemaAttribute.AttDef!;
            if (attributeDef.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
            {
                XsdSimpleValue? simpleValue = attributeDef.DefaultValueTyped as XsdSimpleValue;
                Debug.Assert(simpleValue != null);
                _attributeSchemaInfo.MemberType = simpleValue.XmlType;
            }
            _attributeSchemaInfo.Validity = XmlSchemaValidity.Valid;
        }

        private string? GetDefaultPrefix(string attributeNS)
        {
            IDictionary<string, string> namespaceDecls = NamespaceResolver.GetNamespacesInScope(XmlNamespaceScope.All);
            string? defaultPrefix = null;
            string defaultNS;
            attributeNS = _nameTable.Add(attributeNS); //atomize ns

            foreach (KeyValuePair<string, string> pair in namespaceDecls)
            {
                defaultNS = _nameTable.Add(pair.Value);
                if (object.ReferenceEquals(defaultNS, attributeNS))
                {
                    defaultPrefix = pair.Key;
                    if (defaultPrefix.Length != 0)
                    { //Locate first non-empty prefix
                        return defaultPrefix;
                    }
                }
            }

            return defaultPrefix;
        }

        private string? GetNodeValue()
        {
            return _currentNode!.Value;
        }

        //Code for finding type during partial validation
        private XmlSchemaObject? FindSchemaInfo(XmlElement elementToValidate)
        {
            _isPartialTreeValid = true;
            Debug.Assert(elementToValidate.ParentNode!.NodeType != XmlNodeType.Document); //Handle if it is the documentElement separately

            //Create nodelist to navigate down again
            XmlNode currentNode = elementToValidate;
            IXmlSchemaInfo? parentSchemaInfo;
            int nodeIndex = 0;

            //Check common case of parent node first
            XmlNode? parentNode = currentNode.ParentNode;
            do
            {
                parentSchemaInfo = parentNode!.SchemaInfo;
                if (parentSchemaInfo.SchemaElement != null || parentSchemaInfo.SchemaType != null)
                {
                    break; //Found ancestor with schemaInfo
                }
                CheckNodeSequenceCapacity(nodeIndex);
                _nodeSequenceToValidate![nodeIndex++] = parentNode;
                parentNode = parentNode.ParentNode;
            } while (parentNode != null);

            if (parentNode == null)
            { //Did not find any type info all the way to the root, currentNode is Document || DocumentFragment
                nodeIndex--; //Subtract the one for document and set the node to null
                _nodeSequenceToValidate![nodeIndex] = null;
                return GetTypeFromAncestors(elementToValidate, null, nodeIndex);
            }
            else
            {
                //Start validating down from the parent or ancestor that has schema info and shallow validate all previous siblings
                //to correctly ascertain particle for current node
                CheckNodeSequenceCapacity(nodeIndex);
                _nodeSequenceToValidate![nodeIndex++] = parentNode;
                XmlSchemaObject? ancestorSchemaObject = parentSchemaInfo.SchemaElement;
                ancestorSchemaObject ??= parentSchemaInfo.SchemaType;
                return GetTypeFromAncestors(elementToValidate, ancestorSchemaObject, nodeIndex);
            }
        }

        /*private XmlSchemaElement GetTypeFromParent(XmlElement elementToValidate, XmlSchemaComplexType parentSchemaType) {
            XmlQualifiedName elementName = new XmlQualifiedName(elementToValidate.LocalName, elementToValidate.NamespaceURI);
            XmlSchemaElement elem = parentSchemaType.LocalElements[elementName] as XmlSchemaElement;
            if (elem == null) { //Element not found as direct child of the content model. It might be invalid at this position or it might be a substitution member
                SchemaInfo compiledSchemaInfo = schemas.CompiledInfo;
                XmlSchemaElement memberElem = compiledSchemaInfo.GetElement(elementName);
                if (memberElem != null) {
                }
            }
        }*/

        private void CheckNodeSequenceCapacity(int currentIndex)
        {
            if (_nodeSequenceToValidate == null)
            { //Normally users would call Validate one level down, this allows for 4
                _nodeSequenceToValidate = new XmlNode[4];
            }
            else if (currentIndex >= _nodeSequenceToValidate.Length - 1)
            { //reached capacity of array, Need to increase capacity to twice the initial
                XmlNode[] newNodeSequence = new XmlNode[_nodeSequenceToValidate.Length * 2];
                Array.Copy(_nodeSequenceToValidate, newNodeSequence, _nodeSequenceToValidate.Length);
                _nodeSequenceToValidate = newNodeSequence;
            }
        }

        private XmlSchemaAttribute? FindSchemaInfo(XmlAttribute attributeToValidate)
        {
            XmlElement parentElement = attributeToValidate.OwnerElement!;
            XmlSchemaObject? schemaObject = FindSchemaInfo(parentElement);
            XmlSchemaComplexType? elementSchemaType = GetComplexType(schemaObject);
            if (elementSchemaType == null)
            {
                return null;
            }

            XmlQualifiedName attName = new XmlQualifiedName(attributeToValidate.LocalName, attributeToValidate.NamespaceURI);
            XmlSchemaAttribute? schemaAttribute = elementSchemaType.AttributeUses[attName] as XmlSchemaAttribute;
            if (schemaAttribute == null)
            {
                XmlSchemaAnyAttribute? anyAttribute = elementSchemaType.AttributeWildcard;
                if (anyAttribute != null)
                {
                    if (anyAttribute.NamespaceList!.Allows(attName))
                    { //Match wildcard against global attribute
                        schemaAttribute = _schemas.GlobalAttributes[attName] as XmlSchemaAttribute;
                    }
                }
            }
            return schemaAttribute;
        }

        private XmlSchemaObject? GetTypeFromAncestors(XmlElement elementToValidate, XmlSchemaObject? ancestorType, int ancestorsCount)
        {
            //schemaInfo is currentNode's schemaInfo
            _validator = CreateTypeFinderValidator(ancestorType);
            _schemaInfo = new XmlSchemaInfo();

            //start at the ancestor to start validating
            int startIndex = ancestorsCount - 1;

            bool ancestorHasWildCard = AncestorTypeHasWildcard(ancestorType);
            for (int i = startIndex; i >= 0; i--)
            {
                XmlNode node = _nodeSequenceToValidate![i]!;
                XmlElement currentElement = (node as XmlElement)!;
                ValidateSingleElement(currentElement, false, _schemaInfo);
                if (!ancestorHasWildCard)
                { //store type if ancestor does not have wildcard in its content model
                    currentElement.XmlName = _document.AddXmlName(currentElement.Prefix, currentElement.LocalName, currentElement.NamespaceURI, _schemaInfo);
                    //update wildcard flag
                    ancestorHasWildCard = AncestorTypeHasWildcard(_schemaInfo.SchemaElement);
                }

                _validator.ValidateEndOfAttributes(null);
                if (i > 0)
                {
                    ValidateChildrenTillNextAncestor(node, _nodeSequenceToValidate[i - 1]!);
                }
                else
                { //i == 0
                    ValidateChildrenTillNextAncestor(node, elementToValidate);
                }
            }

            Debug.Assert(_nodeSequenceToValidate![0] == elementToValidate.ParentNode);
            //validate element whose type is needed,
            ValidateSingleElement(elementToValidate, false, _schemaInfo);

            XmlSchemaObject? schemaInfoFound;
            if (_schemaInfo.SchemaElement != null)
            {
                schemaInfoFound = _schemaInfo.SchemaElement;
            }
            else
            {
                schemaInfoFound = _schemaInfo.SchemaType;
            }
            if (schemaInfoFound == null)
            { //Detect if the node was validated lax or skip
                if (_validator.CurrentProcessContents == XmlSchemaContentProcessing.Skip)
                {
                    if (_isPartialTreeValid)
                    { //Then node assessed as skip; if there was error we turn processContents to skip as well. But this is not the same as validating as skip.
                        return XmlSchemaComplexType.AnyTypeSkip;
                    }
                }
                else if (_validator.CurrentProcessContents == XmlSchemaContentProcessing.Lax)
                {
                    return XmlSchemaComplexType.AnyType;
                }
            }
            return schemaInfoFound;
        }

        private static bool AncestorTypeHasWildcard(XmlSchemaObject? ancestorType)
        {
            XmlSchemaComplexType? ancestorSchemaType = GetComplexType(ancestorType);
            if (ancestorType != null)
            {
                return ancestorSchemaType!.HasWildCard;
            }

            return false;
        }

        private static XmlSchemaComplexType? GetComplexType(XmlSchemaObject? schemaObject)
        {
            if (schemaObject == null)
            {
                return null;
            }

            XmlSchemaElement? schemaElement = schemaObject as XmlSchemaElement;
            XmlSchemaComplexType? complexType;
            if (schemaElement != null)
            {
                complexType = schemaElement.ElementSchemaType as XmlSchemaComplexType;
            }
            else
            {
                complexType = schemaObject as XmlSchemaComplexType;
            }

            return complexType;
        }

        private void ValidateSingleElement(XmlElement elementNode, bool skipToEnd, XmlSchemaInfo? newSchemaInfo)
        {
            _nsManager.PushScope();
            Debug.Assert(elementNode != null);

            XmlAttributeCollection attributes = elementNode.Attributes;
            XmlAttribute? attr;

            //Find Xsi attributes that need to be processed before validating the element
            string? xsiNil = null;
            string? xsiType = null;

            for (int i = 0; i < attributes.Count; i++)
            {
                attr = attributes[i];
                string objectNs = attr.NamespaceURI;
                string objectName = attr.LocalName;
                Debug.Assert(_nameTable.Get(attr.NamespaceURI) != null);
                Debug.Assert(_nameTable.Get(attr.LocalName) != null);

                if (Ref.Equal(objectNs, _nsXsi))
                {
                    if (Ref.Equal(objectName, _xsiType))
                    {
                        xsiType = attr.Value;
                    }
                    else if (Ref.Equal(objectName, _xsiNil))
                    {
                        xsiNil = attr.Value;
                    }
                }
                else if (Ref.Equal(objectNs, _nsXmlNs))
                {
                    _nsManager.AddNamespace(attr.Prefix.Length == 0 ? string.Empty : attr.LocalName, attr.Value);
                }
            }

            _validator!.ValidateElement(elementNode.LocalName, elementNode.NamespaceURI, newSchemaInfo, xsiType, xsiNil, null, null);
            //Validate end of element
            if (skipToEnd)
            {
                _validator!.ValidateEndOfAttributes(newSchemaInfo);
                _validator!.SkipToEndElement(newSchemaInfo);
                _nsManager.PopScope(); //Pop current namespace scope
            }
        }

        private void ValidateChildrenTillNextAncestor(XmlNode parentNode, XmlNode childToStopAt)
        {
            XmlNode? child;

            for (child = parentNode.FirstChild; child != null; child = child.NextSibling)
            {
                if (child == childToStopAt)
                {
                    break;
                }

                switch (child.NodeType)
                {
                    case XmlNodeType.EntityReference:
                        ValidateChildrenTillNextAncestor(child, childToStopAt);
                        break;

                    case XmlNodeType.Element: //Flat validation, do not drill down into children
                        ValidateSingleElement((child as XmlElement)!, true, null);
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        _validator!.ValidateText(child.Value!);
                        break;

                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        _validator!.ValidateWhitespace(child.Value!);
                        break;

                    case XmlNodeType.Comment:
                    case XmlNodeType.ProcessingInstruction:
                        break;

                    default:
                        throw new InvalidOperationException(SR.Format(SR.Xml_UnexpectedNodeType, _currentNode!.NodeType));
                }
            }

            Debug.Assert(child == childToStopAt);
        }

        private XmlSchemaValidator CreateTypeFinderValidator(XmlSchemaObject? partialValidationType)
        {
            XmlSchemaValidator findTypeValidator = new XmlSchemaValidator(_document.NameTable, _document.Schemas, _nsManager, XmlSchemaValidationFlags.None);
            findTypeValidator.ValidationEventHandler += new ValidationEventHandler(TypeFinderCallBack);
            if (partialValidationType != null)
            {
                findTypeValidator.Initialize(partialValidationType);
            }
            else
            { //If we walked up to the root and no schemaInfo was there, start validating from root
                findTypeValidator.Initialize();
            }

            return findTypeValidator;
        }

        private void TypeFinderCallBack(object? sender, ValidationEventArgs arg)
        {
            if (arg.Severity == XmlSeverityType.Error)
            {
                _isPartialTreeValid = false;
            }
        }

        private void InternalValidationCallBack(object? sender, ValidationEventArgs arg)
        {
            if (arg.Severity == XmlSeverityType.Error)
            {
                _isValid = false;
            }
            XmlSchemaValidationException? ex = arg.Exception as XmlSchemaValidationException;
            Debug.Assert(ex != null);
            ex.SetSourceObject(_currentNode);
            if (_eventHandler != null)
            { //Invoke user's event handler
                _eventHandler(sender, arg);
            }
            else if (arg.Severity == XmlSeverityType.Error)
            {
                throw ex;
            }
        }
    }
}
