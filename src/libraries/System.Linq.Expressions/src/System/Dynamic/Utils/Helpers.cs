// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Dynamic.Utils
{
    // Miscellaneous helpers that don't belong anywhere else
    internal static class Helpers
    {
        internal static T CommonNode<T>(T first, T second, Func<T, T> parent) where T : class
        {
            EqualityComparer<T> cmp = EqualityComparer<T>.Default;
            if (cmp.Equals(first, second))
            {
                return first;
            }
            var set = new HashSet<T>(cmp);
            for (T t = first; t != null; t = parent(t))
            {
                set.Add(t);
            }
            for (T t = second; t != null; t = parent(t))
            {
                if (set.Contains(t))
                {
                    return t;
                }
            }
            return null;
        }

        internal static void IncrementCount<T>(T key, Dictionary<T, int> dict)
        {
            int count;
            dict.TryGetValue(key, out count);
            dict[key] = count + 1;
        }
    }
}
