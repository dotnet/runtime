// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Threading.RateLimiting
{
    /// <summary>
    /// A strongly-typed name of metadata that can be stored in a <see cref="RateLimitLease"/>.
    /// </summary>
    /// <typeparam name="T">The type the metadata will be.</typeparam>
    public sealed class MetadataName<T> : IEquatable<MetadataName<T>>
    {
        private readonly string _name;

        /// <summary>
        /// Constructs a <see cref="MetadataName{T}"/> object with the given name.
        /// </summary>
        /// <param name="name">The name of the <see cref="MetadataName"/> object.</param>
        public MetadataName(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Gets the name of the metadata.
        /// </summary>
        public string Name => _name;

        /// <inheritdoc/>
        public override string ToString()
        {
            return _name ?? string.Empty;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _name == null ? 0 : _name.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is MetadataName<T> && Equals((MetadataName<T>)obj);
        }

        /// <inheritdoc/>
        public bool Equals(MetadataName<T>? other)
        {
            if (other is null)
            {
                return false;
            }
            // NOTE: intentionally ordinal and case sensitive, matches CNG.
            return _name == other._name;
        }

        /// <summary>
        /// Determines whether two <see cref="MetadataName{T}"/> are equal to each other.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(MetadataName<T> left, MetadataName<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="MetadataName{T}"/> are not equal to each other.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(MetadataName<T> left, MetadataName<T> right)
        {
            return !(left == right);
        }
    }
}
