// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization
{
    internal abstract class ReflectionReader
    {
        private delegate object ReflectionReadValueDelegate(Type itemType, string itemName, string itemNs);
        private delegate object CollectionSetItemDelegate(object resultCollection, object collectionItem, int itemIndex);

        private readonly static MethodInfo s_getCollectionSetItemDelegateMethod = typeof(ReflectionReader).GetMethod(nameof(GetCollectionSetItemDelegate), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        private readonly static MethodInfo s_objectToKeyValuePairGetKey = typeof(ReflectionReader).GetMethod(nameof(ObjectToKeyValuePairGetKey), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private readonly static MethodInfo s_objectToKeyValuePairGetValue = typeof(ReflectionReader).GetMethod(nameof(ObjectToKeyValuePairGetValue), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        protected ClassDataContract _classContract;
        protected XmlReaderDelegator _xmlReaderArg;
        protected XmlObjectSerializerReadContext _contextArg;
        protected XmlDictionaryString[] _memberNamesArg;
        protected XmlDictionaryString[] _memberNamespacesArg;

        protected CollectionDataContract _collectionContract;
        protected XmlDictionaryString _collectionItemName;
        protected XmlDictionaryString _collectionItemNamespace;

        public ReflectionReader(ClassDataContract classDataContract)
        {
            _classContract = classDataContract;
        }

        public ReflectionReader(CollectionDataContract collectionContract)
        {
            _collectionContract = collectionContract;
        }

        protected void ReflectionInitArgs(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces)
        {
            _xmlReaderArg = xmlReader;
            _contextArg = context;
            _memberNamesArg = memberNames;
            _memberNamespacesArg = memberNamespaces;
        }

        protected object ReflectionReadClassInternal(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces)
        {
            var classContract = _classContract;
            ReflectionInitArgs(xmlReader, context, memberNames, memberNamespaces);
            object obj = CreateObject(_classContract);
            context.AddNewObject(obj);
            InvokeOnDeserializing(obj, context, _classContract);

            ReflectionReadMembers(ref obj);
            obj = ResolveAdapterObject(obj, classContract);

            InvokeOnDeserialized(obj, context, _classContract);

            return obj;
        }

        protected abstract void ReflectionReadMembers(ref object obj);

        protected int ReflectionGetMembers(ClassDataContract classContract, DataMember[] members)
        {
            int memberCount = (classContract.BaseContract == null) ? 0 : ReflectionGetMembers(classContract.BaseContract, members);
            int childElementIndex = memberCount;
            for (int i = 0; i < classContract.Members.Count; i++, memberCount++)
            {
                members[childElementIndex + i] = classContract.Members[i];
            }

            return memberCount;
        }

        protected void ReflectionReadMember(ref object obj, int memberIndex, XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, DataMember[] members)
        {
            DataMember dataMember = members[memberIndex];

            Debug.Assert(dataMember != null);
            Type memberType = dataMember.MemberType;
            if (dataMember.IsGetOnlyCollection)
            {
                var memberValue = ReflectionGetMemberValue(obj, dataMember);
                context.StoreCollectionMemberInfo(memberValue);
                ReflectionReadValue(dataMember, GetClassContractNamespace(_classContract));
            }
            else
            {
                var value = ReflectionReadValue(dataMember, _classContract.StableName.Namespace);
                MemberInfo memberInfo = dataMember.MemberInfo;
                Debug.Assert(memberInfo != null);

                ReflectionSetMemberValue(ref obj, value, dataMember);
            }
        }

        protected abstract string GetClassContractNamespace(ClassDataContract _classContract);

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

            return ReflectionReadValue(type, name, ns, dataMember.MemberPrimitiveContract);
        }

        protected object ReflectionReadValue(Type type, string name, string ns, PrimitiveDataContract primitiveContractForOriginalType = null)
        {
            object value = null;
            int nullables = 0;
            while (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
            {
                nullables++;
                type = type.GetGenericArguments()[0];
            }

            PrimitiveDataContract primitiveContract = nullables != 0 ?
                PrimitiveDataContract.GetPrimitiveDataContract(type)
                : (primitiveContractForOriginalType ?? PrimitiveDataContract.GetPrimitiveDataContract(type));

            if ((primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject) || nullables != 0 || type.GetTypeInfo().IsValueType)
            {
                _contextArg.ReadAttributes(_xmlReaderArg);
                string objectId = _contextArg.ReadIfNullOrRef(_xmlReaderArg, Globals.TypeOfString, true);
                if (objectId != null)
                {
                    if (objectId.Length == 0)
                    {
                        objectId = _contextArg.GetObjectId();
                        if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject)
                        {
                            value = primitiveContract.ReadXmlValue(_xmlReaderArg, _contextArg);
                            _contextArg.AddNewObject(value);
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
                    _contextArg.ReadAttributes(_xmlReaderArg);
                    string objectId = _contextArg.ReadIfNullOrRef(_xmlReaderArg, Globals.TypeOfString, true);
                    if (objectId != null)
                    {
                        if (objectId.Length == 0)
                        {
                            objectId = _contextArg.GetObjectId();
                            if (hasValidPrimitiveContract)
                            {
                                value = primitiveContract.ReadXmlValue(_xmlReaderArg, _contextArg);
                                _contextArg.AddNewObject(value);
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
            return _contextArg.InternalDeserialize(_xmlReaderArg, DataContract.GetId(type.TypeHandle), type.TypeHandle, name, ns);
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
                var contextArg = context.GetStreamingContext();
                classContract.OnDeserialized.Invoke(obj, new object[] { contextArg });
            }
        }

        private static object CreateObject(ClassDataContract classContract)
        {
            object obj;
            if (!classContract.CreateNewInstanceViaDefaultConstructor(out obj))
            {
                Type classType = classContract.UnderlyingType;
                obj = XmlFormatReaderGenerator.UnsafeGetUninitializedObject(classType);
            }

            return obj;
        }

        private static object ResolveAdapterObject(object obj, ClassDataContract classContract)
        {
            Type objType = obj.GetType();
            if (objType == Globals.TypeOfDateTimeOffsetAdapter)
            {
                obj = DateTimeOffsetAdapter.GetDateTimeOffset((DateTimeOffsetAdapter)obj);
            }
            else if (obj is IKeyValuePairAdapter)
            {
                obj = classContract.GetKeyValuePairMethodInfo.Invoke(obj, Array.Empty<object>());
            }

            return obj;
        }

        protected object ReflectionReadCollectionInternal(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, CollectionDataContract collectionContract)
        {
            ReflectionInitArgs(xmlReader, context, null, null);
            _collectionItemName = itemName;
            _collectionItemNamespace = itemNamespace;

            return ReflectionReadCollectionCore(collectionContract);
        }

        protected abstract string GetCollectionContractItemName(CollectionDataContract collectionContract);
        protected abstract string GetCollectionContractNamespace(CollectionDataContract collectionContract);


        private object ReflectionReadCollectionCore(CollectionDataContract collectionContract)
        {
            bool isArray = (collectionContract.Kind == CollectionKind.Array);

            int arraySize = _contextArg.GetArraySize();
            string objectId = _contextArg.GetObjectId();
            object resultArray = null;
            if (isArray && ReflectionTryReadPrimitiveArray(collectionContract.UnderlyingType, collectionContract.ItemType, arraySize, out resultArray))
            {
                return resultArray;
            }

            if (arraySize != -1)
            {
                throw new NotImplementedException();
            }

            string itemName = GetCollectionContractItemName(collectionContract);
            string itemNs = GetCollectionContractNamespace(collectionContract);
            object resultCollection = ReflectionCreateCollection(collectionContract);
            _contextArg.AddNewObject(resultCollection);
            _contextArg.IncrementItemCount(1);

            if (!ReflectionReadSpecialCollection(collectionContract, resultCollection))
            {
                ReflectionReadValueDelegate reflectionReadValueDelegate = null;
                CollectionSetItemDelegate collectionSetItemDelegate = null;

                int index = 0;
                for (; index < int.MaxValue; index++)
                {
                    if (_xmlReaderArg.IsStartElement(_collectionItemName, _collectionItemNamespace))
                    {
                        _contextArg.IncrementItemCount(1);
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
                        if (_xmlReaderArg.NodeType == XmlNodeType.EndElement)
                        {
                            break;
                        }
                        if (!_xmlReaderArg.IsStartElement())
                        {
                            throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, _xmlReaderArg);
                        }
                        _contextArg.SkipUnknownElement(_xmlReaderArg);
                        index--;
                    }
                }

                if (IsArrayLikeCollection(collectionContract))
                {
                    MethodInfo trimArraySizeMethod = XmlFormatGeneratorStatics.TrimArraySizeMethod.MakeGenericMethod(collectionContract.ItemType);
                    resultCollection = trimArraySizeMethod.Invoke(null, new object[] { resultCollection, index });
                }
            }

            return resultCollection;
        }

        protected virtual bool ReflectionReadSpecialCollection(CollectionDataContract collectionContract, object resultCollection)
        {
            return false;
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
                return ReflectionReadDictionaryItem(collectionContract);
            }
            else
            {
                reflectionReadValueDelegate = reflectionReadValueDelegate ?? GetReflectionReadValueDelegate(itemType, itemName, itemNs);
                return reflectionReadValueDelegate(itemType, itemName, itemNs);
            }
        }

        protected abstract object ReflectionReadDictionaryItem(CollectionDataContract collectionContract);

        private bool ReflectionTryReadPrimitiveArray(Type type, Type itemType, int arraySize, out object resultArray)
        {
            XmlDictionaryString collectionItemName = _collectionItemName;
            XmlDictionaryString itemNamespace = _collectionItemNamespace;
            resultArray = null;

            PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
            if (primitiveContract == null)
                return false;

            switch (itemType.GetTypeCode())
            {
                case TypeCode.Boolean:
                    {
                        bool[] boolArray = null;
                        _xmlReaderArg.TryReadBooleanArray(_contextArg, collectionItemName, itemNamespace, arraySize, out boolArray);
                        resultArray = boolArray;
                    }
                    break;
                case TypeCode.DateTime:
                    {
                        DateTime[] dateTimeArray = null;
                        _xmlReaderArg.TryReadDateTimeArray(_contextArg, collectionItemName, itemNamespace, arraySize, out dateTimeArray);
                        resultArray = dateTimeArray;
                    }
                    break;
                case TypeCode.Decimal:
                    {
                        decimal[] decimalArray = null;
                        _xmlReaderArg.TryReadDecimalArray(_contextArg, collectionItemName, itemNamespace, arraySize, out decimalArray);
                        resultArray = decimalArray;
                    }
                    break;
                case TypeCode.Int32:
                    {
                        int[] intArray = null;
                        _xmlReaderArg.TryReadInt32Array(_contextArg, collectionItemName, itemNamespace, arraySize, out intArray);
                        resultArray = intArray;
                    }
                    break;
                case TypeCode.Int64:
                    {
                        long[] longArray = null;
                        _xmlReaderArg.TryReadInt64Array(_contextArg, collectionItemName, itemNamespace, arraySize, out longArray);
                        resultArray = longArray;
                    }
                    break;
                case TypeCode.Single:
                    {
                        float[] floatArray = null;
                        _xmlReaderArg.TryReadSingleArray(_contextArg, collectionItemName, itemNamespace, arraySize, out floatArray);
                        resultArray = floatArray;
                    }
                    break;
                case TypeCode.Double:
                    {
                        double[] doubleArray = null;
                        _xmlReaderArg.TryReadDoubleArray(_contextArg, collectionItemName, itemNamespace, arraySize, out doubleArray);
                        resultArray = doubleArray;
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        protected void ReflectionReadGetOnlyCollectionInternal(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNs, CollectionDataContract collectionContract)
        {
            ReflectionInitArgs(xmlReader, context, null, null);
            _collectionItemName = itemName;
            _collectionItemNamespace = itemNs;

            object resultCollection = _contextArg.GetCollectionMember();
            if (ReflectionReadSpecialCollection(collectionContract, resultCollection))
            {
                return;
            }

            if (_xmlReaderArg.IsStartElement(_collectionItemName, _collectionItemNamespace))
            {
                
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
                    if (_xmlReaderArg.IsStartElement(_collectionItemName, _collectionItemNamespace))
                    {
                        _contextArg.IncrementItemCount(1);
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
                        if (_xmlReaderArg.NodeType == XmlNodeType.EndElement)
                        {
                            break;
                        }
                        if (!_xmlReaderArg.IsStartElement())
                        {
                            throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, _xmlReaderArg);
                        }
                        _contextArg.SkipUnknownElement(_xmlReaderArg);
                        index--;
                    }
                }
            }
        }
    }
}
