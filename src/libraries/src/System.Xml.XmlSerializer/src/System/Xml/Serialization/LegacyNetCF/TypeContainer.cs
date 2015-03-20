// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Xml;
using System.Text;

namespace System.Xml.Serialization.LegacyNetCF
{
    /// <summary>
    /// Specifies how the serializer should serialize and desrialize a 
    /// given type. 
    /// </summary>
    internal enum SerializerType
    {
        Primitive,
        Array,
        Complex,
        Enumerated,
        Serializable,
        XmlNode,
        Custom,
        UnknownHeader,
        Collection,
        Enumerable,
        SoapFault
    }

    /// <summary>
    /// Specifies the type of the object.
    /// </summary>
    internal enum TypeID
    {
        Boolean,        // 0 (0x00)
        Byte,           // 1 (0x01)
        Char,           // 2 (0x02)
        Decimal,        // 3 (0x03)
        Double,         // 4 (0x04)
        Int16,          // 5 (0x05)
        Int32,          // 6 (0x06)
        Int64,          // 7 (0x07)
        SByte,          // 8 (0x08)
        Single,         // 9 (0x09)
        UInt16,         //10 (0x0A)
        UInt32,         //11 (0x0B)
        UInt64,         //12 (0x0C)
        String,         //13 (0x0D)
        DateTime,       //14 (0x0E)
        //other types
        Guid,           //15 (0x0F)
        //soap only types
        Date,           //16 (0x10)
        Time,           //17 (0x11)
        //generic array type
        ArrayLike,      //18 (0x12)
        //generic
        Compound        //19 (0x13)
    }

    /// <summary>
    /// For the byte[] types, which textual representation format should be used.
    /// </summary>
    internal enum CustomSerializerType
    {
        None,
        Base64,
        Hex,
        QName,
        Object,
    }

    [Flags]
    internal enum TypeOrigin
    {
        None = 0x0000,
        Intrinsic = 0x0001,
        User = 0x0010,
        All = Intrinsic | User
    }

    internal struct TypeAndNamespace
    {
        public TypeAndNamespace(Type type, string defaultNamespace)
        {
            if (type == null) throw new ArgumentNullException("type");
            Type = type;
            DefaultNamespace = defaultNamespace;
        }
        public readonly Type Type;
        public readonly string DefaultNamespace;

        public override bool Equals(object obj)
        {
            if (!(obj is TypeAndNamespace)) return false;
            TypeAndNamespace other = (TypeAndNamespace)obj;
            return Type == other.Type && DefaultNamespace == other.DefaultNamespace;
        }
        public override int GetHashCode()
        {
            return Type.GetHashCode() + (DefaultNamespace != null ? DefaultNamespace.GetHashCode() : 0);
        }
    }

    internal class TypeContainer
    {
        // Static storage for built in types
        private static Dictionary<XmlQualifiedName, LogicalType> s_intrinsicsByName;
        private static Dictionary<Type, LogicalType> s_intrinsicsByType;

        // Instance storage for user defined type using encoded symantics
        private Dictionary<XmlQualifiedName, LogicalType> _userEncodedTypesByQName = new Dictionary<XmlQualifiedName, LogicalType>();
        private Dictionary<TypeAndNamespace, LogicalType> _userEncodedTypesByType = new Dictionary<TypeAndNamespace, LogicalType>();

        // Instance storage for user defined type using literal symantics
        private Dictionary<XmlQualifiedName, LogicalType> _userLiteralTypesByQName = new Dictionary<XmlQualifiedName, LogicalType>();
        private Dictionary<TypeAndNamespace, LogicalType> _userLiteralTypesByType = new Dictionary<TypeAndNamespace, LogicalType>();

        // Locks the look up operations
        private object _lookUpLock = new object();

        // PERFORMANCE FEATURE:
        // For an outgoing message, compound types are serialized after the 
        // wrapper element.  This means that namespaces for every element 
        // need to redeclare unless they are defined on the Envelope.  This
        // member is a collection of all of namespaces that will may appear 
        // in the encoded message. These namespaces will be defined on the 
        // Envelope.         
        private List<string> _userEncodedNamespaces = new List<string>(1);

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Static Constructor
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//

