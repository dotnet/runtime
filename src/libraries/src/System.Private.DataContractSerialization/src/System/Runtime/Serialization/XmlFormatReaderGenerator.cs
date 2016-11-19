// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;
#if NET_NATIVE
using Internal.Runtime.Augments;
#endif

namespace System.Runtime.Serialization
{
#if USE_REFEMIT || NET_NATIVE
    public delegate object XmlFormatClassReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces);
    public delegate object XmlFormatCollectionReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, CollectionDataContract collectionContract);
    public delegate void XmlFormatGetOnlyCollectionReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, CollectionDataContract collectionContract);

    public sealed class XmlFormatReaderGenerator
#else
    internal delegate object XmlFormatClassReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces);
    internal delegate object XmlFormatCollectionReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, CollectionDataContract collectionContract);
    internal delegate void XmlFormatGetOnlyCollectionReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, CollectionDataContract collectionContract);

    internal sealed class XmlFormatReaderGenerator
#endif
    {
        private static readonly Func<Type, object> s_getUninitializedObjectDelegate = (Func<Type, object>)
            typeof(string)
            .GetTypeInfo()
            .Assembly
            .GetType("System.Runtime.Serialization.FormatterServices")
            ?.GetMethod("GetUninitializedObject", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            ?.CreateDelegate(typeof(Func<Type, object>));

        private static readonly ConcurrentDictionary<Type, bool> s_typeHasDefaultConstructorMap = new ConcurrentDictionary<Type, bool>();

        private CriticalHelper _helper;

        public XmlFormatReaderGenerator()
        {
            _helper = new CriticalHelper();
        }

        public XmlFormatClassReaderDelegate GenerateClassReader(ClassDataContract classContract)
        {
            return _helper.GenerateClassReader(classContract);
        }

        public XmlFormatCollectionReaderDelegate GenerateCollectionReader(CollectionDataContract collectionContract)
        {
            return _helper.GenerateCollectionReader(collectionContract);
        }

        public XmlFormatGetOnlyCollectionReaderDelegate GenerateGetOnlyCollectionReader(CollectionDataContract collectionContract)
        {
            return _helper.GenerateGetOnlyCollectionReader(collectionContract);
        }

        /// <SecurityNote>
        /// Review - handles all aspects of IL generation including initializing the DynamicMethod.
        ///          changes to how IL generated could affect how data is deserialized and what gets access to data,
        ///          therefore we mark it for review so that changes to generation logic are reviewed.
        /// </SecurityNote>
        private class CriticalHelper
        {
#if !NET_NATIVE
            private CodeGenerator _ilg;
            private LocalBuilder _objectLocal;
            private Type _objectType;
            private ArgBuilder _xmlReaderArg;
            private ArgBuilder _contextArg;
            private ArgBuilder _memberNamesArg;
            private ArgBuilder _memberNamespacesArg;
            private ArgBuilder _collectionContractArg;
#endif

            public XmlFormatClassReaderDelegate GenerateClassReader(ClassDataContract classContract)
            {
                if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
                {
                    return new ReflectionXmlClassReader(classContract).ReflectionReadClass;
                }
#if NET_NATIVE
                else if (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
                {
                    return new ReflectionXmlClassReader(classContract).ReflectionReadClass;
                }
#endif
                else
                {
#if NET_NATIVE
                    throw new InvalidOperationException("Cannot generate class reader");
#else
                    _ilg = new CodeGenerator();
                    bool memberAccessFlag = classContract.RequiresMemberAccessForRead(null);
                    try
                    {
                        _ilg.BeginMethod("Read" + classContract.StableName.Name + "FromXml", Globals.TypeOfXmlFormatClassReaderDelegate, memberAccessFlag);
                    }
                    catch (SecurityException securityException)
                    {
                        if (memberAccessFlag)
                        {
                            classContract.RequiresMemberAccessForRead(securityException);
                        }
                        else
                        {
                            throw;
                        }
                    }

                    InitArgs();
                    CreateObject(classContract);
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, _objectLocal);
                    InvokeOnDeserializing(classContract);
                    LocalBuilder objectId = null;
                    if (classContract.IsISerializable)
                    {
                        ReadISerializable(classContract);
                    }
                    else
                    {
                        ReadClass(classContract);
                    }

                    if (Globals.TypeOfIDeserializationCallback.IsAssignableFrom(classContract.UnderlyingType))
                    {
                        _ilg.Call(_objectLocal, XmlFormatGeneratorStatics.OnDeserializationMethod, null);
                    }

                    InvokeOnDeserialized(classContract);
                    if (objectId == null)
                    {
                        _ilg.Load(_objectLocal);

                        // Do a conversion back from DateTimeOffsetAdapter to DateTimeOffset after deserialization.
                        // DateTimeOffsetAdapter is used here for deserialization purposes to bypass the ISerializable implementation
                        // on DateTimeOffset; which does not work in partial trust.

                        if (classContract.UnderlyingType == Globals.TypeOfDateTimeOffsetAdapter)
                        {
                            _ilg.ConvertValue(_objectLocal.LocalType, Globals.TypeOfDateTimeOffsetAdapter);
                            _ilg.Call(XmlFormatGeneratorStatics.GetDateTimeOffsetMethod);
                            _ilg.ConvertValue(Globals.TypeOfDateTimeOffset, _ilg.CurrentMethod.ReturnType);
                        }
                        //Copy the KeyValuePairAdapter<K,T> to a KeyValuePair<K,T>. 
                        else if (classContract.IsKeyValuePairAdapter)
                        {
                            _ilg.Call(classContract.GetKeyValuePairMethodInfo);
                            _ilg.ConvertValue(Globals.TypeOfKeyValuePair.MakeGenericType(classContract.KeyValuePairGenericArguments), _ilg.CurrentMethod.ReturnType);
                        }
                        else
                        {
                            _ilg.ConvertValue(_objectLocal.LocalType, _ilg.CurrentMethod.ReturnType);
                        }
                    }
                    return (XmlFormatClassReaderDelegate)_ilg.EndMethod();
#endif
                }
            }

            public XmlFormatCollectionReaderDelegate GenerateCollectionReader(CollectionDataContract collectionContract)
            {
                if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
                {
                    return new ReflectionXmlCollectionReader().ReflectionReadCollection;
                }
#if NET_NATIVE
                else if (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
                {
                    return new ReflectionXmlCollectionReader().ReflectionReadCollection;
                }
#endif
                else
                {
#if NET_NATIVE
                    throw new InvalidOperationException("Cannot generate class reader");
#else
                    _ilg = GenerateCollectionReaderHelper(collectionContract, false /*isGetOnlyCollection*/);
                    ReadCollection(collectionContract);
                    _ilg.Load(_objectLocal);
                    _ilg.ConvertValue(_objectLocal.LocalType, _ilg.CurrentMethod.ReturnType);
                    return (XmlFormatCollectionReaderDelegate)_ilg.EndMethod();
#endif
                }
            }

            public XmlFormatGetOnlyCollectionReaderDelegate GenerateGetOnlyCollectionReader(CollectionDataContract collectionContract)
            {
                if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
                {
                    return new ReflectionXmlCollectionReader().ReflectionReadGetOnlyCollection;
                }
#if NET_NATIVE
                else if (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
                {
                    return new ReflectionXmlCollectionReader().ReflectionReadGetOnlyCollection;
                }
#endif
                else
                {
#if NET_NATIVE
                    throw new InvalidOperationException("Cannot generate class reader");
#else
                    _ilg = GenerateCollectionReaderHelper(collectionContract, true /*isGetOnlyCollection*/);
                    ReadGetOnlyCollection(collectionContract);
                    return (XmlFormatGetOnlyCollectionReaderDelegate)_ilg.EndMethod();
#endif
                }
            }

#if !NET_NATIVE
            private CodeGenerator GenerateCollectionReaderHelper(CollectionDataContract collectionContract, bool isGetOnlyCollection)
            {
                _ilg = new CodeGenerator();
                bool memberAccessFlag = collectionContract.RequiresMemberAccessForRead(null);
                try
                {
                    if (isGetOnlyCollection)
                    {
                        _ilg.BeginMethod("Read" + collectionContract.StableName.Name + "FromXml" + "IsGetOnly", Globals.TypeOfXmlFormatGetOnlyCollectionReaderDelegate, memberAccessFlag);
                    }
                    else
                    {
                        _ilg.BeginMethod("Read" + collectionContract.StableName.Name + "FromXml" + string.Empty, Globals.TypeOfXmlFormatCollectionReaderDelegate, memberAccessFlag);
                    }
                }
                catch (SecurityException securityException)
                {
                    if (memberAccessFlag)
                    {
                        collectionContract.RequiresMemberAccessForRead(securityException);
                    }
                    else
                    {
                        throw;
                    }
                }
                InitArgs();
                _collectionContractArg = _ilg.GetArg(4);
                return _ilg;
            }

            private void InitArgs()
            {
                _xmlReaderArg = _ilg.GetArg(0);
                _contextArg = _ilg.GetArg(1);
                _memberNamesArg = _ilg.GetArg(2);
                _memberNamespacesArg = _ilg.GetArg(3);
            }


            private void CreateObject(ClassDataContract classContract)
            {
                Type type = _objectType = classContract.UnderlyingType;
                if (type.GetTypeInfo().IsValueType && !classContract.IsNonAttributedType)
                    type = Globals.TypeOfValueType;

                _objectLocal = _ilg.DeclareLocal(type, "objectDeserialized");

                if (classContract.UnderlyingType == Globals.TypeOfDBNull)
                {
                    _ilg.LoadMember(Globals.TypeOfDBNull.GetField("Value"));
                    _ilg.Stloc(_objectLocal);
                }
                else if (classContract.IsNonAttributedType)
                {
                    if (type.GetTypeInfo().IsValueType)
                    {
                        _ilg.Ldloca(_objectLocal);
                        _ilg.InitObj(type);
                    }
                    else
                    {
                        _ilg.New(classContract.GetNonAttributedTypeConstructor());
                        _ilg.Stloc(_objectLocal);
                    }
                }
                else
                {
                    _ilg.Call(null, XmlFormatGeneratorStatics.GetUninitializedObjectMethod, DataContract.GetIdForInitialization(classContract));
                    _ilg.ConvertValue(Globals.TypeOfObject, type);
                    _ilg.Stloc(_objectLocal);
                }
            }

            private void InvokeOnDeserializing(ClassDataContract classContract)
            {
                if (classContract.BaseContract != null)
                    InvokeOnDeserializing(classContract.BaseContract);
                if (classContract.OnDeserializing != null)
                {
                    _ilg.LoadAddress(_objectLocal);
                    _ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
                    _ilg.Load(_contextArg);
                    _ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
                    _ilg.Call(classContract.OnDeserializing);
                }
            }

            private void InvokeOnDeserialized(ClassDataContract classContract)
            {
                if (classContract.BaseContract != null)
                    InvokeOnDeserialized(classContract.BaseContract);
                if (classContract.OnDeserialized != null)
                {
                    _ilg.LoadAddress(_objectLocal);
                    _ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
                    _ilg.Load(_contextArg);
                    _ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
                    _ilg.Call(classContract.OnDeserialized);
                }
            }


            private void ReadClass(ClassDataContract classContract)
            {
                if (classContract.HasExtensionData)
                {
                    LocalBuilder extensionDataLocal = _ilg.DeclareLocal(Globals.TypeOfExtensionDataObject, "extensionData");
                    _ilg.New(XmlFormatGeneratorStatics.ExtensionDataObjectCtor);
                    _ilg.Store(extensionDataLocal);
                    ReadMembers(classContract, extensionDataLocal);

                    ClassDataContract currentContract = classContract;
                    while (currentContract != null)
                    {
                        MethodInfo extensionDataSetMethod = currentContract.ExtensionDataSetMethod;
                        if (extensionDataSetMethod != null)
                            _ilg.Call(_objectLocal, extensionDataSetMethod, extensionDataLocal);
                        currentContract = currentContract.BaseContract;
                    }
                }
                else
                {
                    ReadMembers(classContract, null /*extensionDataLocal*/);
                }
            }

            private void ReadMembers(ClassDataContract classContract, LocalBuilder extensionDataLocal)
            {
                int memberCount = classContract.MemberNames.Length;
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, memberCount);

                LocalBuilder memberIndexLocal = _ilg.DeclareLocal(Globals.TypeOfInt, "memberIndex", -1);

                int firstRequiredMember;
                bool[] requiredMembers = GetRequiredMembers(classContract, out firstRequiredMember);
                bool hasRequiredMembers = (firstRequiredMember < memberCount);
                LocalBuilder requiredIndexLocal = hasRequiredMembers ? _ilg.DeclareLocal(Globals.TypeOfInt, "requiredIndex", firstRequiredMember) : null;

                object forReadElements = _ilg.For(null, null, null);
                _ilg.Call(null, XmlFormatGeneratorStatics.MoveToNextElementMethod, _xmlReaderArg);
                _ilg.IfFalseBreak(forReadElements);
                if (hasRequiredMembers)
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetMemberIndexWithRequiredMembersMethod, _xmlReaderArg, _memberNamesArg, _memberNamespacesArg, memberIndexLocal, requiredIndexLocal, extensionDataLocal);
                else
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetMemberIndexMethod, _xmlReaderArg, _memberNamesArg, _memberNamespacesArg, memberIndexLocal, extensionDataLocal);
                Label[] memberLabels = _ilg.Switch(memberCount);
                ReadMembers(classContract, requiredMembers, memberLabels, memberIndexLocal, requiredIndexLocal);
                _ilg.EndSwitch();
                _ilg.EndFor();
                if (hasRequiredMembers)
                {
                    _ilg.If(requiredIndexLocal, Cmp.LessThan, memberCount);
                    _ilg.Call(null, XmlFormatGeneratorStatics.ThrowRequiredMemberMissingExceptionMethod, _xmlReaderArg, memberIndexLocal, requiredIndexLocal, _memberNamesArg);
                    _ilg.EndIf();
                }
            }

            private int ReadMembers(ClassDataContract classContract, bool[] requiredMembers, Label[] memberLabels, LocalBuilder memberIndexLocal, LocalBuilder requiredIndexLocal)
            {
                int memberCount = (classContract.BaseContract == null) ? 0 : ReadMembers(classContract.BaseContract, requiredMembers,
                    memberLabels, memberIndexLocal, requiredIndexLocal);

                for (int i = 0; i < classContract.Members.Count; i++, memberCount++)
                {
                    DataMember dataMember = classContract.Members[i];
                    Type memberType = dataMember.MemberType;
                    _ilg.Case(memberLabels[memberCount], dataMember.Name);
                    if (dataMember.IsRequired)
                    {
                        int nextRequiredIndex = memberCount + 1;
                        for (; nextRequiredIndex < requiredMembers.Length; nextRequiredIndex++)
                            if (requiredMembers[nextRequiredIndex])
                                break;
                        _ilg.Set(requiredIndexLocal, nextRequiredIndex);
                    }

                    LocalBuilder value = null;

                    if (dataMember.IsGetOnlyCollection)
                    {
                        _ilg.LoadAddress(_objectLocal);
                        _ilg.LoadMember(dataMember.MemberInfo);
                        value = _ilg.DeclareLocal(memberType, dataMember.Name + "Value");
                        _ilg.Stloc(value);
                        _ilg.Call(_contextArg, XmlFormatGeneratorStatics.StoreCollectionMemberInfoMethod, value);
                        ReadValue(memberType, dataMember.Name, classContract.StableName.Namespace);
                    }
                    else
                    {
                        value = ReadValue(memberType, dataMember.Name, classContract.StableName.Namespace);
                        _ilg.LoadAddress(_objectLocal);
                        _ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
                        _ilg.Ldloc(value);
                        _ilg.StoreMember(dataMember.MemberInfo);
                    }

#if FEATURE_LEGACYNETCF
                    // The DataContractSerializer in the full framework doesn't support unordered elements: 
                    // deserialization will fail if the data members in the XML are not sorted alphabetically.
                    // But the NetCF DataContractSerializer does support unordered element. To maintain compatibility
                    // with Mango we always search for the member from the beginning of the member list.
                    // We set memberIndexLocal to -1 because GetMemberIndex always starts from memberIndex+1.
                    if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                        ilg.Set(memberIndexLocal, (int)-1);
                    else
#endif // FEATURE_LEGACYNETCF
                    _ilg.Set(memberIndexLocal, memberCount);

                    _ilg.EndCase();
                }
                return memberCount;
            }

            private bool[] GetRequiredMembers(ClassDataContract contract, out int firstRequiredMember)
            {
                int memberCount = contract.MemberNames.Length;
                bool[] requiredMembers = new bool[memberCount];
                GetRequiredMembers(contract, requiredMembers);
                for (firstRequiredMember = 0; firstRequiredMember < memberCount; firstRequiredMember++)
                    if (requiredMembers[firstRequiredMember])
                        break;
                return requiredMembers;
            }

            private int GetRequiredMembers(ClassDataContract contract, bool[] requiredMembers)
            {
                int memberCount = (contract.BaseContract == null) ? 0 : GetRequiredMembers(contract.BaseContract, requiredMembers);
                List<DataMember> members = contract.Members;
                for (int i = 0; i < members.Count; i++, memberCount++)
                {
                    requiredMembers[memberCount] = members[i].IsRequired;
                }
                return memberCount;
            }

            private void ReadISerializable(ClassDataContract classContract)
            {
                ConstructorInfo ctor = classContract.GetISerializableConstructor();
                _ilg.LoadAddress(_objectLocal);
                _ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.ReadSerializationInfoMethod, _xmlReaderArg, classContract.UnderlyingType);
                _ilg.Load(_contextArg);
                _ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
                _ilg.Call(ctor);
            }

            private LocalBuilder ReadValue(Type type, string name, string ns)
            {
                LocalBuilder value = _ilg.DeclareLocal(type, "valueRead");
                LocalBuilder nullableValue = null;
                int nullables = 0;
                while (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
                {
                    nullables++;
                    type = type.GetGenericArguments()[0];
                }

                PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
                if ((primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject) || nullables != 0 || type.GetTypeInfo().IsValueType)
                {
                    LocalBuilder objectId = _ilg.DeclareLocal(Globals.TypeOfString, "objectIdRead");
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.ReadAttributesMethod, _xmlReaderArg);
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.ReadIfNullOrRefMethod, _xmlReaderArg, type, DataContract.IsTypeSerializable(type));
                    _ilg.Stloc(objectId);
                    // Deserialize null
                    _ilg.If(objectId, Cmp.EqualTo, Globals.NullObjectId);
                    if (nullables != 0)
                    {
                        _ilg.LoadAddress(value);
                        _ilg.InitObj(value.LocalType);
                    }
                    else if (type.GetTypeInfo().IsValueType)
                        ThrowValidationException(SR.Format(SR.ValueTypeCannotBeNull, DataContract.GetClrTypeFullName(type)));
                    else
                    {
                        _ilg.Load(null);
                        _ilg.Stloc(value);
                    }

                    // Deserialize value

                    // Compare against Globals.NewObjectId, which is set to string.Empty
                    _ilg.ElseIfIsEmptyString(objectId);
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetObjectIdMethod);
                    _ilg.Stloc(objectId);
                    if (type.GetTypeInfo().IsValueType)
                    {
                        _ilg.IfNotIsEmptyString(objectId);
                        ThrowValidationException(SR.Format(SR.ValueTypeCannotHaveId, DataContract.GetClrTypeFullName(type)));
                        _ilg.EndIf();
                    }
                    if (nullables != 0)
                    {
                        nullableValue = value;
                        value = _ilg.DeclareLocal(type, "innerValueRead");
                    }

                    if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject)
                    {
                        _ilg.Call(_xmlReaderArg, primitiveContract.XmlFormatReaderMethod);
                        _ilg.Stloc(value);
                        if (!type.GetTypeInfo().IsValueType)
                            _ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, value);
                    }
                    else
                    {
                        InternalDeserialize(value, type, name, ns);
                    }
                    // Deserialize ref
                    _ilg.Else();
                    if (type.GetTypeInfo().IsValueType)
                        ThrowValidationException(SR.Format(SR.ValueTypeCannotHaveRef, DataContract.GetClrTypeFullName(type)));
                    else
                    {
                        _ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetExistingObjectMethod, objectId, type, name, ns);
                        _ilg.ConvertValue(Globals.TypeOfObject, type);
                        _ilg.Stloc(value);
                    }
                    _ilg.EndIf();

                    if (nullableValue != null)
                    {
                        _ilg.If(objectId, Cmp.NotEqualTo, Globals.NullObjectId);
                        WrapNullableObject(value, nullableValue, nullables);
                        _ilg.EndIf();
                        value = nullableValue;
                    }
                }
                else
                {
                    InternalDeserialize(value, type, name, ns);
                }

                return value;
            }

            private void InternalDeserialize(LocalBuilder value, Type type, string name, string ns)
            {
                _ilg.Load(_contextArg);
                _ilg.Load(_xmlReaderArg);
                Type declaredType = type;
                _ilg.Load(DataContract.GetId(declaredType.TypeHandle));
                _ilg.Ldtoken(declaredType);
                _ilg.Load(name);
                _ilg.Load(ns);
                _ilg.Call(XmlFormatGeneratorStatics.InternalDeserializeMethod);

                _ilg.ConvertValue(Globals.TypeOfObject, type);
                _ilg.Stloc(value);
            }

            private void WrapNullableObject(LocalBuilder innerValue, LocalBuilder outerValue, int nullables)
            {
                Type innerType = innerValue.LocalType, outerType = outerValue.LocalType;
                _ilg.LoadAddress(outerValue);
                _ilg.Load(innerValue);
                for (int i = 1; i < nullables; i++)
                {
                    Type type = Globals.TypeOfNullable.MakeGenericType(innerType);
                    _ilg.New(type.GetConstructor(new Type[] { innerType }));
                    innerType = type;
                }
                _ilg.Call(outerType.GetConstructor(new Type[] { innerType }));
            }

            private void ReadCollection(CollectionDataContract collectionContract)
            {
                Type type = collectionContract.UnderlyingType;
                Type itemType = collectionContract.ItemType;
                bool isArray = (collectionContract.Kind == CollectionKind.Array);

                ConstructorInfo constructor = collectionContract.Constructor;

                if (type.GetTypeInfo().IsInterface)
                {
                    switch (collectionContract.Kind)
                    {
                        case CollectionKind.GenericDictionary:
                            type = Globals.TypeOfDictionaryGeneric.MakeGenericType(itemType.GetGenericArguments());
                            constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, Array.Empty<Type>());
                            break;
                        case CollectionKind.Dictionary:
                            type = Globals.TypeOfHashtable;
                            constructor = XmlFormatGeneratorStatics.HashtableCtor;
                            break;
                        case CollectionKind.Collection:
                        case CollectionKind.GenericCollection:
                        case CollectionKind.Enumerable:
                        case CollectionKind.GenericEnumerable:
                        case CollectionKind.List:
                        case CollectionKind.GenericList:
                            type = itemType.MakeArrayType();
                            isArray = true;
                            break;
                    }
                }
                string itemName = collectionContract.ItemName;
                string itemNs = collectionContract.StableName.Namespace;

                _objectLocal = _ilg.DeclareLocal(type, "objectDeserialized");
                if (!isArray)
                {
                    if (type.GetTypeInfo().IsValueType)
                    {
                        _ilg.Ldloca(_objectLocal);
                        _ilg.InitObj(type);
                    }
                    else
                    {
                        _ilg.New(constructor);
                        _ilg.Stloc(_objectLocal);
                        _ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, _objectLocal);
                    }
                }

                LocalBuilder size = _ilg.DeclareLocal(Globals.TypeOfInt, "arraySize");
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetArraySizeMethod);
                _ilg.Stloc(size);

                LocalBuilder objectId = _ilg.DeclareLocal(Globals.TypeOfString, "objectIdRead");
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetObjectIdMethod);
                _ilg.Stloc(objectId);

                bool canReadPrimitiveArray = false;
                if (isArray && TryReadPrimitiveArray(type, itemType, size))
                {
                    canReadPrimitiveArray = true;
                    _ilg.IfNot();
                }

                _ilg.If(size, Cmp.EqualTo, -1);

                LocalBuilder growingCollection = null;
                if (isArray)
                {
                    growingCollection = _ilg.DeclareLocal(type, "growingCollection");
                    _ilg.NewArray(itemType, 32);
                    _ilg.Stloc(growingCollection);
                }
                LocalBuilder i = _ilg.DeclareLocal(Globals.TypeOfInt, "i");
                object forLoop = _ilg.For(i, 0, Int32.MaxValue);
                IsStartElement(_memberNamesArg, _memberNamespacesArg);
                _ilg.If();
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
                LocalBuilder value = ReadCollectionItem(collectionContract, itemType, itemName, itemNs);
                if (isArray)
                {
                    MethodInfo ensureArraySizeMethod = XmlFormatGeneratorStatics.EnsureArraySizeMethod.MakeGenericMethod(itemType);
                    _ilg.Call(null, ensureArraySizeMethod, growingCollection, i);
                    _ilg.Stloc(growingCollection);
                    _ilg.StoreArrayElement(growingCollection, i, value);
                }
                else
                    StoreCollectionValue(_objectLocal, value, collectionContract);
                _ilg.Else();
                IsEndElement();
                _ilg.If();
                _ilg.Break(forLoop);
                _ilg.Else();
                HandleUnexpectedItemInCollection(i);
                _ilg.EndIf();
                _ilg.EndIf();

                _ilg.EndFor();
                if (isArray)
                {
                    MethodInfo trimArraySizeMethod = XmlFormatGeneratorStatics.TrimArraySizeMethod.MakeGenericMethod(itemType);
                    _ilg.Call(null, trimArraySizeMethod, growingCollection, i);
                    _ilg.Stloc(_objectLocal);
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectWithIdMethod, objectId, _objectLocal);
                }
                _ilg.Else();

                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, size);
                if (isArray)
                {
                    _ilg.NewArray(itemType, size);
                    _ilg.Stloc(_objectLocal);
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, _objectLocal);
                }
                LocalBuilder j = _ilg.DeclareLocal(Globals.TypeOfInt, "j");
                _ilg.For(j, 0, size);
                IsStartElement(_memberNamesArg, _memberNamespacesArg);
                _ilg.If();
                LocalBuilder itemValue = ReadCollectionItem(collectionContract, itemType, itemName, itemNs);
                if (isArray)
                    _ilg.StoreArrayElement(_objectLocal, j, itemValue);
                else
                    StoreCollectionValue(_objectLocal, itemValue, collectionContract);
                _ilg.Else();
                HandleUnexpectedItemInCollection(j);
                _ilg.EndIf();
                _ilg.EndFor();
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.CheckEndOfArrayMethod, _xmlReaderArg, size, _memberNamesArg, _memberNamespacesArg);
                _ilg.EndIf();

                if (canReadPrimitiveArray)
                {
                    _ilg.Else();
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectWithIdMethod, objectId, _objectLocal);
                    _ilg.EndIf();
                }
            }

            private void ReadGetOnlyCollection(CollectionDataContract collectionContract)
            {
                Type type = collectionContract.UnderlyingType;
                Type itemType = collectionContract.ItemType;
                bool isArray = (collectionContract.Kind == CollectionKind.Array);
                string itemName = collectionContract.ItemName;
                string itemNs = collectionContract.StableName.Namespace;

                _objectLocal = _ilg.DeclareLocal(type, "objectDeserialized");
                _ilg.Load(_contextArg);
                _ilg.LoadMember(XmlFormatGeneratorStatics.GetCollectionMemberMethod);
                _ilg.ConvertValue(Globals.TypeOfObject, type);
                _ilg.Stloc(_objectLocal);

                //check that items are actually going to be deserialized into the collection
                IsStartElement(_memberNamesArg, _memberNamespacesArg);
                _ilg.If();
                _ilg.If(_objectLocal, Cmp.EqualTo, null);
                _ilg.Call(null, XmlFormatGeneratorStatics.ThrowNullValueReturnedForGetOnlyCollectionExceptionMethod, type);

                _ilg.Else();
                LocalBuilder size = _ilg.DeclareLocal(Globals.TypeOfInt, "arraySize");
                if (isArray)
                {
                    _ilg.Load(_objectLocal);
                    _ilg.Call(XmlFormatGeneratorStatics.GetArrayLengthMethod);
                    _ilg.Stloc(size);
                }

                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, _objectLocal);

                LocalBuilder i = _ilg.DeclareLocal(Globals.TypeOfInt, "i");
                object forLoop = _ilg.For(i, 0, Int32.MaxValue);
                IsStartElement(_memberNamesArg, _memberNamespacesArg);
                _ilg.If();
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
                LocalBuilder value = ReadCollectionItem(collectionContract, itemType, itemName, itemNs);
                if (isArray)
                {
                    _ilg.If(size, Cmp.EqualTo, i);
                    _ilg.Call(null, XmlFormatGeneratorStatics.ThrowArrayExceededSizeExceptionMethod, size, type);
                    _ilg.Else();
                    _ilg.StoreArrayElement(_objectLocal, i, value);
                    _ilg.EndIf();
                }
                else
                    StoreCollectionValue(_objectLocal, value, collectionContract);
                _ilg.Else();
                IsEndElement();
                _ilg.If();
                _ilg.Break(forLoop);
                _ilg.Else();
                HandleUnexpectedItemInCollection(i);
                _ilg.EndIf();
                _ilg.EndIf();
                _ilg.EndFor();
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.CheckEndOfArrayMethod, _xmlReaderArg, size, _memberNamesArg, _memberNamespacesArg);

                _ilg.EndIf();
                _ilg.EndIf();
            }

            private bool TryReadPrimitiveArray(Type type, Type itemType, LocalBuilder size)
            {
                PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
                if (primitiveContract == null)
                    return false;

                string readArrayMethod = null;
                switch (itemType.GetTypeCode())
                {
                    case TypeCode.Boolean:
                        readArrayMethod = "TryReadBooleanArray";
                        break;
                    case TypeCode.DateTime:
                        readArrayMethod = "TryReadDateTimeArray";
                        break;
                    case TypeCode.Decimal:
                        readArrayMethod = "TryReadDecimalArray";
                        break;
                    case TypeCode.Int32:
                        readArrayMethod = "TryReadInt32Array";
                        break;
                    case TypeCode.Int64:
                        readArrayMethod = "TryReadInt64Array";
                        break;
                    case TypeCode.Single:
                        readArrayMethod = "TryReadSingleArray";
                        break;
                    case TypeCode.Double:
                        readArrayMethod = "TryReadDoubleArray";
                        break;
                    default:
                        break;
                }
                if (readArrayMethod != null)
                {
                    _ilg.Load(_xmlReaderArg);
                    _ilg.Load(_contextArg);
                    _ilg.Load(_memberNamesArg);
                    _ilg.Load(_memberNamespacesArg);
                    _ilg.Load(size);
                    _ilg.Ldloca(_objectLocal);
                    _ilg.Call(typeof(XmlReaderDelegator).GetMethod(readArrayMethod, Globals.ScanAllMembers));
                    return true;
                }
                return false;
            }

            private LocalBuilder ReadCollectionItem(CollectionDataContract collectionContract, Type itemType, string itemName, string itemNs)
            {
                if (collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary)
                {
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.ResetAttributesMethod);
                    LocalBuilder value = _ilg.DeclareLocal(itemType, "valueRead");
                    _ilg.Load(_collectionContractArg);
                    _ilg.Call(XmlFormatGeneratorStatics.GetItemContractMethod);
                    _ilg.Load(_xmlReaderArg);
                    _ilg.Load(_contextArg);
                    _ilg.Call(XmlFormatGeneratorStatics.ReadXmlValueMethod);
                    _ilg.ConvertValue(Globals.TypeOfObject, itemType);
                    _ilg.Stloc(value);
                    return value;
                }
                else
                {
                    return ReadValue(itemType, itemName, itemNs);
                }
            }

            private void StoreCollectionValue(LocalBuilder collection, LocalBuilder value, CollectionDataContract collectionContract)
            {
                if (collectionContract.Kind == CollectionKind.GenericDictionary || collectionContract.Kind == CollectionKind.Dictionary)
                {
                    ClassDataContract keyValuePairContract = DataContract.GetDataContract(value.LocalType) as ClassDataContract;
                    if (keyValuePairContract == null)
                    {
                        DiagnosticUtility.DebugAssert("Failed to create contract for KeyValuePair type");
                    }
                    DataMember keyMember = keyValuePairContract.Members[0];
                    DataMember valueMember = keyValuePairContract.Members[1];
                    LocalBuilder pairKey = _ilg.DeclareLocal(keyMember.MemberType, keyMember.Name);
                    LocalBuilder pairValue = _ilg.DeclareLocal(valueMember.MemberType, valueMember.Name);
                    _ilg.LoadAddress(value);
                    _ilg.LoadMember(keyMember.MemberInfo);
                    _ilg.Stloc(pairKey);
                    _ilg.LoadAddress(value);
                    _ilg.LoadMember(valueMember.MemberInfo);
                    _ilg.Stloc(pairValue);

                    _ilg.Call(collection, collectionContract.AddMethod, pairKey, pairValue);
                    if (collectionContract.AddMethod.ReturnType != Globals.TypeOfVoid)
                        _ilg.Pop();
                }
                else
                {
                    _ilg.Call(collection, collectionContract.AddMethod, value);
                    if (collectionContract.AddMethod.ReturnType != Globals.TypeOfVoid)
                        _ilg.Pop();
                }
            }

            private void HandleUnexpectedItemInCollection(LocalBuilder iterator)
            {
                IsStartElement();
                _ilg.If();
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.SkipUnknownElementMethod, _xmlReaderArg);
                _ilg.Dec(iterator);
                _ilg.Else();
                ThrowUnexpectedStateException(XmlNodeType.Element);
                _ilg.EndIf();
            }

            private void IsStartElement(ArgBuilder nameArg, ArgBuilder nsArg)
            {
                _ilg.Call(_xmlReaderArg, XmlFormatGeneratorStatics.IsStartElementMethod2, nameArg, nsArg);
            }

            private void IsStartElement()
            {
                _ilg.Call(_xmlReaderArg, XmlFormatGeneratorStatics.IsStartElementMethod0);
            }

            private void IsEndElement()
            {
                _ilg.Load(_xmlReaderArg);
                _ilg.LoadMember(XmlFormatGeneratorStatics.NodeTypeProperty);
                _ilg.Load(XmlNodeType.EndElement);
                _ilg.Ceq();
            }

            private void ThrowUnexpectedStateException(XmlNodeType expectedState)
            {
                _ilg.Call(null, XmlFormatGeneratorStatics.CreateUnexpectedStateExceptionMethod, expectedState, _xmlReaderArg);
                _ilg.Throw();
            }

            private void ThrowValidationException(string msg, params object[] values)
            {
                {
                    _ilg.Load(msg);
                }
                ThrowValidationException();
            }

            private void ThrowValidationException()
            {
                //SerializationException is internal in SL and so cannot be directly invoked from DynamicMethod
                //So use helper function to create SerializationException
                _ilg.Call(XmlFormatGeneratorStatics.CreateSerializationExceptionMethod);
                _ilg.Throw();
            }
