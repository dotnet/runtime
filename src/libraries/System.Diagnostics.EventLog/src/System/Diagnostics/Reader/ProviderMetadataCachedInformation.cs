// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Collections.Generic;
using Microsoft.Win32;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// This class does not expose underlying Provider metadata objects. Instead it
    /// exposes a limited set of Provider metadata information from the cache. The reason
    /// for this is so the cache can easily Dispose the metadata object without worrying
    /// about who is using it.
    /// </summary>
    internal sealed class ProviderMetadataCachedInformation
    {
        private readonly Dictionary<ProviderMetadataId, CacheItem> _cache;
        private readonly int _maximumCacheSize;
        private readonly EventLogSession _session;
        private readonly string _logfile;

        private sealed class ProviderMetadataId
        {
            public ProviderMetadataId(string providerName, CultureInfo cultureInfo)
            {
                ProviderName = providerName;
                TheCultureInfo = cultureInfo;
            }

            public override bool Equals(object obj)
            {
                ProviderMetadataId rhs = obj as ProviderMetadataId;
                if (rhs == null)
                    return false;
                if (ProviderName.Equals(rhs.ProviderName) && (TheCultureInfo == rhs.TheCultureInfo))
                    return true;
                return false;
            }

            public override int GetHashCode()
            {
                return ProviderName.GetHashCode() ^ TheCultureInfo.GetHashCode();
            }

            public string ProviderName { get; }
            public CultureInfo TheCultureInfo { get; }
        }

        private sealed class CacheItem
        {
            public CacheItem(ProviderMetadata pm)
            {
                ProviderMetadata = pm;
                TheTime = DateTime.Now;
            }

            public DateTime TheTime { get; set; }

            public ProviderMetadata ProviderMetadata { get; }
        }

        public ProviderMetadataCachedInformation(EventLogSession session, string logfile, int maximumCacheSize)
        {
            Debug.Assert(session != null);
            _session = session;
            _logfile = logfile;
            _cache = new Dictionary<ProviderMetadataId, CacheItem>();
            _maximumCacheSize = maximumCacheSize;
        }

        private bool IsCacheFull()
        {
            return _cache.Count == _maximumCacheSize;
        }

        private bool IsProviderinCache(ProviderMetadataId key)
        {
            return _cache.ContainsKey(key);
        }

        private void DeleteCacheEntry(ProviderMetadataId key)
        {
            if (!IsProviderinCache(key))
                return;

            CacheItem value = _cache[key];
            _cache.Remove(key);

            value.ProviderMetadata.Dispose();
        }

        private void AddCacheEntry(ProviderMetadataId key, ProviderMetadata pm)
        {
            if (IsCacheFull())
                FlushOldestEntry();

            CacheItem value = new CacheItem(pm);
            _cache.Add(key, value);
            return;
        }

        private void FlushOldestEntry()
        {
            double maxPassedTime = -10;
            DateTime timeNow = DateTime.Now;
            ProviderMetadataId keyToDelete = null;

            // Get the entry in the cache which was not accessed for the longest time.
            foreach (KeyValuePair<ProviderMetadataId, CacheItem> kvp in _cache)
            {
                // The time difference (in ms) between the timeNow and the last used time of each entry
                TimeSpan timeDifference = timeNow.Subtract(kvp.Value.TheTime);

                // For the "unused" items (with ReferenceCount == 0)   -> can possible be deleted.
                if (timeDifference.TotalMilliseconds >= maxPassedTime)
                {
                    maxPassedTime = timeDifference.TotalMilliseconds;
                    keyToDelete = kvp.Key;
                }
            }

            if (keyToDelete != null)
                DeleteCacheEntry(keyToDelete);
        }

        private static void UpdateCacheValueInfoForHit(CacheItem cacheItem)
        {
            cacheItem.TheTime = DateTime.Now;
        }

        private ProviderMetadata GetProviderMetadata(ProviderMetadataId key)
        {
            if (!IsProviderinCache(key))
            {
                ProviderMetadata pm;
                try
                {
                    pm = new ProviderMetadata(key.ProviderName, _session, key.TheCultureInfo, _logfile);
                }
                catch (EventLogNotFoundException)
                {
                    pm = new ProviderMetadata(key.ProviderName, _session, key.TheCultureInfo);
                }
                AddCacheEntry(key, pm);
                return pm;
            }
            else
            {
                CacheItem cacheItem = _cache[key];
                ProviderMetadata pm = cacheItem.ProviderMetadata;

                // check Provider metadata to be sure it's hasn't been
                // uninstalled since last time it was used.

                try
                {
                    UpdateCacheValueInfoForHit(cacheItem);
                }
                catch (EventLogException)
                {
                    DeleteCacheEntry(key);
                    try
                    {
                        pm = new ProviderMetadata(key.ProviderName, _session, key.TheCultureInfo, _logfile);
                    }
                    catch (EventLogNotFoundException)
                    {
                        pm = new ProviderMetadata(key.ProviderName, _session, key.TheCultureInfo);
                    }
                    AddCacheEntry(key, pm);
                }

                return pm;
            }
        }

        public string GetFormatDescription(string ProviderName, EventLogHandle eventHandle)
        {
            lock (this)
            {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);

                try
                {
                    ProviderMetadata pm = GetProviderMetadata(key);
                    return NativeWrapper.EvtFormatMessageRenderName(pm.Handle, eventHandle, UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageEvent);
                }
                catch (EventLogNotFoundException)
                {
                    return null;
                }
            }
        }

        public string GetFormatDescription(string ProviderName, EventLogHandle eventHandle, string[] values)
        {
            lock (this)
            {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                try
                {
                    return NativeWrapper.EvtFormatMessageFormatDescription(pm.Handle, eventHandle, values);
                }
                catch (EventLogNotFoundException)
                {
                    return null;
                }
            }
        }

        public string GetLevelDisplayName(string ProviderName, EventLogHandle eventHandle)
        {
            lock (this)
            {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                return NativeWrapper.EvtFormatMessageRenderName(pm.Handle, eventHandle, UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageLevel);
            }
        }

        public string GetOpcodeDisplayName(string ProviderName, EventLogHandle eventHandle)
        {
            lock (this)
            {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                return NativeWrapper.EvtFormatMessageRenderName(pm.Handle, eventHandle, UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageOpcode);
            }
        }

        public string GetTaskDisplayName(string ProviderName, EventLogHandle eventHandle)
        {
            lock (this)
            {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                return NativeWrapper.EvtFormatMessageRenderName(pm.Handle, eventHandle, UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageTask);
            }
        }

        public IEnumerable<string> GetKeywordDisplayNames(string ProviderName, EventLogHandle eventHandle)
        {
            lock (this)
            {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                return NativeWrapper.EvtFormatMessageRenderKeywords(pm.Handle, eventHandle, UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageKeyword);
            }
        }
    }
}
