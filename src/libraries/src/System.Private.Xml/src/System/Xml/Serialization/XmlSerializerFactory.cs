// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System.Reflection;
    using System.Collections;
    using System.IO;
    using System.Xml.Schema;
    using System;
    using System.Text;
    using System.Threading;
    using System.Globalization;
    using System.Xml.Serialization.Configuration;
    using System.Diagnostics;
    using System.Xml.Serialization;

    public class XmlSerializerFactory
    {
        private static TempAssemblyCache s_cache = new TempAssemblyCache();

        public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace)
        {
            return CreateSerializer(type, overrides, extraTypes, root, defaultNamespace, null);
        }

        public XmlSerializer CreateSerializer(Type type, XmlRootAttribute root)
        {
            return CreateSerializer(type, null, Array.Empty<Type>(), root, null, null);
        }

        public XmlSerializer CreateSerializer(Type type, Type[] extraTypes)
        {
            return CreateSerializer(type, null, extraTypes, null, null, null);
        }

        public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides overrides)
        {
            return CreateSerializer(type, overrides, Array.Empty<Type>(), null, null, null);
        }

        public XmlSerializer CreateSerializer(XmlTypeMapping xmlTypeMapping)
        {
            return new XmlSerializer(xmlTypeMapping);
        }

        public XmlSerializer CreateSerializer(Type type)
        {
            return CreateSerializer(type, (string)null);
        }

        public XmlSerializer CreateSerializer(Type type, string defaultNamespace)
        {
            return new XmlSerializer(type, defaultNamespace);
        }

        public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace, string location)
        {
            return new XmlSerializer(type, overrides, extraTypes, root, defaultNamespace, location);
        }
    }
}