        static TypeContainer()
        {
            //These types come the fx xml serialization file Types.cs
            // I don't have a type for Object (i.e. any).
            // Do I need ambiguous?
            LogicalType[] iTypes = new LogicalType[] {
                MakeIntrinsicType( typeof(string), "string", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, true ),
                MakeIntrinsicType( typeof(int), "int", Soap.XsdUrl, SerializerType.Primitive, TypeID.Int32, true ),
                MakeIntrinsicType( typeof(bool), "boolean", Soap.XsdUrl, SerializerType.Primitive, TypeID.Boolean, true),
                MakeIntrinsicType( typeof(short), "short", Soap.XsdUrl, SerializerType.Primitive, TypeID.Int16, true ),
                MakeIntrinsicType( typeof(long), "long", Soap.XsdUrl, SerializerType.Primitive, TypeID.Int64, true ),
                MakeIntrinsicType( typeof(float), "float", Soap.XsdUrl, SerializerType.Primitive, TypeID.Single, true ),
                MakeIntrinsicType( typeof(double), "double", Soap.XsdUrl, SerializerType.Primitive, TypeID.Double, true ),
                MakeIntrinsicType( typeof(decimal), "decimal", Soap.XsdUrl, SerializerType.Primitive, TypeID.Decimal, true ),
                MakeIntrinsicType( typeof(DateTime), "dateTime", Soap.XsdUrl, SerializerType.Primitive, TypeID.DateTime, true ),
                MakeIntrinsicType( typeof(System.Xml.XmlQualifiedName), "QName", Soap.XsdUrl, SerializerType.Custom, TypeID.Compound, true, CustomSerializerType.QName ),
                MakeIntrinsicType( typeof(byte), "unsignedByte", Soap.XsdUrl, SerializerType.Primitive, TypeID.Byte, true ),
                MakeIntrinsicType( typeof(sbyte), "byte", Soap.XsdUrl, SerializerType.Primitive, TypeID.SByte, true ),
                MakeIntrinsicType( typeof(ushort), "unsignedShort", Soap.XsdUrl, SerializerType.Primitive, TypeID.UInt16, true ),
                MakeIntrinsicType( typeof(uint), "unsignedInt", Soap.XsdUrl, SerializerType.Primitive, TypeID.UInt32, true ),
                MakeIntrinsicType( typeof(ulong), "unsignedLong", Soap.XsdUrl, SerializerType.Primitive, TypeID.UInt64, true ),
                MakeIntrinsicType( typeof(object), "anyType", Soap.XsdUrl, SerializerType.Complex, TypeID.Compound, true, CustomSerializerType.Object ),
                //start ambiguous types
                MakeIntrinsicType( typeof(DateTime), "date", Soap.XsdUrl, SerializerType.Primitive, TypeID.Date, false ),
                MakeIntrinsicType( typeof(DateTime), "time", Soap.XsdUrl, SerializerType.Primitive, TypeID.Time, false ),
                MakeIntrinsicType( typeof(string), "Name", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "NCName", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "NMTOKEN", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "NMTOKENS", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(byte[]), "base64Binary", Soap.XsdUrl, SerializerType.Custom, TypeID.Compound, true, CustomSerializerType.Base64 ),
                MakeIntrinsicType( typeof(byte[]), "hexBinary", Soap.XsdUrl, SerializerType.Custom, TypeID.Compound, false, CustomSerializerType.Hex ),
                //start non-xsd types
                MakeIntrinsicType( typeof(Guid), "guid", Soap.UrtTypesNS, SerializerType.Primitive, TypeID.Guid, true ),
                MakeIntrinsicType( typeof(char), "char", Soap.UrtTypesNS, SerializerType.Primitive, TypeID.Char, true ),
                MakeIntrinsicType( typeof(byte[]), "base64", Soap.Encoding, SerializerType.Custom, TypeID.Compound, false, CustomSerializerType.Base64 ),
                //begin unsupported types.
                MakeIntrinsicType( typeof(string), "anyURI", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "duration", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "ENTITY", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "ENTITIES", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "gDay", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "gMonth", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "gMonthDay", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "gYear", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "gYearMonth", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "ID", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "IDREF", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "IDREFS", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "integer", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "language", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "negativeInteger", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "nonNegativeInteger", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "nonPositiveInteger", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "normalizedString", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "NOTATION", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "positiveInteger", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false ),
                MakeIntrinsicType( typeof(string), "token", Soap.XsdUrl, SerializerType.Primitive, TypeID.String, false )
            };

            s_intrinsicsByType = new Dictionary<Type, LogicalType>();
            for (int i = 0; i < iTypes.Length; ++i)
            {
                if ((iTypes[i].MappingFlags & TypeMappingFlags.AllowTypeMapping) != 0)
                    s_intrinsicsByType[iTypes[i].Type] = iTypes[i];
            }

            s_intrinsicsByName = new Dictionary<XmlQualifiedName, LogicalType>(iTypes.Length);
            for (int i = 0; i < iTypes.Length; ++i)
            {
                XmlQualifiedName qname = new XmlQualifiedName(iTypes[i].Name, iTypes[i].Namespace);
                Debug.Assert(!s_intrinsicsByName.ContainsKey(qname), "Adding the same intrinsic name twice");
                s_intrinsicsByName[qname] = iTypes[i];
            }
        }

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Static Helper Methods
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//


