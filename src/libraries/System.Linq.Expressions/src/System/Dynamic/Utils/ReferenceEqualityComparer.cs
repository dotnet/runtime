// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Dynamic.Utils
{
    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    {
        internal static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

        private ReferenceEqualityComparer() { }

        public bool Equals(T x, T y)
        {
            return object.ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
