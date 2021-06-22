// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Configuration;
using System.Runtime.Versioning;

namespace System.Runtime.Caching.Configuration
{
#if NET5_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
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
