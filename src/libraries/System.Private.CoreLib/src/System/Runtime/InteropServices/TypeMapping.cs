// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Entry type for interop type mapping logic.
    /// </summary>
    public static class TypeMapping
    {
        /// <summary>
        /// Returns the External type type map generated for the current application.
        /// </summary>
        /// <typeparam name="TTypeMapGroup">Type map group</typeparam>
        /// <returns>Requested type map</returns>
        /// <remarks>
        /// Call sites are recognized by the Trimmer to preserve matching type-map entries.
        /// </remarks>
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public static IReadOnlyDictionary<string, Type> GetOrCreateExternalTypeMapping<TTypeMapGroup>()
        {
#if MONO
            return MonoExternalTypeMap<TTypeMapGroup>.s_instance.Value;
#else
            return TypeMapLazyDictionary.CreateExternalTypeMap((RuntimeType)typeof(TTypeMapGroup));
#endif
        }

        /// <summary>
        /// Returns the associated type type map generated for the current application.
        /// </summary>
        /// <typeparam name="TTypeMapGroup">Type map group</typeparam>
        /// <returns>Requested type map</returns>
        /// <remarks>
        /// Call sites are recognized by the Trimmer to preserve matching type-map entries.
        /// </remarks>
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public static IReadOnlyDictionary<Type, Type> GetOrCreateProxyTypeMapping<TTypeMapGroup>()
        {
#if MONO
            throw new NotSupportedException();
#else
            return TypeMapLazyDictionary.CreateProxyTypeMap((RuntimeType)typeof(TTypeMapGroup));
#endif
        }

#if MONO
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        private sealed class MonoExternalTypeMap<TTypeMapGroup> : IReadOnlyDictionary<string, Type>
        {
            internal static readonly Lazy<MonoExternalTypeMap<TTypeMapGroup>> s_instance = new(Create);
            private readonly Dictionary<string, Type> _entries = new(StringComparer.Ordinal);

            private static MonoExternalTypeMap<TTypeMapGroup> Create()
            {
                Assembly? entryAssembly = AppContext.GetData("System.Runtime.InteropServices.TypeMappingEntryAssembly") is string name
                    ? Assembly.Load(name)
                    : Assembly.GetEntryAssembly();
                if (entryAssembly is null)
                    throw new InvalidOperationException(SR.InvalidOperation_TypeMapMissingEntryAssembly);

                var map = new MonoExternalTypeMap<TTypeMapGroup>();
                var pending = new List<Assembly> { entryAssembly };
                var visited = new HashSet<Assembly>();
                for (int index = 0; index < pending.Count; index++)
                {
                    Assembly assembly = pending[index];
                    if (!visited.Add(assembly))
                        continue;

                    // Attribute constructors are not retained by trimming; read their metadata instead.
                    foreach (CustomAttributeData attribute in assembly.GetCustomAttributesData())
                    {
                        if (attribute.AttributeType == typeof(TypeMapAttribute<TTypeMapGroup>))
                        {
                            // Conditional entries can reference trim targets that no longer exist.
                            if (attribute.Constructor.GetParameters().Length != 2)
                                throw new PlatformNotSupportedException();

                            map._entries.Add((string)attribute.ConstructorArguments[0].Value!,
                                (Type)attribute.ConstructorArguments[1].Value!);
                        }
                        else if (attribute.AttributeType == typeof(TypeMapAssemblyTargetAttribute<TTypeMapGroup>))
                        {
                            pending.Add(Assembly.Load((string)attribute.ConstructorArguments[0].Value!));
                        }
                    }
                }

                return map;
            }

            public Type this[string key] => _entries[key];
            public bool TryGetValue(string key, [MaybeNullWhen(false)] out Type value) => _entries.TryGetValue(key, out value);

            // Enumeration would expose entries that the trimmer treats as conditional.
            public IEnumerable<string> Keys => throw new NotSupportedException();
            public IEnumerable<Type> Values => throw new NotSupportedException();
            public int Count => throw new NotSupportedException();
            public bool ContainsKey(string key) => throw new NotSupportedException();
            public IEnumerator<KeyValuePair<string, Type>> GetEnumerator() => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
        }
#endif
    }
}