#endif
        }

        static internal object UnsafeGetUninitializedObject(Type type)
        {
#if !NET_NATIVE
            if (type.GetTypeInfo().IsValueType)
            {
                  return Activator.CreateInstance(type);
            }

            const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;
            bool hasDefaultConstructor = s_typeHasDefaultConstructorMap.GetOrAdd(type, t => t.GetConstructor(Flags, Array.Empty<Type>()) != null);
            return hasDefaultConstructor ? Activator.CreateInstance(type) : TryGetUninitializedObjectWithFormatterServices(type) ?? Activator.CreateInstance(type);
#else
            return RuntimeAugments.NewObject(type.TypeHandle);
#endif
        }

        /// <SecurityNote>
        /// Critical - Elevates by calling GetUninitializedObject which has a LinkDemand
        /// Safe - marked as such so that it's callable from transparent generated IL. Takes id as parameter which 
        ///        is guaranteed to be in internal serialization cache. 
        /// </SecurityNote>
#if USE_REFEMIT
        public static object UnsafeGetUninitializedObject(int id)
#else
        static internal object UnsafeGetUninitializedObject(int id)
#endif
        {
            var type = DataContract.GetDataContractForInitialization(id).TypeForInitialization;
            return UnsafeGetUninitializedObject(type);
        }

        static internal object TryGetUninitializedObjectWithFormatterServices(Type type) =>
            s_getUninitializedObjectDelegate?.Invoke(type);
    }
}
