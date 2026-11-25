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

        /// <summary>
        /// Tries to get the value that the type mapped by a key in the external type map of a group provides.
        /// </summary>
        /// <typeparam name="TTypeMapGroup">The type map group.</typeparam>
        /// <typeparam name="TValue">The type of the value. The mapped type has to implement <see cref="ITypeMapValue{TValue}"/> for it.</typeparam>
        /// <param name="key">The key of the external type map entry.</param>
        /// <param name="value">When this method returns, contains the value provided by the mapped type if the key is found and the mapped type implements <see cref="ITypeMapValue{TValue}"/>; otherwise, the default value of <typeparamref name="TValue"/>. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true" /> if the key is in the map and the mapped type provides a <typeparamref name="TValue"/>; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Trimming and AOT compilation treat calls to this method as an intrinsic, so <typeparamref name="TTypeMapGroup"/> and <typeparamref name="TValue"/> have to be known statically (they can't be, or contain, generic parameters).
        /// Unlike <see cref="GetOrCreateExternalTypeMapping{TTypeMapGroup}"/>, a call does not make the mapped types visible to reflection when the application is compiled ahead of time.
        /// </remarks>
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public static bool TryGetValue<TTypeMapGroup, TValue>(string key, [MaybeNullWhen(false)] out TValue value)
        {
#if MONO
            throw new NotSupportedException();
#else
            ArgumentNullException.ThrowIfNull(key);
            return TypeMapLazyDictionary.TryGetValue((RuntimeType)typeof(TTypeMapGroup), key, out value);
#endif
        }

        /// <summary>
        /// Tries to get the value that the type mapped by a key in the proxy type map of a group provides.
        /// </summary>
        /// <typeparam name="TTypeMapGroup">The type map group.</typeparam>
        /// <typeparam name="TValue">The type of the value. The mapped type has to implement <see cref="ITypeMapValue{TValue}"/> for it.</typeparam>
        /// <param name="key">The key of the proxy type map entry.</param>
        /// <param name="value">When this method returns, contains the value provided by the mapped type if the key is found and the mapped type implements <see cref="ITypeMapValue{TValue}"/>; otherwise, the default value of <typeparamref name="TValue"/>. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true" /> if the key is in the map and the mapped type provides a <typeparamref name="TValue"/>; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Trimming and AOT compilation treat calls to this method as an intrinsic, so <typeparamref name="TTypeMapGroup"/> and <typeparamref name="TValue"/> have to be known statically (they can't be, or contain, generic parameters).
        /// Unlike <see cref="GetOrCreateProxyTypeMapping{TTypeMapGroup}"/>, a call does not make the mapped types visible to reflection when the application is compiled ahead of time.
        /// </remarks>
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public static bool TryGetValue<TTypeMapGroup, TValue>(Type key, [MaybeNullWhen(false)] out TValue value)
        {
#if MONO
            throw new NotSupportedException();
#else
            ArgumentNullException.ThrowIfNull(key);
            return TypeMapLazyDictionary.TryGetValue((RuntimeType)typeof(TTypeMapGroup), key, out value);
#endif
        }
    }
}
