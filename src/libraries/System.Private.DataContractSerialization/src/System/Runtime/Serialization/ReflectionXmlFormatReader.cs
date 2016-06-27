// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Diagnostics;
#if NET_NATIVE
using Internal.Runtime.Augments;
#endif

namespace System.Runtime.Serialization
{
    internal sealed class ReflectionXmlFormatReader
    {
        private delegate object ReflectionReadValueDelegate(Type itemType, string itemName, string itemNs);
        private delegate object CollectionSetItemDelegate(object resultCollection, object collectionItem, int itemIndex);

        private ClassDataContract _classContract;
        private CollectionDataContract _collectionContract;
        private XmlReaderDelegator _arg0XmlReader;
        private XmlObjectSerializerReadContext _arg1Context;
        private XmlDictionaryString[] _arg2MemberNames;
        private XmlDictionaryString[] _arg3MemberNamespaces;
        private XmlDictionaryString _arg2CollectionItemName;
        private XmlDictionaryString _arg3CollectionItemNamespace;

        internal ReflectionXmlFormatReader(ClassDataContract classContract)
        {
            _classContract = classContract;
        }

        internal ReflectionXmlFormatReader(CollectionDataContract collectionContract)
        {
            _collectionContract = collectionContract;
        }

        internal object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces)
        {
            ReflectionInitArgs(xmlReader, context, memberNames, memberNamespaces);
            object obj = ReflectionCreateObject(_classContract);
            context.AddNewObject(obj);
            InvokeOnDeserializing(obj, context, _classContract);
            ReflectionReadMembers(ref obj, xmlReader, context, memberNames, memberNamespaces);

            Type objType = obj.GetType();
            if (objType == Globals.TypeOfDateTimeOffsetAdapter)
            {
                obj = DateTimeOffsetAdapter.GetDateTimeOffset((DateTimeOffsetAdapter)obj);
            }
            else if (obj is IKeyValuePairAdapter)
            {
                obj = _classContract.GetKeyValuePairMethodInfo.Invoke(obj, Array.Empty<object>());
            }

            InvokeOnDeserialized(obj, context, _classContract);

            return obj;
        }

        private void ReflectionInitArgs(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces)
        {
            _arg0XmlReader = xmlReader;
            _arg1Context = context;
            _arg2MemberNames = memberNames;
            _arg3MemberNamespaces = memberNamespaces;
        }

        private void InvokeOnDeserializing(object obj, XmlObjectSerializerReadContext context, ClassDataContract classContract)
        {
            if (classContract.BaseContract != null)
                InvokeOnDeserializing(obj, context, classContract.BaseContract);
            if (classContract.OnDeserializing != null)
            {
                var contextArg = context.GetStreamingContext();
                classContract.OnDeserializing.Invoke(obj, new object[] { contextArg });
            }
        }

        private void InvokeOnDeserialized(object obj, XmlObjectSerializerReadContext context, ClassDataContract classContract)
        {
            if (classContract.BaseContract != null)
                InvokeOnDeserialized(obj, context, classContract.BaseContract);
            if (classContract.OnDeserialized != null)
            {
                var contextArg = context.GetStreamingContext() ;
                classContract.OnDeserialized.Invoke(obj, new object[] { contextArg });
            }
        }

        private void ReflectionReadMembers(ref object obj, XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces)
        {
            int memberCount = _classContract.MemberNames.Length;
            context.IncrementItemCount(memberCount);
            int memberIndex = -1;
            int firstRequiredMember;
            bool[] requiredMembers = GetRequiredMembers(_classContract, out firstRequiredMember);
            bool hasRequiredMembers = (firstRequiredMember < memberCount);
            int requiredIndex = hasRequiredMembers ? firstRequiredMember : -1;
            int index = -1;
            DataMember[] members = new DataMember[memberCount];
            int reflectedMemberCount = ReflectionGetMembers(_classContract, members);
            Debug.Assert(reflectedMemberCount == memberCount, "The value returned by ReflectionGetMembers() should equal to memberCount.");

            while (true)
            {
                if (!XmlObjectSerializerReadContext.MoveToNextElement(xmlReader))
                {
                    return;
                }
                if (hasRequiredMembers)
                {
                    index = context.GetMemberIndexWithRequiredMembers(xmlReader, memberNames, memberNamespaces, memberIndex, requiredIndex, null);
                }
                else
                {
                    index = context.GetMemberIndex(xmlReader, memberNames, memberNamespaces, memberIndex, null);
                }

                // GetMemberIndex returns memberNames.Length if member not found
                if (index < members.Length)
                {
                    ReflectionReadMember(ref obj, index, xmlReader, context, members);
                    memberIndex = index;
                    requiredIndex = index + 1;
                }
            }
        }