        /// <summary>
        /// Creates a LogicalType for a type that is intrinsic to the system that are
        /// are always present in the system.
        /// </summary>
        public static LogicalType MakeIntrinsicType(Type type, string name, string ns, SerializerType serializer, TypeID typeID, bool allowTypeMapping, CustomSerializerType customSerializerType)
        {
            TypeMappingFlags flags = allowTypeMapping ? TypeMappingFlags.Default : TypeMappingFlags.AllowNameMapping;
            bool isNullable = type.GetTypeInfo().IsValueType || type == typeof(object) || type == typeof(string) || type == typeof(XmlQualifiedName);
            LogicalType lType = new LogicalType(name, ns, true, type, isNullable, serializer, typeID, flags);
            lType.CustomSerializer = customSerializerType;
            if (customSerializerType == CustomSerializerType.Object)
            {
                lType.Members = new MemberValueCollection();
            }
            return lType;
        }
        public static LogicalType MakeIntrinsicType(Type type, string name, string ns, SerializerType serializer, TypeID typeID, bool allowTypeMapping)
        {
            return MakeIntrinsicType(type, name, ns, serializer, typeID, allowTypeMapping, CustomSerializerType.None);
        }

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Type Access Methods
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//

        /// <summary>
        /// Check whether the specified Type object is intrinsicly mapped to a LogicalType object.
        /// Returns true if there is a mapping. Otherwise, false is returned. 
        /// </summary>
        public static bool ContainsIntrinsicType(Type type)
        {
            return s_intrinsicsByType.ContainsKey(type);
        }

        /// <summary>
        /// Check whether the specified Type object is mapped to a LogicalType object.
        /// Returns true if there is a mapping. Otherwise, false is returned. 
        /// </summary>
        /// <param name="typeOrigin">Specifies whether the type that was found is either 
        /// intrinsic or user defined.</param>
        public bool ContainsType(Type type, string ns, bool encoded, out TypeOrigin typeOrigin)
        {
            return ContainsType(new TypeAndNamespace(type, ns), encoded, TypeOrigin.All, out typeOrigin);
        }

        /// <summary>
        /// Check whether the specified Type object is mapped to a LogicalType object.
        /// Returns true if there is a mapping. Otherwise, false is returned. 
        /// </summary>
        /// <param name="typeOrigin">Specifies whether the type that was found is either 
        /// intrinsic or user defined.</param>
        public bool ContainsType(Type type, string ns, bool encoded, TypeOrigin originsToSearch, out TypeOrigin typeOrigin)
        {
            return ContainsType(new TypeAndNamespace(type, ns), encoded, originsToSearch, out typeOrigin);
        }

