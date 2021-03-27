// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Caching.Configuration;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace System.Runtime.Caching
{
    internal sealed class MemoryCacheStatistics : IDisposable
    {
        private const int MEMORYSTATUS_INTERVAL_5_SECONDS = 5 * 1000;
        private const int MEMORYSTATUS_INTERVAL_30_SECONDS = 30 * 1000;

        private int _configCacheMemoryLimitMegabytes;
        private int _configPhysicalMemoryLimitPercentage;
        private int _configPollingInterval;
        private int _inCacheManagerThread;
        private int _disposed;
        private long _lastTrimCount;
        private long _lastTrimDurationTicks; // used only for debugging
        private int _lastTrimGen2Count;
        private int _lastTrimPercent;
        private DateTime _lastTrimTime;
        private int _pollingInterval;
        private GCHandleRef<Timer> _timerHandleRef;
        private readonly object _timerLock;
        private long _totalCountBeforeTrim;

        private CacheMemoryMonitor _cacheMemoryMonitor;
        private readonly MemoryCache _memoryCache;
        private readonly PhysicalMemoryMonitor _physicalMemoryMonitor;

        // private

        private MemoryCacheStatistics()
        {
            //hide default ctor
        }

        private void AdjustTimer()
        {
            lock (_timerLock)
            {
                if (_timerHandleRef == null)
                    return;

                Timer timer = _timerHandleRef.Target;

                // the order of these if statements is important

                // When above the high pressure mark, interval should be 5 seconds or less
                if (_physicalMemoryMonitor.IsAboveHighPressure() || _cacheMemoryMonitor.IsAboveHighPressure())
                {
                    if (_pollingInterval > MEMORYSTATUS_INTERVAL_5_SECONDS)
                    {
                        _pollingInterval = MEMORYSTATUS_INTERVAL_5_SECONDS;
                        timer.Change(_pollingInterval, _pollingInterval);
                    }
                    return;
                }

                // When above half the low pressure mark, interval should be 30 seconds or less
                if ((_cacheMemoryMonitor.PressureLast > _cacheMemoryMonitor.PressureLow / 2)
                    || (_physicalMemoryMonitor.PressureLast > _physicalMemoryMonitor.PressureLow / 2))
                {
                    // allow interval to fall back down when memory pressure goes away
                    int newPollingInterval = Math.Min(_configPollingInterval, MEMORYSTATUS_INTERVAL_30_SECONDS);
                    if (_pollingInterval != newPollingInterval)
                    {
                        _pollingInterval = newPollingInterval;
                        timer.Change(_pollingInterval, _pollingInterval);
                    }
                    return;
                }

                // there is no pressure, interval should be the value from config
                if (_pollingInterval != _configPollingInterval)
                {
                    _pollingInterval = _configPollingInterval;
                    timer.Change(_pollingInterval, _pollingInterval);
                }
            }
        }

        // timer callback
        private void CacheManagerTimerCallback(object state)
        {
            CacheManagerThread(0);
        }

        internal long GetLastSize()
        {
            return _cacheMemoryMonitor.PressureLast;
        }

        private int GetPercentToTrim()
        {
            int gen2Count = GC.CollectionCount(2);
            // has there been a Gen 2 Collection since the last trim?
            if (gen2Count != _lastTrimGen2Count)
            {
                return Math.Max(_physicalMemoryMonitor.GetPercentToTrim(_lastTrimTime, _lastTrimPercent), _cacheMemoryMonitor.GetPercentToTrim(_lastTrimTime, _lastTrimPercent));
            }
            else
            {
                return 0;
            }
        }

        private void InitializeConfiguration(NameValueCollection config)
        {
            MemoryCacheElement element = null;
            if (!_memoryCache.ConfigLess)
            {
                MemoryCacheSection section = ConfigurationManager.GetSection("system.runtime.caching/memoryCache") as MemoryCacheSection;
                if (section != null)
                {
                    element = section.NamedCaches[_memoryCache.Name];
                }
            }

            if (element != null)
            {
                _configCacheMemoryLimitMegabytes = element.CacheMemoryLimitMegabytes;
                _configPhysicalMemoryLimitPercentage = element.PhysicalMemoryLimitPercentage;
                double milliseconds = element.PollingInterval.TotalMilliseconds;
                _configPollingInterval = (milliseconds < (double)int.MaxValue) ? (int)milliseconds : int.MaxValue;
            }
            else
            {
                _configPollingInterval = ConfigUtil.DefaultPollingTimeMilliseconds;
                _configCacheMemoryLimitMegabytes = 0;
                _configPhysicalMemoryLimitPercentage = 0;
            }

            if (config != null)
            {
                _configPollingInterval = ConfigUtil.GetIntValueFromTimeSpan(config, ConfigUtil.PollingInterval, _configPollingInterval);
                _configCacheMemoryLimitMegabytes = ConfigUtil.GetIntValue(config, ConfigUtil.CacheMemoryLimitMegabytes, _configCacheMemoryLimitMegabytes, true, int.MaxValue);
                _configPhysicalMemoryLimitPercentage = ConfigUtil.GetIntValue(config, ConfigUtil.PhysicalMemoryLimitPercentage, _configPhysicalMemoryLimitPercentage, true, 100);
            }
#if !NETCOREAPP3_1_OR_GREATER
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _configPhysicalMemoryLimitPercentage > 0)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_PhysicalMemoryLimitPercentage);
            }
