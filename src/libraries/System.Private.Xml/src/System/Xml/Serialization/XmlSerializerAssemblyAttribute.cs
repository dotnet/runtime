// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;

    /// <include file='doc\XmlSerializerAssemblyAttribute.uex' path='docs/doc[@for="XmlSerializerAssemblyAttribute"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class XmlSerializerAssemblyAttribute : System.Attribute
    {
        private string _assemblyName;
        private string _codeBase;

        /// <include file='doc\XmlSerializerAssemblyAttribute.uex' path='docs/doc[@for="XmlSerializerAssemblyAttribute.XmlSerializerAssemblyAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerAssemblyAttribute() : this(null, null) { }

        /// <include file='doc\XmlSerializerAssemblyAttribute.uex' path='docs/doc[@for="XmlSerializerAssemblyAttribute.XmlSerializerAssemblyAttribute1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerAssemblyAttribute(string assemblyName) : this(assemblyName, null) { }

        /// <include file='doc\XmlSerializerAssemblyAttribute.uex' path='docs/doc[@for="XmlSerializerAssemblyAttribute.XmlSerializerAssemblyAttribute2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializerAssemblyAttribute(string assemblyName, string codeBase)
        {
            _assemblyName = assemblyName;
            _codeBase = codeBase;
        }

        /// <include file='doc\XmlSerializerAssemblyAttribute.uex' path='docs/doc[@for="XmlSerializerAssemblyAttribute.Location"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string CodeBase
        {
            get { return _codeBase; }
            set { _codeBase = value; }
        }

        /// <include file='doc\XmlSerializerAssemblyAttribute.uex' path='docs/doc[@for="XmlSerializerAssemblyAttribute.AssemblyName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string AssemblyName
        {
            get { return _assemblyName; }
            set { _assemblyName = value; }
        }
    }
}
