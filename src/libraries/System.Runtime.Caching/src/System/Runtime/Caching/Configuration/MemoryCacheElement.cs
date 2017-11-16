// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Configuration;

namespace System.Runtime.Caching.Configuration
{
    public sealed class MemoryCacheElement : ConfigurationElement
    {
        private static ConfigurationPropertyCollection s_properties;
        private static readonly ConfigurationProperty s_propName;
        private static readonly ConfigurationProperty s_propPhysicalMemoryLimitPercentage;
        private static readonly ConfigurationProperty s_propCacheMemoryLimitMegabytes;
        private static readonly ConfigurationProperty s_propPollingInterval;

        static MemoryCacheElement()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection();

            s_propName =
                new ConfigurationProperty("name",
                                          typeof(string),
                                          null,
                                          new WhiteSpaceTrimStringConverter(),
                                          new StringValidator(1),
                                          ConfigurationPropertyOptions.IsRequired |
                                          ConfigurationPropertyOptions.IsKey);

            s_propPhysicalMemoryLimitPercentage =
                new ConfigurationProperty("physicalMemoryLimitPercentage",
                                          typeof(int),
                                          (int)0,
                                          null,
                                          new IntegerValidator(0, 100),
                                          ConfigurationPropertyOptions.None);

            s_propCacheMemoryLimitMegabytes =
                new ConfigurationProperty("cacheMemoryLimitMegabytes",
                                          typeof(int),
                                          (int)0,
                                          null,
                                          new IntegerValidator(0, Int32.MaxValue),
                                          ConfigurationPropertyOptions.None);

            s_propPollingInterval =
                new ConfigurationProperty("pollingInterval",
                                          typeof(TimeSpan),
                                          TimeSpan.FromMilliseconds(ConfigUtil.DefaultPollingTimeMilliseconds),
                                          new InfiniteTimeSpanConverter(),
                                          new PositiveTimeSpanValidator(),
                                          ConfigurationPropertyOptions.None);

            s_properties.Add(s_propName);
            s_properties.Add(s_propPhysicalMemoryLimitPercentage);
            s_properties.Add(s_propCacheMemoryLimitMegabytes);
            s_properties.Add(s_propPollingInterval);
        }

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
