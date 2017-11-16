// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;

namespace System.Runtime.Caching
{
    public class CacheItemPolicy
    {
        private DateTimeOffset _absExpiry;
        private TimeSpan _sldExpiry;
        private Collection<ChangeMonitor> _changeMonitors;
        private CacheItemPriority _priority;
        private CacheEntryRemovedCallback _removedCallback;
        private CacheEntryUpdateCallback _updateCallback;

        public DateTimeOffset AbsoluteExpiration
        {
            get { return _absExpiry; }
            set { _absExpiry = value; }
        }

        public Collection<ChangeMonitor> ChangeMonitors
        {
            get
            {
                if (_changeMonitors == null)
                {
                    _changeMonitors = new Collection<ChangeMonitor>();
                }
                return _changeMonitors;
            }
        }

        public CacheItemPriority Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public CacheEntryRemovedCallback RemovedCallback
        {
            get { return _removedCallback; }
            set { _removedCallback = value; }
        }

        public TimeSpan SlidingExpiration
        {
            get { return _sldExpiry; }
            set { _sldExpiry = value; }
        }

        public CacheEntryUpdateCallback UpdateCallback
        {
            get { return _updateCallback; }
            set { _updateCallback = value; }
        }

        public CacheItemPolicy()
        {
            _absExpiry = ObjectCache.InfiniteAbsoluteExpiration;
            _sldExpiry = ObjectCache.NoSlidingExpiration;
            _priority = CacheItemPriority.Default;
        }
    }
}
