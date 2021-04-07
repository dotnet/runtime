// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    using System.Diagnostics.CodeAnalysis;

    public class XmlSerializerFactory
    {
        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides? overrides, Type[]? extraTypes, XmlRootAttribute? root, string? defaultNamespace)
        {
            return CreateSerializer(type, overrides, extraTypes, root, defaultNamespace, null);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlSerializer CreateSerializer(Type type, XmlRootAttribute? root)
        {
            return CreateSerializer(type, null, Type.EmptyTypes, root, null, null);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlSerializer CreateSerializer(Type type, Type[]? extraTypes)
        {
            return CreateSerializer(type, null, extraTypes, null, null, null);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides? overrides)
        {
            return CreateSerializer(type, overrides, Type.EmptyTypes, null, null, null);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlSerializer CreateSerializer(XmlTypeMapping xmlTypeMapping)
        {
            return new XmlSerializer(xmlTypeMapping);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlSerializer CreateSerializer(Type type)
        {
            return CreateSerializer(type, (string?)null);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlSerializer CreateSerializer(Type type, string? defaultNamespace)
        {
            return new XmlSerializer(type, defaultNamespace);
        }

        [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
        public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides? overrides, Type[]? extraTypes, XmlRootAttribute? root, string? defaultNamespace, string? location)
        {
            return new XmlSerializer(type, overrides, extraTypes, root, defaultNamespace, location);
        }
    }
}
