// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Internal.LowLevelLinq
{
    internal static partial class LowLevelEnumerable
    {
        public static List<T> ToList<T>(this IEnumerable<T> source)
        {
            List<T> result;

            var collection = source as ICollection<T>;
            if (collection != null)
                result = new List<T>(collection.Count);
            else
                result = new List<T>();

            foreach (var element in source)
                result.Add(element);

            return result;
        }
    }
}
