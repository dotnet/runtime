// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System.Reflection;
using System;
using System.Text;


namespace System.Xml.Serialization.LegacyNetCF
{
    public class XmlTypeMapping : XmlMapping
    {
        internal XmlTypeMapping(LogicalType logicalType, XmlSerializationReflector reflector) : base(logicalType, reflector)
        {
        }

        //internal XmlTypeMapping(LogicalType logicalType, XmlSerializationReflector reflector, bool isSoap) : base(logicalType, reflector, isSoap) {            
        //}

        public string TypeName
        {
            get
            {
                return GetTypeName(this.logicalType.Type);
            }
        }

        public string TypeFullName
        {
            get { return this.logicalType.Type.ToString(); }
        }

        public string XsdTypeName
        {
            get { return this.logicalType.TypeAccessor.Name; }
        }

        public string XsdTypeNamespace
        {
            get { return this.logicalType.TypeAccessor.Namespace; }
        }

        // helper method which does special handling for arrays and generics        
        private string GetTypeName(Type t)
        {
            if (t.IsArray)
            {
                return "ArrayOf" + GetTypeName(t.GetElementType());
            }
            else if (t.GetTypeInfo().IsGenericType)
            {
                StringBuilder typeName = new StringBuilder();
                StringBuilder ns = new StringBuilder();
                string name = t.Name;
                int arity = name.IndexOf("`", StringComparison.Ordinal);
                if (arity >= 0)
                {
                    name = name.Substring(0, arity);
                }
                typeName.Append(name);
                typeName.Append("Of");
                Type[] arguments = t.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++)
                {
                    typeName.Append(GetTypeName(arguments[i]));
                    ns.Append(arguments[i].Namespace);
                }
                return typeName.ToString();
            }
            return t.Name;
        }
    }
}
