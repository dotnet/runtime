// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Immutable.Test
{
    /// <summary>
    /// An equality comparer that considers all values to be equal.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class EverythingEqual<T> : IEqualityComparer<T>, IEqualityComparer
    {
        private static EverythingEqual<T> singleton = new EverythingEqual<T>();

        private EverythingEqual() { }

        internal static EverythingEqual<T> Default
        {
            get
            {
                return singleton;
            }
        }

        public bool Equals(T x, T y)
        {
            return true;
        }

        public int GetHashCode(T obj)
        {
            return 1;
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return true;
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return 1;
        }
    }
}
