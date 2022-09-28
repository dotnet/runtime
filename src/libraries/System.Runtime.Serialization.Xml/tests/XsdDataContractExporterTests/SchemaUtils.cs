// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Xunit;

namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
{
    internal class SchemaUtils
    {
        internal static string SerializationNamespace = "http://schemas.microsoft.com/2003/10/Serialization/";
        static XmlWriterSettings writerSettings = new XmlWriterSettings() { Indent = true };

        public static string OrderedContains(string expected, ref string actual)
        {
            Assert.Contains(expected, actual);
            actual = actual.Substring(actual.IndexOf(expected));
            return actual;
        }

        public static string DumpSchema(XmlSchemaSet schemas)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            foreach (XmlSchema schema in schemas.Schemas())
            {
                if (schema.TargetNamespace != SerializationNamespace)
                {
                    schema.Write(sw);
                }
                sw.WriteLine();
            }
            sw.Flush();
            return sb.ToString();
        }

        internal static XmlSchema GetSchema(XmlSchemaSet schemaSet, string targetNs)
        {
            XmlSchema schema = null;
            foreach (XmlSchema ctSchema in schemaSet.Schemas())
            {
                if (ctSchema.TargetNamespace == targetNs)
                {
                    schema = ctSchema;
                    break;
                }
            }
            return schema;
        }
    }
}
