// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema
{
    using System.Xml.Serialization;

    public enum XmlSchemaContentProcessing
    {
        [XmlIgnore]
        None,

        [XmlEnum("skip")]
        Skip,

        [XmlEnum("lax")]
        Lax,

        [XmlEnum("strict")]
        Strict
    }
}
