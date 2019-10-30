// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

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
