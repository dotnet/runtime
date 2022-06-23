// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Schema;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Xml;
using MS.Internal.Xml.Cache;
using MS.Internal.Xml.XPath;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.XPath
{
    // Provides a navigation interface API using XPath data model.
    [DebuggerDisplay("{debuggerDisplayProxy}")]
    public abstract class XPathNavigator : XPathItem, ICloneable, IXPathNavigable, IXmlNamespaceResolver
    {
        internal static readonly XPathNavigatorKeyComparer comparer = new XPathNavigatorKeyComparer();

        //-----------------------------------------------
        // Object
        //-----------------------------------------------

        public override string ToString()
        {
            return Value;
        }

        //-----------------------------------------------
        // XPathItem
        //-----------------------------------------------

        public sealed override bool IsNode
        {
            get { return true; }
        }

        public override XmlSchemaType? XmlType
        {
            get
            {
                IXmlSchemaInfo? schemaInfo = SchemaInfo;
                if (schemaInfo != null)
                {
                    if (schemaInfo.Validity == XmlSchemaValidity.Valid)
                    {
                        XmlSchemaType? memberType = schemaInfo.MemberType;
                        if (memberType != null)
                        {
                            return memberType;
                        }
                        return schemaInfo.SchemaType;
                    }
                }
                return null;
            }
        }

        public virtual void SetValue(string value)
        {
            throw new NotSupportedException();
        }

        public override object TypedValue
        {
            get
            {
                IXmlSchemaInfo? schemaInfo = SchemaInfo;
                XmlSchemaType? schemaType;
                XmlSchemaDatatype? datatype;
                if (schemaInfo != null)
                {
                    if (schemaInfo.Validity == XmlSchemaValidity.Valid)
                    {
                        schemaType = schemaInfo.MemberType;
                        if (schemaType == null)
                        {
                            schemaType = schemaInfo.SchemaType;
                        }
                        if (schemaType != null)
                        {
                            datatype = schemaType.Datatype;
                            if (datatype != null)
                            {
                                return schemaType.ValueConverter.ChangeType(Value, datatype.ValueType, this);
                            }
                        }
                    }
                    else
                    {
                        schemaType = schemaInfo.SchemaType;
                        if (schemaType != null)
                        {
                            datatype = schemaType.Datatype;
                            if (datatype != null)
                            {
                                return schemaType.ValueConverter.ChangeType(datatype.ParseValue(Value, NameTable, this), datatype.ValueType, this);
                            }
                        }
                    }
                }
                return Value;
            }
        }

        public virtual void SetTypedValue(object typedValue)
        {
            ArgumentNullException.ThrowIfNull(typedValue);

            switch (NodeType)
            {
                case XPathNodeType.Element:
                case XPathNodeType.Attribute:
                    break;
                default:
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
            }
            string? value = null;
            IXmlSchemaInfo? schemaInfo = SchemaInfo;
            if (schemaInfo != null)
            {
                XmlSchemaType? schemaType = schemaInfo.SchemaType;
                if (schemaType != null)
                {
                    value = schemaType.ValueConverter.ToString(typedValue, this);
                    XmlSchemaDatatype? datatype = schemaType.Datatype;
                    if (datatype != null)
                    {
                        datatype.ParseValue(value, NameTable, this);
                    }
                }
            }
            if (value == null)
            {
                value = XmlUntypedConverter.Untyped.ToString(typedValue, this);
            }
            SetValue(value);
        }

        public override Type ValueType
        {
            get
            {
                IXmlSchemaInfo? schemaInfo = SchemaInfo;
                XmlSchemaType? schemaType;
                XmlSchemaDatatype? datatype;
                if (schemaInfo != null)
                {
                    if (schemaInfo.Validity == XmlSchemaValidity.Valid)
                    {
                        schemaType = schemaInfo.MemberType;
                        if (schemaType == null)
                        {
                            schemaType = schemaInfo.SchemaType;
                        }
                        if (schemaType != null)
                        {
                            datatype = schemaType.Datatype;
                            if (datatype != null)
                            {
                                return datatype.ValueType;
                            }
                        }
                    }
                    else
                    {
                        schemaType = schemaInfo.SchemaType;
                        if (schemaType != null)
                        {
                            datatype = schemaType.Datatype;
                            if (datatype != null)
                            {
                                return datatype.ValueType;
                            }
                        }
                    }
                }
                return typeof(string);
            }
        }

        public override bool ValueAsBoolean
        {
            get
            {
                IXmlSchemaInfo? schemaInfo = SchemaInfo;
                XmlSchemaType? schemaType;
                XmlSchemaDatatype? datatype;
                if (schemaInfo != null)
                {
                    if (schemaInfo.Validity == XmlSchemaValidity.Valid)
                    {
                        schemaType = schemaInfo.MemberType;
                        if (schemaType == null)
                        {
                            schemaType = schemaInfo.SchemaType;
                        }
                        if (schemaType != null)
                        {
                            return schemaType.ValueConverter.ToBoolean(Value);
                        }
                    }
                    else
                    {
                        schemaType = schemaInfo.SchemaType;
                        if (schemaType != null)
                        {
                            datatype = schemaType.Datatype;
                            if (datatype != null)
                            {
                                return schemaType.ValueConverter.ToBoolean(datatype.ParseValue(Value, NameTable, this));
                            }
                        }
                    }
                }
                return XmlUntypedConverter.Untyped.ToBoolean(Value);
            }
        }

        public override DateTime ValueAsDateTime
        {
            get
            {
                IXmlSchemaInfo? schemaInfo = SchemaInfo;
                XmlSchemaType? schemaType;
                XmlSchemaDatatype? datatype;
                if (schemaInfo != null)
                {
                    if (schemaInfo.Validity == XmlSchemaValidity.Valid)
                    {
                        schemaType = schemaInfo.MemberType;
                        if (schemaType == null)
                        {
                            schemaType = schemaInfo.SchemaType;
                        }
                        if (schemaType != null)
                        {
                            return schemaType.ValueConverter.ToDateTime(Value);
                        }
                    }
                    else
                    {
                        schemaType = schemaInfo.SchemaType;
                        if (schemaType != null)
                        {
                            datatype = schemaType.Datatype;
                            if (datatype != null)
                            {
                                return schemaType.ValueConverter.ToDateTime(datatype.ParseValue(Value, NameTable, this));
                            }
                        }
                    }
                }
                return XmlUntypedConverter.Untyped.ToDateTime(Value);
            }
        }

        public override double ValueAsDouble
        {
            get
            {
                IXmlSchemaInfo? schemaInfo = SchemaInfo;
                XmlSchemaType? schemaType;
                XmlSchemaDatatype? datatype;
                if (schemaInfo != null)
                {
                    if (schemaInfo.Validity == XmlSchemaValidity.Valid)
                    {
                        schemaType = schemaInfo.MemberType;
                        if (schemaType == null)
                        {
                            schemaType = schemaInfo.SchemaType;
                        }
                        if (schemaType != null)
                        {
                            return schemaType.ValueConverter.ToDouble(Value);
                        }
                    }
                    else
                    {
                        schemaType = schemaInfo.SchemaType;
                        if (schemaType != null)
                        {
                            datatype = schemaType.Datatype;
                            if (datatype != null)
                            {
                                return schemaType.ValueConverter.ToDouble(datatype.ParseValue(Value, NameTable, this));
                            }
                        }
                    }
                }
                return XmlUntypedConverter.Untyped.ToDouble(Value);
            }
        }

        public override int ValueAsInt
        {
            get
            {
                IXmlSchemaInfo? schemaInfo = SchemaInfo;
                XmlSchemaType? schemaType;
                XmlSchemaDatatype? datatype;
                if (schemaInfo != null)
                {
                    if (schemaInfo.Validity == XmlSchemaValidity.Valid)
                    {
                        schemaType = schemaInfo.MemberType;
                        if (schemaType == null)
                        {
                            schemaType = schemaInfo.SchemaType;
                        }
                        if (schemaType != null)
                        {
                            return schemaType.ValueConverter.ToInt32(Value);
                        }
                    }
                    else
                    {
                        schemaType = schemaInfo.SchemaType;
                        if (schemaType != null)
                        {
                            datatype = schemaType.Datatype;
                            if (datatype != null)
                            {
                                return schemaType.ValueConverter.ToInt32(datatype.ParseValue(Value, NameTable, this));
                            }
                        }
                    }
                }
                return XmlUntypedConverter.Untyped.ToInt32(Value);
            }
        }

        public override long ValueAsLong
        {
            get
            {
                IXmlSchemaInfo? schemaInfo = SchemaInfo;
                XmlSchemaType? schemaType;
                XmlSchemaDatatype? datatype;
                if (schemaInfo != null)
                {
                    if (schemaInfo.Validity == XmlSchemaValidity.Valid)
                    {
                        schemaType = schemaInfo.MemberType;
                        if (schemaType == null)
                        {
                            schemaType = schemaInfo.SchemaType;
                        }
                        if (schemaType != null)
                        {
                            return schemaType.ValueConverter.ToInt64(Value);
                        }
                    }
                    else
                    {
                        schemaType = schemaInfo.SchemaType;
                        if (schemaType != null)
                        {
                            datatype = schemaType.Datatype;
                            if (datatype != null)
                            {
                                return schemaType.ValueConverter.ToInt64(datatype.ParseValue(Value, NameTable, this));
                            }
                        }
                    }
                }
                return XmlUntypedConverter.Untyped.ToInt64(Value);
            }
        }

        public override object ValueAs(Type returnType, IXmlNamespaceResolver? nsResolver)
        {
            if (nsResolver == null)
            {
                nsResolver = this;
            }
            IXmlSchemaInfo? schemaInfo = SchemaInfo;
            XmlSchemaType? schemaType;
            XmlSchemaDatatype? datatype;
            if (schemaInfo != null)
            {
                if (schemaInfo.Validity == XmlSchemaValidity.Valid)
                {
                    schemaType = schemaInfo.MemberType;
                    if (schemaType == null)
                    {
                        schemaType = schemaInfo.SchemaType;
                    }
                    if (schemaType != null)
                    {
                        return schemaType.ValueConverter.ChangeType(Value, returnType, nsResolver);
                    }
                }
                else
                {
                    schemaType = schemaInfo.SchemaType;
                    if (schemaType != null)
                    {
                        datatype = schemaType.Datatype;
                        if (datatype != null)
                        {
                            return schemaType.ValueConverter.ChangeType(datatype.ParseValue(Value, NameTable, nsResolver), returnType, nsResolver);
                        }
                    }
                }
            }
            return XmlUntypedConverter.Untyped.ChangeType(Value, returnType, nsResolver);
        }

        //-----------------------------------------------
        // ICloneable
        //-----------------------------------------------

        object ICloneable.Clone()
        {
            return Clone();
        }

        //-----------------------------------------------
        // IXPathNavigable
        //-----------------------------------------------

        public virtual XPathNavigator CreateNavigator()
        {
            return Clone();
        }

        //-----------------------------------------------
        // IXmlNamespaceResolver
        //-----------------------------------------------

        public abstract XmlNameTable NameTable { get; }

        public virtual string? LookupNamespace(string prefix)
        {
            if (prefix == null)
                return null;

            if (NodeType != XPathNodeType.Element)
            {
                XPathNavigator navSave = Clone();

                // If current item is not an element, then try parent
                if (navSave.MoveToParent())
                    return navSave.LookupNamespace(prefix);
            }
            else if (MoveToNamespace(prefix))
            {
                string namespaceURI = Value;
                MoveToParent();
                return namespaceURI;
            }

            // Check for "", "xml", and "xmlns" prefixes
            if (prefix.Length == 0)
                return string.Empty;
            else if (prefix == "xml")
                return XmlReservedNs.NsXml;
            else if (prefix == "xmlns")
                return XmlReservedNs.NsXmlNs;

            return null;
        }

        public virtual string? LookupPrefix(string namespaceURI)
        {
            if (namespaceURI == null)
                return null;

            XPathNavigator navClone = Clone();

            if (NodeType != XPathNodeType.Element)
            {
                // If current item is not an element, then try parent
                if (navClone.MoveToParent())
                    return navClone.LookupPrefix(namespaceURI);
            }
            else
            {
                if (navClone.MoveToFirstNamespace(XPathNamespaceScope.All))
                {
                    // Loop until a matching namespace is found
                    do
                    {
                        if (namespaceURI == navClone.Value)
                            return navClone.LocalName;
                    }
                    while (navClone.MoveToNextNamespace(XPathNamespaceScope.All));
                }
            }

            // Check for default, "xml", and "xmlns" namespaces
            if (namespaceURI == LookupNamespace(string.Empty))
                return string.Empty;
            else if (namespaceURI == XmlReservedNs.NsXml)
                return "xml";
            else if (namespaceURI == XmlReservedNs.NsXmlNs)
                return "xmlns";

            return null;
        }

        public virtual IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            XPathNodeType nt = NodeType;
            if ((nt != XPathNodeType.Element && scope != XmlNamespaceScope.Local) || nt == XPathNodeType.Attribute || nt == XPathNodeType.Namespace)
            {
                XPathNavigator navSave = Clone();

                // If current item is not an element, then try parent
                if (navSave.MoveToParent())
                    return navSave.GetNamespacesInScope(scope);
            }

            Dictionary<string, string> dict = new Dictionary<string, string>();

            // "xml" prefix always in scope
            if (scope == XmlNamespaceScope.All)
                dict["xml"] = XmlReservedNs.NsXml;

            // Now add all in-scope namespaces
            if (MoveToFirstNamespace((XPathNamespaceScope)scope))
            {
                do
                {
                    string prefix = LocalName;
                    string ns = Value;

                    // Exclude xmlns="" declarations unless scope = Local
                    if (prefix.Length != 0 || ns.Length != 0 || scope == XmlNamespaceScope.Local)
                        dict[prefix] = ns;
                }
                while (MoveToNextNamespace((XPathNamespaceScope)scope));

                MoveToParent();
            }

            return dict;
        }

        //-----------------------------------------------
        // XPathNavigator
        //-----------------------------------------------

        // Returns an object of type IKeyComparer. Using this the navigators can be hashed
        // on the basis of actual position it represents rather than the clr reference of
        // the navigator object.
        public static IEqualityComparer NavigatorComparer
        {
            get { return comparer; }
        }

        public abstract XPathNavigator Clone();

        public abstract XPathNodeType NodeType { get; }

        public abstract string LocalName { get; }

        public abstract string Name { get; }

        public abstract string NamespaceURI { get; }

        public abstract string Prefix { get; }

        public abstract string BaseURI { get; }

        public abstract bool IsEmptyElement { get; }

        public virtual string XmlLang
        {
            get
            {
                XPathNavigator navClone = Clone();
                do
                {
                    if (navClone.MoveToAttribute("lang", XmlReservedNs.NsXml))
                        return navClone.Value;
                }
                while (navClone.MoveToParent());

                return string.Empty;
            }
        }

        public virtual XmlReader ReadSubtree()
        {
            switch (NodeType)
            {
                case XPathNodeType.Root:
                case XPathNodeType.Element:
                    break;
                default:
                    throw new InvalidOperationException(SR.Xpn_BadPosition);
            }
            return CreateReader();
        }

        public virtual void WriteSubtree(XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteNode(this, true);
        }

        public virtual object? UnderlyingObject
        {
            get { return null; }
        }

        public virtual bool HasAttributes
        {
            get
            {
                if (!MoveToFirstAttribute())
                    return false;

                MoveToParent();
                return true;
            }
        }

        public virtual string GetAttribute(string localName, string namespaceURI)
        {
            string value;

            if (!MoveToAttribute(localName, namespaceURI))
                return "";

            value = Value;
            MoveToParent();

            return value;
        }

        public virtual bool MoveToAttribute(string localName, string namespaceURI)
        {
            if (MoveToFirstAttribute())
            {
                do
                {
                    if (localName == LocalName && namespaceURI == NamespaceURI)
                        return true;
                }
                while (MoveToNextAttribute());

                MoveToParent();
            }

            return false;
        }

        public abstract bool MoveToFirstAttribute();

        public abstract bool MoveToNextAttribute();

        public virtual string GetNamespace(string name)
        {
            string value;

            if (!MoveToNamespace(name))
            {
                if (name == "xml")
                    return XmlReservedNs.NsXml;
                if (name == "xmlns")
                    return XmlReservedNs.NsXmlNs;
                return string.Empty;
            }

            value = Value;
            MoveToParent();

            return value;
        }

        public virtual bool MoveToNamespace(string name)
        {
            if (MoveToFirstNamespace(XPathNamespaceScope.All))
            {
                do
                {
                    if (name == LocalName)
                        return true;
                }
                while (MoveToNextNamespace(XPathNamespaceScope.All));

                MoveToParent();
            }

            return false;
        }

        public abstract bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope);

        public abstract bool MoveToNextNamespace(XPathNamespaceScope namespaceScope);

        public bool MoveToFirstNamespace() { return MoveToFirstNamespace(XPathNamespaceScope.All); }

        public bool MoveToNextNamespace() { return MoveToNextNamespace(XPathNamespaceScope.All); }

        public abstract bool MoveToNext();

        public abstract bool MoveToPrevious();

        public virtual bool MoveToFirst()
        {
            switch (NodeType)
            {
                case XPathNodeType.Attribute:
                case XPathNodeType.Namespace:
                    // MoveToFirst should only succeed for content-typed nodes
                    return false;
            }

            if (!MoveToParent())
                return false;

            return MoveToFirstChild();
        }

        public abstract bool MoveToFirstChild();

        public abstract bool MoveToParent();

        public virtual void MoveToRoot()
        {
            while (MoveToParent())
                ;
        }

        public abstract bool MoveTo(XPathNavigator other);

        public abstract bool MoveToId(string id);

        public virtual bool MoveToChild(string localName, string namespaceURI)
        {
            if (MoveToFirstChild())
            {
                do
                {
                    if (NodeType == XPathNodeType.Element && localName == LocalName && namespaceURI == NamespaceURI)
                        return true;
                }
                while (MoveToNext());
                MoveToParent();
            }

            return false;
        }

        public virtual bool MoveToChild(XPathNodeType type)
        {
            if (MoveToFirstChild())
            {
                int mask = GetContentKindMask(type);
                do
                {
                    if (((1 << (int)NodeType) & mask) != 0)
                        return true;
                }
                while (MoveToNext());

                MoveToParent();
            }

            return false;
        }

        public virtual bool MoveToFollowing(string localName, string namespaceURI)
        {
            return MoveToFollowing(localName, namespaceURI, null);
        }

        public virtual bool MoveToFollowing(string localName, string namespaceURI, XPathNavigator? end)
        {
            XPathNavigator navSave = Clone();

            if (end != null)
            {
                switch (end.NodeType)
                {
                    case XPathNodeType.Attribute:
                    case XPathNodeType.Namespace:
                        // Scan until we come to the next content-typed node
                        // after the attribute or namespace node
                        end = end.Clone();
                        end.MoveToNonDescendant();
                        break;
                }
            }
            switch (NodeType)
            {
                case XPathNodeType.Attribute:
                case XPathNodeType.Namespace:
                    if (!MoveToParent())
                    {
                        return false;
                    }
                    break;
            }
            do
            {
                if (!MoveToFirstChild())
                {
                    // Look for next sibling
                    while (true)
                    {
                        if (MoveToNext())
                            break;

                        if (!MoveToParent())
                        {
                            // Restore previous position and return false
                            MoveTo(navSave);
                            return false;
                        }
                    }
                }

                // Have we reached the end of the scan?
                if (end != null && IsSamePosition(end))
                {
                    // Restore previous position and return false
                    MoveTo(navSave);
                    return false;
                }
            }
            while (NodeType != XPathNodeType.Element
                   || localName != LocalName
                   || namespaceURI != NamespaceURI);

            return true;
        }

        public virtual bool MoveToFollowing(XPathNodeType type)
        {
            return MoveToFollowing(type, null);
        }

        public virtual bool MoveToFollowing(XPathNodeType type, XPathNavigator? end)
        {
            XPathNavigator navSave = Clone();
            int mask = GetContentKindMask(type);

            if (end != null)
            {
                switch (end.NodeType)
                {
                    case XPathNodeType.Attribute:
                    case XPathNodeType.Namespace:
                        // Scan until we come to the next content-typed node
                        // after the attribute or namespace node
                        end = end.Clone();
                        end.MoveToNonDescendant();
                        break;
                }
            }
            switch (NodeType)
            {
                case XPathNodeType.Attribute:
                case XPathNodeType.Namespace:
                    if (!MoveToParent())
                    {
                        return false;
                    }
                    break;
            }
            do
            {
                if (!MoveToFirstChild())
                {
                    // Look for next sibling
                    while (true)
                    {
                        if (MoveToNext())
                            break;

                        if (!MoveToParent())
                        {
                            // Restore previous position and return false
                            MoveTo(navSave);
                            return false;
                        }
                    }
                }

                // Have we reached the end of the scan?
                if (end != null && IsSamePosition(end))
                {
                    // Restore previous position and return false
                    MoveTo(navSave);
                    return false;
                }
            }
            while (((1 << (int)NodeType) & mask) == 0);

            return true;
        }

        public virtual bool MoveToNext(string localName, string namespaceURI)
        {
            XPathNavigator navClone = Clone();

            while (MoveToNext())
            {
                if (NodeType == XPathNodeType.Element && localName == LocalName && namespaceURI == NamespaceURI)
                    return true;
            }
            MoveTo(navClone);
            return false;
        }

        public virtual bool MoveToNext(XPathNodeType type)
        {
            XPathNavigator navClone = Clone();
            int mask = GetContentKindMask(type);

            while (MoveToNext())
            {
                if (((1 << (int)NodeType) & mask) != 0)
                    return true;
            }

            MoveTo(navClone);
            return false;
        }

        public virtual bool HasChildren
        {
            get
            {
                if (MoveToFirstChild())
                {
                    MoveToParent();
                    return true;
                }
                return false;
            }
        }

        public abstract bool IsSamePosition(XPathNavigator other);

        public virtual bool IsDescendant([NotNullWhen(true)] XPathNavigator? nav)
        {
            if (nav != null)
            {
                nav = nav.Clone();
                while (nav.MoveToParent())
                    if (nav.IsSamePosition(this))
                        return true;
            }
            return false;
        }

        public virtual XmlNodeOrder ComparePosition(XPathNavigator? nav)
        {
            if (nav == null)
            {
                return XmlNodeOrder.Unknown;
            }

            if (IsSamePosition(nav))
                return XmlNodeOrder.Same;

            XPathNavigator n1 = this.Clone();
            XPathNavigator n2 = nav.Clone();

            int depth1 = GetDepth(n1.Clone());
            int depth2 = GetDepth(n2.Clone());

            if (depth1 > depth2)
            {
                while (depth1 > depth2)
                {
                    n1.MoveToParent();
                    depth1--;
                }
                if (n1.IsSamePosition(n2))
                    return XmlNodeOrder.After;
            }

            if (depth2 > depth1)
            {
                while (depth2 > depth1)
                {
                    n2.MoveToParent();
                    depth2--;
                }
                if (n1.IsSamePosition(n2))
                    return XmlNodeOrder.Before;
            }

            XPathNavigator parent1 = n1.Clone();
            XPathNavigator parent2 = n2.Clone();

            while (true)
            {
                if (!parent1.MoveToParent() || !parent2.MoveToParent())
                    return XmlNodeOrder.Unknown;

                if (parent1.IsSamePosition(parent2))
                {
                    if (n1.GetType().ToString() != "Microsoft.VisualStudio.Modeling.StoreNavigator")
                    {
                        Debug.Assert(CompareSiblings(n1.Clone(), n2.Clone()) != CompareSiblings(n2.Clone(), n1.Clone()), "IsSamePosition() on custom navigator returns inconsistent results");
                    }
                    return CompareSiblings(n1, n2);
                }

                n1.MoveToParent();
                n2.MoveToParent();
            }
        }

        public virtual IXmlSchemaInfo? SchemaInfo
        {
            get { return this as IXmlSchemaInfo; }
        }

        public virtual bool CheckValidity(XmlSchemaSet schemas, ValidationEventHandler validationEventHandler)
        {
            IXmlSchemaInfo? schemaInfo;
            XmlSchemaType? schemaType = null;
            XmlSchemaElement? schemaElement = null;
            XmlSchemaAttribute? schemaAttribute = null;

            switch (NodeType)
            {
                case XPathNodeType.Root:
                    if (schemas == null)
                    {
                        throw new InvalidOperationException(SR.XPathDocument_MissingSchemas);
                    }
                    schemaType = null;
                    break;
                case XPathNodeType.Element:
                    if (schemas == null)
                    {
                        throw new InvalidOperationException(SR.XPathDocument_MissingSchemas);
                    }
                    schemaInfo = SchemaInfo;
                    if (schemaInfo != null)
                    {
                        schemaType = schemaInfo.SchemaType;
                        schemaElement = schemaInfo.SchemaElement;
                    }
                    if (schemaType == null
                        && schemaElement == null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XPathDocument_NotEnoughSchemaInfo, null));
                    }
                    break;
                case XPathNodeType.Attribute:
                    if (schemas == null)
                    {
                        throw new InvalidOperationException(SR.XPathDocument_MissingSchemas);
                    }
                    schemaInfo = SchemaInfo;
                    if (schemaInfo != null)
                    {
                        schemaType = schemaInfo.SchemaType;
                        schemaAttribute = schemaInfo.SchemaAttribute;
                    }
                    if (schemaType == null
                        && schemaAttribute == null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XPathDocument_NotEnoughSchemaInfo, null));
                    }
                    break;
                default:
                    throw new InvalidOperationException(SR.Format(SR.XPathDocument_ValidateInvalidNodeType, null));
            }

            Debug.Assert(schemaType != null || this.NodeType == XPathNodeType.Root, "schemaType != null  || this.NodeType == XPathNodeType.Root");

            XPathNavigatorReader reader = (XPathNavigatorReader)CreateReader();

            CheckValidityHelper validityTracker = new CheckValidityHelper(validationEventHandler, reader);
            validationEventHandler = new ValidationEventHandler(validityTracker.ValidationCallback);
            XmlReader validatingReader = GetValidatingReader(reader, schemas, validationEventHandler, schemaType, schemaElement, schemaAttribute);

            while (validatingReader.Read())
                ;

            return validityTracker.IsValid;
        }

        private static XmlReader GetValidatingReader(XmlReader reader, XmlSchemaSet schemas, ValidationEventHandler validationEvent, XmlSchemaType? schemaType, XmlSchemaElement? schemaElement, XmlSchemaAttribute? schemaAttribute)
        {
            if (schemaAttribute != null)
            {
                return schemaAttribute.Validate(reader, null, schemas, validationEvent);
            }
            else if (schemaElement != null)
            {
                return schemaElement.Validate(reader, null, schemas, validationEvent);
            }
            else if (schemaType != null)
            {
                return schemaType.Validate(reader, null, schemas, validationEvent);
            }
            Debug.Assert(schemas != null, "schemas != null");
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Auto;
            readerSettings.ValidationType = ValidationType.Schema;
            readerSettings.Schemas = schemas;
            readerSettings.ValidationEventHandler += validationEvent;
            return XmlReader.Create(reader, readerSettings);
        }

        private sealed class CheckValidityHelper
        {
            private bool _isValid;
            private readonly ValidationEventHandler _nextEventHandler;
            private readonly XPathNavigatorReader _reader;

            internal CheckValidityHelper(ValidationEventHandler nextEventHandler, XPathNavigatorReader reader)
            {
                _isValid = true;
                _nextEventHandler = nextEventHandler;
                _reader = reader;
            }

            internal void ValidationCallback(object? sender, ValidationEventArgs args)
            {
                Debug.Assert(args != null);
                if (args.Severity == XmlSeverityType.Error)
                    _isValid = false;
                XmlSchemaValidationException? exception = args.Exception as XmlSchemaValidationException;
                if (exception != null && _reader != null)
                    exception.SetSourceObject(_reader.UnderlyingObject);

                if (_nextEventHandler != null)
                {
                    _nextEventHandler(sender, args);
                }
                else if (exception != null && args.Severity == XmlSeverityType.Error)
                {
                    throw exception;
                }
            }

            internal bool IsValid
            {
                get { return _isValid; }
            }
        }

        public virtual XPathExpression Compile(string xpath)
        {
            return XPathExpression.Compile(xpath);
        }

        public virtual XPathNavigator? SelectSingleNode(string xpath)
        {
            return SelectSingleNode(XPathExpression.Compile(xpath));
        }

        public virtual XPathNavigator? SelectSingleNode(string xpath, IXmlNamespaceResolver? resolver)
        {
            return SelectSingleNode(XPathExpression.Compile(xpath, resolver));
        }

        public virtual XPathNavigator? SelectSingleNode(XPathExpression expression)
        {
            // PERF BUG: this actually caches _all_ matching nodes...
            XPathNodeIterator iter = this.Select(expression);
            if (iter.MoveNext())
            {
                return iter.Current;
            }
            return null;
        }

        public virtual XPathNodeIterator Select(string xpath)
        {
            return this.Select(XPathExpression.Compile(xpath));
        }

        public virtual XPathNodeIterator Select(string xpath, IXmlNamespaceResolver? resolver)
        {
            return this.Select(XPathExpression.Compile(xpath, resolver));
        }

        public virtual XPathNodeIterator Select(XPathExpression expr)
        {
            XPathNodeIterator? result = Evaluate(expr) as XPathNodeIterator;
            if (result == null)
            {
                throw XPathException.Create(SR.Xp_NodeSetExpected);
            }
            return result;
        }

        public virtual object Evaluate(string xpath)
        {
            return Evaluate(XPathExpression.Compile(xpath), null);
        }

        public virtual object Evaluate(string xpath, IXmlNamespaceResolver? resolver)
        {
            return this.Evaluate(XPathExpression.Compile(xpath, resolver));
        }

        public virtual object Evaluate(XPathExpression expr)
        {
            return Evaluate(expr, null);
        }

        public virtual object Evaluate(XPathExpression expr, XPathNodeIterator? context)
        {
            CompiledXpathExpr? cexpr = expr as CompiledXpathExpr;
            if (cexpr == null)
            {
                throw XPathException.Create(SR.Xp_BadQueryObject);
            }
            Query query = Query.Clone(cexpr.QueryTree);
            query.Reset();

            if (context == null)
            {
                context = new XPathSingletonIterator(this.Clone(), /*moved:*/true);
            }

            object result = query.Evaluate(context);

            if (result is XPathNodeIterator)
            {
                Debug.Assert(context.Current != null);
                return new XPathSelectionIterator(context.Current, query);
            }

            return result;
        }

        public virtual bool Matches(XPathExpression expr)
        {
            CompiledXpathExpr? cexpr = expr as CompiledXpathExpr;
            if (cexpr == null)
                throw XPathException.Create(SR.Xp_BadQueryObject);

            // We should clone query because some Query.MatchNode() alter expression state and this may brake
            // SelectionIterators that are running using this Query
            // Example of MatchNode() that alert the state is FilterQuery.MatchNode()
            Query query = Query.Clone(cexpr.QueryTree);

            try
            {
                return query.MatchNode(this) != null;
            }
            catch (XPathException)
            {
                throw XPathException.Create(SR.Xp_InvalidPattern, cexpr.Expression);
            }
        }

        public virtual bool Matches(string xpath)
        {
            return Matches(CompileMatchPattern(xpath));
        }

        public virtual XPathNodeIterator SelectChildren(XPathNodeType type)
        {
            return new XPathChildIterator(this.Clone(), type);
        }

        public virtual XPathNodeIterator SelectChildren(string name, string namespaceURI)
        {
            return new XPathChildIterator(this.Clone(), name, namespaceURI);
        }

        public virtual XPathNodeIterator SelectAncestors(XPathNodeType type, bool matchSelf)
        {
            return new XPathAncestorIterator(this.Clone(), type, matchSelf);
        }

        public virtual XPathNodeIterator SelectAncestors(string name, string namespaceURI, bool matchSelf)
        {
            return new XPathAncestorIterator(this.Clone(), name, namespaceURI, matchSelf);
        }

        public virtual XPathNodeIterator SelectDescendants(XPathNodeType type, bool matchSelf)
        {
            return new XPathDescendantIterator(this.Clone(), type, matchSelf);
        }

        public virtual XPathNodeIterator SelectDescendants(string name, string namespaceURI, bool matchSelf)
        {
            return new XPathDescendantIterator(this.Clone(), name, namespaceURI, matchSelf);
        }

        public virtual bool CanEdit
        {
            get
            {
                return false;
            }
        }

        public virtual XmlWriter PrependChild()
        {
            throw new NotSupportedException();
        }

        public virtual XmlWriter AppendChild()
        {
            throw new NotSupportedException();
        }

        public virtual XmlWriter InsertAfter()
        {
            throw new NotSupportedException();
        }

        public virtual XmlWriter InsertBefore()
        {
            throw new NotSupportedException();
        }

        public virtual XmlWriter CreateAttributes()
        {
            throw new NotSupportedException();
        }

        public virtual XmlWriter ReplaceRange(XPathNavigator lastSiblingToReplace)
        {
            throw new NotSupportedException();
        }

        public virtual void ReplaceSelf(string newNode)
        {
            XmlReader reader = CreateContextReader(newNode, false);
            ReplaceSelf(reader);
        }

        public virtual void ReplaceSelf(XmlReader newNode)
        {
            ArgumentNullException.ThrowIfNull(newNode);

            XPathNodeType type = NodeType;
            if (type == XPathNodeType.Root
                || type == XPathNodeType.Attribute
                || type == XPathNodeType.Namespace)
            {
                throw new InvalidOperationException(SR.Xpn_BadPosition);
            }
            XmlWriter writer = ReplaceRange(this);
            BuildSubtree(newNode, writer);
            writer.Close();
        }

        public virtual void ReplaceSelf(XPathNavigator newNode)
        {
            ArgumentNullException.ThrowIfNull(newNode);

            XmlReader reader = newNode.CreateReader();
            ReplaceSelf(reader);
        }

        // Returns the markup representing the current node and all of its children.
        public virtual string OuterXml
        {
            get
            {
                StringWriter stringWriter;
                XmlWriterSettings writerSettings;
                XmlWriter xmlWriter;

                // Attributes and namespaces are not allowed at the top-level by the well-formed writer
                if (NodeType == XPathNodeType.Attribute)
                {
                    return $"{Name}=\"{Value}\"";
                }
                else if (NodeType == XPathNodeType.Namespace)
                {
                    if (LocalName.Length == 0)
                        return $"xmlns=\"{Value}\"";
                    else
                        return $"xmlns:{LocalName}=\"{Value}\"";
                }

                stringWriter = new StringWriter(CultureInfo.InvariantCulture);

                writerSettings = new XmlWriterSettings();
                writerSettings.Indent = true;
                writerSettings.OmitXmlDeclaration = true;
                writerSettings.ConformanceLevel = ConformanceLevel.Auto;

                xmlWriter = XmlWriter.Create(stringWriter, writerSettings);
                try
                {
                    xmlWriter.WriteNode(this, true);
                }
                finally
                {
                    xmlWriter.Close();
                }

                return stringWriter.ToString();
            }

            set
            {
                ReplaceSelf(value);
            }
        }

        // Returns the markup representing just the children of the current node.
        public virtual string InnerXml
        {
            get
            {
                switch (NodeType)
                {
                    case XPathNodeType.Root:
                    case XPathNodeType.Element:
                        StringWriter stringWriter;
                        XmlWriterSettings writerSettings;
                        XmlWriter xmlWriter;

                        stringWriter = new StringWriter(CultureInfo.InvariantCulture);

                        writerSettings = new XmlWriterSettings();
                        writerSettings.Indent = true;
                        writerSettings.OmitXmlDeclaration = true;
                        writerSettings.ConformanceLevel = ConformanceLevel.Auto;
                        xmlWriter = XmlWriter.Create(stringWriter, writerSettings);

                        try
                        {
                            if (MoveToFirstChild())
                            {
                                do
                                {
                                    xmlWriter.WriteNode(this, true);
                                }
                                while (MoveToNext());

                                // Restore position
                                MoveToParent();
                            }
                        }
                        finally
                        {
                            xmlWriter.Close();
                        }
                        return stringWriter.ToString();
                    case XPathNodeType.Attribute:
                    case XPathNodeType.Namespace:
                        return Value;
                    default:
                        return string.Empty;
                }
            }

            set
            {
                ArgumentNullException.ThrowIfNull(value);

                switch (NodeType)
                {
                    case XPathNodeType.Root:
                    case XPathNodeType.Element:
                        XPathNavigator edit = CreateNavigator();
                        while (edit.MoveToFirstChild())
                        {
                            edit.DeleteSelf();
                        }
                        if (value.Length != 0)
                        {
                            edit.AppendChild(value);
                        }
                        break;
                    case XPathNodeType.Attribute:
                        SetValue(value);
                        break;
                    default:
                        throw new InvalidOperationException(SR.Xpn_BadPosition);
                }
            }
        }

        public virtual void AppendChild(string newChild)
        {
            XmlReader reader = CreateContextReader(newChild, true);
            AppendChild(reader);
        }

        public virtual void AppendChild(XmlReader newChild)
        {
            ArgumentNullException.ThrowIfNull(newChild);

            XmlWriter writer = AppendChild();
            BuildSubtree(newChild, writer);
            writer.Close();
        }

        public virtual void AppendChild(XPathNavigator newChild)
        {
            ArgumentNullException.ThrowIfNull(newChild);

            if (!IsValidChildType(newChild.NodeType))
            {
                throw new InvalidOperationException(SR.Xpn_BadPosition);
            }
            XmlReader reader = newChild.CreateReader();
            AppendChild(reader);
        }

        public virtual void PrependChild(string newChild)
        {
            XmlReader reader = CreateContextReader(newChild, true);
            PrependChild(reader);
        }

        public virtual void PrependChild(XmlReader newChild)
        {
            ArgumentNullException.ThrowIfNull(newChild);

            XmlWriter writer = PrependChild();
            BuildSubtree(newChild, writer);
            writer.Close();
        }

        public virtual void PrependChild(XPathNavigator newChild)
        {
            ArgumentNullException.ThrowIfNull(newChild);

            if (!IsValidChildType(newChild.NodeType))
            {
                throw new InvalidOperationException(SR.Xpn_BadPosition);
            }
            XmlReader reader = newChild.CreateReader();
            PrependChild(reader);
        }

        public virtual void InsertBefore(string newSibling)
        {
            XmlReader reader = CreateContextReader(newSibling, false);
            InsertBefore(reader);
        }

        public virtual void InsertBefore(XmlReader newSibling)
        {
            ArgumentNullException.ThrowIfNull(newSibling);

            XmlWriter writer = InsertBefore();
            BuildSubtree(newSibling, writer);
            writer.Close();
        }

        public virtual void InsertBefore(XPathNavigator newSibling)
        {
            ArgumentNullException.ThrowIfNull(newSibling);

            if (!IsValidSiblingType(newSibling.NodeType))
            {
                throw new InvalidOperationException(SR.Xpn_BadPosition);
            }
            XmlReader reader = newSibling.CreateReader();
            InsertBefore(reader);
        }

        public virtual void InsertAfter(string newSibling)
        {
            XmlReader reader = CreateContextReader(newSibling, false);
            InsertAfter(reader);
        }

        public virtual void InsertAfter(XmlReader newSibling)
        {
            ArgumentNullException.ThrowIfNull(newSibling);

            XmlWriter writer = InsertAfter();
            BuildSubtree(newSibling, writer);
            writer.Close();
        }

        public virtual void InsertAfter(XPathNavigator newSibling)
        {
            ArgumentNullException.ThrowIfNull(newSibling);

            if (!IsValidSiblingType(newSibling.NodeType))
            {
                throw new InvalidOperationException(SR.Xpn_BadPosition);
            }
            XmlReader reader = newSibling.CreateReader();
            InsertAfter(reader);
        }

        public virtual void DeleteRange(XPathNavigator lastSiblingToDelete)
        {
            throw new NotSupportedException();
        }

        public virtual void DeleteSelf()
        {
            DeleteRange(this);
        }

        public virtual void PrependChildElement(string prefix, string localName, string namespaceURI, string value)
        {
            XmlWriter writer = PrependChild();
            writer.WriteStartElement(prefix, localName, namespaceURI);
            if (value != null)
            {
                writer.WriteString(value);
            }
            writer.WriteEndElement();
            writer.Close();
        }

        public virtual void AppendChildElement(string prefix, string localName, string namespaceURI, string value)
        {
            XmlWriter writer = AppendChild();
            writer.WriteStartElement(prefix, localName, namespaceURI);
            if (value != null)
            {
                writer.WriteString(value);
            }
            writer.WriteEndElement();
            writer.Close();
        }

        public virtual void InsertElementBefore(string prefix, string localName, string namespaceURI, string value)
        {
            XmlWriter writer = InsertBefore();
            writer.WriteStartElement(prefix, localName, namespaceURI);
            if (value != null)
            {
                writer.WriteString(value);
            }
            writer.WriteEndElement();
            writer.Close();
        }

        public virtual void InsertElementAfter(string prefix, string localName, string namespaceURI, string value)
        {
            XmlWriter writer = InsertAfter();
            writer.WriteStartElement(prefix, localName, namespaceURI);
            if (value != null)
            {
                writer.WriteString(value);
            }
            writer.WriteEndElement();
            writer.Close();
        }

        public virtual void CreateAttribute(string prefix, string localName, string namespaceURI, string value)
        {
            XmlWriter writer = CreateAttributes();
            writer.WriteStartAttribute(prefix, localName, namespaceURI);
            if (value != null)
            {
                writer.WriteString(value);
            }
            writer.WriteEndAttribute();
            writer.Close();
        }

        //-----------------------------------------------
        // Internal
        //-----------------------------------------------

        internal bool MoveToPrevious(string localName, string namespaceURI)
        {
            XPathNavigator navClone = Clone();

            string? atomizedLocalName = (localName != null) ? NameTable.Get(localName) : null;
            while (MoveToPrevious())
            {
                if (NodeType == XPathNodeType.Element && atomizedLocalName == LocalName && namespaceURI == NamespaceURI)
                    return true;
            }

            MoveTo(navClone);
            return false;
        }

        internal bool MoveToPrevious(XPathNodeType type)
        {
            XPathNavigator navClone = Clone();
            int mask = GetContentKindMask(type);

            while (MoveToPrevious())
            {
                if (((1 << (int)NodeType) & mask) != 0)
                    return true;
            }

            MoveTo(navClone);
            return false;
        }

        internal bool MoveToNonDescendant()
        {
            // If current node is document, there is no next non-descendant
            if (NodeType == XPathNodeType.Root)
                return false;

            // If sibling exists, it is the next non-descendant
            if (MoveToNext())
                return true;

            // The current node is either an attribute, namespace, or last child node
            XPathNavigator navSave = Clone();

            if (!MoveToParent())
                return false;

            switch (navSave.NodeType)
            {
                case XPathNodeType.Attribute:
                case XPathNodeType.Namespace:
                    // Next node in document order is first content-child of parent
                    if (MoveToFirstChild())
                        return true;
                    break;
            }

            while (!MoveToNext())
            {
                if (!MoveToParent())
                {
                    // Restore original position and return false
                    MoveTo(navSave);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns ordinal number of attribute, namespace or child node within its parent.
        /// Order is reversed for attributes and child nodes to avoid O(N**2) running time.
        /// This property is useful for debugging, and also used in UniqueId implementation.
        /// </summary>
        internal uint IndexInParent
        {
            get
            {
                XPathNavigator nav = this.Clone();
                uint idx = 0;

                switch (NodeType)
                {
                    case XPathNodeType.Attribute:
                        while (nav.MoveToNextAttribute())
                        {
                            idx++;
                        }
                        break;
                    case XPathNodeType.Namespace:
                        while (nav.MoveToNextNamespace())
                        {
                            idx++;
                        }
                        break;
                    default:
                        while (nav.MoveToNext())
                        {
                            idx++;
                        }
                        break;
                }
                return idx;
            }
        }

        internal static readonly char[] NodeTypeLetter = new char[] {
            'R',    // Root
            'E',    // Element
            'A',    // Attribute
            'N',    // Namespace
            'T',    // Text
            'S',    // SignificantWhitespace
            'W',    // Whitespace
            'P',    // ProcessingInstruction
            'C',    // Comment
            'X',    // All
        };

        internal static readonly char[] UniqueIdTbl = new char[] {
            'A',  'B',  'C',  'D',  'E',  'F',  'G',  'H',  'I',  'J',
            'K',  'L',  'M',  'N',  'O',  'P',  'Q',  'R',  'S',  'T',
            'U',  'V',  'W',  'X',  'Y',  'Z',  '1',  '2',  '3',  '4',
            '5',  '6'
        };

        // Requirements for id:
        //  1. must consist of alphanumeric characters only
        //  2. must begin with an alphabetic character
        //  3. same id is generated for the same node
        //  4. ids are unique
        //
        //  id = node type letter + reverse path to root in terms of encoded IndexInParent integers from node to root separated by 0's if needed
        internal virtual string UniqueId
        {
            get
            {
                XPathNavigator nav = this.Clone();
                StringBuilder sb = new StringBuilder();

                // Ensure distinguishing attributes, namespaces and child nodes
                sb.Append(NodeTypeLetter[(int)NodeType]);

                while (true)
                {
                    uint idx = nav.IndexInParent;
                    if (!nav.MoveToParent())
                    {
                        break;
                    }
                    if (idx <= 0x1f)
                    {
                        sb.Append(UniqueIdTbl[idx]);
                    }
                    else
                    {
                        sb.Append('0');
                        do
                        {
                            sb.Append(UniqueIdTbl[idx & 0x1f]);
                            idx >>= 5;
                        } while (idx != 0);
                        sb.Append('0');
                    }
                }
                return sb.ToString();
            }
        }

        private static XPathExpression CompileMatchPattern(string xpath)
        {
            bool hasPrefix;
            Query query = new QueryBuilder().BuildPatternQuery(xpath, out hasPrefix);
            return new CompiledXpathExpr(query, xpath, hasPrefix);
        }

        private static int GetDepth(XPathNavigator nav)
        {
            int depth = 0;
            while (nav.MoveToParent())
            {
                depth++;
            }
            return depth;
        }

        // XPath based comparison for namespaces, attributes and other
        // items with the same parent element.
        //
        //                 n2
        //                 namespace(0)    attribute(-1)   other(-2)
        // n1
        // namespace(0)    ?(0)            before(-1)      before(-2)
        // attribute(1)    after(1)        ?(0)            before(-1)
        // other    (2)    after(2)        after(1)        ?(0)
        private static XmlNodeOrder CompareSiblings(XPathNavigator n1, XPathNavigator n2)
        {
            int cmp = 0;

#if DEBUG
            Debug.Assert(!n1.IsSamePosition(n2));
            XPathNavigator p1 = n1.Clone(), p2 = n2.Clone();
            Debug.Assert(p1.MoveToParent() && p2.MoveToParent() && p1.IsSamePosition(p2));
#endif
            switch (n1.NodeType)
            {
                case XPathNodeType.Namespace:
                    break;
                case XPathNodeType.Attribute:
                    cmp += 1;
                    break;
                default:
                    cmp += 2;
                    break;
            }
            switch (n2.NodeType)
            {
                case XPathNodeType.Namespace:
                    if (cmp == 0)
                    {
                        while (n1.MoveToNextNamespace())
                        {
                            if (n1.IsSamePosition(n2))
                            {
                                return XmlNodeOrder.Before;
                            }
                        }
                    }
                    break;
                case XPathNodeType.Attribute:
                    cmp -= 1;
                    if (cmp == 0)
                    {
                        while (n1.MoveToNextAttribute())
                        {
                            if (n1.IsSamePosition(n2))
                            {
                                return XmlNodeOrder.Before;
                            }
                        }
                    }
                    break;
                default:
                    cmp -= 2;
                    if (cmp == 0)
                    {
                        while (n1.MoveToNext())
                        {
                            if (n1.IsSamePosition(n2))
                            {
                                return XmlNodeOrder.Before;
                            }
                        }
                    }
                    break;
            }
            return cmp < 0 ? XmlNodeOrder.Before : XmlNodeOrder.After;
        }

        internal static XmlNamespaceManager GetNamespaces(IXmlNamespaceResolver resolver)
        {
            XmlNamespaceManager mngr = new XmlNamespaceManager(new NameTable());
            IDictionary<string, string> dictionary = resolver.GetNamespacesInScope(XmlNamespaceScope.All);
            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                //"xmlns " is always in the namespace manager so adding it would throw an exception
                if (pair.Key != "xmlns")
                    mngr.AddNamespace(pair.Key, pair.Value);
            }
            return mngr;
        }

        // Get mask that will allow XPathNodeType content matching to be performed using only a shift and an and operation
        internal const int AllMask = 0x7FFFFFFF;
        internal const int NoAttrNmspMask = AllMask & ~(1 << (int)XPathNodeType.Attribute) & ~(1 << (int)XPathNodeType.Namespace);
        internal const int TextMask = (1 << (int)XPathNodeType.Text) | (1 << (int)XPathNodeType.SignificantWhitespace) | (1 << (int)XPathNodeType.Whitespace);
        internal static readonly int[] ContentKindMasks = {
            (1 << (int) XPathNodeType.Root),                        // Root
            (1 << (int) XPathNodeType.Element),                     // Element
            0,                                                      // Attribute (not content)
            0,                                                      // Namespace (not content)
            TextMask,                                               // Text
            (1 << (int) XPathNodeType.SignificantWhitespace),       // SignificantWhitespace
            (1 << (int) XPathNodeType.Whitespace),                  // Whitespace
            (1 << (int) XPathNodeType.ProcessingInstruction),       // ProcessingInstruction
            (1 << (int) XPathNodeType.Comment),                     // Comment
            NoAttrNmspMask,                                         // All
        };

        internal static int GetContentKindMask(XPathNodeType type)
        {
            return ContentKindMasks[(int)type];
        }

        internal static int GetKindMask(XPathNodeType type)
        {
            if (type == XPathNodeType.All)
                return AllMask;
            else if (type == XPathNodeType.Text)
                return TextMask;

            return (1 << (int)type);
        }

        internal static bool IsText(XPathNodeType type)
        {
            //return ((1 << (int) type) & TextMask) != 0;
            return unchecked((uint)(type - XPathNodeType.Text)) <= (XPathNodeType.Whitespace - XPathNodeType.Text);
        }

        // Lax check for potential child item.
        private bool IsValidChildType(XPathNodeType type)
        {
            switch (NodeType)
            {
                case XPathNodeType.Root:
                    switch (type)
                    {
                        case XPathNodeType.Element:
                        case XPathNodeType.SignificantWhitespace:
                        case XPathNodeType.Whitespace:
                        case XPathNodeType.ProcessingInstruction:
                        case XPathNodeType.Comment:
                            return true;
                    }
                    break;
                case XPathNodeType.Element:
                    switch (type)
                    {
                        case XPathNodeType.Element:
                        case XPathNodeType.Text:
                        case XPathNodeType.SignificantWhitespace:
                        case XPathNodeType.Whitespace:
                        case XPathNodeType.ProcessingInstruction:
                        case XPathNodeType.Comment:
                            return true;
                    }
                    break;
            }
            return false;
        }

        // Lax check for potential sibling item.
        private bool IsValidSiblingType(XPathNodeType type)
        {
            switch (NodeType)
            {
                case XPathNodeType.Element:
                case XPathNodeType.Text:
                case XPathNodeType.SignificantWhitespace:
                case XPathNodeType.Whitespace:
                case XPathNodeType.ProcessingInstruction:
                case XPathNodeType.Comment:
                    switch (type)
                    {
                        case XPathNodeType.Element:
                        case XPathNodeType.Text:
                        case XPathNodeType.SignificantWhitespace:
                        case XPathNodeType.Whitespace:
                        case XPathNodeType.ProcessingInstruction:
                        case XPathNodeType.Comment:
                            return true;
                    }
                    break;
            }
            return false;
        }

        private XmlReader CreateReader()
        {
            return XPathNavigatorReader.Create(this);
        }

        private XmlReader CreateContextReader(string xml, bool fromCurrentNode)
        {
            ArgumentNullException.ThrowIfNull(xml);

            // We have to set the namespace context for the reader.
            XPathNavigator editor = CreateNavigator();
            // scope starts from parent.
            XmlNamespaceManager mgr = new XmlNamespaceManager(NameTable);
            if (!fromCurrentNode)
            {
                editor.MoveToParent(); // should always succeed.
            }
            if (editor.MoveToFirstNamespace(XPathNamespaceScope.All))
            {
                do
                {
                    mgr.AddNamespace(editor.LocalName, editor.Value);
                }
                while (editor.MoveToNextNamespace(XPathNamespaceScope.All));
            }
            // BUGBUG: How can we preserve the whitespace setting
            XmlParserContext context = new XmlParserContext(NameTable, mgr, null, XmlSpace.Default);
            XmlTextReader reader = new XmlTextReader(xml, XmlNodeType.Element, context);
            // BUGBUG: Whitespace handling??
            reader.WhitespaceHandling = WhitespaceHandling.Significant;
            return reader;
        }

        internal static void BuildSubtree(XmlReader reader, XmlWriter writer)
        {
            // important (perf) string literal...
            string xmlnsUri = XmlReservedNs.NsXmlNs; // http://www.w3.org/2000/xmlns/
            ReadState readState = reader.ReadState;

            if (readState != ReadState.Initial
                && readState != ReadState.Interactive)
            {
                throw new ArgumentException(SR.Xml_InvalidOperation, nameof(reader));
            }
            int level = 0;
            if (readState == ReadState.Initial)
            {
                if (!reader.Read())
                    return;
                level++; // if start in initial, read everything (not just first)
            }
            do
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        bool isEmptyElement = reader.IsEmptyElement;

                        while (reader.MoveToNextAttribute())
                        {
                            if ((object)reader.NamespaceURI == (object)xmlnsUri)
                            {
                                if (reader.Prefix.Length == 0)
                                {
                                    // Default namespace declaration "xmlns"
                                    Debug.Assert(reader.LocalName == "xmlns");
                                    writer.WriteAttributeString("", "xmlns", xmlnsUri, reader.Value);
                                }
                                else
                                {
                                    Debug.Assert(reader.Prefix == "xmlns");
                                    writer.WriteAttributeString("xmlns", reader.LocalName, xmlnsUri, reader.Value);
                                }
                            }
                            else
                            {
                                writer.WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                                writer.WriteString(reader.Value);
                                writer.WriteEndAttribute();
                            }
                        }

                        reader.MoveToElement();
                        if (isEmptyElement)
                        {
                            // there might still be a value, if there is a default value specified in the schema
                            writer.WriteEndElement();
                        }
                        else
                        {
                            level++;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        writer.WriteFullEndElement();
                        //should not read beyond the level of the reader's original position.
                        level--;
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        writer.WriteString(reader.Value);
                        break;
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        writer.WriteString(reader.Value);
                        break;
                    case XmlNodeType.Comment:
                        writer.WriteComment(reader.Value);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        writer.WriteProcessingInstruction(reader.LocalName, reader.Value);
                        break;
                    case XmlNodeType.EntityReference:
                        reader.ResolveEntity();
                        break;
                    case XmlNodeType.EndEntity:
                    case XmlNodeType.None:
                    case XmlNodeType.DocumentType:
                    case XmlNodeType.XmlDeclaration:
                        break;
                    case XmlNodeType.Attribute:
                        if ((object)reader.NamespaceURI == (object)xmlnsUri)
                        {
                            if (reader.Prefix.Length == 0)
                            {
                                // Default namespace declaration "xmlns"
                                Debug.Assert(reader.LocalName == "xmlns");
                                writer.WriteAttributeString("", "xmlns", xmlnsUri, reader.Value);
                            }
                            else
                            {
                                Debug.Assert(reader.Prefix == "xmlns");
                                writer.WriteAttributeString("xmlns", reader.LocalName, xmlnsUri, reader.Value);
                            }
                        }
                        else
                        {
                            writer.WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                            writer.WriteString(reader.Value);
                            writer.WriteEndAttribute();
                        }
                        break;
                }
            }
            while (reader.Read() && (level > 0));
        }

        private object debuggerDisplayProxy { get { return new DebuggerDisplayProxy(this); } }

        [DebuggerDisplay("{ToString()}")]
        internal struct DebuggerDisplayProxy
        {
            private readonly XPathNavigator _nav;
            public DebuggerDisplayProxy(XPathNavigator nav)
            {
                _nav = nav;
            }
            public override string ToString()
            {
                string result = _nav.NodeType.ToString();
                switch (_nav.NodeType)
                {
                    case XPathNodeType.Element:
                        result += $", Name=\"{_nav.Name}\"";
                        break;
                    case XPathNodeType.Attribute:
                    case XPathNodeType.Namespace:
                    case XPathNodeType.ProcessingInstruction:
                        result += $", Name=\"{_nav.Name}\"";
                        result += $", Value=\"{XmlConvert.EscapeValueForDebuggerDisplay(_nav.Value)}\"";
                        break;
                    case XPathNodeType.Text:
                    case XPathNodeType.Whitespace:
                    case XPathNodeType.SignificantWhitespace:
                    case XPathNodeType.Comment:
                        result += $", Value=\"{XmlConvert.EscapeValueForDebuggerDisplay(_nav.Value)}\"";
                        break;
                }
                return result;
            }
        }
    }
}
