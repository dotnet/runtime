// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.Versioning;

namespace System.Xml.Schema
{
    internal class XNodeValidator
    {
        XmlSchemaSet schemas;
        ValidationEventHandler validationEventHandler;

        XObject source;
        bool addSchemaInfo;
        XmlNamespaceManager namespaceManager;
        XmlSchemaValidator validator;

        Dictionary<XmlSchemaInfo, XmlSchemaInfo> schemaInfos;
        ArrayList defaultAttributes;
        XName xsiTypeName;
        XName xsiNilName;

        public XNodeValidator(XmlSchemaSet schemas, ValidationEventHandler validationEventHandler)
        {
            this.schemas = schemas;
            this.validationEventHandler = validationEventHandler;

            XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            xsiTypeName = xsi.GetName("type");
            xsiNilName = xsi.GetName("nil");
        }

        public void Validate(XObject source, XmlSchemaObject partialValidationType, bool addSchemaInfo)
        {
            this.source = source;
            this.addSchemaInfo = addSchemaInfo;
            XmlSchemaValidationFlags validationFlags = XmlSchemaValidationFlags.AllowXmlAttributes;
            XmlNodeType nt = source.NodeType;
            switch (nt)
            {
                case XmlNodeType.Document:
                    source = ((XDocument)source).Root;
                    if (source == null) throw new InvalidOperationException(SR.InvalidOperation_MissingRoot);
                    validationFlags |= XmlSchemaValidationFlags.ProcessIdentityConstraints;
                    break;
                case XmlNodeType.Element:
                    break;
                case XmlNodeType.Attribute:
                    if (((XAttribute)source).IsNamespaceDeclaration) goto default;
                    if (source.Parent == null) throw new InvalidOperationException(SR.InvalidOperation_MissingParent);
                    break;
                default:
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadNodeType, nt));
            }
            namespaceManager = new XmlNamespaceManager(schemas.NameTable);
            PushAncestorsAndSelf(source.Parent);
            validator = new XmlSchemaValidator(schemas.NameTable, schemas, namespaceManager, validationFlags);
            validator.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            validator.XmlResolver = null;
            if (partialValidationType != null)
            {
                validator.Initialize(partialValidationType);
            }
            else
            {
                validator.Initialize();
            }

