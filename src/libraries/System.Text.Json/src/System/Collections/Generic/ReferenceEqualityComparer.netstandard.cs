// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Passed to the <see cref="DefaultReferenceResolver._objectToReferenceIdMap"/> meant for serialization.
    /// It forces the dictionary to do a ReferenceEquals comparison when comparing the TKey object.
    /// </summary>
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>, IEqualityComparer
    {
        private ReferenceEqualityComparer() { }

        public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