        // Put all members of the contract (including types from base contract) into 'members'.
        private int ReflectionGetMembers(ClassDataContract classContract, DataMember[] members)
        {
            int memberCount = (classContract.BaseContract == null) ? 0 : ReflectionGetMembers(classContract.BaseContract, members);
            int childElementIndex = memberCount;
            for (int i = 0; i < classContract.Members.Count; i++, memberCount++)
            {
                members[childElementIndex + i] = classContract.Members[i];
            }

            return memberCount;
        }

        private void ReflectionReadMember(ref object obj, int memberIndex, XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, DataMember[] members)
        {
            DataMember dataMember = members[memberIndex];

            Debug.Assert(dataMember != null);
            Type memberType = dataMember.MemberType;
            if (dataMember.IsGetOnlyCollection)
            {
                var memberValue = ReflectionGetMemberValue(obj, dataMember);
                context.StoreCollectionMemberInfo(memberValue);
                ReflectionReadValue(dataMember, _classContract.StableName.Namespace);
            }
            else
            {
                var value = ReflectionReadValue(dataMember, _classContract.StableName.Namespace);
                MemberInfo memberInfo = dataMember.MemberInfo;
                Debug.Assert(memberInfo != null);

                ReflectionSetMemberValue(ref obj, value, dataMember);
            }
        }

        private object ReflectionGetMemberValue(object obj, DataMember dataMember)
        {
            return dataMember.Getter(obj);
        }

        private void ReflectionSetMemberValue(ref object obj, object memberValue, DataMember dataMember)
        {
            dataMember.Setter(ref obj, memberValue);
        }

        private object ReflectionReadValue(DataMember dataMember, string ns)
        {
            Type type = dataMember.MemberType;
            string name = dataMember.Name;
            object value = null;
            int nullables = 0;
            while (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
            {
                nullables++;
                type = type.GetGenericArguments()[0];
            }

            PrimitiveDataContract primitiveContract = nullables != 0 ? PrimitiveDataContract.GetPrimitiveDataContract(type) : dataMember.MemberPrimitiveContract;
            if ((primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject) || nullables != 0 || type.GetTypeInfo().IsValueType)
            {
                _arg1Context.ReadAttributes(_arg0XmlReader);
                string objectId = _arg1Context.ReadIfNullOrRef(_arg0XmlReader, Globals.TypeOfString, true);
                if (objectId != null)
                {
                    if (objectId.Length == 0)
                    {
                        objectId = _arg1Context.GetObjectId();
                        if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject)
                        {
                            value = primitiveContract.ReadXmlValue(_arg0XmlReader, _arg1Context);
                            _arg1Context.AddNewObject(value);
                        }
                        else
                        {
                            value = ReflectionInternalDeserialize(type, name, ns);
                        }
                    }
                    else if (type.GetTypeInfo().IsValueType)
                    {
                        throw new SerializationException(SR.Format(SR.ValueTypeCannotHaveId, DataContract.GetClrTypeFullName(type)));
                    }
                }
                else
                {
                    value = null;
                }
            }
            else
            {
                value = ReflectionInternalDeserialize(type, name, ns);
            }

            return value;
        }

