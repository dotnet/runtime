// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.Serialization
{
    internal static class SchemaHelper
    {

        internal static bool NamespacesEqual(string ns1, string ns2)
        {
            if (ns1 == null || ns1.Length == 0)
                return (ns2 == null || ns2.Length == 0);
            else
                return ns1 == ns2;
        }

        internal static XmlSchemaType GetSchemaType(XmlSchemaSet schemas, XmlQualifiedName typeQName, out XmlSchema outSchema)
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
                        XmlSchemaType schemaType = schemaObj as XmlSchemaType;
                        if (schemaType != null && schemaType.Name == typeQName.Name)
                        {
                            return schemaType;
                        }
                    }
                }
            }
            return null;
        }

        internal static XmlSchemaElement GetSchemaElement(XmlSchemaSet schemas, XmlQualifiedName elementQName, out XmlSchema outSchema)
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
                        XmlSchemaElement schemaElement = schemaObj as XmlSchemaElement;
                        if (schemaElement != null && schemaElement.Name == elementQName.Name)
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
            if (ns == null) { ns = string.Empty; }

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

        static XmlSchema CreateSchema(string ns, XmlSchemaSet schemas)
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
    }
}
