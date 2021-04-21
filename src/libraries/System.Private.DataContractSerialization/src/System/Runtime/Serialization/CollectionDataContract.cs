// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Xml;
    using System.Linq;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;
    using System.Security;
    using System.Diagnostics.CodeAnalysis;

    // The interface is a perf optimization.
    // Only KeyValuePairAdapter should implement the interface.
    internal interface IKeyValuePairAdapter { }

    //Special Adapter class to serialize KeyValuePair as Dictionary needs it when polymorphism is involved
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/System.Collections.Generic")]
    internal sealed class KeyValuePairAdapter<K, T> : IKeyValuePairAdapter
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

    internal interface IKeyValue
    {
        object? Key { get; set; }
        object? Value { get; set; }
    }

    [DataContract(Namespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
    internal struct KeyValue<K, V> : IKeyValue
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

        object? IKeyValue.Key
        {
            get { return _key; }
            set { _key = (K)value!; }
        }

        object? IKeyValue.Value
        {
            get { return _value; }
            set { _value = (V)value!; }
        }
    }

    internal enum CollectionKind : byte
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

    internal sealed class CollectionDataContract : DataContract
    {
        private XmlDictionaryString _collectionItemName;

        private XmlDictionaryString? _childElementNamespace;

        private DataContract? _itemContract;

        private CollectionDataContractCriticalHelper _helper;

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal CollectionDataContract(Type type) : base(new CollectionDataContractCriticalHelper(type))
        {
            InitCollectionDataContract(this);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, string? deserializationExceptionMessage)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, deserializationExceptionMessage))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo? addMethod, ConstructorInfo? constructor)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo? addMethod, ConstructorInfo? constructor, bool isConstructorCheckRequired)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor, isConstructorCheckRequired))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private CollectionDataContract(Type type, string invalidCollectionInSharedContractMessage) : base(new CollectionDataContractCriticalHelper(type, invalidCollectionInSharedContractMessage))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        [MemberNotNull(nameof(_helper))]
        [MemberNotNull(nameof(_collectionItemName))]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void InitCollectionDataContract(DataContract? sharedTypeContract)
        {
            _helper = (base.Helper as CollectionDataContractCriticalHelper)!;
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
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
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

        internal DataContract? SharedTypeContract
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

        public string? KeyName
        {
            get
            { return _helper.KeyName; }

            set
            { _helper.KeyName = value; }
        }

        public string? ValueName
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

        public XmlDictionaryString? ChildElementNamespace
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
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
                                XmlDictionaryString? tempChildElementNamespace = ClassDataContract.GetChildNamespaceToDeclare(this, ItemType, new XmlDictionary());
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

        internal bool IsItemTypeNullable
        {
            get { return _helper.IsItemTypeNullable; }
            set { _helper.IsItemTypeNullable = value; }
        }

        internal bool IsConstructorCheckRequired
        {
            get
            { return _helper.IsConstructorCheckRequired; }

            set
            { _helper.IsConstructorCheckRequired = value; }
        }

        internal MethodInfo? GetEnumeratorMethod
        {
            get
            { return _helper.GetEnumeratorMethod; }
        }

        internal MethodInfo? AddMethod
        {
            get
            { return _helper.AddMethod; }
        }

        internal ConstructorInfo? Constructor
        {
            get
            { return _helper.Constructor; }
        }

        public override DataContractDictionary? KnownDataContracts
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            { return _helper.KnownDataContracts; }

            set
            { _helper.KnownDataContracts = value; }
        }

        internal string? InvalidCollectionInSharedContractMessage
        {
            get
            { return _helper.InvalidCollectionInSharedContractMessage; }
        }

        internal string? DeserializationExceptionMessage
        {
            get { return _helper.DeserializationExceptionMessage; }
        }

        internal bool IsReadOnlyContract
        {
            get { return DeserializationExceptionMessage != null; }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatCollectionWriterDelegate CreateXmlFormatWriterDelegate()
        {
            return new XmlFormatWriterGenerator().GenerateCollectionWriter(this);
        }

        internal XmlFormatCollectionWriterDelegate XmlFormatWriterDelegate
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_helper.XmlFormatWriterDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatWriterDelegate == null)
                        {
                            XmlFormatCollectionWriterDelegate tempDelegate = CreateXmlFormatWriterDelegate();
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatWriterDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatWriterDelegate;
            }
            set
            {
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatCollectionReaderDelegate CreateXmlFormatReaderDelegate()
        {
            return new XmlFormatReaderGenerator().GenerateCollectionReader(this);
        }

        internal XmlFormatCollectionReaderDelegate XmlFormatReaderDelegate
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_helper.XmlFormatReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatReaderDelegate == null)
                        {
                            if (IsReadOnlyContract)
                            {
                                ThrowInvalidDataContractException(_helper.DeserializationExceptionMessage, type: null);
                            }

                            XmlFormatCollectionReaderDelegate tempDelegate = CreateXmlFormatReaderDelegate();
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatReaderDelegate;
            }
            set
            {
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatGetOnlyCollectionReaderDelegate CreateXmlFormatGetOnlyCollectionReaderDelegate()
        {
            return new XmlFormatReaderGenerator().GenerateGetOnlyCollectionReader(this);
        }


        internal XmlFormatGetOnlyCollectionReaderDelegate XmlFormatGetOnlyCollectionReaderDelegate
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_helper.XmlFormatGetOnlyCollectionReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatGetOnlyCollectionReaderDelegate == null)
                        {
                            if (UnderlyingType.IsInterface && (Kind == CollectionKind.Enumerable || Kind == CollectionKind.Collection || Kind == CollectionKind.GenericEnumerable))
                            {
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GetOnlyCollectionMustHaveAddMethod, GetClrTypeFullName(UnderlyingType))));
                            }

                            if (IsReadOnlyContract)
                            {
                                ThrowInvalidDataContractException(_helper.DeserializationExceptionMessage, type: null);
                            }

                            if (Kind != CollectionKind.Array && AddMethod == null)
                            {
                                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.GetOnlyCollectionMustHaveAddMethod, GetClrTypeFullName(UnderlyingType))));
                            }

                            XmlFormatGetOnlyCollectionReaderDelegate tempDelegate = CreateXmlFormatGetOnlyCollectionReaderDelegate();
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatGetOnlyCollectionReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatGetOnlyCollectionReaderDelegate;
            }
            set
            {
            }
        }

        internal void IncrementCollectionCount(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
        {
            _helper.IncrementCollectionCount(xmlWriter, obj, context);
        }

        internal IEnumerator GetEnumeratorForCollection(object obj)
        {
            return _helper.GetEnumeratorForCollection(obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal Type GetCollectionElementType()
        {
            return _helper.GetCollectionElementType();
        }

        private sealed class CollectionDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private static Type[]? s_knownInterfaces;

            private Type _itemType = null!; // _itemType is always set except for the "invalid" CollectionDataContract
            private bool _isItemTypeNullable;
            private CollectionKind _kind;
            private readonly MethodInfo? _getEnumeratorMethod;
            private readonly MethodInfo? _addMethod;
            private readonly ConstructorInfo? _constructor;
            private readonly string? _deserializationExceptionMessage;
            private DataContract? _itemContract;
            private DataContract? _sharedTypeContract;
            private DataContractDictionary? _knownDataContracts;
            private bool _isKnownTypeAttributeChecked;
            private string _itemName = null!; // _itemName is always set except for the "invalid" CollectionDataContract
            private bool _itemNameSetExplicit;
            private XmlDictionaryString _collectionItemName = null!; // _itemName is always set except for the "invalid" CollectionDataContract
            private string? _keyName;
            private string? _valueName;
            private XmlDictionaryString? _childElementNamespace;
            private readonly string? _invalidCollectionInSharedContractMessage;
            private XmlFormatCollectionReaderDelegate? _xmlFormatReaderDelegate;
            private XmlFormatGetOnlyCollectionReaderDelegate? _xmlFormatGetOnlyCollectionReaderDelegate;
            private XmlFormatCollectionWriterDelegate? _xmlFormatWriterDelegate;
            private bool _isConstructorCheckRequired;

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

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            private void Init(CollectionKind kind, Type? itemType, CollectionDataContractAttribute? collectionContractAttribute)
            {
                _kind = kind;
                if (itemType != null)
                {
                    _itemType = itemType;
                    _isItemTypeNullable = DataContract.IsTypeNullable(itemType);

                    bool isDictionary = (kind == CollectionKind.Dictionary || kind == CollectionKind.GenericDictionary);
                    string? itemName = null, keyName = null, valueName = null;
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

            // array
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type) : base(type)
            {
                if (type == Globals.TypeOfArray)
                    type = Globals.TypeOfObjectArray;
                if (type.GetArrayRank() > 1)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SR.SupportForMultidimensionalArraysNotPresent));
                this.StableName = DataContract.GetStableName(type);
                Init(CollectionKind.Array, type.GetElementType(), null);
            }

            // read-only collection
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type,
                CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, string? deserializationExceptionMessage)
                : base(type)
            {
                if (getEnumeratorMethod == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionMustHaveGetEnumeratorMethod, GetClrTypeFullName(type))));
                if (itemType == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionMustHaveItemType, GetClrTypeFullName(type))));

                CollectionDataContractAttribute? collectionContractAttribute;
                this.StableName = DataContract.GetCollectionStableName(type, itemType, out collectionContractAttribute);

                Init(kind, itemType, collectionContractAttribute);
                _getEnumeratorMethod = getEnumeratorMethod;
                _deserializationExceptionMessage = deserializationExceptionMessage;
            }

            // collection
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type,
                CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo? addMethod, ConstructorInfo? constructor) : base(type)
            {
                if (getEnumeratorMethod == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionMustHaveGetEnumeratorMethod, DataContract.GetClrTypeFullName(type))));
                if (addMethod == null && !type.IsInterface)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(type))));
                if (itemType == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.CollectionMustHaveItemType, DataContract.GetClrTypeFullName(type))));

                CollectionDataContractAttribute? collectionContractAttribute;
                this.StableName = DataContract.GetCollectionStableName(type, itemType, out collectionContractAttribute);

                Init(kind, itemType, collectionContractAttribute);
                _getEnumeratorMethod = getEnumeratorMethod;
                _addMethod = addMethod;
                _constructor = constructor;
            }

            // collection
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo? addMethod, ConstructorInfo? constructor, bool isConstructorCheckRequired)
                : this(type, kind, itemType, getEnumeratorMethod, addMethod, constructor)
            {
                _isConstructorCheckRequired = isConstructorCheckRequired;
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type,
                string invalidCollectionInSharedContractMessage) : base(type)
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
                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
                get
                {
                    if (_itemContract == null)
                    {
                        if (IsDictionary)
                        {
                            if (string.CompareOrdinal(KeyName, ValueName) == 0)
                            {
                                DataContract.ThrowInvalidDataContractException(
                                    SR.Format(SR.DupKeyValueName, DataContract.GetClrTypeFullName(UnderlyingType), KeyName),
                                    UnderlyingType);
                            }
                            Debug.Assert(KeyName != null);
                            Debug.Assert(ValueName != null);
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

            internal DataContract? SharedTypeContract
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

            internal string? KeyName
            {
                get { return _keyName; }
                set { _keyName = value; }
            }

            internal string? ValueName
            {
                get { return _valueName; }
                set { _valueName = value; }
            }

            internal bool IsDictionary => KeyName != null;

            public string? DeserializationExceptionMessage => _deserializationExceptionMessage;

            public XmlDictionaryString? ChildElementNamespace
            {
                get { return _childElementNamespace; }
                set { _childElementNamespace = value; }
            }

            internal bool IsItemTypeNullable
            {
                get { return _isItemTypeNullable; }
                set { _isItemTypeNullable = value; }
            }

            internal MethodInfo? GetEnumeratorMethod => _getEnumeratorMethod;

            internal MethodInfo? AddMethod => _addMethod;

            internal ConstructorInfo? Constructor => _constructor;

            internal override DataContractDictionary? KnownDataContracts
            {
                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
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

            internal string? InvalidCollectionInSharedContractMessage => _invalidCollectionInSharedContractMessage;

            internal bool ItemNameSetExplicit => _itemNameSetExplicit;

            internal XmlFormatCollectionWriterDelegate? XmlFormatWriterDelegate
            {
                get { return _xmlFormatWriterDelegate; }
                set { _xmlFormatWriterDelegate = value; }
            }

            internal XmlFormatCollectionReaderDelegate? XmlFormatReaderDelegate
            {
                get { return _xmlFormatReaderDelegate; }
                set { _xmlFormatReaderDelegate = value; }
            }

            internal XmlFormatGetOnlyCollectionReaderDelegate? XmlFormatGetOnlyCollectionReaderDelegate
            {
                get { return _xmlFormatGetOnlyCollectionReaderDelegate; }
                set { _xmlFormatGetOnlyCollectionReaderDelegate = value; }
            }

            private delegate void IncrementCollectionCountDelegate(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context);

            private IncrementCollectionCountDelegate? _incrementCollectionCountDelegate;

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
                                    c.IncrementCollectionCount(x, (ICollection)o);
                                };
                            }
                            break;
                        case CollectionKind.GenericCollection:
                        case CollectionKind.GenericList:
                            {
                                var buildIncrementCollectionCountDelegate = GetBuildIncrementCollectionCountGenericDelegate(ItemType);
                                _incrementCollectionCountDelegate = (IncrementCollectionCountDelegate)buildIncrementCollectionCountDelegate.Invoke(null, Array.Empty<object>())!;
                            }
                            break;
                        case CollectionKind.GenericDictionary:
                            {
                                var buildIncrementCollectionCountDelegate = GetBuildIncrementCollectionCountGenericDelegate(typeof(KeyValuePair<,>).MakeGenericType(ItemType.GetGenericArguments()));
                                _incrementCollectionCountDelegate = (IncrementCollectionCountDelegate)buildIncrementCollectionCountDelegate.Invoke(null, Array.Empty<object>())!;
                            }
                            break;
                        default:
                            // Do nothing.
                            _incrementCollectionCountDelegate = DummyIncrementCollectionCount;
                            break;
                    }
                }

                _incrementCollectionCountDelegate(xmlWriter, obj, context);

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
                Justification = "The call to MakeGenericMethod is safe due to the fact that CollectionDataContractCriticalHelper.BuildIncrementCollectionCountDelegate<T> is not annotated.")]
                static MethodInfo GetBuildIncrementCollectionCountGenericDelegate(Type type) => BuildIncrementCollectionCountDelegateMethod.MakeGenericMethod(type);
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
                Justification = "The call to MakeGenericMethod is safe due to the fact that CollectionDataContractCriticalHelper.BuildIncrementCollectionCountDelegate<T> is not annotated.")]
            private MethodInfo GetBuildIncrementCollectionCountGenericDelegate(Type type) => BuildIncrementCollectionCountDelegateMethod.MakeGenericMethod(type);

            private static MethodInfo? s_buildIncrementCollectionCountDelegateMethod;

            private static MethodInfo BuildIncrementCollectionCountDelegateMethod
            {
                get
                {
                    if (s_buildIncrementCollectionCountDelegateMethod == null)
                    {
                        s_buildIncrementCollectionCountDelegateMethod = typeof(CollectionDataContractCriticalHelper).GetMethod(nameof(BuildIncrementCollectionCountDelegate), Globals.ScanAllMembers)!;
                    }

                    return s_buildIncrementCollectionCountDelegateMethod;
                }
            }

            private static IncrementCollectionCountDelegate BuildIncrementCollectionCountDelegate<T>()
            {
                return (xmlwriter, obj, context) =>
                {
                    context.IncrementCollectionCountGeneric<T>(xmlwriter, (ICollection<T>)obj);

                };
            }

            private delegate IEnumerator CreateGenericDictionaryEnumeratorDelegate(IEnumerator enumerator);

            private CreateGenericDictionaryEnumeratorDelegate? _createGenericDictionaryEnumeratorDelegate;

            internal IEnumerator GetEnumeratorForCollection(object obj)
            {
                IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
                if (Kind == CollectionKind.GenericDictionary)
                {
                    if (_createGenericDictionaryEnumeratorDelegate == null)
                    {
                        var keyValueTypes = ItemType.GetGenericArguments();
                        MethodInfo buildCreateGenericDictionaryEnumerator = GetBuildCreateGenericDictionaryEnumeratorGenericMethod(keyValueTypes);
                        _createGenericDictionaryEnumeratorDelegate = (CreateGenericDictionaryEnumeratorDelegate)buildCreateGenericDictionaryEnumerator.Invoke(null, Array.Empty<object>())!;
                    }

                    enumerator = _createGenericDictionaryEnumeratorDelegate(enumerator);
                }
                else if (Kind == CollectionKind.Dictionary)
                {
                    enumerator = new DictionaryEnumerator(((IDictionary)obj).GetEnumerator());
                }

                return enumerator;

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
                Justification = "The call to MakeGenericMethod is safe due to the fact that CollectionDataContractCriticalHelper.BuildCreateGenericDictionaryEnumerator<K,V> is not annotated.")]
                static MethodInfo GetBuildCreateGenericDictionaryEnumeratorGenericMethod(Type[] keyValueTypes) => GetBuildCreateGenericDictionaryEnumeratorMethodInfo.MakeGenericMethod(keyValueTypes[0], keyValueTypes[1]);
            }

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal Type GetCollectionElementType()
            {
                Debug.Assert(Kind != CollectionKind.Array, "GetCollectionElementType should not be called on Arrays");
                Debug.Assert(GetEnumeratorMethod != null, "GetEnumeratorMethod should be non-null for non-Arrays");

                Type? enumeratorType = null;
                if (Kind == CollectionKind.GenericDictionary)
                {
                    Type[] keyValueTypes = ItemType.GetGenericArguments();
                    enumeratorType = Globals.TypeOfGenericDictionaryEnumerator.MakeGenericType(keyValueTypes);
                }
                else if (Kind == CollectionKind.Dictionary)
                {
                    enumeratorType = Globals.TypeOfDictionaryEnumerator;
                }
                else
                {
                    enumeratorType = GetEnumeratorMethod.ReturnType;
                }

                MethodInfo? getCurrentMethod = enumeratorType.GetMethod(Globals.GetCurrentMethodName, BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
                if (getCurrentMethod == null)
                {
                    if (enumeratorType.IsInterface)
                    {
                        getCurrentMethod = XmlFormatGeneratorStatics.GetCurrentMethod;
                    }
                    else
                    {
                        Type ienumeratorInterface = Globals.TypeOfIEnumerator;
                        if (Kind == CollectionKind.GenericDictionary || Kind == CollectionKind.GenericCollection || Kind == CollectionKind.GenericEnumerable)
                        {
                            Type[] interfaceTypes = enumeratorType.GetInterfaces();
                            foreach (Type interfaceType in interfaceTypes)
                            {
                                if (interfaceType.IsGenericType
                                    && interfaceType.GetGenericTypeDefinition() == Globals.TypeOfIEnumeratorGeneric
                                    && interfaceType.GetGenericArguments()[0] == ItemType)
                                {
                                    ienumeratorInterface = interfaceType;
                                    break;
                                }
                            }
                        }

                        getCurrentMethod = GetTargetMethodWithName(Globals.GetCurrentMethodName, enumeratorType, ienumeratorInterface)!;
                    }
                }

                Type elementType = getCurrentMethod.ReturnType;
                return elementType;
            }

            private static MethodInfo? s_buildCreateGenericDictionaryEnumerator;

            private static MethodInfo GetBuildCreateGenericDictionaryEnumeratorMethodInfo
            {
                get
                {
                    if (s_buildCreateGenericDictionaryEnumerator == null)
                    {
                        s_buildCreateGenericDictionaryEnumerator = typeof(CollectionDataContractCriticalHelper).GetMethod(nameof(BuildCreateGenericDictionaryEnumerator), Globals.ScanAllMembers)!;
                    }

                    return s_buildCreateGenericDictionaryEnumerator;
                }
            }

            private static CreateGenericDictionaryEnumeratorDelegate BuildCreateGenericDictionaryEnumerator<K, V>()
            {
                return (enumerator) =>
                {
                    return new GenericDictionaryEnumerator<K, V>((IEnumerator<KeyValuePair<K, V>>)enumerator);
                };
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract? GetSharedTypeContract(Type type)
        {
            if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, false))
            {
                return this;
            }
            if (type.IsDefined(Globals.TypeOfDataContractAttribute, false))
            {
                return new ClassDataContract(type);
            }
            return null;
        }

        internal static bool IsCollectionInterface(Type type)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            return ((IList<Type>)KnownInterfaces).Contains(type);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool IsCollection(Type type)
        {
            Type? itemType;
            return IsCollection(type, out itemType);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool IsCollection(Type type, [NotNullWhen(true)] out Type? itemType)
        {
            return IsCollectionHelper(type, out itemType, true /*constructorRequired*/);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool IsCollection(Type type, bool constructorRequired)
        {
            Type? itemType;
            return IsCollectionHelper(type, out itemType, constructorRequired);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool IsCollectionHelper(Type type, [NotNullWhen(true)] out Type? itemType, bool constructorRequired)
        {
            if (type.IsArray && DataContract.GetBuiltInDataContract(type) == null)
            {
                itemType = type.GetElementType()!;
                return true;
            }
            DataContract? dataContract;
            return IsCollectionOrTryCreate(type, false /*tryCreate*/, out dataContract, out itemType, constructorRequired);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool TryCreate(Type type, [NotNullWhen(true)] out DataContract? dataContract)
        {
            Type itemType;
            return IsCollectionOrTryCreate(type, true /*tryCreate*/, out dataContract!, out itemType, true /*constructorRequired*/);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool CreateGetOnlyCollectionDataContract(Type type, [NotNullWhen(true)] out DataContract? dataContract)
        {
            Type itemType;
            if (type.IsArray)
            {
                dataContract = new CollectionDataContract(type);
                return true;
            }
            else
            {
                return IsCollectionOrTryCreate(type, true /*tryCreate*/, out dataContract!, out itemType, false /*constructorRequired*/);
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool TryCreateGetOnlyCollectionDataContract(Type type, [NotNullWhen(true)] out DataContract? dataContract)
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
                    return IsCollectionOrTryCreate(type, true /*tryCreate*/, out dataContract!, out itemType, false /*constructorRequired*/);
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

        // Once https://github.com/mono/linker/issues/1731 is fixed we can remove the suppression from here as it won't be needed any longer.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:GetMethod",
            Justification = "The DynamicallyAccessedMembers declarations will ensure the interface methods will be preserved.")]
        internal static MethodInfo? GetTargetMethodWithName(string name,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            Type interfaceType)
        {
            Type? t = type.GetInterfaces().Where(it => it.Equals(interfaceType)).FirstOrDefault();
            return t?.GetMethod(name);
        }

        private static bool IsArraySegment(Type t)
        {
            return t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(ArraySegment<>));
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool IsCollectionOrTryCreate(Type type, bool tryCreate, out DataContract? dataContract, out Type itemType, bool constructorRequired)
        {
            dataContract = null;
            itemType = Globals.TypeOfObject;

            if (DataContract.GetBuiltInDataContract(type) != null)
            {
                return HandleIfInvalidCollection(type, tryCreate, false/*hasCollectionDataContract*/, false/*isBaseTypeCollection*/,
                    SR.CollectionTypeCannotBeBuiltIn, null, ref dataContract);
            }

            MethodInfo? addMethod, getEnumeratorMethod;
            bool hasCollectionDataContract = IsCollectionDataContract(type);
            bool isReadOnlyContract = false;
            string? deserializationExceptionMessage = null;
            Type? baseType = type.BaseType;
            bool isBaseTypeCollection = (baseType != null && baseType != Globals.TypeOfObject
                && baseType != Globals.TypeOfValueType && baseType != Globals.TypeOfUri) ? IsCollection(baseType) : false;

            // Avoid creating an invalid collection contract for Serializable types since we can create a ClassDataContract instead
            bool createContractWithException = isBaseTypeCollection && !type.IsSerializable;

            if (type.IsDefined(Globals.TypeOfDataContractAttribute, false))
            {
                return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, createContractWithException,
                    SR.CollectionTypeCannotHaveDataContract, null, ref dataContract);
            }

            if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type) || IsArraySegment(type))
            {
                return false;
            }

            if (!Globals.TypeOfIEnumerable.IsAssignableFrom(type))
            {
                return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, createContractWithException,
                    SR.CollectionTypeIsNotIEnumerable, null, ref dataContract);
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
                                getEnumeratorMethod = Globals.TypeOfIEnumerableGeneric.MakeGenericType(Globals.TypeOfKeyValuePair.MakeGenericType(genericArgs)).GetMethod(Globals.GetEnumeratorMethodName)!;
                            }
                            else
                            {
                                itemType = genericArgs[0];
                                if (interfaceTypeToCheck == Globals.TypeOfICollectionGeneric || interfaceTypeToCheck == Globals.TypeOfIListGeneric)
                                {
                                    addMethod = Globals.TypeOfICollectionGeneric.MakeGenericType(itemType).GetMethod(Globals.AddMethodName);
                                }
                                getEnumeratorMethod = Globals.TypeOfIEnumerableGeneric.MakeGenericType(itemType).GetMethod(Globals.GetEnumeratorMethodName)!;
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

                            getEnumeratorMethod = typeof(IEnumerable).GetMethod(Globals.GetEnumeratorMethodName)!;
                        }
                        if (tryCreate)
                            dataContract = new CollectionDataContract(type, (CollectionKind)(i + 1), itemType, getEnumeratorMethod, addMethod, null/*defaultCtor*/);
                        return true;
                    }
                }
            }
            ConstructorInfo? defaultCtor = null;
            if (!type.IsValueType)
            {
                defaultCtor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
                if (defaultCtor == null && constructorRequired)
                {
                    // All collection types could be considered read-only collections except collection types that are marked [Serializable].
                    // Collection types marked [Serializable] cannot be read-only collections for backward compatibility reasons.
                    // DataContract types and POCO types cannot be collection types, so they don't need to be factored in
                    if (type.IsSerializable)
                    {
                        return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, createContractWithException,
                            SR.CollectionTypeDoesNotHaveDefaultCtor, null, ref dataContract);
                    }
                    else
                    {
                        isReadOnlyContract = true;
                        GetReadOnlyCollectionExceptionMessages(type, SR.CollectionTypeDoesNotHaveDefaultCtor, null, out deserializationExceptionMessage);
                    }
                }
            }

            Type? knownInterfaceType = null;
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
                return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, createContractWithException,
                    SR.CollectionTypeIsNotIEnumerable, null, ref dataContract);
            }

            Debug.Assert(knownInterfaceType != null);
            if (kind == CollectionKind.Enumerable || kind == CollectionKind.Collection || kind == CollectionKind.GenericEnumerable)
            {
                if (multipleDefinitions)
                    knownInterfaceType = Globals.TypeOfIEnumerable;
                itemType = knownInterfaceType.IsGenericType ? knownInterfaceType.GetGenericArguments()[0] : Globals.TypeOfObject;
                GetCollectionMethods(type, knownInterfaceType, new Type[] { itemType },
                                     false /*addMethodOnInterface*/,
                                     out getEnumeratorMethod, out addMethod);

                Debug.Assert(getEnumeratorMethod != null);

                if (addMethod == null)
                {
                    // All collection types could be considered read-only collections except collection types that are marked [Serializable].
                    // Collection types marked [Serializable] cannot be read-only collections for backward compatibility reasons.
                    // DataContract types and POCO types cannot be collection types, so they don't need to be factored in.
                    if (type.IsSerializable)
                    {
                        return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, createContractWithException,
                            SR.CollectionTypeDoesNotHaveAddMethod, DataContract.GetClrTypeFullName(itemType), ref dataContract);
                    }
                    else
                    {
                        isReadOnlyContract = true;
                        GetReadOnlyCollectionExceptionMessages(type, SR.CollectionTypeDoesNotHaveAddMethod, DataContract.GetClrTypeFullName(itemType), out deserializationExceptionMessage);
                    }
                }

                if (tryCreate)
                {
                    dataContract = isReadOnlyContract ?
                        new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, deserializationExceptionMessage) :
                        new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, addMethod, defaultCtor, !constructorRequired);
                }
            }
            else
            {
                if (multipleDefinitions)
                {
                    return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, createContractWithException,
                        SR.CollectionTypeHasMultipleDefinitionsOfInterface, KnownInterfaces[(int)kind - 1].Name, ref dataContract);
                }
                Type[]? addMethodTypeArray = null;
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
                    Debug.Assert(addMethodTypeArray != null);
                    GetCollectionMethods(type, knownInterfaceType, addMethodTypeArray,
                                     true /*addMethodOnInterface*/,
                                     out getEnumeratorMethod, out addMethod);

                    Debug.Assert(getEnumeratorMethod != null);

                    dataContract = isReadOnlyContract ?
                        new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, deserializationExceptionMessage) :
                        new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, addMethod, defaultCtor, !constructorRequired);
                }
            }

            return true;
        }

        internal static bool IsCollectionDataContract(Type type)
        {
            return type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, false);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool HandleIfInvalidCollection(Type type, bool tryCreate, bool hasCollectionDataContract, bool createContractWithException, string message, string? param, ref DataContract? dataContract)
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

        private static void GetReadOnlyCollectionExceptionMessages(Type type, string message, string? param, out string deserializationExceptionMessage)
        {
            deserializationExceptionMessage = GetInvalidCollectionMessage(message, SR.Format(SR.ReadOnlyCollectionDeserialization, GetClrTypeFullName(type)), param);
        }

        private static string GetInvalidCollectionMessage(string message, string nestedMessage, string? param)
        {
            return (param == null) ? SR.Format(message, nestedMessage) : SR.Format(message, nestedMessage, param);
        }

        // Once https://github.com/mono/linker/issues/1731 is fixed we can remove the suppression from here as it won't be needed any longer.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:GetMethod",
            Justification = "The DynamicallyAccessedMembers declarations will ensure the interface methods will be preserved.")]
        private static void FindCollectionMethodsOnInterface(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            Type interfaceType,
            ref MethodInfo? addMethod, ref MethodInfo? getEnumeratorMethod)
        {
            Type? t = type.GetInterfaces().Where(it => it.Equals(interfaceType)).FirstOrDefault();
            if (t != null)
            {
                addMethod = t.GetMethod(Globals.AddMethodName) ?? addMethod;
                getEnumeratorMethod = t.GetMethod(Globals.GetEnumeratorMethodName) ?? getEnumeratorMethod;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static void GetCollectionMethods(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            Type interfaceType,
            Type[] addMethodTypeArray, bool addMethodOnInterface, out MethodInfo? getEnumeratorMethod, out MethodInfo? addMethod)
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
                        // The for loop below depeneds on the order for the items in parentInterfaceTypes, which
                        // doesnt' seem right. But it's the behavior of DCS on the full framework.
                        // Sorting the array to make sure the behavior is consistent with Desktop's.
                        Array.Sort(parentInterfaceTypes, (x, y) => string.Compare(x.FullName, y.FullName));
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
            }

            if (getEnumeratorMethod == null)
            {
                getEnumeratorMethod = type.GetMethod(Globals.GetEnumeratorMethodName, BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
                if (getEnumeratorMethod == null || !Globals.TypeOfIEnumerator.IsAssignableFrom(getEnumeratorMethod.ReturnType))
                {
                    Type? ienumerableInterface = interfaceType.GetInterfaces().Where(t => t.FullName!.StartsWith("System.Collections.Generic.IEnumerable")).FirstOrDefault();
                    if (ienumerableInterface == null)
                        ienumerableInterface = Globals.TypeOfIEnumerable;
                    getEnumeratorMethod = GetIEnumerableGetEnumeratorMethod(type, ienumerableInterface);
                }
            }
        }

        private static MethodInfo? GetIEnumerableGetEnumeratorMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            Type type,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            Type ienumerableInterface)
                => GetTargetMethodWithName(Globals.GetEnumeratorMethodName, type, ienumerableInterface);

        private static bool IsKnownInterface(Type type)
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
        internal bool RequiresMemberAccessForRead(SecurityException? securityException)
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
                                   this.AddMethod!.Name),
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
        internal bool RequiresMemberAccessForWrite(SecurityException? securityException)
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext? context)
        {
            Debug.Assert(context != null);

            // IsGetOnlyCollection value has already been used to create current collectiondatacontract, value can now be reset.
            context.IsGetOnlyCollection = false;
            XmlFormatWriterDelegate(xmlWriter, obj, context, this);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context)
        {
            Debug.Assert(context != null);

            xmlReader.Read();
            object? o = null;
            if (context.IsGetOnlyCollection)
            {
                // IsGetOnlyCollection value has already been used to create current collectiondatacontract, value can now be reset.
                context.IsGetOnlyCollection = false;
                XmlFormatGetOnlyCollectionReaderDelegate(xmlReader, context, CollectionItemName, Namespace, this);
            }
            else
            {
                o = XmlFormatReaderDelegate(xmlReader, context, CollectionItemName, Namespace, this);
            }
            xmlReader.ReadEndElement();
            return o;
        }

        internal sealed class DictionaryEnumerator : IEnumerator<KeyValue<object, object?>>
        {
            private readonly IDictionaryEnumerator _enumerator;

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

            public KeyValue<object, object?> Current
            {
                get { return new KeyValue<object, object?>(_enumerator.Key, _enumerator.Value); }
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

        internal sealed class GenericDictionaryEnumerator<K, V> : IEnumerator<KeyValue<K, V>>
        {
            private readonly IEnumerator<KeyValuePair<K, V>> _enumerator;

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
