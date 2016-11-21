// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Runtime.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Xml;
    using System.Linq;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;
    using System.Security;

    // The interface is a perf optimization.
    // Only KeyValuePairAdapter should implement the interface.
    internal interface IKeyValuePairAdapter { }

    //Special Adapter class to serialize KeyValuePair as Dictionary needs it when polymorphism is involved
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/System.Collections.Generic")]
    internal class KeyValuePairAdapter<K, T> : IKeyValuePairAdapter
    {
        private K _kvpKey;
        private T _kvpValue;

        public KeyValuePairAdapter(KeyValuePair<K, T> kvPair)
        {
            _kvpKey = kvPair.Key;
            _kvpValue = kvPair.Value;
        }

        [DataMember(Name = "key")]
        public K Key
        {
            get { return _kvpKey; }
            set { _kvpKey = value; }
        }

        [DataMember(Name = "value")]
        public T Value
        {
            get
            {
                return _kvpValue;
            }
            set
            {
                _kvpValue = value;
            }
        }

        internal KeyValuePair<K, T> GetKeyValuePair()
        {
            return new KeyValuePair<K, T>(_kvpKey, _kvpValue);
        }

        internal static KeyValuePairAdapter<K, T> GetKeyValuePairAdapter(KeyValuePair<K, T> kvPair)
        {
            return new KeyValuePairAdapter<K, T>(kvPair);
        }
    }

#if USE_REFEMIT
    public interface IKeyValue
#else
    internal interface IKeyValue
#endif
    {
        object Key { get; set; }
        object Value { get; set; }
    }

    [DataContract(Namespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
#if USE_REFEMIT
    public struct KeyValue<K, V> : IKeyValue
#else
    internal struct KeyValue<K, V> : IKeyValue
#endif
    {
        private K _key;
        private V _value;

        internal KeyValue(K key, V value)
        {
            _key = key;
            _value = value;
        }

        [DataMember(IsRequired = true)]
        public K Key
        {
            get { return _key; }
            set { _key = value; }
        }

        [DataMember(IsRequired = true)]
        public V Value
        {
            get { return _value; }
            set { _value = value; }
        }

        object IKeyValue.Key
        {
            get { return _key; }
            set { _key = (K)value; }
        }
        
        object IKeyValue.Value
        {
            get { return _value; }
            set { _value = (V)value; }
        }
    }

#if NET_NATIVE
    public enum CollectionKind : byte
#else
    internal enum CollectionKind : byte
#endif
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

#if USE_REFEMIT || NET_NATIVE
    public sealed class CollectionDataContract : DataContract
#else
    internal sealed class CollectionDataContract : DataContract
#endif
    {
        private XmlDictionaryString _collectionItemName;
        
        private XmlDictionaryString _childElementNamespace;
        
        private DataContract _itemContract;

        private CollectionDataContractCriticalHelper _helper;

        public CollectionDataContract(CollectionKind kind) : base(new CollectionDataContractCriticalHelper(kind))
        {
            InitCollectionDataContract(this);
        }

        internal CollectionDataContract(Type type) : base(new CollectionDataContractCriticalHelper(type))
        {
            InitCollectionDataContract(this);
        }

        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor, bool isConstructorCheckRequired)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor, isConstructorCheckRequired))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        private CollectionDataContract(Type type, string invalidCollectionInSharedContractMessage) : base(new CollectionDataContractCriticalHelper(type, invalidCollectionInSharedContractMessage))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        private void InitCollectionDataContract(DataContract sharedTypeContract)
        {
            _helper = base.Helper as CollectionDataContractCriticalHelper;
            _collectionItemName = _helper.CollectionItemName;
            if (_helper.Kind == CollectionKind.Dictionary || _helper.Kind == CollectionKind.GenericDictionary)
            {
                _itemContract = _helper.ItemContract;
            }
            _helper.SharedTypeContract = sharedTypeContract;
        }

        private static Type[] KnownInterfaces
        {
            get
            { return CollectionDataContractCriticalHelper.KnownInterfaces; }
        }

        internal CollectionKind Kind
        {
            get
            { return _helper.Kind; }
        }

        public Type ItemType
        {
            get
            { return _helper.ItemType; }
            set { _helper.ItemType = value; }
        }

        public DataContract ItemContract
        {
            get
            {
                return _itemContract ?? _helper.ItemContract;
            }

            set
            {
                _itemContract = value;
                _helper.ItemContract = value;
            }
        }

        internal DataContract SharedTypeContract
        {
            get
            { return _helper.SharedTypeContract; }
        }

        public string ItemName
        {
            get
            { return _helper.ItemName; }

            set
            { _helper.ItemName = value; }
        }

        public XmlDictionaryString CollectionItemName
        {
            get { return _collectionItemName; }
            set { _collectionItemName = value; }
        }

        public string KeyName
        {
            get
            { return _helper.KeyName; }

            set
            { _helper.KeyName = value; }
        }

        public string ValueName
        {
            get
            { return _helper.ValueName; }

            set
            { _helper.ValueName = value; }
        }

        internal bool IsDictionary
        {
            get { return KeyName != null; }
        }

        public XmlDictionaryString ChildElementNamespace
        {
            get
            {
                if (_childElementNamespace == null)
                {
                    lock (this)
                    {
                        if (_childElementNamespace == null)
                        {
                            if (_helper.ChildElementNamespace == null && !IsDictionary)
                            {
                                XmlDictionaryString tempChildElementNamespace = ClassDataContract.GetChildNamespaceToDeclare(this, ItemType, new XmlDictionary());
                                Interlocked.MemoryBarrier();
                                _helper.ChildElementNamespace = tempChildElementNamespace;
                            }
                            _childElementNamespace = _helper.ChildElementNamespace;
                        }
                    }
                }
                return _childElementNamespace;
            }
        }

        internal bool IsConstructorCheckRequired
        {
            get
            { return _helper.IsConstructorCheckRequired; }

            set
            { _helper.IsConstructorCheckRequired = value; }
        }

        internal MethodInfo GetEnumeratorMethod
        {
            get
            { return _helper.GetEnumeratorMethod; }
        }

        internal MethodInfo AddMethod
        {
            get
            { return _helper.AddMethod; }
        }

        internal ConstructorInfo Constructor
        {
            get
            { return _helper.Constructor; }
        }

        public override DataContractDictionary KnownDataContracts
        {
            get
            { return _helper.KnownDataContracts; }

            set
            { _helper.KnownDataContracts = value; }
        }

        internal string InvalidCollectionInSharedContractMessage
        {
            get
            { return _helper.InvalidCollectionInSharedContractMessage; }
        }

