// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;

namespace System.Collections.Generic
{
    /// <summary>
    /// Internal helper functions for working with enumerables.
    /// </summary>
    internal static partial class EnumerableHelpers
    {
        /// <summary>Converts an enumerable to an array.</summary>
        /// <param name="source">The enumerable to convert.</param>
        /// <returns>The resulting array.</returns>
        internal static T[] ToArray<T>(IEnumerable<T> source) => ToArrayEnumerable<T>.Instance.ToArray(source);
    }
}
