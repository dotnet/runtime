// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using AstUtils = System.Linq.Expressions.Utils;

namespace System.Dynamic
{
    /// <summary>
    /// Represents an object with members that can be dynamically added and removed at runtime.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public sealed class ExpandoObject : IDynamicMetaObjectProvider, IDictionary<string, object>, INotifyPropertyChanged
    {
        private static readonly MethodInfo ExpandoTryGetValue =
            typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.ExpandoTryGetValue));

        private static readonly MethodInfo ExpandoTrySetValue =
            typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.ExpandoTrySetValue));

        private static readonly MethodInfo ExpandoTryDeleteValue =
            typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.ExpandoTryDeleteValue));

        private static readonly MethodInfo ExpandoPromoteClass =
            typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.ExpandoPromoteClass));

        private static readonly MethodInfo ExpandoCheckVersion =
            typeof(RuntimeOps).GetMethod(nameof(RuntimeOps.ExpandoCheckVersion));

        internal readonly object LockObject;                          // the readonly field is used for locking the Expando object
        private ExpandoData _data;                                    // the data currently being held by the Expando object
        private int _count;                                           // the count of available members

        internal readonly static object Uninitialized = new object(); // A marker object used to identify that a value is uninitialized.

        internal const int AmbiguousMatchFound = -2;        // The value is used to indicate there exists ambiguous match in the Expando object
        internal const int NoMatch = -1;                    // The value is used to indicate there is no matching member

        private PropertyChangedEventHandler _propertyChanged;

        /// <summary>
        /// Creates a new ExpandoObject with no members.
        /// </summary>
        public ExpandoObject()
        {
            _data = ExpandoData.Empty;
            LockObject = new object();
        }

        #region Get/Set/Delete Helpers

        /// <summary>
        /// Try to get the data stored for the specified class at the specified index.  If the
        /// class has changed a full lookup for the slot will be performed and the correct
        /// value will be retrieved.
        /// </summary>
        internal bool TryGetValue(object indexClass, int index, string name, bool ignoreCase, out object value)
        {
            // read the data now.  The data is immutable so we get a consistent view.
            // If there's a concurrent writer they will replace data and it just appears
            // that we won the race
            ExpandoData data = _data;
            if (data.Class != indexClass || ignoreCase)
            {
                /* Re-search for the index matching the name here if
                 *  1) the class has changed, we need to get the correct index and return
                 *  the value there.
                 *  2) the search is case insensitive:
                 *      a. the member specified by index may be deleted, but there might be other
                 *      members matching the name if the binder is case insensitive.
                 *      b. the member that exactly matches the name didn't exist before and exists now,
                 *      need to find the exact match.
                 */
                index = data.Class.GetValueIndex(name, ignoreCase, this);
                if (index == ExpandoObject.AmbiguousMatchFound)
                {
                    throw System.Linq.Expressions.Error.AmbiguousMatchInExpandoObject(name);
                }
            }

            if (index == ExpandoObject.NoMatch)
            {
                value = null;
                return false;
            }

            // Capture the value into a temp, so it doesn't get mutated after we check
            // for Uninitialized.
            object temp = data[index];
            if (temp == Uninitialized)
            {
                value = null;
                return false;
            }

            // index is now known to be correct
            value = temp;
            return true;
        }

        /// <summary>
        /// Sets the data for the specified class at the specified index.  If the class has
        /// changed then a full look for the slot will be performed.  If the new class does
        /// not have the provided slot then the Expando's class will change. Only case sensitive
        /// setter is supported in ExpandoObject.
        /// </summary>
        internal void TrySetValue(object indexClass, int index, object value, string name, bool ignoreCase, bool add)
        {
            ExpandoData data;
            object oldValue;

            lock (LockObject)
            {
                data = _data;

                if (data.Class != indexClass || ignoreCase)
                {
                    // The class has changed or we are doing a case-insensitive search,
                    // we need to get the correct index and set the value there.  If we
                    // don't have the value then we need to promote the class - that
                    // should only happen when we have multiple concurrent writers.
                    index = data.Class.GetValueIndex(name, ignoreCase, this);
                    if (index == ExpandoObject.AmbiguousMatchFound)
                    {
                        throw System.Linq.Expressions.Error.AmbiguousMatchInExpandoObject(name);
                    }
                    if (index == ExpandoObject.NoMatch)
                    {
                        // Before creating a new class with the new member, need to check
                        // if there is the exact same member but is deleted. We should reuse
                        // the class if there is such a member.
                        int exactMatch = ignoreCase ?
                            data.Class.GetValueIndexCaseSensitive(name) :
                            index;
                        if (exactMatch != ExpandoObject.NoMatch)
                        {
                            Debug.Assert(data[exactMatch] == Uninitialized);
                            index = exactMatch;
                        }
                        else
                        {
                            ExpandoClass newClass = data.Class.FindNewClass(name);
                            data = PromoteClassCore(data.Class, newClass);
                            // After the class promotion, there must be an exact match,
                            // so we can do case-sensitive search here.
                            index = data.Class.GetValueIndexCaseSensitive(name);
                            Debug.Assert(index != ExpandoObject.NoMatch);
                        }
                    }
                }

                // Setting an uninitialized member increases the count of available members
                oldValue = data[index];
                if (oldValue == Uninitialized)
                {
                    _count++;
                }
                else if (add)
                {
                    throw System.Linq.Expressions.Error.SameKeyExistsInExpando(name);
                }

                data[index] = value;
            }

            // Notify property changed outside the lock
            PropertyChangedEventHandler propertyChanged = _propertyChanged;
            if (propertyChanged != null && value != oldValue)
            {
                propertyChanged(this, new PropertyChangedEventArgs(data.Class.Keys[index]));
            }
        }

        /// <summary>
        /// Deletes the data stored for the specified class at the specified index.
        /// </summary>
        internal bool TryDeleteValue(object indexClass, int index, string name, bool ignoreCase, object deleteValue)
        {
            ExpandoData data;
            lock (LockObject)
            {
                data = _data;

                if (data.Class != indexClass || ignoreCase)
                {
                    // the class has changed or we are doing a case-insensitive search,
                    // we need to get the correct index.  If there is no associated index
                    // we simply can't have the value and we return false.
                    index = data.Class.GetValueIndex(name, ignoreCase, this);
                    if (index == ExpandoObject.AmbiguousMatchFound)
                    {
                        throw System.Linq.Expressions.Error.AmbiguousMatchInExpandoObject(name);
                    }
                }
                if (index == ExpandoObject.NoMatch)
                {
                    return false;
                }

                object oldValue = data[index];
                if (oldValue == Uninitialized)
                {
                    return false;
                }

                // Make sure the value matches, if requested.
                //
                // It's a shame we have to call Equals with the lock held but
                // there doesn't seem to be a good way around that, and
                // ConcurrentDictionary in mscorlib does the same thing.
                if (deleteValue != Uninitialized && !object.Equals(oldValue, deleteValue))
                {
                    return false;
                }

                data[index] = Uninitialized;

                // Deleting an available member decreases the count of available members
                _count--;
            }

            // Notify property changed outside the lock
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(data.Class.Keys[index]));

            return true;
        }

        /// <summary>
        /// Returns true if the member at the specified index has been deleted,
        /// otherwise false. Call this function holding the lock.
        /// </summary>
        internal bool IsDeletedMember(int index)
        {
            Debug.Assert(index >= 0 && index <= _data.Length);

            if (index == _data.Length)
            {
                // The member is a newly added by SetMemberBinder and not in data yet
                return false;
            }

            return _data[index] == ExpandoObject.Uninitialized;
        }

        /// <summary>
        /// Exposes the ExpandoClass which we've associated with this
        /// Expando object.  Used for type checks in rules.
        /// </summary>
        internal ExpandoClass Class => _data.Class;

        /// <summary>
        /// Promotes the class from the old type to the new type and returns the new
        /// ExpandoData object.
        /// </summary>
        private ExpandoData PromoteClassCore(ExpandoClass oldClass, ExpandoClass newClass)
        {
            Debug.Assert(oldClass != newClass);

            lock (LockObject)
            {
                if (_data.Class == oldClass)
                {
                    _data = _data.UpdateClass(newClass);
                }
                return _data;
            }
        }

        /// <summary>
        /// Internal helper to promote a class.  Called from our RuntimeOps helper.  This
        /// version simply doesn't expose the ExpandoData object which is a private
        /// data structure.
        /// </summary>
        internal void PromoteClass(object oldClass, object newClass)
        {
            PromoteClassCore((ExpandoClass)oldClass, (ExpandoClass)newClass);
        }

        #endregion

        #region IDynamicMetaObjectProvider Members

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new MetaExpando(parameter, this);
        }

        #endregion

        #region Helper methods
        private void TryAddMember(string key, object value)
        {
            ContractUtils.RequiresNotNull(key, nameof(key));
            // Pass null to the class, which forces lookup.
            TrySetValue(null, -1, value, key, ignoreCase: false, add: true);
        }

        private bool TryGetValueForKey(string key, out object value)
        {
            // Pass null to the class, which forces lookup.
            return TryGetValue(null, -1, key, ignoreCase: false, value: out value);
        }

        private bool ExpandoContainsKey(string key)
        {
            return _data.Class.GetValueIndexCaseSensitive(key) >= 0;
        }

        // We create a non-generic type for the debug view for each different collection type
        // that uses DebuggerTypeProxy, instead of defining a generic debug view type and
        // using different instantiations. The reason for this is that support for generics
        // with using DebuggerTypeProxy is limited. For C#, DebuggerTypeProxy supports only
        // open types (from MSDN http://msdn.microsoft.com/en-us/library/d8eyd8zc.aspx).
        private sealed class KeyCollectionDebugView
        {
            private readonly ICollection<string> _collection;

            public KeyCollectionDebugView(ICollection<string> collection)
            {
                ContractUtils.RequiresNotNull(collection, nameof(collection));
                _collection = collection;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public string[] Items
            {
                get
                {
                    string[] items = new string[_collection.Count];
                    _collection.CopyTo(items, 0);
                    return items;
                }
            }
        }

        [DebuggerTypeProxy(typeof(KeyCollectionDebugView))]
        [DebuggerDisplay("Count = {Count}")]
        private class KeyCollection : ICollection<string>
        {
            private readonly ExpandoObject _expando;
            private readonly int _expandoVersion;
            private readonly int _expandoCount;
            private readonly ExpandoData _expandoData;

            internal KeyCollection(ExpandoObject expando)
            {
                lock (expando.LockObject)
                {
                    _expando = expando;
                    _expandoVersion = expando._data.Version;
                    _expandoCount = expando._count;
                    _expandoData = expando._data;
                }
            }

            private void CheckVersion()
            {
                if (_expando._data.Version != _expandoVersion || _expandoData != _expando._data)
                {
                    //the underlying expando object has changed
                    throw System.Linq.Expressions.Error.CollectionModifiedWhileEnumerating();
                }
            }

            #region ICollection<string> Members

            public void Add(string item)
            {
                throw System.Linq.Expressions.Error.CollectionReadOnly();
            }

            public void Clear()
            {
                throw System.Linq.Expressions.Error.CollectionReadOnly();
            }

            public bool Contains(string item)
            {
                lock (_expando.LockObject)
                {
                    CheckVersion();
                    return _expando.ExpandoContainsKey(item);
                }
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                ContractUtils.RequiresNotNull(array, nameof(array));
                ContractUtils.RequiresArrayRange(array, arrayIndex, _expandoCount, nameof(arrayIndex), nameof(Count));
                lock (_expando.LockObject)
                {
                    CheckVersion();
                    ExpandoData data = _expando._data;
                    for (int i = 0; i < data.Class.Keys.Length; i++)
                    {
                        if (data[i] != Uninitialized)
                        {
                            array[arrayIndex++] = data.Class.Keys[i];
                        }
                    }
                }
            }

            public int Count
            {
                get
                {
                    CheckVersion();
                    return _expandoCount;
                }
            }

            public bool IsReadOnly => true;

            public bool Remove(string item)
            {
                throw System.Linq.Expressions.Error.CollectionReadOnly();
            }

            #endregion

            #region IEnumerable<string> Members

            public IEnumerator<string> GetEnumerator()
            {
                for (int i = 0, n = _expandoData.Class.Keys.Length; i < n; i++)
                {
                    CheckVersion();
                    if (_expandoData[i] != Uninitialized)
                    {
                        yield return _expandoData.Class.Keys[i];
                    }
                }
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        // We create a non-generic type for the debug view for each different collection type
        // that uses DebuggerTypeProxy, instead of defining a generic debug view type and
        // using different instantiations. The reason for this is that support for generics
        // with using DebuggerTypeProxy is limited. For C#, DebuggerTypeProxy supports only
        // open types (from MSDN http://msdn.microsoft.com/en-us/library/d8eyd8zc.aspx).
        private sealed class ValueCollectionDebugView
        {
            private readonly ICollection<object> _collection;

            public ValueCollectionDebugView(ICollection<object> collection)
            {
                ContractUtils.RequiresNotNull(collection, nameof(collection));
                _collection = collection;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object[] Items
            {
                get
                {
                    object[] items = new object[_collection.Count];
                    _collection.CopyTo(items, 0);
                    return items;
                }
            }
        }

        [DebuggerTypeProxy(typeof(ValueCollectionDebugView))]
        [DebuggerDisplay("Count = {Count}")]
        private class ValueCollection : ICollection<object>
        {
            private readonly ExpandoObject _expando;
            private readonly int _expandoVersion;
            private readonly int _expandoCount;
            private readonly ExpandoData _expandoData;

            internal ValueCollection(ExpandoObject expando)
            {
                lock (expando.LockObject)
                {
                    _expando = expando;
                    _expandoVersion = expando._data.Version;
                    _expandoCount = expando._count;
                    _expandoData = expando._data;
                }
            }

            private void CheckVersion()
            {
                if (_expando._data.Version != _expandoVersion || _expandoData != _expando._data)
                {
                    //the underlying expando object has changed
                    throw System.Linq.Expressions.Error.CollectionModifiedWhileEnumerating();
                }
            }

            #region ICollection<string> Members

            public void Add(object item)
            {
                throw System.Linq.Expressions.Error.CollectionReadOnly();
            }

            public void Clear()
            {
                throw System.Linq.Expressions.Error.CollectionReadOnly();
            }

            public bool Contains(object item)
            {
                lock (_expando.LockObject)
                {
                    CheckVersion();

                    ExpandoData data = _expando._data;
                    for (int i = 0; i < data.Class.Keys.Length; i++)
                    {
                        // See comment in TryDeleteValue; it's okay to call
                        // object.Equals with the lock held.
                        if (object.Equals(data[i], item))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            public void CopyTo(object[] array, int arrayIndex)
            {
                ContractUtils.RequiresNotNull(array, nameof(array));
                ContractUtils.RequiresArrayRange(array, arrayIndex, _expandoCount, nameof(arrayIndex), nameof(Count));
                lock (_expando.LockObject)
                {
                    CheckVersion();
                    ExpandoData data = _expando._data;
                    for (int i = 0; i < data.Class.Keys.Length; i++)
                    {
                        if (data[i] != Uninitialized)
                        {
                            array[arrayIndex++] = data[i];
                        }
                    }
                }
            }

            public int Count
            {
                get
                {
                    CheckVersion();
                    return _expandoCount;
                }
            }

            public bool IsReadOnly => true;

            public bool Remove(object item)
            {
                throw System.Linq.Expressions.Error.CollectionReadOnly();
            }

            #endregion

            #region IEnumerable<string> Members

            public IEnumerator<object> GetEnumerator()
            {
                ExpandoData data = _expando._data;
                for (int i = 0; i < data.Class.Keys.Length; i++)
                {
                    CheckVersion();
                    // Capture the value into a temp so we don't inadvertently
                    // return Uninitialized.
                    object temp = data[i];
                    if (temp != Uninitialized)
                    {
                        yield return temp;
                    }
                }
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion

        #region IDictionary<string, object> Members

        ICollection<string> IDictionary<string, object>.Keys => new KeyCollection(this);

        ICollection<object> IDictionary<string, object>.Values => new ValueCollection(this);

        object IDictionary<string, object>.this[string key]
        {
            get
            {
                object value;
                if (!TryGetValueForKey(key, out value))
                {
                    throw System.Linq.Expressions.Error.KeyDoesNotExistInExpando(key);
                }
                return value;
            }
            set
            {
                ContractUtils.RequiresNotNull(key, nameof(key));
                // Pass null to the class, which forces lookup.
                TrySetValue(null, -1, value, key, ignoreCase: false, add: false);
            }
        }

        void IDictionary<string, object>.Add(string key, object value)
        {
            this.TryAddMember(key, value);
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            ContractUtils.RequiresNotNull(key, nameof(key));

            ExpandoData data = _data;
            int index = data.Class.GetValueIndexCaseSensitive(key);
            return index >= 0 && data[index] != Uninitialized;
        }

        bool IDictionary<string, object>.Remove(string key)
        {
            ContractUtils.RequiresNotNull(key, nameof(key));
            // Pass null to the class, which forces lookup.
            return TryDeleteValue(null, -1, key, ignoreCase: false, deleteValue: Uninitialized);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            return TryGetValueForKey(key, out value);
        }

        #endregion

        #region ICollection<KeyValuePair<string, object>> Members

        int ICollection<KeyValuePair<string, object>>.Count => _count;

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            TryAddMember(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            // We remove both class and data!
            ExpandoData data;
            lock (LockObject)
            {
                data = _data;
                _data = ExpandoData.Empty;
                _count = 0;
            }

            // Notify property changed for all properties.
            var propertyChanged = _propertyChanged;
            if (propertyChanged != null)
            {
                for (int i = 0, n = data.Class.Keys.Length; i < n; i++)
                {
                    if (data[i] != Uninitialized)
                    {
                        propertyChanged(this, new PropertyChangedEventArgs(data.Class.Keys[i]));
                    }
                }
            }
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            object value;
            if (!TryGetValueForKey(item.Key, out value))
            {
                return false;
            }

            return object.Equals(value, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ContractUtils.RequiresNotNull(array, nameof(array));
            ContractUtils.RequiresArrayRange(array, arrayIndex, _count, nameof(arrayIndex), nameof(ICollection<KeyValuePair<string, object>>.Count));

            // We want this to be atomic and not throw
            lock (LockObject)
            {
                foreach (KeyValuePair<string, object> item in this)
                {
                    array[arrayIndex++] = item;
                }
            }
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return TryDeleteValue(null, -1, item.Key, ignoreCase: false, deleteValue: item.Value);
        }

        #endregion

        #region IEnumerable<KeyValuePair<string, object>> Member

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            ExpandoData data = _data;
            return GetExpandoEnumerator(data, data.Version);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            ExpandoData data = _data;
            return GetExpandoEnumerator(data, data.Version);
        }

        // Note: takes the data and version as parameters so they will be
        // captured before the first call to MoveNext().
        private IEnumerator<KeyValuePair<string, object>> GetExpandoEnumerator(ExpandoData data, int version)
        {
            for (int i = 0; i < data.Class.Keys.Length; i++)
            {
                if (_data.Version != version || data != _data)
                {
                    // The underlying expando object has changed:
                    // 1) the version of the expando data changed
                    // 2) the data object is changed
                    throw System.Linq.Expressions.Error.CollectionModifiedWhileEnumerating();
                }
                // Capture the value into a temp so we don't inadvertently
                // return Uninitialized.
                object temp = data[i];
                if (temp != Uninitialized)
                {
                    yield return new KeyValuePair<string, object>(data.Class.Keys[i], temp);
                }
            }
        }

        #endregion

        #region MetaExpando

        private class MetaExpando : DynamicMetaObject
        {
            public MetaExpando(Expression expression, ExpandoObject value)
                : base(expression, BindingRestrictions.Empty, value)
            {
            }

            private DynamicMetaObject BindGetOrInvokeMember(DynamicMetaObjectBinder binder, string name, bool ignoreCase, DynamicMetaObject fallback, Func<DynamicMetaObject, DynamicMetaObject> fallbackInvoke)
            {
                ExpandoClass klass = Value.Class;

                //try to find the member, including the deleted members
                int index = klass.GetValueIndex(name, ignoreCase, Value);

                ParameterExpression value = Expression.Parameter(typeof(object), "value");

                Expression tryGetValue = Expression.Call(
                    ExpandoTryGetValue,
                    GetLimitedSelf(),
                    Expression.Constant(klass, typeof(object)),
                    AstUtils.Constant(index),
                    Expression.Constant(name),
                    AstUtils.Constant(ignoreCase),
                    value
                );

                var result = new DynamicMetaObject(value, BindingRestrictions.Empty);
                if (fallbackInvoke != null)
                {
                    result = fallbackInvoke(result);
                }

                result = new DynamicMetaObject(
                    Expression.Block(
                        new TrueReadOnlyCollection<ParameterExpression>(value),
                        Expression.Condition(
                            tryGetValue,
                            result.Expression,
                            fallback.Expression,
                            typeof(object)
                        )
                    ),
                    result.Restrictions.Merge(fallback.Restrictions)
                );

                return AddDynamicTestAndDefer(binder, Value.Class, null, result);
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                ContractUtils.RequiresNotNull(binder, nameof(binder));
                return BindGetOrInvokeMember(
                    binder,
                    binder.Name,
                    binder.IgnoreCase,
                    binder.FallbackGetMember(this),
                    null
                );
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                ContractUtils.RequiresNotNull(binder, nameof(binder));
                return BindGetOrInvokeMember(
                    binder,
                    binder.Name,
                    binder.IgnoreCase,
                    binder.FallbackInvokeMember(this, args),
                    value => binder.FallbackInvoke(value, args, null)
                );
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                ContractUtils.RequiresNotNull(binder, nameof(binder));
                ContractUtils.RequiresNotNull(value, nameof(value));

                ExpandoClass klass;
                int index;

                ExpandoClass originalClass = GetClassEnsureIndex(binder.Name, binder.IgnoreCase, Value, out klass, out index);

                return AddDynamicTestAndDefer(
                    binder,
                    klass,
                    originalClass,
                    new DynamicMetaObject(
                        Expression.Call(
                            ExpandoTrySetValue,
                            GetLimitedSelf(),
                            Expression.Constant(klass, typeof(object)),
                            AstUtils.Constant(index),
                            Expression.Convert(value.Expression, typeof(object)),
                            Expression.Constant(binder.Name),
                            AstUtils.Constant(binder.IgnoreCase)
                        ),
                        BindingRestrictions.Empty
                    )
                );
            }

            public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
            {
                ContractUtils.RequiresNotNull(binder, nameof(binder));

                int index = Value.Class.GetValueIndex(binder.Name, binder.IgnoreCase, Value);

                Expression tryDelete = Expression.Call(
                    ExpandoTryDeleteValue,
                    GetLimitedSelf(),
                    Expression.Constant(Value.Class, typeof(object)),
                    AstUtils.Constant(index),
                    Expression.Constant(binder.Name),
                    AstUtils.Constant(binder.IgnoreCase)
                );
                DynamicMetaObject fallback = binder.FallbackDeleteMember(this);

                DynamicMetaObject target = new DynamicMetaObject(
                    Expression.IfThen(Expression.Not(tryDelete), fallback.Expression),
                    fallback.Restrictions
                );

                return AddDynamicTestAndDefer(binder, Value.Class, null, target);
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                var expandoData = Value._data;
                var klass = expandoData.Class;
                for (int i = 0; i < klass.Keys.Length; i++)
                {
                    object val = expandoData[i];
                    if (val != ExpandoObject.Uninitialized)
                    {
                        yield return klass.Keys[i];
                    }
                }
            }

            /// <summary>
            /// Adds a dynamic test which checks if the version has changed.  The test is only necessary for
            /// performance as the methods will do the correct thing if called with an incorrect version.
            /// </summary>
            private DynamicMetaObject AddDynamicTestAndDefer(DynamicMetaObjectBinder binder, ExpandoClass klass, ExpandoClass originalClass, DynamicMetaObject succeeds)
            {
                Expression ifTestSucceeds = succeeds.Expression;
                if (originalClass != null)
                {
                    // we are accessing a member which has not yet been defined on this class.
                    // We force a class promotion after the type check.  If the class changes the
                    // promotion will fail and the set/delete will do a full lookup using the new
                    // class to discover the name.
                    Debug.Assert(originalClass != klass);

                    ifTestSucceeds = Expression.Block(
                        Expression.Call(
                            null,
                            ExpandoPromoteClass,
                            GetLimitedSelf(),
                            Expression.Constant(originalClass, typeof(object)),
                            Expression.Constant(klass, typeof(object))
                        ),
                        succeeds.Expression
                    );
                }

                return new DynamicMetaObject(
                    Expression.Condition(
                        Expression.Call(
                            null,
                            ExpandoCheckVersion,
                            GetLimitedSelf(),
                            Expression.Constant(originalClass ?? klass, typeof(object))
                        ),
                        ifTestSucceeds,
                        binder.GetUpdateExpression(ifTestSucceeds.Type)
                    ),
                    GetRestrictions().Merge(succeeds.Restrictions)
                );
            }

            /// <summary>
            /// Gets the class and the index associated with the given name.  Does not update the expando object.  Instead
            /// this returns both the original and desired new class.  A rule is created which includes the test for the
            /// original class, the promotion to the new class, and the set/delete based on the class post-promotion.
            /// </summary>
            private ExpandoClass GetClassEnsureIndex(string name, bool caseInsensitive, ExpandoObject obj, out ExpandoClass klass, out int index)
            {
                ExpandoClass originalClass = Value.Class;

                index = originalClass.GetValueIndex(name, caseInsensitive, obj);
                if (index == ExpandoObject.AmbiguousMatchFound)
                {
                    klass = originalClass;
                    return null;
                }
                if (index == ExpandoObject.NoMatch)
                {
                    // go ahead and find a new class now...
                    ExpandoClass newClass = originalClass.FindNewClass(name);

                    klass = newClass;
                    index = newClass.GetValueIndexCaseSensitive(name);

                    Debug.Assert(index != ExpandoObject.NoMatch);
                    return originalClass;
                }
                else
                {
                    klass = originalClass;
                    return null;
                }
            }

            /// <summary>
            /// Returns our Expression converted to our known LimitType
            /// </summary>
            private Expression GetLimitedSelf()
            {
                if (TypeUtils.AreEquivalent(Expression.Type, LimitType))
                {
                    return Expression;
                }
                return Expression.Convert(Expression, LimitType);
            }

            /// <summary>
            /// Returns a Restrictions object which includes our current restrictions merged
            /// with a restriction limiting our type
            /// </summary>
            private BindingRestrictions GetRestrictions()
            {
                Debug.Assert(Restrictions == BindingRestrictions.Empty, "We don't merge, restrictions are always empty");

                return BindingRestrictions.GetTypeRestriction(this);
            }

            public new ExpandoObject Value => (ExpandoObject)base.Value;
        }

        #endregion

        #region ExpandoData

        /// <summary>
        /// Stores the class and the data associated with the class as one atomic
        /// pair.  This enables us to do a class check in a thread safe manner w/o
        /// requiring locks.
        /// </summary>
        private class ExpandoData
        {
            internal static ExpandoData Empty = new ExpandoData();

            /// <summary>
            /// the dynamically assigned class associated with the Expando object
            /// </summary>
            internal readonly ExpandoClass Class;

            /// <summary>
            /// data stored in the expando object, key names are stored in the class.
            ///
            /// Expando._data must be locked when mutating the value.  Otherwise a copy of it
            /// could be made and lose values.
            /// </summary>
            private readonly object[] _dataArray;

            /// <summary>
            /// Indexer for getting/setting the data
            /// </summary>
            internal object this[int index]
            {
                get
                {
                    return _dataArray[index];
                }
                set
                {
                    //when the array is updated, version increases, even the new value is the same
                    //as previous. Dictionary type has the same behavior.
                    _version++;
                    _dataArray[index] = value;
                }
            }

            internal int Version => _version;

            internal int Length => _dataArray.Length;

            /// <summary>
            /// Constructs an empty ExpandoData object with the empty class and no data.
            /// </summary>
            private ExpandoData()
            {
                Class = ExpandoClass.Empty;
                _dataArray = Array.Empty<object>();
            }

            /// <summary>
            /// the version of the ExpandoObject that tracks set and delete operations
            /// </summary>
            private int _version;

            /// <summary>
            /// Constructs a new ExpandoData object with the specified class and data.
            /// </summary>
            internal ExpandoData(ExpandoClass klass, object[] data, int version)
            {
                Class = klass;
                _dataArray = data;
                _version = version;
            }

            /// <summary>
            /// Update the associated class and increases the storage for the data array if needed.
            /// </summary>
            internal ExpandoData UpdateClass(ExpandoClass newClass)
            {
                if (_dataArray.Length >= newClass.Keys.Length)
                {
                    // we have extra space in our buffer, just initialize it to Uninitialized.
                    this[newClass.Keys.Length - 1] = ExpandoObject.Uninitialized;
                    return new ExpandoData(newClass, _dataArray, _version);
                }
                else
                {
                    // we've grown too much - we need a new object array
                    int oldLength = _dataArray.Length;
                    object[] arr = new object[GetAlignedSize(newClass.Keys.Length)];
                    Array.Copy(_dataArray, 0, arr, 0, _dataArray.Length);
                    ExpandoData newData = new ExpandoData(newClass, arr, _version);
                    newData[oldLength] = ExpandoObject.Uninitialized;
                    return newData;
                }
            }

            private static int GetAlignedSize(int len)
            {
                // the alignment of the array for storage of values (must be a power of two)
                const int DataArrayAlignment = 8;

                // round up and then mask off lower bits
                return (len + (DataArrayAlignment - 1)) & (~(DataArrayAlignment - 1));
            }
        }

        #endregion

        #region INotifyPropertyChanged

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        #endregion
    }
}

namespace System.Runtime.CompilerServices
{
    //
    // Note: these helpers are kept as simple wrappers so they have a better 
    // chance of being inlined.
    //
    public static partial class RuntimeOps
    {
        /// <summary>
        /// Gets the value of an item in an expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="indexClass">The class of the expando object.</param>
        /// <param name="index">The index of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
        /// <param name="value">The out parameter containing the value of the member.</param>
        /// <returns>True if the member exists in the expando object, otherwise false.</returns>
        [Obsolete("do not use this method", error: true), EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ExpandoTryGetValue(ExpandoObject expando, object indexClass, int index, string name, bool ignoreCase, out object value)
        {
            return expando.TryGetValue(indexClass, index, name, ignoreCase, out value);
        }

        /// <summary>
        /// Sets the value of an item in an expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="indexClass">The class of the expando object.</param>
        /// <param name="index">The index of the member.</param>
        /// <param name="value">The value of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
        /// <returns>
        /// Returns the index for the set member.
        /// </returns>
        [Obsolete("do not use this method", error: true), EditorBrowsable(EditorBrowsableState.Never)]
        public static object ExpandoTrySetValue(ExpandoObject expando, object indexClass, int index, object value, string name, bool ignoreCase)
        {
            expando.TrySetValue(indexClass, index, value, name, ignoreCase, false);
            return value;
        }

        /// <summary>
        /// Deletes the value of an item in an expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="indexClass">The class of the expando object.</param>
        /// <param name="index">The index of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
        /// <returns>true if the item was successfully removed; otherwise, false.</returns>
        [Obsolete("do not use this method", error: true), EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ExpandoTryDeleteValue(ExpandoObject expando, object indexClass, int index, string name, bool ignoreCase)
        {
            return expando.TryDeleteValue(indexClass, index, name, ignoreCase, ExpandoObject.Uninitialized);
        }

        /// <summary>
        /// Checks the version of the expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="version">The version to check.</param>
        /// <returns>true if the version is equal; otherwise, false.</returns>
        [Obsolete("do not use this method", error: true), EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ExpandoCheckVersion(ExpandoObject expando, object version)
        {
            return expando.Class == version;
        }

        /// <summary>
        /// Promotes an expando object from one class to a new class.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="oldClass">The old class of the expando object.</param>
        /// <param name="newClass">The new class of the expando object.</param>
        [Obsolete("do not use this method", error: true), EditorBrowsable(EditorBrowsableState.Never)]
        public static void ExpandoPromoteClass(ExpandoObject expando, object oldClass, object newClass)
        {
            expando.PromoteClass(oldClass, newClass);
        }
    }
}
