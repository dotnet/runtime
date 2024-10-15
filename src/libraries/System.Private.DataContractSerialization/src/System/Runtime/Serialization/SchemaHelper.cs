// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using SchemaObjectDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.SchemaObjectInfo>;

namespace System.Runtime.Serialization
{
    internal sealed class SchemaObjectInfo
    {
        internal XmlSchemaType? _type;
        internal XmlSchemaElement? _element;
        internal XmlSchema? _schema;
        internal List<XmlSchemaType>? _knownTypes;

        internal SchemaObjectInfo(XmlSchemaType? type, XmlSchemaElement? element, XmlSchema? schema, List<XmlSchemaType>? knownTypes)
        {
            _type = type;
            _element = element;
            _schema = schema;
            _knownTypes = knownTypes;
        }
    }

    internal sealed class SchemaDefinedType
    {
        internal XmlQualifiedName _xmlName;

        public SchemaDefinedType(XmlQualifiedName xmlName)
        {
            _xmlName = xmlName;
        }
    }

    internal enum SchemaDefinedEnum { SchemaDefinedEnumValue };

    internal static class SchemaHelper
    {
        internal static bool NamespacesEqual(string? ns1, string? ns2)
        {
            if (string.IsNullOrEmpty(ns1))
                return string.IsNullOrEmpty(ns2);
            else
                return ns1 == ns2;
        }

        internal static XmlSchemaType? GetSchemaType(SchemaObjectDictionary schemaInfo, XmlQualifiedName typeName)
        {
            SchemaObjectInfo? schemaObjectInfo;
            if (schemaInfo.TryGetValue(typeName, out schemaObjectInfo))
            {
                return schemaObjectInfo._type;
            }
            return null;
        }

        internal static XmlSchemaType? GetSchemaType(XmlSchemaSet schemas, XmlQualifiedName typeQName, out XmlSchema? outSchema)
        {
            outSchema = null;
            ICollection currentSchemas = schemas.Schemas();
            string ns = typeQName.Namespace;
            foreach (XmlSchema schema in currentSchemas)
            {
                if (NamespacesEqual(ns, schema.TargetNamespace))
                {
                    outSchema = schema;
                    foreach (XmlSchemaObject schemaObj in schema.Items)
                    {
                        if (schemaObj is XmlSchemaType schemaType && schemaType.Name == typeQName.Name)
                        {
                            return schemaType;
                        }
                    }
                }
            }
            return null;
        }

        internal static XmlSchemaElement? GetSchemaElement(SchemaObjectDictionary schemaInfo, XmlQualifiedName elementName)
        {
            SchemaObjectInfo? schemaObjectInfo;
            if (schemaInfo.TryGetValue(elementName, out schemaObjectInfo))
            {
                return schemaObjectInfo._element;
            }
            return null;
        }

        internal static XmlSchemaElement? GetSchemaElement(XmlSchemaSet schemas, XmlQualifiedName elementQName, out XmlSchema? outSchema)
        {
            outSchema = null;
            ICollection currentSchemas = schemas.Schemas();
            string ns = elementQName.Namespace;
            foreach (XmlSchema schema in currentSchemas)
            {
                if (NamespacesEqual(ns, schema.TargetNamespace))
                {
                    outSchema = schema;
                    foreach (XmlSchemaObject schemaObj in schema.Items)
                    {
                        if (schemaObj is XmlSchemaElement schemaElement && schemaElement.Name == elementQName.Name)
                        {
                            return schemaElement;
                        }
                    }
                }
            }
            return null;
        }

        internal static XmlSchema GetSchema(string ns, XmlSchemaSet schemas)
        {
            ns ??= string.Empty;

            ICollection currentSchemas = schemas.Schemas();
            foreach (XmlSchema schema in currentSchemas)
            {
                if ((schema.TargetNamespace == null && ns.Length == 0) || ns.Equals(schema.TargetNamespace))
                {
                    return schema;
                }
            }
            return CreateSchema(ns, schemas);
        }

        private static XmlSchema CreateSchema(string ns, XmlSchemaSet schemas)
        {
            XmlSchema schema = new XmlSchema();

            schema.ElementFormDefault = XmlSchemaForm.Qualified;
            if (ns.Length > 0)
            {
                schema.TargetNamespace = ns;
                schema.Namespaces.Add(Globals.TnsPrefix, ns);
            }


            schemas.Add(schema);
            return schema;
        }

        internal static void AddElementForm(XmlSchemaElement element, XmlSchema schema)
        {
            if (schema.ElementFormDefault != XmlSchemaForm.Qualified)
            {
                element.Form = XmlSchemaForm.Qualified;
            }
        }

        internal static void AddSchemaImport(string ns, XmlSchema schema)
        {
            if (SchemaHelper.NamespacesEqual(ns, schema.TargetNamespace) || SchemaHelper.NamespacesEqual(ns, Globals.SchemaNamespace) || SchemaHelper.NamespacesEqual(ns, Globals.SchemaInstanceNamespace))
                return;

            foreach (object item in schema.Includes)
            {
                if (item is XmlSchemaImport)
                {
                    if (SchemaHelper.NamespacesEqual(ns, ((XmlSchemaImport)item).Namespace))
                        return;
                }
            }

            XmlSchemaImport import = new XmlSchemaImport();
            if (ns != null && ns.Length > 0)
                import.Namespace = ns;
            schema.Includes.Add(import);
        }

        internal static XmlSchema? GetSchemaWithType(SchemaObjectDictionary schemaInfo, XmlSchemaSet schemas, XmlQualifiedName typeName)
        {
            SchemaObjectInfo? schemaObjectInfo;
            if (schemaInfo.TryGetValue(typeName, out schemaObjectInfo))
            {
                if (schemaObjectInfo._schema != null)
                    return schemaObjectInfo._schema;
            }
            ICollection currentSchemas = schemas.Schemas();
            string ns = typeName.Namespace;
            foreach (XmlSchema schema in currentSchemas)
            {
                if (NamespacesEqual(ns, schema.TargetNamespace))
                {
                    return schema;
                }
            }
            return null;
        }

        internal static XmlSchema? GetSchemaWithGlobalElementDeclaration(XmlSchemaElement element, XmlSchemaSet schemas)
        {
            ICollection currentSchemas = schemas.Schemas();
            foreach (XmlSchema schema in currentSchemas)
            {
                foreach (XmlSchemaObject schemaObject in schema.Items)
                {
                    if (schemaObject is XmlSchemaElement schemaElement && schemaElement == element)
                    {
                        return schema;
                    }
                }
            }
            return null;
        }

        internal static XmlQualifiedName? GetGlobalElementDeclaration(XmlSchemaSet schemas, XmlQualifiedName typeQName, out bool isNullable)
        {
            ICollection currentSchemas = schemas.Schemas();
            isNullable = false;
            foreach (XmlSchema schema in currentSchemas)
            {
                foreach (XmlSchemaObject schemaObject in schema.Items)
                {
                    if (schemaObject is XmlSchemaElement schemaElement && schemaElement.SchemaTypeName.Equals(typeQName))
                    {
                        isNullable = schemaElement.IsNillable;
                        return new XmlQualifiedName(schemaElement.Name, schema.TargetNamespace);
                    }
                }
            }
            return null;
        }
    }
}
