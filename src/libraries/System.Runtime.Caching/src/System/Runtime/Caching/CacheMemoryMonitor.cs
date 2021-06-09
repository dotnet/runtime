// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Caching.Configuration;
using System.Runtime.Caching.Hosting;
using System.Diagnostics;
using System.Security;
using System.Threading;

namespace System.Runtime.Caching
{
    // CacheMemoryMonitor uses the internal System.SizedReference type to determine
    // the size of the cache itselt, and helps us know when to drop entries to avoid
    // exceeding the cache's memory limit.  The limit is configurable (see ConfigUtil.cs).
    internal sealed class CacheMemoryMonitor : MemoryMonitor, IDisposable
    {
        private const long PRIVATE_BYTES_LIMIT_2GB = 800 * MEGABYTE;
        private const long PRIVATE_BYTES_LIMIT_3GB = 1800 * MEGABYTE;
        private const long PRIVATE_BYTES_LIMIT_64BIT = 1L * TERABYTE;
        private const int SAMPLE_COUNT = 2;

        private static IMemoryCacheManager s_memoryCacheManager;
        private static long s_autoPrivateBytesLimit = -1;
        private static long s_effectiveProcessMemoryLimit = -1;

        private readonly MemoryCache _memoryCache;
        private readonly long[] _cacheSizeSamples;
        private readonly DateTime[] _cacheSizeSampleTimes;
        private int _idx;
        private SRefMultiple _sizedRefMultiple;
        private int _gen2Count;
        private long _memoryLimit;

        internal long MemoryLimit
        {
            get { return _memoryLimit; }
        }

        private CacheMemoryMonitor()
        {
            // hide default ctor
        }

        internal CacheMemoryMonitor(MemoryCache memoryCache, int cacheMemoryLimitMegabytes)
        {
            _memoryCache = memoryCache;
            _gen2Count = GC.CollectionCount(2);
            _cacheSizeSamples = new long[SAMPLE_COUNT];
            _cacheSizeSampleTimes = new DateTime[SAMPLE_COUNT];
            if (memoryCache.UseMemoryCacheManager)
            {
                InitMemoryCacheManager();   // This magic thing connects us to ObjectCacheHost magically. :/
            }
            InitDisposableMembers(cacheMemoryLimitMegabytes);
        }

