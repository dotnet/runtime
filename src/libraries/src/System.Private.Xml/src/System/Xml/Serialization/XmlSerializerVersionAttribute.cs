// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class XmlSerializerVersionAttribute : System.Attribute
    {
        private string _mvid;
        private string _serializerVersion;
        private string _ns;
        private Type _type;

        public XmlSerializerVersionAttribute()
        {
        }

        public XmlSerializerVersionAttribute(Type type)
        {
            _type = type;
        }

        public string ParentAssemblyId
        {
            get { return _mvid; }
            set { _mvid = value; }
        }

        public string Version
        {
            get { return _serializerVersion; }
            set { _serializerVersion = value; }
        }


        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }
    }
}
