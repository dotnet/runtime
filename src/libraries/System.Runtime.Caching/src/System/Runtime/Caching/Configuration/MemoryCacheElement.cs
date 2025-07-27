// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.Caching.Resources;
using System.Runtime.Versioning;

namespace System.Runtime.Caching.Configuration
{
    /// <summary>
    /// Defines the physical memory monitoring modes for the cache.
    /// </summary>
    internal enum PhysicalMemoryMode
    {
        /// <summary>
        /// Legacy mode - uses platform-specific memory detection with GC-induced stats on non-Windows.
        /// </summary>
        Legacy = 0,

        /// <summary>
        /// Default mode - uses GCMemoryInfo without inducing GC collections.
        /// </summary>
        Default = 1,

        /// <summary>
        /// GC thresholds mode - uses GCMemoryInfo.HighMemoryLoadThresholdBytes instead of percentage of total memory.
        /// </summary>
        GCThresholds = 2
    }

#if NETCOREAPP
    [UnsupportedOSPlatform("browser")]
#endif
    internal sealed class MemoryCacheElement : ConfigurationElement
    {
        private static readonly ConfigurationProperty s_propName =
            new ConfigurationProperty("name",
                typeof(string),
                null,
                new WhiteSpaceTrimStringConverter(),
                new StringValidator(1),
                ConfigurationPropertyOptions.IsRequired |
                ConfigurationPropertyOptions.IsKey);
        private static readonly ConfigurationProperty s_propPhysicalMemoryLimitPercentage =
            new ConfigurationProperty("physicalMemoryLimitPercentage",
                typeof(int),
                (int)0,
                null,
                new IntegerValidator(0, 100),
                ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty s_propPhysicalMemoryMode =
            new ConfigurationProperty("physicalMemoryMode",
                typeof(string),
                "Legacy",
                null,
                null,
                ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty s_propCacheMemoryLimitMegabytes =
            new ConfigurationProperty("cacheMemoryLimitMegabytes",
                typeof(int),
                (int)0,
                null,
                new IntegerValidator(0, int.MaxValue),
                ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty s_propPollingInterval =
            new ConfigurationProperty("pollingInterval",
                typeof(TimeSpan),
                TimeSpan.FromMilliseconds(ConfigUtil.DefaultPollingTimeMilliseconds),
                new InfiniteTimeSpanConverter(),
                new PositiveTimeSpanValidator(),
                ConfigurationPropertyOptions.None);
        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection()
        {
            s_propName,
            s_propPhysicalMemoryLimitPercentage,
            s_propPhysicalMemoryMode,
            s_propCacheMemoryLimitMegabytes,
            s_propPollingInterval
        };

        internal MemoryCacheElement()
        {
        }

        public MemoryCacheElement(string name)
        {
            Name = name;
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                return s_properties;
            }
        }

        [ConfigurationProperty("name", DefaultValue = "", IsRequired = true, IsKey = true)]
        [TypeConverter(typeof(WhiteSpaceTrimStringConverter))]
        [StringValidator(MinLength = 1)]
        public string Name
        {
            get
            {
                return (string)base["name"];
            }
            set
            {
                base["name"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the percentage of physical memory that can be used before cache entries are removed.
        /// Valid values: 0 (auto-calculated defaults), 1-100 (specific percentage of physical memory).
        /// </summary>
        [ConfigurationProperty("physicalMemoryLimitPercentage", DefaultValue = (int)0)]
        [IntegerValidator(MinValue = 0, MaxValue = 100)]
        public int PhysicalMemoryLimitPercentage
        {
            get
            {
                return (int)base["physicalMemoryLimitPercentage"];
            }
            set
            {
                base["physicalMemoryLimitPercentage"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the physical memory monitoring mode.
        /// Valid values:
        /// - "Legacy": Platform-specific memory detection (default)
        /// - "Default": Use GC.GetGCMemoryInfo().TotalAvailableMemoryBytes without inducing GC
        /// - "GCThresholds": Follow GC's high memory load threshold
        /// - "Default:1234567890": Use Default mode - specified against a static amount of available RAM (in bytes)
        /// </summary>
        [ConfigurationProperty("physicalMemoryMode", DefaultValue = "Legacy")]
        internal string PhysicalMemoryModeRaw
        {
            get
            {
                return (string)base["physicalMemoryMode"];
            }
        }

        private bool _modeIsParsed;
        private PhysicalMemoryMode _parsedMode;
        private long? _parsedMemoryBytes;

        /// <summary>
        /// Gets the parsed physical memory mode enum value.
        /// </summary>
        public PhysicalMemoryMode PhysicalMemoryMode
        {
            get
            {
                if (!_modeIsParsed)
                {
                    ConfigUtil.ParsePhysicalMemoryMode(PhysicalMemoryModeRaw, out _parsedMode, out _parsedMemoryBytes);
                    _modeIsParsed = true;
                }
                return _parsedMode;
            }
        }

        /// <summary>
        /// Gets the parsed memory bytes value if specified in the format "Mode:bytes", otherwise null.
        /// </summary>
        public long? PhysicalMemoryBytes
        {
            get
            {
                if (!_modeIsParsed)
                {
                    ConfigUtil.ParsePhysicalMemoryMode(PhysicalMemoryModeRaw, out _parsedMode, out _parsedMemoryBytes);
                    _modeIsParsed = true;
                }
                return _parsedMemoryBytes;
            }
        }

        [ConfigurationProperty("cacheMemoryLimitMegabytes", DefaultValue = (int)0)]
        [IntegerValidator(MinValue = 0)]
        public int CacheMemoryLimitMegabytes
        {
            get
            {
                return (int)base["cacheMemoryLimitMegabytes"];
            }
            set
            {
                base["cacheMemoryLimitMegabytes"] = value;
            }
        }

        [ConfigurationProperty("pollingInterval", DefaultValue = "00:02:00")]
        [TypeConverter(typeof(InfiniteTimeSpanConverter))]
        public TimeSpan PollingInterval
        {
            get
            {
                return (TimeSpan)base["pollingInterval"];
            }
            set
            {
                base["pollingInterval"] = value;
            }
        }
    }
}
