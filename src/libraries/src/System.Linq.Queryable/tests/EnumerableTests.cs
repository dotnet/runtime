// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;

namespace System.Linq.Tests
{
    public abstract class EnumerableBasedTests
    {
        protected class AnagramEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null | y == null) return false;
                int length = x.Length;
                if (length != y.Length) return false;
                using (var en = x.OrderBy(i => i).GetEnumerator())
                {
                    foreach (char c in y.OrderBy(i => i))
                    {
                        en.MoveNext();
                        if (c != en.Current) return false;
                    }
                }
                return true;
            }

            public int GetHashCode(string obj)
            {
                if (obj == null) return 0;
                int hash = obj.Length;
                foreach (char c in obj)
                    hash ^= (int)c;
                return hash;
            }
        }

        protected struct StringWithIntArray
        {
            public string name { get; set; }
            public int?[] total { get; set; }
        }
    }
}
