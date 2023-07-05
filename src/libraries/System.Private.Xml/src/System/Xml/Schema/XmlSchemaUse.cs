// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Serialization;

namespace System.Xml.Schema
{
    //nzeng: if change the enum, have to change xsdbuilder as well.
    public enum XmlSchemaUse
    {
        [XmlIgnore]
        None,

        [XmlEnum("optional")]
        Optional,

        [XmlEnum("prohibited")]
        Prohibited,

        [XmlEnum("required")]
        Required,
    }
}
