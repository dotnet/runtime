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

        internal T GetOrCreateValue(Type t, Func<T> f)
        {
            T? ret;
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(t.Assembly);

            // Null and non-collectible load contexts use the default table
            if (alc == null || !alc.IsCollectible)
            {
                if ((ret = (T?)_defaultTable[t]) == null)
                {
                    lock (_defaultTable)
                    {
                        if ((ret = (T?)_defaultTable[t]) == null)
                        {
                            ret = f();
                            _defaultTable[t] = ret;
                        }
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
                        if (!_collectibleTable.TryGetValue(t, out ret))
                        {
                            ret = f();
                            _collectibleTable.AddOrUpdate(t, ret);
                        }
                    }
                }
            }

            return ret;
        }
    }
}