        private bool ContainsType(TypeAndNamespace typeAndNamespace, bool encoded, TypeOrigin originsToSearch, out TypeOrigin typeOrigin)
        {
            // If a specific namespace is needed, check to see whether the type sought after is
            // a one-namespace-fits-all and has already been reflected.
            if (typeAndNamespace.DefaultNamespace != null)
            { // looking for a specific namespace
                // does a one-size-fits-all exist?
                bool anyNamespaceVersionExists = ContainsTypeInternal(new TypeAndNamespace(typeAndNamespace.Type, null),
                    encoded, originsToSearch, out typeOrigin);
                if (anyNamespaceVersionExists) return true;
            }
            // The sought for type either hasn't been reflected over, or must be prepared
            // for each namespace it appears in.  Search for the specific one and return result.
            return ContainsTypeInternal(typeAndNamespace, encoded, originsToSearch, out typeOrigin);
        }

        private bool ContainsTypeInternal(TypeAndNamespace typeAndNamespace, bool encoded, TypeOrigin originsToSearch, out TypeOrigin typeOrigin)
        {
            Dictionary<TypeAndNamespace, LogicalType> typeTable;
            LogicalType lType;

            lock (_lookUpLock)
            {
                typeTable = encoded ? _userEncodedTypesByType : _userLiteralTypesByType;

                if (0 != (originsToSearch & TypeOrigin.Intrinsic) && s_intrinsicsByType.ContainsKey(typeAndNamespace.Type))
                {
                    lType = s_intrinsicsByType[typeAndNamespace.Type];
                    if (lType != null)
                    {
                        typeOrigin = TypeOrigin.Intrinsic;
                        return true;
                    }
                }

                if (0 != (originsToSearch & TypeOrigin.User) && typeTable.ContainsKey(typeAndNamespace))
                {
                    lType = typeTable[typeAndNamespace];
                    if (lType != null)
                    {
                        typeOrigin = TypeOrigin.User;
                        return true;
                    }
                }

                typeOrigin = TypeOrigin.None;
                return false;
            }
        }

        /// <summary>
        /// Check whether the specified name and namespace is mapped to a LogicalType 
        /// object. Returns true if there is a mapping. Otherwise, false is returned. 
        /// </summary>
        /// <param name="typeOrigin">Specifies whether 
        /// the type that was found is either intrinsic or user defined.
        /// </param>
        public bool ContainsType(XmlQualifiedName qname, bool encoded, out TypeOrigin typeOrigin)
        {
            return ContainsType(qname, encoded, TypeOrigin.All, out typeOrigin);
        }

        /// <summary>
        /// Check whether the specified name and namespace is mapped to a LogicalType 
        /// object. Returns true if there is a mapping. Otherwise, false is returned. 
        /// </summary>
        /// <param name="typeOrigin">Specifies whether 
        /// the type that was found is either intrinsic or user defined.
        /// </param>
        public bool ContainsType(XmlQualifiedName qname, bool encoded, TypeOrigin originsToSearch, out TypeOrigin typeOrigin)
        {
            Dictionary<XmlQualifiedName, LogicalType> nameTable;

            lock (_lookUpLock)
            {
                nameTable = encoded ? _userEncodedTypesByQName : _userLiteralTypesByQName;

                if (0 != (originsToSearch & TypeOrigin.Intrinsic) &&
                    s_intrinsicsByName.ContainsKey(qname))
                {
                    typeOrigin = TypeOrigin.Intrinsic;
                    return true;
                }

                if (0 != (originsToSearch & TypeOrigin.User) && nameTable.ContainsKey(qname))
                {
                    typeOrigin = TypeOrigin.User;
                    return true;
                }

                typeOrigin = TypeOrigin.None;
                return false;
            }
        }

        /// <summary>
        /// Retrieves the LogicalType mapped to the specified Type object. Null is 
        /// returned if no LogicalType is mapped to the specified Type. 
        /// </summary>
        /// <param name="typeOrigin">
        /// The TypeOrigin out parameter specifies whether the type that was found is 
        /// either intrinsic or user defined.
        /// </param>
        public LogicalType GetType(Type type, string ns, bool encoded, out TypeOrigin typeOrigin)
        {
            return GetType(new TypeAndNamespace(type, ns), encoded, TypeOrigin.All, out typeOrigin);
        }

        /// <summary>
        /// Retrieves the LogicalType mapped to the specified Type object. Null is 
        /// returned if no LogicalType is mapped to the specified Type. 
        /// </summary>
        internal static LogicalType GetIntrinsicType(Type type)
        {
            return ContainsIntrinsicType(type) ? s_intrinsicsByType[type] : null;
        }

