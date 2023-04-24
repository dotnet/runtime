// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal readonly struct ServiceCacheKey : IEquatable<ServiceCacheKey>
    {
        public static ServiceCacheKey Empty { get; } = new ServiceCacheKey(null, 0);

        /// <summary>
        /// Type of service being cached
        /// </summary>
        public Type? Type { get; }

        /// <summary>
        /// Reverse index of the service when resolved in <c>IEnumerable&lt;Type&gt;</c> where default instance gets slot 0.
        /// For example for service collection
        ///  IService Impl1
        ///  IService Impl2
        ///  IService Impl3
        /// We would get the following cache keys:
        ///  Impl1 2
        ///  Impl2 1
        ///  Impl3 0
        /// </summary>
        public int Slot { get; }

        public ServiceCacheKey(Type? type, int slot)
        {
            Type = type;
            Slot = slot;
        }

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public bool Equals(ServiceCacheKey other) =>
            Type == other.Type && Slot == other.Slot;

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is ServiceCacheKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type?.GetHashCode() ?? 23) * 397) ^ Slot;
            }
        }
    }
}