#if NET_NATIVE
        private XmlFormatCollectionWriterDelegate _xmlFormatWriterDelegate;
        public XmlFormatCollectionWriterDelegate XmlFormatWriterDelegate
#else
        internal XmlFormatCollectionWriterDelegate XmlFormatWriterDelegate
#endif
        {
            get
            {
#if NET_NATIVE
                if (DataContractSerializer.Option == SerializationOption.CodeGenOnly
                || (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup && _xmlFormatWriterDelegate != null))
                {
                    return _xmlFormatWriterDelegate;
                }
#endif
                if (_helper.XmlFormatWriterDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatWriterDelegate == null)
                        {
                            XmlFormatCollectionWriterDelegate tempDelegate = new XmlFormatWriterGenerator().GenerateCollectionWriter(this);
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatWriterDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatWriterDelegate;
            }
            set
            {
#if NET_NATIVE
                _xmlFormatWriterDelegate = value;
#endif
            }
        }

#if NET_NATIVE
        private XmlFormatCollectionReaderDelegate _xmlFormatReaderDelegate;
        public XmlFormatCollectionReaderDelegate XmlFormatReaderDelegate
#else
        internal XmlFormatCollectionReaderDelegate XmlFormatReaderDelegate
#endif
        {
            get
            {
#if NET_NATIVE
                if (DataContractSerializer.Option == SerializationOption.CodeGenOnly
                || (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup && _xmlFormatReaderDelegate != null))
                {
                    return _xmlFormatReaderDelegate;
                }
#endif
                if (_helper.XmlFormatReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatReaderDelegate == null)
                        {
                            XmlFormatCollectionReaderDelegate tempDelegate = new XmlFormatReaderGenerator().GenerateCollectionReader(this);
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatReaderDelegate;
            }
            set
            {
#if NET_NATIVE
                _xmlFormatReaderDelegate = value;
#endif
            }
        }

#if NET_NATIVE
        private XmlFormatGetOnlyCollectionReaderDelegate _xmlFormatGetOnlyCollectionReaderDelegate;
        public XmlFormatGetOnlyCollectionReaderDelegate XmlFormatGetOnlyCollectionReaderDelegate
#else
        internal XmlFormatGetOnlyCollectionReaderDelegate XmlFormatGetOnlyCollectionReaderDelegate
#endif
        {
            get
            {
#if NET_NATIVE
                if (DataContractSerializer.Option == SerializationOption.CodeGenOnly
                || (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup && _xmlFormatGetOnlyCollectionReaderDelegate != null))
                {
                    return _xmlFormatGetOnlyCollectionReaderDelegate;
                }
#endif
                if (_helper.XmlFormatGetOnlyCollectionReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatGetOnlyCollectionReaderDelegate == null)
                        {
                            if (UnderlyingType.GetTypeInfo().IsInterface && (Kind == CollectionKind.Enumerable || Kind == CollectionKind.Collection || Kind == CollectionKind.GenericEnumerable))
                            {
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GetOnlyCollectionMustHaveAddMethod, GetClrTypeFullName(UnderlyingType))));
                            }
                            Debug.Assert(AddMethod != null || Kind == CollectionKind.Array, "Add method cannot be null if the collection is being used as a get-only property");
                            XmlFormatGetOnlyCollectionReaderDelegate tempDelegate = new XmlFormatReaderGenerator().GenerateGetOnlyCollectionReader(this);
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatGetOnlyCollectionReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatGetOnlyCollectionReaderDelegate;
            }
            set
            {
#if NET_NATIVE
                _xmlFormatGetOnlyCollectionReaderDelegate = value;
#endif
            }
        }

        internal void IncrementCollectionCount(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
        {
            _helper.IncrementCollectionCount(xmlWriter, obj, context);
        }

        internal IEnumerator GetEnumeratorForCollection(object obj, out Type enumeratorReturnType)
        {
            return _helper.GetEnumeratorForCollection(obj, out enumeratorReturnType);
        }

        private class CollectionDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private static Type[] s_knownInterfaces;

            private Type _itemType;
            private CollectionKind _kind;
            private readonly MethodInfo _getEnumeratorMethod, _addMethod;
            private readonly ConstructorInfo _constructor;
            private DataContract _itemContract;
            private DataContract _sharedTypeContract;
            private DataContractDictionary _knownDataContracts;
            private bool _isKnownTypeAttributeChecked;
            private string _itemName;
            private bool _itemNameSetExplicit;
            private XmlDictionaryString _collectionItemName;
            private string _keyName;
            private string _valueName;
            private XmlDictionaryString _childElementNamespace;
            private string _invalidCollectionInSharedContractMessage;
            private XmlFormatCollectionReaderDelegate _xmlFormatReaderDelegate;
            private XmlFormatGetOnlyCollectionReaderDelegate _xmlFormatGetOnlyCollectionReaderDelegate;
            private XmlFormatCollectionWriterDelegate _xmlFormatWriterDelegate;
            private bool _isConstructorCheckRequired = false;

            internal static Type[] KnownInterfaces
            {
                get
                {
                    if (s_knownInterfaces == null)
                    {
                        // Listed in priority order
                        s_knownInterfaces = new Type[]
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
                    return s_knownInterfaces;
                }
            }

            private void Init(CollectionKind kind, Type itemType, CollectionDataContractAttribute collectionContractAttribute)
            {
                _kind = kind;
                if (itemType != null)
                {
                    _itemType = itemType;

                    bool isDictionary = (kind == CollectionKind.Dictionary || kind == CollectionKind.GenericDictionary);
                    string itemName = null, keyName = null, valueName = null;
                    if (collectionContractAttribute != null)
                    {
                        if (collectionContractAttribute.IsItemNameSetExplicitly)
                        {
                            if (collectionContractAttribute.ItemName == null || collectionContractAttribute.ItemName.Length == 0)
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractItemName, DataContract.GetClrTypeFullName(UnderlyingType))));
                            itemName = DataContract.EncodeLocalName(collectionContractAttribute.ItemName);
                            _itemNameSetExplicit = true;
                        }
                        if (collectionContractAttribute.IsKeyNameSetExplicitly)
                        {
                            if (collectionContractAttribute.KeyName == null || collectionContractAttribute.KeyName.Length == 0)
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractKeyName, DataContract.GetClrTypeFullName(UnderlyingType))));
                            if (!isDictionary)
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractKeyNoDictionary, DataContract.GetClrTypeFullName(UnderlyingType), collectionContractAttribute.KeyName)));
                            keyName = DataContract.EncodeLocalName(collectionContractAttribute.KeyName);
                        }
                        if (collectionContractAttribute.IsValueNameSetExplicitly)
                        {
                            if (collectionContractAttribute.ValueName == null || collectionContractAttribute.ValueName.Length == 0)
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractValueName, DataContract.GetClrTypeFullName(UnderlyingType))));
                            if (!isDictionary)
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractValueNoDictionary, DataContract.GetClrTypeFullName(UnderlyingType), collectionContractAttribute.ValueName)));
                            valueName = DataContract.EncodeLocalName(collectionContractAttribute.ValueName);
                        }
                    }

                    XmlDictionary dictionary = isDictionary ? new XmlDictionary(5) : new XmlDictionary(3);
                    this.Name = dictionary.Add(this.StableName.Name);
                    this.Namespace = dictionary.Add(this.StableName.Namespace);
                    _itemName = itemName ?? DataContract.GetStableName(DataContract.UnwrapNullableType(itemType)).Name;
                    _collectionItemName = dictionary.Add(_itemName);
                    if (isDictionary)
                    {
                        _keyName = keyName ?? Globals.KeyLocalName;
                        _valueName = valueName ?? Globals.ValueLocalName;
                    }
                }
                if (collectionContractAttribute != null)
                {
                    this.IsReference = collectionContractAttribute.IsReference;
                }
            }

            internal CollectionDataContractCriticalHelper(CollectionKind kind)
                : base()
            {
                Init(kind, null, null);
            }

            // array
            internal CollectionDataContractCriticalHelper(Type type) : base(type)
            {
                if (type == Globals.TypeOfArray)
                    type = Globals.TypeOfObjectArray;
                if (type.GetArrayRank() > 1)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SR.Format(SR.SupportForMultidimensionalArraysNotPresent)));
                this.StableName = DataContract.GetStableName(type);
                Init(CollectionKind.Array, type.GetElementType(), null);
            }

            // collection
            internal CollectionDataContractCriticalHelper(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor) : base(type)
            {
                if (getEnumeratorMethod == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionMustHaveGetEnumeratorMethod, DataContract.GetClrTypeFullName(type))));
                if (addMethod == null && !type.GetTypeInfo().IsInterface)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(type))));
                if (itemType == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionMustHaveItemType, DataContract.GetClrTypeFullName(type))));

                CollectionDataContractAttribute collectionContractAttribute;
                this.StableName = DataContract.GetCollectionStableName(type, itemType, out collectionContractAttribute);

                Init(kind, itemType, collectionContractAttribute);
                _getEnumeratorMethod = getEnumeratorMethod;
                _addMethod = addMethod;
                _constructor = constructor;
            }

            // collection
            internal CollectionDataContractCriticalHelper(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor, bool isConstructorCheckRequired)
                : this(type, kind, itemType, getEnumeratorMethod, addMethod, constructor)
            {
                _isConstructorCheckRequired = isConstructorCheckRequired;
            }

            internal CollectionDataContractCriticalHelper(Type type, string invalidCollectionInSharedContractMessage) : base(type)
            {
                Init(CollectionKind.Collection, null /*itemType*/, null);
                _invalidCollectionInSharedContractMessage = invalidCollectionInSharedContractMessage;
            }

            internal CollectionKind Kind
            {
                get { return _kind; }
            }

            internal Type ItemType
            {
                get { return _itemType; }
                set { _itemType = value; }
            }

            internal DataContract ItemContract
            {
                get
                {
                    if (_itemContract == null && UnderlyingType != null)
                    {
                        if (IsDictionary)
                        {
                            if (String.CompareOrdinal(KeyName, ValueName) == 0)
                            {
                                DataContract.ThrowInvalidDataContractException(
                                    SR.Format(SR.DupKeyValueName, DataContract.GetClrTypeFullName(UnderlyingType), KeyName),
                                    UnderlyingType);
                            }
                            _itemContract = ClassDataContract.CreateClassDataContractForKeyValue(ItemType, Namespace, new string[] { KeyName, ValueName });
                            // Ensure that DataContract gets added to the static DataContract cache for dictionary items
                            DataContract.GetDataContract(ItemType);
                        }
                        else
                        {
                            _itemContract = DataContract.GetDataContractFromGeneratedAssembly(ItemType);
                            if (_itemContract == null)
                            {
                                _itemContract = DataContract.GetDataContract(ItemType);
                            }
                        }
                    }
                    return _itemContract;
                }
                set
                {
                    _itemContract = value;
                }
            }

            internal DataContract SharedTypeContract
            {
                get { return _sharedTypeContract; }
                set { _sharedTypeContract = value; }
            }

            internal string ItemName
            {
                get { return _itemName; }
                set { _itemName = value; }
            }

            internal bool IsConstructorCheckRequired
            {
                get { return _isConstructorCheckRequired; }
                set { _isConstructorCheckRequired = value; }
            }

            public XmlDictionaryString CollectionItemName
            {
                get { return _collectionItemName; }
            }

            internal string KeyName
            {
                get { return _keyName; }
                set { _keyName = value; }
            }

            internal string ValueName
            {
                get { return _valueName; }
                set { _valueName = value; }
            }

            internal bool IsDictionary => KeyName != null;

            public XmlDictionaryString ChildElementNamespace
            {
                get { return _childElementNamespace; }
                set { _childElementNamespace = value; }
            }

            internal MethodInfo GetEnumeratorMethod => _getEnumeratorMethod;

            internal MethodInfo AddMethod => _addMethod;

            internal ConstructorInfo Constructor => _constructor;

            internal override DataContractDictionary KnownDataContracts
            {
                get
                {
                    if (!_isKnownTypeAttributeChecked && UnderlyingType != null)
                    {
                        lock (this)
                        {
                            if (!_isKnownTypeAttributeChecked)
                            {
                                _knownDataContracts = DataContract.ImportKnownTypeAttributes(this.UnderlyingType);
                                Interlocked.MemoryBarrier();
                                _isKnownTypeAttributeChecked = true;
                            }
                        }
                    }
                    return _knownDataContracts;
                }

                set
                { _knownDataContracts = value; }
            }

            internal string InvalidCollectionInSharedContractMessage => _invalidCollectionInSharedContractMessage;

            internal bool ItemNameSetExplicit => _itemNameSetExplicit;

            internal XmlFormatCollectionWriterDelegate XmlFormatWriterDelegate
            {
                get { return _xmlFormatWriterDelegate; }
                set { _xmlFormatWriterDelegate = value; }
            }

            internal XmlFormatCollectionReaderDelegate XmlFormatReaderDelegate
            {
                get { return _xmlFormatReaderDelegate; }
                set { _xmlFormatReaderDelegate = value; }
            }

            internal XmlFormatGetOnlyCollectionReaderDelegate XmlFormatGetOnlyCollectionReaderDelegate
            {
                get { return _xmlFormatGetOnlyCollectionReaderDelegate; }
                set { _xmlFormatGetOnlyCollectionReaderDelegate = value; }
            }

            private delegate void IncrementCollectionCountDelegate(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context);

            private IncrementCollectionCountDelegate _incrementCollectionCountDelegate = null;

            private static void DummyIncrementCollectionCount(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context) { }

            internal void IncrementCollectionCount(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
            {
                if (_incrementCollectionCountDelegate == null)
                {
                    switch (Kind)
                    {
                        case CollectionKind.Collection:
                        case CollectionKind.List:
                        case CollectionKind.Dictionary:
                            {
                                _incrementCollectionCountDelegate = (x, o, c) =>
                                {
                                    context.IncrementCollectionCount(x, (ICollection)o);
                                };
                            }
                            break;
                        case CollectionKind.GenericCollection:
                        case CollectionKind.GenericList:
                            {
                                var buildIncrementCollectionCountDelegate = s_buildIncrementCollectionCountDelegateMethod.MakeGenericMethod(ItemType);
                                _incrementCollectionCountDelegate = (IncrementCollectionCountDelegate)buildIncrementCollectionCountDelegate.Invoke(null, Array.Empty<object>());
                            }
                            break;
                        case CollectionKind.GenericDictionary:
                            {
                                var buildIncrementCollectionCountDelegate = s_buildIncrementCollectionCountDelegateMethod.MakeGenericMethod(Globals.TypeOfKeyValuePair.MakeGenericType(ItemType.GetGenericArguments()));
                                _incrementCollectionCountDelegate = (IncrementCollectionCountDelegate)buildIncrementCollectionCountDelegate.Invoke(null, Array.Empty<object>());
                            }
                            break;
                        default:
                            // Do nothing.
                            _incrementCollectionCountDelegate = DummyIncrementCollectionCount;
                            break;
                    }
                }

                _incrementCollectionCountDelegate(xmlWriter, obj, context);
            }

            private static MethodInfo s_buildIncrementCollectionCountDelegateMethod = typeof(CollectionDataContractCriticalHelper).GetMethod(nameof(BuildIncrementCollectionCountDelegate), Globals.ScanAllMembers);

            private static IncrementCollectionCountDelegate BuildIncrementCollectionCountDelegate<T>()
            {
                return (xmlwriter, obj, context) =>
                {
                    context.IncrementCollectionCountGeneric<T>(xmlwriter, (ICollection<T>)obj);
    
                };
            }

            private delegate IEnumerator CreateGenericDictionaryEnumeratorDelegate(IEnumerator enumerator);

            private CreateGenericDictionaryEnumeratorDelegate _createGenericDictionaryEnumeratorDelegate;

            internal IEnumerator GetEnumeratorForCollection(object obj, out Type enumeratorReturnType)
            {
                IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
                if (Kind == CollectionKind.GenericDictionary)
                {
                    if (_createGenericDictionaryEnumeratorDelegate == null)
                    {
                        var keyValueTypes = ItemType.GetGenericArguments();
                        var buildCreateGenericDictionaryEnumerator = s_buildCreateGenericDictionaryEnumerator.MakeGenericMethod(keyValueTypes[0], keyValueTypes[1]);
                        _createGenericDictionaryEnumeratorDelegate = (CreateGenericDictionaryEnumeratorDelegate)buildCreateGenericDictionaryEnumerator.Invoke(null, Array.Empty<object>());
                    }

                    enumerator = _createGenericDictionaryEnumeratorDelegate(enumerator);                                       
                }
                else if (Kind == CollectionKind.Dictionary)
                {
                    enumerator = new DictionaryEnumerator(((IDictionary)obj).GetEnumerator());
                }

                enumeratorReturnType = EnumeratorReturnType;

                return enumerator;
            }

            private Type _enumeratorReturnType;

            public Type EnumeratorReturnType
            {
                get
                {
                    _enumeratorReturnType = _enumeratorReturnType ?? GetCollectionEnumeratorReturnType();
                    return _enumeratorReturnType;
                }
            }

            private Type GetCollectionEnumeratorReturnType()
            {
                Type enumeratorReturnType;
                if (Kind == CollectionKind.GenericDictionary)
                {
                    var keyValueTypes = ItemType.GetGenericArguments();
                    enumeratorReturnType =  Globals.TypeOfKeyValue.MakeGenericType(keyValueTypes);
                }
                else if (Kind == CollectionKind.Dictionary)
                {

                    enumeratorReturnType = Globals.TypeOfObject;
                }
                else if (Kind == CollectionKind.GenericCollection
                      || Kind == CollectionKind.GenericList)
                {
                    enumeratorReturnType = ItemType;
                }
                else
                {
                    var enumeratorType = GetEnumeratorMethod.ReturnType;
                    if (enumeratorType.GetTypeInfo().IsGenericType)
                    {
                        MethodInfo getCurrentMethod = enumeratorType.GetMethod(Globals.GetCurrentMethodName, BindingFlags.Instance | BindingFlags.Public, Array.Empty<Type>());
                        enumeratorReturnType = getCurrentMethod.ReturnType;
                    }
                    else
                    {
                        enumeratorReturnType = Globals.TypeOfObject;
                    }
                }

                return enumeratorReturnType;
            }

            private static MethodInfo s_buildCreateGenericDictionaryEnumerator = typeof(CollectionDataContractCriticalHelper).GetMethod(nameof(BuildCreateGenericDictionaryEnumerator), Globals.ScanAllMembers);
            private static CreateGenericDictionaryEnumeratorDelegate BuildCreateGenericDictionaryEnumerator<K, V>()
            {
                return (enumerator) =>
                {
                    return new GenericDictionaryEnumerator<K, V>((IEnumerator<KeyValuePair<K, V>>)enumerator);
                };
            }
        }

        private DataContract GetSharedTypeContract(Type type)
        {
            if (type.GetTypeInfo().IsDefined(Globals.TypeOfCollectionDataContractAttribute, false))
            {
                return this;
            }
            if (type.GetTypeInfo().IsDefined(Globals.TypeOfDataContractAttribute, false))
            {
                return new ClassDataContract(type);
            }
            return null;
        }

        internal static bool IsCollectionInterface(Type type)
        {
            if (type.GetTypeInfo().IsGenericType)
                type = type.GetGenericTypeDefinition();
            return ((IList<Type>)KnownInterfaces).Contains(type);
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

        internal static bool IsCollection(Type type, bool constructorRequired)
        {
            Type itemType;
            return IsCollectionHelper(type, out itemType, constructorRequired);
        }

        private static bool IsCollectionHelper(Type type, out Type itemType, bool constructorRequired)
        {
            if (type.IsArray && DataContract.GetBuiltInDataContract(type) == null)
            {
                itemType = type.GetElementType();
                return true;
            }
            DataContract dataContract;
            return IsCollectionOrTryCreate(type, false /*tryCreate*/, out dataContract, out itemType, constructorRequired);
        }

        internal static bool TryCreate(Type type, out DataContract dataContract)
        {
            Type itemType;
            return IsCollectionOrTryCreate(type, true /*tryCreate*/, out dataContract, out itemType, true /*constructorRequired*/);
        }

        internal static bool CreateGetOnlyCollectionDataContract(Type type, out DataContract dataContract)
        {
            Type itemType;
            if (type.IsArray)
            {
                dataContract = new CollectionDataContract(type);
                return true;
            }
            else
            {
                return IsCollectionOrTryCreate(type, true /*tryCreate*/, out dataContract, out itemType, false /*constructorRequired*/);
            }
        }

        internal static bool TryCreateGetOnlyCollectionDataContract(Type type, out DataContract dataContract)
        {
            dataContract = DataContract.GetDataContractFromGeneratedAssembly(type);
            if (dataContract == null)
            {
                Type itemType;
                if (type.IsArray)
                {
                    dataContract = new CollectionDataContract(type);
                    return true;
                }
                else
                {
                    return IsCollectionOrTryCreate(type, true /*tryCreate*/, out dataContract, out itemType, false /*constructorRequired*/);
                }
            }
            else
            {
                if (dataContract is CollectionDataContract)
                {
                    return true;
                }
                else
                {
                    dataContract = null;
                    return false;
                }
            }
        }

        internal static MethodInfo GetTargetMethodWithName(string name, Type type, Type interfaceType)
        {
            Type t = type.GetInterfaces().Where(it => it.Equals(interfaceType)).FirstOrDefault();
            if (t == null)
                return null;
            return t.GetMethod(name);
        }

        private static bool IsArraySegment(Type t)
        {
            return t.GetTypeInfo().IsGenericType && (t.GetGenericTypeDefinition() == typeof(ArraySegment<>));
        }

        private static bool IsCollectionOrTryCreate(Type type, bool tryCreate, out DataContract dataContract, out Type itemType, bool constructorRequired)
        {
            dataContract = null;
            itemType = Globals.TypeOfObject;

            if (DataContract.GetBuiltInDataContract(type) != null)
            {
                return HandleIfInvalidCollection(type, tryCreate, false/*hasCollectionDataContract*/, false/*isBaseTypeCollection*/,
                    SR.CollectionTypeCannotBeBuiltIn, null, ref dataContract);
            }
            MethodInfo addMethod, getEnumeratorMethod;
            bool hasCollectionDataContract = IsCollectionDataContract(type);
            Type baseType = type.GetTypeInfo().BaseType;
            bool isBaseTypeCollection = (baseType != null && baseType != Globals.TypeOfObject
                && baseType != Globals.TypeOfValueType && baseType != Globals.TypeOfUri) ? IsCollection(baseType) : false;

            if (type.GetTypeInfo().IsDefined(Globals.TypeOfDataContractAttribute, false))
            {
                return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, isBaseTypeCollection,
                    SR.CollectionTypeCannotHaveDataContract, null, ref dataContract);
            }

            if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type) || IsArraySegment(type))
            {
                return false;
            }

            if (!Globals.TypeOfIEnumerable.IsAssignableFrom(type))
            {
                return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, isBaseTypeCollection,
                    SR.CollectionTypeIsNotIEnumerable, null, ref dataContract);
            }
            if (type.GetTypeInfo().IsInterface)
            {
                Type interfaceTypeToCheck = type.GetTypeInfo().IsGenericType ? type.GetGenericTypeDefinition() : type;
                Type[] knownInterfaces = KnownInterfaces;
                for (int i = 0; i < knownInterfaces.Length; i++)
                {
                    if (knownInterfaces[i] == interfaceTypeToCheck)
                    {
                        addMethod = null;
                        if (type.GetTypeInfo().IsGenericType)
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

                                // ICollection<T> has AddMethod
                                var collectionType = Globals.TypeOfICollectionGeneric.MakeGenericType(itemType);
                                if (collectionType.IsAssignableFrom(type))
                                {
                                    addMethod = collectionType.GetMethod(Globals.AddMethodName);
                                }

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

                                // IList has AddMethod
                                if (interfaceTypeToCheck == Globals.TypeOfIList)
                                {
                                    addMethod = type.GetMethod(Globals.AddMethodName);
                                }
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
            if (!type.GetTypeInfo().IsValueType)
            {
                defaultCtor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Array.Empty<Type>());
                if (defaultCtor == null && constructorRequired)
                {
                    return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, isBaseTypeCollection/*createContractWithException*/,
                        SR.CollectionTypeDoesNotHaveDefaultCtor, null, ref dataContract);
                }
            }

            Type knownInterfaceType = null;
            CollectionKind kind = CollectionKind.None;
            bool multipleDefinitions = false;
            Type[] interfaceTypes = type.GetInterfaces();
            foreach (Type interfaceType in interfaceTypes)
            {
                Type interfaceTypeToCheck = interfaceType.GetTypeInfo().IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
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
                return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, isBaseTypeCollection,
                    SR.CollectionTypeIsNotIEnumerable, null, ref dataContract);
            }

            if (kind == CollectionKind.Enumerable || kind == CollectionKind.Collection || kind == CollectionKind.GenericEnumerable)
            {
                if (multipleDefinitions)
                    knownInterfaceType = Globals.TypeOfIEnumerable;
                itemType = knownInterfaceType.GetTypeInfo().IsGenericType ? knownInterfaceType.GetGenericArguments()[0] : Globals.TypeOfObject;
                GetCollectionMethods(type, knownInterfaceType, new Type[] { itemType },
                                     false /*addMethodOnInterface*/,
                                     out getEnumeratorMethod, out addMethod);
                if (addMethod == null)
                {
                    return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, isBaseTypeCollection/*createContractWithException*/,
                        SR.CollectionTypeDoesNotHaveAddMethod, DataContract.GetClrTypeFullName(itemType), ref dataContract);
                }
                if (tryCreate)
                    dataContract = new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, addMethod, defaultCtor, !constructorRequired);
            }
            else
            {
                if (multipleDefinitions)
                {
                    return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, isBaseTypeCollection/*createContractWithException*/,
                        SR.CollectionTypeHasMultipleDefinitionsOfInterface, KnownInterfaces[(int)kind - 1].Name, ref dataContract);
                }
                Type[] addMethodTypeArray = null;
                switch (kind)
                {
                    case CollectionKind.GenericDictionary:
                        addMethodTypeArray = knownInterfaceType.GetGenericArguments();
                        bool isOpenGeneric = knownInterfaceType.GetTypeInfo().IsGenericTypeDefinition
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
                                     true /*addMethodOnInterface*/,
                                     out getEnumeratorMethod, out addMethod);

                    dataContract = DataContract.GetDataContractFromGeneratedAssembly(type);
                    if (dataContract == null)
                    {
                        dataContract = new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, addMethod, defaultCtor, !constructorRequired);
                    }
                }
            }

            return true;
        }

        internal static bool IsCollectionDataContract(Type type)
        {
            return type.GetTypeInfo().IsDefined(Globals.TypeOfCollectionDataContractAttribute, false);
        }

        private static bool HandleIfInvalidCollection(Type type, bool tryCreate, bool hasCollectionDataContract, bool createContractWithException, string message, string param, ref DataContract dataContract)
        {
            if (hasCollectionDataContract)
            {
                if (tryCreate)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(GetInvalidCollectionMessage(message, SR.Format(SR.InvalidCollectionDataContract, DataContract.GetClrTypeFullName(type)), param)));
                return true;
            }

            if (createContractWithException)
            {
                if (tryCreate)
                    dataContract = new CollectionDataContract(type, GetInvalidCollectionMessage(message, SR.Format(SR.InvalidCollectionType, DataContract.GetClrTypeFullName(type)), param));
                return true;
            }

            return false;
        }

        private static string GetInvalidCollectionMessage(string message, string nestedMessage, string param)
        {
            return (param == null) ? SR.Format(message, nestedMessage) : SR.Format(message, nestedMessage, param);
        }

        private static void FindCollectionMethodsOnInterface(Type type, Type interfaceType, ref MethodInfo addMethod, ref MethodInfo getEnumeratorMethod)
        {
            Type t = type.GetInterfaces().Where(it => it.Equals(interfaceType)).FirstOrDefault();
            if (t != null)
            {
                addMethod = t.GetMethod(Globals.AddMethodName) ?? addMethod;
                getEnumeratorMethod = t.GetMethod(Globals.GetEnumeratorMethodName) ?? getEnumeratorMethod;
            }
        }

        private static void GetCollectionMethods(Type type, Type interfaceType, Type[] addMethodTypeArray, bool addMethodOnInterface, out MethodInfo getEnumeratorMethod, out MethodInfo addMethod)
        {
            addMethod = getEnumeratorMethod = null;

            if (addMethodOnInterface)
            {
                addMethod = type.GetMethod(Globals.AddMethodName, BindingFlags.Instance | BindingFlags.Public, addMethodTypeArray);
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
                // GetMethod returns Add() method with parameter closest matching T in assignability/inheritance chain
                addMethod = type.GetMethod(Globals.AddMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, addMethodTypeArray);
                if (addMethod == null)
                    return;
            }

            if (getEnumeratorMethod == null)
            {
                getEnumeratorMethod = type.GetMethod(Globals.GetEnumeratorMethodName, BindingFlags.Instance | BindingFlags.Public, Array.Empty<Type>());
                if (getEnumeratorMethod == null || !Globals.TypeOfIEnumerator.IsAssignableFrom(getEnumeratorMethod.ReturnType))
                {
                    Type ienumerableInterface = interfaceType.GetInterfaces().Where(t => t.FullName.StartsWith("System.Collections.Generic.IEnumerable")).FirstOrDefault();
                    if (ienumerableInterface == null)
                        ienumerableInterface = Globals.TypeOfIEnumerable;
                    getEnumeratorMethod = GetTargetMethodWithName(Globals.GetEnumeratorMethodName, type, ienumerableInterface);
                }
            }
        }

        private static bool IsKnownInterface(Type type)
        {
            Type typeToCheck = type.GetTypeInfo().IsGenericType ? type.GetGenericTypeDefinition() : type;
            foreach (Type knownInterfaceType in KnownInterfaces)
            {
                if (typeToCheck == knownInterfaceType)
                {
                    return true;
                }
            }
            return false;
        }

        internal override DataContract GetValidContract(SerializationMode mode)
        {
            if (InvalidCollectionInSharedContractMessage != null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(InvalidCollectionInSharedContractMessage));

            return this;
        }

        internal override DataContract GetValidContract()
        {
            if (this.IsConstructorCheckRequired)
            {
                CheckConstructor();
            }
            return this;
        }

        private void CheckConstructor()
        {
            if (this.Constructor == null)
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionTypeDoesNotHaveDefaultCtor, DataContract.GetClrTypeFullName(this.UnderlyingType))));
            }
            else
            {
                this.IsConstructorCheckRequired = false;
            }
        }

        internal override bool IsValidContract(SerializationMode mode)
        {
            return (InvalidCollectionInSharedContractMessage == null);
        }

        /// <SecurityNote>
        /// Review - calculates whether this collection requires MemberAccessPermission for deserialization.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal bool RequiresMemberAccessForRead(SecurityException securityException)
        {
            if (!IsTypeVisible(UnderlyingType))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException));
                }
                return true;
            }
            if (ItemType != null && !IsTypeVisible(ItemType))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(ItemType)),
                            securityException));
                }
                return true;
            }
            if (ConstructorRequiresMemberAccess(Constructor))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractNoPublicConstructor,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException));
                }
                return true;
            }
            if (MethodRequiresMemberAccess(this.AddMethod))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                           new SecurityException(SR.Format(
                                   SR.PartialTrustCollectionContractAddMethodNotPublic,
                                   DataContract.GetClrTypeFullName(UnderlyingType),
                                   this.AddMethod.Name),
                               securityException));
                }
                return true;
            }

            return false;
        }

        /// <SecurityNote>
        /// Review - calculates whether this collection requires MemberAccessPermission for serialization.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal bool RequiresMemberAccessForWrite(SecurityException securityException)
        {
            if (!IsTypeVisible(UnderlyingType))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException));
                }
                return true;
            }
            if (ItemType != null && !IsTypeVisible(ItemType))
            {
                if (securityException != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(ItemType)),
                            securityException));
                }
                return true;
            }

            return false;
        }

        public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
        {
            // IsGetOnlyCollection value has already been used to create current collectiondatacontract, value can now be reset. 
            context.IsGetOnlyCollection = false;
            XmlFormatWriterDelegate(xmlWriter, obj, context, this);
        }

        public override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
        {
            xmlReader.Read();
            object o = null;
            if (context.IsGetOnlyCollection)
            {
                // IsGetOnlyCollection value has already been used to create current collectiondatacontract, value can now be reset. 
                context.IsGetOnlyCollection = false;
#if NET_NATIVE
                if (XmlFormatGetOnlyCollectionReaderDelegate == null)
                {
                    throw new InvalidDataContractException(SR.Format(SR.SerializationCodeIsMissingForType, UnderlyingType.ToString()));
                }
#endif
                XmlFormatGetOnlyCollectionReaderDelegate(xmlReader, context, CollectionItemName, Namespace, this);
            }
            else
            {
                o = XmlFormatReaderDelegate(xmlReader, context, CollectionItemName, Namespace, this);
            }
            xmlReader.ReadEndElement();
            return o;
        }

        internal class DictionaryEnumerator : IEnumerator<KeyValue<object, object>>
        {
            private IDictionaryEnumerator _enumerator;

            public DictionaryEnumerator(IDictionaryEnumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public KeyValue<object, object> Current
            {
                get { return new KeyValue<object, object>(_enumerator.Key, _enumerator.Value); }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }

        internal class GenericDictionaryEnumerator<K, V> : IEnumerator<KeyValue<K, V>>
        {
            private IEnumerator<KeyValuePair<K, V>> _enumerator;

            public GenericDictionaryEnumerator(IEnumerator<KeyValuePair<K, V>> enumerator)
            {
                _enumerator = enumerator;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public KeyValue<K, V> Current
            {
                get
                {
                    KeyValuePair<K, V> current = _enumerator.Current;
                    return new KeyValue<K, V>(current.Key, current.Value);
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }
    }
}