            IXmlLineInfo orginal = SaveLineInfo(source);
            if (nt == XmlNodeType.Attribute)
            {
                ValidateAttribute((XAttribute)source);
            }
            else
            {
                ValidateElement((XElement)source);
            }
            validator.EndValidation();
            RestoreLineInfo(orginal);
        }

        XmlSchemaInfo GetDefaultAttributeSchemaInfo(XmlSchemaAttribute sa)
        {
            XmlSchemaInfo si = new XmlSchemaInfo();
            si.IsDefault = true;
            si.IsNil = false;
            si.SchemaAttribute = sa;
            XmlSchemaSimpleType st = sa.AttributeSchemaType;
            si.SchemaType = st;
            if (st.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
            {
                string value = GetDefaultValue(sa);
                foreach (XmlSchemaSimpleType mt in ((XmlSchemaSimpleTypeUnion)st.Content).BaseMemberTypes)
                {
                    object typedValue = null;
                    try
                    {
                        typedValue = mt.Datatype.ParseValue(value, schemas.NameTable, namespaceManager);
                    }
                    catch (XmlSchemaException)
                    {
                    }
                    if (typedValue != null)
                    {
                        si.MemberType = mt;
                        break;
                    }
                }
            }
            si.Validity = XmlSchemaValidity.Valid;
            return si;
        }

        string GetDefaultValue(XmlSchemaAttribute sa)
        {
            XmlQualifiedName name = sa.RefName;
            if (!name.IsEmpty)
            {
                sa = schemas.GlobalAttributes[name] as XmlSchemaAttribute;
                if (sa == null) return null;
            }
            string s = sa.FixedValue;
            if (s != null) return s;
            return sa.DefaultValue;
        }

        string GetDefaultValue(XmlSchemaElement se)
        {
            XmlQualifiedName name = se.RefName;
            if (!name.IsEmpty)
            {
                se = schemas.GlobalElements[name] as XmlSchemaElement;
                if (se == null) return null;
            }
            string s = se.FixedValue;
            if (s != null) return s;
            return se.DefaultValue;
        }

        void ReplaceSchemaInfo(XObject o, XmlSchemaInfo schemaInfo)
        {
            if (schemaInfos == null)
            {
                schemaInfos = new Dictionary<XmlSchemaInfo, XmlSchemaInfo>(new XmlSchemaInfoEqualityComparer());
            }
            XmlSchemaInfo si = o.Annotation<XmlSchemaInfo>();
            if (si != null)
            {
                if (!schemaInfos.ContainsKey(si))
                {
                    schemaInfos.Add(si, si);
                }
                o.RemoveAnnotations<XmlSchemaInfo>();
            }
            if (!schemaInfos.TryGetValue(schemaInfo, out si))
            {
                si = schemaInfo;
                schemaInfos.Add(si, si);
            }
            o.AddAnnotation(si);
        }

        void PushAncestorsAndSelf(XElement e)
        {
            while (e != null)
            {
                XAttribute a = e.lastAttr;
                if (a != null)
                {
                    do
                    {
                        a = a.next;
                        if (a.IsNamespaceDeclaration)
                        {
                            string localName = a.Name.LocalName;
                            if (localName == "xmlns")
                            {
                                localName = string.Empty;
                            }
                            if (!namespaceManager.HasNamespace(localName))
                            {
                                namespaceManager.AddNamespace(localName, a.Value);
                            }
                        }
                    } while (a != e.lastAttr);
                }
                e = e.parent as XElement;
            }
        }

        void PushElement(XElement e, ref string xsiType, ref string xsiNil)
        {
            namespaceManager.PushScope();
            XAttribute a = e.lastAttr;
            if (a != null)
            {
                do
                {
                    a = a.next;
                    if (a.IsNamespaceDeclaration)
                    {
                        string localName = a.Name.LocalName;
                        if (localName == "xmlns")
                        {
                            localName = string.Empty;
                        }
                        namespaceManager.AddNamespace(localName, a.Value);
                    }
                    else
                    {
                        XName name = a.Name;
                        if (name == xsiTypeName)
                        {
                            xsiType = a.Value;
                        }
                        else if (name == xsiNilName)
                        {
                            xsiNil = a.Value;
                        }
                    }
                } while (a != e.lastAttr);
            }
        }

        IXmlLineInfo SaveLineInfo(XObject source)
        {
            IXmlLineInfo previousLineInfo = validator.LineInfoProvider;
            validator.LineInfoProvider = source as IXmlLineInfo;
            return previousLineInfo;
        }

        void RestoreLineInfo(IXmlLineInfo originalLineInfo)
        {
            validator.LineInfoProvider = originalLineInfo;
        }


        void ValidateAttribute(XAttribute a)
        {
            IXmlLineInfo original = SaveLineInfo(a);
            XmlSchemaInfo si = addSchemaInfo ? new XmlSchemaInfo() : null;
            source = a;
            validator.ValidateAttribute(a.Name.LocalName, a.Name.NamespaceName, a.Value, si);
            if (addSchemaInfo)
            {
                ReplaceSchemaInfo(a, si);
            }
            RestoreLineInfo(original);
        }

        void ValidateAttributes(XElement e)
        {
            XAttribute a = e.lastAttr;
            IXmlLineInfo orginal = SaveLineInfo(a);
            if (a != null)
            {
                do
                {
                    a = a.next;
                    if (!a.IsNamespaceDeclaration)
                    {
                        ValidateAttribute(a);
                    }
                } while (a != e.lastAttr);
                source = e;
            }
            if (addSchemaInfo)
            {
                if (defaultAttributes == null)
                {
                    defaultAttributes = new ArrayList();
                }
                else
                {
                    defaultAttributes.Clear();
                }
                validator.GetUnspecifiedDefaultAttributes(defaultAttributes);
                foreach (XmlSchemaAttribute sa in defaultAttributes)
                {
                    a = new XAttribute(XNamespace.Get(sa.QualifiedName.Namespace).GetName(sa.QualifiedName.Name), GetDefaultValue(sa));
                    ReplaceSchemaInfo(a, GetDefaultAttributeSchemaInfo(sa));
                    e.Add(a);
                }
            }
            RestoreLineInfo(orginal);
        }

        void ValidateElement(XElement e)
        {
            XmlSchemaInfo si = addSchemaInfo ? new XmlSchemaInfo() : null;
            string xsiType = null;
            string xsiNil = null;
            PushElement(e, ref xsiType, ref xsiNil);
            IXmlLineInfo original = SaveLineInfo(e);
            source = e;
            validator.ValidateElement(e.Name.LocalName, e.Name.NamespaceName, si, xsiType, xsiNil, null, null);
            ValidateAttributes(e);
            validator.ValidateEndOfAttributes(si);
            ValidateNodes(e);
            validator.ValidateEndElement(si);
            if (addSchemaInfo)
            {
                if (si.Validity == XmlSchemaValidity.Valid && si.IsDefault)
                {
                    e.Value = GetDefaultValue(si.SchemaElement);
                }
                ReplaceSchemaInfo(e, si);
            }
            RestoreLineInfo(original);
            namespaceManager.PopScope();
        }

        void ValidateNodes(XElement e)
        {
            XNode n = e.content as XNode;
            IXmlLineInfo orginal = SaveLineInfo(n);
            if (n != null)
            {
                do
                {
                    n = n.next;
                    XElement c = n as XElement;
                    if (c != null)
                    {
                        ValidateElement(c);
                    }
                    else
                    {
                        XText t = n as XText;
                        if (t != null)
                        {
                            string s = t.Value;
                            if (s.Length > 0)
                            {
                                validator.LineInfoProvider = t as IXmlLineInfo;
                                validator.ValidateText(s);
                            }
                        }
                    }
                } while (n != e.content);
                source = e;
            }
            else
            {
                string s = e.content as string;
                if (s != null && s.Length > 0)
                {
                    validator.ValidateText(s);
                }
            }
            RestoreLineInfo(orginal);
        }

        void ValidationCallback(object sender, ValidationEventArgs e)
        {
            if (validationEventHandler != null)
            {
                validationEventHandler(source, e);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                throw e.Exception;
            }
        }
    }

    internal class XmlSchemaInfoEqualityComparer : IEqualityComparer<XmlSchemaInfo>
    {
        public bool Equals(XmlSchemaInfo si1, XmlSchemaInfo si2)
        {
            if (si1 == si2) return true;
            if (si1 == null || si2 == null) return false;
            return si1.ContentType == si2.ContentType &&
                   si1.IsDefault == si2.IsDefault &&
                   si1.IsNil == si2.IsNil &&
                   (object)si1.MemberType == (object)si2.MemberType &&
                   (object)si1.SchemaAttribute == (object)si2.SchemaAttribute &&
                   (object)si1.SchemaElement == (object)si2.SchemaElement &&
                   (object)si1.SchemaType == (object)si2.SchemaType &&
                   si1.Validity == si2.Validity;
        }

        public int GetHashCode(XmlSchemaInfo si)
        {
            if (si == null) return 0;
            int h = (int)si.ContentType;
            if (si.IsDefault)
            {
                h ^= 1;
            }
            if (si.IsNil)
            {
                h ^= 1;
            }
            XmlSchemaSimpleType memberType = si.MemberType;
            if (memberType != null)
            {
                h ^= memberType.GetHashCode();
            }
            XmlSchemaAttribute schemaAttribute = si.SchemaAttribute;
            if (schemaAttribute != null)
            {
                h ^= schemaAttribute.GetHashCode();
            }
            XmlSchemaElement schemaElement = si.SchemaElement;
            if (schemaElement != null)
            {
                h ^= schemaElement.GetHashCode();
            }
            XmlSchemaType schemaType = si.SchemaType;
            if (schemaType != null)
            {
                h ^= schemaType.GetHashCode();
            }
            h ^= (int)si.Validity;
            return h;
        }
    }

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the schema information that has been assigned to the <see cref="XElement"/> as a result of schema validation.
        /// </summary>
        /// <param name="source">Extension point</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Reviewed by the design group.")]
        public static IXmlSchemaInfo GetSchemaInfo(this XElement source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Annotation<IXmlSchemaInfo>();
        }

        /// <summary>
        /// Gets the schema information that has been assigned to the <see cref="XAttribute"/> as a result of schema validation.
        /// </summary>
        /// <param name="source">Extension point</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Reviewed by the design group.")]
        public static IXmlSchemaInfo GetSchemaInfo(this XAttribute source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Annotation<IXmlSchemaInfo>();
        }

        /// <summary>
        /// Validate a <see cref="XDocument"/>
        /// </summary>
        /// <param name="source">Extension point</param>
        /// <param name="schemas">The <see cref="XmlSchemaSet"/> used for validation</param>
        /// <param name="validationEventHandler">The <see cref="ValidationEventHandler"/> 
        /// that receives schema validation warnings and errors encountered during schema 
        /// validation</param>
        public static void Validate(this XDocument source, XmlSchemaSet schemas, ValidationEventHandler validationEventHandler)
        {
            source.Validate(schemas, validationEventHandler, false);
        }

        /// <summary>
        /// Validate a <see cref="XDocument"/>
        /// </summary>
        /// <param name="source">Extension point</param>
        /// <param name="schemas">The <see cref="XmlSchemaSet"/> used for validation</param>
        /// <param name="validationEventHandler">The <see cref="ValidationEventHandler"/> 
        /// that receives schema validation warnings and errors encountered during schema 
        /// validation</param>
        /// <param name="addSchemaInfo">If enabled the <see cref="XDocument"/> and the corresponding 
        /// subtree is augmented with PSVI in the form of <see cref="IXmlSchemaInfo"/> annotations, 
        /// default attributes and default element values</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Reviewed by the design group.")]
        public static void Validate(this XDocument source, XmlSchemaSet schemas, ValidationEventHandler validationEventHandler, bool addSchemaInfo)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (schemas == null) throw new ArgumentNullException(nameof(schemas));
            new XNodeValidator(schemas, validationEventHandler).Validate(source, null, addSchemaInfo);
        }

        /// <summary>
        /// Validate a <see cref="XElement"/>
        /// </summary>
        /// <param name="source">Extension point</param>
        /// <param name="partialValidationType">An <see cref="XmlSchemaElement"/> or 
        /// <see cref="XmlSchemaType"/> object used to initialize the partial validation 
        /// context</param>
        /// <param name="schemas">The <see cref="XmlSchemaSet"/> used for validation</param>
        /// <param name="validationEventHandler">The <see cref="ValidationEventHandler"/> that 
        /// receives schema validation warnings and errors encountered during schema 
        /// validation</param>
        public static void Validate(this XElement source, XmlSchemaObject partialValidationType, XmlSchemaSet schemas, ValidationEventHandler validationEventHandler)
        {
            source.Validate(partialValidationType, schemas, validationEventHandler, false);
        }

        /// <summary>
        /// Validate a <see cref="XElement"/>
        /// </summary>
        /// <param name="source">Extension point</param>
        /// <param name="partialValidationType">An <see cref="XmlSchemaElement"/> or 
        /// <see cref="XmlSchemaType"/> object used to initialize the partial validation 
        /// context</param>
        /// <param name="schemas">The <see cref="XmlSchemaSet"/> used for validation</param>
        /// <param name="validationEventHandler">The <see cref="ValidationEventHandler"/> that 
        /// receives schema validation warnings and errors encountered during schema 
        /// validation</param>
        /// <param name="addSchemaInfo">If enabled the <see cref="XElement"/> and the corresponding 
        /// subtree is augmented with PSVI in the form of <see cref="IXmlSchemaInfo"/> annotations, 
        /// default attributes and default element values</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Reviewed by the design group.")]
        public static void Validate(this XElement source, XmlSchemaObject partialValidationType, XmlSchemaSet schemas, ValidationEventHandler validationEventHandler, bool addSchemaInfo)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (partialValidationType == null) throw new ArgumentNullException(nameof(partialValidationType));
            if (schemas == null) throw new ArgumentNullException(nameof(schemas));
            new XNodeValidator(schemas, validationEventHandler).Validate(source, partialValidationType, addSchemaInfo);
        }

        /// <summary>
        /// Validate a <see cref="XAttribute"/>
        /// </summary>
        /// <param name="source">Extension point</param>
        /// <param name="partialValidationType">An <see cref="XmlSchemaAttribute"/> or 
        /// <see cref="XmlSchemaType"/> object used to initialize the partial validation 
        /// context</param>
        /// <param name="schemas">The <see cref="XmlSchemaSet"/> used for validation</param>
        /// <param name="validationEventHandler">The <see cref="ValidationEventHandler"/> that 
        /// receives schema validation warnings and errors encountered during schema 
        /// validation</param>
        public static void Validate(this XAttribute source, XmlSchemaObject partialValidationType, XmlSchemaSet schemas, ValidationEventHandler validationEventHandler)
        {
            source.Validate(partialValidationType, schemas, validationEventHandler, false);
        }

        /// <summary>
        /// Validate a <see cref="XAttribute"/>
        /// </summary>
        /// <param name="source">Extension point</param>
        /// <param name="partialValidationType">An <see cref="XmlSchemaAttribute"/> or 
        /// <see cref="XmlSchemaType"/> object used to initialize the partial validation 
        /// context</param>
        /// <param name="schemas">The <see cref="XmlSchemaSet"/> used for validation</param>
        /// <param name="validationEventHandler">The <see cref="ValidationEventHandler"/> that 
        /// receives schema validation warnings and errors encountered during schema 
        /// validation</param>
        /// <param name="addSchemaInfo">If enabled the <see cref="XAttribute"/> is augmented with PSVI 
        /// in the form of <see cref="IXmlSchemaInfo"/> annotations, default attributes and 
        /// default element values</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Reviewed by the design group.")]
        public static void Validate(this XAttribute source, XmlSchemaObject partialValidationType, XmlSchemaSet schemas, ValidationEventHandler validationEventHandler, bool addSchemaInfo)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (partialValidationType == null) throw new ArgumentNullException(nameof(partialValidationType));
            if (schemas == null) throw new ArgumentNullException(nameof(schemas));
            new XNodeValidator(schemas, validationEventHandler).Validate(source, partialValidationType, addSchemaInfo);
        }
    }
}