#endif
        }

        private void InitDisposableMembers()
        {
            bool dispose = true;
            try
            {
                _cacheMemoryMonitor = new CacheMemoryMonitor(_memoryCache, _configCacheMemoryLimitMegabytes);
                Timer timer;
                // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
                bool restoreFlow = false;
                try
                {
                    if (!ExecutionContext.IsFlowSuppressed())
                    {
                        ExecutionContext.SuppressFlow();
                        restoreFlow = true;
                    }

                    timer = new Timer(new TimerCallback(CacheManagerTimerCallback), null, _configPollingInterval, _configPollingInterval);
                }
                finally
                {
                    // Restore the current ExecutionContext
                    if (restoreFlow)
                        ExecutionContext.RestoreFlow();
                }

                _timerHandleRef = new GCHandleRef<Timer>(timer);
                dispose = false;
            }
            finally
            {
                if (dispose)
                {
                    Dispose();
                }
            }
        }

        private void SetTrimStats(long trimDurationTicks, long totalCountBeforeTrim, long trimCount)
        {
            _lastTrimDurationTicks = trimDurationTicks;

            int gen2Count = GC.CollectionCount(2);
            // has there been a Gen 2 Collection since the last trim?
            if (gen2Count != _lastTrimGen2Count)
            {
                _lastTrimTime = DateTime.UtcNow;
                _totalCountBeforeTrim = totalCountBeforeTrim;
                _lastTrimCount = trimCount;
            }
            else
            {
                // we've done multiple trims between Gen 2 collections, so only add to the trim count
                _lastTrimCount += trimCount;
            }
            _lastTrimGen2Count = gen2Count;

            _lastTrimPercent = (int)((_lastTrimCount * 100L) / _totalCountBeforeTrim);
        }

        private void Update()
        {
            _physicalMemoryMonitor.Update();
            _cacheMemoryMonitor.Update();
        }

        // public/internal

        internal long CacheMemoryLimit
        {
            get
            {
                return _cacheMemoryMonitor.MemoryLimit;
            }
        }

        internal long PhysicalMemoryLimit
        {
            get
            {
                return _physicalMemoryMonitor.MemoryLimit;
            }
        }

        internal TimeSpan PollingInterval
        {
            get
            {
                return TimeSpan.FromMilliseconds(_configPollingInterval);
            }
        }

        internal MemoryCacheStatistics(MemoryCache memoryCache, NameValueCollection config)
        {
            _memoryCache = memoryCache;
            _lastTrimGen2Count = -1;
            _lastTrimTime = DateTime.MinValue;
            _timerLock = new object();
            InitializeConfiguration(config);
            _pollingInterval = _configPollingInterval;
            _physicalMemoryMonitor = new PhysicalMemoryMonitor(_configPhysicalMemoryLimitPercentage);
            InitDisposableMembers();
        }

        internal long CacheManagerThread(int minPercent)
        {
            if (Interlocked.Exchange(ref _inCacheManagerThread, 1) != 0)
                return 0;
            try
            {
                if (_disposed == 1)
                {
                    return 0;
                }
                Dbg.Trace("MemoryCacheStats", "**BEG** CacheManagerThread " + DateTime.Now.ToString("T", CultureInfo.InvariantCulture));

                // The timer thread must always call Update so that the CacheManager
                // knows the size of the cache.
                Update();
                AdjustTimer();

                int percent = Math.Max(minPercent, GetPercentToTrim());
                long beginTotalCount = _memoryCache.GetCount();
                Stopwatch sw = Stopwatch.StartNew();
                long trimmedOrExpired = _memoryCache.Trim(percent);
                sw.Stop();
                // 1) don't update stats if the trim happend because MAX_COUNT was exceeded
                // 2) don't update stats unless we removed at least one entry
                if (percent > 0 && trimmedOrExpired > 0)
                {
                    SetTrimStats(sw.Elapsed.Ticks, beginTotalCount, trimmedOrExpired);
                }

                Dbg.Trace("MemoryCacheStats", "**END** CacheManagerThread: "
                            + ", percent=" + percent
                            + ", beginTotalCount=" + beginTotalCount
                            + ", trimmed=" + trimmedOrExpired
                            + ", Milliseconds=" + sw.ElapsedMilliseconds);

#if PERF
                Debug.WriteLine("CacheCommon.CacheManagerThread:"
                                                    + " minPercent= " + minPercent
                                                    + ", percent= " + percent
                                                    + ", beginTotalCount=" + beginTotalCount
                                                    + ", trimmed=" + trimmedOrExpired
                                                    + ", Milliseconds=" + sw.ElapsedMilliseconds + Environment.NewLine);
#endif
                return trimmedOrExpired;
            }
            finally
            {
                Interlocked.Exchange(ref _inCacheManagerThread, 0);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                lock (_timerLock)
                {
                    GCHandleRef<Timer> timerHandleRef = _timerHandleRef;
                    if (timerHandleRef != null && Interlocked.CompareExchange(ref _timerHandleRef, null, timerHandleRef) == timerHandleRef)
                    {
                        timerHandleRef.Dispose();
                        Dbg.Trace("MemoryCacheStats", "Stopped CacheMemoryTimers");
                    }
                }
                while (_inCacheManagerThread != 0)
                {
                    Thread.Sleep(100);
                }
                if (_cacheMemoryMonitor != null)
                {
                    _cacheMemoryMonitor.Dispose();
                }
                // Don't need to call GC.SuppressFinalize(this) for sealed types without finalizers.
            }
        }

        internal void UpdateConfig(NameValueCollection config)
        {
            int pollingInterval = ConfigUtil.GetIntValueFromTimeSpan(config, ConfigUtil.PollingInterval, _configPollingInterval);
            int cacheMemoryLimitMegabytes = ConfigUtil.GetIntValue(config, ConfigUtil.CacheMemoryLimitMegabytes, _configCacheMemoryLimitMegabytes, true, int.MaxValue);
            int physicalMemoryLimitPercentage = ConfigUtil.GetIntValue(config, ConfigUtil.PhysicalMemoryLimitPercentage, _configPhysicalMemoryLimitPercentage, true, 100);

            if (pollingInterval != _configPollingInterval)
            {
                lock (_timerLock)
                {
                    _configPollingInterval = pollingInterval;
                }
            }

            if (cacheMemoryLimitMegabytes == _configCacheMemoryLimitMegabytes
                && physicalMemoryLimitPercentage == _configPhysicalMemoryLimitPercentage)
            {
                return;
            }

            try
            {
                try
                {
                }
                finally
                {
                    // prevent ThreadAbortEx from interrupting
                    while (Interlocked.Exchange(ref _inCacheManagerThread, 1) != 0)
                    {
                        Thread.Sleep(100);
                    }
                }
                if (_disposed == 0)
                {
                    if (cacheMemoryLimitMegabytes != _configCacheMemoryLimitMegabytes)
                    {
                        _cacheMemoryMonitor.SetLimit(cacheMemoryLimitMegabytes);
                        _configCacheMemoryLimitMegabytes = cacheMemoryLimitMegabytes;
                    }
                    if (physicalMemoryLimitPercentage != _configPhysicalMemoryLimitPercentage)
                    {
                        _physicalMemoryMonitor.SetLimit(physicalMemoryLimitPercentage);
                        _configPhysicalMemoryLimitPercentage = physicalMemoryLimitPercentage;
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _inCacheManagerThread, 0);
            }
        }
    }
}
