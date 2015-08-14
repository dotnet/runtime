// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net.Cache
{
    public enum HttpCacheAgeControl
    {
        MaxAge = 2,
        MaxAgeAndMaxStale = 6,
        MaxAgeAndMinFresh = 3,
        MaxStale = 4,
        MinFresh = 1,
        None = 0,
    }
    public enum HttpRequestCacheLevel
    {
        BypassCache = 1,
        CacheIfAvailable = 3,
        CacheOnly = 2,
        CacheOrNextCacheOnly = 7,
        Default = 0,
        NoCacheNoStore = 6,
        Refresh = 8,
        Reload = 5,
        Revalidate = 4,
    }
    public partial class HttpRequestCachePolicy : System.Net.Cache.RequestCachePolicy
    {
        public HttpRequestCachePolicy() { }
        public HttpRequestCachePolicy(System.DateTime cacheSyncDate) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan ageOrFreshOrStale) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan maxAge, System.TimeSpan freshOrStale) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan maxAge, System.TimeSpan freshOrStale, System.DateTime cacheSyncDate) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel level) { }
        public System.DateTime CacheSyncDate { get { return default(System.DateTime); } }
        public new System.Net.Cache.HttpRequestCacheLevel Level { get { return default(System.Net.Cache.HttpRequestCacheLevel); } }
        public System.TimeSpan MaxAge { get { return default(System.TimeSpan); } }
        public System.TimeSpan MaxStale { get { return default(System.TimeSpan); } }
        public System.TimeSpan MinFresh { get { return default(System.TimeSpan); } }
        public override string ToString() { return default(string); }
    }
    public enum RequestCacheLevel
    {
        BypassCache = 1,
        CacheIfAvailable = 3,
        CacheOnly = 2,
        Default = 0,
        NoCacheNoStore = 6,
        Reload = 5,
        Revalidate = 4,
    }
    public partial class RequestCachePolicy
    {
        public RequestCachePolicy() { }
        public RequestCachePolicy(System.Net.Cache.RequestCacheLevel level) { }
        public System.Net.Cache.RequestCacheLevel Level { get { return default(System.Net.Cache.RequestCacheLevel); } }
        public override string ToString() { return default(string); }
    }
}
