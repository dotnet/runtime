// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Schema;

namespace System.Xml.Serialization
{
    /// <include file='doc\IXmlSerializable.uex' path='docs/doc[@for="IXmlSerializable"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public interface IXmlSerializable
    {
        /// <include file='doc\IXmlSerializable.uex' path='docs/doc[@for="IXmlSerializable.GetSchema"]/*' />
        XmlSchema GetSchema();
        /// <include file='doc\IXmlSerializable.uex' path='docs/doc[@for="IXmlSerializable.ReadXml"]/*' />
        void ReadXml(XmlReader reader);
        /// <include file='doc\IXmlSerializable.uex' path='docs/doc[@for="IXmlSerializable.WriteXml"]/*' />
        void WriteXml(XmlWriter writer);
    }
}
