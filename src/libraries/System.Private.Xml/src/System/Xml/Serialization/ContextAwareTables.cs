// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.Loader;

    internal class ContextAwareTables<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T> where T : class?
    {
        private Hashtable _defaultTable;
        private ConditionalWeakTable<Type, T> _collectibleTable;

        public ContextAwareTables()
        {
            _defaultTable = new Hashtable();
            _collectibleTable = new ConditionalWeakTable<Type, T>();
        }

        internal T GetOrCreateValue(Type t, Func<Type, T> f)
        {
            // The fast and most common default case
            T? ret = (T?)_defaultTable[t];
            if (ret != null)
                return ret;

            // Common case for collectible contexts
            if (_collectibleTable.TryGetValue(t, out ret))
                return ret;

            // Not found. Do the slower work of creating the value in the correct collection.
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(t.Assembly);

            // Null and non-collectible load contexts use the default table
            if (alc == null || !alc.IsCollectible)
            {
                lock (_defaultTable)
                {
                    if ((ret = (T?)_defaultTable[t]) == null)
                    {
                        ret = f(t);
                        _defaultTable[t] = ret;
                    }
                }
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
}
