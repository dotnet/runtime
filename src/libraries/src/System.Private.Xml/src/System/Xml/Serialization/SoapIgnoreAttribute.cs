// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;

    /// <include file='doc\SoapIgnoreAttribute.uex' path='docs/doc[@for="SoapIgnoreAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SoapIgnoreAttribute : System.Attribute
    {
        /// <include file='doc\SoapIgnoreAttribute.uex' path='docs/doc[@for="SoapIgnoreAttribute.SoapIgnoreAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public SoapIgnoreAttribute()
        {
        }
    }
}
