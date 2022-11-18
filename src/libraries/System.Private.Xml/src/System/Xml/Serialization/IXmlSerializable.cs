// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Xml;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public interface IXmlSerializable
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        XmlSchema? GetSchema();
        void ReadXml(XmlReader reader);
        void WriteXml(XmlWriter writer);
    }
}
