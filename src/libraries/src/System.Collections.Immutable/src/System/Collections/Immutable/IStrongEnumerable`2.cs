// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An interface that must be implemented by collections that want to avoid
    /// boxing their own enumerators when using the 
    /// <see cref="ImmutableExtensions.GetEnumerableDisposable{T, TEnumerator}(IEnumerable{T})"/>
    /// method.
    /// </summary>
    /// <typeparam name="T">The type of value to be enumerated.</typeparam>
    /// <typeparam name="TEnumerator">The type of the enumerator struct.</typeparam>
    internal interface IStrongEnumerable<out T, TEnumerator>
        where TEnumerator : struct, IStrongEnumerator<T>
    {
        /// <summary>
        /// Gets the strongly-typed enumerator.
        /// </summary>
        /// <returns></returns>
        TEnumerator GetEnumerator();
    }
}
