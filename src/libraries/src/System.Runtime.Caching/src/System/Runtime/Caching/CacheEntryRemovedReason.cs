// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.Caching
{
    public enum CacheEntryRemovedReason
    {
        Removed = 0, //Explicitly removed via API call
        Expired,
        Evicted,     //Evicted to free up space
        ChangeMonitorChanged,  //An associated programmatic dependency triggered eviction
        CacheSpecificEviction  //Catch-all for custom providers
    }
}
