// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization
{
    internal interface IKeyValue
    {
        object? Key { get; set; }
        object? Value { get; set; }
    }

    [DataContract(Namespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
    internal struct KeyValue<K, V> : IKeyValue
    {
        internal KeyValue(K key, V value)
        {
            Key = key;
            Value = value;
        }

        [DataMember(IsRequired = true)]
        public K Key { get; set; }

        [DataMember(IsRequired = true)]
        public V Value { get; set; }

        object? IKeyValue.Key
        {
            get => this.Key;
            set => this.Key = (K)value!;
        }

        object? IKeyValue.Value
        {
            get => this.Value;
            set => this.Value = (V)value!;
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
}

namespace System.Runtime.Serialization.DataContracts
{
    internal sealed class CollectionDataContract : DataContract
    {
        internal const string ContractTypeString = nameof(CollectionDataContract);
        public override string? ContractType => ContractTypeString;

        private XmlDictionaryString _collectionItemName;

        private XmlDictionaryString? _childElementNamespace;

        private DataContract? _itemContract;

        private CollectionDataContractCriticalHelper _helper;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal CollectionDataContract(Type type) : base(new CollectionDataContractCriticalHelper(type))
        {
            InitCollectionDataContract(this);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal CollectionDataContract(Type type, DataContract itemContract) : base(new CollectionDataContractCriticalHelper(type, itemContract))
        {
            InitCollectionDataContract(this);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal CollectionDataContract(Type type, CollectionKind kind) : base(new CollectionDataContractCriticalHelper(type, kind))
        {
            InitCollectionDataContract(this);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, string? serializationExceptionMessage, string? deserializationExceptionMessage)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, serializationExceptionMessage, deserializationExceptionMessage))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo? addMethod, ConstructorInfo? constructor)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo? addMethod, ConstructorInfo? constructor, bool isConstructorCheckRequired)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor, isConstructorCheckRequired))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private CollectionDataContract(Type type, string invalidCollectionInSharedContractMessage) : base(new CollectionDataContractCriticalHelper(type, invalidCollectionInSharedContractMessage))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }

        [MemberNotNull(nameof(_helper))]
        [MemberNotNull(nameof(_collectionItemName))]
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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

        private static Type[] KnownInterfaces => CollectionDataContractCriticalHelper.KnownInterfaces;

        internal CollectionKind Kind => _helper.Kind;

        internal Type ItemType => _helper.ItemType;

        internal DataContract ItemContract
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => _itemContract ?? _helper.ItemContract;

            set
            {
                _itemContract = value;
                _helper.ItemContract = value;
            }
        }

        internal DataContract? SharedTypeContract => _helper.SharedTypeContract;

        internal string ItemName
        {
            get => _helper.ItemName;
            set => _helper.ItemName = value;
        }

        internal XmlDictionaryString CollectionItemName => _collectionItemName;

        internal string? KeyName
        {
            get => _helper.KeyName;
            set => _helper.KeyName = value;
        }

        internal string? ValueName
        {
            get => _helper.ValueName;
            set => _helper.ValueName = value;
        }

        public override bool IsDictionaryLike([NotNullWhen(true)] out string? keyName, [NotNullWhen(true)] out string? valueName, [NotNullWhen(true)] out string? itemName)
        {
            keyName = KeyName;
            valueName = ValueName;
            itemName = ItemName;
            return IsDictionary;
        }

        public override DataContract BaseContract
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => ItemContract;
        }

        internal bool IsDictionary => KeyName != null;

        internal XmlDictionaryString? ChildElementNamespace
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
            get => _helper.IsItemTypeNullable;
            set => _helper.IsItemTypeNullable = value;
        }

        internal bool IsConstructorCheckRequired
        {
            get => _helper.IsConstructorCheckRequired;
            set => _helper.IsConstructorCheckRequired = value;
        }

        internal MethodInfo? GetEnumeratorMethod => _helper.GetEnumeratorMethod;

        internal MethodInfo? AddMethod => _helper.AddMethod;

        internal ConstructorInfo? Constructor => _helper.Constructor;

        public override DataContractDictionary? KnownDataContracts
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => _helper.KnownDataContracts;
            internal set => _helper.KnownDataContracts = value;
        }

        internal string? InvalidCollectionInSharedContractMessage => _helper.InvalidCollectionInSharedContractMessage;

        internal string? SerializationExceptionMessage => _helper.SerializationExceptionMessage;

        internal string? DeserializationExceptionMessage => _helper.DeserializationExceptionMessage;

        internal bool IsReadOnlyContract => DeserializationExceptionMessage != null;

        private bool ItemNameSetExplicit => _helper.ItemNameSetExplicit;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatCollectionWriterDelegate CreateXmlFormatWriterDelegate()
        {
            return new XmlFormatWriterGenerator().GenerateCollectionWriter(this);
        }

        internal XmlFormatCollectionWriterDelegate XmlFormatWriterDelegate
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatCollectionReaderDelegate CreateXmlFormatReaderDelegate()
        {
            return new XmlFormatReaderGenerator().GenerateCollectionReader(this);
        }

        internal XmlFormatCollectionReaderDelegate XmlFormatReaderDelegate
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private XmlFormatGetOnlyCollectionReaderDelegate CreateXmlFormatGetOnlyCollectionReaderDelegate()
        {
            return new XmlFormatReaderGenerator().GenerateGetOnlyCollectionReader(this);
        }


        internal XmlFormatGetOnlyCollectionReaderDelegate XmlFormatGetOnlyCollectionReaderDelegate
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
                                throw new InvalidDataContractException(SR.Format(SR.GetOnlyCollectionMustHaveAddMethod, GetClrTypeFullName(UnderlyingType)));
                            }

                            if (IsReadOnlyContract)
                            {
                                ThrowInvalidDataContractException(_helper.DeserializationExceptionMessage, type: null);
                            }

                            if (Kind != CollectionKind.Array && AddMethod == null)
                            {
                                throw new InvalidDataContractException(SR.Format(SR.GetOnlyCollectionMustHaveAddMethod, GetClrTypeFullName(UnderlyingType)));
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        internal void IncrementCollectionCount(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
        {
            _helper.IncrementCollectionCount(xmlWriter, obj, context);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        internal IEnumerator GetEnumeratorForCollection(object obj)
        {
            return _helper.GetEnumeratorForCollection(obj);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
            private readonly string? _serializationExceptionMessage;
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

            internal static Type[] KnownInterfaces =>
                // Listed in priority order
                s_knownInterfaces ??= new Type[]
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

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
                                throw new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractItemName, DataContract.GetClrTypeFullName(UnderlyingType)));
                            itemName = DataContract.EncodeLocalName(collectionContractAttribute.ItemName);
                            _itemNameSetExplicit = true;
                        }
                        if (collectionContractAttribute.IsKeyNameSetExplicitly)
                        {
                            if (collectionContractAttribute.KeyName == null || collectionContractAttribute.KeyName.Length == 0)
                                throw new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractKeyName, DataContract.GetClrTypeFullName(UnderlyingType)));
                            if (!isDictionary)
                                throw new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractKeyNoDictionary, DataContract.GetClrTypeFullName(UnderlyingType), collectionContractAttribute.KeyName));
                            keyName = DataContract.EncodeLocalName(collectionContractAttribute.KeyName);
                        }
                        if (collectionContractAttribute.IsValueNameSetExplicitly)
                        {
                            if (collectionContractAttribute.ValueName == null || collectionContractAttribute.ValueName.Length == 0)
                                throw new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractValueName, DataContract.GetClrTypeFullName(UnderlyingType)));
                            if (!isDictionary)
                                throw new InvalidDataContractException(SR.Format(SR.InvalidCollectionContractValueNoDictionary, DataContract.GetClrTypeFullName(UnderlyingType), collectionContractAttribute.ValueName));
                            valueName = DataContract.EncodeLocalName(collectionContractAttribute.ValueName);
                        }
                    }

                    XmlDictionary dictionary = isDictionary ? new XmlDictionary(5) : new XmlDictionary(3);
                    Name = dictionary.Add(XmlName.Name);
                    Namespace = dictionary.Add(XmlName.Namespace);
                    _itemName = itemName ?? DataContract.GetXmlName(DataContract.UnwrapNullableType(itemType)).Name;
                    _collectionItemName = dictionary.Add(_itemName);
                    if (isDictionary)
                    {
                        _keyName = keyName ?? Globals.KeyLocalName;
                        _valueName = valueName ?? Globals.ValueLocalName;
                    }
                }
                if (collectionContractAttribute != null)
                {
                    IsReference = collectionContractAttribute.IsReference;
                }
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type) : base(type)
            {
                if (type == Globals.TypeOfArray)
                    type = Globals.TypeOfObjectArray;
                if (type.GetArrayRank() > 1)
                    throw new NotSupportedException(SR.SupportForMultidimensionalArraysNotPresent);
                XmlName = DataContract.GetXmlName(type);
                Init(CollectionKind.Array, type.GetElementType(), null);
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type,
                CollectionKind kind) : base(type)
            {
                XmlName = DataContract.GetXmlName(type);
                Init(kind, type.GetElementType(), null);
            }

            // array
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type,
                DataContract itemContract) : base(type)
            {
                if (type.GetArrayRank() > 1)
                    throw new NotSupportedException(SR.SupportForMultidimensionalArraysNotPresent);
                XmlName = CreateQualifiedName(Globals.ArrayPrefix + itemContract.XmlName.Name, itemContract.XmlName.Namespace);
                _itemContract = itemContract;
                Init(CollectionKind.Array, type.GetElementType(), null);
            }

            // read-only collection
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type,
                CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, string? serializationExceptionMessage, string? deserializationExceptionMessage)
                : base(type)
            {
                if (getEnumeratorMethod == null)
                    throw new InvalidDataContractException(SR.Format(SR.CollectionMustHaveGetEnumeratorMethod, GetClrTypeFullName(type)));
                if (itemType == null)
                    throw new InvalidDataContractException(SR.Format(SR.CollectionMustHaveItemType, GetClrTypeFullName(type)));

                CollectionDataContractAttribute? collectionContractAttribute;
                XmlName = DataContract.GetCollectionXmlName(type, itemType, out collectionContractAttribute);

                Init(kind, itemType, collectionContractAttribute);
                _getEnumeratorMethod = getEnumeratorMethod;
                _serializationExceptionMessage = serializationExceptionMessage;
                _deserializationExceptionMessage = deserializationExceptionMessage;
            }

            // collection
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type,
                CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo? addMethod, ConstructorInfo? constructor)
                : this(type, kind, itemType, getEnumeratorMethod, (string?)null, (string?)null)
            {
                if (addMethod == null && !type.IsInterface)
                    throw new InvalidDataContractException(SR.Format(SR.CollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(type)));

                _addMethod = addMethod;
                _constructor = constructor;
            }

            // collection
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo? addMethod, ConstructorInfo? constructor, bool isConstructorCheckRequired)
                : this(type, kind, itemType, getEnumeratorMethod, addMethod, constructor)
            {
                _isConstructorCheckRequired = isConstructorCheckRequired;
            }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal CollectionDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type,
                string invalidCollectionInSharedContractMessage) : base(type)
            {
                Init(CollectionKind.Collection, null /*itemType*/, null);
                _invalidCollectionInSharedContractMessage = invalidCollectionInSharedContractMessage;
            }

            internal CollectionKind Kind => _kind;

            internal Type ItemType => _itemType;

            internal DataContract ItemContract
            {
                [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
                get
                {
                    if (_itemContract == null)
                    {
                        if (IsDictionary)
                        {
                            if (KeyName == ValueName)
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
                            _itemContract = DataContract.GetDataContract(ItemType);
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
                get => _sharedTypeContract;
                set => _sharedTypeContract = value;
            }

            internal string ItemName
            {
                get => _itemName;
                set => _itemName = value;
            }

            internal bool IsConstructorCheckRequired
            {
                get => _isConstructorCheckRequired;
                set => _isConstructorCheckRequired = value;
            }

            internal XmlDictionaryString CollectionItemName => _collectionItemName;

            internal string? KeyName
            {
                get => _keyName;
                set => _keyName = value;
            }

            internal string? ValueName
            {
                get => _valueName;
                set => _valueName = value;
            }

            internal bool IsDictionary => KeyName != null;

            internal string? SerializationExceptionMessage => _serializationExceptionMessage;

            internal string? DeserializationExceptionMessage => _deserializationExceptionMessage;

            internal XmlDictionaryString? ChildElementNamespace
            {
                get => _childElementNamespace;
                set => _childElementNamespace = value;
            }

            internal bool IsItemTypeNullable
            {
                get => _isItemTypeNullable;
                set => _isItemTypeNullable = value;
            }

            internal MethodInfo? GetEnumeratorMethod => _getEnumeratorMethod;

            internal MethodInfo? AddMethod => _addMethod;

            internal ConstructorInfo? Constructor => _constructor;

            internal override DataContractDictionary? KnownDataContracts
            {
                [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
                get
                {
                    if (!_isKnownTypeAttributeChecked && UnderlyingType != null)
                    {
                        lock (this)
                        {
                            if (!_isKnownTypeAttributeChecked)
                            {
                                _knownDataContracts = DataContract.ImportKnownTypeAttributes(UnderlyingType);
                                Interlocked.MemoryBarrier();
                                _isKnownTypeAttributeChecked = true;
                            }
                            _knownDataContracts ??= new DataContractDictionary();
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
                get => _xmlFormatWriterDelegate;
                set => _xmlFormatWriterDelegate = value;
            }

            internal XmlFormatCollectionReaderDelegate? XmlFormatReaderDelegate
            {
                get => _xmlFormatReaderDelegate;
                set => _xmlFormatReaderDelegate = value;
            }

            internal XmlFormatGetOnlyCollectionReaderDelegate? XmlFormatGetOnlyCollectionReaderDelegate
            {
                get => _xmlFormatGetOnlyCollectionReaderDelegate;
                set => _xmlFormatGetOnlyCollectionReaderDelegate = value;
            }

            private delegate void IncrementCollectionCountDelegate(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context);

            private IncrementCollectionCountDelegate? _incrementCollectionCountDelegate;

            private static void DummyIncrementCollectionCount(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context) { }

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
                                MethodInfo? buildIncrementCollectionCountDelegate = GetBuildIncrementCollectionCountGenericDelegate(ItemType);
                                _incrementCollectionCountDelegate = (IncrementCollectionCountDelegate)buildIncrementCollectionCountDelegate.Invoke(null, Array.Empty<object>())!;
                            }
                            break;
                        case CollectionKind.GenericDictionary:
                            {
                                MethodInfo? buildIncrementCollectionCountDelegate = GetBuildIncrementCollectionCountGenericDelegate(typeof(KeyValuePair<,>).MakeGenericType(ItemType.GetGenericArguments()));
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

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
                Justification = "The call to MakeGenericMethod is safe due to the fact that CollectionDataContractCriticalHelper.BuildIncrementCollectionCountDelegate<T> is not annotated.")]
            private static MethodInfo GetBuildIncrementCollectionCountGenericDelegate(Type type) => BuildIncrementCollectionCountDelegateMethod.MakeGenericMethod(type);

            private static MethodInfo? s_buildIncrementCollectionCountDelegateMethod;

            private static MethodInfo BuildIncrementCollectionCountDelegateMethod =>
                s_buildIncrementCollectionCountDelegateMethod ??= typeof(CollectionDataContractCriticalHelper).GetMethod(nameof(BuildIncrementCollectionCountDelegate), Globals.ScanAllMembers)!;

            private static IncrementCollectionCountDelegate BuildIncrementCollectionCountDelegate<T>()
            {
                return (xmlwriter, obj, context) =>
                {
                    context.IncrementCollectionCountGeneric<T>(xmlwriter, (ICollection<T>)obj);
                };
            }

            private delegate IEnumerator CreateGenericDictionaryEnumeratorDelegate(IEnumerator enumerator);

            private CreateGenericDictionaryEnumeratorDelegate? _createGenericDictionaryEnumeratorDelegate;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            internal IEnumerator GetEnumeratorForCollection(object obj)
            {
                IEnumerator enumerator = ((IEnumerable)obj).GetEnumerator();
                if (Kind == CollectionKind.GenericDictionary)
                {
                    if (_createGenericDictionaryEnumeratorDelegate == null)
                    {
                        Type[]? keyValueTypes = ItemType.GetGenericArguments();
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

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal Type GetCollectionElementType()
            {
                Debug.Assert(Kind != CollectionKind.Array, "GetCollectionElementType should not be called on Arrays");
                Debug.Assert(GetEnumeratorMethod != null, "GetEnumeratorMethod should be non-null for non-Arrays");

                Type? enumeratorType;
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

            private static MethodInfo GetBuildCreateGenericDictionaryEnumeratorMethodInfo =>
                s_buildCreateGenericDictionaryEnumerator ??= typeof(CollectionDataContractCriticalHelper).GetMethod(nameof(BuildCreateGenericDictionaryEnumerator), Globals.ScanAllMembers)!;

            private static CreateGenericDictionaryEnumeratorDelegate BuildCreateGenericDictionaryEnumerator<K, V>()
            {
                return (enumerator) =>
                {
                    return new GenericDictionaryEnumerator<K, V>((IEnumerator<KeyValuePair<K, V>>)enumerator);
                };
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private DataContract? GetSharedTypeContract(Type type)
        {
            if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, false))
            {
                return this;
            }
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
            if (type.IsSerializable || type.IsDefined(Globals.TypeOfDataContractAttribute, false))
            {
                return new ClassDataContract(type);
            }
#pragma warning restore SYSLIB0050
            return null;
        }

        internal static bool IsCollectionInterface(Type type)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            return ((IList<Type>)KnownInterfaces).Contains(type);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool IsCollection(Type type)
        {
            return IsCollection(type, out _);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool IsCollection(Type type, [NotNullWhen(true)] out Type? itemType)
        {
            return IsCollectionHelper(type, out itemType, true /*constructorRequired*/);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool IsCollection(Type type, bool constructorRequired, bool skipIfReadOnlyContract)
        {
            return IsCollectionHelper(type, out _, constructorRequired, skipIfReadOnlyContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool IsCollectionHelper(Type type, [NotNullWhen(true)] out Type? itemType, bool constructorRequired, bool skipIfReadOnlyContract = false)
        {
            if (type.IsArray && DataContract.GetBuiltInDataContract(type) == null)
            {
                itemType = type.GetElementType()!;
                return true;
            }
            return IsCollectionOrTryCreate(type, tryCreate: false, out _, out itemType, constructorRequired, skipIfReadOnlyContract);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool TryCreate(Type type, [NotNullWhen(true)] out DataContract? dataContract)
        {
            return IsCollectionOrTryCreate(type, tryCreate: true, out dataContract!, out _, constructorRequired: true);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool TryCreateGetOnlyCollectionDataContract(Type type, [NotNullWhen(true)] out DataContract? dataContract)
        {
            if (type.IsArray)
            {
                dataContract = new CollectionDataContract(type);
                return true;
            }
            else
            {
                return IsCollectionOrTryCreate(type, tryCreate: true, out dataContract!, out _, constructorRequired: false);
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool IsCollectionOrTryCreate(Type type, bool tryCreate, out DataContract? dataContract, out Type itemType, bool constructorRequired, bool skipIfReadOnlyContract = false)
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
            string? serializationExceptionMessage = null;
            string? deserializationExceptionMessage = null;
            Type? baseType = type.BaseType;
            bool isBaseTypeCollection = (baseType != null && baseType != Globals.TypeOfObject
                && baseType != Globals.TypeOfValueType && baseType != Globals.TypeOfUri) ? IsCollection(baseType) : false;

            // Avoid creating an invalid collection contract for Serializable types since we can create a ClassDataContract instead
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
            bool createContractWithException = isBaseTypeCollection && !type.IsSerializable;
#pragma warning restore SYSLIB0050

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
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
                    if (type.IsSerializable)
                    {
                        return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, createContractWithException,
                            SR.CollectionTypeDoesNotHaveDefaultCtor, null, ref dataContract);
                    }
#pragma warning restore SYSLIB0050
                    else
                    {
                        isReadOnlyContract = true;
                        GetReadOnlyCollectionExceptionMessages(type, hasCollectionDataContract, SR.CollectionTypeDoesNotHaveDefaultCtor, null, out serializationExceptionMessage, out deserializationExceptionMessage);
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
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete
                    if (type.IsSerializable || skipIfReadOnlyContract)
                    {
                        return HandleIfInvalidCollection(type, tryCreate, hasCollectionDataContract, createContractWithException && !skipIfReadOnlyContract,
                            SR.CollectionTypeDoesNotHaveAddMethod, DataContract.GetClrTypeFullName(itemType), ref dataContract);
                    }
#pragma warning restore SYSLIB0050
                    else
                    {
                        isReadOnlyContract = true;
                        GetReadOnlyCollectionExceptionMessages(type, hasCollectionDataContract, SR.CollectionTypeDoesNotHaveAddMethod, DataContract.GetClrTypeFullName(itemType), out serializationExceptionMessage, out deserializationExceptionMessage);
                    }
                }

                if (tryCreate)
                {
                    dataContract = isReadOnlyContract ?
                        new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, serializationExceptionMessage, deserializationExceptionMessage) :
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
                        new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, serializationExceptionMessage, deserializationExceptionMessage) :
                        new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, addMethod, defaultCtor, !constructorRequired);
                }
            }

            return !(isReadOnlyContract && skipIfReadOnlyContract);
        }

        internal static bool IsCollectionDataContract(Type type)
        {
            return type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, false);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static bool HandleIfInvalidCollection(Type type, bool tryCreate, bool hasCollectionDataContract, bool createContractWithException, string message, string? param, ref DataContract? dataContract)
        {
            if (hasCollectionDataContract)
            {
                if (tryCreate)
                    throw new InvalidDataContractException(GetInvalidCollectionMessage(message, SR.Format(SR.InvalidCollectionDataContract, DataContract.GetClrTypeFullName(type)), param));
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

        private static void GetReadOnlyCollectionExceptionMessages(Type type, bool hasCollectionDataContract, string message, string? param, out string serializationExceptionMessage, out string deserializationExceptionMessage)
        {
            serializationExceptionMessage = GetInvalidCollectionMessage(message, SR.Format(hasCollectionDataContract ? SR.InvalidCollectionDataContract : SR.InvalidCollectionType, GetClrTypeFullName(type)), param);
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
                    Type? ienumerableInterface =
                        interfaceType.GetInterfaces().Where(t => t.FullName!.StartsWith("System.Collections.Generic.IEnumerable")).FirstOrDefault() ??
                        Globals.TypeOfIEnumerable;
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract>? boundContracts = null)
        {
            DataContract boundContract;
            if (boundContracts != null && boundContracts.TryGetValue(this, out boundContract!))
                return boundContract;

            // This type-binding ('boundType') stuff is new. We did not do this in NetFx. We used to use default contract constructors and let the
            // underlying type get filled in later. But default constructors for DataContracts runs afoul of requiring an underlying type. Our web of nullable
            // notations make it hard to get around. But it also allows us to feel good about using .UnderlyingType from matching parameter contracts.
            Type type = UnderlyingType;
            Type[] paramTypes = type.GetGenericArguments();
            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (paramTypes[i].IsGenericParameter)
                    paramTypes[i] = paramContracts[paramTypes[i].GenericParameterPosition].UnderlyingType;
            }
            Type boundType = type.MakeGenericType(paramTypes);

            CollectionDataContract boundCollectionContract = new CollectionDataContract(boundType);
            boundContracts ??= new Dictionary<DataContract, DataContract>();
            boundContracts.Add(this, boundCollectionContract);
            boundCollectionContract.ItemContract = ItemContract.BindGenericParameters(paramContracts, boundContracts);
            boundCollectionContract.IsItemTypeNullable = !boundCollectionContract.ItemContract.IsValueType;
            boundCollectionContract.ItemName = ItemNameSetExplicit ? ItemName : boundCollectionContract.ItemContract.XmlName.Name;
            boundCollectionContract.KeyName = KeyName;
            boundCollectionContract.ValueName = ValueName;
            boundCollectionContract.XmlName = CreateQualifiedName(DataContract.ExpandGenericParameters(XmlConvert.DecodeName(XmlName.Name), new GenericNameProvider(DataContract.GetClrTypeFullName(UnderlyingType), paramContracts)),
                IsCollectionDataContract(UnderlyingType) ? XmlName.Namespace : DataContract.GetCollectionNamespace(boundCollectionContract.ItemContract.XmlName.Namespace));
            return boundCollectionContract;
        }

        internal override DataContract GetValidContract(bool verifyConstructor = false)
        {
            if (verifyConstructor && IsConstructorCheckRequired)
            {
                CheckConstructor();
                return this;
            }

            if (InvalidCollectionInSharedContractMessage != null)
                throw new InvalidDataContractException(InvalidCollectionInSharedContractMessage);
            return this;
        }

        private void CheckConstructor()
        {
            if (Constructor == null)
            {
                throw new InvalidDataContractException(SR.Format(SR.CollectionTypeDoesNotHaveDefaultCtor, DataContract.GetClrTypeFullName(UnderlyingType)));
            }
            else
            {
                IsConstructorCheckRequired = false;
            }
        }

        internal override bool IsValidContract()
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
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException);
                }
                return true;
            }
            if (ItemType != null && !IsTypeVisible(ItemType))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(ItemType)),
                            securityException);
                }
                return true;
            }
            if (ConstructorRequiresMemberAccess(Constructor))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractNoPublicConstructor,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException);
                }
                return true;
            }
            if (MethodRequiresMemberAccess(AddMethod))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                   SR.PartialTrustCollectionContractAddMethodNotPublic,
                                   DataContract.GetClrTypeFullName(UnderlyingType),
                                   AddMethod!.Name),
                               securityException);
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
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(UnderlyingType)),
                            securityException);
                }
                return true;
            }
            if (ItemType != null && !IsTypeVisible(ItemType))
            {
                if (securityException != null)
                {
                    throw new SecurityException(SR.Format(
                                SR.PartialTrustCollectionContractTypeNotPublic,
                                DataContract.GetClrTypeFullName(ItemType)),
                            securityException);
                }
                return true;
            }

            return false;
        }

        [UnconditionalSuppressMessage("AOT Analysis", "IL3050:RequiresDynamicCode",
            Justification = "All ctor's required to create an instance of this type are marked with RequiresDynamicCode.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "All ctor's required to create an instance of this type are marked with RequiresUnreferencedCode.")]
        internal override bool Equals(object? other, HashSet<DataContractPairKey>? checkedContracts)
        {
            if (IsEqualOrChecked(other, checkedContracts))
                return true;

            if (base.Equals(other, checkedContracts))
            {
                if (other is CollectionDataContract dataContract)
                {
                    bool thisItemTypeIsNullable = (ItemContract == null) ? false : !ItemContract.IsValueType;
                    bool otherItemTypeIsNullable = (dataContract.ItemContract == null) ? false : !dataContract.ItemContract.IsValueType;
                    return ItemName == dataContract.ItemName &&
                        (IsItemTypeNullable || thisItemTypeIsNullable) == (dataContract.IsItemTypeNullable || otherItemTypeIsNullable) &&
                        ItemContract != null && ItemContract.Equals(dataContract.ItemContract, checkedContracts);
                }
            }
            return false;
        }

        public override int GetHashCode() => base.GetHashCode();

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext? context)
        {
            Debug.Assert(context != null);

            // IsGetOnlyCollection value has already been used to create current collectiondatacontract, value can now be reset.
            context.IsGetOnlyCollection = false;
            XmlFormatWriterDelegate(xmlWriter, obj, context, this);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override object? ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext? context)
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

            public KeyValue<object, object?> Current => new KeyValue<object, object?>(_enumerator.Key, _enumerator.Value);

            object System.Collections.IEnumerator.Current => Current;

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

            object System.Collections.IEnumerator.Current => Current;

            public void Reset()
            {
                _enumerator.Reset();
            }
        }
    }
}
