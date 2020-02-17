// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Passed to the <see cref="DefaultReferenceResolver._objectToReferenceIdMap"/> meant for serialization.
    /// It forces the dictionary to do a ReferenceEquals comparison when comparing the TKey object.
    /// </summary>
    internal sealed class ReferenceEqualsEqualityComparer<T> : IEqualityComparer<T>
    {
        public static ReferenceEqualsEqualityComparer<T> Comparer = new ReferenceEqualsEqualityComparer<T>();

        bool IEqualityComparer<T>.Equals([AllowNull] T x, [AllowNull] T y)
        {
            return ReferenceEquals(x, y);
        }

        int IEqualityComparer<T>.GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj!);
        }
    }
}
