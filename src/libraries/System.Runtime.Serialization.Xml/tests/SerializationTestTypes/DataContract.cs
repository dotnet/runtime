// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SerializationTestTypes
{
    interface IGenericNameProvider
    {
        int GetParameterCount();
        string GetParameterName(int paramIndex);
        string GetNamespaces();
        string GetGenericTypeName();
        bool ParametersFromBuiltInNamespaces { get; }
    }

    class GenericNameProvider : IGenericNameProvider
    {
        string genericTypeName;
        object[] genericParams;
        internal GenericNameProvider(Type type)
            : this(type.GetGenericTypeDefinition().FullName, type.GetGenericArguments())
        {
        }

        internal GenericNameProvider(string genericTypeName, object[] genericParams)
        {
            this.genericTypeName = genericTypeName;
            this.genericParams = new object[genericParams.Length];
            genericParams.CopyTo(this.genericParams, 0);
        }

        public int GetParameterCount()
        {
            return genericParams.Length;
        }

        public string GetParameterName(int paramIndex)
        {
            return GetStableName(paramIndex).Name;
        }

        public string GetNamespaces()
        {
            StringBuilder namespaces = new StringBuilder();
            for (int j = 0; j < GetParameterCount(); j++)
                namespaces.Append(" ").Append(GetStableName(j).Namespace);
            return namespaces.ToString();
        }

        public string GetGenericTypeName()
        {
            return genericTypeName;
        }

        public bool ParametersFromBuiltInNamespaces
        {
            get
            {
                bool parametersFromBuiltInNamespaces = true;
                for (int j = 0; j < GetParameterCount(); j++)
                {
                    if (parametersFromBuiltInNamespaces)
                        parametersFromBuiltInNamespaces = DataContract.IsBuiltInNamespace(GetStableName(j).Namespace);
                    else
                        break;
                }
                return parametersFromBuiltInNamespaces;
            }
        }

        XmlQualifiedName GetStableName(int i)
        {
            object o = genericParams[i];
            XmlQualifiedName qname = o as XmlQualifiedName;
            if (qname == null)
            {
                Type paramType = o as Type;
                if (paramType != null)
                    genericParams[i] = qname = DataContract.GetStableName(paramType);
                else
                    genericParams[i] = qname = ((DataContract)o).StableName;
            }
            return qname;
        }
    }

    class GenericInfo : IGenericNameProvider
    {
        string genericTypeName;
        XmlQualifiedName stableName;
        List<GenericInfo> paramGenericInfos;

        internal GenericInfo(XmlQualifiedName stableName, string genericTypeName)
        {
            this.stableName = stableName;
            this.genericTypeName = genericTypeName;
        }

        internal void Add(GenericInfo actualParamInfo)
        {
            if (paramGenericInfos == null)
                paramGenericInfos = new List<GenericInfo>();
            paramGenericInfos.Add(actualParamInfo);
        }

        internal XmlQualifiedName GetExpandedStableName()
        {
            if (paramGenericInfos == null)
                return stableName;
            return new XmlQualifiedName(DataContract.ExpandGenericParameters(XmlConvert.DecodeName(stableName.Name), this), stableName.Namespace);
        }

        internal string GetStableNamespace()
        {
            return stableName.Namespace;
        }

        internal XmlQualifiedName StableName
        {
            get { return stableName; }
        }

        internal IList<GenericInfo> Parameters
        {
            get { return paramGenericInfos; }
        }

        public int GetParameterCount()
        {
            return paramGenericInfos.Count;
        }

        public string GetParameterName(int paramIndex)
        {
            return paramGenericInfos[paramIndex].GetExpandedStableName().Name;
        }

        public string GetNamespaces()
        {
            StringBuilder namespaces = new StringBuilder();
            for (int j = 0; j < paramGenericInfos.Count; j++)
                namespaces.Append(" ").Append(paramGenericInfos[j].GetStableNamespace());
            return namespaces.ToString();
        }

        public string GetGenericTypeName()
        {
            return genericTypeName;
        }

        public bool ParametersFromBuiltInNamespaces
        {
            get
            {
                bool parametersFromBuiltInNamespaces = true;
                for (int j = 0; j < paramGenericInfos.Count; j++)
                {
                    if (parametersFromBuiltInNamespaces)
                        parametersFromBuiltInNamespaces = DataContract.IsBuiltInNamespace(paramGenericInfos[j].GetStableNamespace());
                    else
                        break;
                }
                return parametersFromBuiltInNamespaces;
            }
        }

    }

    class RuntimeTypeHandleEqualityComparer : IEqualityComparer<RuntimeTypeHandle>
    {
        static RuntimeTypeHandleEqualityComparer comparer;

        public static RuntimeTypeHandleEqualityComparer Comparer
        {
            get
            {
                if (comparer == null)
                {
                    comparer = new RuntimeTypeHandleEqualityComparer();
                }

                return comparer;
            }
        }

        public bool Equals(RuntimeTypeHandle x, RuntimeTypeHandle y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(RuntimeTypeHandle obj)
        {
            return obj.GetHashCode();
        }
    }

    public class DataContract
    {
        static Dictionary<RuntimeTypeHandle, DataContract> cache = new Dictionary<RuntimeTypeHandle, DataContract>(RuntimeTypeHandleEqualityComparer.Comparer);
        static MD5 md5 = null;

        Type underlyingType;
        bool isValueType;
        XmlQualifiedName stableName;
        protected internal bool supportCollectionDataContract;

        public static DataContract GetDataContract(Type type, bool supportCollectionDataContract)
        {
            return GetDataContract(type.TypeHandle, type, supportCollectionDataContract);
        }

        internal static bool IsBuiltInNamespace(string ns)
        {
            return (ns == Globals.SchemaNamespace || ns == Globals.SerializationNamespace);
        }

        internal static string ExpandGenericParameters(string format, IGenericNameProvider genericNameProvider)
        {
            string digest = null;
            StringBuilder typeName = new StringBuilder();
            for (int i = 0; i < format.Length; i++)
            {
                char ch = format[i];
                if (ch == '{')
                {
                    i++;
                    int start = i;
                    for (; i < format.Length; i++)
                        if (format[i] == '}')
                            break;
                    if (i == format.Length)
                        throw new ArgumentException("GenericNameBraceMismatch");
                    if (format[start] == '#' && i == (start + 1))
                    {
                        if (!genericNameProvider.ParametersFromBuiltInNamespaces)
                        {
                            if (digest == null)
                                digest = GetNamespacesDigest(string.Format(CultureInfo.InvariantCulture, " {0}{1}", genericNameProvider.GetParameterCount(), genericNameProvider.GetNamespaces()));
                            typeName.Append(digest);
                        }
                    }
                    else
                    {
                        int paramIndex;
                        if (!int.TryParse(format.Substring(start, i - start), out paramIndex) || paramIndex < 0 || paramIndex >= genericNameProvider.GetParameterCount())
                            throw new ArgumentException("GenericParameterNotValid");
                        typeName.Append(genericNameProvider.GetParameterName(paramIndex));
                    }
                }
                else
                    typeName.Append(ch);
            }
            return typeName.ToString();
        }

        private static string GetNamespacesDigest(string namespaces)
        {
            if (md5 == null)
                md5 = MD5.Create();
            byte[] namespaceBytes = Encoding.UTF8.GetBytes(namespaces);
            byte[] digestBytes = md5.ComputeHash(namespaceBytes);
            char[] digestChars = new char[24];
            const int digestLen = 6;
            int digestCharsLen = Convert.ToBase64CharArray(digestBytes, 0, digestLen, digestChars, 0);
            StringBuilder digest = new StringBuilder();
            for (int i = 0; i < digestCharsLen; i++)
            {
                char ch = digestChars[i];
                switch (ch)
                {
                    case '=':
                        break;
                    case '/':
                        digest.Append("_S");
                        break;
                    case '+':
                        digest.Append("_P");
                        break;
                    default:
                        digest.Append(ch);
                        break;
                }
            }
            return digest.ToString();
        }

        internal virtual DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract> boundContracts)
        {
            return this;
        }

        public static DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type type)
        {
            return GetDataContract(typeHandle, type, false);
        }

        public static DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type type, bool supportCollectionDataContract)
        {
            DataContract dataContract = null;
            if (!cache.TryGetValue(typeHandle, out dataContract))
            {
                lock (cache)
                {
                    if (!cache.TryGetValue(typeHandle, out dataContract))
                    {
                        if (type == null)
                            type = Type.GetTypeFromHandle(typeHandle);
                        type = UnwrapNullableType(type);
                        dataContract = CreateDataContract(type, supportCollectionDataContract);
                        cache.Add(typeHandle, dataContract);
                    }
                }
            }
            return dataContract;
        }

        internal static Type UnwrapNullableType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
                type = type.GetGenericArguments()[0];
            return type;
        }

        public static DataContract CreateDataContract(Type type, bool supportCollectionDataContract)
        {
            DataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(type);

            if (primitiveContract != null)
                return primitiveContract;

            if (type.IsArray)
                return new ArrayDataContract(type);

            if (type.IsEnum)
                return new EnumDataContract(type);
            if (supportCollectionDataContract)
            {
                DataContract collectionDataContract;
                Type itemType;
                if (CollectionDataContract.IsCollectionOrTryCreate(type, true, out collectionDataContract, out itemType, true))
                {
                    return collectionDataContract;
                }
            }
            return new ClassDataContract(type);
        }

        public static XmlQualifiedName GetStableName(Type type)
        {
            bool hasDataContract;
            return GetStableName(type, out hasDataContract);
        }

        public static XmlQualifiedName GetStableName(Type type, bool supportCollectionDataContract)
        {
            bool hasDataContract;
            return GetStableName(type, out hasDataContract, supportCollectionDataContract);
        }

        public static XmlQualifiedName GetStableName(Type type, out bool hasDataContract)
        {
            return GetStableName(type, out hasDataContract, false);
        }

        public static XmlQualifiedName GetStableName(Type type, out bool hasDataContract, bool supportCollectionDataContract)
        {
            DataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
            if (primitiveContract != null)
            {
                hasDataContract = false;
                return primitiveContract.StableName;
            }

            string name = null, ns = null;
            if (type.IsArray)
            {
                hasDataContract = false;
                Type elementType = type;
                string arrayOfPrefix = "";
                while (elementType.IsArray)
                {
                    arrayOfPrefix += "ArrayOf";
                    elementType = elementType.GetElementType();
                    if (elementType == Globals.TypeOfByteArray)
                        break;
                }
                XmlQualifiedName elementStableName = GetStableName(elementType, supportCollectionDataContract);
                name = arrayOfPrefix + elementStableName.Name;
                ns = (elementStableName.Namespace == Globals.SchemaNamespace) ? Globals.SerializationNamespace : elementStableName.Namespace;
            }
            else if (type.IsSerializable && !type.IsEnum)
            {
                if (type.IsDefined(Globals.TypeOfDataContractAttribute, false))
                    throw new Exception("TypeTwoWaySerializable" + type.FullName);
                hasDataContract = false;
                name = GetDefaultStableLocalName(type);
                ns = GetDefaultStableNamespace(type);
            }
            else
            {
                object[] dataContractAttributes = type.GetCustomAttributes(Globals.TypeOfDataContractAttribute, false);
                if (dataContractAttributes != null && dataContractAttributes.Length > 0)
                {
                    DataContractAttribute dataContractAttribute = (DataContractAttribute)dataContractAttributes[0];
                    hasDataContract = true;
                    if (dataContractAttribute.Name != null)
                    {
                        name = dataContractAttribute.Name;
                        if (name.Length == 0)
                            throw new Exception("InvalidDataContractName" + type.FullName);
                    }
                    else
                        name = GetDefaultStableLocalName(type);

                    if (dataContractAttribute.Namespace != null)
                    {
                        ns = dataContractAttribute.Namespace;
                    }
                    else
                    {
                        string clrNs = type.Namespace;
                        if (clrNs == null)
                            clrNs = string.Empty;
                        ns = GetGlobalContractNamespace(clrNs, type.Module);
                        if (ns == null)
                            ns = GetGlobalContractNamespace(clrNs, type.Assembly);

                        if (ns == null)
                            ns = GetDefaultStableNamespace(type);
                    }
                    Uri uri;
                    if (Uri.TryCreate(ns, UriKind.RelativeOrAbsolute, out uri))
                    {
                        string normalizedNs = uri.ToString();

                        if (normalizedNs == Globals.SerializationNamespace || normalizedNs == Globals.SchemaNamespace || normalizedNs == Globals.SchemaInstanceNamespace)
                            throw new Exception("InvalidNamespace" + type.FullName + " :" + Globals.SerializationNamespace + " :" + Globals.SchemaNamespace + " :" + Globals.SchemaInstanceNamespace);
                    }
                }
                else if (type.IsEnum)
                {
                    hasDataContract = false;
                    name = GetDefaultStableLocalName(type);
                    ns = GetDefaultStableNamespace(type);
                }
                else if (type.GetInterface("System.Xml.Serialization.IXmlSerializable") != null)
                {
                    hasDataContract = false;
                    name = GetDefaultStableLocalName(type);
                    ns = GetDefaultStableNamespace(type);
                }
                else
                {
                    object[] collectionDataContractAttributes = type.GetCustomAttributes(Globals.TypeOfCollectionDataContractAttribute, false);
                    if (supportCollectionDataContract && collectionDataContractAttributes != null && collectionDataContractAttributes.Length > 0)
                    {
                        CollectionDataContractAttribute collectionDataContractAttribute = (CollectionDataContractAttribute)collectionDataContractAttributes[0];
                        hasDataContract = true;
                        if (collectionDataContractAttribute.Name != null)
                        {
                            name = collectionDataContractAttribute.Name;
                            if (name.Length == 0)
                                throw new Exception("InvalidDataContractName" + type.FullName);
                        }
                        else
                            name = GetDefaultStableLocalName(type);

                        if (collectionDataContractAttribute.Namespace != null)
                        {
                            ns = collectionDataContractAttribute.Namespace;
                        }
                        else
                        {
                            string clrNs = type.Namespace;
                            if (clrNs == null)
                                clrNs = string.Empty;
                            ns = GetGlobalContractNamespace(clrNs, type.Module);
                            if (ns == null)
                                ns = GetGlobalContractNamespace(clrNs, type.Assembly);

                            if (ns == null)
                                ns = GetDefaultStableNamespace(type);
                        }
                        Uri uri;
                        if (Uri.TryCreate(ns, UriKind.RelativeOrAbsolute, out uri))
                        {
                            string normalizedNs = uri.ToString();

                            if (normalizedNs == Globals.SerializationNamespace || normalizedNs == Globals.SchemaNamespace || normalizedNs == Globals.SchemaInstanceNamespace)
                                throw new Exception("InvalidNamespace" + type.FullName + " :" + Globals.SerializationNamespace + " :" + Globals.SchemaNamespace + " :" + Globals.SchemaInstanceNamespace);
                        }
                    }
                    else
                    {
                        hasDataContract = false;
                        name = GetDefaultStableLocalName(type);
                        ns = GetDefaultStableNamespace(type);
                    }
                }
            }
            return new XmlQualifiedName(name, ns);
        }

        static string GetDefaultStableLocalName(Type type)
        {
            if (type.DeclaringType == null)
                return type.Name;
            int nsLen = (type.Namespace == null) ? 0 : type.Namespace.Length;
            if (nsLen > 0)
                nsLen++;
            return type.FullName.Substring(nsLen).Replace('+', '.');
        }

        static string GetDefaultStableNamespace(Type type)
        {
            return GetDefaultStableNamespace(type.Namespace);
        }

        public static string GetDefaultStableNamespace(string clrNs)
        {
            if (clrNs == null) clrNs = string.Empty;
            return Globals.DefaultNamespace + clrNs.Replace('.', '/');
        }

        static string GetGlobalContractNamespace(string clrNs, ICustomAttributeProvider customAttribuetProvider)
        {
            object[] nsAttributes = customAttribuetProvider.GetCustomAttributes(typeof(ContractNamespaceAttribute), false);
            string dataContractNs = null;
            for (int i = 0; i < nsAttributes.Length; i++)
            {
                ContractNamespaceAttribute nsAttribute = (ContractNamespaceAttribute)nsAttributes[i];
                string clrNsInAttribute = nsAttribute.ClrNamespace;
                if (clrNsInAttribute == null)
                    clrNsInAttribute = string.Empty;
                if (clrNsInAttribute == clrNs)
                {
                    if (nsAttribute.ContractNamespace == null)
                        throw new Exception("InvalidGlobalContractNamespace:" + clrNs);
                    if (dataContractNs != null)
                        throw new Exception("ContractNamespaceAlreadySet:" + dataContractNs + " :: " + nsAttribute.ContractNamespace + " :: " + clrNs);
                    dataContractNs = nsAttribute.ContractNamespace;
                }
            }
            return dataContractNs;
        }

        bool GetIsReferenceValue()
        {
            Type type = this.UnderlyingType;
            {
                object[] dataContractAttributes = type.GetCustomAttributes(Globals.TypeOfDataContractAttribute, false);
                object[] serAttributes = type.GetCustomAttributes(Globals.TypeOfSerializableAttribute, false);

                if (dataContractAttributes != null && dataContractAttributes.Length > 0)
                {
                    DataContractAttribute dataContractAttribute = (DataContractAttribute)dataContractAttributes[0];
                    if (!dataContractAttribute.IsReference && this is ClassDataContract && ((ClassDataContract)this).BaseContract != null)
                    {
                        return ((ClassDataContract)this).BaseContract.IsReference;
                    }
                    else
                    {
                        return dataContractAttribute.IsReference;
                    }
                }
                else if (serAttributes != null && serAttributes.Length > 0)
                {
                    if (this is ClassDataContract && ((ClassDataContract)this).BaseContract != null)
                    {
                        return ((ClassDataContract)this).BaseContract.IsReference;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            {
                object[] collectionDataContractAttributes = type.GetCustomAttributes(Globals.TypeOfCollectionDataContractAttribute, false);
                if (collectionDataContractAttributes != null && collectionDataContractAttributes.Length > 0)
                {
                    CollectionDataContractAttribute collectionDataContractAttribute = (CollectionDataContractAttribute)collectionDataContractAttributes[0];
                    return collectionDataContractAttribute.IsReference;
                }
            }
            return false;
        }

        public DataContract()
        {
        }

        public DataContract(bool supportCollectionDataContract)
        {
            this.supportCollectionDataContract = supportCollectionDataContract;
        }

        public DataContract(Type type)
        {
            underlyingType = type;
            isValueType = type.IsValueType;
        }

        public DataContract(Type type, bool supportCollectionDataContract)
            : this(type)
        {
            this.supportCollectionDataContract = supportCollectionDataContract;
        }

        public Type UnderlyingType
        {
            get { return underlyingType; }
        }

        public virtual string TopLevelElementName
        {
            get { return StableName.Name; }
        }

        public virtual string TopLevelElementNamespace
        {
            get { return StableName.Namespace; }
        }

        public bool IsValueType
        {
            get { return isValueType; }
            set { isValueType = value; }
        }

        public XmlQualifiedName StableName
        {
            get { return stableName; }
            set { stableName = value; }
        }

        public virtual bool IsISerializable
        {
            get { return false; }
            set { throw new InvalidOperationException("RequiresClassDataContractToSetIsISerializable"); }
        }

        public bool IsReference
        {
            get { return GetIsReferenceValue(); }
        }

        static bool IsAlpha(char ch)
        {
            return (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z');
        }

        static bool IsDigit(char ch)
        {
            return (ch >= '0' && ch <= '9');
        }

        static bool IsAsciiLocalName(string localName)
        {
            if (string.IsNullOrEmpty(localName) || localName.Length == 0)
                return false;
            if (!IsAlpha(localName[0]))
                return false;
            for (int i = 1; i < localName.Length; i++)
            {
                char ch = localName[i];
                if (!IsAlpha(ch) && !IsDigit(ch))
                    return false;
            }
            return true;
        }

        internal static string EncodeLocalName(string localName)
        {
            if (IsAsciiLocalName(localName))
                return localName;

            if (IsValidNCName(localName))
                return localName;

            return XmlConvert.EncodeLocalName(localName);
        }

        internal static bool IsValidNCName(string name)
        {
            try
            {
                XmlConvert.VerifyNCName(name);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }

        public override bool Equals(object other)
        {
            if ((object)this == other)
                return true;
            DataContract dataContract = other as DataContract;
            if (dataContract != null)
            {
                return (StableName.Name == dataContract.StableName.Name && StableName.Namespace == dataContract.StableName.Namespace && IsValueType == dataContract.IsValueType && IsReference == dataContract.IsReference);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class ClassDataContract : DataContract
    {
        ClassDataContract baseContract;
        List<DataMember> members;
        string[][] memberNames;
        bool isISerializable;
        bool hasDataContract;

        public ClassDataContract(Type type)
            : this(type, false)
        {
        }

        public ClassDataContract(Type type, bool supportCollectionDataContract)
            : base(type, supportCollectionDataContract)
        {
            Init(type);
        }


        void Init(Type type)
        {
            this.StableName = DataContract.GetStableName(type, out hasDataContract, supportCollectionDataContract);
            Type baseType = type.BaseType;
            isISerializable = (Globals.TypeOfISerializable.IsAssignableFrom(type));
            if (isISerializable)
            {
                if (hasDataContract)
                    throw new Exception("DataContractTypeCannotBeISerializable: " + type.FullName);
                if (!Globals.TypeOfISerializable.IsAssignableFrom(baseType))
                {
                    while (baseType != null)
                    {
                        if (baseType.IsDefined(Globals.TypeOfDataContractAttribute, false))
                            throw new Exception("ISerializableCannotInheritFromDataContract:" + type.FullName + "::" + baseType.FullName);
                        baseType = baseType.BaseType;
                    }
                }
            }
            if (baseType != null && baseType != Globals.TypeOfObject && baseType != Globals.TypeOfValueType)
                this.BaseContract = (ClassDataContract)DataContract.GetDataContract(baseType, supportCollectionDataContract);
            else
                this.BaseContract = null;
        }

        void ImportDataMembers()
        {
            Type type = this.UnderlyingType;
            members = new List<DataMember>();
            Dictionary<string, DataMember> memberNamesTable = new Dictionary<string, DataMember>();
            MemberInfo[] memberInfos = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < memberInfos.Length; i++)
            {
                MemberInfo member = memberInfos[i];
                if (hasDataContract)
                {
                    object[] memberAttributes = member.GetCustomAttributes(typeof(System.Runtime.Serialization.DataMemberAttribute), false);
                    if (memberAttributes != null && memberAttributes.Length > 0)
                    {
                        if (memberAttributes.Length > 1)
                            throw new Exception("TooManyDataMembers:" + member.DeclaringType.FullName + "::" + member.Name);
                        if (member.MemberType == MemberTypes.Property)
                        {
                            PropertyInfo property = (PropertyInfo)member;
                            MethodInfo getMethod = property.GetGetMethod(true);
                            if (getMethod != null && IsMethodOverriding(getMethod))
                                continue;
                            MethodInfo setMethod = property.GetSetMethod(true);
                            if (setMethod != null && IsMethodOverriding(setMethod))
                                continue;
                            if (getMethod == null)
                                throw new Exception(" NoGetMethodForProperty : " + property.DeclaringType + " ::" + property.Name);
                            if (setMethod == null)
                                throw new Exception("NoSetMethodForProperty : " + property.DeclaringType + " :: " + property.Name);
                            if (getMethod.GetParameters().Length > 0)
                                throw new Exception("IndexedPropertyCannotBeSerialized :" + property.DeclaringType + " :: " + property.Name);
                        }
                        else if (member.MemberType != MemberTypes.Field)
                            throw new Exception("InvalidMember : " + type.FullName + " :: " + member.Name + " :: " + typeof(System.Runtime.Serialization.DataMemberAttribute).FullName);

                        DataMember memberContract = new DataMember(member);
                        System.Runtime.Serialization.DataMemberAttribute memberAttribute = (System.Runtime.Serialization.DataMemberAttribute)memberAttributes[0];
                        if (memberAttribute.Name == null)
                            memberContract.Name = member.Name;
                        else
                            memberContract.Name = memberAttribute.Name;
                        memberContract.Order = memberAttribute.Order;
                        memberContract.IsRequired = memberAttribute.IsRequired;
                        CheckAndAddMember(members, memberContract, memberNamesTable);
                    }
                }
                else
                {
                    FieldInfo field = member as FieldInfo;
                    if (field != null && !field.IsNotSerialized)
                    {
                        DataMember memberContract = new DataMember(member);
                        memberContract.Name = member.Name;
                        object[] optionalFields = field.GetCustomAttributes(Globals.TypeOfOptionalFieldAttribute, false);
                        if (optionalFields == null || optionalFields.Length == 0)
                            memberContract.Order = Globals.DefaultVersion;
                        else
                        {
                            memberContract.IsRequired = Globals.DefaultIsRequired;
                        }
                        CheckAndAddMember(members, memberContract, memberNamesTable);
                    }
                }
            }
            if (members.Count > 1)
                members.Sort(DataMemberComparer.Singleton);
        }

        public static void CheckAndAddMember(List<DataMember> members, DataMember memberContract, Dictionary<string, DataMember> memberNamesTable)
        {
            DataMember existingMemberContract;
            if (memberNamesTable.TryGetValue(memberContract.Name, out existingMemberContract))
                throw new Exception("DupMemberName :" + existingMemberContract.MemberInfo.Name + " :: " + memberContract.MemberInfo.Name + " :: " + memberContract.MemberInfo.DeclaringType.FullName + " :: " + memberContract.Name);
            memberNamesTable.Add(memberContract.Name, memberContract);
            members.Add(memberContract);
        }

        static bool IsMethodOverriding(MethodInfo method)
        {
            return method.IsVirtual && ((method.Attributes & MethodAttributes.NewSlot) == 0);
        }

        public ClassDataContract BaseContract
        {
            get { return baseContract; }
            set { baseContract = value; }
        }

        public string[][] MemberNames
        {
            get
            {
                if (memberNames == null)
                {
                    lock (this)
                    {
                        if (memberNames == null && Members != null)
                        {
                            if (baseContract == null)
                                memberNames = new string[1][];
                            else
                            {
                                int baseTypesCount = baseContract.MemberNames.Length;
                                memberNames = new string[baseTypesCount + 1][];
                                Array.Copy(baseContract.MemberNames, memberNames, baseTypesCount);
                            }
                            string[] declaredMemberNames = new string[Members.Count];
                            for (int i = 0; i < Members.Count; i++)
                                declaredMemberNames[i] = Members[i].Name;
                            memberNames[memberNames.Length - 1] = declaredMemberNames;
                        }
                    }
                }
                return memberNames;
            }
        }

        public List<DataMember> Members
        {
            get
            {
                if (members == null && UnderlyingType != null && !IsISerializable)
                {
                    lock (this)
                    {
                        if (members == null)
                        {
                            ImportDataMembers();
                        }
                    }
                }
                return members;
            }
            set { members = value; }
        }

        public override bool Equals(object other)
        {
            if ((object)this == other)
                return true;

            if (base.Equals(other))
            {
                ClassDataContract dataContract = other as ClassDataContract;
                if (dataContract != null)
                {
                    if (IsISerializable)
                    {
                        if (!dataContract.IsISerializable)
                            return false;
                    }
                    else
                    {
                        if (dataContract.IsISerializable)
                            return false;

                        if (Members == null)
                        {
                            if (dataContract.Members != null)
                                return false;
                        }
                        else if (dataContract.Members == null)
                            return false;
                        else
                        {
                            if (Members.Count != dataContract.Members.Count)
                                return false;

                            for (int i = 0; i < Members.Count; i++)
                            {
                                if (!Members[i].Equals(dataContract.Members[i]))
                                    return false;
                            }
                        }
                    }

                    if (BaseContract == null)
                        return (dataContract.BaseContract == null);
                    else if (dataContract.BaseContract == null)
                        return false;
                    else
                        return BaseContract.StableName.Equals(dataContract.BaseContract.StableName);
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public class DataMemberComparer : IComparer<DataMember>
        {
            public int Compare(DataMember x, DataMember y)
            {
                if (x.Order < y.Order)
                    return -1;
                if (x.Order > y.Order)
                    return 1;
                return string.Compare(x.Name, y.Name, StringComparison.InvariantCulture);
            }

            public bool Equals(DataMember x, DataMember y)
            {
                return x == y;
            }

            public int GetHashCode(DataMember x)
            {
                return ((object)x).GetHashCode();
            }
            public static DataMemberComparer Singleton = new DataMemberComparer();
        }

        public class DataMemberOrderComparer : IComparer<DataMember>
        {
            public int Compare(DataMember x, DataMember y)
            {
                return x.Order - y.Order;
            }

            public bool Equals(DataMember x, DataMember y)
            {
                return x.Order == y.Order;
            }

            public int GetHashCode(DataMember x)
            {
                return ((object)x).GetHashCode();
            }
            public static DataMemberOrderComparer Singleton = new DataMemberOrderComparer();
        }

    }

    public class DataMember
    {
        DataContract memberTypeContract;
        string name;
        int order;
        bool isRequired;
        bool isNullable;
        MemberInfo memberInfo;
        protected internal bool supportCollectionDataContract;

        public DataMember()
        {
        }

        public DataMember(MemberInfo memberInfo)
        {
            this.memberInfo = memberInfo;
        }

        public MemberInfo MemberInfo
        {
            get { return memberInfo; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public int Order
        {
            get { return order; }
            set { order = value; }
        }

        internal bool IsNullable
        {
            get { return isNullable; }
            set { isNullable = value; }
        }

        public bool IsRequired
        {
            get { return isRequired; }
            set { isRequired = value; }
        }

        public object GetMemberValue(object obj)
        {
            FieldInfo field = MemberInfo as FieldInfo;

            if (field != null)
                return ((FieldInfo)MemberInfo).GetValue(obj);

            return ((PropertyInfo)MemberInfo).GetValue(obj, null);
        }

        public Type MemberType
        {
            get
            {
                FieldInfo field = MemberInfo as FieldInfo;
                if (field != null)
                    return field.FieldType;
                return ((PropertyInfo)MemberInfo).PropertyType;
            }
        }

        public DataContract MemberTypeContract
        {
            get
            {
                if (memberTypeContract == null)
                {
                    if (MemberInfo != null)
                    {
                        lock (this)
                        {
                            if (memberTypeContract == null)
                            {
                                memberTypeContract = DataContract.GetDataContract(MemberType, supportCollectionDataContract);
                            }
                        }
                    }
                }
                return memberTypeContract;
            }
            set
            {
                memberTypeContract = value;
            }
        }

        public override bool Equals(object other)
        {
            if ((object)this == other)
                return true;

            DataMember dataMember = other as DataMember;
            if (dataMember != null)
            {
                return (Name == dataMember.Name
                        && IsNullable == dataMember.IsNullable
                        && IsRequired == dataMember.IsRequired
                        && MemberTypeContract.StableName.Equals(dataMember.MemberTypeContract.StableName));
            }
            return false;
        }

        public static bool operator <(DataMember dm1, DataMember dm2)
        {
            if (dm1.Order != dm2.Order)
                return dm1.Order < dm2.Order;
            else
                return ((string.Compare(dm1.Name, dm2.Name) < 0) ? true : false);
        }

        public static bool operator >(DataMember dm1, DataMember dm2)
        {
            if (dm1.Order != dm2.Order)
                return dm1.Order > dm2.Order;
            else
                return ((string.Compare(dm1.Name, dm2.Name) > 0) ? true : false);
        }

        public bool NameOrderEquals(object other)
        {
            if ((object)this == other)
                return true;
            DataMember dataMember = other as DataMember;
            if (dataMember != null)
            {
                return (Name == dataMember.Name
                        && Order == dataMember.Order
                        && MemberTypeContract.StableName.Equals(dataMember.MemberTypeContract.StableName));
            }
            return false;

        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class PrimitiveDataContract : DataContract
    {
        static Dictionary<Type, PrimitiveDataContract> typeToContract = new Dictionary<Type, PrimitiveDataContract>();
        static Dictionary<XmlQualifiedName, PrimitiveDataContract> nameToContract = new Dictionary<XmlQualifiedName, PrimitiveDataContract>();
        static PrimitiveDataContract objectContract = new PrimitiveDataContract(typeof(object));

        static PrimitiveDataContract()
        {
            Add(new PrimitiveDataContract(typeof(char)));
            Add(new PrimitiveDataContract(typeof(bool)));
            Add(new PrimitiveDataContract(typeof(sbyte)));
            Add(new PrimitiveDataContract(typeof(byte)));
            Add(new PrimitiveDataContract(typeof(short)));
            Add(new PrimitiveDataContract(typeof(ushort)));
            Add(new PrimitiveDataContract(typeof(int)));
            Add(new PrimitiveDataContract(typeof(uint)));
            Add(new PrimitiveDataContract(typeof(long)));
            Add(new PrimitiveDataContract(typeof(ulong)));
            Add(new PrimitiveDataContract(typeof(float)));
            Add(new PrimitiveDataContract(typeof(double)));
            Add(new PrimitiveDataContract(typeof(decimal)));
            Add(new PrimitiveDataContract(typeof(DateTime)));
            Add(new PrimitiveDataContract(typeof(string)));
            Add(new PrimitiveDataContract(typeof(byte[])));
            Add(new PrimitiveDataContract(typeof(TimeSpan)));
            Add(new PrimitiveDataContract(typeof(Guid)));
            Add(new PrimitiveDataContract(typeof(Uri)));
            Add(objectContract);
        }
        public static void Add(PrimitiveDataContract primitiveContract)
        {
            typeToContract.Add(primitiveContract.UnderlyingType, primitiveContract);
            nameToContract.Add(primitiveContract.StableName, primitiveContract);
        }

        public static PrimitiveDataContract GetPrimitiveDataContract(Type type)
        {
            PrimitiveDataContract retVal = null;
            if (type.IsInterface)
                retVal = objectContract;
            else
                typeToContract.TryGetValue(type, out retVal);
            return retVal;
        }

        public static PrimitiveDataContract GetPrimitiveDataContract(string name, string ns)
        {
            PrimitiveDataContract retVal = null;
            nameToContract.TryGetValue(new XmlQualifiedName(name, ns), out retVal);
            return retVal;
        }

        PrimitiveDataContract(Type type) : base(type)
        {
            string name = null;
            string ns = Globals.SchemaNamespace;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Char:
                    name = "char";
                    ns = Globals.SerializationNamespace;
                    break;
                case TypeCode.Boolean:
                    name = "boolean";
                    break;
                case TypeCode.SByte:
                    name = "byte";
                    break;
                case TypeCode.Byte:
                    name = "unsignedByte";
                    break;
                case TypeCode.Int16:
                    name = "short";
                    break;
                case TypeCode.UInt16:
                    name = "unsignedShort";
                    break;
                case TypeCode.Int32:
                    name = "int";
                    break;
                case TypeCode.UInt32:
                    name = "unsignedInt";
                    break;
                case TypeCode.Int64:
                    name = "long";
                    break;
                case TypeCode.UInt64:
                    name = "unsignedLong";
                    break;
                case TypeCode.Single:
                    name = "float";
                    break;
                case TypeCode.Double:
                    name = "double";
                    break;
                case TypeCode.Decimal:
                    name = "decimal";
                    break;
                case TypeCode.DateTime:
                    name = "dateTime";
                    break;
                case TypeCode.String:
                    name = "string";
                    break;
                default:
                    if (type == Globals.TypeOfByteArray)
                    {
                        name = "base64Binary";
                        ns = Globals.SerializationNamespace;
                    }
                    else if (type == Globals.TypeOfObject)
                    {
                        name = "anyType";
                    }
                    else if (type == Globals.TypeOfTimeSpan)
                    {
                        name = "duration";
                    }
                    else if (type == Globals.TypeOfGuid)
                    {
                        name = "guid";
                    }
                    else if (type == Globals.TypeOfUri)
                    {
                        name = "anyURI";
                    }
                    else
                        throw new Exception(string.Format("{0} is an invalidPrimitiveType", type.FullName));
                    break;
            }
            StableName = new XmlQualifiedName(name, ns);
        }

        public override string TopLevelElementNamespace
        {
            get { return Globals.SerializationNamespace; }
        }
    }

    public class ArrayDataContract : DataContract
    {
        DataContract itemContract;
        int rank;

        public ArrayDataContract(Type type, bool supportCollectionDataContract)
            : base(type, supportCollectionDataContract)
        {
            rank = type.GetArrayRank();
            StableName = DataContract.GetStableName(type, supportCollectionDataContract);
        }

        public ArrayDataContract(Type type)
            : this(type, false)
        {
        }

        public DataContract ItemContract
        {
            get
            {
                if (itemContract == null && UnderlyingType != null)
                {
                    itemContract = DataContract.GetDataContract(UnderlyingType.GetElementType(), supportCollectionDataContract);
                }
                return itemContract;
            }
            set
            {
                itemContract = value;
            }
        }

        public override bool Equals(object other)
        {
            if ((object)this == other)
                return true;

            if (base.Equals(other))
            {
                ArrayDataContract dataContract = other as ArrayDataContract;
                if (dataContract != null)
                {
                    return ItemContract.Equals(dataContract.itemContract);
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class EnumDataContract : DataContract
    {
        PrimitiveDataContract baseContract;
        List<DataMember> members;
        List<long> values;
        bool isULong;
        bool isFlags;
        bool hasDataContract;

        public EnumDataContract()
        {
            IsValueType = true;
        }

        public EnumDataContract(Type type) : base(type)
        {
            StableName = DataContract.GetStableName(type, out hasDataContract);
            Type baseType = Enum.GetUnderlyingType(type);
            baseContract = PrimitiveDataContract.GetPrimitiveDataContract(baseType);
            isULong = (baseType == Globals.TypeOfULong);
            IsFlags = type.IsDefined(Globals.TypeOfFlagsAttribute, false);
        }

        public PrimitiveDataContract BaseContract
        {
            get
            {
                return baseContract;
            }
            set
            {
                baseContract = value;
                isULong = (baseContract.UnderlyingType == Globals.TypeOfULong);
            }
        }

        void ImportDataMembers()
        {
            Type type = this.UnderlyingType;
            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            members = new List<DataMember>(fields.Length);
            Dictionary<string, DataMember> memberNamesTable = new Dictionary<string, DataMember>();
            values = new List<long>(fields.Length);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                bool dataMemberValid = false;
                object[] memberAttributes = field.GetCustomAttributes(Globals.TypeOfDataMemberAttribute, false);
                if (hasDataContract)
                {
                    if (memberAttributes != null && memberAttributes.Length > 0)
                    {
                        if (memberAttributes.Length > 1)
                            throw new Exception("TooManyDataMembers :" + field.DeclaringType.FullName + " :: " + field.Name);
                        DataMemberAttribute memberAttribute = (DataMemberAttribute)memberAttributes[0];
                        DataMember memberContract = new DataMember(field);
                        if (memberAttribute.Name == null || memberAttribute.Name.Length == 0)
                            memberContract.Name = field.Name;
                        else
                            memberContract.Name = memberAttribute.Name;
                        ClassDataContract.CheckAndAddMember(members, memberContract, memberNamesTable);
                        dataMemberValid = true;
                    }
                }
                else
                {
                    if (!field.IsNotSerialized)
                    {
                        DataMember memberContract = new DataMember(field);
                        memberContract.Name = field.Name;
                        ClassDataContract.CheckAndAddMember(members, memberContract, memberNamesTable);
                        dataMemberValid = true;
                    }
                }

                if (dataMemberValid)
                {
                    object enumValue = field.GetValue(null);
                    if (isULong)
                        values.Add((long)((IConvertible)enumValue).ToUInt64(null));
                    else
                        values.Add(((IConvertible)enumValue).ToInt64(null));
                }
            }
        }

        public List<DataMember> Members
        {
            get
            {
                if (members == null && UnderlyingType != null)
                {
                    lock (this)
                    {
                        if (members == null)
                        {
                            ImportDataMembers();
                        }
                    }
                }
                return members;
            }
            set { members = value; }
        }

        public List<long> Values
        {
            get
            {
                if (values == null && UnderlyingType != null)
                {
                    lock (this)
                    {
                        if (values == null)
                        {
                            ImportDataMembers();
                        }
                    }
                }
                return values;
            }
            set { values = value; }
        }

        public bool IsFlags
        {
            get { return isFlags; }
            set { isFlags = value; }
        }

        public override bool Equals(object other)
        {
            if ((object)this == other)
                return true;

            if (base.Equals(other))
            {
                EnumDataContract dataContract = other as EnumDataContract;
                if (dataContract != null)
                {
                    if (IsFlags != dataContract.IsFlags)
                        return false;

                    if (Members.Count != dataContract.Members.Count || Values.Count != dataContract.Values.Count)
                        return false;

                    for (int i = 0; i < Members.Count; i++)
                    {
                        if (Values[i] != dataContract.Values[i] || Members[i].Name != dataContract.Members[i].Name)
                            return false;
                    }

                    return BaseContract.StableName.Equals(dataContract.BaseContract.StableName);
                }
            }
            return false;
        }
        public override int GetHashCode()
        {
            if (members != null)
            {
                int hash = 0;
                foreach (DataMember member in members)
                    hash += member.Name.GetHashCode();
                hash += base.StableName.GetHashCode();
                return hash;
            }
            else
            {
                return base.GetHashCode();
            }
        }
    }

    public enum CollectionKind : byte
    {
        None,
        GenericDictionary,
        Dictionary,
        GenericList,
        GenericCollection,
        List,
        GenericEnumerable,
        Collection,
        Enumerable,
        Array,
    }

    [DataContractAttribute(Namespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
    public struct KeyValue<K, V>
    {
        K key;
        V value;

        internal KeyValue(K key, V value)
        {
            this.key = key;
            this.value = value;
        }

        [DataMember(IsRequired = true)]
        public K Key
        {
            get { return key; }
            set { key = value; }
        }

        [DataMember(IsRequired = true)]
        public V Value
        {
            get { return value; }
            set { this.value = value; }
        }
    }

    public class CollectionDataContract : DataContract
    {
        public CollectionKind Kind;
        public Type ItemType;
        public string ItemName;
        public string CollectionItemName;
        public string KeyName;
        public string ValueName;

        public bool IsDictionary
        {
            get { return !string.IsNullOrEmpty(KeyName); }
        }

        public MethodInfo GetEnumeratorMethod;
        public MethodInfo AddMethod;
        public ConstructorInfo Constructor;

        public CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor)
            : base(type)
        {
            this.supportCollectionDataContract = true;
            StableName = DataContract.GetStableName(type, true); //TODO
            Kind = kind;
            this.ItemType = itemType;
            this.KeyName = null;
            this.ValueName = null;

            this.GetEnumeratorMethod = getEnumeratorMethod;
            this.AddMethod = addMethod;
            this.Constructor = constructor;
            if (itemType != null)
            {
                itemContract = DataContract.GetDataContract(itemType, true);
            }
            Init(type);
        }

        DataContract itemContract;
        public DataContract ItemContract
        {
            get
            {
                if (itemContract == null && UnderlyingType != null)
                {
                    itemContract = DataContract.GetDataContract(UnderlyingType.GetElementType(), true);
                }
                return itemContract;
            }
            set
            {
                itemContract = value;
            }
        }

        void Init(Type type)
        {
            object[] collectionDataContractAttributes = type.GetCustomAttributes(Globals.TypeOfCollectionDataContractAttribute, false);
            CollectionDataContractAttribute collectionContractAttribute = null;

            if (collectionDataContractAttributes != null && collectionDataContractAttributes.Length > 0)
            {
                collectionContractAttribute = (CollectionDataContractAttribute)collectionDataContractAttributes[0];
            }

            if (ItemType != null)
            {
                bool isDictionary = (Kind == CollectionKind.Dictionary || Kind == CollectionKind.GenericDictionary);
                string itemName = null, keyName = null, valueName = null;
                if (collectionContractAttribute != null)
                {
                    if (!string.IsNullOrEmpty(collectionContractAttribute.ItemName))
                    {
                        itemName = DataContract.EncodeLocalName(collectionContractAttribute.ItemName);
                    }
                    if (!string.IsNullOrEmpty(collectionContractAttribute.KeyName))
                    {
                        keyName = DataContract.EncodeLocalName(collectionContractAttribute.KeyName);
                    }
                    if (!string.IsNullOrEmpty(collectionContractAttribute.ValueName))
                    {
                        valueName = DataContract.EncodeLocalName(collectionContractAttribute.ValueName);
                    }
                }

                this.ItemName = itemName ?? DataContract.GetStableName(DataContract.UnwrapNullableType(ItemType), true).Name;
                this.CollectionItemName = this.ItemName;
                if (isDictionary)
                {
                    this.KeyName = keyName ?? Globals.KeyLocalName;
                    this.ValueName = valueName ?? Globals.ValueLocalName;
                }
            }
        }

        static bool IsCollectionHelper(Type type, out Type itemType, bool constructorRequired)
        {
            if (type.IsArray && DataContract.GetDataContract(type, true) == null)
            {
                itemType = type.GetElementType();
                return true;
            }
            DataContract dataContract;
            return IsCollectionOrTryCreate(type, false /*tryCreate*/, out dataContract, out itemType, constructorRequired);
        }

        internal static bool IsCollectionDataContract(Type type)
        {
            return type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, false);
        }

        public static bool IsCollectionOrTryCreate(Type type, bool tryCreate, out DataContract dataContract, out Type itemType, bool constructorRequired)
        {
            dataContract = null;
            itemType = Globals.TypeOfObject;
            MethodInfo addMethod, getEnumeratorMethod;
            bool hasCollectionDataContract = IsCollectionDataContract(type);
            Type baseType = type.BaseType;
            bool isBaseTypeCollection = (baseType != null && baseType != Globals.TypeOfObject
                && baseType != Globals.TypeOfValueType && baseType != Globals.TypeOfUri) ? IsCollection(baseType) : false;

            if (!Globals.TypeOfIEnumerable.IsAssignableFrom(type) ||
                IsDC(type) || Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
            {
                return false;
            }

            if (type.IsInterface)
            {
                Type interfaceTypeToCheck = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                Type[] knownInterfaces = KnownInterfaces;
                for (int i = 0; i < knownInterfaces.Length; i++)
                {
                    if (knownInterfaces[i] == interfaceTypeToCheck)
                    {
                        addMethod = null;
                        if (type.IsGenericType)
                        {
                            Type[] genericArgs = type.GetGenericArguments();
                            if (interfaceTypeToCheck == Globals.TypeOfIDictionaryGeneric)
                            {
                                itemType = Globals.TypeOfKeyValue.MakeGenericType(genericArgs);
                                addMethod = type.GetMethod(Globals.AddMethodName);
                                getEnumeratorMethod = Globals.TypeOfIEnumerableGeneric.MakeGenericType(Globals.TypeOfKeyValuePair.MakeGenericType(genericArgs)).GetMethod(Globals.GetEnumeratorMethodName);
                            }
                            else
                            {
                                itemType = genericArgs[0];
                                getEnumeratorMethod = Globals.TypeOfIEnumerableGeneric.MakeGenericType(itemType).GetMethod(Globals.GetEnumeratorMethodName);
                            }
                        }
                        else
                        {
                            if (interfaceTypeToCheck == Globals.TypeOfIDictionary)
                            {
                                itemType = typeof(KeyValue<object, object>);
                                addMethod = type.GetMethod(Globals.AddMethodName);
                            }
                            else
                            {
                                itemType = Globals.TypeOfObject;
                            }
                            getEnumeratorMethod = Globals.TypeOfIEnumerable.GetMethod(Globals.GetEnumeratorMethodName);
                        }
                        if (tryCreate)
                            dataContract = new CollectionDataContract(type, (CollectionKind)(i + 1), itemType, getEnumeratorMethod, addMethod, null/*defaultCtor*/);
                        return true;
                    }
                }
            }
            ConstructorInfo defaultCtor = null;
            if (!type.IsValueType && constructorRequired)
            {
                defaultCtor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Globals.EmptyTypeArray, null);
            }

            Type knownInterfaceType = null;
            CollectionKind kind = CollectionKind.None;
            bool multipleDefinitions = false;
            Type[] interfaceTypes = type.GetInterfaces();
            foreach (Type interfaceType in interfaceTypes)
            {
                Type interfaceTypeToCheck = interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
                Type[] knownInterfaces = KnownInterfaces;
                for (int i = 0; i < knownInterfaces.Length; i++)
                {
                    if (knownInterfaces[i] == interfaceTypeToCheck)
                    {
                        CollectionKind currentKind = (CollectionKind)(i + 1);
                        if (kind == CollectionKind.None || currentKind < kind)
                        {
                            kind = currentKind;
                            knownInterfaceType = interfaceType;
                            multipleDefinitions = false;
                        }
                        else if ((kind & currentKind) == currentKind)
                            multipleDefinitions = true;
                        break;
                    }
                }
            }

            if (kind == CollectionKind.None)
            {
                throw new Exception("CollectionTypeIsNotIEnumerable");
            }

            if (kind == CollectionKind.Enumerable || kind == CollectionKind.Collection || kind == CollectionKind.GenericEnumerable)
            {
                if (multipleDefinitions)
                    knownInterfaceType = Globals.TypeOfIEnumerable;
                itemType = knownInterfaceType.IsGenericType ? knownInterfaceType.GetGenericArguments()[0] : Globals.TypeOfObject;
                GetCollectionMethods(type, knownInterfaceType, new Type[] { itemType },
                                     false,
                                     out getEnumeratorMethod, out addMethod);
                if (tryCreate)
                    dataContract = new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, addMethod, defaultCtor);
            }
            else
            {
                if (multipleDefinitions)
                {
                    throw new Exception("SR.CollectionTypeHasMultipleDefinitionsOfInterface");
                }

                Type[] addMethodTypeArray = null;
                switch (kind)
                {
                    case CollectionKind.GenericDictionary:
                        addMethodTypeArray = knownInterfaceType.GetGenericArguments();
                        bool isOpenGeneric = knownInterfaceType.IsGenericTypeDefinition
                            || (addMethodTypeArray[0].IsGenericParameter && addMethodTypeArray[1].IsGenericParameter);
                        itemType = isOpenGeneric ? Globals.TypeOfKeyValue : Globals.TypeOfKeyValue.MakeGenericType(addMethodTypeArray);
                        break;
                    case CollectionKind.Dictionary:
                        addMethodTypeArray = new Type[] { Globals.TypeOfObject, Globals.TypeOfObject };
                        itemType = Globals.TypeOfKeyValue.MakeGenericType(addMethodTypeArray);
                        break;
                    case CollectionKind.GenericList:
                    case CollectionKind.GenericCollection:
                        addMethodTypeArray = knownInterfaceType.GetGenericArguments();
                        itemType = addMethodTypeArray[0];
                        break;
                    case CollectionKind.List:
                        itemType = Globals.TypeOfObject;
                        addMethodTypeArray = new Type[] { itemType };
                        break;
                }
                if (tryCreate)
                {
                    GetCollectionMethods(type, knownInterfaceType, addMethodTypeArray,
                                     true,
                                     out getEnumeratorMethod, out addMethod);
                    dataContract = new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, addMethod, defaultCtor);
                }
            }
            return true;
        }

        internal static bool IsDC(Type type)
        {
            if (type.GetCustomAttributes(Globals.TypeOfDataContractAttribute, false) != null && type.GetCustomAttributes(Globals.TypeOfDataContractAttribute, false).Length > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool IsCollection(Type type)
        {
            Type itemType;
            return IsCollection(type, out itemType);
        }

        internal static bool IsCollection(Type type, out Type itemType)
        {
            return IsCollectionHelper(type, out itemType, true /*constructorRequired*/);
        }

        static Type[] _knownInterfaces;
        internal static Type[] KnownInterfaces
        {
            get
            {
                if (_knownInterfaces == null)
                {
                    _knownInterfaces = new Type[]
                    {
                        Globals.TypeOfIDictionaryGeneric,
                        Globals.TypeOfIDictionary,
                        Globals.TypeOfIListGeneric,
                        Globals.TypeOfICollectionGeneric,
                        Globals.TypeOfIList,
                        Globals.TypeOfIEnumerableGeneric,
                        Globals.TypeOfICollection,
                        Globals.TypeOfIEnumerable
                    };
                }
                return _knownInterfaces;
            }
        }

        static void FindCollectionMethodsOnInterface(Type type, Type interfaceType, ref MethodInfo addMethod, ref MethodInfo getEnumeratorMethod)
        {
            InterfaceMapping mapping = type.GetInterfaceMap(interfaceType);
            for (int i = 0; i < mapping.TargetMethods.Length; i++)
            {
                if (mapping.InterfaceMethods[i].Name == Globals.AddMethodName)
                    addMethod = mapping.InterfaceMethods[i];
                else if (mapping.InterfaceMethods[i].Name == Globals.GetEnumeratorMethodName)
                    getEnumeratorMethod = mapping.InterfaceMethods[i];
            }
        }

        static bool IsKnownInterface(Type type)
        {
            Type typeToCheck = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            foreach (Type knownInterfaceType in KnownInterfaces)
            {
                if (typeToCheck == knownInterfaceType)
                {
                    return true;
                }
            }
            return false;
        }

        static void GetCollectionMethods(Type type, Type interfaceType, Type[] addMethodTypeArray, bool addMethodOnInterface, out MethodInfo getEnumeratorMethod, out MethodInfo addMethod)
        {
            addMethod = getEnumeratorMethod = null;

            if (addMethodOnInterface)
            {
                addMethod = type.GetMethod(Globals.AddMethodName, BindingFlags.Instance | BindingFlags.Public, null, addMethodTypeArray, null);
                if (addMethod == null || addMethod.GetParameters()[0].ParameterType != addMethodTypeArray[0])
                {
                    FindCollectionMethodsOnInterface(type, interfaceType, ref addMethod, ref getEnumeratorMethod);
                    if (addMethod == null)
                    {
                        Type[] parentInterfaceTypes = interfaceType.GetInterfaces();
                        foreach (Type parentInterfaceType in parentInterfaceTypes)
                        {
                            if (IsKnownInterface(parentInterfaceType))
                            {
                                FindCollectionMethodsOnInterface(type, parentInterfaceType, ref addMethod, ref getEnumeratorMethod);
                                if (addMethod == null)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                addMethod = type.GetMethod(Globals.AddMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, addMethodTypeArray, null);
                if (addMethod == null)
                    return;
            }

            if (getEnumeratorMethod == null)
            {
                getEnumeratorMethod = type.GetMethod(Globals.GetEnumeratorMethodName, BindingFlags.Instance | BindingFlags.Public, null, Globals.EmptyTypeArray, null);
                if (getEnumeratorMethod == null || !Globals.TypeOfIEnumerator.IsAssignableFrom(getEnumeratorMethod.ReturnType))
                {
                    Type ienumerableInterface = interfaceType.GetInterface("System.Collections.Generic.IEnumerable*");
                    if (ienumerableInterface == null)
                        ienumerableInterface = Globals.TypeOfIEnumerable;
                    getEnumeratorMethod = GetTargetMethodWithName(Globals.GetEnumeratorMethodName, type, ienumerableInterface);
                }
            }
        }

        internal static MethodInfo GetTargetMethodWithName(string name, Type type, Type interfaceType)
        {
            InterfaceMapping mapping = type.GetInterfaceMap(interfaceType);
            for (int i = 0; i < mapping.TargetMethods.Length; i++)
            {
                if (mapping.InterfaceMethods[i].Name == name)
                    return mapping.InterfaceMethods[i];
            }
            return null;
        }
        public override bool Equals(object other)
        {
            if ((object)this == other)
                return true;
            if (base.Equals(other))
            {
                CollectionDataContract collectionContract = other as CollectionDataContract;
                if (collectionContract != null)
                {
                    if (!collectionContract.ItemContract.Equals(this.ItemContract)) { return false; }
                    if (collectionContract.ItemName != this.ItemName) { return false; }
                    if (collectionContract.KeyName != this.KeyName) { return false; }
                    if (collectionContract.ValueName != this.ValueName) { return false; }
                    if (collectionContract.TopLevelElementName != this.TopLevelElementName) { return false; }
                    if (collectionContract.TopLevelElementNamespace != this.TopLevelElementNamespace) { return false; }
                    if (collectionContract.IsDictionary != this.IsDictionary) { return false; }
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public static class Globals
    {
        internal static Type TypeOfObject = typeof(object);
        internal static Type TypeOfValueType = typeof(ValueType);
        internal static Type TypeOfArray = typeof(Array);
        internal static Type TypeOfEnum = typeof(Enum);
        internal static Type TypeOfString = typeof(string);
        internal static Type TypeOfStringArray = typeof(string[]);
        internal static Type TypeOfInt = typeof(int);
        internal static Type TypeOfIntArray = typeof(int[]);
        internal static Type TypeOfLong = typeof(long);
        internal static Type TypeOfULong = typeof(ulong);
        internal static Type TypeOfVoid = typeof(void);
        internal static Type TypeOfDouble = typeof(double);
        internal static Type TypeOfBool = typeof(bool);
        internal static Type TypeOfByte = typeof(byte);
        internal static Type TypeOfByteArray = typeof(byte[]);
        internal static Type TypeOfTimeSpan = typeof(TimeSpan);
        internal static Type TypeOfGuid = typeof(Guid);
        internal static Type TypeOfUri = typeof(Uri);
        internal static Type TypeOfIntPtr = typeof(IntPtr);
        internal static Type TypeOfStreamingContext = typeof(StreamingContext);
        internal static Type TypeOfISerializable = typeof(ISerializable);
        internal static Type TypeOfIDeserializationCallback = typeof(IDeserializationCallback);
        internal static Type TypeOfIObjectReference = typeof(IObjectReference);
        internal static Type TypeOfBytePtr = typeof(byte*);
        internal static Type TypeOfKnownTypeAttribute = typeof(KnownTypeAttribute);
        internal static Type TypeOfDataContractAttribute = typeof(DataContractAttribute);
        internal static Type TypeOfContractNamespaceAttribute = typeof(ContractNamespaceAttribute);
        internal static Type TypeOfDataMemberAttribute = typeof(DataMemberAttribute);
        internal static Type TypeOfOptionalFieldAttribute = typeof(OptionalFieldAttribute);
        internal static Type TypeOfObjectArray = typeof(object[]);
        internal static Type TypeOfOnSerializingAttribute = typeof(OnSerializingAttribute);
        internal static Type TypeOfOnSerializedAttribute = typeof(OnSerializedAttribute);
        internal static Type TypeOfOnDeserializingAttribute = typeof(OnDeserializingAttribute);
        internal static Type TypeOfOnDeserializedAttribute = typeof(OnDeserializedAttribute);
        internal static Type TypeOfFlagsAttribute = typeof(FlagsAttribute);
        internal static Type TypeOfSerializableAttribute = typeof(SerializableAttribute);
        internal static Type TypeOfSerializationInfo = typeof(SerializationInfo);
        internal static Type TypeOfSerializationInfoEnumerator = typeof(SerializationInfoEnumerator);
        internal static Type TypeOfSerializationEntry = typeof(SerializationEntry);
        internal static Type TypeOfIXmlSerializable = typeof(IXmlSerializable);
        internal static Type TypeOfXmlSchemaProviderAttribute = typeof(XmlSchemaProviderAttribute);
        internal static Type TypeOfXmlRootAttribute = typeof(XmlRootAttribute);
        internal static Type TypeOfXmlQualifiedName = typeof(XmlQualifiedName);
        internal static Type TypeOfXmlSchemaType = typeof(XmlSchemaType);
        internal static Type TypeOfXmlSchemaSet = typeof(XmlSchemaSet);
        internal static object[] EmptyObjectArray = new object[0];
        internal static Type[] EmptyTypeArray = new Type[0];
        internal static Type TypeOfIExtensibleDataObject = typeof(IExtensibleDataObject);
        internal static Type TypeOfExtensionDataObject = typeof(ExtensionDataObject);
        internal static Type TypeOfNullable = typeof(Nullable<>);
        internal static Type TypeOfCollectionDataContractAttribute = typeof(CollectionDataContractAttribute);
        internal static Type TypeOfIEnumerable = typeof(IEnumerable);
        internal static Type TypeOfIDictionaryGeneric = typeof(IDictionary<,>);
        internal static Type TypeOfIEnumerableGeneric = typeof(IEnumerable<>);
        internal static Type TypeOfIDictionary = typeof(IDictionary);
        internal static Type TypeOfKeyValuePair = typeof(KeyValuePair<,>);
        internal static Type TypeOfKeyValue = typeof(KeyValue<,>);
        internal static Type TypeOfIListGeneric = typeof(IList<>);
        internal static Type TypeOfICollectionGeneric = typeof(ICollection<>);
        internal static Type TypeOfIList = typeof(IList);
        internal static Type TypeOfICollection = typeof(ICollection);
        internal static Type TypeOfIEnumerator = typeof(IEnumerator);
        public const string KeyLocalName = "Key";
        public const string ValueLocalName = "Value";
        public const string AddMethodName = "Add";
        public static string SchemaInstanceNamespace = XmlSchema.InstanceNamespace;
        public static string SchemaNamespace = XmlSchema.Namespace;
        public static string DefaultNamespace = "http://tempuri.org/";
        public static bool DefaultIsRequired = false;
        public static int DefaultVersion = 1;
        public static string GetEnumeratorMethodName = "GetEnumerator";
        public static string SerializationNamespace = "http://schemas.microsoft.com/2003/10/Serialization/";
    }
}
