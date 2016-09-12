// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Xunit;

public static partial class XmlSerializerTests
{
    [Fact]
    // XmlTypeMapping is not included in System.Xml.XmlSerializer 4.0.0.0 facade in GAC
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
    public static void Xml_FromMappings()
    {
        var types = new[] { typeof(Guid), typeof(List<string>) };
        XmlReflectionImporter importer = new XmlReflectionImporter();
        XmlTypeMapping[] mappings = new XmlTypeMapping[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            mappings[i] = importer.ImportTypeMapping(types[i]);
        }
        var serializers = XmlSerializer.FromMappings(mappings, typeof(object));
        Xml_GuidAsRoot(serializers[0]);
        Xml_ListGenericRoot(serializers[1]);
    }

    [Fact]
    // XmlTypeMapping is not included in System.Xml.XmlSerializer 4.0.0.0 facade in GAC
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
    public static void Xml_ConstructorWithTypeMapping()
    {
        XmlTypeMapping mapping = null;
        XmlSerializer serializer = null;
        Assert.Throws<ArgumentNullException>(() => { new XmlSerializer(mapping); });

        mapping = new XmlReflectionImporter(null, null).ImportTypeMapping(typeof(List<string>));
        serializer = new XmlSerializer(mapping);
        Xml_ListGenericRoot(serializer);
    }

}
