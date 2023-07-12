// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace System.Runtime.Serialization
{
    internal sealed class ContextAwareIndex<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> where T : class?
    {
        private (T? strong, WeakReference<T>? weak)[] _items;
        private ConditionalWeakTable<Type, T> _keepAlive;

        public int Length => _items.Length;

        public ContextAwareIndex(int size)
        {
            _items = new (T?, WeakReference<T>?)[size];
            _keepAlive = new ConditionalWeakTable<Type, T>();
        }

        public T? GetItem(int index) => _items[index].strong ?? (_items[index].weak?.TryGetTarget(out T? ret) == true ? ret : null);

        public void SetItem(int index, T value, Type keepAliveType)
        {
            // Check for unloadability to decide how to store the value
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(keepAliveType.Assembly);
            if (alc == null || !alc.IsCollectible)
            {
                _items[index].strong = value;
            }
            else
            {
                _items[index].weak = new WeakReference<T>(value);
                _keepAlive.Add(keepAliveType, value);
            }
        }

        public void Resize(int newSize)
        {
            Array.Resize<(T?, WeakReference<T>?)>(ref _items, newSize);
        }
    }

    internal sealed class ContextAwareDictionary<TType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>
        where TType : Type
        where T : class?
    {
        private readonly ConcurrentDictionary<TType, T> _fastDictionary = new();
        private readonly ConditionalWeakTable<TType, T> _collectibleTable = new();


        internal T GetOrAdd(TType t, Func<TType, T> f)
        {
            T? ret;

            // The fast and most common default case
            if (_fastDictionary.TryGetValue(t, out ret))
                return ret;

            // Common case for collectible contexts
            if (_collectibleTable.TryGetValue(t, out ret))
                return ret;

            // Not found. Do the slower work of creating the value in the correct collection.
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(t.Assembly);

            // Null and non-collectible load contexts use the default table
            if (alc == null || !alc.IsCollectible)
            {
                return _fastDictionary.GetOrAdd(t, f);
            }

            // Collectible load contexts should use the ConditionalWeakTable so they can be unloaded
            else
            {
                lock (_collectibleTable)
                {
                    if (!_collectibleTable.TryGetValue(t, out ret))
                    {
                        ret = f(t);
                        _collectibleTable.AddOrUpdate(t, ret);
                    }
                }
            }

            return ret;
        }
    }

    //internal sealed class ContextAwareTables<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T> where T : class?
    //{
    //    private readonly Hashtable _defaultTable;
    //    private readonly ConditionalWeakTable<Type, T> _collectibleTable;

    //    public ContextAwareTables()
    //    {
    //        _defaultTable = new Hashtable();
    //        _collectibleTable = new ConditionalWeakTable<Type, T>();
    //    }

    //    internal T GetOrCreateValue(Type t, Func<Type, T> f)
    //    {
    //        // The fast and most common default case
    //        T? ret = (T?)_defaultTable[t];
    //        if (ret != null)
    //            return ret;

    //        // Common case for collectible contexts
    //        if (_collectibleTable.TryGetValue(t, out ret))
    //            return ret;

    //        // Not found. Do the slower work of creating the value in the correct collection.
    //        AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(t.Assembly);

    //        // Null and non-collectible load contexts use the default table
    //        if (alc == null || !alc.IsCollectible)
    //        {
    //            lock (_defaultTable)
    //            {
    //                if ((ret = (T?)_defaultTable[t]) == null)
    //                {
    //                    ret = f(t);
    //                    _defaultTable[t] = ret;
    //                }
    //            }
    //        }

    //        // Collectible load contexts should use the ConditionalWeakTable so they can be unloaded
    //        else
    //        {
    //            lock (_collectibleTable)
    //            {
    //                if (!_collectibleTable.TryGetValue(t, out ret))
    //                {
    //                    ret = f(t);
    //                    _collectibleTable.AddOrUpdate(t, ret);
    //                }
    //            }
    //        }

    //        return ret;
    //    }
    //}
}
