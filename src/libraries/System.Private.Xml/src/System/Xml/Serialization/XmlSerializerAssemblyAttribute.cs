// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class XmlSerializerAssemblyAttribute : System.Attribute
    {
        private string _assemblyName;
        private string _codeBase;

        public XmlSerializerAssemblyAttribute() : this(null, null) { }

        public XmlSerializerAssemblyAttribute(string assemblyName) : this(assemblyName, null) { }

        public XmlSerializerAssemblyAttribute(string assemblyName, string codeBase)
        {
            _assemblyName = assemblyName;
            _codeBase = codeBase;
        }

        public string CodeBase
        {
            get { return _codeBase; }
            set { _codeBase = value; }
        }

        public string AssemblyName
        {
            get { return _assemblyName; }
            set { _assemblyName = value; }
        }
    }
}
