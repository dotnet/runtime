// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace System.Xml.Serialization
{
    internal sealed class ContextAwareTables<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> where T : class?
    {
        private readonly Hashtable _defaultTable;
        private readonly ConditionalWeakTable<Type, T> _collectibleTable;
        // Used for the "inverted" scenario: the type being serialized is in the default ALC but
        // the serializer is created from within a collectible ALC. Keyed by the first assembly
        // from the caller's collectible ALC so entries are GC'd when that ALC is unloaded.
        private readonly ConditionalWeakTable<Assembly, Hashtable> _collectibleAlcTable;

        public ContextAwareTables()
        {
            _defaultTable = new Hashtable();
            _collectibleTable = new ConditionalWeakTable<Type, T>();
            _collectibleAlcTable = new ConditionalWeakTable<Assembly, Hashtable>();
        }

        // Returns the first assembly from the current contextual reflection context's collectible
        // ALC to use as a stable weak key for the inverted caching scenario.
        private static Assembly? GetInvertedAlcKeyAssembly(Type t)
        {
            AssemblyLoadContext? typeAlc = AssemblyLoadContext.GetLoadContext(t.Assembly);
            if (typeAlc?.IsCollectible == true)
                return null; // Direct case: handled by _collectibleTable keyed on the type

            AssemblyLoadContext? currentAlc = AssemblyLoadContext.CurrentContextualReflectionContext;
            if (currentAlc?.IsCollectible == true)
            {
                foreach (Assembly a in currentAlc.Assemblies)
                    return a;
            }
            return null;
        }

        internal T GetOrCreateValue(Type t, Func<Type, T> f)
        {
            // The fast and most common default case
            T? ret = (T?)_defaultTable[t];
            if (ret != null)
                return ret;

            // Common case for collectible types (type itself is in a collectible ALC)
            if (_collectibleTable.TryGetValue(t, out ret))
                return ret;

            // Inverted scenario: type is in the default ALC but caller is in a collectible ALC
            Assembly? alcKeyAssembly = GetInvertedAlcKeyAssembly(t);
            if (alcKeyAssembly != null && _collectibleAlcTable.TryGetValue(alcKeyAssembly, out Hashtable? alcTable))
            {
                ret = (T?)alcTable[t];
                if (ret != null)
                    return ret;
            }

            // Not found. Do the slower work of creating the value in the correct collection.
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(t.Assembly);

            if (alc?.IsCollectible == true)
            {
                // Type is in a collectible ALC — use the ConditionalWeakTable so entries can be
                // collected when the ALC is unloaded (keyed on the type itself)
                lock (_collectibleTable)
                {
                    if (!_collectibleTable.TryGetValue(t, out ret))
                    {
                        ret = f(t);
                        _collectibleTable.AddOrUpdate(t, ret);
                    }
                }
            }
            else if (alcKeyAssembly != null)
            {
                // Inverted scenario: type appears to be in the default ALC but the caller is in a
                // collectible ALC. Use a per-ALC hashtable so entries are GC'd when the ALC unloads.
                lock (_collectibleAlcTable)
                {
                    if (!_collectibleAlcTable.TryGetValue(alcKeyAssembly, out alcTable))
                    {
                        alcTable = new Hashtable();
                        _collectibleAlcTable.Add(alcKeyAssembly, alcTable);
                    }

                    lock (alcTable)
                    {
                        if ((ret = (T?)alcTable[t]) == null)
                        {
                            ret = f(t);
                            alcTable[t] = ret;
                        }
                    }
                }
            }
            else
            {
                // Null and non-collectible load contexts use the default table
                lock (_defaultTable)
                {
                    if ((ret = (T?)_defaultTable[t]) == null)
                    {
                        ret = f(t);
                        _defaultTable[t] = ret;
                    }
                }
            }

            return ret;
        }
    }
}
