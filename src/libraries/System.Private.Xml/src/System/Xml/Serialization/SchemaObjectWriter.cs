// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System;
    using System.Text;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;

    internal sealed class XmlAttributeComparer : IComparer
    {
        public int Compare(object? o1, object? o2)
        {
            XmlAttribute a1 = (XmlAttribute)o1!;
            XmlAttribute a2 = (XmlAttribute)o2!;
            int ns = string.Compare(a1.NamespaceURI, a2.NamespaceURI, StringComparison.Ordinal);
            if (ns == 0)
            {
                return string.Compare(a1.Name, a2.Name, StringComparison.Ordinal);
            }
            return ns;
        }
    }

    internal sealed class XmlFacetComparer : IComparer
    {
        public int Compare(object? o1, object? o2)
        {
            XmlSchemaFacet f1 = (XmlSchemaFacet)o1!;
            XmlSchemaFacet f2 = (XmlSchemaFacet)o2!;
            return string.Compare(f1.GetType().Name + ":" + f1.Value, f2.GetType().Name + ":" + f2.Value, StringComparison.Ordinal);
        }
    }

    internal sealed class QNameComparer : IComparer
    {
        public int Compare(object? o1, object? o2)
        {
            XmlQualifiedName qn1 = (XmlQualifiedName)o1!;
            XmlQualifiedName qn2 = (XmlQualifiedName)o2!;
            int ns = string.Compare(qn1.Namespace, qn2.Namespace, StringComparison.Ordinal);
            if (ns == 0)
            {
                return string.Compare(qn1.Name, qn2.Name, StringComparison.Ordinal);
            }
            return ns;
        }
    }

    internal sealed class XmlSchemaObjectComparer : IComparer
    {
        private readonly QNameComparer _comparer = new QNameComparer();
        public int Compare(object? o1, object? o2)
        {
            return _comparer.Compare(NameOf((XmlSchemaObject?)o1), NameOf((XmlSchemaObject?)o2));
        }

        internal static XmlQualifiedName NameOf(XmlSchemaObject? o)
        {
            if (o is XmlSchemaAttribute)
            {
                return ((XmlSchemaAttribute)o).QualifiedName;
            }
            else if (o is XmlSchemaAttributeGroup)
            {
                return ((XmlSchemaAttributeGroup)o).QualifiedName;
            }
            else if (o is XmlSchemaComplexType)
            {
                return ((XmlSchemaComplexType)o).QualifiedName;
            }
            else if (o is XmlSchemaSimpleType)
            {
                return ((XmlSchemaSimpleType)o).QualifiedName;
            }
            else if (o is XmlSchemaElement)
            {
                return ((XmlSchemaElement)o).QualifiedName;
            }
            else if (o is XmlSchemaGroup)
            {
                return ((XmlSchemaGroup)o).QualifiedName;
            }
            else if (o is XmlSchemaGroupRef)
            {
                return ((XmlSchemaGroupRef)o).RefName;
            }
            else if (o is XmlSchemaNotation)
            {
                return ((XmlSchemaNotation)o).QualifiedName;
            }
            else if (o is XmlSchemaSequence)
            {
                XmlSchemaSequence s = (XmlSchemaSequence)o;
                if (s.Items.Count == 0)
                    return new XmlQualifiedName(".sequence", Namespace(o));
                return NameOf(s.Items[0]);
            }
            else if (o is XmlSchemaAll)
            {
                XmlSchemaAll a = (XmlSchemaAll)o;
                if (a.Items.Count == 0)
                    return new XmlQualifiedName(".all", Namespace(o));
                return NameOf(a.Items);
            }
            else if (o is XmlSchemaChoice)
            {
                XmlSchemaChoice c = (XmlSchemaChoice)o;
                if (c.Items.Count == 0)
                    return new XmlQualifiedName(".choice", Namespace(o));
                return NameOf(c.Items);
            }
            else if (o is XmlSchemaAny)
            {
                return new XmlQualifiedName("*", SchemaObjectWriter.ToString(((XmlSchemaAny)o).NamespaceList));
            }
            else if (o is XmlSchemaIdentityConstraint)
            {
                return ((XmlSchemaIdentityConstraint)o).QualifiedName;
            }
            return new XmlQualifiedName("?", Namespace(o));
        }

        internal static XmlQualifiedName NameOf(XmlSchemaObjectCollection items)
        {
            ArrayList list = new ArrayList();

            for (int i = 0; i < items.Count; i++)
            {
                list.Add(NameOf(items[i]));
            }
            list.Sort(new QNameComparer());
            return (XmlQualifiedName)list[0]!;
        }

        internal static string? Namespace(XmlSchemaObject? o)
        {
            while (o != null && !(o is XmlSchema))
            {
                o = o.Parent;
            }
            return o == null ? "" : ((XmlSchema)o).TargetNamespace;
        }
    }

    internal sealed class SchemaObjectWriter
    {
        private readonly StringBuilder _w = new StringBuilder();
        private int _indentLevel = -1;

        private void WriteIndent()
        {
            for (int i = 0; i < _indentLevel; i++)
            {
                _w.Append(' ');
            }
        }
        private void WriteAttribute(string localName, string ns, string? value)
        {
            if (value == null || value.Length == 0)
                return;
            _w.Append(',');
            _w.Append(ns);
            if (ns != null && ns.Length != 0)
                _w.Append(':');
            _w.Append(localName);
            _w.Append('=');
            _w.Append(value);
        }
        private void WriteAttribute(string localName, string ns, XmlQualifiedName value)
        {
            if (value.IsEmpty)
                return;
            WriteAttribute(localName, ns, value.ToString());
        }

        private void WriteStartElement(string name)
        {
            NewLine();
            _indentLevel++;
            _w.Append('[');
            _w.Append(name);
        }
        private void WriteEndElement()
        {
            _w.Append(']');
            _indentLevel--;
        }
        private void NewLine()
        {
            _w.Append(Environment.NewLine);
            WriteIndent();
        }

        private string GetString()
        {
            return _w.ToString();
        }

        private void WriteAttribute(XmlAttribute a)
        {
            if (a.Value != null)
            {
                WriteAttribute(a.Name, a.NamespaceURI, a.Value);
            }
        }

        private void WriteAttributes(XmlAttribute[]? a, XmlSchemaObject o)
        {
            if (a == null) return;
            ArrayList attrs = new ArrayList();
            for (int i = 0; i < a.Length; i++)
            {
                attrs.Add(a[i]);
            }
            attrs.Sort(new XmlAttributeComparer());
            for (int i = 0; i < attrs.Count; i++)
            {
                XmlAttribute attribute = (XmlAttribute)attrs[i]!;
                WriteAttribute(attribute);
            }
        }

        [return: NotNullIfNotNull("list")]
        internal static string? ToString(NamespaceList? list)
        {
            if (list == null)
                return null;
            switch (list.Type)
            {
                case NamespaceList.ListType.Any:
                    return "##any";
                case NamespaceList.ListType.Other:
                    return "##other";
                case NamespaceList.ListType.Set:
                    ArrayList ns = new ArrayList();

                    foreach (string s in list.Enumerate)
                    {
                        ns.Add(s);
                    }
                    ns.Sort();
                    StringBuilder sb = new StringBuilder();
                    bool first = true;
                    foreach (string s in ns)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(' ');
                        }
                        if (s.Length == 0)
                        {
                            sb.Append("##local");
                        }
                        else
                        {
                            sb.Append(s);
                        }
                    }
                    return sb.ToString();

                default:
                    return list.ToString();
            }
        }

        internal string WriteXmlSchemaObject(XmlSchemaObject? o)
        {
            if (o == null) return string.Empty;
            Write3_XmlSchemaObject((XmlSchemaObject?)o);
            return GetString();
        }

        private void WriteSortedItems(XmlSchemaObjectCollection? items)
        {
            if (items == null) return;

            ArrayList list = new ArrayList();
            for (int i = 0; i < items.Count; i++)
            {
                list.Add(items[i]);
            }
            list.Sort(new XmlSchemaObjectComparer());
            for (int i = 0; i < list.Count; i++)
            {
                Write3_XmlSchemaObject((XmlSchemaObject?)list[i]);
            }
        }

        private void Write1_XmlSchemaAttribute(XmlSchemaAttribute? o)
        {
            if (o is null) return;
            WriteStartElement("attribute");
            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            WriteAttribute(@"default", @"", ((string?)o.@DefaultValue));
            WriteAttribute(@"fixed", @"", ((string?)o.@FixedValue));
            if (o.Parent != null && !(o.Parent is XmlSchema))
            {
                if (o.QualifiedName != null && !o.QualifiedName.IsEmpty && o.QualifiedName.Namespace != null && o.QualifiedName.Namespace.Length != 0)
                {
                    WriteAttribute(@"form", @"", "qualified");
                }
                else
                {
                    WriteAttribute(@"form", @"", "unqualified");
                }
            }
            WriteAttribute(@"name", @"", ((string?)o.@Name));

            if (!o.RefName.IsEmpty)
            {
                WriteAttribute("ref", "", o.RefName);
            }
            else if (!o.SchemaTypeName.IsEmpty)
            {
                WriteAttribute("type", "", o.SchemaTypeName);
            }
            XmlSchemaUse use = o.Use == XmlSchemaUse.None ? XmlSchemaUse.Optional : o.Use;
            WriteAttribute(@"use", @"", Write30_XmlSchemaUse(use));
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            Write9_XmlSchemaSimpleType((XmlSchemaSimpleType?)o.@SchemaType);
            WriteEndElement();
        }

        private void Write3_XmlSchemaObject(XmlSchemaObject? o)
        {
            if (o is null) return;
            System.Type t = o.GetType();

            if (t == typeof(XmlSchemaComplexType))
            {
                Write35_XmlSchemaComplexType((XmlSchemaComplexType)o);
                return;
            }
            else if (t == typeof(XmlSchemaSimpleType))
            {
                Write9_XmlSchemaSimpleType((XmlSchemaSimpleType)o);
                return;
            }
            else if (t == typeof(XmlSchemaElement))
            {
                Write46_XmlSchemaElement((XmlSchemaElement)o);
                return;
            }
            else if (t == typeof(XmlSchemaAppInfo))
            {
                Write7_XmlSchemaAppInfo((XmlSchemaAppInfo)o);
                return;
            }
            else if (t == typeof(XmlSchemaDocumentation))
            {
                Write6_XmlSchemaDocumentation((XmlSchemaDocumentation)o);
                return;
            }
            else if (t == typeof(XmlSchemaAnnotation))
            {
                Write5_XmlSchemaAnnotation((XmlSchemaAnnotation)o);
                return;
            }
            else if (t == typeof(XmlSchemaGroup))
            {
                Write57_XmlSchemaGroup((XmlSchemaGroup)o);
                return;
            }
            else if (t == typeof(XmlSchemaXPath))
            {
                Write49_XmlSchemaXPath("xpath", "", (XmlSchemaXPath)o);
                return;
            }
            else if (t == typeof(XmlSchemaIdentityConstraint))
            {
                Write48_XmlSchemaIdentityConstraint((XmlSchemaIdentityConstraint)o);
                return;
            }
            else if (t == typeof(XmlSchemaUnique))
            {
                Write51_XmlSchemaUnique((XmlSchemaUnique)o);
                return;
            }
            else if (t == typeof(XmlSchemaKeyref))
            {
                Write50_XmlSchemaKeyref((XmlSchemaKeyref)o);
                return;
            }
            else if (t == typeof(XmlSchemaKey))
            {
                Write47_XmlSchemaKey((XmlSchemaKey)o);
                return;
            }
            else if (t == typeof(XmlSchemaGroupRef))
            {
                Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)o);
                return;
            }
            else if (t == typeof(XmlSchemaAny))
            {
                Write53_XmlSchemaAny((XmlSchemaAny)o);
                return;
            }
            else if (t == typeof(XmlSchemaSequence))
            {
                Write54_XmlSchemaSequence((XmlSchemaSequence)o);
                return;
            }
            else if (t == typeof(XmlSchemaChoice))
            {
                Write52_XmlSchemaChoice((XmlSchemaChoice)o);
                return;
            }
            else if (t == typeof(XmlSchemaAll))
            {
                Write43_XmlSchemaAll((XmlSchemaAll)o);
                return;
            }
            else if (t == typeof(XmlSchemaComplexContentRestriction))
            {
                Write56_XmlSchemaComplexContentRestriction((XmlSchemaComplexContentRestriction)o);
                return;
            }
            else if (t == typeof(XmlSchemaComplexContentExtension))
            {
                Write42_XmlSchemaComplexContentExtension((XmlSchemaComplexContentExtension)o);
                return;
            }
            else if (t == typeof(XmlSchemaSimpleContentRestriction))
            {
                Write40_XmlSchemaSimpleContentRestriction((XmlSchemaSimpleContentRestriction)o);
                return;
            }
            else if (t == typeof(XmlSchemaSimpleContentExtension))
            {
                Write38_XmlSchemaSimpleContentExtension((XmlSchemaSimpleContentExtension)o);
                return;
            }
            else if (t == typeof(XmlSchemaComplexContent))
            {
                Write41_XmlSchemaComplexContent((XmlSchemaComplexContent)o);
                return;
            }
            else if (t == typeof(XmlSchemaSimpleContent))
            {
                Write36_XmlSchemaSimpleContent((XmlSchemaSimpleContent)o);
                return;
            }
            else if (t == typeof(XmlSchemaAnyAttribute))
            {
                Write33_XmlSchemaAnyAttribute((XmlSchemaAnyAttribute)o);
                return;
            }
            else if (t == typeof(XmlSchemaAttributeGroupRef))
            {
                Write32_XmlSchemaAttributeGroupRef((XmlSchemaAttributeGroupRef)o);
                return;
            }
            else if (t == typeof(XmlSchemaAttributeGroup))
            {
                Write31_XmlSchemaAttributeGroup((XmlSchemaAttributeGroup)o);
                return;
            }
            else if (t == typeof(XmlSchemaSimpleTypeRestriction))
            {
                Write15_XmlSchemaSimpleTypeRestriction((XmlSchemaSimpleTypeRestriction)o);
                return;
            }
            else if (t == typeof(XmlSchemaSimpleTypeList))
            {
                Write14_XmlSchemaSimpleTypeList((XmlSchemaSimpleTypeList)o);
                return;
            }
            else if (t == typeof(XmlSchemaSimpleTypeUnion))
            {
                Write12_XmlSchemaSimpleTypeUnion((XmlSchemaSimpleTypeUnion)o);
                return;
            }
            else if (t == typeof(XmlSchemaAttribute))
            {
                Write1_XmlSchemaAttribute((XmlSchemaAttribute)o);
                return;
            }
        }

        private void Write5_XmlSchemaAnnotation(XmlSchemaAnnotation? o)
        {
            if (o is null) return;
            WriteStartElement("annotation");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            System.Xml.Schema.XmlSchemaObjectCollection a = (System.Xml.Schema.XmlSchemaObjectCollection)o.@Items;
            if (a != null)
            {
                for (int ia = 0; ia < a.Count; ia++)
                {
                    XmlSchemaObject ai = (XmlSchemaObject)a[ia];
                    if (ai is XmlSchemaAppInfo)
                    {
                        Write7_XmlSchemaAppInfo((XmlSchemaAppInfo)ai);
                    }
                    else if (ai is XmlSchemaDocumentation)
                    {
                        Write6_XmlSchemaDocumentation((XmlSchemaDocumentation)ai);
                    }
                }
            }
            WriteEndElement();
        }

        private void Write6_XmlSchemaDocumentation(XmlSchemaDocumentation o)
        {
            if (o is null) return;
            WriteStartElement("documentation");

            WriteAttribute(@"source", @"", ((string?)o.@Source));
            WriteAttribute(@"lang", @"http://www.w3.org/XML/1998/namespace", ((string?)o.@Language));
            XmlNode?[]? a = (XmlNode?[]?)o.@Markup;
            if (a != null)
            {
                for (int ia = 0; ia < a.Length; ia++)
                {
                    XmlNode ai = (XmlNode)a[ia]!;
                    WriteStartElement("node");
                    WriteAttribute("xml", "", ai.OuterXml);
                }
            }
            WriteEndElement();
        }

        private void Write7_XmlSchemaAppInfo(XmlSchemaAppInfo? o)
        {
            if (o is null) return;
            WriteStartElement("appinfo");

            WriteAttribute("source", "", o.Source);
            XmlNode?[]? a = (XmlNode?[]?)o.@Markup;
            if (a != null)
            {
                for (int ia = 0; ia < a.Length; ia++)
                {
                    XmlNode ai = (XmlNode)a[ia]!;
                    WriteStartElement("node");
                    WriteAttribute("xml", "", ai.OuterXml);
                }
            }
            WriteEndElement();
        }

        private void Write9_XmlSchemaSimpleType(XmlSchemaSimpleType? o)
        {
            if (o is null) return;
            WriteStartElement("simpleType");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            WriteAttribute(@"name", @"", ((string?)o.@Name));
            WriteAttribute(@"final", @"", Write11_XmlSchemaDerivationMethod(o.FinalResolved));
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            if (o.@Content is XmlSchemaSimpleTypeUnion)
            {
                Write12_XmlSchemaSimpleTypeUnion((XmlSchemaSimpleTypeUnion)o.@Content);
            }
            else if (o.@Content is XmlSchemaSimpleTypeRestriction)
            {
                Write15_XmlSchemaSimpleTypeRestriction((XmlSchemaSimpleTypeRestriction)o.@Content);
            }
            else if (o.@Content is XmlSchemaSimpleTypeList)
            {
                Write14_XmlSchemaSimpleTypeList((XmlSchemaSimpleTypeList)o.@Content);
            }
            WriteEndElement();
        }

        private string Write11_XmlSchemaDerivationMethod(XmlSchemaDerivationMethod v)
        {
            return v.ToString();
        }

        private void Write12_XmlSchemaSimpleTypeUnion(XmlSchemaSimpleTypeUnion? o)
        {
            if (o is null) return;
            WriteStartElement("union");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);

            if (o.MemberTypes != null)
            {
                ArrayList list = new ArrayList();
                for (int i = 0; i < o.MemberTypes.Length; i++)
                {
                    list.Add(o.MemberTypes[i]);
                }
                list.Sort(new QNameComparer());

                _w.Append(',');
                _w.Append("memberTypes=");

                for (int i = 0; i < list.Count; i++)
                {
                    XmlQualifiedName q = (XmlQualifiedName)list[i]!;
                    _w.Append(q.ToString());
                    _w.Append(',');
                }
            }
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteSortedItems(o.@BaseTypes);
            WriteEndElement();
        }

        private void Write14_XmlSchemaSimpleTypeList(XmlSchemaSimpleTypeList o)
        {
            if (o is null) return;
            WriteStartElement("list");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            if (!o.@ItemTypeName.IsEmpty)
            {
                WriteAttribute(@"itemType", @"", o.@ItemTypeName);
            }
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            Write9_XmlSchemaSimpleType((XmlSchemaSimpleType?)o.@ItemType);
            WriteEndElement();
        }

        private void Write15_XmlSchemaSimpleTypeRestriction(XmlSchemaSimpleTypeRestriction? o)
        {
            if (o is null) return;
            WriteStartElement("restriction");
            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            if (!o.@BaseTypeName.IsEmpty)
            {
                WriteAttribute(@"base", @"", o.@BaseTypeName);
            }
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            Write9_XmlSchemaSimpleType((XmlSchemaSimpleType?)o.@BaseType);
            WriteFacets(o.Facets);
            WriteEndElement();
        }

        private void WriteFacets(XmlSchemaObjectCollection? facets)
        {
            if (facets == null) return;

            ArrayList a = new ArrayList();
            for (int i = 0; i < facets.Count; i++)
            {
                a.Add(facets[i]);
            }
            a.Sort(new XmlFacetComparer());
            for (int ia = 0; ia < a.Count; ia++)
            {
                XmlSchemaObject? ai = (XmlSchemaObject?)a[ia];
                if (ai is XmlSchemaMinExclusiveFacet)
                {
                    Write_XmlSchemaFacet("minExclusive", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaMaxInclusiveFacet)
                {
                    Write_XmlSchemaFacet("maxInclusive", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaMaxExclusiveFacet)
                {
                    Write_XmlSchemaFacet("maxExclusive", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaMinInclusiveFacet)
                {
                    Write_XmlSchemaFacet("minInclusive", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaLengthFacet)
                {
                    Write_XmlSchemaFacet("length", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaEnumerationFacet)
                {
                    Write_XmlSchemaFacet("enumeration", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaMinLengthFacet)
                {
                    Write_XmlSchemaFacet("minLength", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaPatternFacet)
                {
                    Write_XmlSchemaFacet("pattern", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaTotalDigitsFacet)
                {
                    Write_XmlSchemaFacet("totalDigits", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaMaxLengthFacet)
                {
                    Write_XmlSchemaFacet("maxLength", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaWhiteSpaceFacet)
                {
                    Write_XmlSchemaFacet("whiteSpace", (XmlSchemaFacet)ai);
                }
                else if (ai is XmlSchemaFractionDigitsFacet)
                {
                    Write_XmlSchemaFacet("fractionDigit", (XmlSchemaFacet)ai);
                }
            }
        }

        private void Write_XmlSchemaFacet(string name, XmlSchemaFacet? o)
        {
            if (o is null) return;
            WriteStartElement(name);

            WriteAttribute("id", "", o.Id);
            WriteAttribute("value", "", o.Value);
            if (o.IsFixed)
            {
                WriteAttribute(@"fixed", @"", XmlConvert.ToString(o.IsFixed));
            }
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteEndElement();
        }

        private string? Write30_XmlSchemaUse(XmlSchemaUse v)
        {
            string? s = null;
            switch (v)
            {
                case XmlSchemaUse.@Optional: s = @"optional"; break;
                case XmlSchemaUse.@Prohibited: s = @"prohibited"; break;
                case XmlSchemaUse.@Required: s = @"required"; break;
                default: break;
            }
            return s;
        }

        private void Write31_XmlSchemaAttributeGroup(XmlSchemaAttributeGroup? o)
        {
            if (o is null) return;
            WriteStartElement("attributeGroup");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute(@"name", @"", ((string?)o.@Name));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteSortedItems(o.Attributes);
            Write33_XmlSchemaAnyAttribute((XmlSchemaAnyAttribute?)o.@AnyAttribute);
            WriteEndElement();
        }

        private void Write32_XmlSchemaAttributeGroupRef(XmlSchemaAttributeGroupRef? o)
        {
            if (o is null) return;
            WriteStartElement("attributeGroup");

            WriteAttribute(@"id", @"", ((string?)o.@Id));

            if (!o.RefName.IsEmpty)
            {
                WriteAttribute("ref", "", o.RefName);
            }
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteEndElement();
        }

        private void Write33_XmlSchemaAnyAttribute(XmlSchemaAnyAttribute? o)
        {
            if (o is null) return;
            WriteStartElement("anyAttribute");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute("namespace", "", ToString(o.NamespaceList));
            XmlSchemaContentProcessing process = o.@ProcessContents == XmlSchemaContentProcessing.@None ? XmlSchemaContentProcessing.Strict : o.@ProcessContents;
            WriteAttribute(@"processContents", @"", Write34_XmlSchemaContentProcessing(process));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteEndElement();
        }

        private string? Write34_XmlSchemaContentProcessing(XmlSchemaContentProcessing v)
        {
            string? s = null;
            switch (v)
            {
                case XmlSchemaContentProcessing.@Skip: s = @"skip"; break;
                case XmlSchemaContentProcessing.@Lax: s = @"lax"; break;
                case XmlSchemaContentProcessing.@Strict: s = @"strict"; break;
                default: break;
            }
            return s;
        }

        private void Write35_XmlSchemaComplexType(XmlSchemaComplexType o)
        {
            if (o is null) return;
            WriteStartElement("complexType");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute(@"name", @"", ((string?)o.@Name));
            WriteAttribute(@"final", @"", Write11_XmlSchemaDerivationMethod(o.FinalResolved));
            if (((bool)o.@IsAbstract) != false)
            {
                WriteAttribute(@"abstract", @"", XmlConvert.ToString((bool)((bool)o.@IsAbstract)));
            }
            WriteAttribute(@"block", @"", Write11_XmlSchemaDerivationMethod(o.BlockResolved));
            if (((bool)o.@IsMixed) != false)
            {
                WriteAttribute(@"mixed", @"", XmlConvert.ToString((bool)((bool)o.@IsMixed)));
            }
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            if (o.@ContentModel is XmlSchemaComplexContent)
            {
                Write41_XmlSchemaComplexContent((XmlSchemaComplexContent)o.@ContentModel);
            }
            else if (o.@ContentModel is XmlSchemaSimpleContent)
            {
                Write36_XmlSchemaSimpleContent((XmlSchemaSimpleContent)o.@ContentModel);
            }
            if (o.@Particle is XmlSchemaSequence)
            {
                Write54_XmlSchemaSequence((XmlSchemaSequence)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaGroupRef)
            {
                Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaChoice)
            {
                Write52_XmlSchemaChoice((XmlSchemaChoice)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaAll)
            {
                Write43_XmlSchemaAll((XmlSchemaAll)o.@Particle);
            }
            WriteSortedItems(o.Attributes);
            Write33_XmlSchemaAnyAttribute((XmlSchemaAnyAttribute?)o.@AnyAttribute);
            WriteEndElement();
        }

        private void Write36_XmlSchemaSimpleContent(XmlSchemaSimpleContent? o)
        {
            if (o is null) return;
            WriteStartElement("simpleContent");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            if (o.@Content is XmlSchemaSimpleContentRestriction)
            {
                Write40_XmlSchemaSimpleContentRestriction((XmlSchemaSimpleContentRestriction)o.@Content);
            }
            else if (o.@Content is XmlSchemaSimpleContentExtension)
            {
                Write38_XmlSchemaSimpleContentExtension((XmlSchemaSimpleContentExtension)o.@Content);
            }
            WriteEndElement();
        }

        private void Write38_XmlSchemaSimpleContentExtension(XmlSchemaSimpleContentExtension o)
        {
            if (o is null) return;
            WriteStartElement("extension");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            if (!o.@BaseTypeName.IsEmpty)
            {
                WriteAttribute(@"base", @"", o.@BaseTypeName);
            }
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteSortedItems(o.Attributes);
            Write33_XmlSchemaAnyAttribute((XmlSchemaAnyAttribute?)o.@AnyAttribute);
            WriteEndElement();
        }

        private void Write40_XmlSchemaSimpleContentRestriction(XmlSchemaSimpleContentRestriction? o)
        {
            if (o is null) return;
            WriteStartElement("restriction");
            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            if (!o.@BaseTypeName.IsEmpty)
            {
                WriteAttribute(@"base", @"", o.@BaseTypeName);
            }
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            Write9_XmlSchemaSimpleType((XmlSchemaSimpleType?)o.@BaseType);
            WriteFacets(o.Facets);
            WriteSortedItems(o.Attributes);
            Write33_XmlSchemaAnyAttribute((XmlSchemaAnyAttribute?)o.@AnyAttribute);
            WriteEndElement();
        }

        private void Write41_XmlSchemaComplexContent(XmlSchemaComplexContent? o)
        {
            if (o is null) return;
            WriteStartElement("complexContent");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute(@"mixed", @"", XmlConvert.ToString((bool)((bool)o.@IsMixed)));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            if (o.@Content is XmlSchemaComplexContentRestriction)
            {
                Write56_XmlSchemaComplexContentRestriction((XmlSchemaComplexContentRestriction)o.@Content);
            }
            else if (o.@Content is XmlSchemaComplexContentExtension)
            {
                Write42_XmlSchemaComplexContentExtension((XmlSchemaComplexContentExtension)o.@Content);
            }
            WriteEndElement();
        }

        private void Write42_XmlSchemaComplexContentExtension(XmlSchemaComplexContentExtension? o)
        {
            if (o is null) return;
            WriteStartElement("extension");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            if (!o.@BaseTypeName.IsEmpty)
            {
                WriteAttribute(@"base", @"", o.@BaseTypeName);
            }
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            if (o.@Particle is XmlSchemaSequence)
            {
                Write54_XmlSchemaSequence((XmlSchemaSequence)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaGroupRef)
            {
                Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaChoice)
            {
                Write52_XmlSchemaChoice((XmlSchemaChoice)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaAll)
            {
                Write43_XmlSchemaAll((XmlSchemaAll)o.@Particle);
            }
            WriteSortedItems(o.Attributes);
            Write33_XmlSchemaAnyAttribute((XmlSchemaAnyAttribute?)o.@AnyAttribute);
            WriteEndElement();
        }

        private void Write43_XmlSchemaAll(XmlSchemaAll o)
        {
            if (o is null) return;
            WriteStartElement("all");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
            WriteAttribute("maxOccurs", "", o.MaxOccurs == decimal.MaxValue ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteSortedItems(o.@Items);
            WriteEndElement();
        }

        private void Write46_XmlSchemaElement(XmlSchemaElement? o)
        {
            if (o is null) return;
            WriteStartElement("element");
            WriteAttribute(@"id", @"", o.Id);
            WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
            WriteAttribute("maxOccurs", "", o.MaxOccurs == decimal.MaxValue ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
            if (((bool)o.@IsAbstract) != false)
            {
                WriteAttribute(@"abstract", @"", XmlConvert.ToString((bool)((bool)o.@IsAbstract)));
            }
            WriteAttribute(@"block", @"", Write11_XmlSchemaDerivationMethod(o.BlockResolved));
            WriteAttribute(@"default", @"", o.DefaultValue);
            WriteAttribute(@"final", @"", Write11_XmlSchemaDerivationMethod(o.FinalResolved));
            WriteAttribute(@"fixed", @"", o.FixedValue);
            if (o.Parent != null && !(o.Parent is XmlSchema))
            {
                if (o.QualifiedName != null && !o.QualifiedName.IsEmpty && o.QualifiedName.Namespace != null && o.QualifiedName.Namespace.Length != 0)
                {
                    WriteAttribute(@"form", @"", "qualified");
                }
                else
                {
                    WriteAttribute(@"form", @"", "unqualified");
                }
            }
            if (o.Name != null && o.Name.Length != 0)
            {
                WriteAttribute(@"name", @"", o.Name);
            }
            if (o.IsNillable)
            {
                WriteAttribute(@"nillable", @"", XmlConvert.ToString(o.IsNillable));
            }
            if (!o.SubstitutionGroup.IsEmpty)
            {
                WriteAttribute("substitutionGroup", "", o.SubstitutionGroup);
            }
            if (!o.RefName.IsEmpty)
            {
                WriteAttribute("ref", "", o.RefName);
            }
            else if (!o.SchemaTypeName.IsEmpty)
            {
                WriteAttribute("type", "", o.SchemaTypeName);
            }

            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation(o.Annotation);
            if (o.SchemaType is XmlSchemaComplexType)
            {
                Write35_XmlSchemaComplexType((XmlSchemaComplexType)o.SchemaType);
            }
            else if (o.SchemaType is XmlSchemaSimpleType)
            {
                Write9_XmlSchemaSimpleType((XmlSchemaSimpleType)o.SchemaType);
            }
            WriteSortedItems(o.Constraints);
            WriteEndElement();
        }

        private void Write47_XmlSchemaKey(XmlSchemaKey? o)
        {
            if (o is null) return;
            WriteStartElement("key");
            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute(@"name", @"", ((string?)o.@Name));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            Write49_XmlSchemaXPath(@"selector", @"", (XmlSchemaXPath?)o.@Selector);
            {
                XmlSchemaObjectCollection a = (XmlSchemaObjectCollection)o.@Fields;
                if (a != null)
                {
                    for (int ia = 0; ia < a.Count; ia++)
                    {
                        Write49_XmlSchemaXPath(@"field", @"", (XmlSchemaXPath)a[ia]);
                    }
                }
            }
            WriteEndElement();
        }

        private void Write48_XmlSchemaIdentityConstraint(XmlSchemaIdentityConstraint? o)
        {
            if (o is null) return;
            System.Type t = o.GetType();
            if (t == typeof(XmlSchemaUnique))
            {
                Write51_XmlSchemaUnique((XmlSchemaUnique)o);
                return;
            }
            else if (t == typeof(XmlSchemaKeyref))
            {
                Write50_XmlSchemaKeyref((XmlSchemaKeyref)o);
                return;
            }
            else if (t == typeof(XmlSchemaKey))
            {
                Write47_XmlSchemaKey((XmlSchemaKey)o);
                return;
            }
        }

        private void Write49_XmlSchemaXPath(string name, string ns, XmlSchemaXPath? o)
        {
            if (o is null) return;
            WriteStartElement(name);
            WriteAttribute(@"id", @"", o.@Id);
            WriteAttribute(@"xpath", @"", o.@XPath);
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteEndElement();
        }

        private void Write50_XmlSchemaKeyref(XmlSchemaKeyref? o)
        {
            if (o is null) return;
            WriteStartElement("keyref");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute(@"name", @"", ((string?)o.@Name));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            // UNDONE compare reference here
            WriteAttribute(@"refer", @"", o.@Refer);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            Write49_XmlSchemaXPath(@"selector", @"", (XmlSchemaXPath?)o.@Selector);
            {
                XmlSchemaObjectCollection a = (XmlSchemaObjectCollection)o.@Fields;
                if (a != null)
                {
                    for (int ia = 0; ia < a.Count; ia++)
                    {
                        Write49_XmlSchemaXPath(@"field", @"", (XmlSchemaXPath)a[ia]);
                    }
                }
            }
            WriteEndElement();
        }

        private void Write51_XmlSchemaUnique(XmlSchemaUnique? o)
        {
            if (o is null) return;
            WriteStartElement("unique");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute(@"name", @"", ((string?)o.@Name));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            Write49_XmlSchemaXPath("selector", "", (XmlSchemaXPath?)o.@Selector);
            XmlSchemaObjectCollection a = (XmlSchemaObjectCollection)o.@Fields;
            if (a != null)
            {
                for (int ia = 0; ia < a.Count; ia++)
                {
                    Write49_XmlSchemaXPath("field", "", (XmlSchemaXPath)a[ia]);
                }
            }
            WriteEndElement();
        }

        private void Write52_XmlSchemaChoice(XmlSchemaChoice? o)
        {
            if (o is null) return;
            WriteStartElement("choice");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
            WriteAttribute(@"maxOccurs", @"", o.MaxOccurs == decimal.MaxValue ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteSortedItems(o.@Items);
            WriteEndElement();
        }

        private void Write53_XmlSchemaAny(XmlSchemaAny? o)
        {
            if (o is null) return;
            WriteStartElement("any");

            WriteAttribute(@"id", @"", o.@Id);
            WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
            WriteAttribute(@"maxOccurs", @"", o.MaxOccurs == decimal.MaxValue ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
            WriteAttribute(@"namespace", @"", ToString(o.NamespaceList));
            XmlSchemaContentProcessing process = o.@ProcessContents == XmlSchemaContentProcessing.@None ? XmlSchemaContentProcessing.Strict : o.@ProcessContents;
            WriteAttribute(@"processContents", @"", Write34_XmlSchemaContentProcessing(process));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteEndElement();
        }

        private void Write54_XmlSchemaSequence(XmlSchemaSequence? o)
        {
            if (o is null) return;
            WriteStartElement("sequence");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
            WriteAttribute("maxOccurs", "", o.MaxOccurs == decimal.MaxValue ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            XmlSchemaObjectCollection a = (XmlSchemaObjectCollection)o.@Items;
            if (a != null)
            {
                for (int ia = 0; ia < a.Count; ia++)
                {
                    XmlSchemaObject ai = (XmlSchemaObject)a[ia];
                    if (ai is XmlSchemaAny)
                    {
                        Write53_XmlSchemaAny((XmlSchemaAny)ai);
                    }
                    else if (ai is XmlSchemaSequence)
                    {
                        Write54_XmlSchemaSequence((XmlSchemaSequence)ai);
                    }
                    else if (ai is XmlSchemaChoice)
                    {
                        Write52_XmlSchemaChoice((XmlSchemaChoice)ai);
                    }
                    else if (ai is XmlSchemaElement)
                    {
                        Write46_XmlSchemaElement((XmlSchemaElement)ai);
                    }
                    else if (ai is XmlSchemaGroupRef)
                    {
                        Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)ai);
                    }
                }
            }
            WriteEndElement();
        }

        private void Write55_XmlSchemaGroupRef(XmlSchemaGroupRef? o)
        {
            if (o is null) return;
            WriteStartElement("group");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
            WriteAttribute(@"maxOccurs", @"", o.MaxOccurs == decimal.MaxValue ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));

            if (!o.RefName.IsEmpty)
            {
                WriteAttribute("ref", "", o.RefName);
            }
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            WriteEndElement();
        }

        private void Write56_XmlSchemaComplexContentRestriction(XmlSchemaComplexContentRestriction? o)
        {
            if (o is null) return;
            WriteStartElement("restriction");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);

            if (!o.@BaseTypeName.IsEmpty)
            {
                WriteAttribute(@"base", @"", o.@BaseTypeName);
            }

            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            if (o.@Particle is XmlSchemaSequence)
            {
                Write54_XmlSchemaSequence((XmlSchemaSequence)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaGroupRef)
            {
                Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaChoice)
            {
                Write52_XmlSchemaChoice((XmlSchemaChoice)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaAll)
            {
                Write43_XmlSchemaAll((XmlSchemaAll)o.@Particle);
            }
            WriteSortedItems(o.Attributes);
            Write33_XmlSchemaAnyAttribute((XmlSchemaAnyAttribute?)o.@AnyAttribute);
            WriteEndElement();
        }

        private void Write57_XmlSchemaGroup(XmlSchemaGroup? o)
        {
            if (o is null) return;
            WriteStartElement("group");

            WriteAttribute(@"id", @"", ((string?)o.@Id));
            WriteAttribute(@"name", @"", ((string?)o.@Name));
            WriteAttributes((XmlAttribute[]?)o.@UnhandledAttributes, o);
            Write5_XmlSchemaAnnotation((XmlSchemaAnnotation?)o.@Annotation);
            if (o.@Particle is XmlSchemaSequence)
            {
                Write54_XmlSchemaSequence((XmlSchemaSequence)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaChoice)
            {
                Write52_XmlSchemaChoice((XmlSchemaChoice)o.@Particle);
            }
            else if (o.@Particle is XmlSchemaAll)
            {
                Write43_XmlSchemaAll((XmlSchemaAll)o.@Particle);
            }
            WriteEndElement();
        }
    }
}