        private void InitDisposableMembers(int cacheMemoryLimitMegabytes)
        {
            bool dispose = true;
            try
            {
                _sizedRefMultiple = new SRefMultiple(_memoryCache.AllSRefTargets);
                SetLimit(cacheMemoryLimitMegabytes);
                InitHistory();
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

        // Auto-generate the private bytes limit:
        // - On 64bit, the auto value is MIN(60% physical_ram, 1 TB)
        // - On x86, for 2GB, the auto value is MIN(60% physical_ram, 800 MB)
        // - On x86, for 3GB, the auto value is MIN(60% physical_ram, 1800 MB)
        //
        // - If it's not a hosted environment (e.g. console app), the 60% in the above
        //   formulas will become 100% because in un-hosted environment we don't launch
        //   other processes such as compiler, etc.
        private static long AutoPrivateBytesLimit
        {
            get
            {
                long memoryLimit = s_autoPrivateBytesLimit;
                if (memoryLimit == -1)
                {
                    bool is64bit = (IntPtr.Size == 8);

                    long totalPhysical = TotalPhysical;
                    long totalVirtual = TotalVirtual;
                    if (totalPhysical != 0)
                    {
                        long recommendedPrivateByteLimit;
                        if (is64bit)
                        {
                            recommendedPrivateByteLimit = PRIVATE_BYTES_LIMIT_64BIT;
                        }
                        else
                        {
                            // Figure out if it's 2GB or 3GB

                            if (totalVirtual > 2 * GIGABYTE)
                            {
                                recommendedPrivateByteLimit = PRIVATE_BYTES_LIMIT_3GB;
                            }
                            else
                            {
                                recommendedPrivateByteLimit = PRIVATE_BYTES_LIMIT_2GB;
                            }
                        }

                        // use 60% of physical RAM
                        long usableMemory = totalPhysical * 3 / 5;
                        memoryLimit = Math.Min(usableMemory, recommendedPrivateByteLimit);
                    }
                    else
                    {
                        // If GlobalMemoryStatusEx fails, we'll use these as our auto-gen private bytes limit
                        memoryLimit = is64bit ? PRIVATE_BYTES_LIMIT_64BIT : PRIVATE_BYTES_LIMIT_2GB;
                    }
                    Interlocked.Exchange(ref s_autoPrivateBytesLimit, memoryLimit);
                }

                return memoryLimit;
            }
        }

        public void Dispose()
        {
            SRefMultiple sref = _sizedRefMultiple;
            if (sref != null && Interlocked.CompareExchange(ref _sizedRefMultiple, null, sref) == sref)
            {
                sref.Dispose();
            }
            IMemoryCacheManager memoryCacheManager = s_memoryCacheManager;
            if (memoryCacheManager != null)
            {
                memoryCacheManager.ReleaseCache(_memoryCache);
            }
        }

        internal static long EffectiveProcessMemoryLimit
        {
            get
            {
                long memoryLimit = s_effectiveProcessMemoryLimit;
                if (memoryLimit == -1)
                {
                    memoryLimit = AutoPrivateBytesLimit;
                    Interlocked.Exchange(ref s_effectiveProcessMemoryLimit, memoryLimit);
                }
                return memoryLimit;
            }
        }

        protected override int GetCurrentPressure()
        {
            // Call GetUpdatedTotalCacheSize to update the total
            // cache size, if there has been a recent Gen 2 Collection.
            // This update must happen, otherwise the CacheManager won't
            // know the total cache size.
            int gen2Count = GC.CollectionCount(2);
            SRefMultiple sref = _sizedRefMultiple;
            if (gen2Count != _gen2Count && sref != null)
            {
                // update _gen2Count
                _gen2Count = gen2Count;

                // the SizedRef is only updated after a Gen2 Collection

                // increment the index (it's either 1 or 0)
                Debug.Assert(SAMPLE_COUNT == 2);
                _idx = _idx ^ 1;
                // remember the sample time
                _cacheSizeSampleTimes[_idx] = DateTime.UtcNow;
                // remember the sample value
                _cacheSizeSamples[_idx] = sref.ApproximateSize;
                Dbg.Trace("MemoryCacheStats", "SizedRef.ApproximateSize=" + _cacheSizeSamples[_idx]);
                IMemoryCacheManager memoryCacheManager = s_memoryCacheManager;
                if (memoryCacheManager != null)
                {
                    memoryCacheManager.UpdateCacheSize(_cacheSizeSamples[_idx], _memoryCache);
                }
            }

            // if there's no memory limit, then there's nothing more to do
            if (_memoryLimit <= 0)
            {
                return 0;
            }

            long cacheSize = _cacheSizeSamples[_idx];

            // use _memoryLimit as an upper bound so that pressure is a percentage (between 0 and 100, inclusive).
            if (cacheSize > _memoryLimit)
            {
                cacheSize = _memoryLimit;
            }

            // PerfCounter: Cache Percentage Process Memory Limit Used
            //    = memory used by this process / process memory limit at pressureHigh
            // Set private bytes used in kilobytes because the counter is a DWORD

            // PerfCounters.SetCounter(AppPerfCounter.CACHE_PERCENT_PROC_MEM_LIMIT_USED, (int)(cacheSize >> KILOBYTE_SHIFT));

            int result = (int)(cacheSize * 100 / _memoryLimit);
            return result;
        }

        internal override int GetPercentToTrim(DateTime lastTrimTime, int lastTrimPercent)
        {
            int percent = 0;
            if (IsAboveHighPressure())
            {
                long cacheSize = _cacheSizeSamples[_idx];
                if (cacheSize > _memoryLimit)
                {
                    percent = Math.Min(100, (int)((cacheSize - _memoryLimit) * 100L / cacheSize));
                }

#if PERF
                Debug.WriteLine($"CacheMemoryMonitor.GetPercentToTrim: percent={percent:N}, lastTrimPercent={lastTrimPercent:N}{Environment.NewLine}");
#endif
            }
            return percent;
        }

        internal void SetLimit(int cacheMemoryLimitMegabytes)
        {
            long cacheMemoryLimit = ((long)cacheMemoryLimitMegabytes) << MEGABYTE_SHIFT;
            _memoryLimit = cacheMemoryLimit != 0 ?
                cacheMemoryLimit :
                EffectiveProcessMemoryLimit;

            Dbg.Trace("MemoryCacheStats", "CacheMemoryMonitor.SetLimit: _memoryLimit=" + (_memoryLimit >> MEGABYTE_SHIFT) + "Mb");

            if (_memoryLimit > 0)
            {
                _pressureHigh = 100;
                _pressureLow = 80;
            }
            else
            {
                _pressureHigh = 99;
                _pressureLow = 97;
            }

            Dbg.Trace("MemoryCacheStats", "CacheMemoryMonitor.SetLimit: _pressureHigh=" + _pressureHigh +
                        ", _pressureLow=" + _pressureLow);
        }

        private static void InitMemoryCacheManager()
        {
            if (s_memoryCacheManager == null)
            {
                IMemoryCacheManager memoryCacheManager = null;
                IServiceProvider host = ObjectCache.Host;
                if (host != null)
                {
                    memoryCacheManager = host.GetService(typeof(IMemoryCacheManager)) as IMemoryCacheManager;
                }
                if (memoryCacheManager != null)
                {
                    Interlocked.CompareExchange(ref s_memoryCacheManager, memoryCacheManager, null);
                }
            }
        }
    }
}
