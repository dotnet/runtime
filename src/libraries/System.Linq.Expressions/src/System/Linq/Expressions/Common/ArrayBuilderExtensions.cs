// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Helper extension methods for <see cref="ArrayBuilder{T}"/>.
    /// </summary>
    internal static class ArrayBuilderExtensions
    {
        /// <summary>
        /// Returns a read-only collection wrapper around the array that was built.
        /// </summary>
        public static ReadOnlyCollection<T> ToReadOnly<T>(this ArrayBuilder<T> builder)
        {
            return new TrueReadOnlyCollection<T>(builder.ToArray());
        }
    }
}
