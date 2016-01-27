// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: DebugView class for generic collections
** 
** 
**
**
=============================================================================*/

namespace System.Collections.Generic {
    using System;
    using System.Collections.ObjectModel;
    using System.Security.Permissions;
    using System.Diagnostics;    
    using System.Diagnostics.Contracts;

    //
    // VS IDE can't differentiate between types with the same name from different
    // assembly. So we need to use different names for collection debug view for 
    // collections in mscorlib.dll and system.dll.
    //
    internal sealed class Mscorlib_CollectionDebugView<T> {
        private ICollection<T> collection; 
        
        public Mscorlib_CollectionDebugView(ICollection<T> collection) {
            if (collection == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);

                this.collection = collection;
        }
       
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items   { 
            get {
                T[] items = new T[collection.Count];
                collection.CopyTo(items, 0);
                return items;
            }
        }
    }        

    internal sealed class Mscorlib_DictionaryKeyCollectionDebugView<TKey, TValue> {
        private ICollection<TKey> collection; 
        
        public Mscorlib_DictionaryKeyCollectionDebugView(ICollection<TKey> collection) {
            if (collection == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);

                this.collection = collection;
        }
       
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TKey[] Items   { 
            get {
                TKey[] items = new TKey[collection.Count];
                collection.CopyTo(items, 0);
                return items;
            }
        }
    }        

    internal sealed class Mscorlib_DictionaryValueCollectionDebugView<TKey, TValue> {
        private ICollection<TValue> collection; 
        
        public Mscorlib_DictionaryValueCollectionDebugView(ICollection<TValue> collection) {
            if (collection == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);

                this.collection = collection;
        }
       
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TValue[] Items   { 
            get {
                TValue[] items = new TValue[collection.Count];
                collection.CopyTo(items, 0);
                return items;
            }
        }
    }        

    internal sealed class Mscorlib_DictionaryDebugView<K, V> {
        private IDictionary<K, V> dict; 
        
        public Mscorlib_DictionaryDebugView(IDictionary<K, V> dictionary) {
            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);

                this.dict = dictionary;
        }
       
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<K, V>[] Items   { 
            get {
                KeyValuePair<K, V>[] items = new KeyValuePair<K, V>[dict.Count];
                dict.CopyTo(items, 0);
                return items;
            }
        }
    }        

    internal sealed class Mscorlib_KeyedCollectionDebugView<K, T> {
        private KeyedCollection<K, T> kc; 
        
        public Mscorlib_KeyedCollectionDebugView(KeyedCollection<K, T> keyedCollection) {
            if (keyedCollection == null) {
                throw new ArgumentNullException("keyedCollection");
            }
            Contract.EndContractBlock();

            kc = keyedCollection;
        }
       
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items   { 
            get {
                T[] items = new T[kc.Count];
                kc.CopyTo(items, 0);
                return items;
            }
        }        
    }            
}
