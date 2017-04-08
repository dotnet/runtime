// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System.Diagnostics;

    internal static class DiagnosticsSwitches
    {
        private static volatile BooleanSwitch s_xmlSchemaContentModel;
        private static volatile TraceSwitch s_xmlSchema;
        private static volatile BooleanSwitch s_keepTempFiles;
        private static volatile TraceSwitch s_xmlSerialization;
        private static volatile TraceSwitch s_xslTypeInference;
        private static volatile BooleanSwitch s_nonRecursiveTypeLoading;

        public static BooleanSwitch XmlSchemaContentModel
        {
            get
            {
                if (s_xmlSchemaContentModel == null)
                {
                    s_xmlSchemaContentModel = new BooleanSwitch("XmlSchemaContentModel", "Enable tracing for the XmlSchema content model.");
                }
                return s_xmlSchemaContentModel;
            }
        }

        public static TraceSwitch XmlSchema
        {
            get
            {
                if (s_xmlSchema == null)
                {
                    s_xmlSchema = new TraceSwitch("XmlSchema", "Enable tracing for the XmlSchema class.");
                }
                return s_xmlSchema;
            }
        }

        public static BooleanSwitch KeepTempFiles
        {
            get
            {
                if (s_keepTempFiles == null)
                {
                    s_keepTempFiles = new BooleanSwitch("XmlSerialization.Compilation", "Keep XmlSerialization generated (temp) files.");
                }
                return s_keepTempFiles;
            }
        }

        public static TraceSwitch XmlSerialization
        {
            get
            {
                if (s_xmlSerialization == null)
                {
                    s_xmlSerialization = new TraceSwitch("XmlSerialization", "Enable tracing for the System.Xml.Serialization component.");
                }
                return s_xmlSerialization;
            }
        }

        public static TraceSwitch XslTypeInference
        {
            get
            {
                if (s_xslTypeInference == null)
                {
                    s_xslTypeInference = new TraceSwitch("XslTypeInference", "Enable tracing for the XSLT type inference algorithm.");
                }
                return s_xslTypeInference;
            }
        }
        public static BooleanSwitch NonRecursiveTypeLoading
        {
            get
            {
                if (s_nonRecursiveTypeLoading == null)
                {
                    s_nonRecursiveTypeLoading = new BooleanSwitch("XmlSerialization.NonRecursiveTypeLoading", "Turn on non-recursive algorithm generating XmlMappings for CLR types.");
                }
                return s_nonRecursiveTypeLoading;
            }
        }
    }
}
