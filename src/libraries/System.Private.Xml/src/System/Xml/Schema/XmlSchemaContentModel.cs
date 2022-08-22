// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Serialization;

namespace System.Xml.Schema
{
    public abstract class XmlSchemaContentModel : XmlSchemaAnnotated
    {
        [XmlIgnore]
        public abstract XmlSchemaContent? Content { get; set; }
    }
}
