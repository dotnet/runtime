// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    /// <summary>
    /// Implemented by an <see cref="IEqualityComparer{T}"/> to support comparing
    /// a <typeparamref name="TAlternate"/> instance with a <typeparamref name="T"/> instance.
    /// </summary>
    /// <typeparam name="TAlternate">The alternate type to compare.</typeparam>
    /// <typeparam name="T">The type to compare.</typeparam>
    public interface IAlternateEqualityComparer<in TAlternate, T>
        where TAlternate : allows ref struct
        where T : allows ref struct
    {
        /// <summary>Determines whether the specified <paramref name="alternate"/> equals the specified <paramref name="other"/>.</summary>
        /// <param name="alternate">The instance of type <typeparamref name="TAlternate"/> to compare.</param>
        /// <param name="other">The instance of type <typeparamref name="T"/> to compare.</param>
        /// <returns><see langword="true"/> if the specified instances are equal; otherwise, <see langword="false"/>.</returns>
        bool Equals(TAlternate alternate, T other);

        /// <summary>Returns a hash code for the specified alternate instance.</summary>
        /// <param name="alternate">The instance of type <typeparamref name="TAlternate"/> for which to get a hash code.</param>
        /// <returns>A hash code for the specified instance.</returns>
        /// <remarks>
        /// This interface is intended to be implemented on a type that also implements <see cref="IEqualityComparer{T}"/>.
        /// The result of this method should return the same hash code as would invoking the <see cref="IEqualityComparer{T}.GetHashCode"/>
        /// method on any <typeparamref name="T"/> for which <see cref="Equals(TAlternate, T)"/>
        /// returns <see langword="true"/>.
        /// </remarks>
        int GetHashCode(TAlternate alternate);

        /// <summary>
        /// Creates a <typeparamref name="T"/> that is considered by <see cref="Equals(TAlternate, T)"/> to be equal
        /// to the specified <paramref name="alternate"/>.
        /// </summary>
        /// <param name="alternate">The instance of type <typeparamref name="TAlternate"/> for which an equal <typeparamref name="T"/> is required.</param>
        /// <returns>A <typeparamref name="T"/> considered equal to the specified <paramref name="alternate"/>.</returns>
        T Create(TAlternate alternate);
    }
}
