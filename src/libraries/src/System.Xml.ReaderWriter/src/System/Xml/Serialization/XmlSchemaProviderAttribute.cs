// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    /// <include file='doc\XmlSchemaProviderAttribute.uex' path='docs/doc[@for="XmlSchemaProviderAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public sealed class XmlSchemaProviderAttribute : System.Attribute
    {
        private string _methodName;
        private bool _any;

        /// <include file='doc\XmlSchemaProviderAttribute.uex' path='docs/doc[@for="XmlSchemaProviderAttribute.XmlSchemaProviderAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaProviderAttribute(string methodName)
        {
            _methodName = methodName;
        }

        /// <include file='doc\XmlSchemaProviderAttribute.uex' path='docs/doc[@for="XmlSchemaProviderAttribute.MethodName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MethodName
        {
            get { return _methodName; }
        }

        /// <include file='doc\XmlSchemaProviderAttribute.uex' path='docs/doc[@for="XmlSchemaProviderAttribute.IsAny"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsAny
        {
            get { return _any; }
            set { _any = value; }
        }
    }
}
