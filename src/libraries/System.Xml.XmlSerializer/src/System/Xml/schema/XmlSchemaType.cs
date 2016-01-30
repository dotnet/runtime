// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Xml.Serialization;

namespace System.Xml.Schema
{
    internal class XmlSchemaType
    {
        private string _name;

        /// <include file='doc\XmlSchemaType.uex' path='docs/doc[@for="XmlSchemaType.Name"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [XmlAttribute("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}

