// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
        /// Call sites are treated as an intrinsic by the Trimmer and implemented inline.
        /// </remarks>
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public static IReadOnlyDictionary<string, Type> GetOrCreateExternalTypeMapping<TTypeMapGroup>()
        {
#if MONO
            throw new NotSupportedException();
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
        /// Call sites are treated as an intrinsic by the Trimmer and implemented inline.
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