        /// <summary>
        /// Retrieves the LogicalType mapped to the specified Type object. Null is 
        /// returned if no LogicalType is mapped to the specified Type. 
        /// </summary>
        /// <param name="typeOrigin">
        /// The TypeOrigin out parameter specifies whether the type that was found is 
        /// either intrinsic or user defined.
        /// </param>
        public LogicalType GetType(Type type, string ns, bool encoded, TypeOrigin originsToSearch, out TypeOrigin typeOrigin)
        {
            return GetType(new TypeAndNamespace(type, ns), encoded, originsToSearch, out typeOrigin);
        }

        private LogicalType GetType(TypeAndNamespace typeAndNamespace, bool encoded, TypeOrigin originsToSearch, out TypeOrigin typeOrigin)
        {
            // If a specific namespace is needed, check to see whether the type sought after is
            // a one-namespace-fits-all and has already been reflected.
            if (typeAndNamespace.DefaultNamespace != null)
            { // looking for a specific namespace
                // does a one-size-fits-all exist?
                LogicalType lType = GetTypeInternal(new TypeAndNamespace(typeAndNamespace.Type, null), encoded, originsToSearch, out typeOrigin);
                if (lType != null) return lType;
            }
            // The sought for type either hasn't been reflected over, or must be prepared
            // for each namespace it appears in.  Search for the specific one and return result.
            return GetTypeInternal(typeAndNamespace, encoded, originsToSearch, out typeOrigin);
        }

