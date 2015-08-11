// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.Serialization
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.IO;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using System.Xml;
    using System.Linq;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;
    using System.Security;

    //Special Adapter class to serialize KeyValuePair as Dictionary needs it when polymorphism is involved
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/System.Collections.Generic")]
    internal class KeyValuePairAdapter<K, T>
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
            get
            {
                return _kvpKey;
            }
            set
            {
                _kvpKey = value;
            }
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

    [DataContract(Namespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
#if USE_REFEMIT
    public struct KeyValue<K, V>
#else
    internal struct KeyValue<K, V>
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
        [SecurityCritical]
        /// <SecurityNote>
        /// Critical - XmlDictionaryString representing the XML element name for collection items.
        ///            statically cached and used from IL generated code.
        /// </SecurityNote>
        private XmlDictionaryString _collectionItemName;
        [SecurityCritical]
        /// <SecurityNote>
        /// Critical - XmlDictionaryString representing the XML namespace for collection items.
        ///            statically cached and used from IL generated code.
        /// </SecurityNote>
        private XmlDictionaryString _childElementNamespace;
        [SecurityCritical]
        /// <SecurityNote>
        /// Critical - internal DataContract representing the contract for collection items.
        ///            statically cached and used from IL generated code.
        /// </SecurityNote>
        private DataContract _itemContract;
        [SecurityCritical]

        /// <SecurityNote>
        /// Critical - holds instance of CriticalHelper which keeps state that is cached statically for serialization. 
        ///            Static fields are marked SecurityCritical or readonly to prevent
        ///            data from being modified or leaked to other components in appdomain.
        /// </SecurityNote>
        private CollectionDataContractCriticalHelper _helper;

        [SecuritySafeCritical]
        public CollectionDataContract(CollectionKind kind) : base(new CollectionDataContractCriticalHelper(kind))
        {
            InitCollectionDataContract(this);
        }

        /// <SecurityNote>
        /// Critical - initializes SecurityCritical field 'helper'
        /// Safe - doesn't leak anything
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal CollectionDataContract(Type type) : base(new CollectionDataContractCriticalHelper(type))
        {
            InitCollectionDataContract(this);
        }
        [SecuritySafeCritical]

        /// <SecurityNote>
        /// Critical - initializes SecurityCritical field 'helper'
        /// Safe - doesn't leak anything
        /// </SecurityNote>
        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }
        [SecuritySafeCritical]

        /// <SecurityNote>
        /// Critical - initializes SecurityCritical field 'helper'
        /// Safe - doesn't leak anything
        /// </SecurityNote>
        private CollectionDataContract(Type type, CollectionKind kind, Type itemType, MethodInfo getEnumeratorMethod, MethodInfo addMethod, ConstructorInfo constructor, bool isConstructorCheckRequired)
                    : base(new CollectionDataContractCriticalHelper(type, kind, itemType, getEnumeratorMethod, addMethod, constructor, isConstructorCheckRequired))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }
        [SecuritySafeCritical]

        /// <SecurityNote>
        /// Critical - initializes SecurityCritical field 'helper'
        /// Safe - doesn't leak anything
        /// </SecurityNote>
        private CollectionDataContract(Type type, string invalidCollectionInSharedContractMessage) : base(new CollectionDataContractCriticalHelper(type, invalidCollectionInSharedContractMessage))
        {
            InitCollectionDataContract(GetSharedTypeContract(type));
        }
        [SecurityCritical]

        /// <SecurityNote>
        /// Critical - initializes SecurityCritical fields; called from all constructors
        /// </SecurityNote>
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

        private void InitSharedTypeContract()
        {
        }

        private static Type[] KnownInterfaces
        {
            /// <SecurityNote>
            /// Critical - fetches the critical knownInterfaces property
            /// Safe - knownInterfaces only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return CollectionDataContractCriticalHelper.KnownInterfaces; }
        }

        internal CollectionKind Kind
        {
            /// <SecurityNote>
            /// Critical - fetches the critical kind property
            /// Safe - kind only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.Kind; }
        }

        public Type ItemType
        {
            /// <SecurityNote>
            /// Critical - fetches the critical itemType property
            /// Safe - itemType only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.ItemType; }
            set { _helper.ItemType = value; }
        }

        public DataContract ItemContract
        {
            /// <SecurityNote>
            /// Critical - fetches the critical itemContract property
            /// Safe - itemContract only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            {
                return _itemContract ?? _helper.ItemContract;
            }
            /// <SecurityNote>
            /// Critical - sets the critical itemContract property
            /// </SecurityNote>
            [SecurityCritical]
            set
            {
                _itemContract = value;
                _helper.ItemContract = value;
            }
        }

        internal DataContract SharedTypeContract
        {
            /// <SecurityNote>
            /// Critical - fetches the critical sharedTypeContract property
            /// Safe - sharedTypeContract only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.SharedTypeContract; }
        }

        public string ItemName
        {
            /// <SecurityNote>
            /// Critical - fetches the critical itemName property
            /// Safe - itemName only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.ItemName; }
            /// <SecurityNote>
            /// Critical - sets the critical itemName property
            /// </SecurityNote>
            [SecurityCritical]
            set
            { _helper.ItemName = value; }
        }

        public XmlDictionaryString CollectionItemName
        {
            /// <SecurityNote>
            /// Critical - fetches the critical collectionItemName property
            /// Safe - collectionItemName only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _collectionItemName; }
            set { _collectionItemName = value; }
        }

        public string KeyName
        {
            /// <SecurityNote>
            /// Critical - fetches the critical keyName property
            /// Safe - keyName only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.KeyName; }
            /// <SecurityNote>
            /// Critical - sets the critical keyName property
            /// </SecurityNote>
            [SecurityCritical]
            set
            { _helper.KeyName = value; }
        }

        public string ValueName
        {
            /// <SecurityNote>
            /// Critical - fetches the critical valueName property
            /// Safe - valueName only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.ValueName; }
            /// <SecurityNote>
            /// Critical - sets the critical valueName property
            /// </SecurityNote>
            [SecurityCritical]
            set
            { _helper.ValueName = value; }
        }

        internal bool IsDictionary
        {
            get { return KeyName != null; }
        }

        public XmlDictionaryString ChildElementNamespace
        {
            /// <SecurityNote>
            /// Critical - fetches the critical childElementNamespace property
            /// Safe - childElementNamespace only needs to be protected for write; initialized in getter if null
            /// </SecurityNote>
            [SecuritySafeCritical]
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
            /// <SecurityNote>
            /// Critical - fetches the critical isConstructorCheckRequired property
            /// Safe - isConstructorCheckRequired only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.IsConstructorCheckRequired; }
            /// <SecurityNote>
            /// Critical - sets the critical isConstructorCheckRequired property
            /// </SecurityNote>
            [SecurityCritical]
            set
            { _helper.IsConstructorCheckRequired = value; }
        }

        internal MethodInfo GetEnumeratorMethod
        {
            /// <SecurityNote>
            /// Critical - fetches the critical getEnumeratorMethod property
            /// Safe - getEnumeratorMethod only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.GetEnumeratorMethod; }
        }

        internal MethodInfo AddMethod
        {
            /// <SecurityNote>
            /// Critical - fetches the critical addMethod property
            /// Safe - addMethod only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.AddMethod; }
        }

        internal ConstructorInfo Constructor
        {
            /// <SecurityNote>
            /// Critical - fetches the critical constructor property
            /// Safe - constructor only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.Constructor; }
        }

        public override DataContractDictionary KnownDataContracts
        {
            /// <SecurityNote>
            /// Critical - fetches the critical knownDataContracts property
            /// Safe - knownDataContracts only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.KnownDataContracts; }
            /// <SecurityNote>
            /// Critical - sets the critical knownDataContracts property
            /// </SecurityNote>
            [SecurityCritical]
            set
            { _helper.KnownDataContracts = value; }
        }

        internal string InvalidCollectionInSharedContractMessage
        {
            /// <SecurityNote>
            /// Critical - fetches the critical invalidCollectionInSharedContractMessage property
            /// Safe - invalidCollectionInSharedContractMessage only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.InvalidCollectionInSharedContractMessage; }
        }

        private bool ItemNameSetExplicit
        {
            /// <SecurityNote>
            /// Critical - fetches the critical itemNameSetExplicit property
            /// Safe - itemNameSetExplicit only needs to be protected for write
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return _helper.ItemNameSetExplicit; }
        }

