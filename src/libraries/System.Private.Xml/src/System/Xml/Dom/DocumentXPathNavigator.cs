// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml
{
    internal sealed class DocumentXPathNavigator : XPathNavigator, IHasXmlNode
    {
        private readonly XmlDocument _document; // owner document
        private XmlNode _source; // navigator position
        private int _attributeIndex; // index in attribute collection for attribute
        private XmlElement? _namespaceParent; // parent for namespace

        public DocumentXPathNavigator(XmlDocument document, XmlNode node)
        {
            _document = document;
            ResetPosition(node);
        }

        public DocumentXPathNavigator(DocumentXPathNavigator other)
        {
            _document = other._document;
            _source = other._source;
            _attributeIndex = other._attributeIndex;
            _namespaceParent = other._namespaceParent;
        }

        public override XPathNavigator Clone()
        {
            return new DocumentXPathNavigator(this);
        }

        public override void SetValue(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            XmlNode node = _source;
            XmlNode end;

            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    if (((XmlAttribute)node).IsNamespace)
                    {
                        goto default;
                    }
                    node.InnerText = value;
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    CalibrateText();

                    node = _source;
                    end = TextEnd(node);
                    if (node != end)
                    {
                        if (node.IsReadOnly)
                        {
                            throw new InvalidOperationException(SR.Xdom_Node_Modify_ReadOnly);
                        }
                        DeleteToFollowingSibling(node.NextSibling!, end);
                    }
                    goto case XmlNodeType.Element;
                case XmlNodeType.Element:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.Comment:
                    node.InnerText = value;
                    break;
                default:
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
            }
        }

        public override XmlNameTable NameTable
        {
            get
            {
                return _document.NameTable;
            }
        }

        public override XPathNodeType NodeType
        {
            get
            {
                CalibrateText();

                return (XPathNodeType)_source.XPNodeType;
            }
        }

        public override string LocalName
        {
            get
            {
                return _source.XPLocalName;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                if (_source is XmlAttribute attribute
                    && attribute.IsNamespace)
                {
                    return string.Empty;
                }
                return _source.NamespaceURI;
            }
        }

        public override string Name
        {
            get
            {
                switch (_source.NodeType)
                {
                    case XmlNodeType.Element:
                    case XmlNodeType.ProcessingInstruction:
                        return _source.Name;
                    case XmlNodeType.Attribute:
                        if (((XmlAttribute)_source).IsNamespace)
                        {
                            string localName = _source.LocalName;
                            if (Ref.Equal(localName, _document.strXmlns))
                            {
                                return string.Empty; // xmlns declaration
                            }
                            return localName; // xmlns:name declaration
                        }
                        return _source.Name; // attribute
                    default:
                        return string.Empty;
                }
            }
        }

        public override string Prefix
        {
            get
            {
                if (_source is XmlAttribute attribute
                    && attribute.IsNamespace)
                {
                    return string.Empty;
                }
                return _source.Prefix;
            }
        }

        public override string Value
        {
            get
            {
                switch (_source.NodeType)
                {
                    case XmlNodeType.Element:
                    case XmlNodeType.DocumentFragment:
                        return _source.InnerText;
                    case XmlNodeType.Document:
                        return ValueDocument;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        return ValueText;
                    default:
                        Debug.Assert(_source.Value != null);
                        return _source.Value;
                }
            }
        }

        private string ValueDocument
        {
            get
            {
                XmlElement? element = _document.DocumentElement;
                if (element != null)
                {
                    return element.InnerText;
                }
                return string.Empty;
            }
        }

        private string ValueText
        {
            get
            {
                CalibrateText();

                string? value = _source.Value;
                XmlNode? nextSibling = NextSibling(_source);
                if (nextSibling != null
                    && nextSibling.IsText)
                {
                    StringBuilder builder = new StringBuilder(value);
                    do
                    {
                        builder.Append(nextSibling.Value);
                        nextSibling = NextSibling(nextSibling);
                    }
                    while (nextSibling != null
                           && nextSibling.IsText);
                    value = builder.ToString();
                }

                Debug.Assert(value != null);
                return value;
            }
        }

        public override string BaseURI
        {
            get
            {
                return _source.BaseURI;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                if (_source is XmlElement element)
                {
                    return element.IsEmpty;
                }
                return false;
            }
        }

        public override string XmlLang
        {
            get
            {
                return _source.XmlLang;
            }
        }

        public override object UnderlyingObject
        {
            get
            {
                CalibrateText();

                return _source;
            }
        }

        public override bool HasAttributes
        {
            get
            {
                if (_source is XmlElement element
                    && element.HasAttributes)
                {
                    XmlAttributeCollection attributes = element.Attributes;
                    for (int i = 0; i < attributes.Count; i++)
                    {
                        XmlAttribute attribute = attributes[i];
                        if (!attribute.IsNamespace)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public override string GetAttribute(string localName, string namespaceURI)
        {
            return _source.GetXPAttribute(localName, namespaceURI);
        }

        public override bool MoveToAttribute(string localName, string namespaceURI)
        {
            if (_source is XmlElement element
                && element.HasAttributes)
            {
                XmlAttributeCollection attributes = element.Attributes;
                for (int i = 0; i < attributes.Count; i++)
                {
                    XmlAttribute attribute = attributes[i];
                    if (attribute.LocalName == localName
                        && attribute.NamespaceURI == namespaceURI)
                    {
                        if (!attribute.IsNamespace)
                        {
                            _source = attribute;
                            _attributeIndex = i;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        public override bool MoveToFirstAttribute()
        {
            if (_source is XmlElement element
                && element.HasAttributes)
            {
                XmlAttributeCollection attributes = element.Attributes;
                for (int i = 0; i < attributes.Count; i++)
                {
                    XmlAttribute attribute = attributes[i];
                    if (!attribute.IsNamespace)
                    {
                        _source = attribute;
                        _attributeIndex = i;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool MoveToNextAttribute()
        {
            if (!(_source is XmlAttribute attribute)
                || attribute.IsNamespace)
            {
                return false;
            }
            XmlAttributeCollection? attributes;
            if (!CheckAttributePosition(attribute, out attributes, _attributeIndex)
                && !ResetAttributePosition(attribute, attributes, out _attributeIndex))
            {
                return false;
            }
            for (int i = _attributeIndex + 1; i < attributes.Count; i++)
            {
                attribute = attributes[i];
                if (!attribute.IsNamespace)
                {
                    _source = attribute;
                    _attributeIndex = i;
                    return true;
                }
            }
            return false;
        }

        public override string GetNamespace(string name)
        {
            XmlNode? node = _source;
            while (node != null
                   && node.NodeType != XmlNodeType.Element)
            {
                if (node is XmlAttribute attribute)
                {
                    node = attribute.OwnerElement;
                }
                else
                {
                    node = node.ParentNode;
                }
            }

            XmlElement? element = node as XmlElement;
            if (element != null)
            {
                string localName;
                if (name != null
                    && name.Length != 0)
                {
                    localName = name;
                }
                else
                {
                    localName = _document.strXmlns;
                }
                string namespaceUri = _document.strReservedXmlns;

                do
                {
                    XmlAttribute? attribute = element.GetAttributeNode(localName, namespaceUri);
                    if (attribute != null)
                    {
                        return attribute.Value;
                    }
                    element = element.ParentNode as XmlElement;
                }
                while (element != null);
            }

            if (name == _document.strXml)
            {
                return _document.strReservedXml;
            }
            else if (name == _document.strXmlns)
            {
                return _document.strReservedXmlns;
            }
            return string.Empty;
        }

        public override bool MoveToNamespace(string name)
        {
            if (name == _document.strXmlns)
            {
                return false;
            }
            XmlElement? element = _source as XmlElement;
            if (element != null)
            {
                string localName;
                if (name != null
                    && name.Length != 0)
                {
                    localName = name;
                }
                else
                {
                    localName = _document.strXmlns;
                }
                string namespaceUri = _document.strReservedXmlns;

                do
                {
                    XmlAttribute? attribute = element.GetAttributeNode(localName, namespaceUri);
                    if (attribute != null)
                    {
                        _namespaceParent = (XmlElement)_source;
                        _source = attribute;
                        return true;
                    }
                    element = element.ParentNode as XmlElement;
                }
                while (element != null);

                if (name == _document.strXml)
                {
                    _namespaceParent = (XmlElement)_source;
                    _source = _document.NamespaceXml;
                    return true;
                }
            }
            return false;
        }

        public override bool MoveToFirstNamespace(XPathNamespaceScope scope)
        {
            if (!(_source is XmlElement element))
            {
                return false;
            }
            XmlAttributeCollection attributes;
            int index = int.MaxValue;
            switch (scope)
            {
                case XPathNamespaceScope.Local:
                    if (!element.HasAttributes)
                    {
                        return false;
                    }
                    attributes = element.Attributes;
                    if (!MoveToFirstNamespaceLocal(attributes, ref index))
                    {
                        return false;
                    }
                    _source = attributes[index];
                    _attributeIndex = index;
                    _namespaceParent = element;
                    break;
                case XPathNamespaceScope.ExcludeXml:
                    attributes = element.Attributes;
                    if (!MoveToFirstNamespaceGlobal(ref attributes, ref index))
                    {
                        return false;
                    }
                    XmlAttribute attribute = attributes[index];
                    while (Ref.Equal(attribute.LocalName, _document.strXml))
                    {
                        if (!MoveToNextNamespaceGlobal(ref attributes, ref index))
                        {
                            return false;
                        }
                        attribute = attributes[index];
                    }
                    _source = attribute;
                    _attributeIndex = index;
                    _namespaceParent = element;
                    break;
                case XPathNamespaceScope.All:
                    attributes = element.Attributes;
                    if (!MoveToFirstNamespaceGlobal(ref attributes, ref index))
                    {
                        _source = _document.NamespaceXml;
                        // attributeIndex = 0;
                    }
                    else
                    {
                        _source = attributes[index];
                        _attributeIndex = index;
                    }
                    _namespaceParent = element;
                    break;
                default:
                    Debug.Fail($"Unexpected scope {scope}");
                    return false;
            }
            return true;
        }

        private static bool MoveToFirstNamespaceLocal(XmlAttributeCollection attributes, ref int index)
        {
            Debug.Assert(attributes != null);
            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                XmlAttribute attribute = attributes[i];
                if (attribute.IsNamespace)
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }

        private static bool MoveToFirstNamespaceGlobal(ref XmlAttributeCollection attributes, ref int index)
        {
            if (MoveToFirstNamespaceLocal(attributes, ref index))
            {
                return true;
            }

            Debug.Assert(attributes != null && attributes.parent != null);
            XmlElement? element = attributes.parent.ParentNode as XmlElement;
            while (element != null)
            {
                if (element.HasAttributes)
                {
                    attributes = element.Attributes;
                    if (MoveToFirstNamespaceLocal(attributes, ref index))
                    {
                        return true;
                    }
                }
                element = element.ParentNode as XmlElement;
            }
            return false;
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope scope)
        {
            if (!(_source is XmlAttribute attribute)
                || !attribute.IsNamespace)
            {
                return false;
            }
            XmlAttributeCollection? attributes;
            int index = _attributeIndex;
            if (!CheckAttributePosition(attribute, out attributes, index)
                && !ResetAttributePosition(attribute, attributes, out index))
            {
                return false;
            }
            Debug.Assert(_namespaceParent != null);
            switch (scope)
            {
                case XPathNamespaceScope.Local:
                    if (attribute.OwnerElement != _namespaceParent)
                    {
                        return false;
                    }
                    if (!MoveToNextNamespaceLocal(attributes, ref index))
                    {
                        return false;
                    }
                    _source = attributes[index];
                    _attributeIndex = index;
                    break;
                case XPathNamespaceScope.ExcludeXml:
                    string localName;
                    do
                    {
                        if (!MoveToNextNamespaceGlobal(ref attributes, ref index))
                        {
                            return false;
                        }
                        attribute = attributes[index];
                        localName = attribute.LocalName;
                    }
                    while (PathHasDuplicateNamespace(attribute.OwnerElement, _namespaceParent, localName)
                           || Ref.Equal(localName, _document.strXml));
                    _source = attribute;
                    _attributeIndex = index;
                    break;
                case XPathNamespaceScope.All:
                    do
                    {
                        if (!MoveToNextNamespaceGlobal(ref attributes, ref index))
                        {
                            if (PathHasDuplicateNamespace(null, _namespaceParent, _document.strXml))
                            {
                                return false;
                            }
                            else
                            {
                                _source = _document.NamespaceXml;
                                // attributeIndex = 0;
                                return true;
                            }
                        }
                        attribute = attributes[index];
                    }
                    while (PathHasDuplicateNamespace(attribute.OwnerElement, _namespaceParent, attribute.LocalName));
                    _source = attribute;
                    _attributeIndex = index;
                    break;
                default:
                    Debug.Fail($"Unexpected scope {scope}");
                    return false;
            }
            return true;
        }

        private static bool MoveToNextNamespaceLocal(XmlAttributeCollection attributes, ref int index)
        {
            Debug.Assert(attributes != null);
            Debug.Assert(0 <= index && index < attributes.Count);
            for (int i = index - 1; i >= 0; i--)
            {
                XmlAttribute attribute = attributes[i];
                if (attribute.IsNamespace)
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }

        private static bool MoveToNextNamespaceGlobal(ref XmlAttributeCollection attributes, ref int index)
        {
            if (MoveToNextNamespaceLocal(attributes, ref index))
            {
                return true;
            }

            Debug.Assert(attributes != null && attributes.parent != null);
            XmlElement? element = attributes.parent.ParentNode as XmlElement;
            while (element != null)
            {
                if (element.HasAttributes)
                {
                    attributes = element.Attributes;
                    if (MoveToFirstNamespaceLocal(attributes, ref index))
                    {
                        return true;
                    }
                }
                element = element.ParentNode as XmlElement;
            }
            return false;
        }

        private bool PathHasDuplicateNamespace(XmlElement? top, XmlElement bottom, string localName)
        {
            XmlElement? current = bottom;
            string namespaceUri = _document.strReservedXmlns;
            while (current != null
                   && current != top)
            {
                XmlAttribute? attribute = current.GetAttributeNode(localName, namespaceUri);
                if (attribute != null)
                {
                    return true;
                }
                current = current.ParentNode as XmlElement;
            }
            return false;
        }

        public override string? LookupNamespace(string prefix)
        {
            string? ns = base.LookupNamespace(prefix);
            if (ns != null)
            {
                ns = this.NameTable.Add(ns);
            }
            return ns;
        }

        public override bool MoveToNext()
        {
            XmlNode? sibling = NextSibling(_source);
            if (sibling == null)
            {
                return false;
            }
            if (sibling.IsText)
            {
                if (_source.IsText)
                {
                    sibling = NextSibling(TextEnd(sibling));
                    if (sibling == null)
                    {
                        return false;
                    }
                }
            }
            XmlNode? parent = ParentNode(sibling);
            Debug.Assert(parent != null);
            while (!IsValidChild(parent, sibling))
            {
                sibling = NextSibling(sibling);
                if (sibling == null)
                {
                    return false;
                }
            }
            _source = sibling;
            return true;
        }

        public override bool MoveToPrevious()
        {
            XmlNode? sibling = PreviousSibling(_source);
            if (sibling == null)
            {
                return false;
            }
            if (sibling.IsText)
            {
                if (_source.IsText)
                {
                    sibling = PreviousSibling(TextStart(sibling));
                    if (sibling == null)
                    {
                        return false;
                    }
                }
                else
                {
                    sibling = TextStart(sibling);
                }
            }
            XmlNode? parent = ParentNode(sibling);
            Debug.Assert(parent != null);
            while (!IsValidChild(parent, sibling))
            {
                sibling = PreviousSibling(sibling);
                if (sibling == null)
                {
                    return false;
                }
                // if (sibling.IsText) {
                //     sibling = TextStart(sibling);
                // }
            }
            _source = sibling;
            return true;
        }

        public override bool MoveToFirst()
        {
            if (_source.NodeType == XmlNodeType.Attribute)
            {
                return false;
            }
            XmlNode? parent = ParentNode(_source);
            if (parent == null)
            {
                return false;
            }
            XmlNode? sibling = FirstChild(parent);
            Debug.Assert(sibling != null);
            while (!IsValidChild(parent, sibling))
            {
                sibling = NextSibling(sibling);
                if (sibling == null)
                {
                    return false;
                }
            }
            _source = sibling;
            return true;
        }

        public override bool MoveToFirstChild()
        {
            XmlNode? child;
            switch (_source.NodeType)
            {
                case XmlNodeType.Element:
                    child = FirstChild(_source);
                    if (child == null)
                    {
                        return false;
                    }
                    break;
                case XmlNodeType.DocumentFragment:
                case XmlNodeType.Document:
                    child = FirstChild(_source);
                    if (child == null)
                    {
                        return false;
                    }
                    while (!IsValidChild(_source, child))
                    {
                        child = NextSibling(child);
                        if (child == null)
                        {
                            return false;
                        }
                    }
                    break;
                default:
                    return false;
            }
            _source = child;
            return true;
        }

        public override bool MoveToParent()
        {
            XmlNode? parent = ParentNode(_source);
            if (parent != null)
            {
                _source = parent;
                return true;
            }
            if (_source is XmlAttribute attribute)
            {
                parent = attribute.IsNamespace ? _namespaceParent : attribute.OwnerElement;
                if (parent != null)
                {
                    _source = parent;
                    _namespaceParent = null;
                    return true;
                }
            }
            return false;
        }

        public override void MoveToRoot()
        {
            while (true)
            {
                XmlNode? parent = _source.ParentNode;
                if (parent == null)
                {
                    if (!(_source is XmlAttribute attribute))
                    {
                        break;
                    }
                    parent = attribute.IsNamespace ? _namespaceParent : attribute.OwnerElement;
                    if (parent == null)
                    {
                        break;
                    }
                }
                _source = parent;
            }
            _namespaceParent = null;
        }

        public override bool MoveTo(XPathNavigator other)
        {
            if (other is DocumentXPathNavigator that
                && _document == that._document)
            {
                _source = that._source;
                _attributeIndex = that._attributeIndex;
                _namespaceParent = that._namespaceParent;
                return true;
            }
            return false;
        }

        public override bool MoveToId(string id)
        {
            XmlElement? element = _document.GetElementById(id);
            if (element != null)
            {
                _source = element;
                _namespaceParent = null;
                return true;
            }
            return false;
        }

        public override bool MoveToChild(string localName, string namespaceUri)
        {
            if (_source.NodeType == XmlNodeType.Attribute)
            {
                return false;
            }

            XmlNode? child = FirstChild(_source);
            if (child != null)
            {
                do
                {
                    if (child.NodeType == XmlNodeType.Element
                        && child.LocalName == localName
                        && child.NamespaceURI == namespaceUri)
                    {
                        _source = child;
                        return true;
                    }
                    child = NextSibling(child);
                }
                while (child != null);
            }
            return false;
        }

        public override bool MoveToChild(XPathNodeType type)
        {
            if (_source.NodeType == XmlNodeType.Attribute)
            {
                return false;
            }

            XmlNode? child = FirstChild(_source);
            if (child != null)
            {
                int mask = GetContentKindMask(type);
                if (mask == 0)
                {
                    return false;
                }
                do
                {
                    if (((1 << (int)child.XPNodeType) & mask) != 0)
                    {
                        _source = child;
                        return true;
                    }
                    child = NextSibling(child);
                }
                while (child != null);
            }
            return false;
        }

        public override bool MoveToFollowing(string localName, string namespaceUri, XPathNavigator? end)
        {
            XmlNode? pastFollowing = null;
            if (end is DocumentXPathNavigator that)
            {
                if (_document != that._document)
                {
                    return false;
                }
                switch (that._source.NodeType)
                {
                    case XmlNodeType.Attribute:
                        that = (DocumentXPathNavigator)that.Clone();
                        if (!that.MoveToNonDescendant())
                        {
                            return false;
                        }
                        break;
                }
                pastFollowing = that._source;
            }

            XmlNode? following = _source;
            if (following.NodeType == XmlNodeType.Attribute)
            {
                following = ((XmlAttribute)following).OwnerElement;
                if (following == null)
                {
                    return false;
                }
            }
            do
            {
                XmlNode? firstChild = following.FirstChild;
                if (firstChild != null)
                {
                    following = firstChild;
                }
                else
                {
                    while (true)
                    {
                        XmlNode? nextSibling = following.NextSibling;
                        if (nextSibling != null)
                        {
                            following = nextSibling;
                            break;
                        }
                        else
                        {
                            XmlNode? parent = following.ParentNode;
                            if (parent != null)
                            {
                                following = parent;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
                if (following == pastFollowing)
                {
                    return false;
                }
            }
            while (following.NodeType != XmlNodeType.Element
                   || following.LocalName != localName
                   || following.NamespaceURI != namespaceUri);

            _source = following;
            return true;
        }

        public override bool MoveToFollowing(XPathNodeType type, XPathNavigator? end)
        {
            XmlNode? pastFollowing = null;
            if (end is DocumentXPathNavigator that)
            {
                if (_document != that._document)
                {
                    return false;
                }
                switch (that._source.NodeType)
                {
                    case XmlNodeType.Attribute:
                        that = (DocumentXPathNavigator)that.Clone();
                        if (!that.MoveToNonDescendant())
                        {
                            return false;
                        }
                        break;
                }
                pastFollowing = that._source;
            }

            int mask = GetContentKindMask(type);
            if (mask == 0)
            {
                return false;
            }
            XmlNode? following = _source;
            switch (following.NodeType)
            {
                case XmlNodeType.Attribute:
                    following = ((XmlAttribute)following).OwnerElement;
                    if (following == null)
                    {
                        return false;
                    }
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    following = TextEnd(following);
                    break;
            }
            do
            {
                XmlNode? firstChild = following.FirstChild;
                if (firstChild != null)
                {
                    following = firstChild;
                }
                else
                {
                    while (true)
                    {
                        XmlNode? nextSibling = following.NextSibling;
                        if (nextSibling != null)
                        {
                            following = nextSibling;
                            break;
                        }
                        else
                        {
                            XmlNode? parent = following.ParentNode;
                            if (parent != null)
                            {
                                following = parent;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
                if (following == pastFollowing)
                {
                    return false;
                }
            }
            while (((1 << (int)following.XPNodeType) & mask) == 0);

            _source = following;
            return true;
        }

        public override bool MoveToNext(string localName, string namespaceUri)
        {
            XmlNode? sibling = NextSibling(_source);
            if (sibling == null)
            {
                return false;
            }
            do
            {
                if (sibling.NodeType == XmlNodeType.Element
                    && sibling.LocalName == localName
                    && sibling.NamespaceURI == namespaceUri)
                {
                    _source = sibling;
                    return true;
                }
                sibling = NextSibling(sibling);
            }
            while (sibling != null);
            return false;
        }

        public override bool MoveToNext(XPathNodeType type)
        {
            XmlNode? sibling = NextSibling(_source);
            if (sibling == null)
            {
                return false;
            }
            if (sibling.IsText
                && _source.IsText)
            {
                sibling = NextSibling(TextEnd(sibling));
                if (sibling == null)
                {
                    return false;
                }
            }

            int mask = GetContentKindMask(type);
            if (mask == 0)
            {
                return false;
            }
            do
            {
                if (((1 << (int)sibling.XPNodeType) & mask) != 0)
                {
                    _source = sibling;
                    return true;
                }
                sibling = NextSibling(sibling);
            }
            while (sibling != null);
            return false;
        }

        public override bool HasChildren
        {
            get
            {
                XmlNode? child;
                switch (_source.NodeType)
                {
                    case XmlNodeType.Element:
                        child = FirstChild(_source);
                        if (child == null)
                        {
                            return false;
                        }
                        return true;
                    case XmlNodeType.DocumentFragment:
                    case XmlNodeType.Document:
                        child = FirstChild(_source);
                        if (child == null)
                        {
                            return false;
                        }
                        while (!IsValidChild(_source, child))
                        {
                            child = NextSibling(child);
                            if (child == null)
                            {
                                return false;
                            }
                        }
                        return true;
                    default:
                        return false;
                }
            }
        }

        public override bool IsSamePosition(XPathNavigator other)
        {
            if (other is DocumentXPathNavigator that)
            {
                this.CalibrateText();
                that.CalibrateText();

                return _source == that._source
                       && _namespaceParent == that._namespaceParent;
            }
            return false;
        }

        public override bool IsDescendant([NotNullWhen(true)] XPathNavigator? other)
        {
            if (other is DocumentXPathNavigator that)
            {
                return IsDescendant(_source, that._source);
            }
            return false;
        }

        public override IXmlSchemaInfo SchemaInfo
        {
            get
            {
                return _source.SchemaInfo;
            }
        }

        public override bool CheckValidity(XmlSchemaSet schemas, ValidationEventHandler validationEventHandler)
        {
            XmlDocument? ownerDocument;

            if (_source.NodeType == XmlNodeType.Document)
            {
                ownerDocument = (XmlDocument)_source;
            }
            else
            {
                ownerDocument = _source.OwnerDocument;

                if (schemas != null)
                {
                    throw new ArgumentException(SR.Format(SR.XPathDocument_SchemaSetNotAllowed, null));
                }
            }
            if (schemas == null && ownerDocument != null)
            {
                schemas = ownerDocument.Schemas;
            }

            if (schemas == null || schemas.Count == 0)
            {
                throw new InvalidOperationException(SR.XmlDocument_NoSchemaInfo);
            }

            // DocumentSchemaValidator assumes that ownedDocument can never be null.
            Debug.Assert(ownerDocument != null);
            DocumentSchemaValidator validator = new DocumentSchemaValidator(ownerDocument, schemas, validationEventHandler);
            validator.PsviAugmentation = false;
            return validator.Validate(_source);
        }

        private static XmlNode? OwnerNode(XmlNode node)
        {
            XmlNode? parent = node.ParentNode;
            if (parent != null)
            {
                return parent;
            }
            if (node is XmlAttribute attribute)
            {
                return attribute.OwnerElement;
            }
            return null;
        }

        private static int GetDepth(XmlNode node)
        {
            int depth = 0;
            XmlNode? owner = OwnerNode(node);
            while (owner != null)
            {
                depth++;
                owner = OwnerNode(owner);
            }
            return depth;
        }

        //Assuming that node1 and node2 are in the same level; Except when they are namespace nodes, they should have the same parent node
        //the returned value is node2's position corresponding to node1
        private static XmlNodeOrder Compare(XmlNode node1, XmlNode node2)
        {
            Debug.Assert(node1 != null);
            Debug.Assert(node2 != null);
            Debug.Assert(node1 != node2, "Should be handled by ComparePosition()");
            //Attribute nodes come before other children nodes except namespace nodes
            Debug.Assert(OwnerNode(node1) == OwnerNode(node2));
            if (node1.XPNodeType == XPathNodeType.Attribute)
            {
                if (node2.XPNodeType == XPathNodeType.Attribute)
                {
                    XmlElement? element = ((XmlAttribute)node1).OwnerElement;
                    Debug.Assert(element != null);
                    if (element.HasAttributes)
                    {
                        XmlAttributeCollection attributes = element.Attributes;
                        for (int i = 0; i < attributes.Count; i++)
                        {
                            XmlAttribute attribute = attributes[i];
                            if (attribute == node1)
                            {
                                return XmlNodeOrder.Before;
                            }
                            else if (attribute == node2)
                            {
                                return XmlNodeOrder.After;
                            }
                        }
                    }
                    return XmlNodeOrder.Unknown;
                }
                else
                {
                    return XmlNodeOrder.Before;
                }
            }
            if (node2.XPNodeType == XPathNodeType.Attribute)
            {
                return XmlNodeOrder.After;
            }

            //neither of the node is Namespace node or Attribute node
            XmlNode? nextNode = node1.NextSibling;
            while (nextNode != null && nextNode != node2)
                nextNode = nextNode.NextSibling;
            if (nextNode == null)
                //didn't meet node2 in the path to the end, thus it has to be in the front of node1
                return XmlNodeOrder.After;
            else
                //met node2 in the path to the end, so node1 is at front
                return XmlNodeOrder.Before;
        }

        public override XmlNodeOrder ComparePosition(XPathNavigator? other)
        {
            if (!(other is DocumentXPathNavigator that))
            {
                return XmlNodeOrder.Unknown;
            }

            this.CalibrateText();
            that.CalibrateText();

            if (_source == that._source
                && _namespaceParent == that._namespaceParent)
            {
                return XmlNodeOrder.Same;
            }

            if (_namespaceParent != null
                || that._namespaceParent != null)
            {
                return base.ComparePosition(other);
            }

            XmlNode? node1 = _source;
            XmlNode? node2 = that._source;

            XmlNode? parent1 = OwnerNode(node1);
            XmlNode? parent2 = OwnerNode(node2);
            if (parent1 == parent2)
            {
                if (parent1 == null)
                {
                    return XmlNodeOrder.Unknown;
                }
                else
                {
                    Debug.Assert(node1 != node2);
                    return Compare(node1, node2);
                }
            }

            int depth1 = GetDepth(node1);
            int depth2 = GetDepth(node2);
            if (depth2 > depth1)
            {
                while (node2 != null
                       && depth2 > depth1)
                {
                    node2 = OwnerNode(node2);
                    depth2--;
                }
                if (node1 == node2)
                {
                    return XmlNodeOrder.Before;
                }
                Debug.Assert(node2 != null);
                parent2 = OwnerNode(node2);
            }
            else if (depth1 > depth2)
            {
                while (node1 != null
                       && depth1 > depth2)
                {
                    node1 = OwnerNode(node1);
                    depth1--;
                }
                if (node1 == node2)
                {
                    return XmlNodeOrder.After;
                }
                Debug.Assert(node1 != null);
                parent1 = OwnerNode(node1);
            }

            while (parent1 != null
                   && parent2 != null)
            {
                if (parent1 == parent2)
                {
                    Debug.Assert(node1 != node2);
                    return Compare(node1, node2);
                }
                node1 = parent1;
                node2 = parent2;
                parent1 = OwnerNode(node1);
                parent2 = OwnerNode(node2);
            }
            return XmlNodeOrder.Unknown;
        }

        //the function just for XPathNodeList to enumerate current Node.
        XmlNode IHasXmlNode.GetNode() { return _source; }

        public override XPathNodeIterator SelectDescendants(string localName, string namespaceURI, bool matchSelf)
        {
            string? nsAtom = _document.NameTable.Get(namespaceURI);
            if (nsAtom == null || _source.NodeType == XmlNodeType.Attribute)
                return new DocumentXPathNodeIterator_Empty(this);

            Debug.Assert(this.NodeType != XPathNodeType.Attribute && this.NodeType != XPathNodeType.Namespace && this.NodeType != XPathNodeType.All);

            string? localNameAtom = _document.NameTable.Get(localName);
            if (localNameAtom == null)
                return new DocumentXPathNodeIterator_Empty(this);

            if (localNameAtom.Length == 0)
            {
                if (matchSelf)
                    return new DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName(this, nsAtom);
                return new DocumentXPathNodeIterator_ElemChildren_NoLocalName(this, nsAtom);
            }

            if (matchSelf)
                return new DocumentXPathNodeIterator_ElemChildren_AndSelf(this, localNameAtom, nsAtom);
            return new DocumentXPathNodeIterator_ElemChildren(this, localNameAtom, nsAtom);
        }

        public override XPathNodeIterator SelectDescendants(XPathNodeType nt, bool includeSelf)
        {
            if (nt == XPathNodeType.Element)
            {
                XmlNodeType curNT = _source.NodeType;
                if (curNT != XmlNodeType.Document && curNT != XmlNodeType.Element)
                {
                    //only Document, Entity, Element node can have Element node as children ( descendant )
                    //entity nodes should be invisible to XPath data model
                    return new DocumentXPathNodeIterator_Empty(this);
                }
                if (includeSelf)
                    return new DocumentXPathNodeIterator_AllElemChildren_AndSelf(this);
                return new DocumentXPathNodeIterator_AllElemChildren(this);
            }
            return base.SelectDescendants(nt, includeSelf);
        }

        public override bool CanEdit
        {
            get
            {
                return true;
            }
        }

        public override XmlWriter PrependChild()
        {
            switch (_source.NodeType)
            {
                case XmlNodeType.Element:
                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                    break;
                default:
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
            }

            DocumentXmlWriter writer = new DocumentXmlWriter(DocumentXmlWriterType.PrependChild, _source, _document);
            writer.NamespaceManager = GetNamespaceManager(_source, _document);
            return new XmlWellFormedWriter(writer, writer.Settings);
        }

        public override XmlWriter AppendChild()
        {
            switch (_source.NodeType)
            {
                case XmlNodeType.Element:
                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                    break;
                default:
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
            }

            DocumentXmlWriter writer = new DocumentXmlWriter(DocumentXmlWriterType.AppendChild, _source, _document);
            writer.NamespaceManager = GetNamespaceManager(_source, _document);
            return new XmlWellFormedWriter(writer, writer.Settings);
        }

        public override XmlWriter InsertAfter()
        {
            XmlNode node = _source;

            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    node = TextEnd(node);
                    break;
                default:
                    break;
            }

            DocumentXmlWriter writer = new DocumentXmlWriter(DocumentXmlWriterType.InsertSiblingAfter, node, _document);
            writer.NamespaceManager = GetNamespaceManager(node.ParentNode, _document);
            return new XmlWellFormedWriter(writer, writer.Settings);
        }

        public override XmlWriter InsertBefore()
        {
            switch (_source.NodeType)
            {
                case XmlNodeType.Attribute:
                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    CalibrateText();

                    break;
                default:
                    break;
            }

            DocumentXmlWriter writer = new DocumentXmlWriter(DocumentXmlWriterType.InsertSiblingBefore, _source, _document);
            writer.NamespaceManager = GetNamespaceManager(_source.ParentNode, _document);
            return new XmlWellFormedWriter(writer, writer.Settings);
        }

        public override XmlWriter CreateAttributes()
        {
            if (_source.NodeType != XmlNodeType.Element)
            {
                throw new InvalidOperationException(SR.Xpn_BadPosition);
            }

            DocumentXmlWriter writer = new DocumentXmlWriter(DocumentXmlWriterType.AppendAttribute, _source, _document);
            writer.NamespaceManager = GetNamespaceManager(_source, _document);
            return new XmlWellFormedWriter(writer, writer.Settings);
        }

        public override XmlWriter ReplaceRange(XPathNavigator lastSiblingToReplace)
        {
            if (!(lastSiblingToReplace is DocumentXPathNavigator that))
            {
                ArgumentNullException.ThrowIfNull(lastSiblingToReplace);
                throw new NotSupportedException();
            }

            this.CalibrateText();
            that.CalibrateText();

            XmlNode node = _source;
            XmlNode end = that._source;

            if (node == end)
            {
                switch (node.NodeType)
                {
                    case XmlNodeType.Attribute:
                    case XmlNodeType.Document:
                    case XmlNodeType.DocumentFragment:
                        throw new InvalidOperationException(SR.Xpn_BadPosition);
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        end = that.TextEnd(end);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (end.IsText)
                {
                    end = that.TextEnd(end);
                }
                if (!IsFollowingSibling(node, end))
                {
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
                }
            }

            DocumentXmlWriter writer = new DocumentXmlWriter(DocumentXmlWriterType.ReplaceToFollowingSibling, node, _document);
            writer.NamespaceManager = GetNamespaceManager(node.ParentNode, _document);
            writer.Navigator = this;
            writer.EndNode = end;
            return new XmlWellFormedWriter(writer, writer.Settings);
        }

        public override void DeleteRange(XPathNavigator lastSiblingToDelete)
        {
            if (!(lastSiblingToDelete is DocumentXPathNavigator that))
            {
                ArgumentNullException.ThrowIfNull(lastSiblingToDelete);
                throw new NotSupportedException();
            }

            this.CalibrateText();
            that.CalibrateText();

            XmlNode node = _source;
            XmlNode end = that._source;

            if (node == end)
            {
                switch (node.NodeType)
                {
                    case XmlNodeType.Attribute:
                        XmlAttribute attribute = (XmlAttribute)node;
                        if (attribute.IsNamespace)
                        {
                            goto default;
                        }
                        XmlNode? parent = OwnerNode(attribute);
                        DeleteAttribute(attribute, _attributeIndex);
                        if (parent != null)
                        {
                            ResetPosition(parent);
                        }
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        end = that.TextEnd(end);
                        goto case XmlNodeType.Element;
                    case XmlNodeType.Element:
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                        parent = OwnerNode(node);
                        DeleteToFollowingSibling(node, end);
                        if (parent != null)
                        {
                            ResetPosition(parent);
                        }
                        break;
                    default:
                        throw new InvalidOperationException(SR.Xpn_BadPosition);
                }
            }
            else
            {
                if (end.IsText)
                {
                    end = that.TextEnd(end);
                }
                if (!IsFollowingSibling(node, end))
                {
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
                }
                XmlNode? parent = OwnerNode(node);
                DeleteToFollowingSibling(node, end);
                if (parent != null)
                {
                    ResetPosition(parent);
                }
            }
        }

        public override void DeleteSelf()
        {
            XmlNode node = _source;
            XmlNode end = node;

            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    XmlAttribute attribute = (XmlAttribute)node;
                    if (attribute.IsNamespace)
                    {
                        goto default;
                    }
                    XmlNode? parent = OwnerNode(attribute);
                    DeleteAttribute(attribute, _attributeIndex);
                    if (parent != null)
                    {
                        ResetPosition(parent);
                    }
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    CalibrateText();

                    node = _source;
                    end = TextEnd(node);
                    goto case XmlNodeType.Element;
                case XmlNodeType.Element:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.Comment:
                    parent = OwnerNode(node);
                    DeleteToFollowingSibling(node, end);
                    if (parent != null)
                    {
                        ResetPosition(parent);
                    }
                    break;
                default:
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
            }
        }

        private static void DeleteAttribute(XmlAttribute attribute, int index)
        {
            XmlAttributeCollection? attributes;

            if (!CheckAttributePosition(attribute, out attributes, index)
                && !ResetAttributePosition(attribute, attributes, out index))
            {
                throw new InvalidOperationException(SR.Xpn_MissingParent);
            }
            if (attribute.IsReadOnly)
            {
                throw new InvalidOperationException(SR.Xdom_Node_Modify_ReadOnly);
            }
            attributes.RemoveAt(index);
        }

        internal static void DeleteToFollowingSibling(XmlNode node, XmlNode end)
        {
            XmlNode? parent = node.ParentNode;

            if (parent == null)
            {
                throw new InvalidOperationException(SR.Xpn_MissingParent);
            }
            if (node.IsReadOnly
                || end.IsReadOnly)
            {
                throw new InvalidOperationException(SR.Xdom_Node_Modify_ReadOnly);
            }
            while (node != end)
            {
                Debug.Assert(node != null, "This method needs to be called with the beforehand check of NextSibling being not null from node to end");
                XmlNode temp = node;
                node = node.NextSibling!;
                parent.RemoveChild(temp);
            }
            parent.RemoveChild(node);
        }

        private static XmlNamespaceManager GetNamespaceManager(XmlNode? node, XmlDocument document)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(document.NameTable);
            List<XmlElement> elements = new List<XmlElement>();

            while (node != null)
            {
                if (node is XmlElement element
                    && element.HasAttributes)
                {
                    elements.Add(element);
                }
                node = node.ParentNode;
            }
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                namespaceManager.PushScope();
                XmlAttributeCollection attributes = elements[i].Attributes;
                for (int j = 0; j < attributes.Count; j++)
                {
                    XmlAttribute attribute = attributes[j];
                    if (attribute.IsNamespace)
                    {
                        string prefix = attribute.Prefix.Length == 0 ? string.Empty : attribute.LocalName;
                        namespaceManager.AddNamespace(prefix, attribute.Value);
                    }
                }
            }
            return namespaceManager;
        }

        [MemberNotNull(nameof(_source))]
        internal void ResetPosition(XmlNode node)
        {
            Debug.Assert(node != null, "Undefined navigator position");
            Debug.Assert(node == _document || node.OwnerDocument == _document, "Navigator switched documents");
            _source = node;
            if (node is XmlAttribute attribute)
            {
                XmlElement? element = attribute.OwnerElement;
                if (element != null)
                {
                    ResetAttributePosition(attribute, element.Attributes, out _attributeIndex);
                    if (attribute.IsNamespace)
                    {
                        _namespaceParent = element;
                    }
                }
            }
        }

        private static bool ResetAttributePosition(XmlAttribute attribute, [NotNullWhen(true)] XmlAttributeCollection? attributes, out int index)
        {
            if (attributes != null)
            {
                for (int i = 0; i < attributes.Count; i++)
                {
                    if (attribute == attributes[i])
                    {
                        index = i;
                        return true;
                    }
                }
            }
            index = 0;
            return false;
        }

        private static bool CheckAttributePosition(XmlAttribute attribute, [NotNullWhen(true)] out XmlAttributeCollection? attributes, int index)
        {
            XmlElement? element = attribute.OwnerElement;
            if (element != null)
            {
                attributes = element.Attributes;
                if (index >= 0
                    && index < attributes.Count
                    && attribute == attributes[index])
                {
                    return true;
                }
            }
            else
            {
                attributes = null;
            }
            return false;
        }

        private void CalibrateText()
        {
            XmlNode? text = PreviousText(_source);
            while (text != null)
            {
                ResetPosition(text);
                text = PreviousText(text);
            }
        }

        private XmlNode? ParentNode(XmlNode node)
        {
            XmlNode? parent = node.ParentNode;

            if (!_document.HasEntityReferences)
            {
                return parent;
            }
            return ParentNodeTail(parent);
        }

        private static XmlNode? ParentNodeTail(XmlNode? parent)
        {
            while (parent != null
                   && parent.NodeType == XmlNodeType.EntityReference)
            {
                parent = parent.ParentNode;
            }
            return parent;
        }

        private XmlNode? FirstChild(XmlNode node)
        {
            XmlNode? child = node.FirstChild;

            if (!_document.HasEntityReferences)
            {
                return child;
            }
            return FirstChildTail(child);
        }

        private static XmlNode? FirstChildTail(XmlNode? child)
        {
            while (child != null
                   && child.NodeType == XmlNodeType.EntityReference)
            {
                child = child.FirstChild;
            }
            return child;
        }

        private XmlNode? NextSibling(XmlNode node)
        {
            XmlNode? sibling = node.NextSibling;

            if (!_document.HasEntityReferences)
            {
                return sibling;
            }
            return NextSiblingTail(node, sibling);
        }

        private static XmlNode? NextSiblingTail(XmlNode node, XmlNode? sibling)
        {
            XmlNode? current = node;
            while (sibling == null)
            {
                current = current.ParentNode;
                if (current == null
                    || current.NodeType != XmlNodeType.EntityReference)
                {
                    return null;
                }
                sibling = current.NextSibling;
            }
            while (sibling != null
                   && sibling.NodeType == XmlNodeType.EntityReference)
            {
                sibling = sibling.FirstChild;
            }
            return sibling;
        }

        private XmlNode? PreviousSibling(XmlNode node)
        {
            XmlNode? sibling = node.PreviousSibling;

            if (!_document.HasEntityReferences)
            {
                return sibling;
            }
            return PreviousSiblingTail(node, sibling);
        }

        private static XmlNode? PreviousSiblingTail(XmlNode node, XmlNode? sibling)
        {
            XmlNode? current = node;
            while (sibling == null)
            {
                current = current.ParentNode;
                if (current == null
                    || current.NodeType != XmlNodeType.EntityReference)
                {
                    return null;
                }
                sibling = current.PreviousSibling;
            }
            while (sibling != null
                   && sibling.NodeType == XmlNodeType.EntityReference)
            {
                sibling = sibling.LastChild;
            }
            return sibling;
        }

        private XmlNode? PreviousText(XmlNode node)
        {
            XmlNode? text = node.PreviousText;

            if (!_document.HasEntityReferences)
            {
                return text;
            }
            return PreviousTextTail(node, text);
        }

        private static XmlNode? PreviousTextTail(XmlNode node, XmlNode? text)
        {
            if (text != null)
            {
                return text;
            }
            if (!node.IsText)
            {
                return null;
            }
            XmlNode? sibling = node.PreviousSibling;
            XmlNode? current = node;
            while (sibling == null)
            {
                current = current.ParentNode;
                if (current == null
                    || current.NodeType != XmlNodeType.EntityReference)
                {
                    return null;
                }
                sibling = current.PreviousSibling;
            }
            while (sibling != null)
            {
                switch (sibling.NodeType)
                {
                    case XmlNodeType.EntityReference:
                        sibling = sibling.LastChild;
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        return sibling;
                    default:
                        return null;
                }
            }
            return null;
        }

        internal static bool IsFollowingSibling(XmlNode left, [NotNullWhen(true)] XmlNode? right)
        {
            XmlNode? currentLeft = left;
            while (true)
            {
                currentLeft = currentLeft.NextSibling;
                if (currentLeft == null)
                {
                    break;
                }
                if (currentLeft == right)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsDescendant(XmlNode top, XmlNode bottom)
        {
            while (true)
            {
                XmlNode? parent = bottom.ParentNode;
                if (parent == null)
                {
                    if (!(bottom is XmlAttribute attribute))
                    {
                        break;
                    }
                    parent = attribute.OwnerElement;
                    if (parent == null)
                    {
                        break;
                    }
                }
                bottom = parent;
                if (top == bottom)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsValidChild(XmlNode parent, XmlNode child)
        {
            switch (parent.NodeType)
            {
                case XmlNodeType.Element:
                    return true;
                case XmlNodeType.DocumentFragment:
                    switch (child.NodeType)
                    {
                        case XmlNodeType.Element:
                        case XmlNodeType.Text:
                        case XmlNodeType.CDATA:
                        case XmlNodeType.ProcessingInstruction:
                        case XmlNodeType.Comment:
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            return true;
                    }
                    break;
                case XmlNodeType.Document:
                    switch (child.NodeType)
                    {
                        case XmlNodeType.Element:
                        case XmlNodeType.ProcessingInstruction:
                        case XmlNodeType.Comment:
                            return true;
                    }
                    break;
                default:
                    break;
            }
            return false;
        }

        private XmlNode TextStart(XmlNode node)
        {
            XmlNode start;
            XmlNode? current = node;

            do
            {
                start = current;
                current = PreviousSibling(current);
            }
            while (current != null
                   && current.IsText);
            return start;
        }

        private XmlNode TextEnd(XmlNode node)
        {
            XmlNode end;
            XmlNode? current = node;

            do
            {
                end = current;
                current = NextSibling(current);
            }
            while (current != null
                   && current.IsText);
            return end;
        }
    }

    // An iterator that matches no nodes
    internal sealed class DocumentXPathNodeIterator_Empty : XPathNodeIterator
    {
        private readonly XPathNavigator _nav;

        internal DocumentXPathNodeIterator_Empty(DocumentXPathNavigator nav) { _nav = nav.Clone(); }
        internal DocumentXPathNodeIterator_Empty(DocumentXPathNodeIterator_Empty other) { _nav = other._nav.Clone(); }
        public override XPathNodeIterator Clone() { return new DocumentXPathNodeIterator_Empty(this); }
        public override bool MoveNext() { return false; }
        public override XPathNavigator Current { get { return _nav; } }
        public override int CurrentPosition { get { return 0; } }
        public override int Count { get { return 0; } }
    }

    // An iterator that can match any child elements that match the Match condition (overridden in the derived class)
    internal abstract class DocumentXPathNodeIterator_ElemDescendants : XPathNodeIterator
    {
        private readonly DocumentXPathNavigator _nav;
        private int _level;
        private int _position;

        internal DocumentXPathNodeIterator_ElemDescendants(DocumentXPathNavigator nav)
        {
            _nav = (DocumentXPathNavigator)(nav.Clone());
            _level = 0;
            _position = 0;
        }
        internal DocumentXPathNodeIterator_ElemDescendants(DocumentXPathNodeIterator_ElemDescendants other)
        {
            _nav = (DocumentXPathNavigator)(other._nav.Clone());
            _level = other._level;
            _position = other._position;
        }

        protected abstract bool Match(XmlNode node);

        public override XPathNavigator Current
        {
            get { return _nav; }
        }

        public override int CurrentPosition
        {
            get { return _position; }
        }

        protected void SetPosition(int pos)
        {
            _position = pos;
        }

        public override bool MoveNext()
        {
            while (true)
            {
                if (_nav.MoveToFirstChild())
                {
                    _level++;
                }
                else
                {
                    if (_level == 0)
                    {
                        return false;
                    }
                    while (!_nav.MoveToNext())
                    {
                        _level--;
                        if (_level == 0)
                        {
                            return false;
                        }
                        if (!_nav.MoveToParent())
                        {
                            return false;
                        }
                    }
                }
                XmlNode node = (XmlNode)_nav.UnderlyingObject;
                if (node.NodeType == XmlNodeType.Element && Match(node))
                {
                    _position++;
                    return true;
                }
            }
        }
    }

    // Iterate over all element children irrespective of the localName and namespace
    internal class DocumentXPathNodeIterator_AllElemChildren : DocumentXPathNodeIterator_ElemDescendants
    {
        internal DocumentXPathNodeIterator_AllElemChildren(DocumentXPathNavigator nav) : base(nav)
        {
            Debug.Assert(((XmlNode)nav.UnderlyingObject).NodeType != XmlNodeType.Attribute);
        }
        internal DocumentXPathNodeIterator_AllElemChildren(DocumentXPathNodeIterator_AllElemChildren other) : base(other)
        {
        }

        public override XPathNodeIterator Clone()
        {
            return new DocumentXPathNodeIterator_AllElemChildren(this);
        }

        protected override bool Match(XmlNode node)
        {
            Debug.Assert(node != null);
            return (node.NodeType == XmlNodeType.Element);
        }
    }
    // Iterate over all element children irrespective of the localName and namespace, include the self node when testing for localName/ns
    internal sealed class DocumentXPathNodeIterator_AllElemChildren_AndSelf : DocumentXPathNodeIterator_AllElemChildren
    {
        internal DocumentXPathNodeIterator_AllElemChildren_AndSelf(DocumentXPathNavigator nav) : base(nav)
        {
        }
        internal DocumentXPathNodeIterator_AllElemChildren_AndSelf(DocumentXPathNodeIterator_AllElemChildren_AndSelf other) : base(other)
        {
        }

        public override XPathNodeIterator Clone()
        {
            return new DocumentXPathNodeIterator_AllElemChildren_AndSelf(this);
        }

        public override bool MoveNext()
        {
            if (CurrentPosition == 0)
            {
                DocumentXPathNavigator nav = (DocumentXPathNavigator)this.Current;
                XmlNode node = (XmlNode)nav.UnderlyingObject;
                if (node.NodeType == XmlNodeType.Element && Match(node))
                {
                    SetPosition(1);
                    return true;
                }
            }
            return base.MoveNext();
        }
    }
    // Iterate over all element children that have a given namespace but irrespective of the localName
    internal class DocumentXPathNodeIterator_ElemChildren_NoLocalName : DocumentXPathNodeIterator_ElemDescendants
    {
        private readonly string _nsAtom;

        internal DocumentXPathNodeIterator_ElemChildren_NoLocalName(DocumentXPathNavigator nav, string nsAtom) : base(nav)
        {
            Debug.Assert(((XmlNode)nav.UnderlyingObject).NodeType != XmlNodeType.Attribute);
            Debug.Assert(Ref.Equal(nav.NameTable.Get(nsAtom), nsAtom));
            _nsAtom = nsAtom;
        }
        internal DocumentXPathNodeIterator_ElemChildren_NoLocalName(DocumentXPathNodeIterator_ElemChildren_NoLocalName other) : base(other)
        {
            _nsAtom = other._nsAtom;
        }
        public override XPathNodeIterator Clone()
        {
            return new DocumentXPathNodeIterator_ElemChildren_NoLocalName(this);
        }

        protected override bool Match(XmlNode node)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.NodeType == XmlNodeType.Element);
            return Ref.Equal(node.NamespaceURI, _nsAtom);
        }
    }
    // Iterate over all element children that have a given namespace but irrespective of the localName, include self node when checking for ns
    internal sealed class DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName : DocumentXPathNodeIterator_ElemChildren_NoLocalName
    {
        internal DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName(DocumentXPathNavigator nav, string nsAtom) : base(nav, nsAtom)
        {
        }
        internal DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName(DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName other) : base(other)
        {
        }

        public override XPathNodeIterator Clone()
        {
            return new DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName(this);
        }

        public override bool MoveNext()
        {
            if (CurrentPosition == 0)
            {
                DocumentXPathNavigator nav = (DocumentXPathNavigator)this.Current;
                XmlNode node = (XmlNode)nav.UnderlyingObject;
                if (node.NodeType == XmlNodeType.Element && Match(node))
                {
                    SetPosition(1);
                    return true;
                }
            }
            return base.MoveNext();
        }
    }
    // Iterate over all element children that have a given name and namespace
    internal class DocumentXPathNodeIterator_ElemChildren : DocumentXPathNodeIterator_ElemDescendants
    {
        protected string localNameAtom;
        protected string nsAtom;

        internal DocumentXPathNodeIterator_ElemChildren(DocumentXPathNavigator nav, string localNameAtom, string nsAtom) : base(nav)
        {
            Debug.Assert(((XmlNode)nav.UnderlyingObject).NodeType != XmlNodeType.Attribute);
            Debug.Assert(Ref.Equal(nav.NameTable.Get(localNameAtom), localNameAtom));
            Debug.Assert(Ref.Equal(nav.NameTable.Get(nsAtom), nsAtom));
            Debug.Assert(localNameAtom.Length > 0);   // Use DocumentXPathNodeIterator_ElemChildren_NoLocalName class for special magic value of localNameAtom

            this.localNameAtom = localNameAtom;
            this.nsAtom = nsAtom;
        }

        internal DocumentXPathNodeIterator_ElemChildren(DocumentXPathNodeIterator_ElemChildren other) : base(other)
        {
            this.localNameAtom = other.localNameAtom;
            this.nsAtom = other.nsAtom;
        }

        public override XPathNodeIterator Clone()
        {
            return new DocumentXPathNodeIterator_ElemChildren(this);
        }

        protected override bool Match(XmlNode node)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.NodeType == XmlNodeType.Element);
            return Ref.Equal(node.LocalName, localNameAtom) && Ref.Equal(node.NamespaceURI, nsAtom);
        }
    }
    // Iterate over all elem children and itself and check for the given localName (including the magic value "") and namespace
    internal sealed class DocumentXPathNodeIterator_ElemChildren_AndSelf : DocumentXPathNodeIterator_ElemChildren
    {
        internal DocumentXPathNodeIterator_ElemChildren_AndSelf(DocumentXPathNavigator nav, string localNameAtom, string nsAtom)
            : base(nav, localNameAtom, nsAtom)
        {
            Debug.Assert(localNameAtom.Length > 0);   // Use DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName if localName == String.Empty
        }
        internal DocumentXPathNodeIterator_ElemChildren_AndSelf(DocumentXPathNodeIterator_ElemChildren_AndSelf other) : base(other)
        {
        }

        public override XPathNodeIterator Clone()
        {
            return new DocumentXPathNodeIterator_ElemChildren_AndSelf(this);
        }

        public override bool MoveNext()
        {
            if (CurrentPosition == 0)
            {
                DocumentXPathNavigator nav = (DocumentXPathNavigator)this.Current;
                XmlNode node = (XmlNode)nav.UnderlyingObject;
                if (node.NodeType == XmlNodeType.Element && Match(node))
                {
                    SetPosition(1);
                    return true;
                }
            }
            return base.MoveNext();
        }
    }
}
