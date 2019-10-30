// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    [Flags]
    public enum XmlSchemaDerivationMethod
    {
        [XmlEnum("")]
        Empty = 0,

        [XmlEnum("substitution")]
        Substitution = 0x0001,

        [XmlEnum("extension")]
        Extension = 0x0002,

        [XmlEnum("restriction")]
        Restriction = 0x0004,

        [XmlEnum("list")]
        List = 0x0008,

        [XmlEnum("union")]
        Union = 0x0010,

        [XmlEnum("#all")]
        All = 0x00FF,

        [XmlIgnore]
        None = 0x0100
    }
}
