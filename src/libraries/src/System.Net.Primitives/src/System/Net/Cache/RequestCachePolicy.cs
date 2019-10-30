// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Cache
{
    public class RequestCachePolicy
    {
        public RequestCachePolicy()
        {
            Level = RequestCacheLevel.Default;
        }

        public RequestCachePolicy(RequestCacheLevel level)
        {
            if (level < RequestCacheLevel.Default || level > RequestCacheLevel.NoCacheNoStore)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            Level = level;
        }

        public RequestCacheLevel Level { get; }

        public override string ToString() => "Level:" + Level.ToString();
    }
}
