// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Caching.Memory
{
    public partial interface IMemoryCache : System.IDisposable
    {
        Microsoft.Extensions.Caching.Memory.ICacheEntry CreateEntry(object key);
        void Remove(object key);
        bool TryGetValue(object key, out object? value);
    }
}