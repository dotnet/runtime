// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Serialization.DataContracts;

namespace System.Runtime.Serialization
{
    internal sealed class ContextAwareDataContractIndex
    {
        private (DataContract? strong, WeakReference<DataContract>? weak)[] _contracts;
        private ConditionalWeakTable<Type, DataContract> _keepAlive;

        public int Length => _contracts.Length;

        public ContextAwareDataContractIndex(int size)
        {
            _contracts = new (DataContract?, WeakReference<DataContract>?)[size];
            _keepAlive = new ConditionalWeakTable<Type, DataContract>();
        }

        public DataContract? GetItem(int index) => _contracts[index].strong ?? (_contracts[index].weak?.TryGetTarget(out DataContract? ret) == true ? ret : null);

        public void SetItem(int index, DataContract dataContract)
        {
            // Check for unloadability to decide how to store the value
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(dataContract.UnderlyingType.Assembly);
            if (alc == null || !alc.IsCollectible)
            {
                _contracts[index].strong = dataContract;
            }
            else
            {
                _contracts[index].weak = new WeakReference<DataContract>(dataContract);
                _keepAlive.Add(dataContract.UnderlyingType, dataContract);
            }
        }

        public void Resize(int newSize)
        {
            Array.Resize<(DataContract?, WeakReference<DataContract>?)>(ref _contracts, newSize);
        }
    }

    internal sealed class ContextAwareDictionary<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue>
        where TKey : Type
        where TValue : class?
    {
        private readonly ConcurrentDictionary<TKey, TValue> _fastDictionary = new();
        private readonly ConditionalWeakTable<TKey, TValue> _collectibleTable = new();


        internal TValue GetOrAdd(TKey t, Func<TKey, TValue> f)
        {
            TValue? ret;

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
                // The create delegate here could be quite expensive. ConcurrentDictionary semantics would let use
                // do this without a lock and not corrupt the dictionary, but the delegate still might be called multiple
                // times. So we use a lock to ensure the delegate is only called once. Keep the lock off the hot path.
                if (!_fastDictionary.TryGetValue(t, out ret))
                {
                    lock (_fastDictionary)
                    {
                        return _fastDictionary.GetOrAdd(t, f);
                    }
                }
            }

            // Collectible load contexts should use the ConditionalWeakTable so they can be unloaded
            else
            {
                if (!_collectibleTable.TryGetValue(t, out ret))
                {
                    lock (_collectibleTable)
                    {
                        return _collectibleTable.GetValue(t, k => f(k));
                    }
                }
            }

            return ret;
        }
    }
}
