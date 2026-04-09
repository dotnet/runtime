// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Runtime.Caching
{
    public abstract class CacheEntryChangeMonitor : ChangeMonitor
    {
        public abstract ReadOnlyCollection<string> CacheKeys { get; }
        public abstract DateTimeOffset LastModified { get; }
        public abstract string RegionName { get; }
    }
}