#if !NET_NATIVE
        internal XmlFormatCollectionWriterDelegate XmlFormatWriterDelegate
        {
            /// <SecurityNote>
            /// Critical - fetches the critical xmlFormatWriterDelegate property
            /// Safe - xmlFormatWriterDelegate only needs to be protected for write; initialized in getter if null
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            {
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
        }
#else
        public XmlFormatCollectionWriterDelegate XmlFormatWriterDelegate { get; set; }
#endif

#if !NET_NATIVE
        internal XmlFormatCollectionReaderDelegate XmlFormatReaderDelegate
        {
            /// <SecurityNote>
            /// Critical - fetches the critical xmlFormatReaderDelegate property
            /// Safe - xmlFormatReaderDelegate only needs to be protected for write; initialized in getter if null
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            {
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
        }
#else
        public XmlFormatCollectionReaderDelegate XmlFormatReaderDelegate { get; set; }
#endif

#if !NET_NATIVE
        internal XmlFormatGetOnlyCollectionReaderDelegate XmlFormatGetOnlyCollectionReaderDelegate
        {
            /// <SecurityNote>
            /// Critical - fetches the critical xmlFormatReaderDelegate property
            /// Safe - xmlFormatReaderDelegate only needs to be protected for write; initialized in getter if null
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            {
                if (_helper.XmlFormatGetOnlyCollectionReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.XmlFormatGetOnlyCollectionReaderDelegate == null)
                        {
                            XmlFormatGetOnlyCollectionReaderDelegate tempDelegate = new XmlFormatReaderGenerator().GenerateGetOnlyCollectionReader(this);
                            Interlocked.MemoryBarrier();
                            _helper.XmlFormatGetOnlyCollectionReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.XmlFormatGetOnlyCollectionReaderDelegate;
            }
        }
#else
        public XmlFormatGetOnlyCollectionReaderDelegate XmlFormatGetOnlyCollectionReaderDelegate { get; set; }
#endif

        [SecurityCritical]
        /// <SecurityNote>
        /// Critical - holds all state used for (de)serializing collections.
        ///            since the data is cached statically, we lock down access to it.
        /// </SecurityNote>
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
#if NET_NATIVE
                            _itemContract = DataContract.GetDataContractFromGeneratedAssembly(ItemType);
#else
                            _itemContract = DataContract.GetDataContract(ItemType);
#endif
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

            internal bool IsDictionary
            {
                get { return KeyName != null; }
            }

            public XmlDictionaryString ChildElementNamespace
            {
                get { return _childElementNamespace; }
                set { _childElementNamespace = value; }
            }

            internal MethodInfo GetEnumeratorMethod
            {
                get { return _getEnumeratorMethod; }
            }

            internal MethodInfo AddMethod
            {
                get { return _addMethod; }
            }

            internal ConstructorInfo Constructor
            {
                get { return _constructor; }
            }

            internal override DataContractDictionary KnownDataContracts
            {
                [SecurityCritical]
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
                [SecurityCritical]
                set
                { _knownDataContracts = value; }
            }

            internal string InvalidCollectionInSharedContractMessage
            {
                get { return _invalidCollectionInSharedContractMessage; }
            }

            internal bool ItemNameSetExplicit
            {
                get { return _itemNameSetExplicit; }
            }

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
#if !NET_NATIVE
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
#else
            dataContract = DataContract.GetDataContractFromGeneratedAssembly(type);
            if (dataContract is CollectionDataContract)
            {
                return true;
            }
            else
            {
                dataContract = null;
                return false;
            }
#endif
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
#if !NET_NATIVE
                    dataContract = new CollectionDataContract(type, kind, itemType, getEnumeratorMethod, addMethod, defaultCtor, !constructorRequired);
#else
                    dataContract = DataContract.GetDataContractFromGeneratedAssembly(type);
#endif
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

        /// <SecurityNote>
        /// Critical - sets the critical IsConstructorCheckRequired property on CollectionDataContract 
        /// </SecurityNote>
        [SecuritySafeCritical]
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
        internal bool RequiresMemberAccessForRead(SecurityException securityException, string[] serializationAssemblyPatterns)
        {
            if (!IsTypeVisible(UnderlyingType, serializationAssemblyPatterns))
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
            if (ItemType != null && !IsTypeVisible(ItemType, serializationAssemblyPatterns))
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
            if (ConstructorRequiresMemberAccess(Constructor, serializationAssemblyPatterns))
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
            if (MethodRequiresMemberAccess(this.AddMethod, serializationAssemblyPatterns))
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
        internal bool RequiresMemberAccessForWrite(SecurityException securityException, string[] serializationAssemblyPatterns)
        {
            if (!IsTypeVisible(UnderlyingType, serializationAssemblyPatterns))
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
            if (ItemType != null && !IsTypeVisible(ItemType, serializationAssemblyPatterns))
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
