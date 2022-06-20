// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Contains utilities for IJsonTypeInfoResolver
    /// </summary>
    public static class JsonTypeInfoResolver
    {
        /// <summary>
        /// Combines multiple IJsonTypeInfoResolvers
        /// </summary>
        /// <param name="resolvers"></param>
        /// <returns></returns>
        /// <remarks>
        /// All resolvers except last one should return null when they do not know how to create JsonTypeInfo for a given type.
        /// Last resolver on the list should return non-null for most of the types unless explicit type blocking is desired.
        /// </remarks>
        public static IJsonTypeInfoResolver Combine(params IJsonTypeInfoResolver[] resolvers)
        {
            if (resolvers == null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            foreach (var resolver in resolvers)
            {
                if (resolver == null)
                {
                    throw new ArgumentNullException(nameof(resolvers), SR.CombineOneOfResolversIsNull);
                }
            }

            return new CombiningJsonTypeInfoResolver(resolvers);
        }

        private sealed class CombiningJsonTypeInfoResolver : IJsonTypeInfoResolver
        {
            private readonly IJsonTypeInfoResolver[] _resolvers;

            public CombiningJsonTypeInfoResolver(IJsonTypeInfoResolver[] resolvers)
            {
                _resolvers = resolvers.AsSpan().ToArray();
            }

            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                foreach (IJsonTypeInfoResolver resolver in _resolvers)
                {
                    JsonTypeInfo? typeInfo = resolver.GetTypeInfo(type, options);
                    if (typeInfo != null)
                    {
                        return typeInfo;
                    }
                }

                return null;
            }
        }
    }
}
