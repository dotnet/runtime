// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public sealed class XmlSchemaProviderAttribute : System.Attribute
    {
        private readonly string? _methodName;
        private bool _any;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaProviderAttribute(string? methodName)
        {
            _methodName = methodName;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string? MethodName
        {
            get { return _methodName; }
        }

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
