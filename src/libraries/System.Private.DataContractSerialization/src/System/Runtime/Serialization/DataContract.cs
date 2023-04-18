// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization.DataContracts
{
    public abstract class DataContract
    {
        internal const string SerializerTrimmerWarning = "Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the " +
            "required types are preserved.";
        internal const string SerializerAOTWarning = "Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.";

        internal const DynamicallyAccessedMemberTypes DataContractPreserveMemberTypes =
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods |
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.PublicProperties;

        private readonly XmlDictionaryString _name;
        private readonly XmlDictionaryString _ns;
        private readonly DataContractCriticalHelper _helper;

        internal DataContract(DataContractCriticalHelper helper)
        {
            _helper = helper;
            _name = helper.Name;
            _ns = helper.Namespace;
        }

        public virtual string? ContractType => null;

        internal MethodInfo? ParseMethod => _helper.ParseMethod;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetDataContract(Type type)
        {
            return GetDataContract(type.TypeHandle);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetDataContract(RuntimeTypeHandle typeHandle)
        {
            int id = GetId(typeHandle);
            DataContract dataContract = GetDataContractSkipValidation(id, typeHandle, null);
            return dataContract.GetValidContract();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
        {
            DataContract dataContract = GetDataContractSkipValidation(id, typeHandle, null);
            return dataContract.GetValidContract();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type? type)
        {
            return DataContractCriticalHelper.GetDataContractSkipValidation(id, typeHandle, type);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetGetOnlyCollectionDataContract(int id, RuntimeTypeHandle typeHandle, Type? type)
        {
            DataContract dataContract = GetGetOnlyCollectionDataContractSkipValidation(id, typeHandle, type);
            dataContract = dataContract.GetValidContract();
            if (dataContract is ClassDataContract)
            {
                throw new SerializationException(SR.Format(SR.ErrorDeserializing, SR.Format(SR.ErrorTypeInfo, DataContract.GetClrTypeFullName(dataContract.UnderlyingType)), SR.Format(SR.NoSetMethodForProperty, string.Empty, string.Empty)));
            }
            return dataContract;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract GetGetOnlyCollectionDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type? type)
        {
            return DataContractCriticalHelper.GetGetOnlyCollectionDataContractSkipValidation(id, typeHandle, type);
        }

        internal static DataContract GetDataContractForInitialization(int id)
        {
            return DataContractCriticalHelper.GetDataContractForInitialization(id);
        }

        internal static int GetIdForInitialization(ClassDataContract classContract)
        {
            return DataContractCriticalHelper.GetIdForInitialization(classContract);
        }

        internal static int GetId(RuntimeTypeHandle typeHandle)
        {
            return DataContractCriticalHelper.GetId(typeHandle);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract? GetBuiltInDataContract(Type type)
        {
            return DataContractCriticalHelper.GetBuiltInDataContract(type);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public static DataContract? GetBuiltInDataContract(string name, string ns)
        {
            return DataContractCriticalHelper.GetBuiltInDataContract(name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContract? GetBuiltInDataContract(string typeName)
        {
            return DataContractCriticalHelper.GetBuiltInDataContract(typeName);
        }

        internal static string GetNamespace(string key)
        {
            return DataContractCriticalHelper.GetNamespace(key);
        }

        internal static XmlDictionaryString GetClrTypeString(string key)
        {
            return DataContractCriticalHelper.GetClrTypeString(key);
        }

        [DoesNotReturn]
        internal static void ThrowInvalidDataContractException(string? message, Type? type)
        {
            DataContractCriticalHelper.ThrowInvalidDataContractException(message, type);
        }

        internal DataContractCriticalHelper Helper => _helper;

        [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
        public virtual Type UnderlyingType => _helper.UnderlyingType;

        public virtual Type OriginalUnderlyingType => _helper.OriginalUnderlyingType;

        public virtual bool IsBuiltInDataContract => _helper.IsBuiltInDataContract;

        internal Type TypeForInitialization => _helper.TypeForInitialization;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext? context)
        {
            throw new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, DataContract.GetClrTypeFullName(GetType()), DataContract.GetClrTypeFullName(UnderlyingType)));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual object? ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context)
        {
            throw new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, DataContract.GetClrTypeFullName(GetType()), DataContract.GetClrTypeFullName(UnderlyingType)));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            throw new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, DataContract.GetClrTypeFullName(GetType()), DataContract.GetClrTypeFullName(UnderlyingType)));
        }

        internal virtual object ReadXmlElement(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
        {
            throw new InvalidDataContractException(SR.Format(SR.UnexpectedContractType, DataContract.GetClrTypeFullName(GetType()), DataContract.GetClrTypeFullName(UnderlyingType)));
        }

        public virtual bool IsValueType
        {
            get => _helper.IsValueType;
            internal set => _helper.IsValueType = value;
        }

        public virtual bool IsReference
        {
            get => _helper.IsReference;
            internal set => _helper.IsReference = value;
        }

        public virtual XmlQualifiedName XmlName
        {
            get => _helper.XmlName;
            internal set => _helper.XmlName = value;
        }

        public virtual DataContract? BaseContract
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => null;
        }

        internal GenericInfo? GenericInfo
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => _helper.GenericInfo;
            set => _helper.GenericInfo = value;
        }

        public virtual DataContractDictionary? KnownDataContracts
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => _helper.KnownDataContracts;
            internal set => _helper.KnownDataContracts = value;
        }

        public virtual bool IsISerializable
        {
            get => _helper.IsISerializable;
            internal set => _helper.IsISerializable = value;
        }

        internal XmlDictionaryString Name => _name;

        internal virtual XmlDictionaryString Namespace => _ns;

        internal virtual bool HasRoot
        {
            get => _helper.HasRoot;
            set => _helper.HasRoot = value;
        }

        public virtual XmlDictionaryString? TopLevelElementName
        {
            get => _helper.TopLevelElementName;
            internal set => _helper.TopLevelElementName = value;
        }

        public virtual XmlDictionaryString? TopLevelElementNamespace
        {
            get => _helper.TopLevelElementNamespace;
            internal set => _helper.TopLevelElementNamespace = value;
        }

        internal virtual bool CanContainReferences => true;

        internal virtual bool IsPrimitive => false;

        public virtual bool IsDictionaryLike([NotNullWhen(true)] out string? keyName, [NotNullWhen(true)] out string? valueName, [NotNullWhen(true)] out string? itemName)
        {
            keyName = valueName = itemName = null;
            return false;
        }

        public virtual ReadOnlyCollection<DataMember> DataMembers => ReadOnlyCollection<DataMember>.Empty;

        internal virtual void WriteRootElement(XmlWriterDelegator writer, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            if (object.ReferenceEquals(ns, DictionaryGlobals.SerializationNamespace) && !IsPrimitive)
                writer.WriteStartElement(Globals.SerPrefix, name, ns);
            else
                writer.WriteStartElement(name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract>? boundContracts = null)
        {
            return this;
        }

        internal virtual DataContract GetValidContract(bool verifyConstructor = false)
        {
            return this;
        }

        internal virtual bool IsValidContract()
        {
            return true;
        }

        internal class DataContractCriticalHelper
        {
            private static readonly Hashtable s_typeToIDCache = new Hashtable(new HashTableEqualityComparer());
            private static DataContract[] s_dataContractCache = new DataContract[32];
            private static int s_dataContractID;
            private static Dictionary<Type, DataContract?>? s_typeToBuiltInContract;
            private static Dictionary<XmlQualifiedName, DataContract?>? s_nameToBuiltInContract;
            private static Dictionary<string, DataContract?>? s_typeNameToBuiltInContract;
            private static readonly Hashtable s_namespaces = new Hashtable();
            private static Dictionary<string, XmlDictionaryString>? s_clrTypeStrings;
            private static XmlDictionary? s_clrTypeStringsDictionary;

            [ThreadStatic]
            private static TypeHandleRef? s_typeHandleRef;

            private static readonly object s_cacheLock = new object();
            private static readonly object s_createDataContractLock = new object();
            private static readonly object s_initBuiltInContractsLock = new object();
            private static readonly object s_namespacesLock = new object();
            private static readonly object s_clrTypeStringsLock = new object();

            [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
            private readonly Type _underlyingType;
            private Type? _originalUnderlyingType;
            private bool _isValueType;
            private GenericInfo? _genericInfo;
            private XmlQualifiedName _xmlName = null!; // XmlName is always set in concrete ctors set except for the "invalid" CollectionDataContract
            private XmlDictionaryString _name = null!; // Name is always set in concrete ctors set except for the "invalid" CollectionDataContract
            private XmlDictionaryString _ns = null!; // Namespace is always set in concrete ctors set except for the "invalid" CollectionDataContract

            private MethodInfo? _parseMethod;
            private bool _parseMethodSet;

            /// <SecurityNote>
            /// Critical - in deserialization, we initialize an object instance passing this Type to GetUninitializedObject method
            /// </SecurityNote>
            private Type _typeForInitialization;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal static DataContract GetDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type? type)
            {
                DataContract dataContract = s_dataContractCache[id];
                if (dataContract == null)
                {
                    dataContract = CreateDataContract(id, typeHandle, type);
                }
                else
                {
                    return dataContract.GetValidContract(verifyConstructor: true);
                }
                return dataContract;
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal static DataContract GetGetOnlyCollectionDataContractSkipValidation(int id, RuntimeTypeHandle typeHandle, Type? type)
            {
                DataContract dataContract = s_dataContractCache[id] ?? CreateGetOnlyCollectionDataContract(id, typeHandle, type);
                return dataContract;
            }

            internal static DataContract GetDataContractForInitialization(int id)
            {
                DataContract dataContract = s_dataContractCache[id];
                if (dataContract == null)
                {
                    throw new SerializationException(SR.DataContractCacheOverflow);
                }
                return dataContract;
            }

            internal static int GetIdForInitialization(ClassDataContract classContract)
            {
                int id = DataContract.GetId(classContract.TypeForInitialization.TypeHandle);
                if (id < s_dataContractCache.Length && ContractMatches(classContract, s_dataContractCache[id]))
                {
                    return id;
                }
                int currentDataContractId = DataContractCriticalHelper.s_dataContractID;
                for (int i = 0; i < currentDataContractId; i++)
                {
                    if (ContractMatches(classContract, s_dataContractCache[i]))
                    {
                        return i;
                    }
                }
                throw new SerializationException(SR.DataContractCacheOverflow);
            }

            private static bool ContractMatches(DataContract contract, DataContract cachedContract)
            {
                return (cachedContract != null && cachedContract.UnderlyingType == contract.UnderlyingType);
            }

            internal static int GetId(RuntimeTypeHandle typeHandle)
            {
                typeHandle = GetDataContractAdapterTypeHandle(typeHandle);
                s_typeHandleRef ??= new TypeHandleRef();
                s_typeHandleRef.Value = typeHandle;

                object? value = s_typeToIDCache[s_typeHandleRef];
                if (value != null)
                    return ((IntRef)value).Value;

                try
                {
                    lock (s_cacheLock)
                    {
                        value = s_typeToIDCache[s_typeHandleRef];
                        if (value != null)
                            return ((IntRef)value).Value;

                        int nextId = s_dataContractID++;
                        if (nextId >= s_dataContractCache.Length)
                        {
                            int newSize = (nextId < int.MaxValue / 2) ? nextId * 2 : int.MaxValue;
                            if (newSize <= nextId)
                            {
                                Debug.Fail("DataContract cache overflow");
                                throw new SerializationException(SR.DataContractCacheOverflow);
                            }
                            Array.Resize<DataContract>(ref s_dataContractCache, newSize);
                        }
                        IntRef id = new IntRef(nextId);

                        s_typeToIDCache.Add(new TypeHandleRef(typeHandle), id);
                        return id.Value;
                    }
                }
                catch (Exception ex) when (!ExceptionUtility.IsFatal(ex))
                {
                    throw new Exception(ex.Message, ex);
                }
            }

            // check whether a corresponding update is required in ClassDataContract.IsNonAttributedTypeValidForSerialization
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private static DataContract CreateDataContract(int id, RuntimeTypeHandle typeHandle, Type? type)
            {
                DataContract? dataContract = s_dataContractCache[id];
                if (dataContract == null)
                {
                    lock (s_createDataContractLock)
                    {
                        dataContract = s_dataContractCache[id];
                        if (dataContract == null)
                        {
                            type ??= Type.GetTypeFromHandle(typeHandle)!;
                            dataContract = CreateDataContract(type);
                            AssignDataContractToId(dataContract, id);
                        }
                    }
                }

                return dataContract;
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private static DataContract CreateDataContract(Type type)
            {
                type = UnwrapNullableType(type);
                Type originalType = type;
                type = GetDataContractAdapterType(type);

                DataContract? dataContract = GetBuiltInDataContract(type);
                if (dataContract == null)
                {
                    if (type.IsArray)
                        dataContract = new CollectionDataContract(type);
                    else if (type.IsEnum)
                        dataContract = new EnumDataContract(type);
                    else if (type.IsGenericParameter)
                        dataContract = new GenericParameterDataContract(type);
                    else if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
                        dataContract = new XmlDataContract(type);
                    else
                    {
                        if (type.IsPointer)
                            type = Globals.TypeOfReflectionPointer;

                        if (!CollectionDataContract.TryCreate(type, out dataContract))
                        {
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
                            if (!type.IsSerializable && !type.IsDefined(Globals.TypeOfDataContractAttribute, false) && !ClassDataContract.IsNonAttributedTypeValidForSerialization(type))
                            {
                                ThrowInvalidDataContractException(SR.Format(SR.TypeNotSerializable, type), type);
                            }
#pragma warning restore SYSLIB0050
                            dataContract = new ClassDataContract(type);
                            if (type != originalType)
                            {
                                var originalDataContract = new ClassDataContract(originalType);
                                if (dataContract.XmlName != originalDataContract.XmlName)
                                {
                                    // for non-DC types, type adapters will not have the same xml name (contract name).
                                    dataContract.XmlName = originalDataContract.XmlName;
                                }
                            }
                        }
                    }
                }

                return dataContract;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void AssignDataContractToId(DataContract dataContract, int id)
            {
                lock (s_cacheLock)
                {
                    s_dataContractCache[id] = dataContract;
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private static DataContract CreateGetOnlyCollectionDataContract(int id, RuntimeTypeHandle typeHandle, Type? type)
            {
                DataContract? dataContract = null;
                lock (s_createDataContractLock)
                {
                    dataContract = s_dataContractCache[id];
                    if (dataContract == null)
                    {
                        type ??= Type.GetTypeFromHandle(typeHandle)!;
                        type = UnwrapNullableType(type);
                        type = GetDataContractAdapterType(type);
                        if (!CollectionDataContract.TryCreateGetOnlyCollectionDataContract(type, out dataContract))
                        {
                            ThrowInvalidDataContractException(SR.Format(SR.TypeNotSerializable, type), type);
                        }
                        AssignDataContractToId(dataContract, id);
                    }
                }
                // !;   // If null after the lookup and creation attempts above, the 'ThrowInvalidDataContractException' kicks in.
                return dataContract;
            }

            // This method returns adapter types used at runtime to create DataContract.
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal static Type GetDataContractAdapterType(Type type)
            {
                // Replace the DataTimeOffset ISerializable type passed in with the internal DateTimeOffsetAdapter DataContract type.
                // DateTimeOffsetAdapter is used for serialization/deserialization purposes to bypass the ISerializable implementation
                // on DateTimeOffset; which does not work in partial trust and to ensure correct schema import/export scenarios.
                if (type == Globals.TypeOfDateTimeOffset)
                {
                    return Globals.TypeOfDateTimeOffsetAdapter;
                }
                if (type == Globals.TypeOfMemoryStream)
                {
                    return Globals.TypeOfMemoryStreamAdapter;
                }
                return type;
            }

            // Maps adapted types back to the original type
            // Any change to this method should be reflected in GetDataContractAdapterType
            internal static Type GetDataContractOriginalType(Type type)
            {
                if (type == Globals.TypeOfDateTimeOffsetAdapter)
                {
                    return Globals.TypeOfDateTimeOffset;
                }
                if (type == Globals.TypeOfMemoryStreamAdapter)
                {
                    return Globals.TypeOfMemoryStream;
                }
                return type;
            }
            private static RuntimeTypeHandle GetDataContractAdapterTypeHandle(RuntimeTypeHandle typeHandle)
            {
                if (Globals.TypeOfDateTimeOffset.TypeHandle.Equals(typeHandle))
                {
                    return Globals.TypeOfDateTimeOffsetAdapter.TypeHandle;
                }
                if (Globals.TypeOfMemoryStream.TypeHandle.Equals(typeHandle))
                {
                    return Globals.TypeOfMemoryStreamAdapter.TypeHandle;
                }
                return typeHandle;
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal static DataContract? GetBuiltInDataContract(Type type)
            {
                if (type.IsInterface && !CollectionDataContract.IsCollectionInterface(type))
                    type = Globals.TypeOfObject;

                lock (s_initBuiltInContractsLock)
                {
                    s_typeToBuiltInContract ??= new Dictionary<Type, DataContract?>();

                    if (!s_typeToBuiltInContract.TryGetValue(type, out DataContract? dataContract))
                    {
                        TryCreateBuiltInDataContract(type, out dataContract);
                        s_typeToBuiltInContract.Add(type, dataContract);
                    }
                    return dataContract;
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal static DataContract? GetBuiltInDataContract(string name, string ns)
            {
                lock (s_initBuiltInContractsLock)
                {
                    s_nameToBuiltInContract ??= new Dictionary<XmlQualifiedName, DataContract?>();

                    XmlQualifiedName qname = new XmlQualifiedName(name, ns);
                    if (!s_nameToBuiltInContract.TryGetValue(qname, out DataContract? dataContract))
                    {
                        if (TryCreateBuiltInDataContract(name, ns, out dataContract))
                        {
                            s_nameToBuiltInContract.Add(qname, dataContract);
                        }
                    }
                    return dataContract;
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal static DataContract? GetBuiltInDataContract(string typeName)
            {
                if (!typeName.StartsWith("System.", StringComparison.Ordinal))
                    return null;

                lock (s_initBuiltInContractsLock)
                {
                    s_typeNameToBuiltInContract ??= new Dictionary<string, DataContract?>();

                    if (!s_typeNameToBuiltInContract.TryGetValue(typeName, out DataContract? dataContract))
                    {
                        Type? type = null;
                        string name = typeName.Substring(7);
                        if (name == "Char")
                            type = typeof(char);
                        else if (name == "Boolean")
                            type = typeof(bool);
                        else if (name == "SByte")
                            type = typeof(sbyte);
                        else if (name == "Byte")
                            type = typeof(byte);
                        else if (name == "Int16")
                            type = typeof(short);
                        else if (name == "UInt16")
                            type = typeof(ushort);
                        else if (name == "Int32")
                            type = typeof(int);
                        else if (name == "UInt32")
                            type = typeof(uint);
                        else if (name == "Int64")
                            type = typeof(long);
                        else if (name == "UInt64")
                            type = typeof(ulong);
                        else if (name == "Single")
                            type = typeof(float);
                        else if (name == "Double")
                            type = typeof(double);
                        else if (name == "Decimal")
                            type = typeof(decimal);
                        else if (name == "DateTime")
                            type = typeof(DateTime);
                        else if (name == "String")
                            type = typeof(string);
                        else if (name == "Byte[]")
                            type = typeof(byte[]);
                        else if (name == "Object")
                            type = typeof(object);
                        else if (name == "TimeSpan")
                            type = typeof(TimeSpan);
                        else if (name == "Guid")
                            type = typeof(Guid);
                        else if (name == "Uri")
                            type = typeof(Uri);
                        else if (name == "Xml.XmlQualifiedName")
                            type = typeof(XmlQualifiedName);
                        else if (name == "Enum")
                            type = typeof(Enum);
                        else if (name == "ValueType")
                            type = typeof(ValueType);
                        else if (name == "Array")
                            type = typeof(Array);
                        else if (name == "Xml.XmlElement")
                            type = typeof(XmlElement);
                        else if (name == "Xml.XmlNode[]")
                            type = typeof(XmlNode[]);

                        if (type != null)
                            TryCreateBuiltInDataContract(type, out dataContract);

                        s_typeNameToBuiltInContract.Add(typeName, dataContract);
                    }
                    return dataContract;
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal static bool TryCreateBuiltInDataContract(Type type, [NotNullWhen(true)] out DataContract? dataContract)
            {
                if (type.IsEnum) // Type.GetTypeCode will report Enums as TypeCode.IntXX
                {
                    dataContract = null;
                    return false;
                }
                dataContract = null;
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        dataContract = new BooleanDataContract();
                        break;
                    case TypeCode.Byte:
                        dataContract = new UnsignedByteDataContract();
                        break;
                    case TypeCode.Char:
                        dataContract = new CharDataContract();
                        break;
                    case TypeCode.DateTime:
                        dataContract = new DateTimeDataContract();
                        break;
                    case TypeCode.Decimal:
                        dataContract = new DecimalDataContract();
                        break;
                    case TypeCode.Double:
                        dataContract = new DoubleDataContract();
                        break;
                    case TypeCode.Int16:
                        dataContract = new ShortDataContract();
                        break;
                    case TypeCode.Int32:
                        dataContract = new IntDataContract();
                        break;
                    case TypeCode.Int64:
                        dataContract = new LongDataContract();
                        break;
                    case TypeCode.SByte:
                        dataContract = new SignedByteDataContract();
                        break;
                    case TypeCode.Single:
                        dataContract = new FloatDataContract();
                        break;
                    case TypeCode.String:
                        dataContract = new StringDataContract();
                        break;
                    case TypeCode.UInt16:
                        dataContract = new UnsignedShortDataContract();
                        break;
                    case TypeCode.UInt32:
                        dataContract = new UnsignedIntDataContract();
                        break;
                    case TypeCode.UInt64:
                        dataContract = new UnsignedLongDataContract();
                        break;
                    default:
                        if (type == typeof(byte[]))
                            dataContract = new ByteArrayDataContract();
                        else if (type == typeof(object))
                            dataContract = new ObjectDataContract();
                        else if (type == typeof(Uri))
                            dataContract = new UriDataContract();
                        else if (type == typeof(XmlQualifiedName))
                            dataContract = new QNameDataContract();
                        else if (type == typeof(TimeSpan))
                            dataContract = new TimeSpanDataContract();
                        else if (type == typeof(Guid))
                            dataContract = new GuidDataContract();
                        else if (type == typeof(Enum) || type == typeof(ValueType))
                        {
                            dataContract = new SpecialTypeDataContract(type, DictionaryGlobals.ObjectLocalName, DictionaryGlobals.SchemaNamespace);
                        }
                        else if (type == typeof(Array))
                            dataContract = new CollectionDataContract(type);
                        else if (type == typeof(XmlElement) || type == typeof(XmlNode[]))
                            dataContract = new XmlDataContract(type);
                        break;
                }
                return dataContract != null;
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal static bool TryCreateBuiltInDataContract(string name, string ns, [NotNullWhen(true)] out DataContract? dataContract)
            {
                dataContract = null;
                if (ns == DictionaryGlobals.SchemaNamespace.Value)
                {
                    if (DictionaryGlobals.BooleanLocalName.Value == name)
                        dataContract = new BooleanDataContract();
                    else if (DictionaryGlobals.SignedByteLocalName.Value == name)
                        dataContract = new SignedByteDataContract();
                    else if (DictionaryGlobals.UnsignedByteLocalName.Value == name)
                        dataContract = new UnsignedByteDataContract();
                    else if (DictionaryGlobals.ShortLocalName.Value == name)
                        dataContract = new ShortDataContract();
                    else if (DictionaryGlobals.UnsignedShortLocalName.Value == name)
                        dataContract = new UnsignedShortDataContract();
                    else if (DictionaryGlobals.IntLocalName.Value == name)
                        dataContract = new IntDataContract();
                    else if (DictionaryGlobals.UnsignedIntLocalName.Value == name)
                        dataContract = new UnsignedIntDataContract();
                    else if (DictionaryGlobals.LongLocalName.Value == name)
                        dataContract = new LongDataContract();
                    else if (DictionaryGlobals.integerLocalName.Value == name)
                        dataContract = new IntegerDataContract();
                    else if (DictionaryGlobals.positiveIntegerLocalName.Value == name)
                        dataContract = new PositiveIntegerDataContract();
                    else if (DictionaryGlobals.negativeIntegerLocalName.Value == name)
                        dataContract = new NegativeIntegerDataContract();
                    else if (DictionaryGlobals.nonPositiveIntegerLocalName.Value == name)
                        dataContract = new NonPositiveIntegerDataContract();
                    else if (DictionaryGlobals.nonNegativeIntegerLocalName.Value == name)
                        dataContract = new NonNegativeIntegerDataContract();
                    else if (DictionaryGlobals.UnsignedLongLocalName.Value == name)
                        dataContract = new UnsignedLongDataContract();
                    else if (DictionaryGlobals.FloatLocalName.Value == name)
                        dataContract = new FloatDataContract();
                    else if (DictionaryGlobals.DoubleLocalName.Value == name)
                        dataContract = new DoubleDataContract();
                    else if (DictionaryGlobals.DecimalLocalName.Value == name)
                        dataContract = new DecimalDataContract();
                    else if (DictionaryGlobals.DateTimeLocalName.Value == name)
                        dataContract = new DateTimeDataContract();
                    else if (DictionaryGlobals.StringLocalName.Value == name)
                        dataContract = new StringDataContract();
                    else if (DictionaryGlobals.timeLocalName.Value == name)
                        dataContract = new TimeDataContract();
                    else if (DictionaryGlobals.dateLocalName.Value == name)
                        dataContract = new DateDataContract();
                    else if (DictionaryGlobals.hexBinaryLocalName.Value == name)
                        dataContract = new HexBinaryDataContract();
                    else if (DictionaryGlobals.gYearMonthLocalName.Value == name)
                        dataContract = new GYearMonthDataContract();
                    else if (DictionaryGlobals.gYearLocalName.Value == name)
                        dataContract = new GYearDataContract();
                    else if (DictionaryGlobals.gMonthDayLocalName.Value == name)
                        dataContract = new GMonthDayDataContract();
                    else if (DictionaryGlobals.gDayLocalName.Value == name)
                        dataContract = new GDayDataContract();
                    else if (DictionaryGlobals.gMonthLocalName.Value == name)
                        dataContract = new GMonthDataContract();
                    else if (DictionaryGlobals.normalizedStringLocalName.Value == name)
                        dataContract = new NormalizedStringDataContract();
                    else if (DictionaryGlobals.tokenLocalName.Value == name)
                        dataContract = new TokenDataContract();
                    else if (DictionaryGlobals.languageLocalName.Value == name)
                        dataContract = new LanguageDataContract();
                    else if (DictionaryGlobals.NameLocalName.Value == name)
                        dataContract = new NameDataContract();
                    else if (DictionaryGlobals.NCNameLocalName.Value == name)
                        dataContract = new NCNameDataContract();
                    else if (DictionaryGlobals.XSDIDLocalName.Value == name)
                        dataContract = new IDDataContract();
                    else if (DictionaryGlobals.IDREFLocalName.Value == name)
                        dataContract = new IDREFDataContract();
                    else if (DictionaryGlobals.IDREFSLocalName.Value == name)
                        dataContract = new IDREFSDataContract();
                    else if (DictionaryGlobals.ENTITYLocalName.Value == name)
                        dataContract = new ENTITYDataContract();
                    else if (DictionaryGlobals.ENTITIESLocalName.Value == name)
                        dataContract = new ENTITIESDataContract();
                    else if (DictionaryGlobals.NMTOKENLocalName.Value == name)
                        dataContract = new NMTOKENDataContract();
                    else if (DictionaryGlobals.NMTOKENSLocalName.Value == name)
                        dataContract = new NMTOKENDataContract();
                    else if (DictionaryGlobals.ByteArrayLocalName.Value == name)
                        dataContract = new ByteArrayDataContract();
                    else if (DictionaryGlobals.ObjectLocalName.Value == name)
                        dataContract = new ObjectDataContract();
                    else if (DictionaryGlobals.TimeSpanLocalName.Value == name)
                        dataContract = new XsDurationDataContract();
                    else if (DictionaryGlobals.UriLocalName.Value == name)
                        dataContract = new UriDataContract();
                    else if (DictionaryGlobals.QNameLocalName.Value == name)
                        dataContract = new QNameDataContract();
                }
                else if (ns == DictionaryGlobals.SerializationNamespace.Value)
                {
                    if (DictionaryGlobals.TimeSpanLocalName.Value == name)
                        dataContract = new TimeSpanDataContract();
                    else if (DictionaryGlobals.GuidLocalName.Value == name)
                        dataContract = new GuidDataContract();
                    else if (DictionaryGlobals.CharLocalName.Value == name)
                        dataContract = new CharDataContract();
                    else if ("ArrayOfanyType" == name)
                        dataContract = new CollectionDataContract(typeof(Array));
                }
                else if (ns == DictionaryGlobals.AsmxTypesNamespace.Value)
                {
                    if (DictionaryGlobals.CharLocalName.Value == name)
                        dataContract = new AsmxCharDataContract();
                    else if (DictionaryGlobals.GuidLocalName.Value == name)
                        dataContract = new AsmxGuidDataContract();
                }
                else if (ns == Globals.DataContractXmlNamespace)
                {
                    if (name == "XmlElement")
                        dataContract = new XmlDataContract(typeof(XmlElement));
                    else if (name == "ArrayOfXmlNode")
                        dataContract = new XmlDataContract(typeof(XmlNode[]));
                }
                return dataContract != null;
            }

            internal static string GetNamespace(string key)
            {
                object? value = s_namespaces[key];

                if (value != null)
                    return (string)value;

                try
                {
                    lock (s_namespacesLock)
                    {
                        value = s_namespaces[key];

                        if (value != null)
                            return (string)value;

                        s_namespaces.Add(key, key);
                        return key;
                    }
                }
                catch (Exception ex) when (!ExceptionUtility.IsFatal(ex))
                {
                    throw new Exception(ex.Message, ex);
                }
            }

            internal static XmlDictionaryString GetClrTypeString(string key)
            {
                lock (s_clrTypeStringsLock)
                {
                    if (s_clrTypeStrings == null)
                    {
                        s_clrTypeStringsDictionary = new XmlDictionary();
                        s_clrTypeStrings = new Dictionary<string, XmlDictionaryString>();
                        try
                        {
                            s_clrTypeStrings.Add(Globals.TypeOfInt.Assembly.FullName!, s_clrTypeStringsDictionary.Add(Globals.MscorlibAssemblyName));
                        }
                        catch (Exception ex) when (!ExceptionUtility.IsFatal(ex))
                        {
                            throw new Exception(ex.Message, ex);
                        }
                    }
                    if (s_clrTypeStrings.TryGetValue(key, out XmlDictionaryString? value))
                        return value;
                    value = s_clrTypeStringsDictionary!.Add(key);
                    try
                    {
                        s_clrTypeStrings.Add(key, value);
                    }
                    catch (Exception ex) when (!ExceptionUtility.IsFatal(ex))
                    {
                        throw new Exception(ex.Message, ex);
                    }
                    return value;
                }
            }

            [DoesNotReturn]
            internal static void ThrowInvalidDataContractException(string? message, Type? type)
            {
                if (type != null)
                {
                    lock (s_cacheLock)
                    {
                        s_typeHandleRef ??= new TypeHandleRef();
                        s_typeHandleRef.Value = GetDataContractAdapterTypeHandle(type.TypeHandle);

                        if (s_typeToIDCache.ContainsKey(s_typeHandleRef))
                        {
                            lock (s_cacheLock)
                            {
                                s_typeToIDCache.Remove(s_typeHandleRef);
                            }
                        }
                    }
                }

                throw new InvalidDataContractException(message);
            }

            internal DataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type)
            {
                _underlyingType = type;
                SetTypeForInitialization(type);
                _isValueType = type.IsValueType;
            }

            [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
            internal Type UnderlyingType => _underlyingType;

            internal Type OriginalUnderlyingType => _originalUnderlyingType ??= GetDataContractOriginalType(_underlyingType);

            internal virtual bool IsBuiltInDataContract => false;

            internal Type TypeForInitialization => _typeForInitialization;

            [MemberNotNull(nameof(_typeForInitialization))]
            private void SetTypeForInitialization(Type classType)
            {
                // TODO - This 'if' was not commented out in 4.8. But 4.8 was not dealing with nullable notations, which we do have here in Core.
                // With the absence of schema importing, it does not make sense to have a data contract without a valid serializable underlying type. (Even
                // with schema importing it doesn't make sense, but there is a building period while we're still figuring out all the data types and contracts
                // where the underlying type may be null.) Anyway... might it make sense to re-instate this if clause - but use it to throw an exception if
                // we don't meet the criteria? That way we can maintain nullable semantics and not do anything silly trying to keep them simple.
                //if (classType.IsSerializable || classType.IsDefined(Globals.TypeOfDataContractAttribute, false))
                {
                    _typeForInitialization = classType;
                }
            }

            internal bool IsReference { get; set; }

            internal bool IsValueType
            {
                get => _isValueType;
                set => _isValueType = value;
            }

            internal XmlQualifiedName XmlName
            {
                get => _xmlName;
                set => _xmlName = value;
            }

            internal GenericInfo? GenericInfo
            {
                get => _genericInfo;
                set => _genericInfo = value;
            }

            internal virtual DataContractDictionary? KnownDataContracts
            {
                [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
                get => null;
                set { /* do nothing */ }
            }

            internal virtual bool IsISerializable
            {
                get => false;
                set => ThrowInvalidDataContractException(SR.RequiresClassDataContractToSetIsISerializable);
            }

            internal XmlDictionaryString Name
            {
                get => _name;
                set => _name = value;
            }

            internal XmlDictionaryString Namespace
            {
                get => _ns;
                set => _ns = value;
            }

            internal virtual bool HasRoot { get; set; } = true;

            internal virtual XmlDictionaryString? TopLevelElementName
            {
                get => _name;
                set
                {
                    Debug.Assert(value != null);
                    _name = value;
                }
            }

            internal virtual XmlDictionaryString? TopLevelElementNamespace
            {
                get => _ns;
                set
                {
                    Debug.Assert(value != null);
                    _ns = value;
                }
            }

            internal virtual bool CanContainReferences => true;

            internal virtual bool IsPrimitive => false;

            internal MethodInfo? ParseMethod
            {
                get
                {
                    if (!_parseMethodSet)
                    {
                        MethodInfo? method = UnderlyingType.GetMethod(Globals.ParseMethodName, BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(string) });

                        if (method != null && method.ReturnType == UnderlyingType)
                        {
                            _parseMethod = method;
                        }

                        _parseMethodSet = true;
                    }
                    return _parseMethod;
                }
            }

            internal void SetDataContractName(XmlQualifiedName xmlName)
            {
                XmlDictionary dictionary = new XmlDictionary(2);
                Name = dictionary.Add(xmlName.Name);
                Namespace = dictionary.Add(xmlName.Namespace);
                XmlName = xmlName;
            }

            internal void SetDataContractName(XmlDictionaryString name, XmlDictionaryString ns)
            {
                Name = name;
                Namespace = ns;
                XmlName = CreateQualifiedName(name.Value, ns.Value);
            }

            [DoesNotReturn]
            internal void ThrowInvalidDataContractException(string message)
            {
                ThrowInvalidDataContractException(message, UnderlyingType);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool IsTypeSerializable(Type type)
        {
            return IsTypeSerializable(type, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool IsTypeSerializable(Type type, HashSet<Type>? previousCollectionTypes)
        {
            if (
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
                type.IsSerializable ||
#pragma warning restore SYSLIB0050
                type.IsEnum ||
                type.IsDefined(Globals.TypeOfDataContractAttribute, false) ||
                type.IsInterface ||
                type.IsPointer ||
                //Special casing DBNull as its considered a Primitive but is no longer Serializable
                type == Globals.TypeOfDBNull ||
                Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
            {
                return true;
            }
            if (CollectionDataContract.IsCollection(type, out Type? itemType))
            {
                previousCollectionTypes ??= new HashSet<Type>();
                ValidatePreviousCollectionTypes(type, itemType, previousCollectionTypes);
                if (IsTypeSerializable(itemType, previousCollectionTypes))
                {
                    return true;
                }
            }
            return DataContract.GetBuiltInDataContract(type) != null ||
                   ClassDataContract.IsNonAttributedTypeValidForSerialization(type);
        }

        private static void ValidatePreviousCollectionTypes(Type collectionType, Type itemType, HashSet<Type> previousCollectionTypes)
        {
            previousCollectionTypes.Add(collectionType);
            while (itemType.IsArray)
            {
                itemType = itemType.GetElementType()!;
            }

            // Do a breadth first traversal of the generic type tree to
            // produce the closure of all generic argument types and
            // check that none of these is in the previousCollectionTypes
            List<Type> itemTypeClosure = new List<Type>();
            Queue<Type> itemTypeQueue = new Queue<Type>();

            itemTypeQueue.Enqueue(itemType);
            itemTypeClosure.Add(itemType);

            while (itemTypeQueue.Count > 0)
            {
                itemType = itemTypeQueue.Dequeue();
                if (previousCollectionTypes.Contains(itemType))
                {
                    throw new InvalidDataContractException(SR.Format(SR.RecursiveCollectionType, GetClrTypeFullName(itemType)));
                }
                if (itemType.IsGenericType)
                {
                    foreach (Type argType in itemType.GetGenericArguments())
                    {
                        if (!itemTypeClosure.Contains(argType))
                        {
                            itemTypeQueue.Enqueue(argType);
                            itemTypeClosure.Add(argType);
                        }
                    }
                }
            }
        }

        internal static Type UnwrapRedundantNullableType(Type type)
        {
            Type nullableType = type;
            while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
            {
                nullableType = type;
                type = type.GetGenericArguments()[0];
            }
            return nullableType;
        }

        internal static Type UnwrapNullableType(Type type)
        {
            while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
                type = type.GetGenericArguments()[0];
            return type;
        }

        private static bool IsAsciiLocalName(string localName)
        {
            if (localName.Length == 0)
                return false;
            if (!char.IsAsciiLetter(localName[0]))
                return false;
            for (int i = 1; i < localName.Length; i++)
            {
                char ch = localName[i];
                if (!char.IsAsciiLetterOrDigit(ch))
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public static XmlQualifiedName GetXmlName(Type type)
        {
            return GetXmlName(type, out _);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static XmlQualifiedName GetXmlName(Type type, out bool hasDataContract)
        {
            return GetXmlName(type, new HashSet<Type>(), out hasDataContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static XmlQualifiedName GetXmlName(Type type, HashSet<Type> previousCollectionTypes, out bool hasDataContract)
        {
            type = UnwrapRedundantNullableType(type);
            if (TryGetBuiltInXmlAndArrayTypeXmlName(type, previousCollectionTypes, out XmlQualifiedName? xmlName))
            {
                hasDataContract = false;
            }
            else
            {
                if (TryGetDCAttribute(type, out DataContractAttribute? dataContractAttribute))
                {
                    xmlName = GetDCTypeXmlName(type, dataContractAttribute);
                    hasDataContract = true;
                }
                else
                {
                    xmlName = GetNonDCTypeXmlName(type, previousCollectionTypes);
                    hasDataContract = false;
                }
            }

            return xmlName;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static XmlQualifiedName GetDCTypeXmlName(Type type, DataContractAttribute dataContractAttribute)
        {
            string? name, ns;
            if (dataContractAttribute.IsNameSetExplicitly)
            {
                name = dataContractAttribute.Name;
                if (name == null || name.Length == 0)
                    throw new InvalidDataContractException(SR.Format(SR.InvalidDataContractName, DataContract.GetClrTypeFullName(type)));
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                    name = ExpandGenericParameters(name, type);
                name = DataContract.EncodeLocalName(name);
            }
            else
                name = GetDefaultXmlLocalName(type);

            if (dataContractAttribute.IsNamespaceSetExplicitly)
            {
                ns = dataContractAttribute.Namespace;
                if (ns == null)
                    throw new InvalidDataContractException(SR.Format(SR.InvalidDataContractNamespace, DataContract.GetClrTypeFullName(type)));
                CheckExplicitDataContractNamespaceUri(ns, type);
            }
            else
                ns = GetDefaultDataContractNamespace(type);

            return CreateQualifiedName(name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static XmlQualifiedName GetNonDCTypeXmlName(Type type, HashSet<Type> previousCollectionTypes)
        {
            string? name, ns;

            if (CollectionDataContract.IsCollection(type, out Type? itemType))
            {
                ValidatePreviousCollectionTypes(type, itemType, previousCollectionTypes);
                return GetCollectionXmlName(type, itemType, previousCollectionTypes, out _);
            }
            name = GetDefaultXmlLocalName(type);

            // ensures that ContractNamespaceAttribute is honored when used with non-attributed types
            if (ClassDataContract.IsNonAttributedTypeValidForSerialization(type))
            {
                ns = GetDefaultDataContractNamespace(type);
            }
            else
            {
                ns = GetDefaultXmlNamespace(type);
            }
            return CreateQualifiedName(name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool TryGetBuiltInXmlAndArrayTypeXmlName(Type type, HashSet<Type> previousCollectionTypes, [NotNullWhen(true)] out XmlQualifiedName? xmlName)
        {
            xmlName = null;

            DataContract? builtInContract = GetBuiltInDataContract(type);
            if (builtInContract != null)
            {
                xmlName = builtInContract.XmlName;
            }
            else if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
            {
                SchemaExporter.GetXmlTypeInfo(type, out XmlQualifiedName xmlTypeName, out _, out _);
                xmlName = xmlTypeName;
            }
            else if (type.IsArray)
            {
                Type itemType = type.GetElementType()!;
                ValidatePreviousCollectionTypes(type, itemType, previousCollectionTypes);
                xmlName = GetCollectionXmlName(type, itemType, previousCollectionTypes, out _);
            }
            return xmlName != null;
        }

        internal static bool TryGetDCAttribute(Type type, [NotNullWhen(true)] out DataContractAttribute? dataContractAttribute)
        {
            dataContractAttribute = null;

            object[] dataContractAttributes = type.GetCustomAttributes(Globals.TypeOfDataContractAttribute, false).ToArray();
            if (dataContractAttributes != null && dataContractAttributes.Length > 0)
            {
#if DEBUG
                if (dataContractAttributes.Length > 1)
                    throw new InvalidDataContractException(SR.Format(SR.TooManyDataContracts, DataContract.GetClrTypeFullName(type)));
#endif
                dataContractAttribute = (DataContractAttribute)dataContractAttributes[0];
            }

            return dataContractAttribute != null;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static XmlQualifiedName GetCollectionXmlName(Type type, Type itemType, out CollectionDataContractAttribute? collectionContractAttribute)
        {
            return GetCollectionXmlName(type, itemType, new HashSet<Type>(), out collectionContractAttribute);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static XmlQualifiedName GetCollectionXmlName(Type type, Type itemType, HashSet<Type> previousCollectionTypes, out CollectionDataContractAttribute? collectionContractAttribute)
        {
            string? name, ns;
            object[] collectionContractAttributes = type.GetCustomAttributes(Globals.TypeOfCollectionDataContractAttribute, false).ToArray();
            if (collectionContractAttributes != null && collectionContractAttributes.Length > 0)
            {
#if DEBUG
                if (collectionContractAttributes.Length > 1)
                    throw new InvalidDataContractException(SR.Format(SR.TooManyCollectionContracts, DataContract.GetClrTypeFullName(type)));
#endif
                collectionContractAttribute = (CollectionDataContractAttribute)collectionContractAttributes[0];
                if (collectionContractAttribute.IsNameSetExplicitly)
                {
                    name = collectionContractAttribute.Name;
                    if (name == null || name.Length == 0)
                        throw new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractName, DataContract.GetClrTypeFullName(type)));
                    if (type.IsGenericType && !type.IsGenericTypeDefinition)
                        name = ExpandGenericParameters(name, type);
                    name = DataContract.EncodeLocalName(name);
                }
                else
                    name = GetDefaultXmlLocalName(type);

                if (collectionContractAttribute.IsNamespaceSetExplicitly)
                {
                    ns = collectionContractAttribute.Namespace;
                    if (ns == null)
                        throw new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractNamespace, DataContract.GetClrTypeFullName(type)));
                    CheckExplicitDataContractNamespaceUri(ns, type);
                }
                else
                    ns = GetDefaultDataContractNamespace(type);
            }
            else
            {
                collectionContractAttribute = null;
                string arrayOfPrefix = Globals.ArrayPrefix + GetArrayPrefix(ref itemType);
                XmlQualifiedName elementXmlName = GetXmlName(itemType, previousCollectionTypes, out _);
                name = arrayOfPrefix + elementXmlName.Name;
                ns = GetCollectionNamespace(elementXmlName.Namespace);
            }
            return CreateQualifiedName(name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static string GetArrayPrefix(ref Type itemType)
        {
            string arrayOfPrefix = string.Empty;
            while (itemType.IsArray)
            {
                if (DataContract.GetBuiltInDataContract(itemType) != null)
                    break;
                arrayOfPrefix += Globals.ArrayPrefix;
                itemType = itemType.GetElementType()!;
            }
            return arrayOfPrefix;
        }

        internal static string GetCollectionNamespace(string elementNs)
        {
            return IsBuiltInNamespace(elementNs) ? Globals.CollectionsNamespace : elementNs;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual XmlQualifiedName GetArrayTypeName(bool isNullable)
        {
            XmlQualifiedName itemName;
            if (IsValueType && isNullable)
            {
                GenericInfo genericInfo = new GenericInfo(DataContract.GetXmlName(Globals.TypeOfNullable), Globals.TypeOfNullable.FullName!);
                genericInfo.Add(new GenericInfo(XmlName, null));
                genericInfo.AddToLevel(0, 1);
                itemName = genericInfo.GetExpandedXmlName();
            }
            else
            {
                itemName = XmlName;
            }
            string ns = GetCollectionNamespace(itemName.Namespace);
            string name = Globals.ArrayPrefix + itemName.Name;
            return new XmlQualifiedName(name, ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static XmlQualifiedName GetDefaultXmlName(Type type)
        {
            return CreateQualifiedName(GetDefaultXmlLocalName(type), GetDefaultXmlNamespace(type));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static string GetDefaultXmlLocalName(Type type)
        {
            if (type.IsGenericParameter)
                return "{" + type.GenericParameterPosition + "}";
            string typeName;
            string? arrayPrefix = null;
            if (type.IsArray)
                arrayPrefix = GetArrayPrefix(ref type);
            if (type.DeclaringType == null)
                typeName = type.Name;
            else
            {
                int nsLen = (type.Namespace == null) ? 0 : type.Namespace.Length;
                if (nsLen > 0)
                    nsLen++; //include the . following namespace
                typeName = DataContract.GetClrTypeFullName(type).Substring(nsLen).Replace('+', '.');
            }
            if (arrayPrefix != null)
                typeName = arrayPrefix + typeName;
            if (type.IsGenericType)
            {
                StringBuilder localName = new StringBuilder();
                StringBuilder namespaces = new StringBuilder();
                bool parametersFromBuiltInNamespaces = true;
                int iParam = typeName.IndexOf('[');
                if (iParam >= 0)
                    typeName = typeName.Substring(0, iParam);
                IList<int> nestedParamCounts = GetDataContractNameForGenericName(typeName, localName);
                bool isTypeOpenGeneric = type.IsGenericTypeDefinition;
                Type[] genParams = type.GetGenericArguments();
                for (int i = 0; i < genParams.Length; i++)
                {
                    Type genParam = genParams[i];
                    if (isTypeOpenGeneric)
                        localName.Append('{').Append(i).Append('}');
                    else
                    {
                        XmlQualifiedName qname = DataContract.GetXmlName(genParam);
                        localName.Append(qname.Name);
                        namespaces.Append(' ').Append(qname.Namespace);
                        if (parametersFromBuiltInNamespaces)
                            parametersFromBuiltInNamespaces = IsBuiltInNamespace(qname.Namespace);
                    }
                }
                if (isTypeOpenGeneric)
                    localName.Append("{#}");
                else if (nestedParamCounts.Count > 1 || !parametersFromBuiltInNamespaces)
                {
                    foreach (int count in nestedParamCounts)
                        namespaces.Insert(0, count.ToString(CultureInfo.InvariantCulture)).Insert(0, " ");
                    localName.Append(GetNamespacesDigest(namespaces.ToString()));
                }
                typeName = localName.ToString();
            }
            return DataContract.EncodeLocalName(typeName);
        }

        private static string GetDefaultDataContractNamespace(Type type)
        {
            string? clrNs = type.Namespace ?? string.Empty;
            string? ns =
                GetGlobalDataContractNamespace(clrNs, type.Module.GetCustomAttributes(typeof(ContractNamespaceAttribute)).ToArray()) ??
                GetGlobalDataContractNamespace(clrNs, type.Assembly.GetCustomAttributes(typeof(ContractNamespaceAttribute)).ToArray());

            if (ns == null)
            {
                ns = GetDefaultXmlNamespace(type);
            }
            else
            {
                CheckExplicitDataContractNamespaceUri(ns, type);
            }

            return ns;
        }

        internal static List<int> GetDataContractNameForGenericName(string typeName, StringBuilder? localName)
        {
            List<int> nestedParamCounts = new List<int>();
            for (int startIndex = 0, endIndex; ;)
            {
                endIndex = typeName.IndexOf('`', startIndex);
                if (endIndex < 0)
                {
                    localName?.Append(typeName.AsSpan(startIndex));
                    nestedParamCounts.Add(0);
                    break;
                }
                if (localName != null)
                {
                    string tempLocalName = typeName.Substring(startIndex, endIndex - startIndex);
                    localName.Append(tempLocalName);
                }
                while ((startIndex = typeName.IndexOf('.', startIndex + 1, endIndex - startIndex - 1)) >= 0)
                    nestedParamCounts.Add(0);
                startIndex = typeName.IndexOf('.', endIndex);
                if (startIndex < 0)
                {
                    nestedParamCounts.Add(int.Parse(typeName.AsSpan(endIndex + 1), provider: CultureInfo.InvariantCulture));
                    break;
                }
                else
                    nestedParamCounts.Add(int.Parse(typeName.AsSpan(endIndex + 1, startIndex - endIndex - 1), provider: CultureInfo.InvariantCulture));
            }
            localName?.Append("Of");
            return nestedParamCounts;
        }

        internal static bool IsBuiltInNamespace(string ns)
        {
            return (ns == Globals.SchemaNamespace || ns == Globals.SerializationNamespace);
        }

        internal static string GetDefaultXmlNamespace(Type type)
        {
            if (type.IsGenericParameter)
                return "{ns}";
            return GetDefaultXmlNamespace(type.Namespace);
        }

        internal static XmlQualifiedName CreateQualifiedName(string localName, string ns)
        {
            return new XmlQualifiedName(localName, GetNamespace(ns));
        }

        internal static string GetDefaultXmlNamespace(string? clrNs)
        {
            return new Uri(Globals.DataContractXsdBaseNamespaceUri, clrNs ?? string.Empty).AbsoluteUri;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static void GetDefaultXmlName(string fullTypeName, out string localName, out string ns)
        {
            CodeTypeReference typeReference = new CodeTypeReference(fullTypeName);
            GetDefaultName(typeReference, out localName, out ns);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static void GetDefaultName(CodeTypeReference typeReference, out string localName, out string ns)
        {
            string fullTypeName = typeReference.BaseType;
            DataContract? dataContract = GetBuiltInDataContract(fullTypeName);
            if (dataContract != null)
            {
                localName = dataContract.XmlName.Name;
                ns = dataContract.XmlName.Namespace;
                return;
            }

            GetClrNameAndNamespace(fullTypeName, out localName, out ns);
            if (typeReference.TypeArguments.Count > 0)
            {
                StringBuilder localNameBuilder = new StringBuilder();
                StringBuilder argNamespacesBuilder = new StringBuilder();
                bool parametersFromBuiltInNamespaces = true;
                List<int> nestedParamCounts = GetDataContractNameForGenericName(localName, localNameBuilder);
                foreach (CodeTypeReference typeArg in typeReference.TypeArguments)
                {
                    GetDefaultName(typeArg, out string typeArgName, out string typeArgNs);
                    localNameBuilder.Append(typeArgName);
                    argNamespacesBuilder.Append(' ').Append(typeArgNs);
                    if (parametersFromBuiltInNamespaces)
                    {
                        parametersFromBuiltInNamespaces = IsBuiltInNamespace(typeArgNs);
                    }
                }

                if (nestedParamCounts.Count > 1 || !parametersFromBuiltInNamespaces)
                {
                    foreach (int count in nestedParamCounts)
                    {
                        argNamespacesBuilder.Insert(0, count.ToString(CultureInfo.InvariantCulture)).Insert(0, ' ');
                    }

                    localNameBuilder.Append(GetNamespacesDigest(argNamespacesBuilder.ToString()));
                }

                localName = localNameBuilder.ToString();
            }

            localName = DataContract.EncodeLocalName(localName);
            ns = GetDefaultXmlNamespace(ns);
        }

        private static void CheckExplicitDataContractNamespaceUri(string dataContractNs, Type type)
        {
            if (dataContractNs.Length > 0)
            {
                string trimmedNs = dataContractNs.Trim();
                // Code similar to XmlConvert.ToUri (string.Empty is a valid uri but not "   ")
                if (trimmedNs.Length == 0 || trimmedNs.IndexOf("##", StringComparison.Ordinal) != -1)
                    ThrowInvalidDataContractException(SR.Format(SR.DataContractNamespaceIsNotValid, dataContractNs), type);
                dataContractNs = trimmedNs;
            }
            if (Uri.TryCreate(dataContractNs, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                if (uri.ToString() == Globals.SerializationNamespace)
                    ThrowInvalidDataContractException(SR.Format(SR.DataContractNamespaceReserved, Globals.SerializationNamespace), type);
            }
            else
                ThrowInvalidDataContractException(SR.Format(SR.DataContractNamespaceIsNotValid, dataContractNs), type);
        }

        internal static string GetClrTypeFullName(Type type)
        {
            return !type.IsGenericTypeDefinition && type.ContainsGenericParameters ? type.Namespace + "." + type.Name : type.FullName!;
        }

        internal static void GetClrNameAndNamespace(string fullTypeName, out string localName, out string ns)
        {
            int nsEnd = fullTypeName.LastIndexOf('.');
            if (nsEnd < 0)
            {
                ns = string.Empty;
                localName = fullTypeName.Replace('+', '.');
            }
            else
            {
                ns = fullTypeName.Substring(0, nsEnd);
                localName = fullTypeName.Substring(nsEnd + 1).Replace('+', '.');
            }
            int iParam = localName.IndexOf('[');
            if (iParam >= 0)
                localName = localName.Substring(0, iParam);
        }

        internal static string GetDataContractNamespaceFromUri(string uriString)
        {
            return uriString.StartsWith(Globals.DataContractXsdBaseNamespace, StringComparison.Ordinal) ? uriString.Substring(Globals.DataContractXsdBaseNamespace.Length) : uriString;
        }

        private static string? GetGlobalDataContractNamespace(string clrNs, object[] nsAttributes)
        {
            string? dataContractNs = null;
            for (int i = 0; i < nsAttributes.Length; i++)
            {
                ContractNamespaceAttribute nsAttribute = (ContractNamespaceAttribute)nsAttributes[i];
                string clrNsInAttribute = nsAttribute.ClrNamespace ?? string.Empty;
                if (clrNsInAttribute == clrNs)
                {
                    if (nsAttribute.ContractNamespace == null)
                        throw new InvalidDataContractException(SR.Format(SR.InvalidGlobalDataContractNamespace, clrNs));
                    if (dataContractNs != null)
                        throw new InvalidDataContractException(SR.Format(SR.DataContractNamespaceAlreadySet, dataContractNs, nsAttribute.ContractNamespace, clrNs));
                    dataContractNs = nsAttribute.ContractNamespace;
                }
            }
            return dataContractNs;
        }

        private static string GetNamespacesDigest(string namespaces)
        {
            byte[] namespaceBytes = Encoding.UTF8.GetBytes(namespaces);
            byte[] digestBytes = ComputeHash(namespaceBytes);
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

        // An incomplete implementation of MD5 necessary for back-compat.
        // "derived from the RSA Data Security, Inc. MD5 Message-Digest Algorithm"
        // THIS HASH MAY ONLY BE USED FOR BACKWARDS-COMPATIBLE NAME GENERATION.  DO NOT USE FOR SECURITY PURPOSES.
        private static byte[] ComputeHash(byte[] namespaces)
        {
            int[] shifts = new int[] { 7, 12, 17, 22, 5, 9, 14, 20, 4, 11, 16, 23, 6, 10, 15, 21 };
            uint[] sines = new uint[] {
                0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee, 0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
                0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be, 0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,

                0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa, 0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
                0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed, 0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,

                0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c, 0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
                0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05, 0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,

                0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039, 0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
                0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1, 0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391 };

            int blocks = (namespaces.Length + 8) / 64 + 1;

            uint aa = 0x67452301;
            uint bb = 0xefcdab89;
            uint cc = 0x98badcfe;
            uint dd = 0x10325476;

            for (int i = 0; i < blocks; i++)
            {
                byte[] block = namespaces;
                int offset = i * 64;

                if (offset + 64 > namespaces.Length)
                {
                    block = new byte[64];

                    for (int j = offset; j < namespaces.Length; j++)
                    {
                        block[j - offset] = namespaces[j];
                    }
                    if (offset <= namespaces.Length)
                    {
                        block[namespaces.Length - offset] = 0x80;
                    }
                    if (i == blocks - 1)
                    {
                        unchecked
                        {
                            block[56] = (byte)(namespaces.Length << 3);
                            block[57] = (byte)(namespaces.Length >> 5);
                            block[58] = (byte)(namespaces.Length >> 13);
                            block[59] = (byte)(namespaces.Length >> 21);
                        }
                    }

                    offset = 0;
                }

                uint a = aa;
                uint b = bb;
                uint c = cc;
                uint d = dd;

                uint f;
                int g;

                for (int j = 0; j < 64; j++)
                {
                    if (j < 16)
                    {
                        f = b & c | ~b & d;
                        g = j;
                    }
                    else if (j < 32)
                    {
                        f = b & d | c & ~d;
                        g = 5 * j + 1;
                    }
                    else if (j < 48)
                    {
                        f = b ^ c ^ d;
                        g = 3 * j + 5;
                    }
                    else
                    {
                        f = c ^ (b | ~d);
                        g = 7 * j;
                    }

                    g = (g & 0x0f) * 4 + offset;

                    uint hold = d;
                    d = c;
                    c = b;

                    b = unchecked(a + f + sines[j] + BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan(g)));
                    b = b << shifts[j & 3 | j >> 2 & ~3] | b >> 32 - shifts[j & 3 | j >> 2 & ~3];
                    b = unchecked(b + c);

                    a = hold;
                }

                unchecked
                {
                    aa += a;
                    bb += b;

                    if (i < blocks - 1)
                    {
                        cc += c;
                        dd += d;
                    }
                }
            }

            unchecked
            {
                return new byte[] { (byte)aa, (byte)(aa >> 8), (byte)(aa >> 16), (byte)(aa >> 24), (byte)bb, (byte)(bb >> 8) };
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static string ExpandGenericParameters(string format, Type type)
        {
            GenericNameProvider genericNameProviderForType = new GenericNameProvider(type);
            return ExpandGenericParameters(format, genericNameProviderForType);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static string ExpandGenericParameters(string format, IGenericNameProvider genericNameProvider)
        {
            string? digest = null;
            StringBuilder typeName = new StringBuilder();
            IList<int> nestedParameterCounts = genericNameProvider.GetNestedParameterCounts();
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
                        throw new InvalidDataContractException(SR.Format(SR.GenericNameBraceMismatch, format, genericNameProvider.GetGenericTypeName()));
                    if (format[start] == '#' && i == (start + 1))
                    {
                        if (nestedParameterCounts.Count > 1 || !genericNameProvider.ParametersFromBuiltInNamespaces)
                        {
                            if (digest == null)
                            {
                                StringBuilder namespaces = new StringBuilder(genericNameProvider.GetNamespaces());
                                foreach (int count in nestedParameterCounts)
                                    namespaces.Insert(0, count.ToString(CultureInfo.InvariantCulture)).Insert(0, " ");
                                digest = GetNamespacesDigest(namespaces.ToString());
                            }
                            typeName.Append(digest);
                        }
                    }
                    else
                    {
                        if (!int.TryParse(format.AsSpan(start, i - start), out int paramIndex) || paramIndex < 0 || paramIndex >= genericNameProvider.GetParameterCount())
                            throw new InvalidDataContractException(SR.Format(SR.GenericParameterNotValid, format.Substring(start, i - start), genericNameProvider.GetGenericTypeName(), genericNameProvider.GetParameterCount() - 1));
                        typeName.Append(genericNameProvider.GetParameterName(paramIndex));
                    }
                }
                else
                    typeName.Append(ch);
            }
            return typeName.ToString();
        }

        internal static bool IsTypeNullable(Type type)
        {
            return !type.IsValueType ||
                    (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == Globals.TypeOfNullable);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static DataContractDictionary? ImportKnownTypeAttributes(Type type)
        {
            DataContractDictionary? knownDataContracts = null;
            Dictionary<Type, Type> typesChecked = new Dictionary<Type, Type>();
            ImportKnownTypeAttributes(type, typesChecked, ref knownDataContracts);
            return knownDataContracts;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static void ImportKnownTypeAttributes(Type? type, Dictionary<Type, Type> typesChecked, ref DataContractDictionary? knownDataContracts)
        {
            while (type != null && DataContract.IsTypeSerializable(type))
            {
                if (typesChecked.ContainsKey(type))
                    return;

                typesChecked.Add(type, type);
                object[] knownTypeAttributes = type.GetCustomAttributes(Globals.TypeOfKnownTypeAttribute, false).ToArray();
                if (knownTypeAttributes != null)
                {
                    KnownTypeAttribute kt;
                    bool useMethod = false, useType = false;
                    for (int i = 0; i < knownTypeAttributes.Length; ++i)
                    {
                        kt = (KnownTypeAttribute)knownTypeAttributes[i];
                        if (kt.Type != null)
                        {
                            if (useMethod)
                            {
                                DataContract.ThrowInvalidDataContractException(SR.Format(SR.KnownTypeAttributeOneScheme, DataContract.GetClrTypeFullName(type)), type);
                            }

                            CheckAndAdd(kt.Type, typesChecked, ref knownDataContracts);
                            useType = true;
                        }
                        else
                        {
                            if (useMethod || useType)
                            {
                                DataContract.ThrowInvalidDataContractException(SR.Format(SR.KnownTypeAttributeOneScheme, DataContract.GetClrTypeFullName(type)), type);
                            }

                            string? methodName = kt.MethodName;
                            if (methodName == null)
                            {
                                DataContract.ThrowInvalidDataContractException(SR.Format(SR.KnownTypeAttributeNoData, DataContract.GetClrTypeFullName(type)), type);
                            }

                            if (methodName.Length == 0)
                                DataContract.ThrowInvalidDataContractException(SR.Format(SR.KnownTypeAttributeEmptyString, DataContract.GetClrTypeFullName(type)), type);

                            MethodInfo? method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, Type.EmptyTypes);
                            if (method == null)
                                DataContract.ThrowInvalidDataContractException(SR.Format(SR.KnownTypeAttributeUnknownMethod, methodName, DataContract.GetClrTypeFullName(type)), type);

                            if (!Globals.TypeOfTypeEnumerable.IsAssignableFrom(method.ReturnType))
                                DataContract.ThrowInvalidDataContractException(SR.Format(SR.KnownTypeAttributeReturnType, DataContract.GetClrTypeFullName(type), methodName), type);

                            object? types = method.Invoke(null, Array.Empty<object>());
                            if (types == null)
                            {
                                DataContract.ThrowInvalidDataContractException(SR.Format(SR.KnownTypeAttributeMethodNull, DataContract.GetClrTypeFullName(type)), type);
                            }

                            foreach (Type ty in (IEnumerable<Type>)types)
                            {
                                if (ty == null)
                                    DataContract.ThrowInvalidDataContractException(SR.Format(SR.KnownTypeAttributeValidMethodTypes, DataContract.GetClrTypeFullName(type)), type);

                                CheckAndAdd(ty, typesChecked, ref knownDataContracts);
                            }

                            useMethod = true;
                        }
                    }
                }

                // After careful consideration, I don't think this code is necessary anymore. After trying to
                // decipher the intent of the comments here, my best guess is that this was DCJS's way of working
                // around a non-[Serializable] KVP in Silverlight/early .Net Core. The regular DCS went with a
                // KVPAdapter approach. Neither is needed now.
                //
                // But this code does produce additional artifacts in schema handling. To be cautious, just in case
                // somebody needs a KVP contract in addition to the KV contract in their schema, I've kept this
                // here behind this AppContext switch.
                AppContext.TryGetSwitch("Switch.System.Runtime.Serialization.DataContracts.Auto_Import_KVP", out bool autoImportKVP);
                if (autoImportKVP)
                {
                    //For Json we need to add KeyValuePair<K,T> to KnownTypes if the UnderLyingType is a Dictionary<K,T>
                    try
                    {
                        if (DataContract.GetDataContract(type) is CollectionDataContract collectionDataContract && collectionDataContract.IsDictionary &&
                            collectionDataContract.ItemType.GetGenericTypeDefinition() == Globals.TypeOfKeyValue)
                        {
                            DataContract itemDataContract = DataContract.GetDataContract(Globals.TypeOfKeyValuePair.MakeGenericType(collectionDataContract.ItemType.GetGenericArguments()));
                            knownDataContracts ??= new DataContractDictionary();

                            knownDataContracts.TryAdd(itemDataContract.XmlName, itemDataContract);
                        }
                    }
                    catch (InvalidDataContractException)
                    {
                        //Ignore any InvalidDataContractException as this phase is a workaround for lack of ISerializable.
                        //InvalidDataContractException may happen as we walk the type hierarchy back to Object and encounter
                        //types that may not be valid DC. This step is purely for KeyValuePair and shouldn't fail the (de)serialization.
                        //Any IDCE in this case fails the serialization/deserialization process which is not the optimal experience.
                    }
                }

                type = type.BaseType;
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static void CheckAndAdd(Type type, Dictionary<Type, Type> typesChecked, [NotNullIfNotNull(nameof(nameToDataContractTable))] ref DataContractDictionary? nameToDataContractTable)
        {
            type = DataContract.UnwrapNullableType(type);
            DataContract dataContract = DataContract.GetDataContract(type);
            if (nameToDataContractTable == null)
            {
                nameToDataContractTable = new DataContractDictionary();
            }
            else if (nameToDataContractTable.TryGetValue(dataContract.XmlName, out DataContract? alreadyExistingContract))
            {
                // The alreadyExistingContract type was used as-is in NetFx. The call to get the appropriate adapter type was added in CoreFx with https://github.com/dotnet/runtime/commit/50c0a70c52fa66fafa1227be552ccdab5e4cf8e4
                // Don't throw duplicate if its a KeyValuePair<K,T> as it could have been added by Dictionary<K,T>
                if (DataContractCriticalHelper.GetDataContractAdapterType(alreadyExistingContract.UnderlyingType) != DataContractCriticalHelper.GetDataContractAdapterType(type))
                    throw new InvalidOperationException(SR.Format(SR.DupContractInKnownTypes, type, alreadyExistingContract.UnderlyingType, dataContract.XmlName.Namespace, dataContract.XmlName.Name));
                return;
            }
            nameToDataContractTable.Add(dataContract.XmlName, dataContract);
            ImportKnownTypeAttributes(type, typesChecked, ref nameToDataContractTable);
        }

        public sealed override bool Equals(object? obj)
        {
            if ((object)this == obj)
                return true;
            return Equals(obj, new HashSet<DataContractPairKey>());
        }

        internal virtual bool Equals(object? other, HashSet<DataContractPairKey>? checkedContracts)
        {
            if (other is DataContract dataContract)
            {
                return (XmlName.Name == dataContract.XmlName.Name && XmlName.Namespace == dataContract.XmlName.Namespace && IsReference == dataContract.IsReference);
            }
            return false;
        }

        internal bool IsEqualOrChecked(object? other, HashSet<DataContractPairKey>? checkedContracts)
        {
            if (other == null)
                return false;

            if ((object)this == other)
                return true;

            if (checkedContracts != null)
            {
                DataContractPairKey contractPairKey = new DataContractPairKey(this, other);
                if (checkedContracts.Contains(contractPairKey))
                    return true;
                checkedContracts.Add(contractPairKey);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <SecurityNote>
        /// Review - checks type visibility to calculate if access to it requires MemberAccessPermission.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal static bool IsTypeVisible(Type t)
        {
            // Generic parameters are always considered visible.
            if (t.IsGenericParameter)
            {
                return true;
            }

            // The normal Type.IsVisible check requires all nested types to be IsNestedPublic.
            // This does not comply with our convention where they can also have InternalsVisibleTo
            // with our assembly.   The following method performs a recursive walk back the declaring
            // type hierarchy to perform this enhanced IsVisible check.
            if (!IsTypeAndDeclaringTypeVisible(t))
                return false;

            foreach (Type genericType in t.GetGenericArguments())
            {
                if (!IsTypeVisible(genericType))
                    return false;
            }

            return true;
        }

        internal static bool IsTypeAndDeclaringTypeVisible(Type t)
        {
            // Arrays, etc. must consider the underlying element type because the
            // non-element type does not reflect the same type nesting.  For example,
            // MyClass[] would not show as a nested type, even when MyClass is nested.
            if (t.HasElementType)
            {
                return IsTypeVisible(t.GetElementType()!);
            }

            // Nested types are not visible unless their declaring type is visible.
            // Additionally, they must be either IsNestedPublic or in an assembly with InternalsVisibleTo this current assembly.
            // Non-nested types must be public or have this same InternalsVisibleTo relation.
            return t.IsNested
                    ? (t.IsNestedPublic || IsTypeVisibleInSerializationModule(t)) && IsTypeVisible(t.DeclaringType!)
                    : t.IsPublic || IsTypeVisibleInSerializationModule(t);
        }

        /// <SecurityNote>
        /// Review - checks constructor visibility to calculate if access to it requires MemberAccessPermission.
        ///          note: does local check for visibility, assuming that the declaring Type visibility has been checked.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal static bool ConstructorRequiresMemberAccess(ConstructorInfo? ctor)
        {
            return ctor != null && !ctor.IsPublic && !IsMemberVisibleInSerializationModule(ctor);
        }

        /// <SecurityNote>
        /// Review - checks method visibility to calculate if access to it requires MemberAccessPermission.
        ///          note: does local check for visibility, assuming that the declaring Type visibility has been checked.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal static bool MethodRequiresMemberAccess(MethodInfo? method)
        {
            return method != null && !method.IsPublic && !IsMemberVisibleInSerializationModule(method);
        }

        /// <SecurityNote>
        /// Review - checks field visibility to calculate if access to it requires MemberAccessPermission.
        ///          note: does local check for visibility, assuming that the declaring Type visibility has been checked.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal static bool FieldRequiresMemberAccess(FieldInfo? field)
        {
            return field != null && !field.IsPublic && !IsMemberVisibleInSerializationModule(field);
        }

        /// <SecurityNote>
        /// Review - checks type visibility to calculate if access to it requires MemberAccessPermission.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        private static bool IsTypeVisibleInSerializationModule(Type type)
        {
            return (type.Module.Equals(typeof(DataContract).Module) || IsAssemblyFriendOfSerialization(type.Assembly)) && !type.IsNestedPrivate;
        }

        /// <SecurityNote>
        /// Review - checks member visibility to calculate if access to it requires MemberAccessPermission.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        private static bool IsMemberVisibleInSerializationModule(MemberInfo member)
        {
            if (!IsTypeVisibleInSerializationModule(member.DeclaringType!))
                return false;

            if (member is MethodInfo method)
            {
                return (method.IsAssembly || method.IsFamilyOrAssembly);
            }
            else if (member is FieldInfo field)
            {
                return (field.IsAssembly || field.IsFamilyOrAssembly) && IsTypeVisible(field.FieldType);
            }
            else if (member is ConstructorInfo constructor)
            {
                return (constructor.IsAssembly || constructor.IsFamilyOrAssembly);
            }

            return false;
        }

        /// <SecurityNote>
        /// Review - checks member visibility to calculate if access to it requires MemberAccessPermission.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal static bool IsAssemblyFriendOfSerialization(Assembly assembly)
        {
            InternalsVisibleToAttribute[] internalsVisibleAttributes = (InternalsVisibleToAttribute[])assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute));
            foreach (InternalsVisibleToAttribute internalsVisibleAttribute in internalsVisibleAttributes)
            {
                string internalsVisibleAttributeAssemblyName = internalsVisibleAttribute.AssemblyName;

                if (internalsVisibleAttributeAssemblyName.Trim().Equals("System.Runtime.Serialization") ||
                    Globals.FullSRSInternalsVisibleRegex().IsMatch(internalsVisibleAttributeAssemblyName))
                {
                    return true;
                }
            }
            return false;
        }

        internal static string SanitizeTypeName(string typeName)
        {
            return typeName.Replace('.', '_');
        }
    }

    internal interface IGenericNameProvider
    {
        int GetParameterCount();
        IList<int> GetNestedParameterCounts();
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        string GetParameterName(int paramIndex);
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        string GetNamespaces();
        string? GetGenericTypeName();
        bool ParametersFromBuiltInNamespaces
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get;
        }
    }

    internal sealed class GenericNameProvider : IGenericNameProvider
    {
        private readonly string _genericTypeName;
        private readonly object[] _genericParams; //Type or DataContract
        private readonly IList<int> _nestedParamCounts;
        internal GenericNameProvider(Type type)
            : this(DataContract.GetClrTypeFullName(type.GetGenericTypeDefinition()), type.GetGenericArguments())
        {
        }

        internal GenericNameProvider(string genericTypeName, object[] genericParams)
        {
            _genericTypeName = genericTypeName;
            _genericParams = new object[genericParams.Length];
            genericParams.CopyTo(_genericParams, 0);

            DataContract.GetClrNameAndNamespace(genericTypeName, out string name, out _);
            _nestedParamCounts = DataContract.GetDataContractNameForGenericName(name, null);
        }

        public int GetParameterCount()
        {
            return _genericParams.Length;
        }

        public IList<int> GetNestedParameterCounts()
        {
            return _nestedParamCounts;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public string GetParameterName(int paramIndex)
        {
            return GetXmlName(paramIndex).Name;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public string GetNamespaces()
        {
            StringBuilder namespaces = new StringBuilder();
            for (int j = 0; j < GetParameterCount(); j++)
                namespaces.Append(' ').Append(GetXmlName(j).Namespace);
            return namespaces.ToString();
        }

        public string? GetGenericTypeName()
        {
            return _genericTypeName;
        }

        public bool ParametersFromBuiltInNamespaces
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                bool parametersFromBuiltInNamespaces = true;
                for (int j = 0; j < GetParameterCount(); j++)
                {
                    if (parametersFromBuiltInNamespaces)
                        parametersFromBuiltInNamespaces = DataContract.IsBuiltInNamespace(GetXmlName(j).Namespace);
                    else
                        break;
                }
                return parametersFromBuiltInNamespaces;
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlQualifiedName GetXmlName(int i)
        {
            object o = _genericParams[i];
            XmlQualifiedName? qname = o as XmlQualifiedName;
            if (qname == null)
            {
                Type? paramType = o as Type;
                if (paramType != null)
                    _genericParams[i] = qname = DataContract.GetXmlName(paramType);
                else
                    _genericParams[i] = qname = ((DataContract)o).XmlName;
            }
            return qname;
        }
    }

    internal sealed class GenericInfo : IGenericNameProvider
    {
        private readonly string? _genericTypeName;
        private readonly XmlQualifiedName _xmlName;
        private List<GenericInfo>? _paramGenericInfos;
        private readonly List<int> _nestedParamCounts;

        internal GenericInfo(XmlQualifiedName xmlName, string? genericTypeName)
        {
            _xmlName = xmlName;
            _genericTypeName = genericTypeName;
            _nestedParamCounts = new List<int>();
            _nestedParamCounts.Add(0);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public XmlQualifiedName GetExpandedXmlName()
        {
            if (_paramGenericInfos == null)
                return _xmlName;
            return new XmlQualifiedName(DataContract.EncodeLocalName(DataContract.ExpandGenericParameters(XmlConvert.DecodeName(_xmlName.Name), this)), _xmlName.Namespace);
        }

        public XmlQualifiedName XmlName => _xmlName;

        public IList<GenericInfo>? Parameters => _paramGenericInfos;

        internal void Add(GenericInfo actualParamInfo)
        {
            _paramGenericInfos ??= new List<GenericInfo>();
            _paramGenericInfos.Add(actualParamInfo);
        }

        internal void AddToLevel(int level, int count)
        {
            if (level >= _nestedParamCounts.Count)
            {
                do
                {
                    _nestedParamCounts.Add((level == _nestedParamCounts.Count) ? count : 0);
                } while (level >= _nestedParamCounts.Count);
            }
            else
                _nestedParamCounts[level] = _nestedParamCounts[level] + count;
        }

        internal string GetXmlNamespace()
        {
            return _xmlName.Namespace;
        }

        int IGenericNameProvider.GetParameterCount()
        {
            return _paramGenericInfos?.Count ?? 0;
        }

        IList<int> IGenericNameProvider.GetNestedParameterCounts()
        {
            return _nestedParamCounts;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        string IGenericNameProvider.GetParameterName(int paramIndex)
        {
            Debug.Assert(_paramGenericInfos != null);
            return _paramGenericInfos[paramIndex].GetExpandedXmlName().Name;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        string IGenericNameProvider.GetNamespaces()
        {
            if (_paramGenericInfos == null || _paramGenericInfos.Count == 0)
                return "";

            StringBuilder namespaces = new StringBuilder();
            for (int j = 0; j < _paramGenericInfos.Count; j++)
                namespaces.Append(' ').Append(_paramGenericInfos[j].GetXmlNamespace());
            return namespaces.ToString();
        }

        string? IGenericNameProvider.GetGenericTypeName()
        {
            return _genericTypeName;
        }

        bool IGenericNameProvider.ParametersFromBuiltInNamespaces
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                bool parametersFromBuiltInNamespaces = true;

                if (_paramGenericInfos == null || _paramGenericInfos.Count == 0)
                    return parametersFromBuiltInNamespaces;

                for (int j = 0; j < _paramGenericInfos.Count; j++)
                {
                    if (parametersFromBuiltInNamespaces)
                        parametersFromBuiltInNamespaces = DataContract.IsBuiltInNamespace(_paramGenericInfos[j].GetXmlNamespace());
                    else
                        break;
                }
                return parametersFromBuiltInNamespaces;
            }
        }
    }

    internal sealed class DataContractPairKey
    {
        private readonly object _object1;
        private readonly object _object2;

        internal DataContractPairKey(object object1, object object2)
        {
            _object1 = object1;
            _object2 = object2;
        }

        public override bool Equals(object? other)
        {
            if (other is not DataContractPairKey otherKey)
                return false;
            return ((otherKey._object1 == _object1 && otherKey._object2 == _object2) || (otherKey._object1 == _object2 && otherKey._object2 == _object1));
        }

        public override int GetHashCode()
        {
            return _object1.GetHashCode() ^ _object2.GetHashCode();
        }
    }

    internal sealed class HashTableEqualityComparer : IEqualityComparer
    {
        bool IEqualityComparer.Equals(object? x, object? y)
        {
            return ((TypeHandleRef)x!).Value.Equals(((TypeHandleRef)y!).Value);
        }

        public int GetHashCode(object obj)
        {
            return ((TypeHandleRef)obj).Value.GetHashCode();
        }
    }

    internal sealed class TypeHandleRefEqualityComparer : IEqualityComparer<TypeHandleRef>
    {
        public bool Equals(TypeHandleRef? x, TypeHandleRef? y)
        {
            return x!.Value.Equals(y!.Value);
        }

        public int GetHashCode(TypeHandleRef obj)
        {
            return obj.Value.GetHashCode();
        }
    }

    internal sealed class TypeHandleRef
    {
        private RuntimeTypeHandle _value;

        public TypeHandleRef()
        {
        }

        public TypeHandleRef(RuntimeTypeHandle value)
        {
            _value = value;
        }

        public RuntimeTypeHandle Value
        {
            get => _value;
            set => _value = value;
        }
    }

    internal sealed class IntRef
    {
        private readonly int _value;

        public IntRef(int value)
        {
            _value = value;
        }

        public int Value => _value;
    }
}
