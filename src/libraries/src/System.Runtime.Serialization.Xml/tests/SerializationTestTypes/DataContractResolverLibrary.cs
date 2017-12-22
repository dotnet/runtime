// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        
    public class UserTypeToPrimitiveTypeResolver : DataContractResolver
    {
        public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver KTResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            string resolvedTypeName = string.Empty;
            string resolvedNamespace = "http://www.default.com";
            switch (dcType.Name)
            {
                case "UnknownEmployee":
                    resolvedTypeName = "int";                                          
                    resolvedNamespace = "http://www.w3.org/2001/XMLSchema";
                    break;
                case "UserTypeContainer":                    
                    resolvedTypeName = "UserType";                    
                    break;
                default:                    
                    return KTResolver.TryResolveType(dcType, declaredType, null, out typeName, out typeNamespace);
            }
            
            XmlDictionary dic = new XmlDictionary();
            typeName = dic.Add(resolvedTypeName);
            typeNamespace = dic.Add(resolvedNamespace);
            return true;
        }
        
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver KTResolver)
        {
            if (typeNamespace.Equals("http://www.default.com"))
            {
                if (typeName.Equals("UserType"))
                {
                    return typeof(UserTypeContainer);
                }
            }
            if (typeNamespace.Equals("http://www.w3.org/2001/XMLSchema") && typeName.Equals("int"))
            {
                return typeof(UnknownEmployee);
            }

            return KTResolver.ResolveName(typeName, typeNamespace, declaredType, null);
        }
    }

    [Serializable]
    public class SimpleResolver_Ser : DataContractResolver
    {
        private static readonly string s_defaultNs = "http://schemas.datacontract.org/2004/07/";
        public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver KTResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            string resolvedNamespace = string.Empty;
            resolvedNamespace = s_defaultNs;
            XmlDictionary dictionary = new XmlDictionary();
            typeName = dictionary.Add(dcType.FullName);
            typeNamespace = dictionary.Add(resolvedNamespace);
            return true;
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver KTResolver)
        {
            throw new NotImplementedException("Deserialization is supposed to be handled by the SimpleResolver_DeSer resolver.");
        }
    }

    [Serializable]
    public class SimpleResolver_DeSer : DataContractResolver
    {
        private static readonly string s_defaultNs = "http://schemas.datacontract.org/2004/07/";
        public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver KTResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            throw new NotImplementedException("Serialization is supposed to be handled by the SimpleResolver_Ser resolver.");
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver KTResolver)
        {
            if (typeNamespace.Equals(s_defaultNs))
            {
                if (typeName.Equals(typeof(DCRVariations).FullName))
                {
                    return typeof(DCRVariations);
                }
                if (typeName.Equals(typeof(Person).FullName))
                {
                    return typeof(Person);
                }
                if (typeName.Equals(typeof(SimpleDC).FullName))
                {
                    return typeof(SimpleDC);
                }           
            }

            return KTResolver.ResolveName(typeName, typeNamespace, declaredType, null);
        }
    }

    public class VerySimpleResolver : DataContractResolver
    {
        public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver KTResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            if (!KTResolver.TryResolveType(dcType, declaredType, null, out typeName, out typeNamespace))
            {
                typeName = new XmlDictionary().Add(dcType.FullName);
                typeNamespace = new XmlDictionary().Add(dcType.Assembly.FullName);
            }

            return true;
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver KTResolver)
        {
            Type t = KTResolver.ResolveName(typeName, typeNamespace, declaredType, null);
            if (t == null)
            {
                try
                {
                    t = Type.GetType(typeName + "," + typeNamespace);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return t;
        }
    }

    public class ResolverDefaultCollections : DataContractResolver
    {
        private readonly static string s_defaultNs = "http://www.default.com";
        public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver KTResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            string resolvedNamespace = string.Empty;
            resolvedNamespace = s_defaultNs;
            XmlDictionary dictionary = new XmlDictionary();
            typeName = dictionary.Add(dcType.FullName);            
            typeNamespace = dictionary.Add(resolvedNamespace);
            return true;
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver KTResolver)
        {
            if (typeNamespace.Equals(s_defaultNs))
            {
                if (typeName.Equals(typeof(Person).FullName))
                {
                    return typeof(Person);
                }
                if (typeName.Equals(typeof(CharClass).FullName))
                {
                    return typeof(CharClass);
                }
                if (typeName.Equals("System.String"))
                {
                    return typeof(string);
                }
                if (typeName.Equals(typeof(Version1).FullName))
                {
                    return typeof(Version1);
                }
                if (typeName.Equals(typeof(Employee).FullName))
                {
                    return typeof(Employee);
                }
            }

            return KTResolver.ResolveName(typeName, typeNamespace, declaredType, null);
        }
    }

    [Serializable]
    public class SimpleResolver : DataContractResolver
    {
        private static readonly string s_defaultNS = "http://schemas.datacontract.org/2004/07/";

        private TypeLibraryManager _mgr = new TypeLibraryManager();

        public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver KTResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            string resolvedTypeName = string.Empty;
            string resolvedNamespace = string.Empty;
            XmlDictionary dic = new XmlDictionary();

            if (_mgr.AllTypesList.Contains(dcType))
            {
                resolvedTypeName = dcType.FullName + "***";
                resolvedNamespace = s_defaultNS + resolvedTypeName;
                typeName = dic.Add(resolvedTypeName);
                typeNamespace = dic.Add(resolvedNamespace);
            }
            else
            {
                KTResolver.TryResolveType(dcType, declaredType, null, out typeName, out typeNamespace);
            }
            if (typeName == null || typeNamespace == null)
            {
                XmlDictionary dictionary = new XmlDictionary();
                typeName = dictionary.Add(dcType.FullName);
                typeNamespace = dictionary.Add(dcType.Assembly.FullName);
            }
            return true;
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver KTResolver)
        {
            TypeLibraryManager mgr = new TypeLibraryManager();
            string inputTypeName = typeName.Trim('*');
            Type result = null;
            if (null != mgr.AllTypesHashtable[inputTypeName])
            {
                result = (Type)mgr.AllTypesHashtable[inputTypeName];
            }
            else
            {
                result = KTResolver.ResolveName(typeName, typeNamespace, declaredType, null);
            }
            if (null == result)
            {
                try
                {
                    result = Type.GetType(String.Format("{0}, {1}", typeName, typeNamespace));
                }
                catch (System.IO.FileLoadException)
                {
                    //Type.GetType throws exception on netfx if it cannot find a type while it just returns null on NetCore. 
                    //The behavior difference of Type.GetType is a known issue. 
                    //Catch the exception so that test case can pass on netfx.
                    return null;
                }
            }
            return result;
        }
    }
}
