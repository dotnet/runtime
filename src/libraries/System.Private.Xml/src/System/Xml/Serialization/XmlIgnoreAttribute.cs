// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlIgnoreAttribute.uex' path='docs/doc[@for="XmlIgnoreAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class XmlIgnoreAttribute : System.Attribute
    {
        /// <include file='doc\XmlIgnoreAttribute.uex' path='docs/doc[@for="XmlIgnoreAttribute.XmlIgnoreAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlIgnoreAttribute()
        {
        }
    }
}