        // This method is a perf optimization for collections. The original method is ReflectionReadValue.
        private ReflectionReadValueDelegate GetReflectionReadValueDelegate(Type type, string name, string ns)
        {
            object value = null;
            int nullables = 0;
            while (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
            {
                nullables++;
                type = type.GetGenericArguments()[0];
            }

            PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
            bool hasValidPrimitiveContract = primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject;
            if ((primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject) || nullables != 0 || type.GetTypeInfo().IsValueType)
            {

                return (typeArg, nameArg, nsArg) =>
                {
                    _arg1Context.ReadAttributes(_arg0XmlReader);
                    string objectId = _arg1Context.ReadIfNullOrRef(_arg0XmlReader, Globals.TypeOfString, true);
                    if (objectId != null)
                    {
                        if (objectId.Length == 0)
                        {
                            objectId = _arg1Context.GetObjectId();
                            if (hasValidPrimitiveContract)
                            {
                                value = primitiveContract.ReadXmlValue(_arg0XmlReader, _arg1Context);
                                _arg1Context.AddNewObject(value);
                            }
                            else
                            {
                                value = ReflectionInternalDeserialize(type, name, ns);
                            }
                        }
                        else if (type.GetTypeInfo().IsValueType)
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(SR.Format(SR.ValueTypeCannotHaveId, DataContract.GetClrTypeFullName(type))));
                        }
                    }
                    else
                    {
                        value = null;
                    }

                    return value;
                };
            }
            else
            {
                return ReflectionInternalDeserialize;
            }
        }

        private object ReflectionInternalDeserialize(Type type, string name, string ns)
        {
            return _arg1Context.InternalDeserialize(_arg0XmlReader, DataContract.GetId(type.TypeHandle), type.TypeHandle, name, ns);
        }

        private object ReflectionCreateObject(ClassDataContract classContract)
        {
            object obj;
            if (!classContract.CreateNewInstanceViaDefaultConstructor(out obj))
            {
                Type classType = classContract.UnderlyingType;
                obj = XmlFormatReaderGenerator.UnsafeGetUninitializedObject(classType);
            }

            return obj;
        }

        bool IsArrayLikeInterface(CollectionDataContract collectionContract)
        {
            if (collectionContract.UnderlyingType.GetTypeInfo().IsInterface)
            {
                switch (collectionContract.Kind)
                {
                    case CollectionKind.Collection:
                    case CollectionKind.GenericCollection:
                    case CollectionKind.Enumerable:
                    case CollectionKind.GenericEnumerable:
                    case CollectionKind.List:
                    case CollectionKind.GenericList:
                        return true;
                }
            }
            return false;
        }

        bool IsArrayLikeCollection(CollectionDataContract collectionContract)
        {
            return collectionContract.Kind == CollectionKind.Array || IsArrayLikeInterface(collectionContract);
        }

        Type[] arrayConstructorParameters = new Type[] { Globals.TypeOfInt };
        object[] arrayConstructorArguments = new object[] { 1 };
        private object ReflectionCreateCollection(CollectionDataContract collectionContract)
        {
            if (IsArrayLikeCollection(collectionContract))
            {
                Type arrayType = collectionContract.ItemType.MakeArrayType();
                var ci = arrayType.GetConstructor(arrayConstructorParameters);
                arrayConstructorArguments[0] = 32;
                var newArray = ci.Invoke(arrayConstructorArguments);
                return newArray;
            }
            else if (collectionContract.Kind == CollectionKind.GenericDictionary && collectionContract.UnderlyingType.GetTypeInfo().IsInterface)
            {
                Type type = Globals.TypeOfDictionaryGeneric.MakeGenericType(collectionContract.ItemType.GetGenericArguments());
                ConstructorInfo ci = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, Array.Empty<Type>());
                object newGenericDict = ci.Invoke(Array.Empty<object>());
                return newGenericDict;
            }
            else
            {
                if (collectionContract.UnderlyingType.GetTypeInfo().IsValueType)
                {
                    object newValueObject = Activator.CreateInstance(collectionContract.UnderlyingType);
                    return newValueObject;
                }
                else if (collectionContract.UnderlyingType == Globals.TypeOfIDictionary)
                {
                    object newGenericDict = new Dictionary<object, object>();
                    return newGenericDict;
                }
                else
                {
                    ConstructorInfo ci = collectionContract.UnderlyingType.GetConstructor(Array.Empty<Type>());
                    object newCollection = ci.Invoke(Array.Empty<object>());
                    return newCollection;
                }
            }
        }

        internal object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, CollectionDataContract collectionContract)
        {
            ReflectionInitArgs(xmlReader, context, null, null);
            _arg2CollectionItemName = itemName;
            _arg3CollectionItemNamespace = itemNamespace;

            return ReflectionReadCollectionCore(collectionContract);
        }

        private object ReflectionReadCollectionCore(CollectionDataContract collectionContract)
        {
            bool isArray = (collectionContract.Kind == CollectionKind.Array);

            int arraySize = _arg1Context.GetArraySize();
            string objectId = _arg1Context.GetObjectId();
            object resultArray = null;
            if (isArray && ReflectionTryReadPrimitiveArray(collectionContract.UnderlyingType, collectionContract.ItemType, arraySize, out resultArray))
            {
                return resultArray;
            }

            if (arraySize != -1)
            {
                throw new NotImplementedException();
            }

            string itemName = collectionContract.ItemName;
            string itemNs = collectionContract.StableName.Namespace;
            object resultCollection = ReflectionCreateCollection(collectionContract);
            int index = 0;

            ReflectionReadValueDelegate reflectionReadValueDelegate = null;
            CollectionSetItemDelegate collectionSetItemDelegate = null;
            for (; index < int.MaxValue; index++)
            {
                if (_arg0XmlReader.IsStartElement(_arg2CollectionItemName, _arg3CollectionItemNamespace))
                {
                    _arg1Context.IncrementItemCount(1);
                    object collectionItem = ReflectionReadCollectionItem(collectionContract, collectionContract.ItemType, itemName, itemNs, ref reflectionReadValueDelegate);
                    if (collectionSetItemDelegate == null)
                    {
                        MethodInfo getCollectionSetItemDelegateMethod = s_getCollectionSetItemDelegateMethod.MakeGenericMethod(collectionContract.ItemType);
                        collectionSetItemDelegate = (CollectionSetItemDelegate)getCollectionSetItemDelegateMethod.Invoke(this, new object[] { collectionContract, resultCollection, false/*isReadOnlyCollection*/ });
                    }

                    resultCollection = collectionSetItemDelegate(resultCollection, collectionItem, index);
                }
                else
                {
                    if (_arg0XmlReader.NodeType == XmlNodeType.EndElement)
                    {
                        break;
                    }
                    if (!_arg0XmlReader.IsStartElement())
                    {
                        throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, _arg0XmlReader);
                    }
                    _arg1Context.SkipUnknownElement(_arg0XmlReader);
                    index--;
                }
            }

            if (IsArrayLikeCollection(collectionContract))
            {
                MethodInfo trimArraySizeMethod = XmlFormatGeneratorStatics.TrimArraySizeMethod.MakeGenericMethod(collectionContract.ItemType);
                resultCollection = trimArraySizeMethod.Invoke(null, new object[] { resultCollection, index });
            }
            return resultCollection;
        }

        private readonly static MethodInfo s_getCollectionSetItemDelegateMethod = typeof(ReflectionXmlFormatReader).GetMethod(nameof(GetCollectionSetItemDelegate), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        private readonly static MethodInfo s_objectToKeyValuePairGetKey = typeof(ReflectionXmlFormatReader).GetMethod(nameof(ObjectToKeyValuePairGetKey), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private readonly static MethodInfo s_objectToKeyValuePairGetValue = typeof(ReflectionXmlFormatReader).GetMethod(nameof(ObjectToKeyValuePairGetValue), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        private static object ObjectToKeyValuePairGetKey<K, V>(object o)
        {
            return ((KeyValue<K, V>)o).Key;
        }

        private static object ObjectToKeyValuePairGetValue<K, V>(object o)
        {
            return ((KeyValue<K, V>)o).Value;
        }

        private CollectionSetItemDelegate GetCollectionSetItemDelegate<T>(CollectionDataContract collectionContract, object resultCollectionObject, bool isReadOnlyCollection)
        {
            if (isReadOnlyCollection && collectionContract.Kind == CollectionKind.Array)
            {
                int arraySize = ((Array)resultCollectionObject).Length;
                return (resultCollection, collectionItem, index) =>
                {
                    if (index == arraySize)
                    {
                        XmlObjectSerializerReadContext.ThrowArrayExceededSizeException(arraySize, collectionContract.UnderlyingType);
                    }

                    ((T[])resultCollection)[index] = (T)collectionItem;
                    return resultCollection;
                };
            }
            else if (!isReadOnlyCollection && IsArrayLikeCollection(collectionContract))
            {
                return (resultCollection, collectionItem, index) =>
                {
                    resultCollection = XmlObjectSerializerReadContext.EnsureArraySize((T[])resultCollection, index);
                    ((T[])resultCollection)[index] = (T)collectionItem;
                    return resultCollection;
                };
            }
            else if (collectionContract.Kind == CollectionKind.GenericDictionary || collectionContract.Kind == CollectionKind.Dictionary)
            {
                Type keyType = collectionContract.ItemType.GenericTypeArguments[0];
                Type valueType = collectionContract.ItemType.GenericTypeArguments[1];
                Func<object, object> objectToKeyValuePairGetKey = (Func<object, object>)s_objectToKeyValuePairGetKey.MakeGenericMethod(keyType, valueType).CreateDelegate(typeof(Func<object, object>));
                Func<object, object> objectToKeyValuePairGetValue = (Func<object, object>)s_objectToKeyValuePairGetValue.MakeGenericMethod(keyType, valueType).CreateDelegate(typeof(Func<object, object>));

                return (resultCollection, collectionItem, index) =>
                {
                    object key = objectToKeyValuePairGetKey(collectionItem);
                    object value = objectToKeyValuePairGetValue(collectionItem);

                    IDictionary dict = (IDictionary)resultCollection;
                    dict.Add(key, value);
                    return resultCollection;
                };
            }
            else
            {
                Type collectionType = resultCollectionObject.GetType();
                Type genericCollectionType = typeof(ICollection<T>);
                Type typeIList = Globals.TypeOfIList;
                if (genericCollectionType.IsAssignableFrom(collectionType))
                {
                    return (resultCollection, collectionItem, index) =>
                    {
                        ((ICollection<T>)resultCollection).Add((T)collectionItem);
                        return resultCollection;
                    };
                }
                else if (typeIList.IsAssignableFrom(collectionType))
                {
                    return (resultCollection, collectionItem, index) =>
                    {
                        ((IList)resultCollection).Add(collectionItem);
                        return resultCollection;
                    };
                }
                else
                {
                    MethodInfo addMethod = collectionType.GetMethod("Add");
                    if (addMethod == null)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(collectionContract.UnderlyingType))));
                    }

                    return (resultCollection, collectionItem, index) =>
                    {
                        addMethod.Invoke(resultCollection, new object[] { collectionItem });
                        return resultCollection;
                    };
                }
            }
        }

        private object ReflectionReadCollectionItem(CollectionDataContract collectionContract, Type itemType, string itemName, string itemNs,
            ref ReflectionReadValueDelegate reflectionReadValueDelegate)
        {
            if (collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary)
            {
                _arg1Context.ReadAttributes(_arg0XmlReader);
                return collectionContract.ItemContract.ReadXmlValue(_arg0XmlReader, _arg1Context);
            }
            else
            {
                reflectionReadValueDelegate = reflectionReadValueDelegate ?? GetReflectionReadValueDelegate(itemType, itemName, itemNs);
                return reflectionReadValueDelegate(itemType, itemName, itemNs);
            }
        }

        private bool ReflectionTryReadPrimitiveArray(Type type, Type itemType, int arraySize, out object resultArray)
        {
            XmlDictionaryString collectionItemName = _arg2CollectionItemName;
            XmlDictionaryString itemNamespace = _arg3CollectionItemNamespace;
            resultArray = null;

            PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
            if (primitiveContract == null)
                return false;

            switch (itemType.GetTypeCode())
            {
                case TypeCode.Boolean:
                    {
                        bool[] boolArray = null;
                        _arg0XmlReader.TryReadBooleanArray(_arg1Context, collectionItemName, itemNamespace, arraySize, out boolArray);
                        resultArray = boolArray;
                    }
                    break;
                case TypeCode.DateTime:
                    {
                        DateTime[] dateTimeArray = null;
                        _arg0XmlReader.TryReadDateTimeArray(_arg1Context, collectionItemName, itemNamespace, arraySize, out dateTimeArray);
                        resultArray = dateTimeArray;
                    }
                    break;
                case TypeCode.Decimal:
                    {
                        decimal[] decimalArray = null;
                        _arg0XmlReader.TryReadDecimalArray(_arg1Context, collectionItemName, itemNamespace, arraySize, out decimalArray);
                        resultArray = decimalArray;
                    }
                    break;
                case TypeCode.Int32:
                    {
                        int[] intArray = null;
                        _arg0XmlReader.TryReadInt32Array(_arg1Context, collectionItemName, itemNamespace, arraySize, out intArray);
                        resultArray = intArray;
                    }
                    break;
                case TypeCode.Int64:
                    {
                        long[] longArray = null;
                        _arg0XmlReader.TryReadInt64Array(_arg1Context, collectionItemName, itemNamespace, arraySize, out longArray);
                        resultArray = longArray;
                    }
                    break;
                case TypeCode.Single:
                    {
                        float[] floatArray = null;
                        _arg0XmlReader.TryReadSingleArray(_arg1Context, collectionItemName, itemNamespace, arraySize, out floatArray);
                        resultArray = floatArray;
                    }
                    break;
                case TypeCode.Double:
                    {
                        double[] doubleArray = null;
                        _arg0XmlReader.TryReadDoubleArray(_arg1Context, collectionItemName, itemNamespace, arraySize, out doubleArray);
                        resultArray = doubleArray;
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        internal void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNs, CollectionDataContract collectionContract)
        {
            ReflectionInitArgs(xmlReader, context, null, null);
            _arg2CollectionItemName = itemName;
            _arg3CollectionItemNamespace = itemNs;

            if (_arg0XmlReader.IsStartElement(_arg2CollectionItemName, _arg3CollectionItemNamespace))
            {
                object resultCollection = _arg1Context.GetCollectionMember();
                if (resultCollection == null)
                {
                    XmlObjectSerializerReadContext.ThrowNullValueReturnedForGetOnlyCollectionException(collectionContract.UnderlyingType);
                }

                string itemNameStr = collectionContract.ItemName;
                string itemNsStr = collectionContract.StableName.Namespace;

                ReflectionReadValueDelegate reflectionReadValueDelegate = null;
                CollectionSetItemDelegate collectionSetItemDelegate = null;
                for (int index = 0; index < int.MaxValue; index++)
                {
                    if (_arg0XmlReader.IsStartElement(_arg2CollectionItemName, _arg3CollectionItemNamespace))
                    {
                        _arg1Context.IncrementItemCount(1);
                        object collectionItem = ReflectionReadCollectionItem(collectionContract, collectionContract.ItemType, itemNameStr, itemNsStr, ref reflectionReadValueDelegate);
                        if (collectionSetItemDelegate == null)
                        {
                            MethodInfo getCollectionSetItemDelegateMethod = s_getCollectionSetItemDelegateMethod.MakeGenericMethod(collectionContract.ItemType);
                            collectionSetItemDelegate = (CollectionSetItemDelegate)getCollectionSetItemDelegateMethod.Invoke(this, new object[] { collectionContract, resultCollection, true/*isReadOnlyCollection*/ });
                        }

                        collectionSetItemDelegate(resultCollection, collectionItem, index);
                    }
                    else
                    {
                        if (_arg0XmlReader.NodeType == XmlNodeType.EndElement)
                        {
                            break;
                        }
                        if (!_arg0XmlReader.IsStartElement())
                        {
                            throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, _arg0XmlReader);
                        }
                        _arg1Context.SkipUnknownElement(_arg0XmlReader);
                        index--;
                    }
                }
            }
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

    }
}
