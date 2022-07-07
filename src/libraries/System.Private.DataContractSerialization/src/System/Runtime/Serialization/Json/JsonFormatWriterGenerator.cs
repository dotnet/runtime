// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal delegate void JsonFormatClassWriterDelegate(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, ClassDataContract dataContract, XmlDictionaryString[]? memberNames);
    internal delegate void JsonFormatCollectionWriterDelegate(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, CollectionDataContract dataContract);

    internal sealed class JsonFormatWriterGenerator
    {
        private readonly CriticalHelper _helper;

        public JsonFormatWriterGenerator()
        {
            _helper = new CriticalHelper();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal JsonFormatClassWriterDelegate GenerateClassWriter(ClassDataContract classContract)
        {
            return _helper.GenerateClassWriter(classContract);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal JsonFormatCollectionWriterDelegate GenerateCollectionWriter(CollectionDataContract collectionContract)
        {
            return _helper.GenerateCollectionWriter(collectionContract);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The trimmer will never remove the Invoke method from delegates.")]
        internal static MethodInfo GetInvokeMethod(Type delegateType)
        {
            Debug.Assert(typeof(Delegate).IsAssignableFrom(delegateType));
            return delegateType.GetMethod("Invoke")!;
        }

        private sealed class CriticalHelper
        {
            private CodeGenerator _ilg = null!; // initialized in GenerateXXXWriter
            private ArgBuilder _xmlWriterArg = null!; // initialized in InitArgs
            private ArgBuilder _contextArg = null!; // initialized in InitArgs
            private ArgBuilder _dataContractArg = null!; // initialized in InitArgs
            private LocalBuilder _objectLocal = null!; // initialized in InitArgs

            // Used for classes
            private ArgBuilder? _memberNamesArg;
            private int _typeIndex = 1;
            private int _childElementIndex;

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal JsonFormatClassWriterDelegate GenerateClassWriter(ClassDataContract classContract)
            {
                _ilg = new CodeGenerator();
                bool memberAccessFlag = classContract.RequiresMemberAccessForWrite(null);
                try
                {
                    BeginMethod(_ilg, "Write" + DataContract.SanitizeTypeName(classContract.StableName.Name) + "ToJson", typeof(JsonFormatClassWriterDelegate), memberAccessFlag);
                }
                catch (SecurityException securityException)
                {
                    if (memberAccessFlag)
                    {
                        classContract.RequiresMemberAccessForWrite(securityException);
                    }
                    else
                    {
                        throw;
                    }
                }
                InitArgs(classContract.UnderlyingType);
                _memberNamesArg = _ilg.GetArg(4);
                WriteClass(classContract);
                return (JsonFormatClassWriterDelegate)_ilg.EndMethod();
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal JsonFormatCollectionWriterDelegate GenerateCollectionWriter(CollectionDataContract collectionContract)
            {
                _ilg = new CodeGenerator();
                bool memberAccessFlag = collectionContract.RequiresMemberAccessForWrite(null);
                try
                {
                    BeginMethod(_ilg, "Write" + DataContract.SanitizeTypeName(collectionContract.StableName.Name) + "ToJson", typeof(JsonFormatCollectionWriterDelegate), memberAccessFlag);
                }
                catch (SecurityException securityException)
                {
                    if (memberAccessFlag)
                    {
                        collectionContract.RequiresMemberAccessForWrite(securityException);
                    }
                    else
                    {
                        throw;
                    }
                }
                InitArgs(collectionContract.UnderlyingType);
                if (collectionContract.IsReadOnlyContract)
                {
                    ThrowIfCannotSerializeReadOnlyTypes(collectionContract);
                }
                WriteCollection(collectionContract);
                return (JsonFormatCollectionWriterDelegate)_ilg.EndMethod();
            }

            private static void BeginMethod(CodeGenerator ilg, string methodName, Type delegateType, bool allowPrivateMemberAccess)
            {
                MethodInfo signature = GetInvokeMethod(delegateType);
                ParameterInfo[] parameters = signature.GetParameters();
                Type[] paramTypes = new Type[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                    paramTypes[i] = parameters[i].ParameterType;

                DynamicMethod dynamicMethod = new DynamicMethod(methodName, signature.ReturnType, paramTypes, typeof(JsonFormatWriterGenerator).Module, allowPrivateMemberAccess);
                ilg.BeginMethod(dynamicMethod, delegateType, methodName, paramTypes, allowPrivateMemberAccess);
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void InitArgs(Type objType)
            {
                _xmlWriterArg = _ilg.GetArg(0);
                _contextArg = _ilg.GetArg(2);
                _dataContractArg = _ilg.GetArg(3);

                _objectLocal = _ilg.DeclareLocal(objType, "objSerialized");
                ArgBuilder objectArg = _ilg.GetArg(1);
                _ilg.Load(objectArg);

                // Copy the data from the DataTimeOffset object passed in to the DateTimeOffsetAdapter.
                // DateTimeOffsetAdapter is used here for serialization purposes to bypass the ISerializable implementation
                // on DateTimeOffset; which does not work in partial trust.

                if (objType == Globals.TypeOfDateTimeOffsetAdapter)
                {
                    _ilg.ConvertValue(objectArg.ArgType, Globals.TypeOfDateTimeOffset);
                    _ilg.Call(XmlFormatGeneratorStatics.GetDateTimeOffsetAdapterMethod);
                }
                else if (objType == Globals.TypeOfMemoryStreamAdapter)
                {
                    _ilg.ConvertValue(objectArg.ArgType, Globals.TypeOfMemoryStream);
                    _ilg.Call(XmlFormatGeneratorStatics.GetMemoryStreamAdapterMethod);
                }
                //Copy the KeyValuePair<K,T> to a KeyValuePairAdapter<K,T>.
                else if (objType.IsGenericType && objType.GetGenericTypeDefinition() == Globals.TypeOfKeyValuePairAdapter)
                {
                    ClassDataContract dc = (ClassDataContract)DataContract.GetDataContract(objType);
                    _ilg.ConvertValue(objectArg.ArgType, Globals.TypeOfKeyValuePair.MakeGenericType(dc.KeyValuePairGenericArguments!));
                    _ilg.New(dc.KeyValuePairAdapterConstructorInfo!);
                }
                else
                {
                    _ilg.ConvertValue(objectArg.ArgType, objType);
                }
                _ilg.Stloc(_objectLocal);
            }

            private void ThrowIfCannotSerializeReadOnlyTypes(CollectionDataContract classContract)
            {
                ThrowIfCannotSerializeReadOnlyTypes(XmlFormatGeneratorStatics.CollectionSerializationExceptionMessageProperty);
            }

            private void ThrowIfCannotSerializeReadOnlyTypes(PropertyInfo serializationExceptionMessageProperty)
            {
                _ilg.Load(_contextArg);
                _ilg.LoadMember(XmlFormatGeneratorStatics.SerializeReadOnlyTypesProperty);
                _ilg.IfNot();
                _ilg.Load(_dataContractArg);
                _ilg.LoadMember(serializationExceptionMessageProperty);
                _ilg.Load(null);
                _ilg.Call(XmlFormatGeneratorStatics.ThrowInvalidDataContractExceptionMethod);
                _ilg.EndIf();
            }

            private void InvokeOnSerializing(ClassDataContract classContract)
            {
                if (classContract.BaseContract != null)
                    InvokeOnSerializing(classContract.BaseContract);
                if (classContract.OnSerializing != null)
                {
                    _ilg.LoadAddress(_objectLocal);
                    _ilg.Load(_contextArg);
                    _ilg.Call(XmlFormatGeneratorStatics.GetStreamingContextMethod);
                    _ilg.Call(classContract.OnSerializing);
                }
            }

            private void InvokeOnSerialized(ClassDataContract classContract)
            {
                if (classContract.BaseContract != null)
                    InvokeOnSerialized(classContract.BaseContract);
                if (classContract.OnSerialized != null)
                {
                    _ilg.LoadAddress(_objectLocal);
                    _ilg.Load(_contextArg);
                    _ilg.Call(XmlFormatGeneratorStatics.GetStreamingContextMethod);
                    _ilg.Call(classContract.OnSerialized);
                }
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void WriteClass(ClassDataContract classContract)
            {
                InvokeOnSerializing(classContract);
                if (classContract.IsISerializable)
                {
                    _ilg.Call(_contextArg, JsonFormatGeneratorStatics.WriteJsonISerializableMethod, _xmlWriterArg, _objectLocal);
                }
                else
                {
                    if (classContract.HasExtensionData)
                    {
                        LocalBuilder extensionDataLocal = _ilg.DeclareLocal(Globals.TypeOfExtensionDataObject, "extensionData");
                        _ilg.Load(_objectLocal);
                        _ilg.ConvertValue(_objectLocal.LocalType, Globals.TypeOfIExtensibleDataObject);
                        _ilg.LoadMember(JsonFormatGeneratorStatics.ExtensionDataProperty);
                        _ilg.Store(extensionDataLocal);
                        _ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteExtensionDataMethod, _xmlWriterArg, extensionDataLocal, -1);
                        WriteMembers(classContract, extensionDataLocal, classContract);
                    }
                    else
                    {
                        WriteMembers(classContract, null, classContract);
                    }
                }

                InvokeOnSerialized(classContract);
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private int WriteMembers(ClassDataContract classContract, LocalBuilder? extensionDataLocal, ClassDataContract derivedMostClassContract)
            {
                int memberCount = (classContract.BaseContract == null) ? 0 :
                    WriteMembers(classContract.BaseContract, extensionDataLocal, derivedMostClassContract);

                int classMemberCount = classContract.Members!.Count;
                _ilg.Call(thisObj: _contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, classMemberCount);

                for (int i = 0; i < classMemberCount; i++, memberCount++)
                {
                    DataMember member = classContract.Members[i];
                    Type memberType = member.MemberType;
                    LocalBuilder? memberValue = null;

                    _ilg.Load(_contextArg);
                    _ilg.Call(methodInfo: member.IsGetOnlyCollection ?
                        XmlFormatGeneratorStatics.StoreIsGetOnlyCollectionMethod :
                        XmlFormatGeneratorStatics.ResetIsGetOnlyCollectionMethod);

                    if (!member.EmitDefaultValue)
                    {
                        memberValue = LoadMemberValue(member);
                        _ilg.IfNotDefaultValue(memberValue);
                    }

                    bool requiresNameAttribute = DataContractJsonSerializer.CheckIfXmlNameRequiresMapping(classContract.MemberNames![i]);
                    if (requiresNameAttribute || !TryWritePrimitive(memberType, memberValue, member.MemberInfo, arrayItemIndex: null, name: null, nameIndex: i + _childElementIndex))
                    {
                        // Note: DataContractSerializer has member-conflict logic here to deal with the schema export
                        //       requirement that the same member can't be of two different types.
                        if (requiresNameAttribute)
                        {
                            _ilg.Call(thisObj: null, JsonFormatGeneratorStatics.WriteJsonNameWithMappingMethod, _xmlWriterArg, _memberNamesArg, i + _childElementIndex);
                        }
                        else
                        {
                            WriteStartElement(nameLocal: null, nameIndex: i + _childElementIndex);
                        }

                        memberValue ??= LoadMemberValue(member);
                        WriteValue(memberValue);
                        WriteEndElement();
                    }

                    if (classContract.HasExtensionData)
                    {
                        _ilg.Call(thisObj: _contextArg, XmlFormatGeneratorStatics.WriteExtensionDataMethod, _xmlWriterArg, extensionDataLocal, memberCount);
                    }

                    if (!member.EmitDefaultValue)
                    {
                        if (member.IsRequired)
                        {
                            _ilg.Else();
                            _ilg.Call(thisObj: null, XmlFormatGeneratorStatics.ThrowRequiredMemberMustBeEmittedMethod, member.Name, classContract.UnderlyingType);
                        }
                        _ilg.EndIf();
                    }
                }

                _typeIndex++;
                _childElementIndex += classMemberCount;
                return memberCount;
            }

            private LocalBuilder LoadMemberValue(DataMember member)
            {
                _ilg.LoadAddress(_objectLocal);
                _ilg.LoadMember(member.MemberInfo);
                LocalBuilder memberValue = _ilg.DeclareLocal(member.MemberType, member.Name + "Value");
                _ilg.Stloc(memberValue);
                return memberValue;
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void WriteCollection(CollectionDataContract collectionContract)
            {
                LocalBuilder itemName = _ilg.DeclareLocal(typeof(XmlDictionaryString), "itemName");
                _ilg.LoadMember(JsonFormatGeneratorStatics.CollectionItemNameProperty);
                _ilg.Store(itemName);

                if (collectionContract.Kind == CollectionKind.Array)
                {
                    Type itemType = collectionContract.ItemType;
                    LocalBuilder i = _ilg.DeclareLocal(Globals.TypeOfInt, "i");

                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementArrayCountMethod, _xmlWriterArg, _objectLocal);

                    if (!TryWritePrimitiveArray(collectionContract.UnderlyingType, itemType, _objectLocal, itemName))
                    {
                        WriteArrayAttribute();
                        _ilg.For(i, 0, _objectLocal);
                        if (!TryWritePrimitive(itemType, null /*value*/, null /*memberInfo*/, i /*arrayItemIndex*/, itemName, 0 /*nameIndex*/))
                        {
                            WriteStartElement(itemName, 0 /*nameIndex*/);
                            _ilg.LoadArrayElement(_objectLocal, i);
                            LocalBuilder memberValue = _ilg.DeclareLocal(itemType, "memberValue");
                            _ilg.Stloc(memberValue);
                            WriteValue(memberValue);
                            WriteEndElement();
                        }
                        _ilg.EndFor();
                    }
                }
                else
                {
                    Debug.Assert(collectionContract.GetEnumeratorMethod != null);

                    MethodInfo? incrementCollectionCountMethod = null;
                    switch (collectionContract.Kind)
                    {
                        case CollectionKind.Collection:
                        case CollectionKind.List:
                        case CollectionKind.Dictionary:
                            incrementCollectionCountMethod = XmlFormatGeneratorStatics.IncrementCollectionCountMethod;
                            break;
                        case CollectionKind.GenericCollection:
                        case CollectionKind.GenericList:
                            incrementCollectionCountMethod = MakeIncrementCollectionCountGenericMethod(collectionContract.ItemType);
                            break;
                        case CollectionKind.GenericDictionary:
                            incrementCollectionCountMethod = MakeIncrementCollectionCountGenericMethod(Globals.TypeOfKeyValuePair.MakeGenericType(collectionContract.ItemType.GetGenericArguments()));
                            break;
                    }
                    if (incrementCollectionCountMethod != null)
                    {
                        _ilg.Call(_contextArg, incrementCollectionCountMethod, _xmlWriterArg, _objectLocal);
                    }

                    bool isDictionary = false, isGenericDictionary = false;
                    Type? enumeratorType;
                    Type[]? keyValueTypes = null;
                    if (collectionContract.Kind == CollectionKind.GenericDictionary)
                    {
                        isGenericDictionary = true;
                        keyValueTypes = collectionContract.ItemType.GetGenericArguments();
                        enumeratorType = Globals.TypeOfGenericDictionaryEnumerator.MakeGenericType(keyValueTypes);
                    }
                    else if (collectionContract.Kind == CollectionKind.Dictionary)
                    {
                        isDictionary = true;
                        keyValueTypes = new Type[] { Globals.TypeOfObject, Globals.TypeOfObject };
                        enumeratorType = Globals.TypeOfDictionaryEnumerator;
                    }
                    else
                    {
                        enumeratorType = collectionContract.GetEnumeratorMethod.ReturnType;
                    }
                    MethodInfo? moveNextMethod = enumeratorType.GetMethod(Globals.MoveNextMethodName, BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
                    MethodInfo? getCurrentMethod = enumeratorType.GetMethod(Globals.GetCurrentMethodName, BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
                    if (moveNextMethod == null || getCurrentMethod == null)
                    {
                        if (enumeratorType.IsInterface)
                        {
                            moveNextMethod ??= JsonFormatGeneratorStatics.MoveNextMethod;
                            getCurrentMethod ??= JsonFormatGeneratorStatics.GetCurrentMethod;
                        }
                        else
                        {
                            Type ienumeratorInterface = Globals.TypeOfIEnumerator;
                            CollectionKind kind = collectionContract.Kind;
                            if (kind == CollectionKind.GenericDictionary || kind == CollectionKind.GenericCollection || kind == CollectionKind.GenericEnumerable)
                            {
                                Type[] interfaceTypes = enumeratorType.GetInterfaces();
                                foreach (Type interfaceType in interfaceTypes)
                                {
                                    if (interfaceType.IsGenericType
                                        && interfaceType.GetGenericTypeDefinition() == Globals.TypeOfIEnumeratorGeneric
                                        && interfaceType.GetGenericArguments()[0] == collectionContract.ItemType)
                                    {
                                        ienumeratorInterface = interfaceType;
                                        break;
                                    }
                                }
                            }

                            moveNextMethod ??= CollectionDataContract.GetTargetMethodWithName(Globals.MoveNextMethodName, enumeratorType, ienumeratorInterface)!;
                            getCurrentMethod ??= CollectionDataContract.GetTargetMethodWithName(Globals.GetCurrentMethodName, enumeratorType, ienumeratorInterface)!;
                        }
                    }
                    Type elementType = getCurrentMethod.ReturnType;
                    LocalBuilder currentValue = _ilg.DeclareLocal(elementType, "currentValue");

                    LocalBuilder enumerator = _ilg.DeclareLocal(enumeratorType, "enumerator");
                    _ilg.Call(_objectLocal, collectionContract.GetEnumeratorMethod);
                    if (isDictionary)
                    {
                        ConstructorInfo dictEnumCtor = enumeratorType.GetConstructor(Globals.ScanAllMembers, new Type[] { Globals.TypeOfIDictionaryEnumerator })!;
                        _ilg.ConvertValue(collectionContract.GetEnumeratorMethod.ReturnType, Globals.TypeOfIDictionaryEnumerator);
                        _ilg.New(dictEnumCtor);
                    }
                    else if (isGenericDictionary)
                    {
                        Debug.Assert(keyValueTypes != null);
                        Type ctorParam = Globals.TypeOfIEnumeratorGeneric.MakeGenericType(Globals.TypeOfKeyValuePair.MakeGenericType(keyValueTypes));
                        ConstructorInfo dictEnumCtor = enumeratorType.GetConstructor(Globals.ScanAllMembers, new Type[] { ctorParam })!;
                        _ilg.ConvertValue(collectionContract.GetEnumeratorMethod.ReturnType, ctorParam);
                        _ilg.New(dictEnumCtor);
                    }
                    _ilg.Stloc(enumerator);

                    bool canWriteSimpleDictionary = isDictionary || isGenericDictionary;
                    if (canWriteSimpleDictionary)
                    {
                        Debug.Assert(keyValueTypes != null);
                        Type genericDictionaryKeyValueType = Globals.TypeOfKeyValue.MakeGenericType(keyValueTypes);
                        PropertyInfo genericDictionaryKeyProperty = genericDictionaryKeyValueType.GetProperty(JsonGlobals.KeyString)!;
                        PropertyInfo genericDictionaryValueProperty = genericDictionaryKeyValueType.GetProperty(JsonGlobals.ValueString)!;

                        _ilg.Load(_contextArg);
                        _ilg.LoadMember(JsonFormatGeneratorStatics.UseSimpleDictionaryFormatWriteProperty);
                        _ilg.If();
                        WriteObjectAttribute();
                        LocalBuilder pairKey = _ilg.DeclareLocal(Globals.TypeOfString, "key");
                        LocalBuilder pairValue = _ilg.DeclareLocal(keyValueTypes[1], "value");
                        _ilg.ForEach(currentValue, elementType, enumeratorType, enumerator, getCurrentMethod);

                        _ilg.LoadAddress(currentValue);
                        _ilg.LoadMember(genericDictionaryKeyProperty);
                        _ilg.ToString(keyValueTypes[0]);
                        _ilg.Stloc(pairKey);

                        _ilg.LoadAddress(currentValue);
                        _ilg.LoadMember(genericDictionaryValueProperty);
                        _ilg.Stloc(pairValue);

                        WriteStartElement(pairKey, 0 /*nameIndex*/);
                        WriteValue(pairValue);
                        WriteEndElement();

                        _ilg.EndForEach(moveNextMethod);
                        _ilg.Else();
                    }

                    WriteArrayAttribute();

                    _ilg.ForEach(currentValue, elementType, enumeratorType, enumerator, getCurrentMethod);
                    if (incrementCollectionCountMethod == null)
                    {
                        _ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
                    }
                    if (!TryWritePrimitive(elementType, currentValue, null /*memberInfo*/, null /*arrayItemIndex*/, itemName, 0 /*nameIndex*/))
                    {
                        WriteStartElement(itemName, 0 /*nameIndex*/);

                        if (isGenericDictionary || isDictionary)
                        {
                            _ilg.Call(_dataContractArg, JsonFormatGeneratorStatics.GetItemContractMethod);
                            _ilg.Call(JsonFormatGeneratorStatics.GetRevisedItemContractMethod);
                            _ilg.Call(JsonFormatGeneratorStatics.GetJsonDataContractMethod);
                            _ilg.Load(_xmlWriterArg);
                            _ilg.Load(currentValue);
                            _ilg.ConvertValue(currentValue.LocalType, Globals.TypeOfObject);
                            _ilg.Load(_contextArg);
                            _ilg.Load(currentValue.LocalType);
                            _ilg.LoadMember(JsonFormatGeneratorStatics.TypeHandleProperty);
                            _ilg.Call(JsonFormatGeneratorStatics.WriteJsonValueMethod);
                        }
                        else
                        {
                            WriteValue(currentValue);
                        }
                        WriteEndElement();
                    }
                    _ilg.EndForEach(moveNextMethod);

                    if (canWriteSimpleDictionary)
                    {
                        _ilg.EndIf();
                    }
                }

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
                Justification = "The call to MakeGenericMethod is safe due to the fact that IncrementCollectionCountGeneric is not annotated.")]
                static MethodInfo MakeIncrementCollectionCountGenericMethod(Type itemType) => XmlFormatGeneratorStatics.IncrementCollectionCountGenericMethod.MakeGenericMethod(itemType);
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private bool TryWritePrimitive(Type type, LocalBuilder? value, MemberInfo? memberInfo, LocalBuilder? arrayItemIndex, LocalBuilder? name, int nameIndex)
            {
                PrimitiveDataContract? primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
                if (primitiveContract == null || primitiveContract.UnderlyingType == Globals.TypeOfObject)
                    return false;

                // load writer
                if (type.IsValueType)
                {
                    _ilg.Load(_xmlWriterArg);
                }
                else
                {
                    _ilg.Load(_contextArg);
                    _ilg.Load(_xmlWriterArg);
                }
                // load primitive value
                if (value != null)
                {
                    _ilg.Load(value);
                }
                else if (memberInfo != null)
                {
                    _ilg.LoadAddress(_objectLocal);
                    _ilg.LoadMember(memberInfo);
                }
                else
                {
                    _ilg.LoadArrayElement(_objectLocal, arrayItemIndex);
                }
                // load name
                if (name != null)
                {
                    _ilg.Load(name);
                }
                else
                {
                    _ilg.LoadArrayElement(_memberNamesArg!, nameIndex);
                }
                // load namespace
                _ilg.Load(null);
                // call method to write primitive
                _ilg.Call(primitiveContract.XmlFormatWriterMethod);
                return true;
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private bool TryWritePrimitiveArray(Type type, Type itemType, LocalBuilder value, LocalBuilder itemName)
            {
                PrimitiveDataContract? primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
                if (primitiveContract == null)
                    return false;

                string? writeArrayMethod = null;
                switch (Type.GetTypeCode(itemType))
                {
                    case TypeCode.Boolean:
                        writeArrayMethod = "WriteJsonBooleanArray";
                        break;
                    case TypeCode.DateTime:
                        writeArrayMethod = "WriteJsonDateTimeArray";
                        break;
                    case TypeCode.Decimal:
                        writeArrayMethod = "WriteJsonDecimalArray";
                        break;
                    case TypeCode.Int32:
                        writeArrayMethod = "WriteJsonInt32Array";
                        break;
                    case TypeCode.Int64:
                        writeArrayMethod = "WriteJsonInt64Array";
                        break;
                    case TypeCode.Single:
                        writeArrayMethod = "WriteJsonSingleArray";
                        break;
                    case TypeCode.Double:
                        writeArrayMethod = "WriteJsonDoubleArray";
                        break;
                    default:
                        break;
                }
                if (writeArrayMethod != null)
                {
                    WriteArrayAttribute();

                    MethodInfo writeArrayMethodInfo = typeof(JsonWriterDelegator).GetMethod(
                        writeArrayMethod,
                        Globals.ScanAllMembers,
                        new Type[] { type, typeof(XmlDictionaryString), typeof(XmlDictionaryString) })!;
                    _ilg.Call(_xmlWriterArg, writeArrayMethodInfo, value, itemName, null);
                    return true;
                }
                return false;
            }

            private void WriteArrayAttribute()
            {
                _ilg.Call(_xmlWriterArg, JsonFormatGeneratorStatics.WriteAttributeStringMethod,
                    null /* prefix */,
                    JsonGlobals.typeString /* local name */,
                    string.Empty /* namespace */,
                    JsonGlobals.arrayString /* value */);
            }

            private void WriteObjectAttribute()
            {
                _ilg.Call(_xmlWriterArg, JsonFormatGeneratorStatics.WriteAttributeStringMethod,
                    null /* prefix */,
                    JsonGlobals.typeString /* local name */,
                    null /* namespace */,
                    JsonGlobals.objectString /* value */);
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void WriteValue(LocalBuilder memberValue)
            {
                Type memberType = memberValue.LocalType;
                if (memberType.IsPointer)
                {
                    _ilg.Load(memberValue);
                    _ilg.Load(memberType);
                    _ilg.Call(JsonFormatGeneratorStatics.BoxPointer);
                    memberType = typeof(System.Reflection.Pointer);
                    memberValue = _ilg.DeclareLocal(memberType, "memberValueRefPointer");
                    _ilg.Store(memberValue);
                }
                bool isNullableOfT = (memberType.IsGenericType &&
                                      memberType.GetGenericTypeDefinition() == Globals.TypeOfNullable);
                if (memberType.IsValueType && !isNullableOfT)
                {
                    PrimitiveDataContract? primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(memberType);
                    if (primitiveContract != null)
                        _ilg.Call(_xmlWriterArg, primitiveContract.XmlFormatContentWriterMethod, memberValue);
                    else
                        InternalSerialize(XmlFormatGeneratorStatics.InternalSerializeMethod, memberValue, memberType, false /* writeXsiType */);
                }
                else
                {
                    if (isNullableOfT)
                    {
                        memberValue = UnwrapNullableObject(memberValue); //Leaves !HasValue on stack
                        memberType = memberValue.LocalType;
                    }
                    else
                    {
                        _ilg.Load(memberValue);
                        _ilg.Load(null);
                        _ilg.Ceq();
                    }
                    _ilg.If();
                    _ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteNullMethod, _xmlWriterArg, memberType, DataContract.IsTypeSerializable(memberType));
                    _ilg.Else();
                    PrimitiveDataContract? primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(memberType);
                    if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject)
                    {
                        if (isNullableOfT)
                        {
                            _ilg.Call(_xmlWriterArg, primitiveContract.XmlFormatContentWriterMethod, memberValue);
                        }
                        else
                        {
                            _ilg.Call(_contextArg, primitiveContract.XmlFormatContentWriterMethod, _xmlWriterArg, memberValue);
                        }
                    }
                    else
                    {
                        if (memberType == Globals.TypeOfObject || //boxed Nullable<T>
                            memberType == Globals.TypeOfValueType ||
                            ((IList)Globals.TypeOfNullable.GetInterfaces()).Contains(memberType))
                        {
                            _ilg.Load(memberValue);
                            _ilg.ConvertValue(memberValue.LocalType, Globals.TypeOfObject);
                            memberValue = _ilg.DeclareLocal(Globals.TypeOfObject, "unwrappedMemberValue");
                            memberType = memberValue.LocalType;
                            _ilg.Stloc(memberValue);
                            _ilg.If(memberValue, Cmp.EqualTo, null);
                            _ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteNullMethod, _xmlWriterArg, memberType, DataContract.IsTypeSerializable(memberType));
                            _ilg.Else();
                        }
                        InternalSerialize((isNullableOfT ? XmlFormatGeneratorStatics.InternalSerializeMethod : XmlFormatGeneratorStatics.InternalSerializeReferenceMethod),
                            memberValue, memberType, false /* writeXsiType */);

                        if (memberType == Globals.TypeOfObject) //boxed Nullable<T>
                            _ilg.EndIf();
                    }
                    _ilg.EndIf();
                }
            }

            private void InternalSerialize(MethodInfo methodInfo, LocalBuilder memberValue, Type memberType, bool writeXsiType)
            {
                _ilg.Load(_contextArg);
                _ilg.Load(_xmlWriterArg);
                _ilg.Load(memberValue);
                _ilg.ConvertValue(memberValue.LocalType, Globals.TypeOfObject);
                LocalBuilder typeHandleValue = _ilg.DeclareLocal(typeof(RuntimeTypeHandle), "typeHandleValue");
                _ilg.Call(memberValue, XmlFormatGeneratorStatics.GetTypeMethod);
                _ilg.Call(XmlFormatGeneratorStatics.GetTypeHandleMethod);
                _ilg.Stloc(typeHandleValue);
                _ilg.LoadAddress(typeHandleValue);
                _ilg.Ldtoken(memberType);
                _ilg.Call(typeof(RuntimeTypeHandle).GetMethod("Equals", new Type[] { typeof(RuntimeTypeHandle) })!);
                _ilg.Load(writeXsiType);
                _ilg.Load(DataContract.GetId(memberType.TypeHandle));
                _ilg.Ldtoken(memberType);
                _ilg.Call(methodInfo);
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private LocalBuilder UnwrapNullableObject(LocalBuilder memberValue)// Leaves !HasValue on stack
            {
                Type memberType = memberValue.LocalType;
                Label onNull = _ilg.DefineLabel();
                Label end = _ilg.DefineLabel();
                _ilg.LoadAddress(memberValue);
                while (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == Globals.TypeOfNullable)
                {
                    Type innerType = memberType.GetGenericArguments()[0];
                    _ilg.Dup();
                    _ilg.Call(typeof(Nullable<>).MakeGenericType(innerType).GetMethod("get_HasValue")!);
                    _ilg.Brfalse(onNull);
                    _ilg.Call(typeof(Nullable<>).MakeGenericType(innerType).GetMethod("get_Value")!);
                    memberType = innerType;
                }
                memberValue = _ilg.DeclareLocal(memberType, "nullableUnwrappedMemberValue");
                _ilg.Stloc(memberValue);
                _ilg.Load(false); //isNull
                _ilg.Br(end);
                _ilg.MarkLabel(onNull);
                _ilg.Pop();
                _ilg.LoadAddress(memberValue);
                _ilg.InitObj(memberType);
                _ilg.Load(true); //isNull
                _ilg.MarkLabel(end);
                return memberValue;
            }

            private void WriteStartElement(LocalBuilder? nameLocal, int nameIndex)
            {
                _ilg.Load(_xmlWriterArg);

                // localName
                if (nameLocal == null)
                    _ilg.LoadArrayElement(_memberNamesArg!, nameIndex);
                else
                    _ilg.Load(nameLocal);

                // namespace
                _ilg.Load(null);

                if (nameLocal != null && nameLocal.LocalType == typeof(string))
                {
                    _ilg.Call(JsonFormatGeneratorStatics.WriteStartElementStringMethod);
                }
                else
                {
                    _ilg.Call(JsonFormatGeneratorStatics.WriteStartElementMethod);
                }
            }

            private void WriteEndElement()
            {
                _ilg.Call(_xmlWriterArg, JsonFormatGeneratorStatics.WriteEndElementMethod);
            }
        }
    }
}
