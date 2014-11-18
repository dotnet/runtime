// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public static class Helpers
    {
        public static bool EqualsAll<T1, T2>(this IEnumerable<T1> source, IEnumerable<T2> target, Func<T1, T2, bool> comparer)
        {
            using (IEnumerator<T1> e1 = source.GetEnumerator())
            using (IEnumerator<T2> e2 = target.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (e2.MoveNext())
                    {
                        if (!comparer(e1.Current, e2.Current))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                if (e2.MoveNext())
                    return false;
            }
            return true;
        }

        public static XAttributeEqualityComparer<XAttribute> MyAttributeComparer = new XAttributeEqualityComparer<XAttribute>();
    }

    public class XAttributeEqualityComparer<T> : IEqualityComparer, IEqualityComparer<T> where T : XAttribute
    {
        public static XAttributeEqualityComparer<T> GetInstance()
        {
            return new XAttributeEqualityComparer<T>();
        }

        public bool Equals(T n1, T n2)
        {
            Assert.True(n1 != null && n2 != null && n1.Value != null && n2.Value != null);
            return n1.Name.Equals(n2.Name) && n1.Value.Equals(n2.Value);
        }

        public int GetHashCode(T attr)
        {
            Assert.True(attr != null && attr.Value != null);
            return attr.Value.GetHashCode() ^ attr.Name.GetHashCode();
        }

        bool IEqualityComparer.Equals(object n1, object n2)
        {
            return Equals((T)n1, (T)n2);
        }

        int IEqualityComparer.GetHashCode(object n)
        {
            return GetHashCode((T)n);
        }
    }
}
