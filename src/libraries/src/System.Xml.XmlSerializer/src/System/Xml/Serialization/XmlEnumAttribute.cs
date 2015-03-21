// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlEnumAttribute.uex' path='docs/doc[@for="XmlEnumAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field)]
    public class XmlEnumAttribute : System.Attribute
    {
        private string _name;

        /// <include file='doc\XmlEnumAttribute.uex' path='docs/doc[@for="XmlEnumAttribute.XmlEnumAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlEnumAttribute()
        {
        }

        /// <include file='doc\XmlEnumAttribute.uex' path='docs/doc[@for="XmlEnumAttribute.XmlEnumAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlEnumAttribute(string name)
        {
            _name = name;
        }

        /// <include file='doc\XmlEnumAttribute.uex' path='docs/doc[@for="XmlEnumAttribute.Name"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}
