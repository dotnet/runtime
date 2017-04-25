using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;

namespace SerializationTestTypes
{
    [Serializable]
    public class PrimitiveTypeResolver : DataContractResolver
    {
        private readonly static string s_defaultNS = "http://www.default.com";
        private readonly static Dictionary<string, Type> s_types = new Dictionary<string, Type>();

        public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver KTResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            string resolvedTypeName = string.Empty;
            string resolvedNamespace = string.Empty;
            resolvedNamespace = s_defaultNS;
            resolvedTypeName = dcType.Name + "_foo";
            s_types[resolvedTypeName] = dcType;
            XmlDictionary dic = new XmlDictionary();
            typeName = dic.Add(resolvedTypeName);
            typeNamespace = dic.Add(resolvedNamespace);
            return true;
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver KTResolver)
        {
            return s_types[typeName];
        }
    }

    public class EmptyNamespaceResolver : DataContractResolver
    {
        public override bool TryResolveType(Type dataContractType, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            XmlDictionary dic = new XmlDictionary();
            if (dataContractType == typeof(EmptyNsContainer))
            {
                typeName = dic.Add("EmptyNsContainer");
                typeNamespace = dic.Add("MyNamespace");
                return true;
            }
            else if (dataContractType == typeof(UknownEmptyNSAddress))
            {
                typeName = dic.Add("AddressFoo");
                typeNamespace = dic.Add("");
                return true;
            }
            else
            {
                return knownTypeResolver.TryResolveType(dataContractType, declaredType, null, out typeName, out typeNamespace);
            }
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            if (typeNamespace == "MyNamespace")
            {
                switch (typeName)
                {
                    case "EmptyNsContainer":
                        return typeof(EmptyNsContainer);
                }
            }
            else if (typeName.Equals("AddressFoo"))
            {
                return typeof(UknownEmptyNSAddress);
            }

            return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
        }
    }

    [Serializable]
    public class ProxyDataContractResolver : DataContractResolver
    {
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
        }

        public override bool TryResolveType(Type dataContractType, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            Type actualDataContractType = dataContractType.Name.EndsWith("Proxy") ? dataContractType.BaseType : dataContractType;
            return knownTypeResolver.TryResolveType(actualDataContractType, declaredType, null, out typeName, out typeNamespace);
        }
    }

    [Serializable]
    public class POCOTypeResolver : DataContractResolver
    {
        public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver KTResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            string resolvedTypeName = string.Empty;
            string resolvedNamespace = string.Empty;
            resolvedNamespace = "http://www.default.com";
            switch (dcType.Name)
            {
                case "POCOObjectContainer":
                    {
                        resolvedTypeName = "POCO";
                    }
                    break;
                case "Person":
                    {
                        throw new InvalidOperationException("Member with attribute 'IgnoreDataMember' should be ignored during ser");
                    }
                default:
                    {
                        return KTResolver.TryResolveType(dcType, declaredType, null, out typeName, out typeNamespace);
                    }
            }
            //for types resolved by the DCR
            XmlDictionary dic = new XmlDictionary();
            typeName = dic.Add(resolvedTypeName);
            typeNamespace = dic.Add(resolvedNamespace);
            return true;
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver KTResolver)
        {
            if (typeNamespace.Equals("http://www.default.com"))
            {
                if (typeName.Equals("POCO"))
                {
                    return typeof(POCOObjectContainer);
                }
            }
            if (typeName.Equals("Person"))
            {
                throw new InvalidOperationException("Member with attribute 'IgnoreDataMember' should be ignored during deser");
            }
            Type result = KTResolver.ResolveName(typeName, typeNamespace, declaredType, null);
            return result;
        }
    }

    public class WireFormatVerificationResolver : DataContractResolver
    {
        private Type _type;

        public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver KTResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            if (!KTResolver.TryResolveType(dcType, declaredType, null, out typeName, out typeNamespace))
            {
                _type = dcType;
                typeName = new XmlDictionary().Add(dcType.FullName + "***");
                typeNamespace = new XmlDictionary().Add(dcType.Assembly.FullName + "***");
            }
            return true;
        }
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver KTResolver)
        {
            Type t = null;
            if (typeName.Contains("***"))
            {
                t = _type;
            }
            else
            {
                t = KTResolver.ResolveName(typeName, typeNamespace, declaredType, null);
                if (t == null)
                {
                    t = Type.GetType(typeName + "," + typeNamespace);
                }
            }
            return t;
        }
    }
}
