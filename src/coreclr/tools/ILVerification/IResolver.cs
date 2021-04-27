// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.PortableExecutable;

namespace ILVerify
{
    public interface IResolver
    {
        /// <summary>
        /// This method should return the same instance when queried multiple times.
        /// </summary>
        PEReader Resolve(string simpleName);
    }

    /// <summary>
    /// Provides caching logic for implementations of IResolver
    /// </summary>
    public abstract class ResolverBase : IResolver
    {
        private readonly Dictionary<string, PEReader> _resolverCache = new Dictionary<string, PEReader>();

        public PEReader Resolve(string simpleName)
        {
            if (_resolverCache.TryGetValue(simpleName, out PEReader peReader))
            {
                return peReader;
            }

            PEReader result = ResolveCore(simpleName);
            if (result != null)
            {
                _resolverCache.Add(simpleName, result);
                return result;
            }

            return null;
        }

        protected abstract PEReader ResolveCore(string simpleName);
    }
}
