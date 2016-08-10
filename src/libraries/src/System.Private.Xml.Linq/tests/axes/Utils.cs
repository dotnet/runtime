// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml.Linq.Tests
{
    public static class Utils
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
    }
}
