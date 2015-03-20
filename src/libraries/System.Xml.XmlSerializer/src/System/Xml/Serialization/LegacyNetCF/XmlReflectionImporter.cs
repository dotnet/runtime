// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System.Reflection;
using System;
using System.Xml.Schema;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Linq;


namespace System.Xml.Serialization.LegacyNetCF
{
    //<internalonly/>
    public class XmlReflectionImporter
    {
        private XmlSerializationReflector _serializationReflector;
        private XmlAttributeOverrides _attributeOverrides;
        private string _defaultNs;


        public XmlReflectionImporter() : this(null, null)
        {
        }

        public XmlReflectionImporter(string defaultNamespace) : this(null, defaultNamespace)
        {
        }

        public XmlReflectionImporter(XmlAttributeOverrides attributeOverrides) : this(attributeOverrides, null)
        {
        }

        public XmlReflectionImporter(XmlAttributeOverrides attributeOverrides, string defaultNamespace)
        {
            if (defaultNamespace == null)
                defaultNamespace = String.Empty;
            if (attributeOverrides == null)
                attributeOverrides = new XmlAttributeOverrides();
            _attributeOverrides = attributeOverrides;
            _defaultNs = defaultNamespace;
            _defaultNs = defaultNamespace;
            _attributeOverrides = attributeOverrides;
            _serializationReflector = new XmlSerializationReflector(_attributeOverrides, _defaultNs);
        }

        public void IncludeTypes(MemberInfo memberInfo)
        {
            object[] attrs = memberInfo.GetCustomAttributes(typeof(XmlIncludeAttribute), false).ToArray();
            for (int i = 0; i < attrs.Length; i++)
            {
                Type type = ((XmlIncludeAttribute)attrs[i]).Type;
                IncludeType(type);
            }
        }

        public void IncludeType(Type type)
        {
            System.Diagnostics.Debug.Assert(null != _serializationReflector, "The XmlSerializationReflector has not been initialized.");

            _serializationReflector.FindType(type, false /*Encoded*/, _defaultNs);
            _serializationReflector.ReflectIncludedTypes();
        }

        public XmlTypeMapping ImportTypeMapping(Type type)
        {
            return ImportTypeMapping(type, null, null);
        }

        public XmlTypeMapping ImportTypeMapping(Type type, string defaultNamespace)
        {
            return ImportTypeMapping(type, null, defaultNamespace);
        }

        public XmlTypeMapping ImportTypeMapping(Type type, XmlRootAttribute root)
        {
            return ImportTypeMapping(type, root, null);
        }

        public XmlTypeMapping ImportTypeMapping(Type type, XmlRootAttribute root, string defaultNamespace)
        {
            System.Diagnostics.Debug.Assert(null != _serializationReflector, "The XmlSerializationReflector has not been initialized.");

            if (root != null)
            {
                if (_serializationReflector.XmlAttributeOverrides == null)
                    _serializationReflector.XmlAttributeOverrides = new XmlAttributeOverrides();
                if (_serializationReflector.XmlAttributeOverrides[type] != null)
                {
                    _serializationReflector.XmlAttributeOverrides[type].XmlRoot = root;
                }
                else
                {
                    XmlAttributes xmlAttrs = new XmlAttributes();
                    xmlAttrs.XmlRoot = root;
                    _serializationReflector.XmlAttributeOverrides.Add(type, xmlAttrs);
                }
            }

            LogicalType logicalType = _serializationReflector.FindType(type, false/*Encoded*/, defaultNamespace ?? _defaultNs);
            _serializationReflector.ReflectIncludedTypes();
            XmlTypeMapping typeMapping = new XmlTypeMapping(logicalType, _serializationReflector);
            typeMapping.IsSoap = false;
            return typeMapping;
        }
        // The bulk of the rest of the file is if'd out, and the meat of it is replaced by the content of the XmlSerializationReflector class that is specific to NetCF
    }


    [Flags]
    public enum XmlMappingAccess
    {
        None = 0x00,
        Read = 0x01,
        Write = 0x02,
    }
}
