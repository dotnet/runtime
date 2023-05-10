// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.Serialization.DataContracts;
using System.Security;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal delegate object JsonFormatClassReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson? context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString[]? memberNames);
    internal delegate object JsonFormatCollectionReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract);
    internal delegate void JsonFormatGetOnlyCollectionReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract);

    internal sealed class JsonFormatReaderGenerator
    {
        private readonly CriticalHelper _helper;

        public JsonFormatReaderGenerator()
        {
            _helper = new CriticalHelper();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonFormatClassReaderDelegate GenerateClassReader(ClassDataContract classContract)
        {
            return _helper.GenerateClassReader(classContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonFormatCollectionReaderDelegate GenerateCollectionReader(CollectionDataContract collectionContract)
        {
            return _helper.GenerateCollectionReader(collectionContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonFormatGetOnlyCollectionReaderDelegate GenerateGetOnlyCollectionReader(CollectionDataContract collectionContract)
        {
            return _helper.GenerateGetOnlyCollectionReader(collectionContract);
        }

        private sealed class CriticalHelper
        {
            private CodeGenerator _ilg = null!; // initialized in GenerateXXXReader
            private LocalBuilder _objectLocal = null!; // initialized in CreateObject and ReadCollection
            private Type? _objectType;
            private ArgBuilder _xmlReaderArg = null!; // initialized in InitArgs
            private ArgBuilder _contextArg = null!; // initialized in InitArgs
            private ArgBuilder _memberNamesArg = null!; // initialized in InitArgs
            private ArgBuilder? _collectionContractArg;
            private ArgBuilder _emptyDictionaryStringArg = null!; // initialized in InitArgs

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            public JsonFormatClassReaderDelegate GenerateClassReader(ClassDataContract classContract)
            {
                _ilg = new CodeGenerator();
                bool memberAccessFlag = classContract.RequiresMemberAccessForRead(null);
                try
                {
                    BeginMethod(_ilg, "Read" + DataContract.SanitizeTypeName(classContract.XmlName!.Name) + "FromJson", typeof(JsonFormatClassReaderDelegate), memberAccessFlag);
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
                if (classContract.IsISerializable)
                    ReadISerializable(classContract);
                else
                    ReadClass(classContract);

                if (Globals.TypeOfIDeserializationCallback.IsAssignableFrom(classContract.UnderlyingType))
                {
                    _ilg.Call(_objectLocal, JsonFormatGeneratorStatics.OnDeserializationMethod, null);
                }

                InvokeOnDeserialized(classContract);
                if (!InvokeFactoryMethod(classContract))
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
                    else if (classContract.UnderlyingType == Globals.TypeOfMemoryStreamAdapter)
                    {
                        _ilg.ConvertValue(_objectLocal.LocalType, Globals.TypeOfMemoryStreamAdapter);
                        _ilg.Call(XmlFormatGeneratorStatics.GetMemoryStreamMethod);
                        _ilg.ConvertValue(Globals.TypeOfMemoryStream, _ilg.CurrentMethod.ReturnType);
                    }
                    else
                    {
                        _ilg.ConvertValue(_objectLocal.LocalType, _ilg.CurrentMethod.ReturnType);
                    }
                }
                return (JsonFormatClassReaderDelegate)_ilg.EndMethod();
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            public JsonFormatCollectionReaderDelegate GenerateCollectionReader(CollectionDataContract collectionContract)
            {
                _ilg = GenerateCollectionReaderHelper(collectionContract, false /*isGetOnlyCollection*/);
                ReadCollection(collectionContract);
                _ilg.Load(_objectLocal);
                _ilg.ConvertValue(_objectLocal.LocalType, _ilg.CurrentMethod.ReturnType);
                return (JsonFormatCollectionReaderDelegate)_ilg.EndMethod();
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            public JsonFormatGetOnlyCollectionReaderDelegate GenerateGetOnlyCollectionReader(CollectionDataContract collectionContract)
            {
                _ilg = GenerateCollectionReaderHelper(collectionContract, true /*isGetOnlyCollection*/);
                ReadGetOnlyCollection(collectionContract);
                return (JsonFormatGetOnlyCollectionReaderDelegate)_ilg.EndMethod();
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            private CodeGenerator GenerateCollectionReaderHelper(CollectionDataContract collectionContract, bool isGetOnlyCollection)
            {
                _ilg = new CodeGenerator();
                bool memberAccessFlag = collectionContract.RequiresMemberAccessForRead(null);
                try
                {
                    if (isGetOnlyCollection)
                    {
                        BeginMethod(_ilg, "Read" + DataContract.SanitizeTypeName(collectionContract.XmlName.Name) + "FromJson" + "IsGetOnly", typeof(JsonFormatGetOnlyCollectionReaderDelegate), memberAccessFlag);
                    }
                    else
                    {
                        BeginMethod(_ilg, "Read" + DataContract.SanitizeTypeName(collectionContract.XmlName.Name) + "FromJson", typeof(JsonFormatCollectionReaderDelegate), memberAccessFlag);
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

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            private static void BeginMethod(CodeGenerator ilg, string methodName, Type delegateType, bool allowPrivateMemberAccess)
            {
                MethodInfo signature = CodeGenerator.GetInvokeMethod(delegateType);
                ParameterInfo[] parameters = signature.GetParameters();
                Type[] paramTypes = new Type[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                    paramTypes[i] = parameters[i].ParameterType;

                DynamicMethod dynamicMethod = new DynamicMethod(methodName, signature.ReturnType, paramTypes, typeof(JsonFormatReaderGenerator).Module, allowPrivateMemberAccess);
                ilg.BeginMethod(dynamicMethod, delegateType, paramTypes);
            }

            private void InitArgs()
            {
                _xmlReaderArg = _ilg.GetArg(0);
                _contextArg = _ilg.GetArg(1);
                _emptyDictionaryStringArg = _ilg.GetArg(2);
                _memberNamesArg = _ilg.GetArg(3);
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void CreateObject(ClassDataContract classContract)
            {
                _objectType = classContract.UnderlyingType;
                Type type = classContract.ObjectType;

                _objectLocal = _ilg.DeclareLocal(type);

                if (classContract.UnderlyingType == Globals.TypeOfDBNull)
                {
                    _ilg.LoadMember(Globals.TypeOfDBNull.GetField("Value")!);
                    _ilg.Stloc(_objectLocal);
                }
                else if (classContract.IsNonAttributedType)
                {
                    if (type.IsValueType)
                    {
                        _ilg.Ldloca(_objectLocal);
                        _ilg.InitObj(type);
                    }
                    else
                    {
                        _ilg.New(classContract.GetNonAttributedTypeConstructor()!);
                        _ilg.Stloc(_objectLocal);
                    }
                }
                else
                {
                    _ilg.Call(null, JsonFormatGeneratorStatics.GetUninitializedObjectMethod, classContract.TypeForInitialization);
                    _ilg.ConvertValue(Globals.TypeOfObject, type);
                    _ilg.Stloc(_objectLocal);
                }
            }

            private void InvokeOnDeserializing(ClassDataContract classContract)
            {
                if (classContract.BaseClassContract != null)
                    InvokeOnDeserializing(classContract.BaseClassContract);
                if (classContract.OnDeserializing != null)
                {
                    _ilg.LoadAddress(_objectLocal);
                    _ilg.ConvertAddress(_objectLocal.LocalType, _objectType!);
                    _ilg.Load(_contextArg);
                    _ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
                    _ilg.Call(classContract.OnDeserializing);
                }
            }

            private void InvokeOnDeserialized(ClassDataContract classContract)
            {
                if (classContract.BaseClassContract != null)
                    InvokeOnDeserialized(classContract.BaseClassContract);
                if (classContract.OnDeserialized != null)
                {
                    _ilg.LoadAddress(_objectLocal);
                    _ilg.ConvertAddress(_objectLocal.LocalType, _objectType!);
                    _ilg.Load(_contextArg);
                    _ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
                    _ilg.Call(classContract.OnDeserialized);
                }
            }

            private static bool HasFactoryMethod(ClassDataContract classContract)
            {
                return Globals.TypeOfIObjectReference.IsAssignableFrom(classContract.UnderlyingType);
            }

            private bool InvokeFactoryMethod(ClassDataContract classContract)
            {
                if (HasFactoryMethod(classContract))
                {
                    _ilg.Load(_contextArg);
                    _ilg.LoadAddress(_objectLocal);
                    _ilg.ConvertAddress(_objectLocal.LocalType, Globals.TypeOfIObjectReference);
                    _ilg.Load(Globals.NewObjectId);
                    _ilg.Call(XmlFormatGeneratorStatics.GetRealObjectMethod);
                    _ilg.ConvertValue(Globals.TypeOfObject, _ilg.CurrentMethod.ReturnType);
                    return true;
                }

                return false;
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void ReadClass(ClassDataContract classContract)
            {
                if (classContract.HasExtensionData)
                {
                    LocalBuilder extensionDataLocal = _ilg.DeclareLocal(Globals.TypeOfExtensionDataObject);
                    _ilg.New(JsonFormatGeneratorStatics.ExtensionDataObjectCtor);
                    _ilg.Store(extensionDataLocal);
                    ReadMembers(classContract, extensionDataLocal);

                    ClassDataContract? currentContract = classContract;
                    while (currentContract != null)
                    {
                        MethodInfo? extensionDataSetMethod = currentContract.ExtensionDataSetMethod;
                        if (extensionDataSetMethod != null)
                            _ilg.Call(_objectLocal, extensionDataSetMethod, extensionDataLocal);
                        currentContract = currentContract.BaseClassContract;
                    }
                }
                else
                {
                    ReadMembers(classContract, null /*extensionDataLocal*/);
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void ReadMembers(ClassDataContract classContract, LocalBuilder? extensionDataLocal)
            {
                int memberCount = classContract.MemberNames!.Length;
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, memberCount);

                BitFlagsGenerator expectedElements = new BitFlagsGenerator(memberCount, _ilg);
                byte[] requiredElements = new byte[expectedElements.GetLocalCount()];
                SetRequiredElements(classContract, requiredElements);
                SetExpectedElements(expectedElements, 0 /*startIndex*/);

                LocalBuilder memberIndexLocal = _ilg.DeclareLocal(Globals.TypeOfInt, -1);
                Label throwDuplicateMemberLabel = _ilg.DefineLabel();
                Label throwMissingRequiredMembersLabel = _ilg.DefineLabel();

                object forReadElements = _ilg.For(null, null, null);
                _ilg.Call(null, XmlFormatGeneratorStatics.MoveToNextElementMethod, _xmlReaderArg);
                _ilg.IfFalseBreak(forReadElements);
                _ilg.Call(_contextArg, JsonFormatGeneratorStatics.GetJsonMemberIndexMethod, _xmlReaderArg, _memberNamesArg, memberIndexLocal, extensionDataLocal);
                if (memberCount > 0)
                {
                    Label[] memberLabels = _ilg.Switch(memberCount);
                    ReadMembers(classContract, expectedElements, memberLabels, throwDuplicateMemberLabel, memberIndexLocal);
                    _ilg.EndSwitch();
                }
                else
                {
                    _ilg.Pop();
                }
                _ilg.EndFor();
                CheckRequiredElements(expectedElements, requiredElements, throwMissingRequiredMembersLabel);
                Label endOfTypeLabel = _ilg.DefineLabel();
                _ilg.Br(endOfTypeLabel);

                _ilg.MarkLabel(throwDuplicateMemberLabel);
                _ilg.Call(null, JsonFormatGeneratorStatics.ThrowDuplicateMemberExceptionMethod, _objectLocal, _memberNamesArg, memberIndexLocal);

                _ilg.MarkLabel(throwMissingRequiredMembersLabel);
                _ilg.Load(_objectLocal);
                _ilg.ConvertValue(_objectLocal.LocalType, Globals.TypeOfObject);
                _ilg.Load(_memberNamesArg);
                expectedElements.LoadArray();
                LoadArray(requiredElements);
                _ilg.Call(JsonFormatGeneratorStatics.ThrowMissingRequiredMembersMethod);

                _ilg.MarkLabel(endOfTypeLabel);
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private int ReadMembers(ClassDataContract classContract, BitFlagsGenerator expectedElements,
                Label[] memberLabels, Label throwDuplicateMemberLabel, LocalBuilder memberIndexLocal)
            {
                int memberCount = (classContract.BaseClassContract == null) ? 0 :
                    ReadMembers(classContract.BaseClassContract, expectedElements, memberLabels, throwDuplicateMemberLabel, memberIndexLocal);

                for (int i = 0; i < classContract.Members!.Count; i++, memberCount++)
                {
                    DataMember dataMember = classContract.Members[i];
                    Type memberType = dataMember.MemberType;
                    _ilg.Case(memberLabels[memberCount]);
                    _ilg.Set(memberIndexLocal, memberCount);
                    expectedElements.Load(memberCount);
                    _ilg.Brfalse(throwDuplicateMemberLabel);
                    LocalBuilder? value;
                    if (dataMember.IsGetOnlyCollection)
                    {
                        _ilg.LoadAddress(_objectLocal);
                        _ilg.LoadMember(dataMember.MemberInfo);
                        value = _ilg.DeclareLocal(memberType);
                        _ilg.Stloc(value);
                        _ilg.Call(_contextArg, XmlFormatGeneratorStatics.StoreCollectionMemberInfoMethod, value);
                        ReadValue(memberType, dataMember.Name);
                    }
                    else
                    {
                        _ilg.Call(_contextArg, XmlFormatGeneratorStatics.ResetCollectionMemberInfoMethod);
                        value = ReadValue(memberType, dataMember.Name);
                        _ilg.LoadAddress(_objectLocal);
                        _ilg.ConvertAddress(_objectLocal.LocalType, _objectType!);
                        _ilg.Ldloc(value);
                        _ilg.StoreMember(dataMember.MemberInfo);
                    }
                    ResetExpectedElements(expectedElements, memberCount);
                    _ilg.EndCase();
                }
                return memberCount;
            }

            private void CheckRequiredElements(BitFlagsGenerator expectedElements, byte[] requiredElements, Label throwMissingRequiredMembersLabel)
            {
                for (int i = 0; i < requiredElements.Length; i++)
                {
                    _ilg.Load(expectedElements.GetLocal(i));
                    _ilg.Load(requiredElements[i]);
                    _ilg.And();
                    _ilg.Load(0);
                    _ilg.Ceq();
                    _ilg.Brfalse(throwMissingRequiredMembersLabel);
                }
            }

            private void LoadArray(byte[] array)
            {
                LocalBuilder localArray = _ilg.DeclareLocal(Globals.TypeOfByteArray);
                _ilg.NewArray(typeof(byte), array.Length);
                _ilg.Store(localArray);
                for (int i = 0; i < array.Length; i++)
                {
                    _ilg.StoreArrayElement(localArray, i, array[i]);
                }
                _ilg.Load(localArray);
            }

            private int SetRequiredElements(ClassDataContract contract, byte[] requiredElements)
            {
                int memberCount = (contract.BaseClassContract == null) ? 0 :
                    SetRequiredElements(contract.BaseClassContract, requiredElements);
                List<DataMember> members = contract.Members!;
                for (int i = 0; i < members.Count; i++, memberCount++)
                {
                    if (members[i].IsRequired)
                    {
                        BitFlagsGenerator.SetBit(requiredElements, memberCount);
                    }
                }
                return memberCount;
            }

            private static void SetExpectedElements(BitFlagsGenerator expectedElements, int startIndex)
            {
                int memberCount = expectedElements.GetBitCount();
                for (int i = startIndex; i < memberCount; i++)
                {
                    expectedElements.Store(i, true);
                }
            }

            private static void ResetExpectedElements(BitFlagsGenerator expectedElements, int index)
            {
                expectedElements.Store(index, false);
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void ReadISerializable(ClassDataContract classContract)
            {
                ConstructorInfo? ctor = classContract.UnderlyingType.GetConstructor(Globals.ScanAllMembers, JsonFormatGeneratorStatics.SerInfoCtorArgs);
                if (ctor == null)
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.SerializationInfo_ConstructorNotFound, DataContract.GetClrTypeFullName(classContract.UnderlyingType)));
                _ilg.LoadAddress(_objectLocal);
                _ilg.ConvertAddress(_objectLocal.LocalType, _objectType!);
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.ReadSerializationInfoMethod, _xmlReaderArg, classContract.UnderlyingType);
                _ilg.Load(_contextArg);
                _ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
                _ilg.Call(ctor);
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private LocalBuilder ReadValue(Type type, string name)
            {
                LocalBuilder value = _ilg.DeclareLocal(type);
                LocalBuilder? nullableValue = null;
                int nullables = 0;
                while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
                {
                    nullables++;
                    type = type.GetGenericArguments()[0];
                }

                PrimitiveDataContract? primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
                if ((primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject) || nullables != 0 || type.IsValueType)
                {
                    LocalBuilder objectId = _ilg.DeclareLocal(Globals.TypeOfString);
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
                    else if (type.IsValueType)
                        ThrowSerializationException(SR.Format(SR.ValueTypeCannotBeNull, DataContract.GetClrTypeFullName(type)));
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
                    if (type.IsValueType)
                    {
                        _ilg.IfNotIsEmptyString(objectId);
                        ThrowSerializationException(SR.Format(SR.ValueTypeCannotHaveId, DataContract.GetClrTypeFullName(type)));
                        _ilg.EndIf();
                    }
                    if (nullables != 0)
                    {
                        nullableValue = value;
                        value = _ilg.DeclareLocal(type);
                    }

                    if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject)
                    {
                        _ilg.Call(_xmlReaderArg, primitiveContract.XmlFormatReaderMethod);
                        _ilg.Stloc(value);
                        if (!type.IsValueType)
                            _ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, value);
                    }
                    else
                    {
                        InternalDeserialize(value, type, name);
                    }
                    // Deserialize ref
                    _ilg.Else();
                    if (type.IsValueType)
                        ThrowSerializationException(SR.Format(SR.ValueTypeCannotHaveRef, DataContract.GetClrTypeFullName(type)));
                    else
                    {
                        _ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetExistingObjectMethod, objectId, type, name, string.Empty);
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
                    InternalDeserialize(value, type, name);
                }

                return value;
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void InternalDeserialize(LocalBuilder value, Type type, string name)
            {
                _ilg.Load(_contextArg);
                _ilg.Load(_xmlReaderArg);
                Type declaredType = type;
                _ilg.Load(DataContract.GetId(declaredType.TypeHandle));
                _ilg.Ldtoken(declaredType);
                _ilg.Load(name);
                // Empty namespace
                _ilg.Load(string.Empty);
                _ilg.Call(XmlFormatGeneratorStatics.InternalDeserializeMethod);

                if (type.IsPointer)
                {
                    _ilg.Call(JsonFormatGeneratorStatics.UnboxPointer);
                }
                else
                {
                    _ilg.ConvertValue(Globals.TypeOfObject, type);
                }
                _ilg.Stloc(value);
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void WrapNullableObject(LocalBuilder innerValue, LocalBuilder outerValue, int nullables)
            {
                Type innerType = innerValue.LocalType, outerType = outerValue.LocalType;
                _ilg.LoadAddress(outerValue);
                _ilg.Load(innerValue);
                for (int i = 1; i < nullables; i++)
                {
                    Type type = Globals.TypeOfNullable.MakeGenericType(innerType);
                    _ilg.New(type.GetConstructor(new Type[] { innerType })!);
                    innerType = type;
                }
                _ilg.Call(outerType.GetConstructor(new Type[] { innerType })!);
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void ReadCollection(CollectionDataContract collectionContract)
            {
                Type type = collectionContract.UnderlyingType;
                Type itemType = collectionContract.ItemType;
                bool isArray = (collectionContract.Kind == CollectionKind.Array);
                ConstructorInfo constructor = collectionContract.Constructor!;
                if (type.IsInterface)
                {
                    switch (collectionContract.Kind)
                    {
                        case CollectionKind.GenericDictionary:
                            type = Globals.TypeOfDictionaryGeneric.MakeGenericType(itemType.GetGenericArguments());
                            constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes)!;
                            break;
                        case CollectionKind.Dictionary:
                            type = Globals.TypeOfHashtable;
                            constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes)!;
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

                _objectLocal = _ilg.DeclareLocal(type);
                if (!isArray)
                {
                    if (type.IsValueType)
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

                bool canReadSimpleDictionary = collectionContract.Kind == CollectionKind.Dictionary ||
                                               collectionContract.Kind == CollectionKind.GenericDictionary;
                if (canReadSimpleDictionary)
                {
                    _ilg.Load(_contextArg);
                    _ilg.LoadMember(JsonFormatGeneratorStatics.UseSimpleDictionaryFormatReadProperty);
                    _ilg.If();

                    ReadSimpleDictionary(collectionContract, itemType);

                    _ilg.Else();
                }

                LocalBuilder objectId = _ilg.DeclareLocal(Globals.TypeOfString);
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetObjectIdMethod);
                _ilg.Stloc(objectId);

                bool canReadPrimitiveArray = false;
                if (isArray && TryReadPrimitiveArray(itemType))
                {
                    canReadPrimitiveArray = true;
                    _ilg.IfNot();
                }

                LocalBuilder? growingCollection = null;
                if (isArray)
                {
                    growingCollection = _ilg.DeclareLocal(type);
                    _ilg.NewArray(itemType, 32);
                    _ilg.Stloc(growingCollection);
                }
                LocalBuilder i = _ilg.DeclareLocal(Globals.TypeOfInt);
                object forLoop = _ilg.For(i, 0, int.MaxValue);
                // Empty namespace
                IsStartElement(_memberNamesArg, _emptyDictionaryStringArg);
                _ilg.If();
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
                LocalBuilder value = ReadCollectionItem(collectionContract, itemType);
                if (isArray)
                {
                    Debug.Assert(growingCollection != null);
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

                if (canReadPrimitiveArray)
                {
                    _ilg.Else();
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectWithIdMethod, objectId, _objectLocal);
                    _ilg.EndIf();
                }

                if (canReadSimpleDictionary)
                {
                    _ilg.EndIf();
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void ReadSimpleDictionary(CollectionDataContract collectionContract, Type keyValueType)
            {
                Type[] keyValueTypes = keyValueType.GetGenericArguments();
                Type keyType = keyValueTypes[0];
                Type valueType = keyValueTypes[1];

                int keyTypeNullableDepth = 0;
                Type keyTypeOriginal = keyType;
                while (keyType.IsGenericType && keyType.GetGenericTypeDefinition() == Globals.TypeOfNullable)
                {
                    keyTypeNullableDepth++;
                    keyType = keyType.GetGenericArguments()[0];
                }

                ClassDataContract keyValueDataContract = (ClassDataContract)collectionContract.ItemContract;
                DataContract keyDataContract = keyValueDataContract.Members![0].MemberTypeContract;

                KeyParseMode keyParseMode = KeyParseMode.Fail;

                if (keyType == Globals.TypeOfString || keyType == Globals.TypeOfObject)
                {
                    keyParseMode = KeyParseMode.AsString;
                }
                else if (keyType.IsEnum)
                {
                    keyParseMode = KeyParseMode.UsingParseEnum;
                }
                else if (keyDataContract.ParseMethod != null)
                {
                    keyParseMode = KeyParseMode.UsingCustomParse;
                }

                if (keyParseMode == KeyParseMode.Fail)
                {
                    ThrowSerializationException(
                        SR.Format(
                            SR.KeyTypeCannotBeParsedInSimpleDictionary,
                                DataContract.GetClrTypeFullName(collectionContract.UnderlyingType),
                                DataContract.GetClrTypeFullName(keyType)));
                }
                else
                {
                    LocalBuilder nodeType = _ilg.DeclareLocal(typeof(XmlNodeType));

                    _ilg.BeginWhileCondition();
                    _ilg.Call(_xmlReaderArg, JsonFormatGeneratorStatics.MoveToContentMethod);
                    _ilg.Stloc(nodeType);
                    _ilg.Load(nodeType);
                    _ilg.Load(XmlNodeType.EndElement);
                    _ilg.BeginWhileBody(Cmp.NotEqualTo);

                    _ilg.Load(nodeType);
                    _ilg.Load(XmlNodeType.Element);
                    _ilg.If(Cmp.NotEqualTo);
                    ThrowUnexpectedStateException(XmlNodeType.Element);
                    _ilg.EndIf();

                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);

                    if (keyParseMode == KeyParseMode.UsingParseEnum)
                    {
                        _ilg.Load(keyType);
                    }

                    _ilg.Load(_xmlReaderArg);
                    _ilg.Call(JsonFormatGeneratorStatics.GetJsonMemberNameMethod);

                    if (keyParseMode == KeyParseMode.UsingParseEnum)
                    {
                        _ilg.Call(JsonFormatGeneratorStatics.ParseEnumMethod);
                        _ilg.ConvertValue(Globals.TypeOfObject, keyType);
                    }
                    else if (keyParseMode == KeyParseMode.UsingCustomParse)
                    {
                        _ilg.Call(keyDataContract.ParseMethod!);
                    }
                    LocalBuilder pairKey = _ilg.DeclareLocal(keyType);
                    _ilg.Stloc(pairKey);
                    if (keyTypeNullableDepth > 0)
                    {
                        LocalBuilder pairKeyNullable = _ilg.DeclareLocal(keyTypeOriginal);
                        WrapNullableObject(pairKey, pairKeyNullable, keyTypeNullableDepth);
                        pairKey = pairKeyNullable;
                    }

                    LocalBuilder pairValue = ReadValue(valueType, string.Empty);
                    StoreKeyValuePair(_objectLocal, collectionContract, pairKey, pairValue);

                    _ilg.EndWhile();
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void ReadGetOnlyCollection(CollectionDataContract collectionContract)
            {
                Type type = collectionContract.UnderlyingType;
                Type itemType = collectionContract.ItemType;
                bool isArray = (collectionContract.Kind == CollectionKind.Array);
                LocalBuilder size = _ilg.DeclareLocal(Globals.TypeOfInt);

                _objectLocal = _ilg.DeclareLocal(type);
                _ilg.Load(_contextArg);
                _ilg.LoadMember(XmlFormatGeneratorStatics.GetCollectionMemberMethod);
                _ilg.ConvertValue(Globals.TypeOfObject, type);
                _ilg.Stloc(_objectLocal);

                bool canReadSimpleDictionary = collectionContract.Kind == CollectionKind.Dictionary ||
                                               collectionContract.Kind == CollectionKind.GenericDictionary;
                if (canReadSimpleDictionary)
                {
                    _ilg.Load(_contextArg);
                    _ilg.LoadMember(JsonFormatGeneratorStatics.UseSimpleDictionaryFormatReadProperty);
                    _ilg.If();

                    if (!type.IsValueType)
                    {
                        _ilg.If(_objectLocal, Cmp.EqualTo, null);
                        _ilg.Call(null, XmlFormatGeneratorStatics.ThrowNullValueReturnedForGetOnlyCollectionExceptionMethod, type);
                        _ilg.EndIf();
                    }

                    ReadSimpleDictionary(collectionContract, itemType);

                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.CheckEndOfArrayMethod, _xmlReaderArg, size, _memberNamesArg, _emptyDictionaryStringArg);

                    _ilg.Else();
                }

                //check that items are actually going to be deserialized into the collection
                IsStartElement(_memberNamesArg, _emptyDictionaryStringArg);
                _ilg.If();

                if (!type.IsValueType)
                {
                    _ilg.If(_objectLocal, Cmp.EqualTo, null);
                    _ilg.Call(null, XmlFormatGeneratorStatics.ThrowNullValueReturnedForGetOnlyCollectionExceptionMethod, type);
                    _ilg.EndIf();
                }

                if (isArray)
                {
                    _ilg.Load(_objectLocal);
                    _ilg.Call(XmlFormatGeneratorStatics.GetArrayLengthMethod);
                    _ilg.Stloc(size);
                }

                LocalBuilder i = _ilg.DeclareLocal(Globals.TypeOfInt);
                object forLoop = _ilg.For(i, 0, int.MaxValue);
                IsStartElement(_memberNamesArg, _emptyDictionaryStringArg);
                _ilg.If();
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
                LocalBuilder value = ReadCollectionItem(collectionContract, itemType);
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
                _ilg.Call(_contextArg, XmlFormatGeneratorStatics.CheckEndOfArrayMethod, _xmlReaderArg, size, _memberNamesArg, _emptyDictionaryStringArg);

                _ilg.EndIf();

                if (canReadSimpleDictionary)
                {
                    _ilg.EndIf();
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private bool TryReadPrimitiveArray(Type itemType)
            {
                PrimitiveDataContract? primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
                if (primitiveContract == null)
                    return false;

                string? readArrayMethod = null;
                switch (Type.GetTypeCode(itemType))
                {
                    case TypeCode.Boolean:
                        readArrayMethod = "TryReadBooleanArray";
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
                    case TypeCode.DateTime:
                        readArrayMethod = "TryReadJsonDateTimeArray";
                        break;
                    default:
                        break;
                }
                if (readArrayMethod != null)
                {
                    _ilg.Load(_xmlReaderArg);
                    _ilg.ConvertValue(typeof(XmlReaderDelegator), typeof(JsonReaderDelegator));
                    _ilg.Load(_contextArg);
                    _ilg.Load(_memberNamesArg);
                    // Empty namespace
                    _ilg.Load(_emptyDictionaryStringArg);
                    // -1 Array Size
                    _ilg.Load(-1);
                    _ilg.Ldloca(_objectLocal);
                    _ilg.Call(typeof(JsonReaderDelegator).GetMethod(readArrayMethod, Globals.ScanAllMembers)!);
                    return true;
                }
                return false;
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private LocalBuilder ReadCollectionItem(CollectionDataContract collectionContract, Type itemType)
            {
                if (collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary)
                {
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.ResetAttributesMethod);
                    LocalBuilder value = _ilg.DeclareLocal(itemType);
                    _ilg.Load(_collectionContractArg);
                    _ilg.Call(JsonFormatGeneratorStatics.GetItemContractMethod);
                    _ilg.Call(JsonFormatGeneratorStatics.GetRevisedItemContractMethod);
                    _ilg.Load(_xmlReaderArg);
                    _ilg.Load(_contextArg);
                    _ilg.Call(JsonFormatGeneratorStatics.ReadJsonValueMethod);
                    _ilg.ConvertValue(Globals.TypeOfObject, itemType);
                    _ilg.Stloc(value);
                    return value;
                }
                else
                {
                    return ReadValue(itemType, JsonGlobals.itemString);
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void StoreCollectionValue(LocalBuilder collection, LocalBuilder value, CollectionDataContract collectionContract)
            {
                if (collectionContract.Kind == CollectionKind.GenericDictionary || collectionContract.Kind == CollectionKind.Dictionary)
                {
                    ClassDataContract? keyValuePairContract = DataContract.GetDataContract(value.LocalType) as ClassDataContract;
                    if (keyValuePairContract == null)
                    {
                        Debug.Fail("Failed to create contract for KeyValuePair type");
                    }
                    DataMember keyMember = keyValuePairContract.Members![0];
                    DataMember valueMember = keyValuePairContract.Members[1];
                    LocalBuilder pairKey = _ilg.DeclareLocal(keyMember.MemberType);
                    LocalBuilder pairValue = _ilg.DeclareLocal(valueMember.MemberType);
                    _ilg.LoadAddress(value);
                    _ilg.LoadMember(keyMember.MemberInfo);
                    _ilg.Stloc(pairKey);
                    _ilg.LoadAddress(value);
                    _ilg.LoadMember(valueMember.MemberInfo);
                    _ilg.Stloc(pairValue);

                    StoreKeyValuePair(collection, collectionContract, pairKey, pairValue);
                }
                else
                {
                    Debug.Assert(collectionContract.AddMethod != null);

                    _ilg.Call(collection, collectionContract.AddMethod, value);
                    if (collectionContract.AddMethod.ReturnType != Globals.TypeOfVoid)
                        _ilg.Pop();
                }
            }

            private void StoreKeyValuePair(LocalBuilder collection, CollectionDataContract collectionContract, LocalBuilder pairKey, LocalBuilder pairValue)
            {
                Debug.Assert(collectionContract.AddMethod != null);

                _ilg.Call(collection, collectionContract.AddMethod, pairKey, pairValue);
                if (collectionContract.AddMethod.ReturnType != Globals.TypeOfVoid)
                    _ilg.Pop();
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
                _ilg.Call(_xmlReaderArg, JsonFormatGeneratorStatics.IsStartElementMethod2, nameArg, nsArg);
            }

            private void IsStartElement()
            {
                _ilg.Call(_xmlReaderArg, JsonFormatGeneratorStatics.IsStartElementMethod0);
            }

            private void IsEndElement()
            {
                _ilg.Load(_xmlReaderArg);
                _ilg.LoadMember(JsonFormatGeneratorStatics.NodeTypeProperty);
                _ilg.Load(XmlNodeType.EndElement);
                _ilg.Ceq();
            }

            private void ThrowUnexpectedStateException(XmlNodeType expectedState)
            {
                _ilg.Call(null, XmlFormatGeneratorStatics.CreateUnexpectedStateExceptionMethod, expectedState, _xmlReaderArg);
                _ilg.Throw();
            }

            private void ThrowSerializationException(string msg, params object[] values)
            {
                if (values != null && values.Length > 0)
                    _ilg.CallStringFormat(msg, values);
                else
                    _ilg.Load(msg);
                ThrowSerializationException();
            }

            private void ThrowSerializationException()
            {
                _ilg.New(JsonFormatGeneratorStatics.SerializationExceptionCtor);
                _ilg.Throw();
            }

            private enum KeyParseMode
            {
                Fail,
                AsString,
                UsingParseEnum,
                UsingCustomParse
            }
        }
    }
}
