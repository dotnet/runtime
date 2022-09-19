// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Contains utilities and combinators acting on <see cref="IJsonTypeInfoResolver"/>.
    /// </summary>
    public static class JsonTypeInfoResolver
    {
        /// <summary>
        /// Combines multiple <see cref="IJsonTypeInfoResolver"/> sources into one.
        /// </summary>
        /// <param name="resolvers">Sequence of contract resolvers to be queried for metadata.</param>
        /// <returns>A <see cref="IJsonTypeInfoResolver"/> combining results from <paramref name="resolvers"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="resolvers"/> or any of its elements is null.</exception>
        /// <remarks>
        /// The combined resolver will query each of <paramref name="resolvers"/> in the specified order,
        /// returning the first result that is non-null. If all <paramref name="resolvers"/> return null,
        /// then the combined resolver will also return <see langword="null"/>.
        ///
        /// Can be used to combine multiple <see cref="JsonSerializerContext"/> sources,
        /// which typically define contract metadata for small subsets of types.
        /// It can also be used to fall back to <see cref="DefaultJsonTypeInfoResolver"/> wherever necessary.
        /// </remarks>
        public static IJsonTypeInfoResolver Combine(params IJsonTypeInfoResolver[] resolvers)
        {
            if (resolvers == null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            foreach (IJsonTypeInfoResolver? resolver in resolvers)
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
