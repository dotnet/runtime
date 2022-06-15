// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ArgumentOutOfRangeException.ThrowIf(level < RequestCacheLevel.Default || level > RequestCacheLevel.NoCacheNoStore);

            Level = level;
        }

        public RequestCacheLevel Level { get; }

        public override string ToString() => "Level:" + Level.ToString();
    }
}
