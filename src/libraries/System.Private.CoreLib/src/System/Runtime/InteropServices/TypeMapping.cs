// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
#if CORECLR
        [DynamicDependency(nameof(GetExternalTypeMappingForAssembly) + "``1", typeof(TypeMapping))]
#endif
        public static IReadOnlyDictionary<string, Type> GetOrCreateExternalTypeMapping<TTypeMapGroup>()
        {
#if MONO
            throw new NotSupportedException();
#else
            return TypeMapLazyDictionary.CreateExternalTypeMap((RuntimeType)typeof(TTypeMapGroup));
#endif
        }

#if CORECLR
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        internal static Type? GetExternalTypeMappingForAssembly<TTypeMapGroup>(Assembly assembly, string key)
        {
            IReadOnlyDictionary<string, Type> map = TypeMapLazyDictionary.CreateExternalTypeMap(
                (RuntimeType)typeof(TTypeMapGroup), (RuntimeAssembly)assembly);
            return map.TryGetValue(key, out Type? type) ? type : null;
        }
#endif

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

    }
}
