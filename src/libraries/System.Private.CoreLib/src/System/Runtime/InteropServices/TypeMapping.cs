// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Entry type for interop type mapping logic.
    /// </summary>
    public static class TypeMapping
    {
#if !NATIVEAOT
        private static Lock s_lock = new Lock();
        private static Dictionary<Type, TypeMapLazyDictionary> s_externalTypeMapsByGroup = new Dictionary<Type, TypeMapLazyDictionary>();

        [RequiresUnreferencedCode("Lazy TypeMap isn't supported in the Trimmer")]
        private static TypeMapLazyDictionary CreateOrGet(RuntimeType typeGroup)
        {
            Debug.Assert(!typeGroup.IsGenericType, "Type group should not be generic");

            TypeMapLazyDictionary? typeMaps;
            lock (s_lock)
            {
                if (s_externalTypeMapsByGroup.TryGetValue(typeGroup, out typeMaps))
                {
                    return typeMaps;
                }
            }

            RuntimeAssembly? entry = (RuntimeAssembly?)Assembly.GetEntryAssembly();
            if (entry is null)
            {
                // [TODO] Do we throw here?
                throw new NotSupportedException();
            }

            // We create a new type map starting from the entry assembly.
            // This is done outside of the lock since it could be expensive.
            TypeMapLazyDictionary newTypeMaps = new(typeGroup, entry);
            lock (s_lock)
            {
                if (!s_externalTypeMapsByGroup.TryGetValue(typeGroup, out typeMaps))
                {
                    s_externalTypeMapsByGroup.Add(typeGroup, newTypeMaps);
                    typeMaps = newTypeMaps;
                }

                return typeMaps;
            }
        }
#endif

        /// <summary>
        /// Returns the External type type map generated for the current application.
        /// </summary>
        /// <typeparam name="TTypeMapGroup">Type map group</typeparam>
        /// <returns>Requested type map</returns>
        /// <remarks>
        /// Call sites are treated as an intrinsic by the Trimmer and implemented inline.
        /// </remarks>
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public static IReadOnlyDictionary<string, Type> GetOrCreateExternalTypeMapping<TTypeMapGroup>()
        {
#if NATIVEAOT
            throw new NotImplementedException();
#else
            return CreateOrGet((RuntimeType)typeof(TTypeMapGroup)).GetExternalTypeMap();
#endif
        }

        /// <summary>
        /// Returns the associated type type map generated for the current application.
        /// </summary>
        /// <typeparam name="TTypeMapGroup">Type map group</typeparam>
        /// <returns>Requested type map</returns>
        /// <remarks>
        /// Call sites are treated as an intrinsic by the Trimmer and implemented inline.
        /// </remarks>
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public static IReadOnlyDictionary<Type, Type> GetOrCreateProxyTypeMapping<TTypeMapGroup>()
        {
#if NATIVEAOT
            throw new NotImplementedException();
#else
            return CreateOrGet((RuntimeType)typeof(TTypeMapGroup)).GetProxyTypeMap();
#endif
        }
    }
}