        private LogicalType GetTypeInternal(TypeAndNamespace typeAndNamespace, bool encoded, TypeOrigin originsToSearch, out TypeOrigin typeOrigin)
        {
            Dictionary<TypeAndNamespace, LogicalType> typeTable;

            lock (_lookUpLock)
            {
                typeTable = encoded ? _userEncodedTypesByType : _userLiteralTypesByType;

                if (0 != (originsToSearch & TypeOrigin.Intrinsic) && s_intrinsicsByType.ContainsKey(typeAndNamespace.Type))
                {
                    typeOrigin = TypeOrigin.Intrinsic;
                    return s_intrinsicsByType[typeAndNamespace.Type];
                }
                else if (0 != (originsToSearch & TypeOrigin.User) && typeTable.ContainsKey(typeAndNamespace))
                {
                    typeOrigin = TypeOrigin.User;
                    return typeTable[typeAndNamespace];
                }
                else
                {
                    typeOrigin = TypeOrigin.None;
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieves the LogicalType mapped to the specified Type object. Null is 
        /// returned if no LogicalType is mapped to the specified Type. 
        /// </summary>
        /// <param name="typeOrigin">
        /// The TypeOrigin out parameter specifies whether the type that was found is 
        /// either intrinsic or user defined.
        /// </param>
        internal LogicalType GetType(XmlQualifiedName qname, bool encoded, out TypeOrigin typeOrigin)
        {
            return GetType(qname, encoded, TypeOrigin.All, out typeOrigin);
        }

        /// <summary>
        /// Retrieves the LogicalType mapped to the specified Type object. Null is 
        /// returned if no LogicalType is mapped to the specified Type. 
        /// </summary>
        /// <param name="typeOrigin">
        /// The TypeOrigin out parameter specifies whether the type that was found is 
        /// either intrinsic or user defined.
        /// </param>
        internal LogicalType GetType(XmlQualifiedName qname, bool encoded, TypeOrigin originsToSearch, out TypeOrigin typeOrigin)
        {
            Dictionary<XmlQualifiedName, LogicalType> typeTable;

            lock (_lookUpLock)
            {
                typeTable = encoded ? _userEncodedTypesByQName : _userLiteralTypesByQName;

                if (0 != (originsToSearch & TypeOrigin.Intrinsic) &&
                    s_intrinsicsByName.ContainsKey(qname))
                {
                    typeOrigin = TypeOrigin.Intrinsic;
                    return s_intrinsicsByName[qname];
                }
                else if (0 != (originsToSearch & TypeOrigin.User) && typeTable.ContainsKey(qname))
                {
                    typeOrigin = TypeOrigin.User;
                    return typeTable[qname];
                }
                else
                {
                    typeOrigin = TypeOrigin.None;
                    return null;
                }
            }
        }


        /// <summary>
        /// Adds a LogicalType to the TypeContainer. The LogicalType is mapped by 
        /// Framework Type, i.e FX Type => LogicalType, as well as by XmlQualifiedName, 
        /// i.e (Name:Namespace) => LogicalType. 
        /// </summary>
        public void AddType(LogicalType lType, bool encoded)
        {
            AddType(lType, lType.Name, lType.Namespace, encoded);
        }

        /// <summary>
        /// Adds a LogicalType to the TypeContainer. The LogicalType is mapped by 
        /// Framework Type, i.e FX Type => LogicalType, as well as by XmlQualifiedName, 
        /// i.e (Name:Namespace) => LogicalType. 
        /// </summary>
        public void AddType(LogicalType lType, string name, string ns, bool encoded)
        {
            Dictionary<TypeAndNamespace, LogicalType> typesByTypeTable;
            Dictionary<XmlQualifiedName, LogicalType> typesByNameTable;
            bool allowTypeMapping, allowNameMapping;
            XmlQualifiedName typeQName;
            TypeAndNamespace typeAndNamespace;
            // Most type should avoid the null namespace, but some types (like XmlNode)
            // really don't have any need for a ns because they are inherently portable
            // by carrying all their namespace info with them.
            Debug.Assert(ns != null || lType.NamespaceIsPortable, "A non-portable type MUST specify a namespace.");

            lock (_lookUpLock)
            {
                if (encoded)
                {
                    typesByTypeTable = _userEncodedTypesByType;
                    typesByNameTable = _userEncodedTypesByQName;
                }
                else
                {
                    typesByTypeTable = _userLiteralTypesByType;
                    typesByNameTable = _userLiteralTypesByQName;
                }

                allowTypeMapping = (lType.MappingFlags & TypeMappingFlags.AllowTypeMapping) != 0;
                allowNameMapping = (lType.MappingFlags & TypeMappingFlags.AllowNameMapping) != 0;
                typeQName = new XmlQualifiedName(name, ns);
                typeAndNamespace = new TypeAndNamespace(lType.Type, lType.NamespaceIsPortable ? null : ns);

                //if we're allowed to map the C# type to the LogicalType
                if (allowTypeMapping)
                {
                    // Don't add the type yet.  Verify that I can also add it to the type table before committing it.
                    if (typesByTypeTable.ContainsKey(typeAndNamespace))
                    {
                        Debug.WriteLine("Adding " + ns + ":" + lType.Type + " twice.");
                        throw new InvalidOperationException(SR.Format(SR.XmlS_TwoMappings_1, lType.Type));
                    }
                    // We should never have both a null namespace for a type in our table and a non-null namespace.
                    // We should EITHER have the type in our table at most once, with the null namespace;
                    //           OR we should have the type in our table any number of times, all with non-null namespaces.
                    Debug.Assert(!(typeAndNamespace.DefaultNamespace != null &&
                        typesByTypeTable.ContainsKey(new TypeAndNamespace(typeAndNamespace.Type, null))));
                }

                //If we're allowed to map the qname to this LogicalType
                if (allowNameMapping)
                {
                    if (typesByNameTable.ContainsKey(typeQName))
                    {
                        Debug.WriteLine("Two types in " + lType.Namespace + ":" + lType.Name);
                        throw new InvalidOperationException(SR.Format(SR.XmlS_TwoMappings_1, typeQName));
                    }
                    typesByNameTable[typeQName] = lType;
                }

                if (allowTypeMapping)
                {
                    typesByTypeTable[typeAndNamespace] = lType;
                }

                if (encoded && !_userEncodedNamespaces.Contains(ns))
                {
                    _userEncodedNamespaces.Add(ns);
                }
            }
        }


        /// <summary>
        /// Retrieves all of the namespaces that are used to map to a LogicalType. These
        /// namespaces only apply to type added using encoded symantics.
        /// </summary>

        [System.Security.FrameworkVisibilityCompactFrameworkInternalAttribute()]
        internal string[] GetEncodedNamespaces()
        {
            lock (_lookUpLock)
            {
                return _userEncodedNamespaces.ToArray();
            }
        }
    }
}
