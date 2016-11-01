// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;
    using System.Collections;
    using System.Xml.Schema;
    using System.Xml;
    using System.ComponentModel;
    using System.Diagnostics;

    /// <include file='doc\SoapSchemaExporter.uex' path='docs/doc[@for="SoapSchemaExporter"]/*' />
    /// <internalonly/>
    public class SoapSchemaExporter
    {
        internal const XmlSchemaForm elementFormDefault = XmlSchemaForm.Qualified;
        private XmlSchemas _schemas;
        private Hashtable _types = new Hashtable();      // StructMapping/EnumMapping -> XmlSchemaComplexType/XmlSchemaSimpleType
        private bool _exportedRoot;
        private TypeScope _scope;
        private XmlDocument _document;

        private static XmlQualifiedName s_arrayQName = new XmlQualifiedName(Soap.Array, Soap.Encoding);
        private static XmlQualifiedName s_arrayTypeQName = new XmlQualifiedName(Soap.ArrayType, Soap.Encoding);

        /// <include file='doc\SoapSchemaExporter.uex' path='docs/doc[@for="SoapSchemaExporter.SoapSchemaExporter"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapSchemaExporter(XmlSchemas schemas)
        {
            _schemas = schemas;
        }

        /// <include file='doc\SoapSchemaExporter.uex' path='docs/doc[@for="SoapSchemaExporter.ExportTypeMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void ExportTypeMapping(XmlTypeMapping xmlTypeMapping)
        {
            CheckScope(xmlTypeMapping.Scope);
            ExportTypeMapping(xmlTypeMapping.Mapping, null);
        }

        /// <include file='doc\SoapSchemaExporter.uex' path='docs/doc[@for="SoapSchemaExporter.ExportMembersMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void ExportMembersMapping(XmlMembersMapping xmlMembersMapping)
        {
            ExportMembersMapping(xmlMembersMapping, false);
        }

        /// <include file='doc\SoapSchemaExporter.uex' path='docs/doc[@for="SoapSchemaExporter.ExportMembersMapping1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void ExportMembersMapping(XmlMembersMapping xmlMembersMapping, bool exportEnclosingType)
        {
            CheckScope(xmlMembersMapping.Scope);
            MembersMapping membersMapping = (MembersMapping)xmlMembersMapping.Accessor.Mapping;
            if (exportEnclosingType)
            {
                ExportTypeMapping(membersMapping, null);
            }
            else
            {
                foreach (MemberMapping memberMapping in membersMapping.Members)
                {
                    if (memberMapping.Elements.Length > 0)
                        ExportTypeMapping(memberMapping.Elements[0].Mapping, null);
                }
            }
        }

        private void CheckScope(TypeScope scope)
        {
            if (_scope == null)
            {
                _scope = scope;
            }
            else if (_scope != scope)
            {
                throw new InvalidOperationException(SR.XmlMappingsScopeMismatch);
            }
        }

        internal XmlDocument Document
        {
            get
            {
                if (_document == null) _document = new XmlDocument();
                return _document;
            }
        }

        private void CheckForDuplicateType(string newTypeName, string newNamespace)
        {
            XmlSchema schema = _schemas[newNamespace];
            if (schema != null)
            {
                foreach (XmlSchemaObject o in schema.Items)
                {
                    XmlSchemaType type = o as XmlSchemaType;
                    if (type != null && type.Name == newTypeName)
                        throw new InvalidOperationException(SR.Format(SR.XmlDuplicateTypeName, newTypeName, newNamespace));
                }
            }
        }

        private void AddSchemaItem(XmlSchemaObject item, string ns, string referencingNs)
        {
            if (!SchemaContainsItem(item, ns))
            {
                XmlSchema schema = _schemas[ns];
                if (schema == null)
                {
                    schema = new XmlSchema();
                    schema.TargetNamespace = ns == null || ns.Length == 0 ? null : ns;

#pragma warning disable 429   // unreachable code detected:  elementFormDefault is const so it will never be Unqualified
                    schema.ElementFormDefault = elementFormDefault == XmlSchemaForm.Unqualified ? XmlSchemaForm.None : elementFormDefault;
#pragma warning restore 429
                    _schemas.Add(schema);
                }
                schema.Items.Add(item);
            }
            if (referencingNs != null)
                AddSchemaImport(ns, referencingNs);
        }

        private void AddSchemaImport(string ns, string referencingNs)
        {
            if (referencingNs == null || ns == null) return;
            if (ns == referencingNs) return;
            XmlSchema schema = _schemas[referencingNs];
            if (schema == null) throw new InvalidOperationException(SR.Format(SR.XmlMissingSchema, referencingNs));
            if (ns != null && ns.Length > 0 && FindImport(schema, ns) == null)
            {
                XmlSchemaImport import = new XmlSchemaImport();
                import.Namespace = ns;
                schema.Includes.Add(import);
            }
        }

        private bool SchemaContainsItem(XmlSchemaObject item, string ns)
        {
            XmlSchema schema = _schemas[ns];
            if (schema != null)
            {
                return schema.Items.Contains(item);
            }
            return false;
        }

        private XmlSchemaImport FindImport(XmlSchema schema, string ns)
        {
            foreach (object item in schema.Includes)
            {
                if (item is XmlSchemaImport)
                {
                    XmlSchemaImport import = (XmlSchemaImport)item;
                    if (import.Namespace == ns)
                    {
                        return import;
                    }
                }
            }
            return null;
        }

        private XmlQualifiedName ExportTypeMapping(TypeMapping mapping, string ns)
        {
            if (mapping is ArrayMapping)
                return ExportArrayMapping((ArrayMapping)mapping, ns);
            else if (mapping is EnumMapping)
                return ExportEnumMapping((EnumMapping)mapping, ns);
            else if (mapping is PrimitiveMapping)
            {
                PrimitiveMapping pm = (PrimitiveMapping)mapping;
                if (pm.TypeDesc.IsXsdType)
                {
                    return ExportPrimitiveMapping(pm);
                }
                else
                {
                    return ExportNonXsdPrimitiveMapping(pm, ns);
                }
            }
            else if (mapping is StructMapping)
                return ExportStructMapping((StructMapping)mapping, ns);
            else if (mapping is NullableMapping)
                return ExportTypeMapping(((NullableMapping)mapping).BaseMapping, ns);
            else if (mapping is MembersMapping)
                return ExportMembersMapping((MembersMapping)mapping, ns);
            else
                throw new ArgumentException(SR.XmlInternalError, nameof(mapping));
        }

        private XmlQualifiedName ExportNonXsdPrimitiveMapping(PrimitiveMapping mapping, string ns)
        {
            XmlSchemaType type = mapping.TypeDesc.DataType;
            if (!SchemaContainsItem(type, UrtTypes.Namespace))
            {
                AddSchemaItem(type, UrtTypes.Namespace, ns);
            }
            else
            {
                AddSchemaImport(UrtTypes.Namespace, ns);
            }
            return new XmlQualifiedName(mapping.TypeDesc.DataType.Name, UrtTypes.Namespace);
        }

        private XmlQualifiedName ExportPrimitiveMapping(PrimitiveMapping mapping)
        {
            return new XmlQualifiedName(mapping.TypeDesc.DataType.Name, XmlSchema.Namespace);
        }

        private XmlQualifiedName ExportArrayMapping(ArrayMapping mapping, string ns)
        {
            // for the Rpc ArrayMapping  different mappings could have the same schema type
            // we link all mappings corresponding to the same type together
            // loop through all mapping that will map to the same complexType, and export only one, 
            // the obvious choice is the last one.
            while (mapping.Next != null)
            {
                mapping = mapping.Next;
            }

            XmlSchemaComplexType type = (XmlSchemaComplexType)_types[mapping];
            if (type == null)
            {
                CheckForDuplicateType(mapping.TypeName, mapping.Namespace);
                type = new XmlSchemaComplexType();
                type.Name = mapping.TypeName;
                _types.Add(mapping, type);

                // we need to add the type first, to make sure that the schema get created
                AddSchemaItem(type, mapping.Namespace, ns);
                AddSchemaImport(Soap.Encoding, mapping.Namespace);
                AddSchemaImport(Wsdl.Namespace, mapping.Namespace);

                XmlSchemaComplexContentRestriction restriction = new XmlSchemaComplexContentRestriction();
                XmlQualifiedName qname = ExportTypeMapping(mapping.Elements[0].Mapping, mapping.Namespace);

                if (qname.IsEmpty)
                {
                    // this is a root mapping
                    qname = new XmlQualifiedName(Soap.UrType, XmlSchema.Namespace);
                }
                //<attribute ref="soapenc:arrayType" wsdl:arrayType="xsd:float[]"/> 
                XmlSchemaAttribute attr = new XmlSchemaAttribute();
                attr.RefName = s_arrayTypeQName;
                XmlAttribute attribute = new XmlAttribute("wsdl", Wsdl.ArrayType, Wsdl.Namespace, Document);
                attribute.Value = qname.Namespace + ":" + qname.Name + "[]";

                attr.UnhandledAttributes = new XmlAttribute[] { attribute };
                restriction.Attributes.Add(attr);
                restriction.BaseTypeName = s_arrayQName;
                XmlSchemaComplexContent model = new XmlSchemaComplexContent();
                model.Content = restriction;
                type.ContentModel = model;
                if (qname.Namespace != XmlSchema.Namespace)
                    AddSchemaImport(qname.Namespace, mapping.Namespace);
            }
            else
            {
                AddSchemaImport(mapping.Namespace, ns);
            }
            return new XmlQualifiedName(mapping.TypeName, mapping.Namespace);
        }

        private void ExportElementAccessors(XmlSchemaGroupBase group, ElementAccessor[] accessors, bool repeats, bool valueTypeOptional, string ns)
        {
            if (accessors.Length == 0) return;
            if (accessors.Length == 1)
            {
                ExportElementAccessor(group, accessors[0], repeats, valueTypeOptional, ns);
            }
            else
            {
                XmlSchemaChoice choice = new XmlSchemaChoice();
                choice.MaxOccurs = repeats ? decimal.MaxValue : 1;
                choice.MinOccurs = repeats ? 0 : 1;
                for (int i = 0; i < accessors.Length; i++)
                    ExportElementAccessor(choice, accessors[i], false, valueTypeOptional, ns);

                if (choice.Items.Count > 0) group.Items.Add(choice);
            }
        }


        private void ExportElementAccessor(XmlSchemaGroupBase group, ElementAccessor accessor, bool repeats, bool valueTypeOptional, string ns)
        {
            XmlSchemaElement element = new XmlSchemaElement();
            element.MinOccurs = repeats || valueTypeOptional ? 0 : 1;
            element.MaxOccurs = repeats ? decimal.MaxValue : 1;
            element.Name = accessor.Name;
            element.IsNillable = accessor.IsNullable || accessor.Mapping is NullableMapping;
            element.Form = XmlSchemaForm.Unqualified;
            element.SchemaTypeName = ExportTypeMapping(accessor.Mapping, accessor.Namespace);

            group.Items.Add(element);
        }

        private XmlQualifiedName ExportRootMapping(StructMapping mapping)
        {
            if (!_exportedRoot)
            {
                _exportedRoot = true;
                ExportDerivedMappings(mapping);
            }
            return new XmlQualifiedName(Soap.UrType, XmlSchema.Namespace);
        }

        private XmlQualifiedName ExportStructMapping(StructMapping mapping, string ns)
        {
            if (mapping.TypeDesc.IsRoot) return ExportRootMapping(mapping);
            XmlSchemaComplexType type = (XmlSchemaComplexType)_types[mapping];
            if (type == null)
            {
                if (!mapping.IncludeInSchema) throw new InvalidOperationException(SR.Format(SR.XmlSoapCannotIncludeInSchema, mapping.TypeDesc.Name));
                CheckForDuplicateType(mapping.TypeName, mapping.Namespace);
                type = new XmlSchemaComplexType();
                type.Name = mapping.TypeName;
                _types.Add(mapping, type);
                AddSchemaItem(type, mapping.Namespace, ns);
                type.IsAbstract = mapping.TypeDesc.IsAbstract;

                if (mapping.BaseMapping != null && mapping.BaseMapping.IncludeInSchema)
                {
                    XmlSchemaComplexContentExtension extension = new XmlSchemaComplexContentExtension();
                    extension.BaseTypeName = ExportStructMapping(mapping.BaseMapping, mapping.Namespace);
                    XmlSchemaComplexContent model = new XmlSchemaComplexContent();
                    model.Content = extension;
                    type.ContentModel = model;
                }
                ExportTypeMembers(type, mapping.Members, mapping.Namespace);
                ExportDerivedMappings(mapping);
            }
            else
            {
                AddSchemaImport(mapping.Namespace, ns);
            }
            return new XmlQualifiedName(type.Name, mapping.Namespace);
        }

        private XmlQualifiedName ExportMembersMapping(MembersMapping mapping, string ns)
        {
            XmlSchemaComplexType type = (XmlSchemaComplexType)_types[mapping];
            if (type == null)
            {
                CheckForDuplicateType(mapping.TypeName, mapping.Namespace);
                type = new XmlSchemaComplexType();
                type.Name = mapping.TypeName;
                _types.Add(mapping, type);
                AddSchemaItem(type, mapping.Namespace, ns);
                ExportTypeMembers(type, mapping.Members, mapping.Namespace);
            }
            else
            {
                AddSchemaImport(mapping.Namespace, ns);
            }
            return new XmlQualifiedName(type.Name, mapping.Namespace);
        }

        private void ExportTypeMembers(XmlSchemaComplexType type, MemberMapping[] members, string ns)
        {
            XmlSchemaGroupBase seq = new XmlSchemaSequence();
            for (int i = 0; i < members.Length; i++)
            {
                MemberMapping member = members[i];
                if (member.Elements.Length > 0)
                {
                    bool valueTypeOptional = member.CheckSpecified != SpecifiedAccessor.None || member.CheckShouldPersist || !member.TypeDesc.IsValueType;
                    ExportElementAccessors(seq, member.Elements, false, valueTypeOptional, ns);
                }
            }
            if (seq.Items.Count > 0)
            {
                if (type.ContentModel != null)
                {
                    if (type.ContentModel.Content is XmlSchemaComplexContentExtension)
                        ((XmlSchemaComplexContentExtension)type.ContentModel.Content).Particle = seq;
                    else if (type.ContentModel.Content is XmlSchemaComplexContentRestriction)
                        ((XmlSchemaComplexContentRestriction)type.ContentModel.Content).Particle = seq;
                    else
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidContent, type.ContentModel.Content.GetType().Name));
                }
                else
                {
                    type.Particle = seq;
                }
            }
        }

        private void ExportDerivedMappings(StructMapping mapping)
        {
            for (StructMapping derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                if (derived.IncludeInSchema) ExportStructMapping(derived, mapping.TypeDesc.IsRoot ? null : mapping.Namespace);
            }
        }

        private XmlQualifiedName ExportEnumMapping(EnumMapping mapping, string ns)
        {
            XmlSchemaSimpleType dataType = (XmlSchemaSimpleType)_types[mapping];
            if (dataType == null)
            {
                CheckForDuplicateType(mapping.TypeName, mapping.Namespace);
                dataType = new XmlSchemaSimpleType();
                dataType.Name = mapping.TypeName;
                _types.Add(mapping, dataType);
                AddSchemaItem(dataType, mapping.Namespace, ns);

                XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction();
                restriction.BaseTypeName = new XmlQualifiedName("string", XmlSchema.Namespace);

                for (int i = 0; i < mapping.Constants.Length; i++)
                {
                    ConstantMapping constant = mapping.Constants[i];
                    XmlSchemaEnumerationFacet enumeration = new XmlSchemaEnumerationFacet();
                    enumeration.Value = constant.XmlName;
                    restriction.Facets.Add(enumeration);
                }
                if (!mapping.IsFlags)
                {
                    dataType.Content = restriction;
                }
                else
                {
                    XmlSchemaSimpleTypeList list = new XmlSchemaSimpleTypeList();
                    XmlSchemaSimpleType enumType = new XmlSchemaSimpleType();
                    enumType.Content = restriction;
                    list.ItemType = enumType;
                    dataType.Content = list;
                }
            }
            else
            {
                AddSchemaImport(mapping.Namespace, ns);
            }
            return new XmlQualifiedName(mapping.TypeName, mapping.Namespace);
        }
    }
}
