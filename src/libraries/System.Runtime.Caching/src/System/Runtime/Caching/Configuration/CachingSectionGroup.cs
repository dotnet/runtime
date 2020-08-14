// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Configuration;

namespace System.Runtime.Caching.Configuration
{
    internal sealed class CachingSectionGroup : ConfigurationSectionGroup
    {
        public CachingSectionGroup()
        {
        }

        // public properties
        [ConfigurationProperty("memoryCache")]
        public MemoryCacheSection MemoryCaches
        {
            get
            {
                return (MemoryCacheSection)Sections["memoryCache"];
            }
        }
    }
}
