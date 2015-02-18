// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


/*============================================================
**
** Class:  StringBuilderCache
**
** Purpose: provide a cached reusable instance of stringbuilder
**          per thread  it's an optimization that reduces the 
**          number of instances constructed and collected.
**
**  Acquire - is used to get a string builder to use of a 
**            particular size.  It can be called any number of 
**            times, if a stringbuilder is in the cache then
**            it will be returned and the cache emptied.
**            subsequent calls will return a new stringbuilder.
**
**            A StringBuilder instance is cached in 
**            Thread Local Storage and so there is one per thread
**
**  Release - Place the specified builder in the cache if it is 
**            not too big.
**            The stringbuilder should not be used after it has 
**            been released.
**            Unbalanced Releases are perfectly acceptable.  It
**            will merely cause the runtime to create a new 
**            stringbuilder next time Acquire is called.
**
**  GetStringAndRelease
**          - ToString() the stringbuilder, Release it to the 
**            cache and return the resulting string
**
===========================================================*/

using System.Threading;
using System.Text;

namespace System.IO
{
    internal static class StringBuilderCache
    {
        private const int MAX_BUILDER_SIZE = 260;
        private const int DEFAULT_CAPACITY = 16;

        [ThreadStatic]
        private static StringBuilder t_cachedInstance;

        public static StringBuilder Acquire(int capacity = DEFAULT_CAPACITY)
        {
            if (capacity <= MAX_BUILDER_SIZE)
            {
                StringBuilder sb = StringBuilderCache.t_cachedInstance;
                if (sb != null)
                {
                    // Avoid stringbuilder block fragmentation by getting a new StringBuilder
                    // when the requested size is larger than the current capacity
                    if (capacity <= sb.Capacity)
                    {
                        StringBuilderCache.t_cachedInstance = null;
                        sb.Clear();
                        return sb;
                    }
                }
            }
            return new StringBuilder(capacity);
        }

        public static StringBuilder Acquire(string value)
        {
            StringBuilder sb = Acquire(Math.Max(value.Length, DEFAULT_CAPACITY));
            sb.Append(value);
            return sb;
        }

        public static void Release(StringBuilder sb)
        {
            if (sb.Capacity <= MAX_BUILDER_SIZE)
            {
                StringBuilderCache.t_cachedInstance = sb;
            }
        }

        public static string GetStringAndRelease(StringBuilder sb)
        {
            string result = sb.ToString();
            Release(sb);
            return result;
        }
    }
}
